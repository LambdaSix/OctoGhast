using System;
using System.Linq;
using OctoGhast.Framework.Ecs;
using OctoGhast.Spatial;

namespace OctoGhast.Simulation.Movement {
    /// <summary>
    /// Resolves an intent against authoritative terrain/occupancy, commits movement through the
    /// world's atomic spatial mutation seam, then hands the resolved action cost to scheduling.
    /// This type has no transport, renderer, input-device, or ECS-library dependency.
    /// </summary>
    public sealed class MovementCommandProcessor : IMoveCommandSink, IMovementCommandContext {
        private readonly IMovementWorld _world;
        private readonly IActionCostPolicy _costPolicy;
        private readonly IActionScheduler _scheduler;

        public MovementCommandProcessor(IMovementWorld world, IActionCostPolicy costPolicy, IActionScheduler scheduler) {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _costPolicy = costPolicy ?? throw new ArgumentNullException(nameof(costPolicy));
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        }

        public MovementResult Submit(MoveCommand command) {
            if (command == null) throw new ArgumentNullException(nameof(command));
            var lifecycle = new CommandLifecycle(command);
            lifecycle.Admit(0);
            return ResolveAdmitted(lifecycle);
        }

        public MovementResult Submit(MoveCommand command, long admissionSequence) {
            if (command == null) throw new ArgumentNullException(nameof(command));
            var lifecycle = new CommandLifecycle(command);
            lifecycle.Admit(admissionSequence);
            return ResolveAdmitted(lifecycle);
        }

        public MovementResult ResolveAdmitted(CommandLifecycle lifecycle) {
            if (lifecycle == null) throw new ArgumentNullException(nameof(lifecycle));
            lifecycle.Resolve(this);
            var command = lifecycle.Command as MoveCommand;
            if (command == null) throw new ArgumentException("The admitted command is not a movement command.", nameof(lifecycle));
            return command.Resolution;
        }

        public MovementResult ResolveMove(MoveCommand command) {
            if (command == null) throw new ArgumentNullException(nameof(command));
            MovementActorState actor;
            if (!_world.TryGetActor(command.Actor, out actor) || actor == null || actor.Id != command.Actor) {
                var origin = new WorldPosition(0, 0, 0);
                return Result(MovementOutcomeKind.Rejected, command.Actor, origin, origin, null, null, 0);
            }
            if (!actor.CanMove) return Result(MovementOutcomeKind.Rejected, actor.Id, actor.Position, actor.Position, null, null, 0);

            WorldPosition destination;
            if (!TryDestination(actor.Position, command.Direction, out destination)) {
                return Result(MovementOutcomeKind.Rejected, actor.Id, actor.Position, actor.Position, null, null, 0);
            }

            var terrain = _world.GetTerrain(destination);
            if (terrain == null || !terrain.IsTraversable) {
                int blockedCost = ResolveCost(actor, MovementOutcomeKind.BlockedByTerrain, terrain, command.Direction);
                ScheduleIfCharged(actor.Id, blockedCost);
                return Result(MovementOutcomeKind.BlockedByTerrain, actor.Id, actor.Position, destination, null, null, blockedCost);
            }

            var blocker = _world.GetBlockingEntities(destination)
                .Where(id => id != actor.Id)
                .Distinct()
                .OrderBy(id => id)
                .Select(id => (EntityId?)id)
                .FirstOrDefault();
            if (blocker.HasValue) {
                if (_world.IsHostile(actor.Id, blocker.Value)) {
                    var interaction = new InteractionRequest(actor.Id, blocker.Value, "melee");
                    return Result(MovementOutcomeKind.BumpInteraction, actor.Id, actor.Position, destination,
                        blocker, interaction, 0);
                }

                int occupiedCost = ResolveCost(actor, MovementOutcomeKind.BlockedByEntity, terrain, command.Direction);
                ScheduleIfCharged(actor.Id, occupiedCost);
                return Result(MovementOutcomeKind.BlockedByEntity, actor.Id, actor.Position, destination,
                    blocker, null, occupiedCost);
            }

            int actionCost = ResolveCost(actor, MovementOutcomeKind.Moved, terrain, command.Direction);
            if (actionCost <= 0) throw new InvalidOperationException("A successful movement must have a positive action cost.");
            if (!_world.TryMove(actor.Id, actor.Position, destination)) {
                return Result(MovementOutcomeKind.Rejected, actor.Id, actor.Position, destination, null, null, 0);
            }

            // The world adapter contract makes position/index mutation atomic. Scheduling is a
            // deterministic post-resolution operation and receives no presentation time values.
            _scheduler.Reschedule(actor.Id, actionCost);
            return Result(MovementOutcomeKind.Moved, actor.Id, actor.Position, destination, null, null, actionCost);
        }

        private int ResolveCost(MovementActorState actor, MovementOutcomeKind outcome, TerrainMovementState terrain, MoveDirection direction) {
            int cost = _costPolicy.CostFor(actor, outcome, terrain == null ? 0 : terrain.BaseMoveCost, direction);
            if (cost < 0) throw new InvalidOperationException("Action cost policies cannot return negative costs.");
            return cost;
        }

        private void ScheduleIfCharged(EntityId actor, int cost) {
            if (cost > 0) _scheduler.Reschedule(actor, cost);
        }

        private static MovementResult Result(MovementOutcomeKind outcome, EntityId actor, WorldPosition from,
            WorldPosition to, EntityId? blocker, InteractionRequest interaction, int cost) =>
            new MovementResult(outcome, actor, from, to, blocker, interaction, cost);

        private static bool TryDestination(WorldPosition origin, MoveDirection direction, out WorldPosition destination) {
            int dx;
            int dy;
            switch (direction) {
                case MoveDirection.North: dx = 0; dy = -1; break;
                case MoveDirection.NorthEast: dx = 1; dy = -1; break;
                case MoveDirection.East: dx = 1; dy = 0; break;
                case MoveDirection.SouthEast: dx = 1; dy = 1; break;
                case MoveDirection.South: dx = 0; dy = 1; break;
                case MoveDirection.SouthWest: dx = -1; dy = 1; break;
                case MoveDirection.West: dx = -1; dy = 0; break;
                case MoveDirection.NorthWest: dx = -1; dy = -1; break;
                default: destination = origin; return false;
            }

            try {
                destination = new WorldPosition(origin.X + dx, origin.Y + dy, origin.Z);
                return true;
            } catch (OverflowException) {
                destination = origin;
                return false;
            }
        }
    }

    /// <summary>
    /// Minimal Cataclysm-profile action-cost policy. The terrain adapter supplies the normalized
    /// base move cost and Character components supply their composed movement-cost percentage.
    /// This preserves the pinned walking/crouching/prone test ratio (100/200/600 on a 100-cost
    /// normalized tile); terrain, encumbrance, effects and diagonal formulas remain profile inputs.
    /// Blocked moves and bump interactions are not charged as a terrain traversal by this policy.
    /// </summary>
    public sealed class CataclysmMovementCostPolicy : IActionCostPolicy {
        public int CostFor(MovementActorState actor, MovementOutcomeKind outcome, int destinationTerrainMoveCost, MoveDirection direction) {
            if (outcome != MovementOutcomeKind.Moved) return 0;
            if (destinationTerrainMoveCost <= 0) {
                throw new InvalidOperationException("Traversable terrain must define a positive base move cost.");
            }
            if (actor == null) throw new ArgumentNullException(nameof(actor));
            long adjusted = checked((long)destinationTerrainMoveCost * actor.MovementCostPercent);
            return checked((int)((adjusted + 99) / 100));
        }
    }
}
