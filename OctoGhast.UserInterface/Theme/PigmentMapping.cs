using System;
using System.Collections.Generic;
using libtcod;
using OctoGhast.DataStructures;
using OctoGhast.UserInterface.Core;
using OctoGhast.UserInterface.Core.Interface;

namespace OctoGhast.UserInterface.Theme
{
    public enum PigmentType
    {
        /// <summary>
        /// Window background
        /// </summary>
        Window,
        Tooltip,
        DragItem,

        FrameFocus,
        FrameInactive,
        FrameHighlight,
        FrameNormal,
        FrameDepressed,
        FrameSelected,

        ViewFocus,
        ViewInactive,
        ViewHighlight,
        ViewNormal,
        ViewDepressed,
        ViewSelected
    }

    public class Pigment
    {
        private readonly IColor _foreground;
        private readonly IColor _background;
        private readonly TCODBackgroundFlag _backFlag;

        public IColor Foreground {
            get { return _foreground; }
        }

        public IColor Background {
            get { return _background; }
        }

        public TCODBackgroundFlag BackgroundFlag {
            get { return _backFlag; }
        }

        public Pigment(IColor foreground, IColor background, TCODBackgroundFlag backFlag) {
            _foreground = foreground;
            _background = background;
            _backFlag = backFlag;
        }

        public Pigment(IColor foreground, IColor background) : this(foreground, background, TCODBackgroundFlag.Set) {
        }

        public Pigment(long foreground, long background, TCODBackgroundFlag backFlag)
            : this(new Color(foreground), new Color(background), backFlag) {
        }

        public Pigment(long foreground, long background) : this(foreground, background, TCODBackgroundFlag.Set) {
        }

        public Pigment Invert() {
            return new Pigment(Background, Foreground);
        }

        public string GetColourCodes() {
            return String.Format("{0}{1}", Foreground.ForegroundCode(), Background.BackgroundCode());
        }

        public override string ToString() {
            return String.Format("{0}{1}", Foreground, Background);
        }
    }

    public class PigmentMapping
    {
        private readonly Lazy<IDictionary<PigmentType, Pigment>> _map;
        private readonly Lazy<IDictionary<PigmentType, Pigment>> _alternativeMap; 

        public IDictionary<PigmentType, Pigment> Map {
            get { return _map.Value; }
        }

        public IDictionary<PigmentType, Pigment> AlternativeMap {
            get { return _alternativeMap.Value; }
        } 

        public PigmentMapping() : this(null) {
        }

        /// <summary>
        /// Create a new pigment map.
        /// </summary>
        /// <param name="pigments"></param>
        /// <param name="alternativePigments">An alternative set of pigments to use over the normal/default set.</param>
        public PigmentMapping(IDictionary<PigmentType, Pigment> pigments, IDictionary<PigmentType, Pigment> alternativePigments = null) {
            _map = pigments != null
                ? new Lazy<IDictionary<PigmentType, Pigment>>(() => pigments)
                : new Lazy<IDictionary<PigmentType, Pigment>>(CreateDefaultMap);
            _alternativeMap = new Lazy<IDictionary<PigmentType, Pigment>>(() => alternativePigments ?? new Dictionary<PigmentType, Pigment>());
        }

        public PigmentMapping(PigmentMapping pigments, PigmentMapping alternativePigments)
            : this(pigments != null ? pigments.Map : null, alternativePigments.Map) {
        }

        public Pigment this[PigmentType type] {
            get { return AlternativeMap[type] ?? Map[type]; }
        }

        private IDictionary<PigmentType, Pigment> CreateDefaultMap() {
            var defaultMap = new Dictionary<PigmentType, Pigment>
            {
                {PigmentType.Window, new Pigment(0xDDDDDD, 0x000000)},
                {PigmentType.DragItem, new Pigment(0xD6AC8B, 0xF45B00)},
                {PigmentType.Tooltip, new Pigment(0x2B2B8F, 0xCCEEFF)},

                {PigmentType.FrameFocus, new Pigment(0x6D3D00, 0x3E1D00)},
                {PigmentType.FrameNormal, new Pigment(0x6D3D00, 0x3E1D00)},
                {PigmentType.FrameInactive, new Pigment(0x6D3D00, 0x3E1D00)},
                {PigmentType.FrameHighlight, new Pigment(0x6D3D00, 0x3E1D00)},
                {PigmentType.FrameDepressed, new Pigment(0x6D3D00, 0x3E1D00)},
                {PigmentType.FrameSelected, new Pigment(0x6D3D00, 0x3E1D00)},

                {PigmentType.ViewFocus, new Pigment(0xFFFFAA, 0x723E00)},
                {PigmentType.ViewNormal, new Pigment(0xDDDDDD, 0x622E00)},
                {PigmentType.ViewInactive, new Pigment(0x7C7C7C, 0x2E2E2E)},
                {PigmentType.ViewHighlight, new Pigment(0xFFFFAA, 0x723E00)},
                {PigmentType.ViewDepressed, new Pigment(0x6B6B6B, 0x431E00)},
                {PigmentType.ViewSelected, new Pigment(0x0098F4, 0x622E00)},
            };

            return defaultMap;
        } 
    }
}