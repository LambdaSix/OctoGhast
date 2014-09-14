using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OctoGhast.Spatial;

namespace OctoGhast.DataStructures.Entity
{
    public interface IGlyph
    {
        int Icon { get; set; }
    }

    public class Glyph : IGlyph
    {
        public int Icon { get; set; }

        public Glyph(int icon) {
            Icon = icon;
        }
    }

    public interface IGameObject
    {
        Vec Position { get; }
        IGlyph Glyph { get; }
        string Name { get; }
    }

    public class GameObject : IGameObject
    {
        public Vec Position { get; protected set; }
        public IGlyph Glyph { get; protected set; }
        public IColor Color { get; protected set; }
        public string Name { get; protected set; }

        public GameObject(Vec position, IGlyph glyph, IColor color) {
            Position = position;
            Glyph = glyph;
            Color = color;
        }

        public GameObject(Vec position, int glyph, IColor color) : this(position, new Glyph(glyph), color) {
            
        }

        public GameObject(Vec position, int glyph, IColor color, string name) : this(position, new Glyph(glyph), color) {
            Name = name;
        }
    }
}
