using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OctoGhast.Framework.Ecs;
using OctoGhast.Spatial;
using OctoGhast.Simulation;
using OctoGhast.Simulation.Movement;

namespace OctoGhast.Core.Tests.Movement {
    [TestFixture]
    public sealed class MovementCommandTests {
        private static EntityId Entity(int value) => EntityId.Parse(value.ToString("x32"));
        private static MoveCommand Command(string id, EntityId actor, MoveDirection direction) =>
            new MoveCommand(new StableSimulationId(id), actor, direction);
        private static readonly EntityId Player = Entity(1);
        private static readonly EntityId Hostile = Entity(2);

        [Test]
        public void MovingIntoPassableTerrainUpdatesPositionIndexAndSchedulesTerrainCost() {
            var world = new TestWorld();
            world.AddActor(Player, new WorldPosition(0, 0, 0));
            world.SetTerrain(new WorldPosition(1, 0, 0), true, 125);
            var scheduler = new TestScheduler();
            var commands = new MovementCommandProcessor(world, new CataclysmMovementCostPolicy(), scheduler);

            var result = commands.Submit(Command("move-success", Player, MoveDirection.East));

            Assert.That(result.Outcome, Is.EqualTo(MovementOutcomeKind.Moved));
            Assert.That(world.PositionOf(Player), Is.EqualTo(new WorldPosition(1, 0, 0)));
            Assert.That(world.EntitiesAt(new WorldPosition(0, 0, 0)), Is.Empty);
            Assert.That(world.EntitiesAt(new WorldPosition(1, 0, 0)), Is.EqualTo(new[] { Player }));
            Assert.That(result.ActionCost, Is.EqualTo(125));
            Assert.That(scheduler.Costs, Is.EqualTo(new[] { Tuple.Create(Player, 125) }));
        }

        [Test]
        public void ImpassableTerrainDoesNotMutatePositionOrSpatialIndex() {
            var world = new TestWorld();
            world.AddActor(Player, new WorldPosition(0, 0, 0));
            world.SetTerrain(new WorldPosition(1, 0, 0), false, 100);
            var scheduler = new TestScheduler();
            var commands = new MovementCommandProcessor(world, new CataclysmMovementCostPolicy(), scheduler);

            var result = commands.Submit(Command("move-wall", Player, MoveDirection.East));

            Assert.That(result.Outcome, Is.EqualTo(MovementOutcomeKind.BlockedByTerrain));
            Assert.That(world.PositionOf(Player), Is.EqualTo(new WorldPosition(0, 0, 0)));
            Assert.That(world.EntitiesAt(new WorldPosition(0, 0, 0)), Is.EqualTo(new[] { Player }));
            Assert.That(world.EntitiesAt(new WorldPosition(1, 0, 0)), Is.Empty);
            Assert.That(scheduler.Costs, Is.Empty);
        }

        [Test]
        public void HostileDestinationProducesInteractionRequestWithoutMovementOrMovementCharge() {
            var world = new TestWorld();
            world.AddActor(Player, new WorldPosition(0, 0, 0));
            world.AddActor(Hostile, new WorldPosition(1, 0, 0));
            world.SetHostile(Player, Hostile);
            var scheduler = new TestScheduler();
            var commands = new MovementCommandProcessor(world, new CataclysmMovementCostPolicy(), scheduler);

            var result = commands.Submit(Command("move-bump", Player, MoveDirection.East));

            Assert.That(result.Outcome, Is.EqualTo(MovementOutcomeKind.BumpInteraction));
            Assert.That(result.Interaction.InteractionKind, Is.EqualTo("melee"));
            Assert.That(result.Interaction.Target, Is.EqualTo(Hostile));
            Assert.That(world.PositionOf(Player), Is.EqualTo(new WorldPosition(0, 0, 0)));
            Assert.That(scheduler.Costs, Is.Empty, "The resolved interaction, not movement, owns its action cost.");
        }

        [Test]
        public void InProcessClientUsesBoundaryAdmissionAndReceivesPlayerScopedProjection() {
            var world = new TestWorld();
            world.AddActor(Player, new WorldPosition(0, 0, 0));
            world.SetTerrain(new WorldPosition(1, 0, 0), true, 100);
            var commands = new MovementCommandProcessor(world, new CataclysmMovementCostPolicy(), new TestScheduler());
            var playerId = new StableSimulationId("player-a");
            var server = new InProcessMovementServer(commands, new TestVisibility(), 8, 4, 4);
            server.Bind(playerId, Player);
            IClientTransport client = new InProcessClientTransport(server, playerId);

            Assert.That(client.Send(new MoveIntentDto(playerId, 1, MoveDirection.East)), Is.True);
            Assert.That(world.PositionOf(Player), Is.EqualTo(new WorldPosition(0, 0, 0)), "Transport intake must not mutate ECS/world state.");
            Assert.That(client.TryReceive(playerId, out _), Is.False);

            Assert.That(server.PumpAtSimulationBoundary(), Is.EqualTo(1));

            MovementProjection projection;
            Assert.That(client.TryReceive(playerId, out projection), Is.True);
            Assert.That(projection.Audience, Is.EqualTo(playerId));
            Assert.That(projection.RequestSequence, Is.EqualTo(1));
            Assert.That(projection.Outcome, Is.EqualTo(MovementOutcomeKind.Moved));
            Assert.That(projection.From, Is.EqualTo(new WorldPosition(0, 0, 0)));
            Assert.That(projection.To, Is.EqualTo(new WorldPosition(1, 0, 0)));
        }

