using System;
using System.Collections.Generic;
using System.Linq;
using OctoGhast.Framework.Ecs;
using OctoGhast.Spatial;
using OctoGhast.Simulation;

namespace OctoGhast.Simulation.Movement {
    /// <summary>Fixed-size logical request DTO. It contains no ECS or renderer object reference.</summary>
    public sealed class MoveIntentDto {
        public StableSimulationId Player { get; }
        public long Sequence { get; }
        public MoveDirection Direction { get; }
        public MoveIntentDto(StableSimulationId player, long sequence, MoveDirection direction) {
            if (String.IsNullOrWhiteSpace(player.Value)) throw new ArgumentException("A stable player identity is required.", nameof(player));
            if (sequence <= 0) throw new ArgumentOutOfRangeException(nameof(sequence));
            Player = player;
            Sequence = sequence;
            Direction = direction;
        }
    }

    /// <summary>Only client-appropriate movement facts; never serializes components or world objects.</summary>
    public sealed class MovementProjection {
        public StableSimulationId Audience { get; }
        public long RequestSequence { get; }
        public MovementOutcomeKind Outcome { get; }
        public WorldPosition? From { get; }
        public WorldPosition? To { get; }
        public int ActionCost { get; }
        public EntityId? VisibleInteractionTarget { get; }

        public MovementProjection(StableSimulationId audience, long requestSequence, MovementOutcomeKind outcome,
            WorldPosition? from, WorldPosition? to, int actionCost, EntityId? visibleInteractionTarget) {
            Audience = audience;
            RequestSequence = requestSequence;
            Outcome = outcome;
            From = from;
            To = to;
            ActionCost = actionCost;
            VisibleInteractionTarget = visibleInteractionTarget;
        }
    }

    public interface IMovementVisibility {
        bool CanSeeEntity(StableSimulationId player, EntityId entity);
    }

    /// <summary>
    /// AI chooses a direction but cannot resolve or mutate movement itself; it submits the same
    /// command handled for a player after authoritative request admission.
    /// </summary>
    public sealed class AiMovementController {
        private readonly IMoveCommandSink _commands;
        private long _nextSequence;
        public AiMovementController(IMoveCommandSink commands) { _commands = commands ?? throw new ArgumentNullException(nameof(commands)); }
        public MovementResult Act(EntityId actor, MoveDirection direction) {
            if (_nextSequence == long.MaxValue) throw new OverflowException("AI movement sequence exhausted.");
            long sequence = ++_nextSequence;
            var commandId = new StableSimulationId("ai-move:" + actor + ":" + sequence);
            return _commands.Submit(new MoveCommand(commandId, actor, direction, null, sequence));
        }
    }

    /// <summary>
    /// In-process transport proof for single player and tests. Requests enter a bounded queue and
    /// only mutate the world when PumpAtSimulationBoundary is called. No callback runs gameplay.
    /// </summary>
    public sealed class InProcessMovementServer {
        private readonly object _gate = new object();
        private readonly List<PendingMoveIntent> _pending = new List<PendingMoveIntent>();
        private readonly Dictionary<StableSimulationId, EntityId> _controlledActors = new Dictionary<StableSimulationId, EntityId>();
        private readonly Dictionary<StableSimulationId, Queue<MovementProjection>> _outbound = new Dictionary<StableSimulationId, Queue<MovementProjection>>();
        private readonly Dictionary<StableSimulationId, int> _inFlightByPlayer = new Dictionary<StableSimulationId, int>();
        private readonly Dictionary<StableSimulationId, long> _lastAcceptedSequence = new Dictionary<StableSimulationId, long>();
        private readonly MovementCommandProcessor _commands;
        private readonly BoundedCommandAdmission _admission = new BoundedCommandAdmission();
        private readonly IMovementVisibility _visibility;
        private readonly int _inboundCapacity;
        private readonly int _outboundCapacityPerPlayer;
        private readonly int _maxAdmissionsPerStep;
        private long _nextAdmissionSequence;
        private StableSimulationId? _playerAdmissionCursor;

        private sealed class PendingMoveIntent {
            public MoveIntentDto Intent { get; }
            public CommandLifecycle Lifecycle { get; }
            public PendingMoveIntent(MoveIntentDto intent, CommandLifecycle lifecycle) {
                Intent = intent;
                Lifecycle = lifecycle;
            }
        }

        public InProcessMovementServer(MovementCommandProcessor commands, IMovementVisibility visibility,
            int inboundCapacity, int outboundCapacityPerPlayer, int maxAdmissionsPerStep) {
            if (commands == null) throw new ArgumentNullException(nameof(commands));
            if (visibility == null) throw new ArgumentNullException(nameof(visibility));
            if (inboundCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(inboundCapacity));
            if (outboundCapacityPerPlayer <= 0) throw new ArgumentOutOfRangeException(nameof(outboundCapacityPerPlayer));
            if (maxAdmissionsPerStep <= 0) throw new ArgumentOutOfRangeException(nameof(maxAdmissionsPerStep));
            _commands = commands;
            _visibility = visibility;
            _inboundCapacity = inboundCapacity;
            _outboundCapacityPerPlayer = outboundCapacityPerPlayer;
            _maxAdmissionsPerStep = maxAdmissionsPerStep;
        }

        public void Bind(StableSimulationId player, EntityId actor) {
            if (String.IsNullOrWhiteSpace(player.Value)) throw new ArgumentException("A stable player identity is required.", nameof(player));
            if (!actor.IsValid) throw new ArgumentException("A durable actor identity is required.", nameof(actor));
            lock (_gate) {
                _controlledActors[player] = actor;
                if (!_outbound.ContainsKey(player)) _outbound.Add(player, new Queue<MovementProjection>());
            }
        }

