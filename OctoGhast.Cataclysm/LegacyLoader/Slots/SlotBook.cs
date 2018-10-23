using OctoGhast.Entity;

namespace OctoGhast.Cataclysm.LegacyLoader {
    public class SlotBook {
        public StringID<Skill> Skill = StringID<Skill>.NullId;
        public int Level { get; set; } = default;
        public int Req { get; set; } = default;
        public int Fun { get; set; } = default;
        public int Intel { get; set; } = default;

        /// <summary>
        /// How long in in minutes (10 turns) it takes to read.
        /// 'to read' means getting 1 skill-point.
        /// </summary>
        public int Time { get; set; } = 0;

        public int Chapters { get; set; } = 0;
    }
}