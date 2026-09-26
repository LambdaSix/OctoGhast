using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace OctoGhast.Simulation {
    /// <summary>Monotonic canonical coordinate. Its rate and mapping to world chronology are profile-owned.</summary>
    public sealed class SimulationClock {
        public int TicksPerSecond { get; }
        public long CurrentTick { get; private set; }

        public SimulationClock(int ticksPerSecond, long initialTick = 0) {
            if (ticksPerSecond <= 0) throw new ArgumentOutOfRangeException(nameof(ticksPerSecond));
            if (initialTick < 0) throw new ArgumentOutOfRangeException(nameof(initialTick));
            TicksPerSecond = ticksPerSecond;
            CurrentTick = initialTick;
        }

        public long Advance(long stepCount = 1) {
            if (stepCount <= 0) throw new ArgumentOutOfRangeException(nameof(stepCount));
            CurrentTick = checked(CurrentTick + stepCount);
            return CurrentTick;
        }
    }

    /// <summary>An exact rules-profile mapping from canonical steps to the profile's chronology unit.</summary>
    public sealed class ProfileChronologyMapping {
        public string ProfileId { get; }
        public string UnitId { get; }
        public long UnitsNumeratorPerTick { get; }
        public long UnitsDenominatorPerTick { get; }
        public string MappingKey => ProfileId + "/" + UnitId + "/" + UnitsNumeratorPerTick + ":" + UnitsDenominatorPerTick;

        public ProfileChronologyMapping(string profileId, string unitId, long unitsNumeratorPerTick, long unitsDenominatorPerTick) {
            if (string.IsNullOrWhiteSpace(profileId)) throw new ArgumentException("A profile id is required.", nameof(profileId));
            if (string.IsNullOrWhiteSpace(unitId)) throw new ArgumentException("A chronology unit id is required.", nameof(unitId));
            if (unitsNumeratorPerTick <= 0) throw new ArgumentOutOfRangeException(nameof(unitsNumeratorPerTick));
            if (unitsDenominatorPerTick <= 0) throw new ArgumentOutOfRangeException(nameof(unitsDenominatorPerTick));
            ProfileId = profileId;
            UnitId = unitId;
            UnitsNumeratorPerTick = unitsNumeratorPerTick;
            UnitsDenominatorPerTick = unitsDenominatorPerTick;
        }
    }

    /// <summary>Persistable profile chronology coordinate and fractional conversion remainder.</summary>
    public sealed class ProfileChronologyCursor {
        public long Position { get; private set; }
        public long Remainder { get; private set; }
        public string MappingKey { get; private set; }

        public ProfileChronologyCursor(long position = 0, long remainder = 0, string mappingKey = null) {
            if (position < 0) throw new ArgumentOutOfRangeException(nameof(position));
            if (remainder < 0) throw new ArgumentOutOfRangeException(nameof(remainder));
            Position = position;
            Remainder = remainder;
            MappingKey = mappingKey;
        }

        public long Advance(ProfileChronologyMapping mapping, long ticks) {
            if (mapping == null) throw new ArgumentNullException(nameof(mapping));
            if (ticks < 0) throw new ArgumentOutOfRangeException(nameof(ticks));
            if (MappingKey != null && MappingKey != mapping.MappingKey)
                throw new InvalidOperationException("A chronology cursor cannot change profile mapping in place.");
            MappingKey = mapping.MappingKey;
            if (Remainder >= mapping.UnitsDenominatorPerTick) throw new InvalidOperationException("Saved chronology remainder does not match the selected profile mapping.");
            var total = new BigInteger(ticks) * mapping.UnitsNumeratorPerTick + Remainder;
            var units = total / mapping.UnitsDenominatorPerTick;
            if (units > long.MaxValue - Position) throw new OverflowException("Profile chronology exceeds the supported range.");
            Remainder = (long)(total % mapping.UnitsDenominatorPerTick);
            Position += (long)units;
            return (long)units;
        }
    }

    /// <summary>
    /// Converts elapsed host time to due fixed steps without dropping debt. Host pacing state is transient,
    /// never authoritative/persisted, and elapsed time is ignored while globally paused.
    /// </summary>
    public sealed class FixedStepHostPacer {
        private const long HostTicksPerSecond = TimeSpan.TicksPerSecond;
        private BigInteger _fractionNumerator;
        private BigInteger _fractionDenominator = BigInteger.One;
        private long _dueSteps;
        private int _scaleNumerator = 1;
        private int _scaleDenominator = 1;

        public int TicksPerSecond { get; }
        public bool IsPaused { get; private set; }
        public long DueSteps => _dueSteps;
        public BigInteger FractionNumerator => _fractionNumerator;
        public BigInteger FractionDenominator => _fractionDenominator;

        public FixedStepHostPacer(int ticksPerSecond) {
            if (ticksPerSecond <= 0) throw new ArgumentOutOfRangeException(nameof(ticksPerSecond));
            TicksPerSecond = ticksPerSecond;
        }

        public void SetPaused(bool paused) => IsPaused = paused;

        public void SetTimeScale(int numerator, int denominator) {
            if (numerator <= 0) throw new ArgumentOutOfRangeException(nameof(numerator));
            if (denominator <= 0) throw new ArgumentOutOfRangeException(nameof(denominator));
            var gcd = GreatestCommonDivisor(numerator, denominator);
            _scaleNumerator = numerator / gcd;
            _scaleDenominator = denominator / gcd;
        }

        public void Accumulate(TimeSpan elapsed) {
            if (elapsed < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(elapsed));
            if (IsPaused || elapsed == TimeSpan.Zero) return;

            var incomingNumerator = new BigInteger(elapsed.Ticks) * TicksPerSecond * _scaleNumerator;
            var incomingDenominator = new BigInteger(HostTicksPerSecond) * _scaleDenominator;
            var numerator = _fractionNumerator * incomingDenominator + incomingNumerator * _fractionDenominator;
            var denominator = _fractionDenominator * incomingDenominator;
            var steps = numerator / denominator;
            if (steps > long.MaxValue - _dueSteps) throw new OverflowException("Fixed-step debt exceeds the supported range.");
            _dueSteps += (long)steps;
            _fractionNumerator = numerator % denominator;
            _fractionDenominator = denominator;
            var fractionGcd = BigInteger.GreatestCommonDivisor(_fractionNumerator, _fractionDenominator);
            if (fractionGcd > BigInteger.One) {
                _fractionNumerator /= fractionGcd;
                _fractionDenominator /= fractionGcd;
            }
        }

        public int ConsumeDue(int maximumSteps) {
            if (maximumSteps <= 0) throw new ArgumentOutOfRangeException(nameof(maximumSteps));
            var steps = (int)Math.Min(_dueSteps, maximumSteps);
            _dueSteps -= steps;
            return steps;
        }

        private static int GreatestCommonDivisor(int a, int b) {
            while (b != 0) { var remainder = a % b; a = b; b = remainder; }
            return a;
        }
    }

    /// <summary>Signed actor-owned action currency with deterministic fractional accrual.</summary>
    public sealed class ActionBudget {
        public long Available { get; private set; }
        public long AccrualRemainder { get; private set; }
        public long AccrualDenominator { get; }

        public ActionBudget(long accrualDenominator = 1, long available = 0, long accrualRemainder = 0) {
            if (accrualDenominator <= 0) throw new ArgumentOutOfRangeException(nameof(accrualDenominator));
            if (accrualRemainder < 0) throw new ArgumentOutOfRangeException(nameof(accrualRemainder));
            if (accrualRemainder >= accrualDenominator) throw new ArgumentOutOfRangeException(nameof(accrualRemainder));
            AccrualDenominator = accrualDenominator;
            Available = available;
            AccrualRemainder = accrualRemainder;
        }

        /// <summary>A new action may begin only with positive opportunity; its full cost may drive budget negative.</summary>
        public bool CanBeginAction => Available > 0;

        public void Spend(long cost) {
            if (cost < 0) throw new ArgumentOutOfRangeException(nameof(cost));
            Available = checked(Available - cost);
        }

        public long Accrue(long numeratorPerTick, long ticks = 1) {
            if (numeratorPerTick < 0) throw new ArgumentOutOfRangeException(nameof(numeratorPerTick));
            if (ticks < 0) throw new ArgumentOutOfRangeException(nameof(ticks));
            if (AccrualRemainder >= AccrualDenominator) throw new InvalidOperationException("Saved accrual remainder must be less than the configured denominator.");
            var accumulated = new BigInteger(numeratorPerTick) * ticks + AccrualRemainder;
            var amount = accumulated / AccrualDenominator;
            var updated = new BigInteger(Available) + amount;
            if (amount < 0 || updated > long.MaxValue || updated < long.MinValue)
                throw new OverflowException("Action budget exceeds the supported range.");
            AccrualRemainder = (long)(accumulated % AccrualDenominator);
            Available = (long)updated;
            return (long)amount;
        }
    }

    /// <summary>Profile lane plus stable domain keys; admission/player/socket order is not the contention key.</summary>
    public struct SimulationWorkOrderKey : IComparable<SimulationWorkOrderKey>, IEquatable<SimulationWorkOrderKey> {
        public int LaneOrder { get; }
        public StableSimulationId ActorId { get; }
        public StableSimulationId WorkId { get; }
        public long QueueSequence { get; }

        public SimulationWorkOrderKey(int laneOrder, StableSimulationId actorId, StableSimulationId workId, long queueSequence) {
            if (string.IsNullOrWhiteSpace(actorId.Value) || string.IsNullOrWhiteSpace(workId.Value)) throw new ArgumentException("Stable actor and work ids are required.");
            if (queueSequence < 0) throw new ArgumentOutOfRangeException(nameof(queueSequence));
            LaneOrder = laneOrder;
            ActorId = actorId;
            WorkId = workId;
            QueueSequence = queueSequence;
        }

        public int CompareTo(SimulationWorkOrderKey other) {
            var result = LaneOrder.CompareTo(other.LaneOrder);
            if (result != 0) return result;
            result = ActorId.CompareTo(other.ActorId);
            if (result != 0) return result;
            result = WorkId.CompareTo(other.WorkId);
            return result != 0 ? result : QueueSequence.CompareTo(other.QueueSequence);
        }

        public bool Equals(SimulationWorkOrderKey other) => CompareTo(other) == 0;
        public override bool Equals(object obj) => obj is SimulationWorkOrderKey && Equals((SimulationWorkOrderKey)obj);
        public override int GetHashCode() {
            unchecked {
                var hash = LaneOrder;
                hash = hash * 397 ^ ActorId.GetHashCode();
                hash = hash * 397 ^ WorkId.GetHashCode();
                hash = hash * 397 ^ QueueSequence.GetHashCode();
                return hash;
            }
        }
        public static bool operator <(SimulationWorkOrderKey left, SimulationWorkOrderKey right) => left.CompareTo(right) < 0;
        public static bool operator >(SimulationWorkOrderKey left, SimulationWorkOrderKey right) => left.CompareTo(right) > 0;
        public static bool operator ==(SimulationWorkOrderKey left, SimulationWorkOrderKey right) => left.Equals(right);
        public static bool operator !=(SimulationWorkOrderKey left, SimulationWorkOrderKey right) => !left.Equals(right);
    }

    public sealed class ExecutionLane {
        public string Id { get; }
        public int Order { get; }
        public ExecutionLane(string id, int order) {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A lane id is required.", nameof(id));
            Id = id;
            Order = order;
        }
    }

    public sealed class SystemWorkDeclaration {
        public string SystemId { get; }
        public string LaneId { get; }
        public IReadOnlyCollection<string> Writes { get; }
        public IReadOnlyCollection<string> CommutativeWrites { get; }
        public SystemWorkDeclaration(string systemId, string laneId, IEnumerable<string> writes = null, IEnumerable<string> commutativeWrites = null) {
            if (string.IsNullOrWhiteSpace(systemId)) throw new ArgumentException("A system id is required.", nameof(systemId));
            if (string.IsNullOrWhiteSpace(laneId)) throw new ArgumentException("A lane id is required.", nameof(laneId));
            SystemId = systemId;
            LaneId = laneId;
            Writes = Array.AsReadOnly((writes ?? Enumerable.Empty<string>()).Distinct(StringComparer.Ordinal).ToArray());
            CommutativeWrites = Array.AsReadOnly((commutativeWrites ?? Enumerable.Empty<string>()).Distinct(StringComparer.Ordinal).ToArray());
            if (Writes.Any(string.IsNullOrWhiteSpace) || CommutativeWrites.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException("Writer resource keys must be non-empty.");
            if (CommutativeWrites.Except(Writes, StringComparer.Ordinal).Any())
                throw new ArgumentException("A commutative write must also be declared as a write.", nameof(commutativeWrites));
        }
    }

    /// <summary>Versioned profile-owned phase/lane plan; Core provides validation, not a permanent rules phase list.</summary>
    public sealed class SimulationExecutionPlan {
        private readonly Dictionary<string, ExecutionLane> _lanes;
        private readonly HashSet<Tuple<string, string>> _orderedPairs;
        private readonly SystemWorkDeclaration[] _systems;
        public string Version { get; }
        public IReadOnlyList<ExecutionLane> Lanes { get; }

        public SimulationExecutionPlan(string version, IEnumerable<ExecutionLane> lanes,
            IEnumerable<SystemWorkDeclaration> systems, IEnumerable<Tuple<string, string>> explicitBefore = null) {
            if (string.IsNullOrWhiteSpace(version)) throw new ArgumentException("A plan version is required.", nameof(version));
            Version = version;
            var suppliedLanes = (lanes ?? throw new ArgumentNullException(nameof(lanes))).ToArray();
            if (suppliedLanes.Any(l => l == null)) throw new ArgumentException("Lane declarations cannot be null.", nameof(lanes));
            var laneArray = suppliedLanes.OrderBy(l => l.Order).ThenBy(l => l.Id, StringComparer.Ordinal).ToArray();
            if (laneArray.Length == 0 || laneArray.Select(l => l.Order).Distinct().Count() != laneArray.Length || laneArray.Select(l => l.Id).Distinct(StringComparer.Ordinal).Count() != laneArray.Length)
                throw new ArgumentException("A plan requires uniquely identified lanes with unique order values.", nameof(lanes));
            Lanes = Array.AsReadOnly(laneArray);
            _lanes = laneArray.ToDictionary(l => l.Id, StringComparer.Ordinal);
            _systems = (systems ?? throw new ArgumentNullException(nameof(systems))).ToArray();
            if (_systems.Any(s => s == null)) throw new ArgumentException("System declarations cannot be null.", nameof(systems));
            if (_systems.Select(s => s.SystemId).Distinct(StringComparer.Ordinal).Count() != _systems.Length)
                throw new ArgumentException("System ids must be unique.", nameof(systems));
            if (_systems.Any(s => !_lanes.ContainsKey(s.LaneId))) throw new ArgumentException("Every system must reference a declared lane.", nameof(systems));
            _orderedPairs = new HashSet<Tuple<string, string>>(explicitBefore ?? Enumerable.Empty<Tuple<string, string>>());
            foreach (var pair in _orderedPairs) {
                if (pair == null || pair.Item1 == pair.Item2 || !_systems.Any(s => s.SystemId == pair.Item1) || !_systems.Any(s => s.SystemId == pair.Item2))
                    throw new ArgumentException("Explicit ordering must name two distinct declared systems.", nameof(explicitBefore));
                var before = _systems.Single(s => s.SystemId == pair.Item1);
                var after = _systems.Single(s => s.SystemId == pair.Item2);
                if (LaneOrder(before.LaneId) > LaneOrder(after.LaneId))
                    throw new ArgumentException("Explicit ordering cannot contradict the profile lane order.", nameof(explicitBefore));
            }
            ValidateWriters(_systems);
            ValidateNoOrderingCycles();
        }

        public int LaneOrder(string laneId) => _lanes[laneId].Order;

        public IReadOnlyList<SystemWorkDeclaration> OrderSystems() {
            var ordered = new List<SystemWorkDeclaration>();
            foreach (var lane in Lanes) {
                var laneSystems = _systems.Where(s => s.LaneId == lane.Id).ToArray();
                var remaining = new HashSet<string>(laneSystems.Select(s => s.SystemId), StringComparer.Ordinal);
                while (remaining.Count > 0) {
                    var ready = laneSystems.Where(s => remaining.Contains(s.SystemId) &&
                        !_orderedPairs.Any(edge => remaining.Contains(edge.Item1) && edge.Item2 == s.SystemId))
                        .OrderBy(s => s.SystemId, StringComparer.Ordinal).FirstOrDefault();
                    if (ready == null) throw new InvalidOperationException("The profile plan contains a system ordering cycle.");
                    ordered.Add(ready);
                    remaining.Remove(ready.SystemId);
                }
            }
            return ordered.AsReadOnly();
        }

        private void ValidateNoOrderingCycles() {
            foreach (var system in _systems) {
                if (HasPath(system.SystemId, system.SystemId, _systems))
                    throw new ArgumentException("Explicit system ordering contains a cycle.", nameof(_orderedPairs));
            }
        }

        private void ValidateWriters(SystemWorkDeclaration[] systems) {
            for (var i = 0; i < systems.Length; i++) {
                for (var j = i + 1; j < systems.Length; j++) {
                    var left = systems[i];
                    var right = systems[j];
                    if (LaneOrder(left.LaneId) != LaneOrder(right.LaneId)) continue;
                    var overlap = left.Writes.Intersect(right.Writes, StringComparer.Ordinal).ToArray();
                    foreach (var resource in overlap) {
                        var ordered = HasPath(left.SystemId, right.SystemId, systems) || HasPath(right.SystemId, left.SystemId, systems);
                        var commutative = left.CommutativeWrites.Contains(resource, StringComparer.Ordinal) && right.CommutativeWrites.Contains(resource, StringComparer.Ordinal);
                        if (!ordered && !commutative)
                            throw new InvalidOperationException("Unresolved authoritative writer ambiguity for '" + resource + "' between " + left.SystemId + " and " + right.SystemId + ".");
                    }
                }
            }
        }

        private bool HasPath(string from, string to, SystemWorkDeclaration[] systems) {
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var pending = new Stack<string>();
            pending.Push(from);
            while (pending.Count > 0) {
                var current = pending.Pop();
                if (!visited.Add(current)) continue;
                foreach (var edge in _orderedPairs.Where(p => p.Item1 == current)) {
                    if (edge.Item2 == to) return true;
                    pending.Push(edge.Item2);
                }
            }
            return false;
        }
    }

    public sealed class ScheduledWork {
        public long DueTick { get; }
        public string LaneId { get; }
        public StableSimulationId ActorId { get; }
        public StableSimulationId WorkId { get; }
        public long QueueSequence { get; }
        public string Payload { get; }

        public ScheduledWork(long dueTick, string laneId, StableSimulationId actorId, StableSimulationId workId, long queueSequence, string payload) {
            if (dueTick < 0) throw new ArgumentOutOfRangeException(nameof(dueTick));
            if (string.IsNullOrWhiteSpace(laneId)) throw new ArgumentException("A lane id is required.", nameof(laneId));
            if (string.IsNullOrWhiteSpace(actorId.Value) || string.IsNullOrWhiteSpace(workId.Value)) throw new ArgumentException("Stable actor and work ids are required.");
            if (queueSequence < 0) throw new ArgumentOutOfRangeException(nameof(queueSequence));
            DueTick = dueTick; LaneId = laneId; ActorId = actorId; WorkId = workId; QueueSequence = queueSequence; Payload = payload ?? string.Empty;
        }
    }

    public enum ScheduledWorkOrder { ProfileLaneActorWork, DueTickThenInsertion }

    /// <summary>Persistent due work ordered by deadline, profile lane, actor/work identity, then queue-local insertion sequence.</summary>
    public sealed class DeterministicScheduler {
        private readonly List<ScheduledWork> _pending = new List<ScheduledWork>();
        private long _nextSequence;
        private readonly SimulationExecutionPlan _plan;
        private readonly ScheduledWorkOrder _order;
        public long NextSequence => _nextSequence;
        public IReadOnlyList<ScheduledWork> Pending => Array.AsReadOnly(_pending.ToArray());

        public DeterministicScheduler(SimulationExecutionPlan plan, long nextSequence = 0,
            ScheduledWorkOrder order = ScheduledWorkOrder.ProfileLaneActorWork, IEnumerable<ScheduledWork> pending = null) {
            _plan = plan ?? throw new ArgumentNullException(nameof(plan));
            if (nextSequence < 0) throw new ArgumentOutOfRangeException(nameof(nextSequence));
            if (!Enum.IsDefined(typeof(ScheduledWorkOrder), order)) throw new ArgumentOutOfRangeException(nameof(order));
            _nextSequence = nextSequence;
            _order = order;
            var restored = (pending ?? Enumerable.Empty<ScheduledWork>()).ToArray();
            if (restored.Any(w => w == null || w.QueueSequence >= nextSequence) ||
                restored.Select(w => w.QueueSequence).Distinct().Count() != restored.Length)
                throw new ArgumentException("Restored work must have unique queue sequences below the persisted next sequence.", nameof(pending));
            foreach (var item in restored) _plan.LaneOrder(item.LaneId);
            _pending.AddRange(restored);
        }

        public ScheduledWork Schedule(long dueTick, string laneId, StableSimulationId actorId, StableSimulationId workId, string payload) {
            if (dueTick < 0) throw new ArgumentOutOfRangeException(nameof(dueTick));
            _plan.LaneOrder(laneId); // fail early if the profile has no such lane
            var sequence = _nextSequence;
            _nextSequence = checked(_nextSequence + 1);
            var item = new ScheduledWork(dueTick, laneId, actorId, workId, sequence, payload);
            _pending.Add(item);
            return item;
        }

        public IReadOnlyList<ScheduledWork> TakeDue(long currentTick, string laneId = null) {
            if (currentTick < 0) throw new ArgumentOutOfRangeException(nameof(currentTick));
            if (laneId != null) _plan.LaneOrder(laneId);
            var dueSource = _pending.Where(w => w.DueTick <= currentTick && (laneId == null || w.LaneId == laneId));
            var due = _order == ScheduledWorkOrder.DueTickThenInsertion
                ? dueSource.OrderBy(w => w.DueTick).ThenBy(w => w.QueueSequence).ToArray()
                : dueSource.OrderBy(w => w.DueTick).ThenBy(w => _plan.LaneOrder(w.LaneId))
                    .ThenBy(w => w.ActorId).ThenBy(w => w.WorkId).ThenBy(w => w.QueueSequence).ToArray();
            foreach (var item in due) _pending.Remove(item);
            return Array.AsReadOnly(due);
        }

        /// <summary>Consumes a canonical interval [fromTickInclusive, toTickExclusive); an endpoint deadline belongs to the next interval.</summary>
        public IReadOnlyList<ScheduledWork> TakeDueInHalfOpenInterval(long fromTickInclusive, long toTickExclusive, string laneId = null) {
            if (fromTickInclusive < 0) throw new ArgumentOutOfRangeException(nameof(fromTickInclusive));
            if (toTickExclusive < fromTickInclusive) throw new ArgumentOutOfRangeException(nameof(toTickExclusive));
            if (laneId != null) _plan.LaneOrder(laneId);
            var dueSource = _pending.Where(w => w.DueTick >= fromTickInclusive && w.DueTick < toTickExclusive &&
                (laneId == null || w.LaneId == laneId));
            var due = _order == ScheduledWorkOrder.DueTickThenInsertion
                ? dueSource.OrderBy(w => w.DueTick).ThenBy(w => w.QueueSequence).ToArray()
                : dueSource.OrderBy(w => w.DueTick).ThenBy(w => _plan.LaneOrder(w.LaneId))
                    .ThenBy(w => w.ActorId).ThenBy(w => w.WorkId).ThenBy(w => w.QueueSequence).ToArray();
            foreach (var item in due) _pending.Remove(item);
            return Array.AsReadOnly(due);
        }
    }
}
