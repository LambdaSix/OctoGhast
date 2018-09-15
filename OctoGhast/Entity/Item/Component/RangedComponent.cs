using Newtonsoft.Json.Linq;
using OctoGhast.Extensions;

namespace OctoGhast {
    public class RangedComponent : ItemComponent {
        public int ReloadNoiseVolume { get;set; }
        public string Skill { get; set; }
        public string Ammo { get; set; }
        public int RangedDamage { get; set; }
        public int Range { get; set; }
        public int Dispersion { get; set; }
        public int MagazineSize { get; set; }
        public int ReloadTime { get; set; }
        public int Durability { get; set; }


        public static RangedComponent Deserialize(JObject data) {
            return new RangedComponent()
            {
                ReloadNoiseVolume = data.GetValueOr("reload_noise_volume", 0),
                Skill = data.GetValueOr("skill", default(string)),
                Ammo = data.GetValueOr("ammo", default(string)),
                RangedDamage = data.GetValueOr("ranged_damage", 0),
                Range = data.GetValueOr("range", 0),
                Dispersion = data.GetValueOr("dispersion", 0),
                MagazineSize = data.GetValueOr("magazine_size", 0),
                ReloadTime = data.GetValueOr("reload_time", 0),
                Durability = data.GetValueOr("durability", 0),
            };
        }
    }
}