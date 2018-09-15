namespace OctoGhast {
    public class ItemRanged : Item {
        public RangedComponent Component => GetComponent<RangedComponent>();
        private ItemRanged() { }
    }
}