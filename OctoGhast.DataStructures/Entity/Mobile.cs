using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using libtcod;
using OctoGhast.DataStructures.Map;
using OctoGhast.Spatial;

namespace OctoGhast.DataStructures.Entity
{
    public interface IMobile : IGameObject
    {
        bool MoveTo(Vec position);
    }

    public class Mobile : GameObject, IMobile
    {
        public Mobile(Vec position, char glyph, TCODColor color) : base(position, glyph, color) {
            
        }

        private bool CanWalk(Vec position, GameMap gameMap, IEnumerable<Mobile> mobiles) {
            if (isWall(gameMap, position)) {
                return false;
            }

            var canWalk = mobiles.All(actor => actor.Position != position);

            return canWalk;
        }

        private bool isWall(GameMap map, Vec position) {
            return !map.IsWalkable(position);
        }

        public bool MoveTo(Vec pos) {
            return true;
        }

        public bool MoveTo(Vec position, GameMap gameMap, IEnumerable<Mobile> mobiles)
        {
            if (CanWalk(position, gameMap, mobiles))
            {
                Position = position;
                return true;
            }
            return false;
        }
    }
}