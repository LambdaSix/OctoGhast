using Capsicum.Interfaces;

namespace OctoGhast.Activities {
    public class ReadableComponent : IComponent {
        public string ReadableId { get; set; }
        public int Complexity { get; }
        public bool Readable { get; }
        public int PageCount { get; }
        public int HasRecipes { get; }
    }
}