        [Test]
        public void HiddenBumpTargetIsNotIncludedInClientProjection() {
            var world = new TestWorld();
            world.AddActor(Player, new WorldPosition(0, 0, 0));
            world.AddActor(Hostile, new WorldPosition(1, 0, 0));
            world.SetHostile(Player, Hostile);
            var commands = new MovementCommandProcessor(world, new CataclysmMovementCostPolicy(), new TestScheduler());
            var playerId = new StableSimulationId("player-a");
            var server = new InProcessMovementServer(commands, new TestVisibility(), 8, 4, 4);
            server.Bind(playerId, Player);
            var client = new InProcessClientTransport(server, playerId);

            Assert.That(client.Send(new MoveIntentDto(playerId, 1, MoveDirection.East)), Is.True);
            server.PumpAtSimulationBoundary();

            MovementProjection projection;
            Assert.That(client.TryReceive(playerId, out projection), Is.True);
            Assert.That(projection.Outcome, Is.EqualTo(MovementOutcomeKind.BlockedByEntity));
            Assert.That(projection.VisibleInteractionTarget, Is.Null);
        }

        [Test]
        public void AiUsesTheSameCommandSinkAsPlayerMovement() {
            var world = new TestWorld();
            var ai = Entity(3);
            world.AddActor(ai, new WorldPosition(3, 0, 0));
            world.SetTerrain(new WorldPosition(4, 0, 0), true, 80);
            var scheduler = new TestScheduler();
            var commands = new MovementCommandProcessor(world, new CataclysmMovementCostPolicy(), scheduler);
            var controller = new AiMovementController(commands);

            var result = controller.Act(ai, MoveDirection.East);

            Assert.That(result.Outcome, Is.EqualTo(MovementOutcomeKind.Moved));
            Assert.That(world.PositionOf(ai), Is.EqualTo(new WorldPosition(4, 0, 0)));
            Assert.That(scheduler.Costs, Is.EqualTo(new[] { Tuple.Create(ai, 80) }));
        }

        [Test]
        public void TwoPlayersAreAdmittedFairlyUnderOneServerStepBoundary() {
            var world = new TestWorld();
            var second = Entity(4);
            var firstPlayer = new StableSimulationId("player-a");
            var secondPlayer = new StableSimulationId("player-b");
            world.AddActor(Player, new WorldPosition(0, 0, 0));
            world.AddActor(second, new WorldPosition(10, 0, 0));
            world.SetTerrain(new WorldPosition(1, 0, 0), true, 100);
            world.SetTerrain(new WorldPosition(11, 0, 0), true, 100);
            var server = new InProcessMovementServer(
                new MovementCommandProcessor(world, new CataclysmMovementCostPolicy(), new TestScheduler()),
                new TestVisibility(), 4, 2, 1);
            server.Bind(firstPlayer, Player);
            server.Bind(secondPlayer, second);
            var firstClient = new InProcessClientTransport(server, firstPlayer);
            var secondClient = new InProcessClientTransport(server, secondPlayer);
            Assert.That(firstClient.Send(new MoveIntentDto(firstPlayer, 1, MoveDirection.East)), Is.True);
            Assert.That(secondClient.Send(new MoveIntentDto(secondPlayer, 1, MoveDirection.East)), Is.True);

            Assert.That(server.PumpAtSimulationBoundary(), Is.EqualTo(1));
            Assert.That(server.PumpAtSimulationBoundary(), Is.EqualTo(1));

            MovementProjection firstProjection;
            MovementProjection secondProjection;
            Assert.That(firstClient.TryReceive(firstPlayer, out firstProjection), Is.True);
            Assert.That(secondClient.TryReceive(secondPlayer, out secondProjection), Is.True);
            Assert.That(world.PositionOf(Player), Is.EqualTo(new WorldPosition(1, 0, 0)));
            Assert.That(world.PositionOf(second), Is.EqualTo(new WorldPosition(11, 0, 0)));
            Assert.That(firstProjection.Audience, Is.EqualTo(firstPlayer));
            Assert.That(secondProjection.Audience, Is.EqualTo(secondPlayer));
        }

        [Test]
        public void InProcessTransportRejectsWhenAResponseSlotIsAlreadyReserved() {
            var world = new TestWorld();
            world.AddActor(Player, new WorldPosition(0, 0, 0));
            var playerId = new StableSimulationId("player-a");
            var server = new InProcessMovementServer(
                new MovementCommandProcessor(world, new CataclysmMovementCostPolicy(), new TestScheduler()),
                new TestVisibility(), 4, 1, 4);
            server.Bind(playerId, Player);
            var client = new InProcessClientTransport(server, playerId);

            Assert.That(client.Send(new MoveIntentDto(playerId, 1, MoveDirection.East)), Is.True);
            Assert.That(client.Send(new MoveIntentDto(playerId, 2, MoveDirection.East)), Is.False);
        }

