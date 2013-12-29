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

	public class Player : Mobile, IPlayer
	{
        public Player(Vec position, char glyph, TCODColor color) : base(position, glyph, color, "Player") {}

        public override bool Attack(IMobile other) {
            Console.WriteLine("Bumped into {0}, but I have no weapon :(", other.Name);
            return true;
        }
    }
}