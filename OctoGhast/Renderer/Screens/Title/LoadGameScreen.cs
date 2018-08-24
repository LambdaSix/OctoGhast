using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using OctoGhast.Spatial;
using OctoGhast.UserInterface.Controls;
using OctoGhast.UserInterface.Core;
using OctoGhast.UserInterface.Templates;

namespace OctoGhast.Renderer.Screens {
    public class LoadGameScreenModel : ModelBase{
        private WorldInfo _currentWorld;
        private List<WorldInfo> _worlds;

        public WorldInfo CurrentWorld
        {
            get => _currentWorld;
            set { _currentWorld = value;
                OnPropertyChanged(nameof(CurrentWorld));
            }
        }

        public List<WorldInfo> Worlds
        {
            get => _worlds;
            set { _worlds = value; OnPropertyChanged(nameof(Worlds)); }
        }
    }

    public class LoadGameScreen : ScreenBase {
        private LoadGameScreenModel Model { get; } = new LoadGameScreenModel();

        private Label worldName;
        private Label worldModsList;
        private Label worldCharCount;
        private Label worldGenDate;
        private Label worldAccessDate;

        /// <inheritdoc />
        public override void OnSettingUp() {
            base.OnSettingUp();

            Model.Worlds = LoadWorlds().ToList();
            Model.CurrentWorld = Model.Worlds[0];

            var windowSize = ParentWindow.ParentApplication.CurrentWindow.Size;
            var entireWidth = 70;

            var worldList_t = new ListBoxTemplate
            {
                Title = "Worlds",
                UpperLeftPos = new Vec(windowSize.Width / 2 - (entireWidth/2),0),
                HasFrameBorder = true,
                MinimumWidth = entireWidth,
                Items = RetrieveWorldList()
            };

            var panel_t = new PanelTemplate()
            {
                Title = "World Info",
                HasFrameBorder = true,
                Size = new Size(entireWidth, 10)
            };
            panel_t.AlignTo(LayoutDirection.South, worldList_t, 3);

            var anchor = panel_t.UpperLeftPos;

            var worldName_t = new LabelTemplate()
            {
                Label = " ",
                MinimumWidth = entireWidth,
                UpperLeftPos = anchor.Offset(1,1),
                Binding = new BindingTarget
                {
                    Target = () => Model.CurrentWorld.Name,
                    BindMode = BindingMode.OneWay,
                    Formatter = s => $"Name: {s}",
                },
            };

            var worldModList_t = new LabelTemplate()
            {
                Label = " ", MinimumWidth = entireWidth, HasFrameBorder = false,
                Binding = new BindingTarget
                {
                    Target = () => Model.CurrentWorld.Name,
                    BindMode = BindingMode.OneWay,
                    Formatter = s => $"Mods: {s}",
                },
            };
            worldModList_t.AlignTo(LayoutDirection.South, worldName_t, -2);

            var worldCharCount_t = new LabelTemplate()
            {
                Label = " ", MinimumWidth = entireWidth,
                Binding = new BindingTarget
                {
                    Target = () => Model.CurrentWorld.Characters,
                    BindMode = BindingMode.OneWay,
                    Formatter = s => $"Character Count: {s}",
                },
            };
            worldCharCount_t.AlignTo(LayoutDirection.South, worldModList_t, -2);

            var worldGenDate_t = new LabelTemplate()
            {
                Label = " ", MinimumWidth = entireWidth,
                Binding = new BindingTarget
                {
                    Target = () => Model.CurrentWorld.WorldGenTime,
                    BindMode = BindingMode.OneWay,
                    Formatter = s => $"Generated: {s}",
                },
            };
            worldGenDate_t.AlignTo(LayoutDirection.South, worldCharCount_t, -2);

            var worldAccessDate_t = new LabelTemplate()
            {
                Label = " ", MinimumWidth = entireWidth,
                Binding = new BindingTarget
                {
                    Target = () => Model.CurrentWorld.WorldAccessTime,
                    BindMode = BindingMode.OneWay,
                    Formatter = s => $"Last Accessed: {s}",
                },
            };
            worldAccessDate_t.AlignTo(LayoutDirection.South, worldGenDate_t, -2);

            var worldList = new ListBox(worldList_t);
            var worldInfo = new Panel(panel_t);
            worldName = new Label(worldName_t);
            worldModsList = new Label(worldModList_t);
            worldCharCount = new Label(worldCharCount_t);
            worldGenDate = new Label(worldGenDate_t);
            worldAccessDate = new Label(worldAccessDate_t);

            worldList.ItemSelected += (sender, args) => { LoadWorld(args.ListItemData.Label); };
            LoadWorld(Model.Worlds[0].Name);

            ParentWindow.AddControls(worldList, worldInfo, worldName,
                worldModsList, worldCharCount, worldGenDate, worldAccessDate);
        }

        private IEnumerable<ListItemData> RetrieveWorldList() {
            return Model.Worlds.Select(s => new ListItemData(s.Name, ""));
        }

        private WorldInfo GetWorldInfo(string name) {
            return Model.Worlds.Single(s => s.Name == name);
        }

        private void LoadWorld(string name) {
            Model.CurrentWorld = GetWorldInfo(name);
            Console.WriteLine($"Loading world {worldName}");
        }

        private IEnumerable<WorldInfo> LoadWorlds() {
            // TODO: Replace this with the actual world loading code.
            // foreach (var folder in Directory.Enumerate("Saves") {
            //   yield return WorldInfo.Load(folder);
            // }

            // Test code for the window.
            yield return new WorldInfo()
            {
                Name = "Bridgeport",
                Characters = 9,
                WorldGenTime = new DateTime(2018, 01, 01)
            };
            yield return new WorldInfo()
            {
                Name = "New State City",
                Characters = 1,
                WorldGenTime = new DateTime(2018, 06, 01)
            };
            yield return new WorldInfo()
            {
                Name = "Armatidge",
                Characters = 2,
                WorldGenTime = new DateTime(2018, 04, 01)
            };
            yield return new WorldInfo()
            {
                Name = "Newcastle",
                Characters = 8,
                WorldGenTime = new DateTime(2017, 04, 01)
            };
            yield return new WorldInfo()
            {
                Name = "Salt City",
                Characters = 4,
                WorldGenTime = new DateTime(2011, 04, 01)
            };
        }
    }

    public class WorldInfo : INotifyPropertyChanged {
        private DateTime _worldAccessTime = DateTime.Now.AddDays(-12);
        private DateTime _worldGenTime = DateTime.Now.AddDays(-24);
        private int _characters;
        private string _mods = "Core;WildLiving;MoreSurvivalTools;No_Fungus";
        private string _name;

        public string Name
        {
            get => _name;
            set { _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public string Mods
        {
            get => _mods;
            set { _mods = value;
                OnPropertyChanged(nameof(Mods));
            }
        }

        public DateTime WorldGenTime
        {
            get => _worldGenTime;
            set { _worldGenTime = value; OnPropertyChanged(nameof(WorldGenTime)); }
        }

        public DateTime WorldAccessTime
        {
            get => _worldAccessTime;
            set { _worldAccessTime = value; OnPropertyChanged(nameof(WorldAccessTime)); }
        }

        public int Characters
        {
            get => _characters;
            set { _characters = value; OnPropertyChanged(nameof(Characters)); }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}