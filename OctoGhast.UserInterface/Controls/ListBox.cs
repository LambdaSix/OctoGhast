using System;
using System.Collections.Generic;
using System.Linq;
using libtcod;
using OctoGhast.Spatial;
using OctoGhast.UserInterface.Core;
using OctoGhast.UserInterface.Core.Interface;
using OctoGhast.UserInterface.Core.Messages;
using OctoGhast.UserInterface.Templates;
using OctoGhast.UserInterface.Theme;

namespace OctoGhast.UserInterface.Controls
{
    /// <summary>
    /// Information about a ListBox.ItemSelected event.
    /// </summary>
    public class ListItemSelectedEventArgs : EventArgs
    {
        public int Index { get; private set; }

        public ListItemSelectedEventArgs(int index) {
            Index = index;
        }
    }

    /// <summary>
    /// Contains the label and tooltip text for each ListItem
    /// </summary>
    public class ListItemData
    {
        public string Label { get; set; }
        public string TooltipText { get; set; }

        public ListItemData(string label, string tooltipText) {
            Label = label;
            TooltipText = tooltipText;
        }
    }

    public class ListBoxTemplate : ControlTemplate
    {
        public IEnumerable<ListItemData> Items { get; set; }

        /// <summary>
        /// Horizontal alignment of item labels.
        /// </summary>
        public HAlign LabelAlignment { get; set; }

        /// <summary>
        /// Horizontal alignment of the title.
        /// </summary>
        public HAlign TitleAlignment { get; set; }

        /// <summary>
        /// Title string.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// List box width if larger than the calculated width.
        /// </summary>
        public int MinimumWidth { get; set; }

        /// <summary>
        /// Item index to initially select.
        /// </summary>
        public int InitialSelectedIndex { get; set; }

        public ListBoxTemplate() {
            Items = new List<ListItemData>();
            Title = String.Empty;
            LabelAlignment = HAlign.Left;
            TitleAlignment = HAlign.Left;
            HasFrameBorder = true;
        }

        public override Size CalculateSize() {
            if (AutoSizeOverride.Width > 0 && AutoSizeOverride.Height > 0)
                return AutoSizeOverride;

            int width = Title.Length;

            foreach (var i in Items) {
                if (String.IsNullOrWhiteSpace(i.Label))
                    i.Label = String.Empty;

                if (CanvasUtil.MeasureStr(i.Label) > width)
                    width = CanvasUtil.MeasureStr(i.Label);
            }

            width += 2;

            if (HasFrameBorder)
                width += 2;

            if (MinimumWidth > width)
                width = MinimumWidth;

            int height = Items.Count() + 1;
            if (HasFrameBorder)
                height += 3;

            return new Size(width, height);

        }
    }

    public class ListBox : ControlBase
    {
        public event EventHandler<ListItemSelectedEventArgs> ItemSelected;

        public HAlign LabelAlignment { get; set; }
        public HAlign TitleAlignment { get; set; }
        public string Title { get; private set; }
        public int CurrentSelection { get; protected set; }

        private List<ListItemData> Items;
        private int mouseOverIndex;
        private Rect titleRect;
        private Rect itemsRect;
        private int numberItemsDisplayed;

        public ListBox(ListBoxTemplate template) : base(template) {
            throw new NotImplementedException();
        }

        public string GetItemLabel(int index) {
            if (index < 0 || index >= Items.Count())
                throw new ArgumentOutOfRangeException("index");

            return Items.ElementAt(index).Label;
        }

        protected void DrawTitle() {
            if (!String.IsNullOrWhiteSpace(Title)) {
                Canvas.PrintStringAligned(titleRect.TopLeft, Title, TitleAlignment, VAlign.Center, new Size(Title.Length, 1));
            }

            if (HasFrame && Size.Width > 2 && Size.Height > 2) {
                int fY = titleRect.Bottom + 1;

                Canvas.SetDefaultPigment(DetermineFramePigment());
                Canvas.DrawHLine(1, fY, Size.Width - 2);
                Canvas.PrintChar(0, fY, (char) TCODSpecialCharacter.TeeEast);
                Canvas.PrintChar(Size.Width - 1, fY, (char) TCODSpecialCharacter.TeeWest);
            }
        }

