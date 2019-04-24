using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using Object = UnityEngine.Object;
#endif


namespace Catneep.Utils
{
    public static class Bezier
    {

        public static Vector2 GetQuadraticInterpolation(Vector2 a, Vector2 b, Vector2 c, float t)
        {
            Vector2 ab = Vector2.LerpUnclamped(a, b, t);
            Vector2 bc = Vector2.LerpUnclamped(b, c, t);

            return Vector2.LerpUnclamped(ab, bc, t);
        }

    }

    [Serializable]
    public struct QuadraticCurve
    {

        [SerializeField]
        private Vector2 from;
        /// <summary>
        /// El punto global de origen en el que la curva comienza.
        /// </summary>
        public Vector2 FromPoint
        {
            get
            {
                return from;
            }
            set
            {
                from = value;
            }
        }

        [SerializeField]
        private Vector2 toLocal;
        /// <summary>
        /// El punto relativo desde el punto de origen que esta curva termina.
        /// </summary>
        public Vector2 ToLocal { get { return toLocal; } set { toLocal = value; } }
        /// <summary>
        /// El punto global en el que esta curva termina.
        /// </summary>
        public Vector2 ToPoint
        {
            get
            {
                return from + toLocal;
            }
            set
            {
                toLocal = value - from;
            }
        }

        [SerializeField]
        private Vector2 curvatureLocal;
        /// <summary>
        /// El punto relativo desde el punto de origen, que define la curvatura.
        /// </summary>
        public Vector2 CurvatureLocal { get { return curvatureLocal; } set { curvatureLocal = value; } }
        /// <summary>
        /// El punto global de control de la curva, que define la curvatura.
        /// </summary>
        public Vector2 CurvaturePoint
        {
            get
            {
                return from + curvatureLocal;
            }
            set
            {
                curvatureLocal = value - from;
            }
        }


        public QuadraticCurve(Vector2 from, Vector2 toLocal, Vector2 curvatureLocal)
        {
            this.from = from;
            this.toLocal = toLocal;
            this.curvatureLocal = curvatureLocal;
        }
        public QuadraticCurve(QuadraticCurve copyFrom)
        {
            this.from = copyFrom.from;
            this.toLocal = copyFrom.toLocal;
            this.curvatureLocal = copyFrom.curvatureLocal;
        }


        public void DrawGizmos(Vector2 offset, float scale)
        {
            Vector2 aScaled = from * scale + offset;
            Vector2 cScaled = ToPoint * scale + offset;

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(aScaled, 0.01f * scale);
            Gizmos.DrawWireSphere(cScaled, 0.01f * scale);

            Vector2 bScaled = CurvaturePoint * scale + offset;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(bScaled, 0.01f * scale);
            Gizmos.DrawLine(aScaled, bScaled);
            Gizmos.DrawLine(cScaled, bScaled);
        }

        /// <summary>
        /// Devuelve una posición interpolando a través de una curva, desde el 0 hasta el 1,
        /// la interpolación no es uniforme y se suele concentrar en el punto de curvatura.
        /// Para una interpolación uniforme, usar <see cref="PrecomputedEvenCurve"/>
        /// </summary>
        /// <param name="t">Interpolación del 0 al 1 en la curva.</param>
        /// <returns>Posición de la interpolación.</returns>
        public Vector2 GetInterpolation(float t)
        {
            return Bezier.GetQuadraticInterpolation(from, CurvaturePoint, ToPoint, t);
        }

    }

    /// <summary>
    /// Una colección de Vector2 hecho a partir de una curva con un espacio igual entre puntos,
    /// y que además nos permite interpolar del 0 al 1.
    /// </summary>
    public class PrecomputedEvenCurve
    {

        Vector2[] points = new Vector2[0];
        float spacing = 0;