        /// <summary>Rebinds stable player identity; it does not create or destroy the actor.</summary>
        public void Disconnect(StableSimulationId player) {
            if (String.IsNullOrWhiteSpace(player.Value)) throw new ArgumentException("A stable player identity is required.", nameof(player));
            lock (_gate) _controlledActors.Remove(player);
        }

        public bool TrySubmit(MoveIntentDto intent) {
            if (intent == null) throw new ArgumentNullException(nameof(intent));
            lock (_gate) {
                if (!_controlledActors.ContainsKey(intent.Player) || _pending.Count >= _inboundCapacity) return false;
                long lastSequence;
                if (_lastAcceptedSequence.TryGetValue(intent.Player, out lastSequence) && intent.Sequence <= lastSequence) return false;
                var queue = _outbound[intent.Player];
                int inFlight;
                _inFlightByPlayer.TryGetValue(intent.Player, out inFlight);
                // Reserve exactly one bounded response slot when admitting each request.
                if (queue.Count + inFlight >= _outboundCapacityPerPlayer) return false;
                EntityId actor;
                if (!_controlledActors.TryGetValue(intent.Player, out actor)) return false;
                var commandId = new StableSimulationId(intent.Player.Value + ":move:" + intent.Sequence);
                var command = new MoveCommand(commandId, actor, intent.Direction, intent.Player, intent.Sequence);
                _pending.Add(new PendingMoveIntent(intent, new CommandLifecycle(command)));
                _lastAcceptedSequence[intent.Player] = intent.Sequence;
                _inFlightByPlayer[intent.Player] = inFlight + 1;
                return true;
            }
        }

        public bool TryReceive(StableSimulationId player, out MovementProjection projection) {
            lock (_gate) {
                Queue<MovementProjection> queue;
                if (_outbound.TryGetValue(player, out queue) && queue.Count > 0) {
                    projection = queue.Dequeue();
                    return true;
                }
                projection = null;
                return false;
            }
        }

        /// <summary>Deterministically admits a bounded, rotating set of players at one sim boundary.</summary>
        public int PumpAtSimulationBoundary() {
            List<PendingMoveIntent> batch;
            lock (_gate) batch = SelectBatch();
            foreach (var pending in batch) {
                var intent = pending.Intent;
                var result = _commands.ResolveAdmitted(pending.Lifecycle);
                bool discloseTarget = result.Interaction != null && _visibility.CanSeeEntity(intent.Player, result.Interaction.Target);
                Publish(intent, result, discloseTarget);
            }
            return batch.Count;
        }

        private List<PendingMoveIntent> SelectBatch() {
            if (_pending.Count == 0) return new List<PendingMoveIntent>();
            var candidates = _pending.Select(pending => pending.Lifecycle).ToArray();
            var limits = new CommandAdmissionLimits(_maxAdmissionsPerStep, _maxAdmissionsPerStep);
            var admittedBatch = _admission.Admit(candidates, limits, _nextAdmissionSequence, _playerAdmissionCursor);
            _nextAdmissionSequence = admittedBatch.NextAdmissionSequence;
            _playerAdmissionCursor = admittedBatch.NextPlayerCursor;

            var admitted = new HashSet<CommandLifecycle>(admittedBatch.Admitted);
            var selected = _pending.Where(pending => admitted.Contains(pending.Lifecycle))
                // PlayerId order bounds intake only; execution uses stable actor/work identity.
                .OrderBy(pending => pending.Lifecycle.Command.ActorId)
                .ThenBy(pending => pending.Lifecycle.AdmissionSequence)
                .ToList();
            _pending.RemoveAll(pending => admitted.Contains(pending.Lifecycle));
            return selected;
        }

        private void Publish(MoveIntentDto intent, MovementResult result, bool discloseTarget) {
            WorldPosition? from = result.Outcome == MovementOutcomeKind.Rejected ? (WorldPosition?)null : result.From;
            WorldPosition? to = result.Outcome == MovementOutcomeKind.Rejected ? (WorldPosition?)null : result.To;
            var outcome = result.Outcome;
            EntityId? target = null;
            if (result.Interaction != null) {
                if (discloseTarget) target = result.Interaction.Target;
                else outcome = MovementOutcomeKind.BlockedByEntity;
            }
            var projection = new MovementProjection(intent.Player, intent.Sequence, outcome, from, to,
                result.ActionCost, target);
            lock (_gate) {
                int inFlight;
                _inFlightByPlayer.TryGetValue(intent.Player, out inFlight);
                if (inFlight > 1) _inFlightByPlayer[intent.Player] = inFlight - 1;
                else _inFlightByPlayer.Remove(intent.Player);
                _outbound[intent.Player].Enqueue(projection);
            }
        }
    }

    /// <summary>Small client-facing transport adapter, structurally the same API a TCP adapter uses.</summary>
    public interface IClientTransport {
        bool Send(MoveIntentDto request);
        bool TryReceive(StableSimulationId player, out MovementProjection projection);
    }

    public sealed class InProcessClientTransport : IClientTransport {
        private readonly InProcessMovementServer _server;
        private readonly StableSimulationId _player;
        public InProcessClientTransport(InProcessMovementServer server, StableSimulationId player) {
            _server = server ?? throw new ArgumentNullException(nameof(server));
            _player = player;
        }
        public bool Send(MoveIntentDto request) {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.Player != _player) return false;
            return _server.TrySubmit(request);
        }
        public bool TryReceive(StableSimulationId player, out MovementProjection projection) {
            if (player != _player) { projection = null; return false; }
            return _server.TryReceive(_player, out projection);
        }
    }
}
