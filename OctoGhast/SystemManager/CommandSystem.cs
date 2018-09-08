using OctoGhast.Spatial;
using OctoGhast.World;

namespace OctoGhast.SystemManager {
    public enum Direction
    {
        None = 0,
        DownLeft = 1,
        Down = 2,
        DownRight = 3,
        Left = 4,
        Center = 5,
        Right = 6,
        UpLeft = 7,
        Up = 8,
        UpRight = 9
    }

    public class CommandSystem {        
            // Return value is true if the player was able to move
            // false when the player couldn't move, such as trying to move into a wall
            public bool MovePlayer(WorldInstance game, Direction direction)
            {
                int x = game.Player.Position.X;
                int y = game.Player.Position.Y;

                switch (direction)
                {
                    case Direction.Up:
                    {
                        y = game.Player.Position.Y - 1;
                        break;
                    }
                    case Direction.Down:
                    {
                        y = game.Player.Position.Y + 1;
                        break;
                    }
                    case Direction.Left:
                    {
                        x = game.Player.Position.X - 1;
                        break;
                    }
                    case Direction.Right:
                    {
                        x = game.Player.Position.X + 1;
                        break;
                    }
                    default:
                    {
                        return false;
                    }
                }

                if (game.MoveEntity(game.Player, new Vec(x, y)))
                {
                    return true;
                }

                return false;
            }
    }
}