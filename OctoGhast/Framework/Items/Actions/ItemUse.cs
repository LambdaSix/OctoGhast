using InfiniMap;
using OctoGhast.Framework.Mobile;

using static OctoGhast.Translation.Translation;

namespace OctoGhast.Framework.Items.Actions {
    /// <summary>
    /// Defines a base 'iuse'/'use_action' function.
    /// These are intended as specific actions on items that require no json parametrization.
    ///
    /// When supplied with a function delegate, it is wrapped in a UseAction for consumption.
    /// </summary>
    public class ItemUse<T> where T : TemplateType
    {
        public UseAction Action { get; set; }

        public ItemUse() { }

        public ItemUse(UseAction f)
        {
            Action = f;
        }

        public virtual int Use(UseActionData action, BaseCreature player, RLObject<T> item, bool turnTick, WorldSpace position)
        {
            return -1;
        }

        public (bool success, string message) CanInvoke(BaseCreature player, RLObject<T> item, bool turnTick, WorldSpace position)
        {
            if (Action == null)
                return (false, _($"You can't do anything interesting with your {item.GetName()}"));

            return Action.CanInvoke(Action.Data, player, item, turnTick, position);
        }

        public int Invoke(BaseCreature player, RLObject<T> item, bool turnTick, WorldSpace position)
        {
            return Action.Invoke(Action.Data, player, item, turnTick, position);
        }
    }
}