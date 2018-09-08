using System;
using System.Collections.Generic;
using Capsicum;
using Capsicum.Interfaces;

namespace OctoGhast.Activities {
    public class ActivitySystem : ISystem {
        private List<Activity> _activities = new List<Activity>();

        /// <inheritdoc />
        public void Update(ulong time) {
            foreach (var activity in _activities) {
                activity.Update(time);
            }
        }

        public void AddActivity(Activity activity) {
            activity.OnFinishActivity += FinishActivity();
            _activities.Add(activity);
        }

        private EventHandler<Activity> FinishActivity() {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// An Activity is something that takes multiple turns/moves, can be suspended and resumed, and possibly has requirements for continuing.
    /// 
    /// Sensible criteria for if something should be an Activity:
    ///  - Takes time to perform
    ///  - Is Interruptable
    ///  - Can be stopped and continued later
    ///  - Has multiple stages of work (see Butchery)
    ///  - Requires conditions to perform & continue the action
    /// </summary>
    public abstract class Activity {
        /// <summary>
        /// The total amount of time required to complete this activity
        /// </summary>        
        public int TotalTime { get; private set; }

        public Entity Source { get; }
        public Entity Target { get; }

        public bool IsSuspended { get; private set; }

        public bool InterruptFlag { get; private set; }

        public event EventHandler<Activity> OnFinishActivity;

        /// <summary>
        /// Create a new Activity
        /// </summary>
        /// <param name="totalTime">Total period of time this activity will take</param>
        /// <param name="source">The entity that started this activity</param>
        /// <param name="target">The target of this activity</param>
        protected Activity(int totalTime, Entity source, Entity target) {
            // TODO: Work out if this is Moves, Time or Turns.
            TotalTime = totalTime;
            Source = source;
            Target = target;
        }

        protected void AddRequiredTime(int extraTime) {
            TotalTime += extraTime;
        }

        protected T? QuerySource<T>(string message, T?[] options) where T: struct  {
            // If Source doesn't have an PlayerActorComponent tag, ignore this request and return null (or throw exception?)
            if (!Source.TryGetComponent<PlayerActorComponent>(out var isPlayer)) {
                return null;
            }

            // TODO: Emit a message to the Interaction system that we need to query the user, await the reply
            // TODO: Write an UserInteraction system & Messaging system

            throw new NotImplementedException();
        }

        /// <summary>
        /// Start performing an action.
        /// This should check that any required conditions are true before starting.
        /// </summary>
        /// <returns>True if the action was able to start, false if it could not.</returns>
        public abstract bool Start();

        /// <summary>
        /// Called every turn for the Source to update the progress of the activity
        /// </summary>
        /// <param name="turn">The current time</param>
        /// <returns>True if the action should continue processing, false if it has cancelled out or finished</returns>
        public abstract bool Update(UInt64 time);

        /// <summary>
        /// Indicate this activity should suspend.
        /// Sub-classed activities may override this with custom behaviour.
        /// </summary>
        public virtual void Suspend() => IsSuspended = true;

        /// <summary>
        /// Notifies subscribers that this Activity has finished.
        /// This should be called from Update() by the Activity when it has finished.
        /// </summary>
        /// <returns></returns>
        public virtual void Finish() => OnFinishActivity?.Invoke(null, this);

        /// <summary>
        /// Try and interrupt the activity.
        /// </summary>
        /// <remarks>
        /// Implementing classes get a chance to block this.
        /// </remarks>
        public virtual bool Interrupt() => InterruptFlag = true;
    }

    /// <summary>
    /// An Activity is something that takes multiple turns/moves, can be suspended and resumed, and possibly has requirements for continuing.
    /// This is a specialisation of Activity, one that is Asynchronous and shouldn't block the player.
    /// 
    /// Sensible criteria for if something should be an Activity:
    ///  - Takes time to perform
    ///  - Is Interruptable
    ///  - Can be stopped and continued later
    ///  - Has multiple stages of work (see Butchery)
    ///  - Requires conditions to perform & continue the action
    /// </summary>
    public abstract class AsyncActivity : Activity
    {
        /// <inheritdoc />
        protected AsyncActivity(int totalTime, Entity source, Entity target) : base(totalTime, source, target) { }
    }
}