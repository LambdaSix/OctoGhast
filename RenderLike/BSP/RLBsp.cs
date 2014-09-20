using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OpenTK.Graphics.OpenGL;

namespace RenderLike.BSP
{
    public class BspTree
    {
        public BSPNode Root { get; set; }

        public int NodeCount {
            get { return Root.NumberOfDescendents + 1; }
        }        

        public int Depth {
            get { return PostOrder }
        }

        /// <summary>
        /// Enumerate nodes using pre-order traversal.
        /// </summary>
        public IEnumerable<BSPNode> Preorder {
            get {
                var toVisit = new Stack<BSPNode>(NodeCount);
                var current = Root;

                if (current != null)
                    toVisit.Push(current);

                while (toVisit.Count != 0) {
                    current = toVisit.Pop();

                    if (current.Right != null) toVisit.Push(current.Right);
                    if (current.Left != null) toVisit.Push(current.Left);

                    yield return current;
                }
            }
        }

        public IEnumerable<BSPNode> Inorder {
            get {
                var toVisit = new Stack<BSPNode>(NodeCount);
                for (var current = Root; current != null || toVisit.Count != 0; current = current.Right) {
                    toVisit.Push(current);
                    current = current.Left;
                }
            }
        }

        public

        public BspTree(Rectangle rootRect) {
            Root = new BSPNode(rootRect, 0);
        }
    }
}