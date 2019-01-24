using InfiniMap;
using OctoGhast.Cataclysm.Items;
using OctoGhast.Cataclysm.LegacyLoader;
using OctoGhast.Entity;
using OctoGhast.Framework.Items.Actions;
using OctoGhast.Framework.Mobile;

namespace OctoGhast.Cataclysm.UseActions {
    /// <summary>
    /// Specialized version of UseAction for ItemType
    /// </summary>
    public class BaseUseAction : UseAction {
        // TODO: Unify this with the Furniture actions?

        /// <inheritdoc />
        public BaseUseAction(string type) : base(type) { }

        public virtual (bool success, string message) CanInvoke(Player player, BaseItem item, bool turnTick, WorldSpace position) {
            return CanInvoke<ItemType>(player, item, turnTick, position);
        }

        public virtual int Invoke(BaseCreature player, BaseItem item, bool turnTick, WorldSpace position) {
            return Invoke(player, item, turnTick, position);
        }
    }
}