using System;
using System.Linq;
using NUnit.Framework;
using OctoGhast.Simulation;

namespace OctoGhast.Core.Tests {
    [TestFixture]
    public class SimulationRuntimeTests {
        [Test]
        public void CanonicalClockAndFixedStepPacerKeepDebtWithoutDroppingSteps() {
            var pacer = new FixedStepHostPacer(10);
            pacer.Accumulate(TimeSpan.FromMilliseconds(2450));
            Assert.AreEqual(24, pacer.ConsumeDue(24));
            Assert.AreEqual(0, pacer.DueSteps);
            Assert.AreEqual(0, pacer.ConsumeDue(20));
            pacer.Accumulate(TimeSpan.FromMilliseconds(50));
            Assert.AreEqual(1, pacer.ConsumeDue(20));

            var clock = new SimulationClock(10);
            clock.Advance(7);
            Assert.AreEqual(7, clock.CurrentTick);
            pacer.Accumulate(TimeSpan.FromSeconds(1));
            Assert.AreEqual(3, pacer.ConsumeDue(3));
            Assert.AreEqual(7, pacer.DueSteps);
            Assert.AreEqual(7, pacer.ConsumeDue(20));
        }

        [Test]
        public void ProfileChronologyMappingConvertsTicksWithoutBakingInOneRulesRate() {
            var mapping = new ProfileChronologyMapping("cataclysm", "world-second", 1, 10);
            var cursor = new ProfileChronologyCursor();
            Assert.AreEqual(0, cursor.Advance(mapping, 9));
            Assert.AreEqual(9, cursor.Remainder);
            Assert.AreEqual(1, cursor.Advance(mapping, 1));
            Assert.AreEqual(1, cursor.Position);
            Assert.AreEqual(0, cursor.Remainder);
        }

        [Test]
        public void HostPacingIsIgnoredWhilePausedAndTimeScaleIsExact() {
            var pacer = new FixedStepHostPacer(10);
            pacer.SetPaused(true);
            pacer.Accumulate(TimeSpan.FromSeconds(2));
            Assert.AreEqual(0, pacer.DueSteps);
            pacer.SetPaused(false);
            pacer.SetTimeScale(3, 2);
            pacer.Accumulate(TimeSpan.FromSeconds(1));
            Assert.AreEqual(15, pacer.ConsumeDue(20));

            var scaledAtBoundary = new FixedStepHostPacer(10);
            scaledAtBoundary.Accumulate(TimeSpan.FromMilliseconds(50));
            scaledAtBoundary.SetTimeScale(3, 2);
            scaledAtBoundary.Accumulate(TimeSpan.FromTicks(333334));
            Assert.AreEqual(1, scaledAtBoundary.ConsumeDue(20));
        }

        [Test]
        public void ActionBudgetUsesProfileSuppliedRationalAndAllowsNegativeAfterCost() {
            var budget = new ActionBudget(2);
            Assert.AreEqual(3, budget.Accrue(7, 1));
            Assert.AreEqual(4, budget.Accrue(7, 1));
            Assert.AreEqual(7, budget.Available);
            Assert.AreEqual(0, budget.AccrualRemainder);
            Assert.IsTrue(budget.CanBeginAction);
            budget.Spend(12);
            Assert.AreEqual(-5, budget.Available);
            Assert.IsFalse(budget.CanBeginAction);
        }

        [Test]
        public void AdmissionFreezesAndRotatesCandidatesButDoesNotResolveActorContention() {
            var admission = new BoundedCommandAdmission();
            var first = admission.Admit(new[] {
                new CommandLifecycle(new TestCommand("b1", "B", "actor-z", 1)),
                new CommandLifecycle(new TestCommand("a1", "A", "actor-z", 1)),
                new CommandLifecycle(new TestCommand("a2", "A", "actor-a", 2))
            }, new CommandAdmissionLimits(1, 1), 40, null);

            CollectionAssert.AreEqual(new[] { "A" }, first.Admitted.Select(c => c.Command.PlayerId.Value.Value).ToArray());
            Assert.AreEqual(2, first.Deferred.Count);
            Assert.IsTrue(first.Deferred.All(c => c.State == CommandState.Deferred));
            Assert.AreEqual(41, first.NextAdmissionSequence);

            var next = admission.Admit(first.Deferred, new CommandAdmissionLimits(1, 1), first.NextAdmissionSequence, first.NextPlayerCursor);
            Assert.AreEqual("B", next.Admitted[0].Command.PlayerId.Value.Value);

            var third = admission.Admit(next.Deferred, new CommandAdmissionLimits(1, 1), next.NextAdmissionSequence, next.NextPlayerCursor);
            Assert.AreEqual(1, third.Admitted.Count, "A command may remain deferred across more than one bounded intake step.");

            var scopedIds = admission.Admit(new[] {
                new CommandLifecycle(new TestCommand("same-operation", "A", "actor-a", 4)),
                new CommandLifecycle(new TestCommand("same-operation", "B", "actor-b", 4))
            }, new CommandAdmissionLimits(2, 1), next.NextAdmissionSequence, next.NextPlayerCursor);
            Assert.AreEqual(2, scopedIds.Admitted.Count);
        }

