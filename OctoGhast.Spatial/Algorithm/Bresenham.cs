using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OctoGhast.Spatial
{
    public static class Bresenham
    {
        private static void Swap<T>(ref T lhs, ref T rhs) {
            T temp;
            temp = lhs;
            lhs = rhs;
            rhs = temp;
        }

        public static void Line(int x0, int y0, int x1, int y1, Func<int, int, bool> plot) {
            bool steep = Math.Abs(y1 - y0) > Math.Abs(x1 - x0);
            if (steep) {
                Swap(ref x0, ref y0);
                Swap(ref x1, ref y1);
            }
            if (x0 > x1) {
                Swap(ref x0, ref x1);
                Swap(ref y0, ref y1);
            }
            int dX = (x1 - x0), dY = Math.Abs(y1 - y0), err = (dX/2), ystep = (y0 < y1 ? 1 : -1), y = y0;

            for (int x = x0; x <= x1; ++x) {
                if (!(steep ? plot(y, x) : plot(x, y))) return;
                err = err - dY;
                if (err < 0) {
                    y += ystep;
                    err += dX;
                }
            }
        }
    }
}