using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using libtcod;
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

        void Draw(TCODConsole buffer, Vec drawPos);
    }

    public class GameObject : IGameObject
    {
        public Vec Position { get; protected set; }
        public IGlyph Glyph { get; protected set; }
        public TCODColor Color { get; protected set; }

        public GameObject(Vec position, IGlyph glyph, TCODColor color) {
            Position = position;
            Glyph = glyph;
            Color = color;
        }

        public GameObject(Vec position, int glyph, TCODColor color) : this(position, new Glyph(glyph), color) {
            
        }

        public virtual void Draw(TCODConsole buffer, Vec drawPosition)
        {
            buffer.putCharEx(drawPosition.X, drawPosition.Y, Glyph.Icon, Color, TCODColor.black);
        }
    }
}
