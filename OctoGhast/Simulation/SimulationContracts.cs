using System;
using System.Collections.Generic;
using System.Linq;

namespace OctoGhast.Simulation {
    /// <summary>Stable, ordinally ordered identity used at deterministic simulation boundaries.</summary>
    public struct StableSimulationId : IEquatable<StableSimulationId>, IComparable<StableSimulationId> {
        public string Value { get; }

        public StableSimulationId(string value) {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("An id is required.", nameof(value));
            Value = value;
        }

        public int CompareTo(StableSimulationId other) => StringComparer.Ordinal.Compare(Value, other.Value);
        public bool Equals(StableSimulationId other) => StringComparer.Ordinal.Equals(Value, other.Value);
        public override bool Equals(object obj) => obj is StableSimulationId && Equals((StableSimulationId)obj);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        public override string ToString() => Value;
        public static bool operator ==(StableSimulationId left, StableSimulationId right) => left.Equals(right);
        public static bool operator !=(StableSimulationId left, StableSimulationId right) => !left.Equals(right);
    }

    public enum CommandState { Submitted, Admitted, Deferred, Resolved, Rejected }
    public enum CommandRetryPolicy { Unspecified, StateReconciled, IntrinsicIdempotent, DurableOutcome }

    public sealed class CommandOutcome {
        public bool Accepted { get; }
        public string Code { get; }
        public string Detail { get; }

        private CommandOutcome(bool accepted, string code, string detail) {
            Accepted = accepted;
            Code = code ?? throw new ArgumentNullException(nameof(code));
            Detail = detail ?? string.Empty;
        }

        public static CommandOutcome Success(string code = "resolved", string detail = "") => new CommandOutcome(true, code, detail);
        public static CommandOutcome Rejected(string code, string detail = "") => new CommandOutcome(false, code, detail);
    }

    /// <summary>
    /// A player/system intent. It is data until admitted and resolved by authoritative simulation code;
    /// it must not be mutated by a transport or presentation callback.
    /// </summary>
    public interface ISimulationCommand {
        /// <summary>Stable request/operation identity, distinct from the command's semantic type and actor identity.</summary>
        StableSimulationId CommandId { get; }
        string CommandTypeId { get; }
        CommandRetryPolicy RetryPolicy { get; }
        StableSimulationId? PlayerId { get; }
        StableSimulationId ActorId { get; }
        long RequestSequence { get; }
        CommandOutcome Resolve(ISimulationCommandContext context);
    }

    public interface ISimulationCommandContext { }

    public sealed class CommandLifecycle {
        public ISimulationCommand Command { get; }
        public CommandState State { get; private set; }
        public long? AdmissionSequence { get; private set; }
        public CommandOutcome Outcome { get; private set; }

        public CommandLifecycle(ISimulationCommand command) {
            Command = command ?? throw new ArgumentNullException(nameof(command));
            if (string.IsNullOrWhiteSpace(command.CommandId.Value)) throw new ArgumentException("A stable command request id is required.", nameof(command));
            if (string.IsNullOrWhiteSpace(command.CommandTypeId)) throw new ArgumentException("A stable command type id is required.", nameof(command));
            if (string.IsNullOrWhiteSpace(command.ActorId.Value)) throw new ArgumentException("A stable actor id is required.", nameof(command));
            if (command.PlayerId.HasValue && string.IsNullOrWhiteSpace(command.PlayerId.Value.Value)) throw new ArgumentException("A player id cannot be empty.", nameof(command));
            if (command.RequestSequence < 0) throw new ArgumentOutOfRangeException(nameof(command), "Request sequence must be non-negative.");
            if (command.RetryPolicy == CommandRetryPolicy.Unspecified || !Enum.IsDefined(typeof(CommandRetryPolicy), command.RetryPolicy))
                throw new ArgumentException("Every gameplay command must declare its retry/outcome policy.", nameof(command));
            State = CommandState.Submitted;
        }