        [Test]
        public void CommandLifecycleRequiresAdmissionBeforeResolutionAndCapturesRejectedOutcome() {
            var lifecycle = new CommandLifecycle(new TestCommand("move", "player", "actor", 0));
            Assert.Throws<InvalidOperationException>(() => lifecycle.Resolve(new TestContext()));
            lifecycle.Admit(0);
            lifecycle.Resolve(new TestContext());
            Assert.AreEqual(CommandState.Rejected, lifecycle.State);
            Assert.AreEqual("stale-target", lifecycle.Outcome.Code);
        }

        [Test]
        public void PlayerAdmissionOrderDoesNotReplaceStableActorExecutionOrder() {
            var plan = new SimulationExecutionPlan("test-v1", new[] { new ExecutionLane("commands", 1) },
                new[] { new SystemWorkDeclaration("command-resolution", "commands") });
            var batch = new BoundedCommandAdmission().Admit(new[] {
                new CommandLifecycle(new TestCommand("req-a", "A", "character-z", 0)),
                new CommandLifecycle(new TestCommand("req-b", "B", "character-a", 0))
            }, new CommandAdmissionLimits(2, 1), 10, null);
            var execution = batch.Admitted.Select(c => c.ExecutionKey(plan, "commands")).OrderBy(key => key).ToArray();
            CollectionAssert.AreEqual(new[] { "character-a", "character-z" }, execution.Select(key => key.ActorId.Value).ToArray());
        }

        [Test]
        public void QueriesAreSynchronousAndEventsUseAppendSequence() {
            var queryBus = new SynchronousQueryBus();
            queryBus.Register<PositionQuery, int>(query => query.Value + 1);
            Assert.AreEqual(9, queryBus.Ask<PositionQuery, int>(new PositionQuery(8)));

            var events = new OrderedEventJournal();
            events.Publish(12, "actor-moved", "a1");
            events.Publish(12, "door-opened", "d1");
            Assert.AreEqual(2, events.NextSequence);
            CollectionAssert.AreEqual(new[] { "actor-moved", "door-opened" }, events.ReadAfter(0).Select(e => e.TypeId).ToArray());
            var bounded = new OrderedEventJournal(2);
            bounded.Publish(1, "first", "");
            bounded.Publish(1, "second", "");
            bounded.Publish(1, "third", "");
            Assert.Throws<InvalidOperationException>(() => bounded.ReadAfter(-1));
            CollectionAssert.AreEqual(new[] { "second", "third" }, bounded.ReadAfter(0).Select(e => e.TypeId).ToArray());
        }

        [Test]
        public void ActivityRecordEnforcesDurableLifecycleAndMonotonicProgress() {
            var record = new ActivityRecord(new StableSimulationId("work-1"), new StableSimulationId("actor-1"),
                new StableSimulationId("craft"), null, "profile-work-unit", ActivityProgressionKind.ProfileScheduledWork,
                10, 10, ActivityLifecycle.Queued, "recipe=knife");
            record.Start();
            record.ApplyProgress(4, 6, "recipe=knife;stage=1");
            record.Suspend();
            Assert.Throws<InvalidOperationException>(() => record.ApplyProgress(1, 5));
            record.Resume();
            record.ApplyProgress(6, 0);
            Assert.AreEqual(ActivityLifecycle.Completed, record.Lifecycle);
        }

        [Test]
        public void SchedulerUsesProfileLaneThenStableActorAndWorkIdentity() {
            var plan = new SimulationExecutionPlan("test-v1", new[] { new ExecutionLane("early", 0), new ExecutionLane("late", 1) },
                new[] { new SystemWorkDeclaration("system", "early") });
            var scheduler = new DeterministicScheduler(plan);
            scheduler.Schedule(5, "late", new StableSimulationId("a"), new StableSimulationId("w0"), null);
            scheduler.Schedule(5, "early", new StableSimulationId("z"), new StableSimulationId("w0"), null);
            scheduler.Schedule(5, "early", new StableSimulationId("a"), new StableSimulationId("w1"), null);
            scheduler.Schedule(4, "late", new StableSimulationId("z"), new StableSimulationId("w0"), null);

            CollectionAssert.AreEqual(new[] { "4:late:z:w0", "5:early:a:w1", "5:early:z:w0", "5:late:a:w0" },
                scheduler.TakeDue(5).Select(w => w.DueTick + ":" + w.LaneId + ":" + w.ActorId + ":" + w.WorkId).ToArray());
        }

