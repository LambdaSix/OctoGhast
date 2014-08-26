using System;
using libtcod;
using OctoGhast.Spatial;
using OctoGhast.UserInterface.Core;
using OctoGhast.UserInterface.Theme;

namespace OctoGhast.Framework
{
    public class GameInfo
    {
        public bool IsFullscreen { get; set; }
        public Size ScreenSize { get; set; }
        public string Title { get; set; }
        public string Font { get; set; }
        public TCODFontFlags FontFlags { get; set; }
        public PigmentMapping Pigments { get; set; }

        public GameInfo() {
            IsFullscreen = false;
            ScreenSize = new Size(80, 60);
            Title = "";
            Font = null;
            FontFlags = TCODFontFlags.LayoutAsciiInColumn;
            Pigments = new PigmentMapping();
        }
    }

    public class Game : IDisposable, IGame
    {
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

        public static Size ScreenSize {
            get { return new Size(TCODConsole.root.getWidth(), TCODConsole.root.getHeight()); }
        }

        public static Rect ScreenRectangle {
            get { return new Rect(Vec.Zero, ScreenSize); }
        }

        public static Window RootWindow { get; private set; }

        public Window CurrentWindow {
            get { return RootWindow; }
        }

        public Game() {
            IsQuitting = false;
        }

        public void Start(GameInfo info) {
            Setup(info);
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

        protected virtual void Setup(GameInfo info) {
            if (!String.IsNullOrWhiteSpace(info.Font))
                TCODConsole.setCustomFont(info.Font, (int) TCODFontFlags.LayoutTCOD | (int) TCODFontFlags.Grayscale);

            TCODConsole.initRoot(info.ScreenSize.Width, info.ScreenSize.Height, info.Title, info.IsFullscreen,
                TCODRendererType.SDL);

            TCODMouse.showCursor(true);

            if (SetupEventHandler != null)
                SetupEventHandler(this, EventArgs.Empty);

            Pigments = new PigmentMapping(null, info.Pigments);
        }

        protected virtual void Update() {
            if (UpdateEventHandler != null)
                UpdateEventHandler(this, EventArgs.Empty);

            var elapsed = TCODSystem.getElapsedMilli();
            RootWindow.OnTick();
            Input.Update(elapsed);
        }

        private int Run() {
            if (RootWindow == null) {
                var win = new Window(new WindowTemplate(ScreenSize));
                SetWindow(win);
            }

            while (!TCODConsole.isWindowClosed() && !IsQuitting) {
                Update();
                Draw();
            }

            return 0;
        }

        private void Draw() {
            RootWindow.OnDraw();
            TCODConsole.flush();
        }

        private bool _alreadyDisposed;

        ~Game() {
            Dispose(false);
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(true);
        }

        protected virtual void Dispose(bool isDisposing) {
            if (_alreadyDisposed)
                return;

            if (isDisposing) {
                if (RootWindow != null)
                    RootWindow.Dispose();
            }

            _alreadyDisposed = true;
        }
    }
}