        public void Admit(long sequence) {
            if (State != CommandState.Submitted && State != CommandState.Deferred)
                throw new InvalidOperationException("Only submitted or deferred commands can be admitted.");
            if (sequence < 0) throw new ArgumentOutOfRangeException(nameof(sequence));
            AdmissionSequence = sequence;
            State = CommandState.Admitted;
        }

        public void Defer() {
            if (State != CommandState.Submitted && State != CommandState.Deferred)
                throw new InvalidOperationException("Only submitted or already-deferred commands can be deferred.");
            State = CommandState.Deferred;
        }

        public void Resolve(ISimulationCommandContext context) {
            Require(CommandState.Admitted);
            Outcome = Command.Resolve(context) ?? throw new InvalidOperationException("A command must return an outcome.");
            State = Outcome.Accepted ? CommandState.Resolved : CommandState.Rejected;
        }

        public SimulationWorkOrderKey ExecutionKey(SimulationExecutionPlan plan, string laneId) {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (State != CommandState.Admitted || !AdmissionSequence.HasValue)
                throw new InvalidOperationException("Only admitted commands have an authoritative execution key.");
            return new SimulationWorkOrderKey(plan.LaneOrder(laneId), Command.ActorId, Command.CommandId, AdmissionSequence.Value);
        }

        public void Reject(string code, string detail = "") {
            if (State != CommandState.Submitted && State != CommandState.Admitted)
                throw new InvalidOperationException("Only pending commands can be rejected.");
            Outcome = CommandOutcome.Rejected(code, detail);
            State = CommandState.Rejected;
        }

        private void Require(CommandState required) {
            if (State != required) throw new InvalidOperationException("Invalid command lifecycle transition: " + State + " -> " + required + ".");
        }
    }

    public sealed class CommandAdmissionLimits {
        public int GlobalLimit { get; }
        public int PerPlayerLimit { get; }

        public CommandAdmissionLimits(int globalLimit, int perPlayerLimit) {
            if (globalLimit <= 0) throw new ArgumentOutOfRangeException(nameof(globalLimit));
            if (perPlayerLimit <= 0) throw new ArgumentOutOfRangeException(nameof(perPlayerLimit));
            GlobalLimit = globalLimit;
            PerPlayerLimit = perPlayerLimit;
        }
    }

    public sealed class CommandAdmissionBatch {
        public IReadOnlyList<CommandLifecycle> Admitted { get; }
        public IReadOnlyList<CommandLifecycle> Deferred { get; }
        public long NextAdmissionSequence { get; }
        public StableSimulationId? NextPlayerCursor { get; }

        internal CommandAdmissionBatch(IList<CommandLifecycle> admitted, IList<CommandLifecycle> deferred,
            long nextSequence, StableSimulationId? nextPlayerCursor) {
            Admitted = Array.AsReadOnly(admitted.ToArray());
            Deferred = Array.AsReadOnly(deferred.ToArray());
            NextAdmissionSequence = nextSequence;
            NextPlayerCursor = nextPlayerCursor;
        }
    }

