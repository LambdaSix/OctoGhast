using System.Collections.Generic;
using System.Linq;
using libtcod;
using OctoGhast.DataStructures.Entity;
using OctoGhast.DataStructures.Map;
using OctoGhast.Spatial;

namespace OctoGhast.Entity
{
    public interface IMobile : IGameObject
    {
        bool MoveTo(Vec position);
        void Attack(IMobile other);

        /// <summary>
        /// Attempt to move the current mobile.
        /// </summary>
        /// <param name="position">Location to move to</param>
        /// <param name="gameMap">The world map being navigated</param>
        /// <param name="mobiles">List of mobiles to check for collision against</param>
        /// <returns>True if could move, false if couldn't</returns>
        bool MoveTo(Vec position, IGameMap gameMap, IEnumerable<IMobile> mobiles);
    }

    public class Mobile : GameObject, IMobile
    {
        public virtual IMobile Combatant { get; set; }

        public Mobile(Vec position, char glyph, TCODColor color, string name) : base(position, glyph, color, name) {
            
        }

        public virtual void Attack(IMobile other) {
            if (CanAttack(other)) {
                Combatant = other;
            }
        }

        protected virtual bool CanAttack(IMobile other) {
            return true;
        }        

        public virtual bool IsInRange(IMobile other, int distance) {
            return Vec.IsDistanceWithin(other.Position, Position, distance);
        }

        private bool CanWalk(Vec position, IGameMap gameMap, IEnumerable<IMobile> mobiles) {
            if (isWall(gameMap, position)) {
                return false;
            }

            var collidedWith = mobiles.FirstOrDefault(actor => actor.Position == position);

            var canWalk = collidedWith == null;

            if (!canWalk) {
                Attack(collidedWith);
            }

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