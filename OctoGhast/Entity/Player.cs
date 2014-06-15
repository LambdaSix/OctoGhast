using System;
using libtcod;
using OctoGhast.DataStructures.Entity;
using OctoGhast.Renderer;
using OctoGhast.Spatial;

namespace OctoGhast.Entity
{
    public interface IPlayer : IMobile
    {
        // TODO: Player specific things. (If any?)
    }

    public interface IWeapon
    {
        int Range { get;  }
        int Damage { get;  }
        int Durability { get;  }
        string Name { get; }
    }

    public class Dagger : IWeapon
    {
        public int Range { get { return 1; }  }
        public int Damage { get { return 8; } }
        public int Durability { get { return 4; } }

        public string Name { get { return "Dagger"; }}
    }

    public class Player : Mobile, IPlayer
    {
        public IWeapon ActiveWeapon { get; set; }

        public Player(Vec position, char glyph, TCODColor color) : base(position, glyph, color, "Player") {
            ActiveWeapon = new Dagger();
        }

        public override void Attack(IMobile other)
        {
            if (IsInRange(other, ActiveWeapon.Range)) {
                Console.WriteLine("You strike the {0} for {1} points of damage with your {2}!", other.Name, ActiveWeapon.Damage, ActiveWeapon.Name);
            }
        }
    }
}