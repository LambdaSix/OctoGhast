using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
namespace RenderLike.BSP
{
    internal static class BspNodeExtension
    {
        internal static int Level(this BSPNode context, int or) {
            return context != null ? context.Level : or;
        }
    }

    public class BSPTree
    {
        public BSPNode Root { get; set; }

        public int NodeCount {
            get { return Root.NumberOfDescendents + 1; }
        }

        public int Depth {
            get { return Postorder.FirstOrDefault(s => s.IsLeaf).Level(or: 0); }
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

                    if (current.Right != null)
                        toVisit.Push(current.Right);
                    if (current.Left != null)
                        toVisit.Push(current.Left);

                    yield return current;
                }
            }
        }

        public IEnumerable<BSPNode> Inorder {
            get {
                var toVisit = new Stack<BSPNode>(NodeCount);
                for (var current = Root; current != null || toVisit.Count != 0; current = current.Right) {
                    while (current != null) {
                        toVisit.Push(current);
                        current = current.Left;
                    }
                    current = toVisit.Pop();
                    yield return current;
                }
            }
        }

        public IEnumerable<BSPNode> Postorder {
            get {
                // maintain two stacks, one of a list of nodes to visit,
                // and one of booleans, indicating if the note has been processed
                // or not.
                var toVisit = new Stack<BSPNode>(NodeCount);
                var hasBeenProcessed = new Stack<bool>(NodeCount);
                BSPNode current = Root;
                if (current != null) {
                    toVisit.Push(current);
                    hasBeenProcessed.Push(false);
                    current = current.Left;
                }

                while (toVisit.Count != 0) {
                    if (current != null) {
                        // add this node to the stack with a false processed value
                        toVisit.Push(current);
                        hasBeenProcessed.Push(false);
                        current = current.Left;
                    }
                    else {
                        // see if the node on the stack has been processed
                        bool processed = hasBeenProcessed.Pop();
                        BSPNode node = toVisit.Pop();
                        if (!processed) {
                            // if it's not been processed, "recurse" down the right subtree
                            toVisit.Push(node);
                            hasBeenProcessed.Push(true); // it's now been processed
                            current = node.Right;
                        }
                        else
                            yield return node;
                    }
                }
            }
        }

        public IEnumerable<BSPNode> LevelOrder {
            get {
                var queue = new Queue<BSPNode>();
                var current = Root;

                queue.Enqueue(current);

                while (queue.Count != 0) {
                    current = queue.Dequeue();

                    if (current.Left != null)
                        queue.Enqueue(current.Left);
                    if (current.Right != null)
                        queue.Enqueue(current.Right);

                    yield return current;
                }
            }
        }

        public BSPTree(Rectangle rootRect) {
            Root = new BSPNode(rootRect, 0);
        }

        public IEnumerable<BSPNode> GetByLevel(int level) {
            return LevelOrder.Reverse().Where(s => s.Level == level);
        }

        public IEnumerable<BSPNode> GetAllLeaves() {
            return LevelOrder.Reverse().Where(s => s.IsLeaf);
        }

        public void SplitRecursive(int times, int minHSize, int minVSize, float mHRatio, float mVRatio, Rand rand) {
            float hFactor, vFactor, nodeHW, nodeWH;
            SplitDirection dir;
            for (int i = 0; i < times; i++) {
                foreach (var node in GetByLevel(Depth)) {
                    hFactor = vFactor = 1.0f;
                    nodeHW = node.Rect.Height / (float)node.Rect.Width;
                    nodeWH = 1.0f / nodeHW;

                    if (nodeHW > mHRatio) {
                        if (nodeWH > mVRatio) {
                            dir = rand.FromEnum<SplitDirection>();
                        }
                        else {
                            dir = SplitDirection.Horizontal;
                            vFactor = nodeWH;
                        }
                    }
                    else {
                        if (nodeWH > mVRatio) {
                            dir = SplitDirection.Vertical;
                            hFactor = nodeHW;
                        }
                        else {
                            dir = rand.FromEnum<SplitDirection>();
                        }
                    }

                    int pos = 0;

                    if (TrySplitPos(dir, node, minHSize, minVSize, hFactor, vFactor, rand, out pos)) {
                        node.TrySplit(dir, pos);
                    } else {
                      if (dir == SplitDirection.Horizontal)
                          dir = SplitDirection.Vertical;
                      else
                          dir = SplitDirection.Horizontal;

                        if (TrySplitPos(dir, node, minHSize, minVSize, hFactor, vFactor, rand, out pos)) {
                            node.TrySplit(dir, pos);
                        }
                    }
                }
            }
        }

        bool TrySplitPos(SplitDirection dir, BSPNode n, int minHSize, int minVSize, float hFactor, float vFactor, Rand rand,
                out int pos) {
            pos = 0;
            if (dir == SplitDirection.Horizontal) {
                int min2 = (int)(vFactor * 0.5f * n.Rect.Height);
                int minDelta = Math.Max(minVSize, min2);

                if (minDelta > n.Rect.Height / 2)
                    return false;
                pos = rand.GetInt(minDelta, n.Rect.Height - minDelta);
            }
            else {
                int min2 = (int)(hFactor * 0.5f * n.Rect.Width);
                int minDelta = Math.Max(minHSize, min2);

                if (minDelta > n.Rect.Width / 2)
                    return false;

                pos = rand.GetInt(minDelta, n.Rect.Width - minDelta);
            }
            return true;
        }
    }
}