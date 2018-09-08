using System.Collections.Generic;
using Capsicum.Interfaces;

namespace OctoGhast.Activities {
    public class ButcherableComponent : IComponent {
        public Dictionary<string,int> WeightedResults { get; set; }

        public ButcherStage Stage { get; set; }
    }
}