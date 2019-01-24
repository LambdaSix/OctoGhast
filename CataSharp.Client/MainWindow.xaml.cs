using System;
using System.Collections.Generic;
using System.Linq;
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
using TextBlock = System.Windows.Controls.TextBlock;

namespace CataSharp.Client
{
    public class MenuItem
    {
        public string Name { get; set; }
        public string Window { get; set; }
        public MenuItem[] Subtree { get; set; }
    }

    public class NavigationTree {
        public static MenuItem[] NewGameMenu = new[]
        {
            new MenuItem {Name = "Custom Character", Window = "CreateCharacter"}
        };

        public static MenuItem[] MainMenu = new[]
        {
            new MenuItem {Name = "MOTD", Window = "MOTDScreen"},
            new MenuItem {Name = "New Game", Subtree = NewGameMenu}
        };
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

        private void UIElement_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            this.NewGamePanel.Visibility = Visibility.Visible;
            this.Content = new UserControls.CraftWindowControl();
        }

        void Setup() {
            UIElement CreateMenuItem(string name) {
                return new TextBlock()
                {
                    Foreground = Brushes.SlateGray,
                    Padding = new Thickness(20, 0, 20, 0),
                    Text = $"[{name}]",
                    TextEffects = new TextEffectCollection
                    {
                        new TextEffect()
                        {
                            PositionCount = 1, PositionStart = 1, Foreground = Brushes.AntiqueWhite
                        }
                    }
                };
            }

            var root = FindName("MainMenu") as StackPanel;
            foreach (var item in NavigationTree.MainMenu) {
                root.Children.Add(CreateMenuItem(item.Name));
            }
        }

        private TextBlock[] NewGameOrder => new[]
        {
            NewGame_Custom,
            NewGame_Preset,
            NewGame_Random,
            NewGame_Quickplay
        };

        private int CurrentIndex = 0;
        private string ControlListing { get; set; } = "MainControlOrder";

        private void MainWindow_OnKeyDown(object sender, KeyEventArgs e) {
            var stackPanel = FindName("MainMenu") as StackPanel;
            var controlOrder = stackPanel.Children.Cast<TextBlock>().ToArray();

            // HACK: I can already hear the 'this belongs in the viewmodel' complaints
            // But consider: The ViewModel gets no access to the visual tree, and we need that here :)
            if (e.Key == Key.Left) {
                controlOrder[CurrentIndex].FontWeight = FontWeights.Normal;

                CurrentIndex--;
                if (CurrentIndex >= controlOrder.Length)
                    CurrentIndex = 0;
                if (CurrentIndex < 0)
                    CurrentIndex = controlOrder.Length-1;

                controlOrder[CurrentIndex].Focus();
                controlOrder[CurrentIndex].FontWeight = FontWeights.Bold;
            }
            if (e.Key == Key.Right)
            {
                controlOrder[CurrentIndex].FontWeight = FontWeights.Normal;

                CurrentIndex++;
                if (CurrentIndex < 0)
                    CurrentIndex = controlOrder.Length-1;
                if (CurrentIndex >= controlOrder.Length)
                    CurrentIndex = 0;

                controlOrder[CurrentIndex].Focus();
                controlOrder[CurrentIndex].FontWeight = FontWeights.Bold;
            }

            /*
            if (e.Key == Key.Up) {
                if (MainControlOrder[CurrentIndex].Tag is string associatedControlGroup) {
                    var panel = this.FindName(associatedControlGroup) as StackPanel;
                    if (panel.Name == "NewGamePanel") {
                        panel.Visibility = Visibility.Visible;
                        ControlListing = nameof(NewGameOrder);
                    }
                }
            }
            */
        }
    }
}
