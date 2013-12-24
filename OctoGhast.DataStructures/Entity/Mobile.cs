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

        private bool CanWalk(Vec position, IGameMap gameMap, IEnumerable<IMobile> mobiles) {
            if (isWall(gameMap, position)) {
                return false;
            }

            var canWalk = mobiles.All(actor => actor.Position != position);

            return canWalk;
        }

        private bool isWall(IGameMap map, Vec position) {
            return !map.IsWalkable(position);
        }

        public bool MoveTo(Vec pos) {
            return true;
        }

        /// <summary>
        /// Attempt to move the current mobile.
        /// </summary>
        /// <param name="position">Location to move to</param>
        /// <param name="gameMap">The world map being navigated</param>
        /// <param name="mobiles">List of mobiles to check for collision against</param>
        /// <returns>True if could move, false if couldn't</returns>
        public bool MoveTo(Vec position, IGameMap gameMap, IEnumerable<IMobile> mobiles)
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