    /// <summary>
    /// Deterministically admits a frozen candidate set. Player order controls bounded admission only;
    /// it is deliberately not an authoritative execution/contention order.
    /// </summary>
    public sealed class BoundedCommandAdmission {
        public CommandAdmissionBatch Admit(IEnumerable<CommandLifecycle> candidateSource, CommandAdmissionLimits limits,
            long firstSequence, StableSimulationId? playerCursor) {
            if (candidateSource == null) throw new ArgumentNullException(nameof(candidateSource));
            if (limits == null) throw new ArgumentNullException(nameof(limits));
            if (firstSequence < 0) throw new ArgumentOutOfRangeException(nameof(firstSequence));
            if (playerCursor.HasValue && string.IsNullOrWhiteSpace(playerCursor.Value.Value)) throw new ArgumentException("Player cursor cannot be empty.", nameof(playerCursor));

            var candidates = candidateSource.ToArray(); // freeze before applying limits
            var maximumAdmissions = Math.Min(candidates.Length, limits.GlobalLimit);
            if (firstSequence > long.MaxValue - maximumAdmissions)
                throw new ArgumentOutOfRangeException(nameof(firstSequence), "The admission sequence range would overflow.");
            if (candidates.Any(c => c == null || (c.State != CommandState.Submitted && c.State != CommandState.Deferred)))
                throw new ArgumentException("Candidates must be non-null submitted or deferred external commands.", nameof(candidateSource));
            if (candidates.Any(c => !c.Command.PlayerId.HasValue))
                throw new ArgumentException("Bounded external admission requires stable player identity; profile/system work uses execution lanes.", nameof(candidateSource));

            var duplicateIds = candidates.GroupBy(c => Tuple.Create(c.Command.PlayerId.Value, c.Command.CommandId))
                .Any(group => group.Count() > 1);
            if (duplicateIds) throw new ArgumentException("Command ids must be unique within a batch.", nameof(candidateSource));

            var ordered = candidates.OrderBy(c => c.Command.PlayerId.HasValue ? 0 : 1)
                .ThenBy(c => c.Command.PlayerId.HasValue ? c.Command.PlayerId.Value.Value : string.Empty, StringComparer.Ordinal)
                .ThenBy(c => c.Command.RequestSequence)
                .ThenBy(c => c.Command.CommandId);
            var players = ordered.Where(c => c.Command.PlayerId.HasValue).ToArray();
            if (players.Length > 0 && playerCursor.HasValue) {
                var pivot = playerCursor.Value.Value;
                ordered = ordered.OrderBy(c => c.Command.PlayerId.HasValue ? 0 : 1)
                    .ThenBy(c => c.Command.PlayerId.HasValue && StringComparer.Ordinal.Compare(c.Command.PlayerId.Value.Value, pivot) < 0 ? 1 : 0)
                    .ThenBy(c => c.Command.PlayerId.HasValue ? c.Command.PlayerId.Value.Value : string.Empty, StringComparer.Ordinal)
                    .ThenBy(c => c.Command.RequestSequence).ThenBy(c => c.Command.CommandId);
            }

            var selected = new List<CommandLifecycle>();
            var deferred = new List<CommandLifecycle>();
            var playerCounts = new Dictionary<StableSimulationId, int>();
            foreach (var item in ordered) {
                var player = item.Command.PlayerId;
                var count = player.HasValue && playerCounts.ContainsKey(player.Value) ? playerCounts[player.Value] : 0;
                if (selected.Count < limits.GlobalLimit && (!player.HasValue || count < limits.PerPlayerLimit)) {
                    item.Admit(firstSequence + selected.Count);
                    selected.Add(item);
                    if (player.HasValue) playerCounts[player.Value] = count + 1;
                } else {
                    item.Defer();
                    deferred.Add(item);
                }
            }

            StableSimulationId? nextCursor = null;
            if (players.Length > 0) {
                var lastAdmittedPlayer = selected.LastOrDefault(c => c.Command.PlayerId.HasValue);
                var last = lastAdmittedPlayer == null ? (playerCursor ?? players[0].Command.PlayerId) : lastAdmittedPlayer.Command.PlayerId;
                var allPlayers = players.Select(c => c.Command.PlayerId.Value).Distinct().OrderBy(id => id).ToArray();
                var index = Array.FindIndex(allPlayers, id => id == last.Value);
                nextCursor = allPlayers[(index + 1) % allPlayers.Length];
            }
            return new CommandAdmissionBatch(selected, deferred, firstSequence + selected.Count, nextCursor);
        }
    }

    public interface ISimulationQuery<out TResult> { }

    /// <summary>Synchronous read path for facts required to resolve a command or decision.</summary>
    public sealed class SynchronousQueryBus {
        private readonly Dictionary<Type, Delegate> _handlers = new Dictionary<Type, Delegate>();