        [Test]
        public void QueueSpecificInsertionOrderAndPendingSequenceSurviveRestore() {
            var plan = new SimulationExecutionPlan("test-v1", new[] { new ExecutionLane("eoc", 0) },
                new[] { new SystemWorkDeclaration("eoc", "eoc") });
            var scheduler = new DeterministicScheduler(plan, 0, ScheduledWorkOrder.DueTickThenInsertion);
            scheduler.Schedule(10, "eoc", new StableSimulationId("z"), new StableSimulationId("later-key"), "first");
            scheduler.Schedule(10, "eoc", new StableSimulationId("a"), new StableSimulationId("earlier-key"), "second");
            var restored = new DeterministicScheduler(plan, scheduler.NextSequence, ScheduledWorkOrder.DueTickThenInsertion, scheduler.Pending);
            CollectionAssert.AreEqual(new[] { "first", "second" }, restored.TakeDue(10).Select(w => w.Payload).ToArray());
            Assert.AreEqual(2, restored.NextSequence);
        }

        [Test]
        public void ScheduledDeadlinesUseHalfOpenIntervalsAtBoundaries() {
            var plan = new SimulationExecutionPlan("test-v1", new[] { new ExecutionLane("world", 0) },
                new[] { new SystemWorkDeclaration("scheduler", "world") });
            var scheduler = new DeterministicScheduler(plan);
            scheduler.Schedule(2, "world", new StableSimulationId("actor"), new StableSimulationId("deadline-2"), "at-2");
            scheduler.Schedule(3, "world", new StableSimulationId("actor"), new StableSimulationId("deadline-3"), "at-3");
            CollectionAssert.AreEqual(new[] { "at-2" }, scheduler.TakeDueInHalfOpenInterval(0, 3).Select(w => w.Payload).ToArray());
            CollectionAssert.AreEqual(new[] { "at-3" }, scheduler.TakeDueInHalfOpenInterval(3, 4).Select(w => w.Payload).ToArray());
        }

        [Test]
        public void PlanRejectsAmbiguousWritersAndKeepsProfileVersion() {
            var lanes = new[] { new ExecutionLane("actors", 3) };
            Assert.Throws<InvalidOperationException>(() => new SimulationExecutionPlan("cataclysm-v1", lanes, new[] {
                new SystemWorkDeclaration("hazards", "actors", new[] { "actor:budget" }),
                new SystemWorkDeclaration("actor-turn", "actors", new[] { "actor:budget" })
            }));

            var plan = new SimulationExecutionPlan("cataclysm-v2", lanes, new[] {
                new SystemWorkDeclaration("hazards", "actors", new[] { "actor:budget" }),
                new SystemWorkDeclaration("actor-turn", "actors", new[] { "actor:budget" })
            }, new[] { Tuple.Create("hazards", "actor-turn") });
            Assert.AreEqual("cataclysm-v2", plan.Version);
            CollectionAssert.AreEqual(new[] { "hazards", "actor-turn" }, plan.OrderSystems().Select(s => s.SystemId).ToArray());
        }

        private sealed class TestCommand : ISimulationCommand {
            public StableSimulationId CommandId { get; }
            public string CommandTypeId => "test-command";
            public CommandRetryPolicy RetryPolicy => CommandRetryPolicy.StateReconciled;
            public StableSimulationId? PlayerId { get; }
            public StableSimulationId ActorId { get; }
            public long RequestSequence { get; }
            public TestCommand(string id, string player, string actor, long sequence) {
                CommandId = new StableSimulationId(id);
                PlayerId = player == null ? (StableSimulationId?)null : new StableSimulationId(player);
                ActorId = new StableSimulationId(actor);
                RequestSequence = sequence;
            }
            public CommandOutcome Resolve(ISimulationCommandContext context) => CommandOutcome.Rejected("stale-target");
        }
        private sealed class TestContext : ISimulationCommandContext { }
        private sealed class PositionQuery : ISimulationQuery<int> {
            public int Value { get; }
            public PositionQuery(int value) { Value = value; }
        }
    }
}
