using System;
using System.Collections.Generic;
using OctoGhast.Entity;
using OctoGhast.Framework;

namespace OctoGhast {
    [Flags][Obsolete("Use StringID<BodyPartInfo> instead")]
    public enum BodyPart {
        Torso,
        Head,
        Eyes,
        Mouth,
        ArmLeft,
        ArmRight,
        HandLeft,
        HandRight,
        LegLeft,
        LeftRight,
        FootLeft,
        FootRight
    }

    // TODO: Assemble this as a full type, not a bunch of strings.
    public class BodyPartInfo {
        [LoaderInfo("id")]
        public string Id { get; set; }

        [LoaderInfo("name")]
        public string Name { get; set; }

        [LoaderInfo("heading_singular")]
        public string HeadingSingular { get; set; }

        [LoaderInfo("heading_plural")]
        public string HeadingPlural { get; set; }

        [LoaderInfo("hp_bar_ui_text")]
        public string InterfaceText { get; set; }

        [LoaderInfo("encumbrance_test")]
        public string EncumbranceText { get; set; }

        [LoaderInfo("main_part")]
        public StringID<BodyPartInfo> ParentPart { get; set; }

        [LoaderInfo("opposite_part")]
        public StringID<BodyPartInfo> OppositePart { get; set; }

        [LoaderInfo("hit_size")]
        public int HitSize { get; set; }

        [LoaderInfo("hit_size_relative")] // ??
        public IEnumerable<double> RelativeHitSizes { get; set; }

        [LoaderInfo("hit_difficulty")]
        public double HitDifficulty { get; set; }
        
        [LoaderInfo("side")]
        public StringID<EquipmentSide> Side { get; set; }
    }
}