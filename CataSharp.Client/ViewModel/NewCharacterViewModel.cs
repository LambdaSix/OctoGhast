using System.Collections.Generic;
using GalaSoft.MvvmLight;

namespace CataSharp.Client.ViewModel {
    public class StatisticValue {
        public string Name { get; set; }
        public int Value { get; set; }

        public StatisticValue(string name, int value) {
            Name = name;
            Value = Value;
        }
    }

    public class NewCharacterViewModel : ViewModelBase {
        private IEnumerable<StatisticValue> _statsSummary;
        private IEnumerable<StatisticValue> _skills;
        private IEnumerable<string> _traits;

        public int PointsLeft { get; set; } = 0;

        public IEnumerable<StatisticValue> StatsSummary =>
            _statsSummary ?? (_statsSummary = new List<StatisticValue>());

        public IEnumerable<string> Traits =>
            _traits ?? (_traits = new List<string>());

        public IEnumerable<StatisticValue> Skills =>
            _skills ?? (_skills = new List<StatisticValue>());

        public NewCharacterViewModel() {
            _statsSummary = new[]
            {
                new StatisticValue("Strength", 14),
                new StatisticValue("Dexterity", 7),
                new StatisticValue("Intelligence", 9),
                new StatisticValue("Perception", 13),
            };

            _traits = new[]
            {
                "Strong Stomach",
                "Addiction Resistant",
                "Indefatigable",
                "Accomplished Sleeper",
                "Packmule",
                "Light Step",
                "Shaolin Adept",
                "Inconspicuous"
            };

            _skills = new[]
            {
                new StatisticValue("Archery", 6),
                new StatisticValue("Dodging", 6),
                new StatisticValue("Survival", 6),
                new StatisticValue("Bashing Weapons", 4),
                new StatisticValue("Piercing Weapons", 4),
                new StatisticValue("Trapping", 4),
                new StatisticValue("Unarmed Combat", 4),
            };
        }
    }
}