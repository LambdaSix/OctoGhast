using libtcod;

namespace OctoGhast
{
    public class Object
    {
        public int X { get; set; }
        public int Y { get; set; }
        public char Icon { get; set; }
        public TCODColor Color { get; set; }

        public Object(int x, int y, char icon, TCODColor color) {
            X = x;
            Y = y;
            Icon = icon;
            Color = color;
        }

        public void Move(int deltaX, int deltaY) {
            X += deltaX;
            Y += deltaY;
        }

        public void Draw(TCODConsole buffer) {
            buffer.setForegroundColor(Color);
            buffer.putChar(X, Y, Icon);
        }

        public void Clear(TCODConsole buffer) {
            buffer.putChar(X, Y, ' ');
        }
    }
}