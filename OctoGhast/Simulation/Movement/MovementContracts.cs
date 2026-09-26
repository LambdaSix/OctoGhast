using System;
using System.Collections.Generic;
using OctoGhast.Framework.Ecs;
using OctoGhast.Spatial;
using OctoGhast.Simulation;

namespace OctoGhast.Simulation.Movement {
    public enum MoveDirection {
        North, NorthEast, East, SouthEast, South, SouthWest, West, NorthWest
    }

    public enum MovementOutcomeKind {
        Moved,
        BlockedByTerrain,
        BlockedByEntity,
        BumpInteraction,
        Rejected
    }

    /// <summary>Input-neutral action request emitted by players, AI, or an in-process client.</summary>
    public sealed class MoveCommand : ISimulationCommand {
        public StableSimulationId CommandId { get; }
        public string CommandTypeId => "cataclysm.move";
        public CommandRetryPolicy RetryPolicy => CommandRetryPolicy.StateReconciled;
        public StableSimulationId? PlayerId { get; }
        public StableSimulationId ActorId { get; }
        public long RequestSequence { get; }
        public EntityId Actor { get; }
        public MoveDirection Direction { get; }
        public MovementResult Resolution { get; private set; }

        public MoveCommand(StableSimulationId commandId, EntityId actor, MoveDirection direction,
            StableSimulationId? playerId = null, long requestSequence = 0) {
            if (String.IsNullOrWhiteSpace(commandId.Value)) throw new ArgumentException("A stable command identity is required.", nameof(commandId));
            if (!actor.IsValid) throw new ArgumentException("A durable actor identity is required.", nameof(actor));
            if (playerId.HasValue && String.IsNullOrWhiteSpace(playerId.Value.Value)) throw new ArgumentException("A stable player identity is required.", nameof(playerId));
            if (requestSequence < 0) throw new ArgumentOutOfRangeException(nameof(requestSequence));
            CommandId = commandId;
            PlayerId = playerId;
            ActorId = new StableSimulationId(actor.ToString());
            RequestSequence = requestSequence;
            Actor = actor;
            Direction = direction;
        }

        public CommandOutcome Resolve(ISimulationCommandContext context) {
            if (Resolution != null) throw new InvalidOperationException("A movement command can be resolved only once.");
            var movementContext = context as IMovementCommandContext;
            if (movementContext == null) throw new ArgumentException("Movement requires an authoritative movement context.", nameof(context));
            Resolution = movementContext.ResolveMove(this);
            return Resolution.Outcome == MovementOutcomeKind.Rejected
                ? CommandOutcome.Rejected("move.rejected")
                : CommandOutcome.Success("move." + Resolution.Outcome.ToString().ToLowerInvariant());
        }
    }

    /// <summary>Composition-derived movement capability read for one deterministic resolution.</summary>
    public sealed class MovementActorState {
        public EntityId Id { get; }
        public WorldPosition Position { get; }
        public bool CanMove { get; }
        /// <summary>Cataclysm profile actor modifier already derived from movement-state components.</summary>
        public int MovementCostPercent { get; }

        public MovementActorState(EntityId id, WorldPosition position, bool canMove, int movementCostPercent = 100) {
            if (!id.IsValid) throw new ArgumentException("A durable actor identity is required.", nameof(id));
            if (movementCostPercent <= 0) throw new ArgumentOutOfRangeException(nameof(movementCostPercent));
            Id = id;
            Position = position;
            CanMove = canMove;
            MovementCostPercent = movementCostPercent;
        }
    }

    public sealed class TerrainMovementState {
        public bool IsTraversable { get; }
        public int BaseMoveCost { get; }

        public TerrainMovementState(bool isTraversable, int baseMoveCost) {
            if (baseMoveCost < 0) throw new ArgumentOutOfRangeException(nameof(baseMoveCost));
            IsTraversable = isTraversable;
            BaseMoveCost = baseMoveCost;
        }
    }

    /// <summary>
    /// Adapter to the authoritative entity, terrain and spatial services. TryMove must atomically
    /// update the authoritative position, derived spatial cell and index; it must not partially commit.
    /// </summary>
    public interface IMovementWorld {
        bool TryGetActor(EntityId actor, out MovementActorState state);
        TerrainMovementState GetTerrain(WorldPosition position);
        IEnumerable<EntityId> GetBlockingEntities(WorldPosition position);
        bool IsHostile(EntityId actor, EntityId other);
        bool TryMove(EntityId actor, WorldPosition expectedFrom, WorldPosition to);
    }

    /// <summary>Scheduling is downstream of resolution and expressed only in action-cost units.</summary>
    public interface IActionCostPolicy {
        int CostFor(MovementActorState actor, MovementOutcomeKind outcome, int destinationTerrainMoveCost, MoveDirection direction);
    }

    public interface IActionScheduler {
        void Reschedule(EntityId actor, int actionCost);
    }

    public sealed class InteractionRequest {
        public EntityId Initiator { get; }
        public EntityId Target { get; }
        public string InteractionKind { get; }

        public InteractionRequest(EntityId initiator, EntityId target, string interactionKind) {
            if (!initiator.IsValid) throw new ArgumentException("A durable initiator identity is required.", nameof(initiator));
            if (!target.IsValid) throw new ArgumentException("A durable target identity is required.", nameof(target));
            Initiator = initiator;
            Target = target;
            InteractionKind = interactionKind ?? throw new ArgumentNullException(nameof(interactionKind));
        }
    }

    public sealed class MovementResult {
        public MovementOutcomeKind Outcome { get; }
        public EntityId Actor { get; }
        public WorldPosition From { get; }
        public WorldPosition To { get; }
        public EntityId? BlockingEntity { get; }
        public InteractionRequest Interaction { get; }
        public int ActionCost { get; }

        public MovementResult(MovementOutcomeKind outcome, EntityId actor, WorldPosition from, WorldPosition to,
            EntityId? blockingEntity, InteractionRequest interaction, int actionCost) {
            if (actionCost < 0) throw new ArgumentOutOfRangeException(nameof(actionCost));
            Outcome = outcome;
            Actor = actor;
            From = from;
            To = to;
            BlockingEntity = blockingEntity;
            Interaction = interaction;
            ActionCost = actionCost;
        }
    }

    public interface IMoveCommandSink {
        MovementResult Submit(MoveCommand command);
    }

    public interface IMovementCommandContext : ISimulationCommandContext {
        MovementResult ResolveMove(MoveCommand command);
    }
}
