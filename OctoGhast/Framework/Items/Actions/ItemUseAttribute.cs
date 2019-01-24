using System;

namespace OctoGhast.Framework {
    public class ItemUseAttribute : Attribute {
        public string UseName { get; }

        public ItemUseAttribute(string useName) {
            UseName = useName;
        }
    }
}