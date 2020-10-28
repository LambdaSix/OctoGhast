using System;
using System.Collections.Generic;
using System.Net.Configuration;
using OctoGhast.Framework.Mobile;

namespace OctoGhast.Framework.Activities {

    /// <summary>
    /// Base class for handling an activity.
    /// </summary>
    public abstract class ActivityHandler {

        /// <summary>
        /// Can this activity be started in the current situation?
        /// </summary>
        /// <param name="info">ActivityInfo to investigate</param>
        public virtual bool CanStart(ActivityInfo info) => true;

        /// <summary>
        /// Can this activity be continued in the current situation?
        /// This is called every turn before ProcessStep().
        /// If you require in-depth checking, it's recommended to cache your situation
        /// in <see cref="ActivityInfo.ActivityData"/> and re-check periodically.
        /// </summary>
        /// <param name="info">ActivityInfo to investigate</param>
        public virtual bool CanContinue(ActivityInfo info) => true;

        /// <summary>
        /// Called when this Activity is interrupted.
        /// What this should do depends on the activity, 
        /// </summary>
        /// <param name="info"></param>
        /// <returns>
        /// True if the task is 'safely' interrupted and can later be resumed.
        /// False is the task is ruined by interruption and cannot be resumed.
        /// </returns>
        public virtual bool OnInterrupt(ActivityInfo info) => true;

        /// <summary>
        /// Estimate how long this task will take.
        /// </summary>
        /// <param name="info">ActivityInfo to investigate</param>
        /// <returns>TimeDuration of how long the task is likely to take</returns>
        public virtual TimeDuration EstimateDuration(ActivityInfo info) => TimeDuration.FromTurns(0);

        /// <summary>
        /// Process a unit of activity.
        /// </summary>
        /// <param name="info">ActivityInfo to investigate</param>
        /// <returns></returns>
        public virtual IEnumerable<ActivityStep> ProcessStep(ActivityInfo info) {
            yield return new ActivityStep();
        }

        /// <summary>
        /// Finalize this activity.
        /// </summary>
        /// <param name="info">ActivityInfo to investigate</param>
        /// <returns>
        /// A filled ActivityResult structure indicating the status of this activity.
        /// </returns>
        public virtual ActivityResult FinishActivity(ActivityInfo info) => new ActivityResult(success: false);
    }

    /// <summary>
    /// Information about a single step of an activity.
    /// </summary>
    public class ActivityStep {

    }

    /// <summary>
    /// Final information about an activity result.
    /// </summary>
    public class ActivityResult {
        public bool Success { get; }

        public ActivityResult(bool success) {
            Success = success;
        }
    }

    public enum ActivityPriority : short {
        /// <summary>
        /// High Priority, such as maintaining needs or checking for hostile environments.
        /// </summary>
        High,

        /// <summary>
        /// Normal priority tasks, this is generally the current activity.
        /// </summary>
        Normal,
    }

    public class ActivityInfo {
        /// <summary>
        /// The owner of this activity.
        /// </summary>
        public BaseCreature Owner { get; set; }

        /// <summary>
        /// Is this activity a passive task?
        /// </summary>
        public bool IsPassive { get; set; }

        /// <summary>
        /// The turn that this Activity was started at.
        /// </summary>
        public long StartingTurn { get; set; }

        /// <summary>
        /// The current turn, updated once per turn.
        /// </summary>
        public long CurrentTurn { get; set; }

        /// <summary>
        /// Priority of this activity.
        /// </summary>
        public ActivityPriority Priority { get; set; }

        /// <summary>
        /// The handler instance for this activity
        /// </summary>
        public ActivityHandler Handler { get; set; }

        /// <summary>
        /// Any activity data needed by the handler
        /// </summary>
        public Dictionary<string,object> ActivityData { get; set; }

        /// <summary>
        /// Is this activity resumable?
        /// </summary>
        public bool CanResume { get; set; }
    }

    /// <summary>
    /// Part of the BaseCreature API, this allows queueing of activities that may take multiple turns.
    /// </summary>
    public interface IActivityQueue : IEnumerable<ActivityInfo> {
        /// <summary>
        /// Enqueue an activity.
        /// </summary>
        /// <param name="activity"></param>
        /// <returns>A tuple of success and a unique identifier for the activity</returns>
        (bool,string) EnqueueActivity(ActivityInfo activity);

        /// <summary>
        /// Dequeue an activity from the queue.
        /// </summary>
        /// <param name="activity"></param>
        /// <returns>True if the activity was dequeued, false if it could not be.</returns>
        bool DequeueActivity(ActivityInfo activity);

        /// <summary>
        /// Dequeue an activity using it's unique key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        bool DequeueActivity(string key);
    }
}