        public void Register<TQuery, TResult>(Func<TQuery, TResult> handler) where TQuery : ISimulationQuery<TResult> {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var type = typeof(TQuery);
            if (_handlers.ContainsKey(type)) throw new InvalidOperationException("A query handler is already registered for " + type.FullName + ".");
            _handlers.Add(type, handler);
        }

        public TResult Ask<TQuery, TResult>(TQuery query) where TQuery : ISimulationQuery<TResult> {
            Delegate handler;
            if (!_handlers.TryGetValue(typeof(TQuery), out handler))
                throw new InvalidOperationException("No synchronous handler is registered for " + typeof(TQuery).FullName + ".");
            return ((Func<TQuery, TResult>)handler)(query);
        }
    }

    public sealed class SimulationEventEnvelope {
        public long Tick { get; }
        public long Sequence { get; }
        public string TypeId { get; }
        public string Payload { get; }

        internal SimulationEventEnvelope(long tick, long sequence, string typeId, string payload) {
            Tick = tick;
            Sequence = sequence;
            TypeId = typeId;
            Payload = payload ?? string.Empty;
        }
    }

    /// <summary>Append-only deterministic fact stream. Consumers read by sequence and maintain their own cursors.</summary>
    public sealed class OrderedEventJournal {
        private readonly List<SimulationEventEnvelope> _events = new List<SimulationEventEnvelope>();
        private readonly int _retentionCapacity;
        private long _nextSequence;
        public long NextSequence => _nextSequence;
        public long OldestRetainedSequence => _events.Count == 0 ? _nextSequence : _events[0].Sequence;

        public OrderedEventJournal(int retentionCapacity = 4096, long nextSequence = 0) {
            if (retentionCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(retentionCapacity));
            if (nextSequence < 0) throw new ArgumentOutOfRangeException(nameof(nextSequence));
            _retentionCapacity = retentionCapacity;
            _nextSequence = nextSequence;
        }

