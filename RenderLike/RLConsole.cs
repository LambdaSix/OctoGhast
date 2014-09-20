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
                        Font.SetFontSourceRect(ref src, RootSurface.Cells[x + y*RootSurface.Width].Char);
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

            return new Surface(width, height, Font, this);
        }

        public void Blit(Surface src, Surface dst, Rectangle srcRect, int destX, int destY) {
            if (src == null)
                throw new ArgumentNullException("src");
            if (dst == null)
                throw new ArgumentNullException("dst");

            var blitRect = new Rectangle(destX, destY, srcRect.Width, srcRect.Height);
            int deltaX = srcRect.Left - blitRect.Left;
            int deltaY = srcRect.Top - blitRect.Top;

            bool dstIsRoot = dst is RootSurface;
            var dstAsRoot = dst as RootSurface;

            blitRect = Rectangle.Intersect(blitRect, new Rectangle(0, 0, dst.Width, dst.Height));

            for (int y = blitRect.Top; y < blitRect.Bottom; y++) {
                for (int x = blitRect.Left; x < blitRect.Right; x++) {
                    int sx = deltaX + x;
                    int sy = deltaY + y;

                    dst.Cells[x + y*dst.Width].Back = src.Cells[sx + sy*src.Width].Back;
                    dst.Cells[x + y*dst.Width].Fore = src.Cells[sx + sy*src.Width].Fore;
                    dst.Cells[x + y*dst.Width].Char = src.Cells[sx + sy*src.Width].Char;

                    if (dstIsRoot) {
                        dstAsRoot.DirtyCells[x + y*dst.Width] = true;
                    }
                }
            }
        }

        public void BlitAlpha(Surface src, Surface dst, Rectangle srcRect, int destX, int destY, float fgAlpha, float bgAlpha) {
            if (src == null)
                throw new ArgumentNullException("src");
            if (dst == null)
                throw new ArgumentNullException("dst");

            fgAlpha = MathHelper.Clamp(fgAlpha, 0f, 1.0f);
            bgAlpha = MathHelper.Clamp(bgAlpha, 0f, 1.0f);

            var blitRect = new Rectangle(destX, destY, srcRect.Width, srcRect.Height);
            int deltaX = srcRect.Left - blitRect.Left;
            int deltaY = srcRect.Top - blitRect.Top;

            var dstAsRoot = dst as RootSurface;

            blitRect = Rectangle.Intersect(blitRect, new Rectangle(0, 0, dst.Width, dst.Height));
            Color backCol, foreCol;
            char ch;

            for (int y = blitRect.Top; y < blitRect.Bottom; y++) {
                for (int x = blitRect.Left; x < blitRect.Right; x++) {
                    int sx = deltaX + x;
                    int sy = deltaY + y;

                    backCol = dst.Cells[x + y*dst.Width].Back;
                    backCol.A = (byte)(bgAlpha * 255.0f + 0.5f);

                    if (src.Cells[sx + sy*src.Width].Char == ' ') {
                        foreCol = dst.Cells[x + y*dst.Width].Fore;
                        foreCol.A = (byte) (fgAlpha*255.0f + 0.5f);
                        ch = dst.Cells[x + y*dst.Width].Char;
                    }
                    else {
                        foreCol = src.Cells[sx + sy*src.Width].Fore;
                        ch = src.Cells[sx + sy*src.Width].Char;
                    }

                    dst.Cells[x + y*dst.Width].Back = backCol;
                    dst.Cells[x + y*dst.Width].Fore = foreCol;
                    dst.Cells[x + y*dst.Width].Char = ch;

                    if (dstAsRoot != null) {
                        dstAsRoot.DirtyCells[x + y*dst.Width] = true;
                    }
                }
            }
        }

        public void BlitAlpha(Surface src, Surface dst, Rectangle srcRect, int destX, int destY, float alpha) {
            if (src == null)
                throw new ArgumentNullException("src");
            if (dst == null)
                throw new ArgumentNullException("dst");

            alpha = MathHelper.Clamp(alpha, 0f, 1f);

            var blitRect = new Rectangle(destX, destY, srcRect.Width, srcRect.Height);
            int deltaX = srcRect.Left - blitRect.Left;
            int deltaY = srcRect.Top - blitRect.Top;

            var dstAsRoot = dst as RootSurface;

            blitRect = Rectangle.Intersect(blitRect, new Rectangle(0, 0, dst.Width, dst.Height));
            Color backCol, foreCol;
            char ch;

            for (int y = blitRect.Top; y < blitRect.Bottom; y++) {
                for (int x = blitRect.Left; x < blitRect.Right; x++) {
                    int sx = deltaX + x;
                    int sy = deltaY + y;

                    backCol = Color.Lerp(dst.Cells[x + y*dst.Width].Back, src.Cells[sx + sy*src.Width].Back, alpha);

                    if (src.Cells[sx + sy*src.Width].Char == ' ') {
                        foreCol = Color.Lerp(dst.Cells[x + y*dst.Width].Fore, src.Cells[sx + sy*src.Width].Back, alpha);
                        ch = dst.Cells[x + y*dst.Width].Char;
                    }
                    else {
                        foreCol = src.Cells[sx + sy*src.Width].Fore;
                        ch = src.Cells[sx + sy*src.Width].Char;
                    }

                    dst.Cells[x + y*dst.Width].Back = backCol;
                    dst.Cells[x + y*dst.Width].Fore = foreCol;
                    dst.Cells[x + y*dst.Width].Char = ch;

                    if (dstAsRoot != null)
                        dstAsRoot.DirtyCells[x + y*dst.Width] = true;
                }
            }
        }

        public void Blit(Surface src, Surface dst, int destX, int destY) {
            if (src == null)
                throw new ArgumentNullException("src");
            if (dst == null)
                throw new ArgumentNullException("dst");

            Blit(src, dst, new Rectangle(0, 0, src.Width, src.Height), destX, destY);
        }

        public void Blit(Texture2D src, Surface dst, Rectangle srcRect, int destX, int destY) {
            var data = new Color[srcRect.Width*srcRect.Height];
            src.GetData(0, srcRect, data, 0, data.Length);

            for (int i = 0; i < data.Length; i++) {
                int y = i/srcRect.Width;
                int x = i%srcRect.Width;
                dst.PrintChar(destX + x, destY + y, (char)Font.SolidChar, data[i]);
            }
        }

        internal void RootClear() {
            Graphics.SetRenderTarget(RenderTarget);
            Graphics.Clear(RootSurface.DefaultBackground);
            Graphics.SetRenderTarget(null);
        }
    }
}
