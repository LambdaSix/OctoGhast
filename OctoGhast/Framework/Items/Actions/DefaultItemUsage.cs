using InfiniMap;
using OctoGhast.Entity;
using OctoGhast.Framework.Items.Actions;
using OctoGhast.Framework.Mobile;
using static OctoGhast.Translation.Translation;

namespace OctoGhast.Framework {
    [ItemUse("NULL")]
    public class DefaultItemUse : ItemUse<TemplateType> {
        /// <inheritdoc />
        public override int Use(BaseCreature player, RLObject<TemplateType> item, bool turnTick, WorldSpace position) {
            var msg = _($"You can't do anything interesting with your {item.GetName()}");
            player.SendMessage(msg);
            return 0;
        }
    }
}