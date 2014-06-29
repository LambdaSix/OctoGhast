using System;
using System.Collections.Generic;
using System.Linq;
using libtcod;
using OctoGhast.Spatial;
using OctoGhast.UserInterface.Core.Messages;

namespace OctoGhast.UserInterface.Core
{
    public class Schedule
    {
        public Action Callback { get; private set; }
        public uint DelayMs { get; private set; }
        public uint Count { get; private set; }

        public Schedule(Action callback, uint delayMs) {
            Callback = callback;
            DelayMs = delayMs;
        }

        public void Update(uint elapsedMs) {
            Count += elapsedMs;
            if (Count >= DelayMs) {
                Count = 0;
                Callback();
            }
        }

        internal void Reset() {
            Count = 0;
        }
    }

    public abstract class Component
    {
        private ICollection<Schedule> Schedules = new List<Schedule>();
        private ICollection<Schedule> ScheduleRemoveList = new List<Schedule>();
        private ICollection<Schedule> ScheduleAddList = new List<Schedule>();
        private bool _isSetup;

        public bool Initialized { get { return _isSetup; } }

        /// <summary>
        /// Current mouse position in screen space from the last MouseMove
        /// message recieved.
        /// </summary>
        public Vec CurrentMousePosition { get; private set; }

        /// <summary>
        /// Total elapsed milliseconds since the start of application
        /// </summary>
        public uint TotalElapsed { get; private set; }

        /// <summary>
        /// Time elapsed since last tick message this component received.
        /// </summary>
        public uint LastTickElapsed { get; private set; }

        public event EventHandler SettingUp;

        public event EventHandler Tick;

        public event EventHandler Quitting;

        public event EventHandler<KeyboardEventArgs> KeyPressed;

        public event EventHandler<KeyboardEventArgs> KeyReleased;

        public event EventHandler<MouseEventArgs> MouseMoved;

        public event EventHandler<MouseEventArgs> MouseButtonUp;

        public event EventHandler<MouseEventArgs> MouseButtonDown;

        public event EventHandler<MouseEventArgs> MouseHoverBegin;

        public event EventHandler<MouseEventArgs> MouseHoverEnd;

        public event EventHandler<MouseDragEventArgs> MouseDragBegin;

        public event EventHandler<MouseDragEventArgs> MouseDragEnd;

        public void AddSchedule(Schedule schedule) {
            if (containsSchedule(schedule)) {
                throw new ArgumentException("Schedule instances must be unique to a component", "schedule");
            }

            schedule.Reset();
            ScheduleAddList.Add(schedule);
        }

        public void RemoveSchedule(Schedule schedule) {
            // Queue the schedule to be removed.
            if (Schedules.Contains(schedule))
                ScheduleRemoveList.Add(schedule);
            // Or remove it from the pending-add queue.
            if (ScheduleAddList.Contains(schedule))
                ScheduleAddList.Remove(schedule);
        }

        public virtual void OnSettingUp() {
            if (_isSetup)
                return;

            if (SettingUp != null)
                SettingUp(this, EventArgs.Empty);

            _isSetup = true;
        }

        public virtual void OnTick() {
            uint milli = TCODSystem.getElapsedMilli();

            LastTickElapsed = milli - TotalElapsed;
            TotalElapsed = milli;

            if (Tick != null)
                Tick(this, EventArgs.Empty);

            foreach (var schedule in Schedules) {
                schedule.Update(LastTickElapsed);
            }

            if (ScheduleRemoveList.Any()) {
                foreach (var schedule in ScheduleRemoveList) {
                    Schedules.Remove(schedule);
                }
                ScheduleRemoveList = new List<Schedule>();
            }

            if (ScheduleAddList.Any()) {
                foreach (var schedule in ScheduleAddList) {
                    Schedules.Add(schedule);
                }
                ScheduleAddList = new List<Schedule>();
            }
        }

        public virtual void OnQuitting() {
            if (Quitting != null) Quitting(this, EventArgs.Empty);
        }

        public virtual void OnKeyPressed(KeyboardData keyData) {
            if (KeyPressed != null) KeyPressed(this, new KeyboardEventArgs(keyData));
        }

        public virtual void OnKeyReleased(KeyboardData keyData) {
            if (KeyPressed != null) KeyReleased(this, new KeyboardEventArgs(keyData));
        }

        public virtual void OnMouseMoved(MouseData mouseData) {
            CurrentMousePosition = mouseData.Position;
            if (MouseMoved != null) MouseMoved(this, new MouseEventArgs(mouseData));
        }

        public virtual void OnMouseButtonDown(MouseData mouseData) {
            if (MouseButtonDown != null) MouseButtonDown(this, new MouseEventArgs(mouseData));
        }

        public virtual void OnMouseButtonUp(MouseData mouseData) {
            if (MouseButtonUp != null) MouseButtonUp(this, new MouseEventArgs(mouseData));
        }

        public virtual void OnMouseHoverBegin(MouseData mouseData) {
            if (MouseHoverBegin != null) MouseHoverBegin(this, new MouseEventArgs(mouseData));
        }

        public virtual void OnMouseHoverEnd(MouseData mouseData) {
            if (MouseHoverEnd != null) MouseHoverEnd(this, new MouseEventArgs(mouseData));
        }

        public virtual void OnMouseDragBegin(Vec startPosition) {
            if (MouseDragBegin != null) MouseDragBegin(this, new MouseDragEventArgs(startPosition));
        }

        public virtual void OnMouseDragEnd(Vec endPosition) {
            if (MouseDragEnd != null) MouseDragEnd(this, new MouseDragEventArgs(endPosition));
        }

        private bool containsSchedule(Schedule schedule) {
            return Schedules.Contains(schedule) || ScheduleAddList.Contains(schedule);
        }
    }
}