        [TestCase(100, 100)]
        [TestCase(200, 200)]
        [TestCase(600, 600)]
        public void CataclysmMovementCostPreservesPinnedModeCostsOnNormalizedTerrain(int movementCostPercent, int expectedCost) {
            var actor = new MovementActorState(Player, new WorldPosition(0, 0, 0), true, movementCostPercent);
            var policy = new CataclysmMovementCostPolicy();
            int cost = policy.CostFor(actor, MovementOutcomeKind.Moved, 100, MoveDirection.East);
            Assert.That(cost, Is.EqualTo(expectedCost));
        }

        [Test]
        public void TransportBoundaryRejectsDefaultDomainIdentities() {
            Assert.Throws<ArgumentException>(() => new MoveIntentDto(default(StableSimulationId), 1, MoveDirection.East));
            Assert.Throws<ArgumentException>(() => Command("invalid-actor", default(EntityId), MoveDirection.East));

            var world = new TestWorld();
            var server = new InProcessMovementServer(
                new MovementCommandProcessor(world, new CataclysmMovementCostPolicy(), new TestScheduler()),
                new TestVisibility(), 4, 1, 1);
            Assert.Throws<ArgumentException>(() => server.Bind(default(StableSimulationId), Player));
            Assert.Throws<ArgumentException>(() => server.Bind(new StableSimulationId("player-a"), default(EntityId)));
        }

        private sealed class TestWorld : IMovementWorld {
            private readonly Dictionary<EntityId, WorldPosition> _positions = new Dictionary<EntityId, WorldPosition>();
            private readonly Dictionary<WorldPosition, SortedSet<EntityId>> _index = new Dictionary<WorldPosition, SortedSet<EntityId>>();
            private readonly Dictionary<WorldPosition, TerrainMovementState> _terrain = new Dictionary<WorldPosition, TerrainMovementState>();
            private readonly HashSet<Tuple<EntityId, EntityId>> _hostiles = new HashSet<Tuple<EntityId, EntityId>>();

            public void AddActor(EntityId actor, WorldPosition position) {
                _positions.Add(actor, position);
                Bucket(position).Add(actor);
            }
            public void SetTerrain(WorldPosition cell, bool traversable, int cost) { _terrain[cell] = new TerrainMovementState(traversable, cost); }
            public void SetHostile(EntityId left, EntityId right) {
                _hostiles.Add(Tuple.Create(left, right));
                _hostiles.Add(Tuple.Create(right, left));
            }
            public WorldPosition PositionOf(EntityId actor) => _positions[actor];
            public IEnumerable<EntityId> EntitiesAt(WorldPosition cell) {
                SortedSet<EntityId> entities;
                return _index.TryGetValue(cell, out entities) ? entities.ToArray() : Enumerable.Empty<EntityId>();
            }
            public bool TryGetActor(EntityId actor, out MovementActorState state) {
                WorldPosition position;
                if (_positions.TryGetValue(actor, out position)) {
                    state = new MovementActorState(actor, position, true);
                    return true;
                }
                state = null;
                return false;
            }
            public TerrainMovementState GetTerrain(WorldPosition cell) {
                TerrainMovementState terrain;
                return _terrain.TryGetValue(cell, out terrain) ? terrain : new TerrainMovementState(false, 0);
            }
            public IEnumerable<EntityId> GetBlockingEntities(WorldPosition cell) => EntitiesAt(cell);
            public bool IsHostile(EntityId actor, EntityId other) => _hostiles.Contains(Tuple.Create(actor, other));
            public bool TryMove(EntityId actor, WorldPosition expectedFrom, WorldPosition to) {
                if (!_positions.ContainsKey(actor) || _positions[actor] != expectedFrom || EntitiesAt(to).Any()) return false;
                Bucket(expectedFrom).Remove(actor);
                if (_index[expectedFrom].Count == 0) _index.Remove(expectedFrom);
                _positions[actor] = to;
                Bucket(to).Add(actor);
                return true;
            }
            private SortedSet<EntityId> Bucket(WorldPosition cell) {
                SortedSet<EntityId> entities;
                if (!_index.TryGetValue(cell, out entities)) _index.Add(cell, entities = new SortedSet<EntityId>());
                return entities;
            }
        }

        private sealed class TestScheduler : IActionScheduler {
            public List<Tuple<EntityId, int>> Costs { get; } = new List<Tuple<EntityId, int>>();
            public void Reschedule(EntityId actor, int actionCost) { Costs.Add(Tuple.Create(actor, actionCost)); }
        }

        private sealed class TestVisibility : IMovementVisibility {
            public bool CanSeeEntity(StableSimulationId player, EntityId entity) => false;
        }
    }
}
