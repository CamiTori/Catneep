using UnityEngine;


namespace Catneep.Utils
{

    public struct Line2D
    {

        public Vector2 a;
        public Vector2 b;

        public Vector2 Direction
        {
            get { return b - a; }
            set { b = a + value; }
        }

        public Line2D(Vector2 a, Vector2 b, bool bRelativeToA = false)
        {
            this.a = a;
            this.b = b;
            if (bRelativeToA) this.b += a;
        }
        public Line2D(Ray ray)
        {
            this.a = ray.origin;
            this.b = ray.origin + ray.direction;
        }

        public bool TryGetIntersectionPoint(Line2D otherLine, out Vector2 found)
        {
            return TryGetIntersectionPoint(this, otherLine, out found);
        }

        public static bool TryGetIntersectionPoint(Ray2D a, Ray2D b, out Vector2 found)
        {
            return TryGetIntersectionPoint(a.origin, a.origin + a.direction, b.origin, b.origin + b.direction, 
                out found);
        }
        public static bool TryGetIntersectionPoint(Line2D a, Line2D b, out Vector2 found)
        {
            return TryGetIntersectionPoint(a.a, a.b, b.a, b.b, out found);
        }
        public static bool TryGetIntersectionPoint(Vector2 A1, Vector2 A2, Vector2 B1, Vector2 B2, 
            out Vector2 found)
        {
            float tmp = (B2.x - B1.x) * (A2.y - A1.y) - (B2.y - B1.y) * (A2.x - A1.x);

            if (tmp == 0)
            {
                // No solution!
                found = Vector2.zero;
                return false;
            }

            float mu = ((A1.x - B1.x) * (A2.y - A1.y) - (A1.y - B1.y) * (A2.x - A1.x)) / tmp;

            found = new Vector2(
                B1.x + (B2.x - B1.x) * mu,
                B1.y + (B2.y - B1.y) * mu
            );
            return true;
        }

    }

}