        public SimulationEventEnvelope Publish(long tick, string stableTypeId, string payload) {
            if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick));
            if (string.IsNullOrWhiteSpace(stableTypeId)) throw new ArgumentException("A stable event type id is required.", nameof(stableTypeId));
            if (_nextSequence == long.MaxValue) throw new OverflowException("Event sequence exhausted.");
            var entry = new SimulationEventEnvelope(tick, _nextSequence++, stableTypeId, payload);
            _events.Add(entry);
            if (_events.Count > _retentionCapacity) _events.RemoveAt(0);
            return entry;
        }

        public IReadOnlyList<SimulationEventEnvelope> ReadAfter(long sequenceExclusive) {
            if (sequenceExclusive < -1 || sequenceExclusive >= _nextSequence && sequenceExclusive != -1)
                throw new ArgumentOutOfRangeException(nameof(sequenceExclusive));
            if (sequenceExclusive < OldestRetainedSequence - 1)
                throw new InvalidOperationException("The requested event cursor is older than retained history.");
            return Array.AsReadOnly(_events.Where(entry => entry.Sequence > sequenceExclusive).ToArray());
        }
    }

    public enum ActivityLifecycle { Queued, Active, Suspended, Completed, Cancelled, Failed }
    public enum ActivityProgressionKind { ProfileScheduledWork, ActionBudget, CanonicalElapsedTime }

    /// <summary>Mutable runtime record; definitions/policy stay in the owning profile and are referenced by stable id.</summary>
    public sealed class ActivityRecord {
        public StableSimulationId ActivityId { get; }
        public StableSimulationId OwnerActorId { get; }
        public StableSimulationId DefinitionId { get; }
        public StableSimulationId? TargetId { get; }
        public string WorkUnitId { get; }
        public ActivityProgressionKind ProgressionKind { get; }
        public ActivityLifecycle Lifecycle { get; private set; }
        public long TotalWork { get; }
        public long RemainingWork { get; private set; }
        public string StateData { get; private set; }

        public ActivityRecord(StableSimulationId activityId, StableSimulationId ownerActorId, StableSimulationId definitionId,
            StableSimulationId? targetId, string workUnitId, ActivityProgressionKind progressionKind,
            long totalWork, long remainingWork, ActivityLifecycle lifecycle, string stateData = "") {
            if (string.IsNullOrWhiteSpace(activityId.Value) || string.IsNullOrWhiteSpace(ownerActorId.Value) || string.IsNullOrWhiteSpace(definitionId.Value))
                throw new ArgumentException("Activity, owner and definition ids are required.");
            if (targetId.HasValue && string.IsNullOrWhiteSpace(targetId.Value.Value)) throw new ArgumentException("Target id cannot be empty.", nameof(targetId));
            if (string.IsNullOrWhiteSpace(workUnitId)) throw new ArgumentException("A profile-defined work unit id is required.", nameof(workUnitId));
            if (!Enum.IsDefined(typeof(ActivityProgressionKind), progressionKind)) throw new ArgumentOutOfRangeException(nameof(progressionKind));
            if (!Enum.IsDefined(typeof(ActivityLifecycle), lifecycle)) throw new ArgumentOutOfRangeException(nameof(lifecycle));
            if (totalWork < 0 || remainingWork < 0 || remainingWork > totalWork) throw new ArgumentOutOfRangeException(nameof(remainingWork));
            if (lifecycle == ActivityLifecycle.Completed && remainingWork != 0)
                throw new ArgumentException("A completed activity cannot retain work.", nameof(remainingWork));
            ActivityId = activityId;
            OwnerActorId = ownerActorId;
            DefinitionId = definitionId;
            TargetId = targetId;
            WorkUnitId = workUnitId;
            ProgressionKind = progressionKind;
            TotalWork = totalWork;
            RemainingWork = remainingWork;
            Lifecycle = lifecycle;
            StateData = stateData ?? string.Empty;
        }

        public void Start() => Transition(ActivityLifecycle.Queued, ActivityLifecycle.Active);
        public void Suspend() => Transition(ActivityLifecycle.Active, ActivityLifecycle.Suspended);
        public void Resume() => Transition(ActivityLifecycle.Suspended, ActivityLifecycle.Active);
        public void Cancel() => TransitionAny(new[] { ActivityLifecycle.Queued, ActivityLifecycle.Active, ActivityLifecycle.Suspended }, ActivityLifecycle.Cancelled);
        public void Fail() => TransitionAny(new[] { ActivityLifecycle.Queued, ActivityLifecycle.Active, ActivityLifecycle.Suspended }, ActivityLifecycle.Failed);

        public void ApplyProgress(long work, long remainingWork, string stateData = null) {
            if (Lifecycle != ActivityLifecycle.Active) throw new InvalidOperationException("Only active work can progress.");
            if (work < 0 || remainingWork < 0 || remainingWork > RemainingWork || RemainingWork - remainingWork != work)
                throw new ArgumentOutOfRangeException(nameof(work), "Progress must monotonically reduce remaining work by the declared amount.");
            RemainingWork = remainingWork;
            if (stateData != null) StateData = stateData;
            if (RemainingWork == 0) Lifecycle = ActivityLifecycle.Completed;
        }

        private void Transition(ActivityLifecycle from, ActivityLifecycle to) {
            if (Lifecycle != from) throw new InvalidOperationException("Invalid activity lifecycle transition: " + Lifecycle + " -> " + to + ".");
            Lifecycle = to;
        }

        private void TransitionAny(IEnumerable<ActivityLifecycle> from, ActivityLifecycle to) {
            if (!from.Contains(Lifecycle)) throw new InvalidOperationException("Invalid activity lifecycle transition: " + Lifecycle + " -> " + to + ".");
            Lifecycle = to;
        }
    }
}