        protected void DrawItems() {
            for (int i = 0; i < numberItemsDisplayed; i++) {
                DrawItem(i);
            }
        }

        protected void DrawItem(int index) {
            var item = Items.ElementAt(index);

            if (index == CurrentSelection) {
                Canvas.PrintStringAligned(itemsRect.TopLeft.X,
                    itemsRect.TopLeft.Y + index,
                    item.Label,
                    LabelAlignment,
                    itemsRect.Size.Y,
                    Pigments[PigmentType.ViewSelected]);

                Canvas.PrintChar(itemsRect.TopRight.X,
                    itemsRect.TopLeft.Y + index,
                    (char) TCODSpecialCharacter.ArrowWest,
                    Pigments[PigmentType.ViewSelected]);
            }
            else if (index == mouseOverIndex) {
                Canvas.PrintStringAligned(itemsRect.TopLeft.X,
                    itemsRect.TopLeft.Y + index,
                    item.Label,
                    LabelAlignment,
                    itemsRect.Size.Y,
                    Pigments[PigmentType.ViewHighlight]);
            }
            else {
                Canvas.PrintStringAligned(itemsRect.TopLeft.X,
                    itemsRect.TopLeft.Y + index,
                    item.Label,
                    LabelAlignment,
                    itemsRect.Size.Y,
                    Pigments[PigmentType.ViewNormal]);
            }
        }

        protected int GetItemAt(Vec pos) {
            var index = -1;

            if (itemsRect.Contains(pos)) {
                var i = pos.Y - itemsRect.Top;
                index = i;
            }

            if (index < 0 || index >= Items.Count()) {
                index = -1;
            }

            return index;
        }

        protected override void Redraw() {
            base.Redraw();

            DrawTitle();
            DrawItems();
        }

        public override void OnMouseMoved(MouseData mouseData) {
            base.OnMouseMoved(mouseData);

            var pos = ScreenToLocal(mouseData.Position);

            mouseOverIndex = GetItemAt(pos);

            if (mouseOverIndex != -1) {
                TooltipText = Items.ElementAt(mouseOverIndex).TooltipText;
            }
            else {
                TooltipText = null;
            }
        }

        public override void OnMouseButtonDown(MouseData mouseData) {
            base.OnMouseButtonDown(mouseData);

            if (mouseOverIndex != -1) {
                if (CurrentSelection != mouseOverIndex) {
                    CurrentSelection = mouseOverIndex;
                    OnItemSelected(CurrentSelection);
                }
            }
        }

        protected virtual void OnItemSelected(int index) {
            if (ItemSelected != null)
                ItemSelected(this, new ListItemSelectedEventArgs(index));
        }

        private void CalculateMetrics(ListBoxTemplate template) {
            int itemCount = Items.Count();
            int expandTitle = 0;

            int delta = Size.Height - itemCount - 1;
            if (template.HasFrameBorder)
                delta -= 3;

            numberItemsDisplayed = Items.Count;
            if (delta < 0)
                numberItemsDisplayed += delta;
            else if (delta > 0)
                expandTitle = delta;

            int titleWidth = Size.Width;
            int titleHeight = 1 + expandTitle;

            if (Title != "") {
                if (template.HasFrameBorder) {
                    titleRect = new Rect(Vec.Zero.Offset(1, 1), new Size(titleWidth - 2, titleHeight));
                }
                else {
                    titleRect = new Rect(Vec.Zero, new Size(titleWidth, titleHeight));
                }
            }

            int itemsWidth = Size.Width;
            int itemsHeight = numberItemsDisplayed;

            if (template.HasFrameBorder) {
                itemsRect = new Rect(titleRect.BottomLeft.Offset(0, 2), new Size(itemsWidth - 2, itemsHeight));
            }
            else {
                itemsRect = new Rect(titleRect.BottomLeft.Offset(0, 1), new Size(itemsWidth, itemsHeight));
            }
        }
    }
}