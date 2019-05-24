using InfiniMap;
using OctoGhast.Cataclysm.Items;
using OctoGhast.Cataclysm.LegacyLoader;
using OctoGhast.Cataclysm.Loaders.Item;
using OctoGhast.Entity;
using OctoGhast.Framework.Items.Actions;
using OctoGhast.Framework.Mobile;

namespace OctoGhast.Cataclysm.UseActions {

    /// <summary>
    /// Specialized version of UseAction for ItemType
    /// </summary>
    public class ItemUseAction : UseAction {
        // TODO: Unify this with the Furniture actions?

        /// <inheritdoc />
        public ItemUseAction(string type) : base(type) { }

        public virtual (bool success, string message) CanInvoke(UseActionData actionData, Player player, BaseItem item, bool turnTick, WorldSpace position) {
            return CanInvoke<ItemType>(actionData, player, item, turnTick, position);
        }

        public virtual int Invoke(UseActionData actionData, BaseCreature player, BaseItem item, bool turnTick, WorldSpace position) {
            return base.Invoke(actionData, player, item, turnTick, position);
        }
    }
}