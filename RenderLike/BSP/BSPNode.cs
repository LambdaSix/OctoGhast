using Microsoft.Xna.Framework;

namespace RenderLike.BSP
{
    public class BSPNode
    {
        private BSPNode[] Children;

        public Rectangle Rect { get; private set; }
        public SplitDirection SplitDirection { get; private set; }
        public int Level { get; private set; }

        public BSPNode Left {
            get { return Children != null ? Children[0] : null; }
            set {
                if (Children == null)
                    Children = new BSPNode[2];
                Children[0] = value;
            }
        }

        public BSPNode Parent { get; private set; }

        public bool IsRoot {
            get { return Parent == null; }
        }

        public bool IsLeaf {
            get { return Children == null || (Children[0] == null && Children[1] == null); }
        }

        public BSPNode Right {
            get { return IsLeaf ? null : Children[1]; }
            set {
                if (IsLeaf)
                    Children = new BSPNode[2];
                Children[1] = value;
            }
        }

        internal int NumberOfDescendents {
            get {
                return Children == null ? 0 : (Children[0].NumberOfDescendents + Children[1].NumberOfDescendents + 2);
            }
        }

        public bool TrySplit(SplitDirection direction, int size) {
            Rectangle lRect, rRect;

            switch (direction) {
                case SplitDirection.Horizontal: {
                    if (size <= 1 || size >= Rect.Height - 1)
                        return false;

                    lRect = new Rectangle(Rect.X, Rect.Y, Rect.Width, size);
                    rRect = new Rectangle(lRect.Left, lRect.Bottom, Rect.Width, Rect.Height - size);
                    break;
                }
                default:
                case SplitDirection.Vertical: {
                    if (size <= 1 || size >= Rect.Width - 1)
                        return false;

                    lRect = new Rectangle(Rect.X, Rect.Y, size, Rect.Height);
                    rRect = new Rectangle(lRect.Right, lRect.Top, Rect.Width - size, Rect.Height);
                    break;
                }
            }

            if (Children == null)
                Children = new BSPNode[2];

            Children[0] = new BSPNode(lRect, Level + 1, direction, null, null, this);
            Children[1] = new BSPNode(rRect, Level + 1, direction, null, null, this);

            return true;
        }

        public BSPNode(Rectangle rect, int level) {
            Rect = rect;
            Level = level;
        }

        internal BSPNode(Rectangle rect, int level, SplitDirection direction, BSPNode left, BSPNode right, BSPNode parent) {
            Rect = rect;
            Level = level;
            SplitDirection = direction;

            if (left != null && right != null) {
                Children = new BSPNode[2];
                Children[0] = left;
                Children[1] = right;
            }
            Parent = parent;
        }
    }
}