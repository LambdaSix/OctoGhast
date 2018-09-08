using System.Collections.Generic;
using Microsoft.Xna.Framework;
using RenderLike;

namespace OctoGhast.SystemManager {
    public class MessageSystem {
            // Define the maximum number of lines to store
            private static readonly int _maxLines = 9;

            // Use a Queue to keep track of the lines of text
            // The first line added to the log will also be the first removed
            private readonly Queue<string> _lines;

            public MessageSystem()
            {
                _lines = new Queue<string>();
            }

            // Add a line to the MessageLog queue
            public void Add(string message)
            {
                _lines.Enqueue(message);

                // When exceeding the maximum number of lines remove the oldest one.
                if (_lines.Count > _maxLines)
                {
                    _lines.Dequeue();
                }
            }

            // Draw each line of the MessageLog queue to the console
            public void Draw(Surface surface)
            {
                string[] lines = _lines.ToArray();
                for (int i = 0; i < lines.Length; i++) {
                    surface.PrintString(1, i + 1, lines[i], Color.White);
                }
            }
    }
}