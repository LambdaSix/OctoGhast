using System;
using InfiniMap;
using OctoGhast.Entity;
using static OctoGhast.Translation.Translation;

namespace OctoGhast.Framework {
    public abstract class ItemUse {
        public abstract int Use(Player player, Item item, bool turnTick, WorldSpace position);
    }

    [ItemUse("NULL")]
    public class DefaultItemUse : ItemUse {
        /// <inheritdoc />
        public override int Use(Player player, Item item, bool turnTick, WorldSpace position) {
            var msg = _($"You can't do anything interesting with your {item.GetName().Translated}");
            return 0;
        }
    }

    public class ItemUseAttribute : Attribute {
        public string UseName { get; }

        public ItemUseAttribute(string useName) {
            UseName = useName;
        }
    }
}