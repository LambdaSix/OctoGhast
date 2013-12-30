using System.Collections.Generic;
using System.Linq;
using libtcod;
using OctoGhast.DataStructures.Entity;
using OctoGhast.DataStructures.Map;
using OctoGhast.Entity.Behaviours;
using OctoGhast.Spatial;

namespace OctoGhast.Entity
{
    public interface IMobile : IGameObject
    {
        bool MoveTo(Vec position);
        bool Attack(IMobile other);

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
        protected ICollection<IEntityBehaviour> Behaviours { get; set; }

        public Mobile(Vec position, char glyph, TCODColor color, string name) : base(position, glyph, color, name) {
            
        }

        public virtual bool Attack(IMobile other) {
            // TODO: Should we allow multiple attack behaviours per turn?
            foreach (var behaviour in Behaviours.OfType<IAttackingEntityBehaviour>()) {
                behaviour.Attack(other);
            }
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