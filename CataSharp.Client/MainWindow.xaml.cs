using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CataSharp.Client.UserControls;
using TextBlock = System.Windows.Controls.TextBlock;

namespace CataSharp.Client
{
    public class MenuItem
    {
        public string Name { get; set; }
        public UserControl Window { get; set; }
        public MenuCollection SubMenu { get; set; }
        public MenuCollection Parent { get; set; }
        public Action Action { get; set; }
    }

    public class MenuCollection : IEnumerable<MenuItem> {
        public ICollection<MenuItem> Items { get; set; } = new List<MenuItem>();

        public void AddSubmenu(string name, MenuCollection menu) {
            Items.Add(new MenuItem {Name = name, Parent = this, SubMenu = menu});
        }

        public void AddWindow(string name, UserControl userControl) {
            Items.Add(new MenuItem {Name = name, Parent = this, Window = userControl});
        }

        public void AddAction(string name, Action action) {
            Items.Add(new MenuItem{Name = name, Action = action});
        }


        public IEnumerator<MenuItem> GetEnumerator() {
            return Items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return ((IEnumerable) Items).GetEnumerator();
        }
    }

    public class NavigationTree {
        public MenuCollection MainMenu;

        public NavigationTree() {
            MainMenu = new MenuCollection();

            MainMenu.AddWindow("MOTD", null);
            MainMenu.AddSubmenu("New Game", GetNewGameMenu());

            MainMenu.AddWindow("Settings", null);
            MainMenu.AddAction("Quit", () => Environment.Exit(0));
        }

        private MenuCollection GetNewGameMenu() {
            var newGame = new MenuCollection();
            newGame.AddWindow("Custom Character", new NewCharacter() );
            newGame.AddSubmenu("Presets", new MenuCollection
            {
                Items = new[]
                {
                    new MenuItem {Name = "Survivor"},
                    new MenuItem {Name = "Nerd"},
                    new MenuItem {Name = "Military Veteran"},
                }
            });

            return newGame;
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow()
        {
            InitializeComponent();
            Setup();
        }

        void Setup() {
            NavTree = new NavigationTree();
            SetupMenu(NavTree.MainMenu);
        }

        public NavigationTree NavTree { get; set; }

        public MenuCollection CurrentMenu { get; set; }
        public MenuCollection ParentMenu { get; set; }

        private void SetupMenu(MenuCollection menu) {
            CurrentMenu = menu;

            var root = FindName("MainMenu") as StackPanel;
            root?.Children.Clear();
            foreach (var item in menu) {
                root?.Children.Add(CreateMenuItem(item.Name, item));
            }

            _currentIndex = 0;
        }

        private UIElement CreateMenuItem(string name, MenuItem item) {
            var tb = new TextBlock()
            {
                Foreground = Brushes.SlateGray,
                Padding = new Thickness(20, 0, 20, 0),
                Text = $"[{name}]",
                Tag = item,
                TextEffects = new TextEffectCollection {new TextEffect() {PositionCount = 1, PositionStart = 1, Foreground = Brushes.AntiqueWhite}}
            };
            
            if (item.)
        }

        private int _currentIndex = 0;

        private void MainWindow_OnKeyDown(object sender, KeyEventArgs e) {
            var stackPanel = FindName("MainMenu") as StackPanel;
            var controlOrder = stackPanel.Children.Cast<TextBlock>().ToArray();

            // HACK: I can already hear the 'this belongs in the viewmodel' complaints
            // But consider: The ViewModel gets no access to the visual tree, and we need that here :)
            if (e.Key == Key.Left) {
                controlOrder[_currentIndex].FontWeight = FontWeights.Normal;

                _currentIndex--;
                if (_currentIndex >= controlOrder.Length)
                    _currentIndex = 0;
                if (_currentIndex < 0)
                    _currentIndex = controlOrder.Length-1;

                controlOrder[_currentIndex].Focus();
                controlOrder[_currentIndex].FontWeight = FontWeights.Bold;
            }

            if (e.Key == Key.Right) {
                controlOrder[_currentIndex].FontWeight = FontWeights.Normal;

                _currentIndex++;
                if (_currentIndex < 0)
                    _currentIndex = controlOrder.Length - 1;
                if (_currentIndex >= controlOrder.Length)
                    _currentIndex = 0;

                controlOrder[_currentIndex].Focus();
                controlOrder[_currentIndex].FontWeight = FontWeights.Bold;
            }

            if (e.Key == Key.Enter || e.Key == Key.Down) {
                MenuItemActivate(controlOrder[_currentIndex]);
            }

            if (e.Key == Key.Up) {
                if (CurrentMenu == ParentMenu)
                    return;

                if (ParentMenu != null)
                    SetupMenu(ParentMenu);
            }
        }

        private void MenuItemActivate(TextBlock block) {
            if (block.Tag is MenuItem item) {
                var hasSubMenu = item.SubMenu?.Any() ?? false;
                if (hasSubMenu) {
                    ParentMenu = CurrentMenu;
                    SetupMenu(item.SubMenu);
                    _currentIndex = 0;
                }


                if (item.Window != null) {
                    var controlHost = FindName("ControlHost") as StackPanel;
                    controlHost?.Children.Add(item.Window);
                    controlHost?.Focus();
                    item.Window.Visibility = Visibility.Visible;
                }
            }
        }
    }
}
