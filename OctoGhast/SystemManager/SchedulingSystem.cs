using System.Collections.Generic;
using System.Linq;

namespace OctoGhast.SystemManager {

    public interface IScheduleable {
        int Time { get; }
    }

    /// <summary>
    /// This manager handles global time and ticking of entity updates.
    /// It does not handle game-time, that's <seealso cref="OctoGhast.Mechanics.ChronoSystem"/>
    /// </summary>
    public class SchedulingSystem {
        private readonly SortedDictionary<int, List<IScheduleable>> _schedulerMap =
            new SortedDictionary<int, List<IScheduleable>>();

        public int Time { get; private set; } = 0;

        public void Add(IScheduleable scheduleable) {
            int key = Time + scheduleable.Time;
            if (!_schedulerMap.ContainsKey(key))
            {
                _schedulerMap.Add(key, new List<IScheduleable>());
            }
            _schedulerMap[key].Add(scheduleable);
        }

        public void Remove(IScheduleable scheduleable) {
            KeyValuePair<int, List<IScheduleable>> scheduleableListFound
                = new KeyValuePair<int, List<IScheduleable>>(-1, null);

            foreach (var scheduleableList in _schedulerMap) {
                if (scheduleableList.Value.Contains(scheduleable)) {
                    scheduleableListFound = scheduleableList;
                    break;
                }
            }

            if (scheduleableListFound.Value != null) {
                scheduleableListFound.Value.Remove(scheduleable);
                if (scheduleableListFound.Value.Count <= 0) {
                    _schedulerMap.Remove(scheduleableListFound.Key);
                }
            }
        }

        public IScheduleable GetNext() {
            var firstScheduleGroup = _schedulerMap.First();
            var firstSchedule = firstScheduleGroup.Value.First();

            Remove(firstSchedule);
            Time = firstScheduleGroup.Key;
            return firstSchedule;
        }

        public void Clear() {
            Time = 0;
            _schedulerMap.Clear();
        }
    }
}