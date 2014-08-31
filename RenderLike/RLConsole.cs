using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RenderLike
{
    public class RLConsole
    {
        public GraphicsDevice Graphics { get; set; }
        public Font Font { get; set; }
        public SpriteBatch SpriteBatch { get; set; }
        public RootSurface RootSurface { get; set; }
        public RenderTarget2D RenderTarget { get; set; }

        public int CharacterWidth {
            get { return Font == null ? 0 : Font.CharacterWidth; }
        }

        public int CharacterHeight {
            get { return Font == null ? 0 : Font.CharacterHeight; }
        }

        public RLConsole(GraphicsDevice device, Font font, int width, int height) {
            if (device == null)
                throw new ArgumentNullException("device");
            if (font == null)
                throw new ArgumentNullException("font");
            if (width <= 0)
                throw new ArgumentOutOfRangeException("width");
            if (height <= 0)
                throw new ArgumentOutOfRangeException("height");

            Graphics = device;
            Font = font;
            SpriteBatch = new SpriteBatch(device);

            RenderTarget = new RenderTarget2D(device,
                font.CharacterWidth*width,
                font.CharacterHeight*height,
                false, // Mipmap
                SurfaceFormat.Color,
                DepthFormat.None,
                0, // PreferredMultiSampleCount
                RenderTargetUsage.PreserveContents);

            RootSurface = new RootSurface(width, height, font, this);
            RootSurface.Clear();
        }

        public void ChangeFont(Font font) {
            if (font == null)
                throw new ArgumentNullException("font");
            if (font == Font)
                return;

            Font = font;
            RenderTarget = new RenderTarget2D(Graphics,
                font.CharacterWidth*RootSurface.Width,
                font.CharacterHeight*RootSurface.Height,
                false, // Mipmap
                SurfaceFormat.Color,
                DepthFormat.None,
                0, // PreferredMultiSampleCount
                RenderTargetUsage.PreserveContents
                );
            RootSurface.Font = font;
            RootSurface.Clear();
        }

        public RenderTarget2D Flush() {
            Graphics.SetRenderTarget(RenderTarget);
            SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);

            var src = new Rectangle(0, 0, 8, 8);

            for (int y = 0; y < RootSurface.Height; y++) {
                for (int x = 0; x < RootSurface.Width; x++) {
                    if (RootSurface.DirtyCells[x + y*RootSurface.Width] == true) {
                        // Background
                        Font.SetFontSourceRect(ref src, (char) Font.SolidChar);
                        SpriteBatch.Draw(Font.Texture,
                            new Vector2(x*Font.CharacterWidth, y*Font.CharacterWidth),
                            src,
                            RootSurface.Cells[x + y*RootSurface.Width].Back);

                        // Foreground
                        Font.SetFontSourceRect(ref src, RootSurface.Cells[x + y*RootSurface.Width].c);
                        SpriteBatch.Draw(Font.Texture,
                            new Vector2(x*Font.CharacterWidth, y*Font.CharacterHeight),
                            src,
                            RootSurface.Cells[x + y*RootSurface.Width].Fore);
                    }
                }
            }

            SpriteBatch.End();
            Graphics.SetRenderTarget(null);
            Array.Clear(RootSurface.DirtyCells, 0, RootSurface.DirtyCells.Length);
            return RenderTarget;
        }

        public Surface CreateSurface(int width, int height) {
            if (width <= 0)
                throw new ArgumentOutOfRangeException("width");
            if (height <= 0)
                throw new ArgumentOutOfRangeException("height");
        }
    }
}
