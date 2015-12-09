using System;
using OctoGhast.Spatial;
using OctoGhast.UserInterface.Theme;
using RenderLike;

namespace OctoGhast.UserInterface.Core.Interface
{
    /// <summary>
    /// Horizontal Alignment
    /// </summary>
    public enum HAlign
    {
        None,
        Left,
        Center,
        Right,
    }

    /// <summary>
    /// Vertical Alignment
    /// </summary>
    public enum VAlign
    {
        None,
        Top,
        Center,
        Bottom,
    }

    /// <summary>
    /// Provides an interface for an object that can be used for drawing on and then blitted to
    /// a RLConsole or another Canvas.
    /// </summary>
    public interface ICanvas : IDisposable
    {
        Surface Buffer { get; }
        Pigment DefaultPigment { get; }

        // Blit(...)

        /// <summary>
        /// Blit this <seealso cref="RLConsole"/> to the destination Surface at the given co-ordinates.
        /// </summary>
        /// <param name="dstSurface">The desination surface to blit to</param>        
        /// <param name="y">X co-ordinate to blit to in the target</param>
        /// <param name="y">Y co-ordinate to blit to in the target</param>
        void Blit(Surface dstSurface, int x, int y);

        /// <summary>
        /// Blit this <seealso cref="OctoGhast.UserInterface.Core.Canvas"/> to the given <seealso cref="RLConsole"/> at the 
        /// given co-ordinates.
        /// </summary>
        /// <param name="surface">The target <seealso cref="RLConsole"/></param>
        /// <param name="position">Co-ordinates to blit to in the target</param>
        void Blit(Surface surface, Vec position);

        /// <summary>
        /// Blit to the <seealso cref="RLConsole"/>'s root buffer.
        /// </summary>
        /// <param name="x">X co-ordinate to blit to in the root console</param>
        /// <param name="y">Y co-ordinate to blit to in the root console</param>
        void Blit(int x, int y);

        /// <summary>
        /// Blit to the <seealso cref="RLConsole"/>'s root buffer.
        /// </summary>
        /// <param name="x">X co-ordinate to blit to in the root console</param>
        /// <param name="y">Y co-ordinate to blit to in the root console</param>
        /// <param name="alpha">Alpha to apply to dstSurface to be blitted</param>
        void BlitToConsole(int x, int y, float alpha);

        /// <summary>
        /// Blit to the <seealso cref="RLConsole"/>'s root buffer.
        /// </summary>
        /// <param name="position">Co-ordinates to blit to in the root console</param>
        void Blit(Vec position);

        /// <summary>
        /// Blit to the <seealso cref="RLConsole"/>'s root buffer.
        /// </summary>
        /// <param name="position">Co-ordinates to blit to in the root console</param>
        /// <param name="alpha">Alpha to apply to dstSurface to be blitted</param>
        void Blit(Vec position, float alpha);

        /// <summary>
        /// Blit to the <seealso cref="RLConsole"/>'s root buffer
        /// Overwrites alpha values for every cell blitted.
        /// </summary>
        /// <param name="position">Co-ordinates to blit to in the root console</param>
        /// <param name="tooltipFgAlpha">Value to set for foreground alpha</param>
        /// <param name="tooltipBgAlpha">Value to set for the background alpha</param>
        void BlitToConsole(Vec position, float tooltipFgAlpha, float tooltipBgAlpha);

        /// <summary>
        /// Blit this <seealso cref="OctoGhast.UserInterface.Core.Canvas"/> to the destination <seealso cref="OctoGhast.UserInterface.Core.Canvas"/>
        /// </summary>
        /// <param name="dest">Destination canvas to blit to</param>
        /// <param name="x">X co-ordinate to blit to in the destination canvas</param>
        /// <param name="y">Y co-ordinate to blit to in the destination canvas</param>
        void Blit(ICanvas dest, int x, int y);

