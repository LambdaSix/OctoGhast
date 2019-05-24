using InfiniMap;
using OctoGhast.Framework.Mobile;

namespace OctoGhast.Framework.Items.Actions {
    public delegate int ItemUseDelegate(UseActionData actionData, BaseCreature player, RLObject<TemplateType> item, bool turnTick, WorldSpace position);
}