using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using OctoGhast.Spatial;
using OctoGhast.UserInterface.Core;
using OctoGhast.UserInterface.Theme;
using RenderLike;
using Color = Microsoft.Xna.Framework.Color;

namespace OctoGhast.Framework
{
    public class GameInfo
    {
        public bool IsFullscreen { get; set; }
        public Size ScreenSize { get; set; }
        public string Title { get; set; }
        public string Font { get; set; }
        public PigmentMapping Pigments { get; set; }
        public FontLayout FontLayout { get; set; }
        public FontType FontType { get; set; }

        public GameInfo() {
            IsFullscreen = false;
            ScreenSize = new Size(80, 60);
            Title = "";
            Font = null;
            Pigments = new PigmentMapping();
            FontLayout = FontLayout.TCOD;
            FontType = FontType.GreyscaleAA;
        }
    }

    public class Game : Microsoft.Xna.Framework.Game, IGame
    {
        public GameInfo Info { get; set; }

        public static Font CurrentFont { get; set; }

        private GraphicsDeviceManager Graphics { get; set; }
        private SpriteBatch SpriteBatcher { get; set; }
        private RLConsole Console { get; set; }

        public static readonly FrameCounter FrameCounter = new FrameCounter();

        private int ConWidth {
            get { return ScreenSize.Width; }
        }

        private int ConHeight {
            get { return ScreenSize.Height; }
        }

        /// <summary>
        /// Raised when the application sets up.
        /// This is raised after TCOD is initialized, so any code reliant on TCOD
        /// can be used within this event.
        /// This event is for non-standard usage, as most code will subclass Game
        /// and override Setup()
        /// </summary>
        public event EventHandler SetupEventHandler;

        /// <summary>
        /// Raised each iteration of the main application loop.
        /// This event is for non-standard usage, as most code will subclass Game
        /// and override Update()
        /// </summary>
        public event EventHandler UpdateEventHandler;

        public bool IsQuitting { get; set; }
        public PigmentMapping Pigments { get; protected set; }

        public InputManager Input { get; private set; }

        public static Size ScreenSize { get; set; }

        public static Rect ScreenRectangle {
            get { return new Rect(Vec.Zero, ScreenSize); }
        }

        public static Window RootWindow { get; private set; }

        public Window CurrentWindow {
            get { return RootWindow; }
        }

        public Game(GameInfo info) {
            Info = info;
            IsQuitting = false;
            ScreenSize = info.ScreenSize;

            Graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            // TODO: Config
            IsFixedTimeStep = true;
            Graphics.SynchronizeWithVerticalRetrace = true;
        }

        public void Start(GameInfo info) {
            Run();
            RootWindow.OnQuitting();
        }

        public void SetWindow(Window win) {
            if (win == null)
                throw new ArgumentNullException("win");

            Input = new InputManager(win);
            RootWindow = win;
            win.Pigments = new PigmentMapping(Pigments, win.PigmentOverrides);

            if (!win.Initialized)
                win.OnSettingUp();
        }

        protected override void LoadContent() {
            base.LoadContent();

            SpriteBatcher = new SpriteBatch(GraphicsDevice);

            Texture2D fontTexture = Content.Load<Texture2D>(Info.Font);
            CurrentFont = Font.CreateFromTexture(fontTexture, Info.FontLayout, Info.FontType);

            Console = new RLConsole(GraphicsDevice, CurrentFont, ConWidth, ConHeight);
            Config.RootConsoleFunc = () => Console;

            Graphics.PreferredBackBufferWidth = ConWidth*CurrentFont.CharacterWidth;
            Graphics.PreferredBackBufferHeight = ConHeight*CurrentFont.CharacterHeight;
            Graphics.ApplyChanges();

            Console.RootSurface.Clear();

            Setup(Info);
        }

        protected virtual void Setup(GameInfo info) {
            if (SetupEventHandler != null)
                SetupEventHandler(this, EventArgs.Empty);

            Pigments = new PigmentMapping(null, info.Pigments);
        }

        protected override void Update(GameTime gameTime) {
            FrameCounter.Update((float) gameTime.ElapsedGameTime.TotalSeconds);

            if (UpdateEventHandler != null)
                UpdateEventHandler(this, EventArgs.Empty);

            RootWindow.OnTick(gameTime);
            Input.Update(gameTime);

            base.Update(gameTime);
        }

        protected override void BeginRun() {
            base.BeginRun();

            if (RootWindow == null) {
                var win = new Window(new WindowTemplate(ScreenSize));
                SetWindow(win);
            }
        }

        protected override void Draw(GameTime gameTime) {
            Console.RootSurface.Clear();

            Draw();

            base.Draw(gameTime);
        }

        private void Draw() {
            RootWindow.OnDraw();

            var render = Console.Flush();

            SpriteBatcher.Begin();
            {
                SpriteBatcher.Draw(render, Vector2.Zero, Color.White);
            }
            SpriteBatcher.End();
        }

        private bool _alreadyDisposed;

        ~Game() {
            Dispose(false);
        }

        public new void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected new virtual void Dispose(bool isDisposing) {
            if (_alreadyDisposed)
                return;

            if (isDisposing) {
                if (RootWindow != null)
                    RootWindow.Dispose();
            }

            _alreadyDisposed = true;
        }
    }

    public class FrameCounter
    {
        public long TotalFrames { get; private set; }
        public float TotalSeconds { get; private set; }

        public float AverageFramesPerSecond { get; private set; }
        public float CurrentFramesPerSecond { get; private set; }

        public const int MaximumSamples = 100;

        private Queue<float> _sampleBuffer = new Queue<float>();

        public void Update(float deltaTime) {
            CurrentFramesPerSecond = 1.0f/deltaTime;

            _sampleBuffer.Enqueue(CurrentFramesPerSecond);

            if (_sampleBuffer.Count > MaximumSamples) {
                _sampleBuffer.Dequeue();
                AverageFramesPerSecond = _sampleBuffer.Average(i => i);
            }
            else {
                AverageFramesPerSecond = CurrentFramesPerSecond;
            }

            TotalFrames++;
            TotalSeconds += deltaTime;
        }
    }
}