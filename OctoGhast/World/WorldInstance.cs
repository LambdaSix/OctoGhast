using System;
using InfiniMap;
using OctoGhast.DataStructures.Lighting;
using OctoGhast.DataStructures.Map;
using OctoGhast.DataStructures.Renderer;
using OctoGhast.Entity;
using OctoGhast.Renderer.View;
using OctoGhast.Spatial;
using OctoGhast.SystemManager;
using OctoGhast.UserInterface.Core;

namespace OctoGhast.World {
    public class WorldInstance {
        private readonly int _screenHeight;

        private readonly int _screenWidth;
        // The world instance
        // Should contain references to all game systems/managers/schedulers
        // World tick isn't related to UI draw tick

        // Default to a 16x16x32 chunk alignment for now
        private Map2D<ITile> _map { get; } = new Map2D<ITile>(16, 16);

        //public Dictionary<SystemPriority, ISystem> Systems { get; } = new Dictionary<SystemPriority, ISystem>();

        public IPlayer Player { get; }
        public CommandSystem Commands { get; set; }
        public MessageSystem Messages { get; set; }

        public Map2D<ITile> Map => _map;
        public ICamera Camera { get; }

        public WorldInstance(IPlayer player, int screenHeight, int screenWidth) {
            _screenHeight = screenHeight;
            _screenWidth = screenWidth;
            Player = player;
            // Initialize all required systems.
            SetupSystems();
            SetupMechanics();
        }

        public void SetupSystems() {
            //Systems.Add(SystemPriority.Low, new ActivitySystem());
            Messages = new MessageSystem();
            Messages.Add("The rogue arrives on level 1");
        }

        public void SetupMechanics() {

        }

        public void Tick(UInt64 gameTime) {

        }


        public bool MoveEntity(IMobile entity, Vec newPosition) {
            throw new NotImplementedException();
        }

        // Returns true when able to place the Actor on the cell or false otherwise
        public bool SetActorPosition(Mobile actor, int x, int y)
        {
            // Only allow actor placement if the cell is walkable
            if (Map[x, y].IsWalkable)
            {
                // The cell the actor was previously on is now walkable
                SetIsWalkable(actor.Position.X, actor.Position.Y, true);
                // Update the actor's position
                actor.MoveTo(new Vec(x, y));

                // The new cell the actor is on is now not walkable
                SetIsWalkable(actor.Position.X, actor.Position.Y, false);
                // Don't forget to update the field of view if we just repositioned the player
                if (actor is Player)
                {
                    UpdatePlayerFieldOfView();
                }
                return true;
            }
            return false;
        }

        // A helper method for setting the IsWalkable property on a Cell
        public void SetIsWalkable(int x, int y, bool isWalkable)
        {
            ITile cell = Map[x, y];
            cell.IsTransparent = true;
            cell.IsWalkable = true;
            cell.IsExplored = true;
        }

        public bool IsExplored(Vec position)
        {
            return _map[position.X, position.Y].IsExplored;
        }

        public bool IsWalkable(Vec position)
        {
            return _map[position.X, position.Y].IsWalkable;
        }

        public bool IsOpaque(Vec position)
        {
            return !_map[position.X, position.Y].IsTransparent;
        }

        public ITile this[int x, int y]
        {
            get { return _map[x, y]; }
            set { _map[x, y] = value; }
        }

        public ITile this[Vec pos]
        {
            get { return _map[pos.X, pos.Y]; }
            set { _map[pos.X, pos.Y] = value; }
        }

        public void UpdatePlayerFieldOfView() {
            // Compute the field-of-view based on the player's location and awareness
            CalculateFov(Camera.ViewFrustum.Center, 8, (x, y) => new Vec(x, y).ToView(Camera.ViewFrustum));
        }

        public LightMap<TileLightInfo> CalculateFov(Vec viewCenter, int lightRadius, Func<int, int, Vec> translateFunc)
        {
            var lightMap = new LightMap<TileLightInfo>(_screenHeight, _screenWidth);

            // TODO: Loop a list of lights, calculate the FOV for each light then mix it's colour into the tile.

            ShadowCaster.ComputeFieldOfViewWithShadowCasting(viewCenter.X, viewCenter.Y, lightRadius,
                (x, y) => IsOpaque(new Vec(x, y)),
                (x, y) => {
                    var screenPos = translateFunc(x, y);
                    lightMap[screenPos].IsLit = true;
                    lightMap[screenPos].LightColor = new Color(128, 128, 128);
                });
            return lightMap;
        }
    }
}