        /// <summary>
        /// Blit this <seealso cref="OctoGhast.UserInterface.Core.Canvas"/>'s contents to another <seealso cref="OctoGhast.UserInterface.Core.Canvas"/>
        /// </summary>
        /// <param name="dest">Destination canvas to blit to</param>
        /// <param name="destVec">Co-ordinates to blit to in the destination canvas</param>
        void Blit(ICanvas dest, Vec destVec);

        // PrintFrame(...)

        /// <summary>
        /// Print a framed border around this canvas with an optional title
        /// </summary>
        /// <param name="title">Optional centered title</param>
        /// <param name="pigment">Pigment to use when drawing</param>
        void PrintFrame(string title, Pigment pigment = null);

        // Pigments

        /// <summary>
        /// Sets the default <seealso cref="OctoGhast.Framework.Theme.Pigment"/> for this <seealso cref="ICanvas"/>.
        /// If no other pigment is specified for drawing operations, this pigment is used.
        /// </summary>
        /// <param name="pigment">Pigment to make default</param>
        void SetDefaultPigment(Pigment pigment);

        /// <summary>
        /// Sets the <seealso cref="OctoGhast.Framework.Theme.Pigment"/> at a specified point on the screen.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="pigment"></param>
        void SetPigmentAt(int x, int y, Pigment pigment);

        /// <summary>
        /// Sets the <seealso cref="OctoGhast.Framework.Theme.Pigment"/> at a specific point on the screen.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="pigment"></param>
        void SetPigmentAt(Vec position, Pigment pigment);

        // Misc

        /// <summary>
        /// Clear this canvas.
        /// </summary>
        void Clear();

        /// <summary>
        /// Scrolls this canvas
        /// </summary>
        /// <param name="deltaX">The amount to scroll in the horizontal direction</param>
        /// <param name="deltaY">The amount to scroll in the vertical direction</param>
        void Scroll(int deltaX, int deltaY);

        /// <summary>
        /// Scrolls this canvas
        /// </summary>
        /// <param name="delta">The amount to scroll in the X/Y direction</param>
        void Scroll(Vec delta);

        /// <summary>
        /// Trim the given string to the specified length ignoring any color
        /// codes embedded in the string.
        /// </summary>
        /// <param name="text">The string to trim</param>
        /// <param name="length">The target length for the string</param>
        /// <returns>A trimmed string with colour codes left intact.</returns>
        string TrimText(string text, int length);

        /// <summary>
        /// Get the size, in pixels, of a single character.
        /// </summary>
        /// <returns></returns>
        Size MeasureChar();

        /// <summary>
        /// Measure the width/length of the given string when printed.
        /// </summary>
        /// <param name="str">The string to measure</param>
        /// <returns>The measured length of the string</returns>
        int MeasureString(string str);

        /// <summary>
        /// Measure the position of the given string inside a given field length with the
        /// specified horizontal alignment
        /// </summary>
        /// <param name="pos">The position of the left side of the field</param>
        /// <param name="str">The string to measure</param>
        /// <param name="alignment">The given alignment to consider</param>
        /// <param name="fieldLength">How long the field is</param>
        /// <returns>The offset from the left of where the string will be</returns>
        Vec MeasureAlignOffset(Vec pos, string str, HAlign alignment, int fieldLength);

        /// <summary>
        /// Measure the position of the given string inside a given field when aligned both
        /// horizontally and vertically, within a field determined by a top-left anchor and dimension.
        /// </summary>
        /// <param name="pos">The position of the top left of the field</param>
        /// <param name="str">The string to measure</param>
        /// <param name="hAlign">The given horizontal alignment to consider</param>
        /// <param name="vAlign">The given vertical alignment to consider</param>
        /// <param name="fieldSize">The dimensions of the field</param>
        /// <returns>The offset from the top-left of where the string will be</returns>
        Vec MeasureAlignOffset(Vec pos, string str, HAlign hAlign, VAlign vAlign, Size fieldSize);

        // PrintChar(...)

