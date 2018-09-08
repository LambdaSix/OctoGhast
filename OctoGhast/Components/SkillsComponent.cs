using System.Collections.Generic;
using Capsicum.Interfaces;

namespace OctoGhast.Activities {
    public class SkillsComponent : IComponent {
        public Dictionary<string, int> Skills { get; set; }
    }
}