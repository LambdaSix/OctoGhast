using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.InteropServices.ComTypes;
using OctoGhast.Spatial;
using OctoGhast.UserInterface.Core;
using OctoGhast.UserInterface.Core.Interface;
using OctoGhast.UserInterface.Core.Messages;
using OctoGhast.UserInterface.Templates;
using OctoGhast.UserInterface.Theme;

namespace OctoGhast.UserInterface.Controls
{
    public class MenuItemSelectedEventArgs : EventArgs
    {
        public int Index { get; set; }

        public MenuItemSelectedEventArgs(int index) {
            Index = index;
        }
    }

    public class MenuItemData
    {
        public string Label { get; set; }
        public string ToolTip { get; set; }

        public MenuItemData(string label, string toolTip = null) {
            Label = label;
            ToolTip = toolTip;
        }
    }

    public class MenuTemplate : ControlTemplate
    {
        public IEnumerable<MenuItemData> Items { get; set; }
        public HAlign LabelAlignment { get; set; }
        
        public MenuTemplate() {
            
        }

        public override Size CalculateSize() {
            if (AutoSizeOverride.Width > 1 && AutoSizeOverride.Height > 2)
                return AutoSizeOverride;

            var width = Items.Max(label => CanvasUtil.MeasureStr(label.Label ?? String.Empty));
            var height = Items.Count();

            if (HasFrameBorder) {
                width += 2;
                height += 2;
            }

            return new Size(width, height);
        }
    }

    /// <summary>
    /// A menu is similar to a <seealso cref="ListBox"/> except it does not have a title and item selection
    /// immediately closes the control. A menu is automatically closed (Remove from parent) when the mouse leves the region.
    /// </summary>
    public class Menu : ControlBase
    {
        private int _mouseOverIndex;
        private Rect _itemsRect;
        private int _numberItemsDisplayed;

        /// <summary>
        /// Raised when a menu item has been selected with a left mouse button click.
        /// </summary>
        public event EventHandler<MenuItemSelectedEventArgs> ItemSelected;

        public HAlign LabelAlignment { get; set; }

        public IEnumerable<MenuItemData> Items { get; set; }

        public Menu(MenuTemplate template) : base(template) {
            HasFrame = template.HasFrameBorder;

            if (Size.Width < 3 || Size.Height < 3)
                HasFrame = false;

            MouseOverHighlight = template.MouseOverHighlight;
            CanHaveKeyboardFocus = template.CanHaveKeyboardFocus;

            LabelAlignment = template.LabelAlignment;
            Items = template.Items;
            _mouseOverIndex = -1;

            CalcMetrics(template);
        }

        public string GetItemLabel(int index) {
            if (index < 0 || index >= Items.Count())
                throw new ArgumentOutOfRangeException("index");

            return Items.ElementAt(index).Label;
        }

        protected void DrawItems() {
            foreach (var item in Items.Take(_numberItemsDisplayed).Select((item, i) => new {item, Iter = i})) {
                DrawItem(item.Iter);
            }
        }

        protected void DrawItem(int index) {
            var item = Items.ElementAt(index);

            if (index == _mouseOverIndex) {
                Canvas.PrintStringAligned(_itemsRect.TopLeft.X,
                    _itemsRect.TopLeft.Y + index,
                    item.Label,
                    LabelAlignment,
                    _itemsRect.Size.Y,
                    Pigments[PigmentType.ViewHighlight]);
            }
            else {
                Canvas.PrintStringAligned(_itemsRect.TopLeft.X,
                    _itemsRect.TopLeft.Y + index,
                    item.Label,
                    LabelAlignment,
                    _itemsRect.Size.Y,
                    Pigments[PigmentType.ViewNormal]);
            }
        }

        protected int GetItemAt(Vec pos) {
            var index = -1;

            if (_itemsRect.Contains(pos))
                index = pos.Y - _itemsRect.Top;

            if (index < 0 || index >= Items.Count())
                index = -1;

            return index;
        }

        protected override void Redraw() {
            base.Redraw();
            DrawItems();
        }

        public override void OnMouseMoved(MouseData mouseData) {
            base.OnMouseMoved(mouseData);

            var pos = ScreenToLocal(mouseData.Position);

            _mouseOverIndex = GetItemAt(pos);

            if (_mouseOverIndex != -1)
                TooltipText = Items.ElementAt(_mouseOverIndex).ToolTip;
            else
                TooltipText = null;
        }

        public override void OnMouseButtonDown(MouseData mouseData) {
            base.OnMouseButtonDown(mouseData);

            if (_mouseOverIndex != -1)
                OnItemSelected(_mouseOverIndex);
        }

        protected internal override void OnMouseLeave() {
            base.OnMouseLeave();

            ParentWindow.RemoveControl(this);
        }

        protected virtual void OnItemSelected(int index) {
            if (ItemSelected != null)
                ItemSelected(this, new MenuItemSelectedEventArgs(index));

            ParentWindow.RemoveControl(this);
        }

        private void CalcMetrics(MenuTemplate template) {
            _itemsRect = this.LocalRectangle;
            if (HasFrame)
                _itemsRect = _itemsRect.Inflate(-1, -1);

            int delta = _itemsRect.Size.X - Items.Count();
            _numberItemsDisplayed = Items.Count();

            if (delta < 0)
                _numberItemsDisplayed += delta;
        }
    }
}