        /// <summary>
        /// Print a single character at the given co-ordinates.
        /// If <paramref name="pigment"/> is null then the <seealso cref="DefaultPigment"/> is used.
        /// </summary>
        /// <param name="x">X co-ordinate</param>
        /// <param name="y">Y co-ordinate</param>
        /// <param name="character">Character to print</param>
        /// <param name="pigment"><seealso cref="OctoGhast.Framework.Theme.Pigment"/> to use for coloring</param>
        void PrintChar(int x, int y, char character, Pigment pigment = null);

        /// <summary>
        /// Print a single character at the given co-ordinates.
        /// If <paramref name="pigment"/> is null then the <seealso cref="DefaultPigment"/> is used.
        /// </summary>
        /// <param name="pos">Position to print the character</param>
        /// <param name="character">Character to print</param>
        /// <param name="pigment"><seealso cref="OctoGhast.Framework.Theme.Pigment"/> to use for coloring</param>
        void PrintChar(Vec pos, char character, Pigment pigment = null);

        // PrintString(...)

        /// <summary>
        /// Print the given string at the specified co-ordinates.
        /// If <paramref name="pigment"/> is null then the <seealso cref="DefaultPigment"/> is used.
        /// </summary>
        /// <param name="x">X co-ordinate</param>
        /// <param name="y">Y co-ordinate</param>
        /// <param name="str">String to print</param>
        /// <param name="pigment"><seealso cref="OctoGhast.Framework.Theme.Pigment"/> to use for coloring</param>
        void PrintString(int x, int y, string str, Pigment pigment = null);

        /// <summary>
        /// Print the specified string at the given co-ordinates.
        /// If <paramref name="pigment"/> is null then the <seealso cref="DefaultPigment"/> is used.
        /// </summary>
        /// <param name="pos">Position to print string at</param>
        /// <param name="str">String to print</param>
        /// <param name="pigment"><seealso cref="OctoGhast.Framework.Theme.Pigment"/> to use for coloring</param>
        void PrintString(Vec pos, string str, Pigment pigment = null);

        // PrintStringAligned(...)

        /// <summary>
        /// Prints the given string at the specified co-ordinates.
        /// The text is aligned horizontal by the specified alignment and within the field length.
        /// If the text is longer than the field length, it will be trimmed to fit.
        /// The field length must be greater or equal to 1.
        /// If <paramref name="pigment"/> is null then the <seealso cref="DefaultPigment"/> is used.
        /// </summary>
        /// <param name="x">X co-ordinate for the left of the string</param>
        /// <param name="y">Y co-ordinates for the left of the string</param>
        /// <param name="str">The string to print</param>
        /// <param name="alignment">Horizontal alignment to use</param>
        /// <param name="fieldLength">The length of field to fit the string into</param>
        /// <param name="pigment"><seealso cref="OctoGhast.Framework.Theme.Pigment"/> to use for coloring</param>
        void PrintStringAligned(int x, int y, string str, HAlign alignment, int fieldLength, Pigment pigment = null);

        /// <summary>
        /// Prints the given string at the specified co-ordinates.
        /// The text is aligned horizontally and vertically to the specified alignments and
        /// within the specified size of the field.
        /// If the text length is larger than the region, it will be trimmed.
        /// Supports automatically wrapping the text across lines into the height provided
        /// by the target region.
        /// If <paramref name="pigment"/> is null then the <seealso cref="DefaultPigment"/> is used.
        /// </summary>
        /// <param name="x">X co-ordinate of the top-left anchor</param>
        /// <param name="y">Y co-ordinate of the top-left anchor</param>
        /// <param name="str">String to print</param>
        /// <param name="hAlign">Horizontal alignment</param>
        /// <param name="vAlign">Vertical alignment</param>
        /// <param name="fieldSize">Dimensions of the field to contain the string within</param>
        /// <param name="pigment"><seealso cref="OctoGhast.Framework.Theme.Pigment"/> to use for coloring</param>
        void PrintStringAligned(int x, int y, string str, HAlign hAlign, VAlign vAlign, Size fieldSize,
            Pigment pigment = null);