        public PrecomputedEvenCurve()
        {

        }
        public PrecomputedEvenCurve(Vector2 a, Vector2 b, Vector2 c, float spacing, float resolution = 1, float scale = 1)
        {
            UpdateCurve(a, b, c, spacing, resolution, scale);
        }
        public void UpdateCurve(QuadraticCurve curve, float spacing, float resolution = 1, float scale = 1)
        {
            UpdateCurve(curve.FromPoint, curve.CurvaturePoint, curve.ToPoint, spacing, resolution, scale);
        }
        public void UpdateCurve(Vector2 a, Vector2 b, Vector2 c, float spacing, float resolution = 1, float scale = 1)
        {
            // Escalamos según el valor de scale que nos pasen
            a *= scale;
            b *= scale;
            c *= scale;
            spacing *= scale;

            this.spacing = spacing;

            // Creamos una lista en la que ponemos todos los puntos que distribuimos
            // Empezamos agregando el punto a y dos variables que nos indican el último punto
            // y la distancia al mismo
            List<Vector2> points = new List<Vector2> { a };
            Vector2 previousPoint = a;
            float dstSinceLastEvenPoint = 0;

            // Para saber cuanto dividimos la interpolación de la curva bezier,
            // obtenemos una distancia estimada de la curva 
            // (La distancia entre el punto a y b + las distancias entre curvatura y los punto a y b dividido por 2)
            // y obtenemos junto a la resolución y una constante de 10 cuanto tenemos que incrementar 
            // la interpolación o 't'
            float estimatedLength = Vector2.Distance(b, a) + Vector2.Distance(b, c);
            estimatedLength *= .5f;
            estimatedLength += Vector2.Distance(a, c);
            float increaseAmount = 1f / Mathf.CeilToInt(estimatedLength * resolution * 10);

            // Empezamos desde la interpolación bezier 0, o sea desde el punto a
            float t = 0;
            while (t < 1)
            {
                // Al principio de cada iteración, incrementamos t por el valor de incremento
                t += increaseAmount;
                // Conseguimos la posición de la interpolación bezier en ese valor t
                Vector2 pointOnCurve = Bezier.GetQuadraticInterpolation(a, b, c, t);
                // Conseguimos la distancia del anterior punto y el punto que acabamos de conseguir
                dstSinceLastEvenPoint += Vector2.Distance(previousPoint, pointOnCurve);

                // Vemos si la distancia es mayor que la distancia de separación
                // y spawneamos todos los puntos necesarios
                while (dstSinceLastEvenPoint >= spacing)
                {
                    float overShootDst = dstSinceLastEvenPoint - spacing;
                    Vector2 newEvenlySpacedPoint = pointOnCurve + (previousPoint - pointOnCurve).normalized * overShootDst;
                    points.Add(newEvenlySpacedPoint);
                    dstSinceLastEvenPoint = overShootDst;
                    previousPoint = newEvenlySpacedPoint;
                }

                previousPoint = pointOnCurve;
            }

            // Con todos los puntos que agregamos a la lista lo asignamos a nuestro array
            this.points = points.ToArray();
        }

        public void DrawGizmos(Vector2 offset)
        {
            Gizmos.color = Color.yellow;
            foreach (var point in points)
            {
                Gizmos.DrawWireSphere(point + offset, spacing);
            }
        }


        public Vector2 Lerp(float t, float horizontal = 0)
        {
            if (points.Length < 2)
            {
                Debug.LogWarning("Curve point array too small to lerp.");
                return Vector2.zero;
            }

            float floatIndex = points.Length * t;
            int index = (int)(floatIndex);
            index = Mathf.Clamp(index, 0, points.Length - 2);
            floatIndex -= index;

            Vector2 point = points[index];
            Vector2 direction = points[index + 1] - point;

            if (horizontal != 0)
            {
                Vector2 right = new Vector2(direction.y, -direction.x).normalized;
                point += right * horizontal;
            }

            return point + direction * floatIndex;
        }

        public void Dispose()
        {

        }

    }
}
