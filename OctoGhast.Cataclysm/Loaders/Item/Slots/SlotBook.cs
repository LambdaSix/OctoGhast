using OctoGhast.Cataclysm.Loaders.Item.Types;
using OctoGhast.Entity;
using OctoGhast.Framework;

namespace OctoGhast.Cataclysm.Loaders.Item.Slots {
    public class SlotBook {
        [LoaderInfo("skill")]
        public StringID<Skill> Skill { get; set; }

        [LoaderInfo("max_level")]
        public int MaxLevel { get; set; }

        [LoaderInfo("required_level")]
        public int RequiredLevel { get; set; }

        [LoaderInfo("fun")]
        public int Fun { get; set; }

        [LoaderInfo("intelligence")]
        public int Intel { get; set; }

        /// <summary>
        /// How long in in minutes (10 turns) it takes to read.
        /// 'to read' means getting 1 skill-point.
        /// </summary>
        [LoaderInfo("time")]
        public TimeDuration Time { get; set; }

        [LoaderInfo("chapters")]
        public int Chapters { get; set; }
    }
}