        /// <summary>
        /// Prints the given string at the specified co-ordinates.
        /// The text is aligned horizontally by the specified alignment and within the field length.
        /// If the text is longer than the field length, it will be trimmed to fit.
        /// The field length must be greater or equal to 1.
        /// If <paramref name="pigment"/> is null then the <seealso cref="DefaultPigment"/> is used.
        /// </summary>
        /// <param name="pos">The co-ordinates of the top-left anchor</param>
        /// <param name="str">The string to print</param>
        /// <param name="alignment">Horizontal alignment to use</param>
        /// <param name="fieldLength">The length of field to fit the string into</param>
        /// <param name="pigment"><seealso cref="OctoGhast.Framework.Theme.Pigment"/> to use for coloring</param>
        void PrintStringAligned(Vec pos, string str, HAlign alignment, int fieldLength, Pigment pigment = null);

        /// <summary>
        /// Prints the given string at the specified co-ordinates.
        /// The text is aligned horizontally and vertically to the specified alignments and
        /// within the specified size of the field.
        /// If the text length is larger than the region, it will be trimmed.
        /// Supports automatically wrapping the text across lines into the height provided
        /// by the target region.
        /// If <paramref name="pigment"/> is null then the <seealso cref="DefaultPigment"/> is used.
        /// </summary>
        /// <param name="pos">Co-ordinate of the top-left anchor</param>
        /// <param name="str">String to print</param>
        /// <param name="hAlign">Horizontal alignment</param>
        /// <param name="vAlign">Vertical alignment</param>
        /// <param name="fieldSize">Dimensions of the field to contain the string within</param>
        /// <param name="pigment"><seealso cref="OctoGhast.Framework.Theme.Pigment"/> to use for coloring</param>
        void PrintStringAligned(Vec pos, string str, HAlign hAlign, VAlign vAlign, Size fieldSize,
            Pigment pigment = null);

        // Draw*Line(...)

        /// <summary>
        /// Draws a horizontal line of the given length from the start co-ordinates
        /// If <paramref name="pigment"/> is null then the <seealso cref="DefaultPigment"/> is used.
        /// </summary>
        /// <param name="startX">X co-ordinate of the anchor</param>
        /// <param name="startY">Y co-ordinate of the anchor</param>
        /// <param name="length">How long of a line to draw</param>
        /// /// <param name="pigment"><seealso cref="OctoGhast.Framework.Theme.Pigment"/> to use for coloring</param>
        void DrawHLine(int startX, int startY, int length, Pigment pigment = null);

        /// <summary>
        /// Draws a horizontal line of the given length from the start co-ordinates
        /// If <paramref name="pigment"/> is null then the <seealso cref="DefaultPigment"/> is used.
        /// </summary>
        /// <param name="start">Co-ordinates to start drawing at</param>
        /// <param name="length">Length of the line to draw</param>
        /// <param name="pigment"><seealso cref="OctoGhast.Framework.Theme.Pigment"/> to use for coloring</param>
        void DrawHLine(Vec start, int length, Pigment pigment = null);

        /// <summary>
        /// Draws a vertical line of the given length from the start co-ordinates
        /// If <paramref name="pigment"/> is null then the <seealso cref="DefaultPigment"/> is used.
        /// </summary>
        /// <param name="startX">X co-ordinate of the anchor</param>
        /// <param name="startY">Y co-ordinate of the anchor</param>
        /// <param name="length">Length of the line to draw</param>
        /// <param name="pigment"><seealso cref="OctoGhast.Framework.Theme.Pigment"/> to use for coloring</param>
        void DrawVLine(int startX, int startY, int length, Pigment pigment = null);

        /// <summary>
        /// Draws a vertical line of the given length from the start co-ordinates
        /// If <paramref name="pigment"/> is null then the <seealso cref="DefaultPigment"/> is used.
        /// </summary>
        /// <param name="start">Co-ordinate of the anchor</param>
        /// <param name="length">Length of the line to draw</param>
        /// <param name="pigment"><seealso cref="OctoGhast.Framework.Theme.Pigment"/> to use for coloring</param>
        void DrawVLine(Vec start, int length, Pigment pigment = null);
    }
}