using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Catneep.Utils;


public class UILine : Graphic
{

    [Header("Line Renderer")]

    [SerializeField]
    private float lineThickness = 2;

    [SerializeField]
    [Range(0f, 90f)]
    private float maxAngleDiffAngleJoin = 85f;

    [SerializeField]
    private Sprite image;
    public override Texture mainTexture
    {
        get
        {
            return image != null ? image.texture : null;
        }
    }

    [SerializeField]
    private Vector2[] points;
    public Vector2[] Points
    {
        get
        {
            return points;
        }
        set
        {
            points = value;
            UpdateGeometry();
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (points == null || points.Length < 2) return;

        float bottomCapSize = 0f, topCapSize = 0f;
        float yUV = 0, yUVSize = 1;
        if (image != null)
        {
            Vector2 spriteSize = image.rect.size;

            bottomCapSize = image.border.y;
            topCapSize = image.border.w;

            yUV = bottomCapSize / spriteSize.y;
            yUVSize = (spriteSize.y - bottomCapSize - topCapSize) / spriteSize.y;

            float sizeMultiplier = lineThickness / spriteSize.x;
            bottomCapSize *= sizeMultiplier;
            topCapSize *= sizeMultiplier;
        }

        List<Vector2> allPoints = new List<Vector2>(points);
        if (bottomCapSize > 0)
        {
            allPoints.Insert(0, allPoints[0] + (allPoints[0] - allPoints[1]).normalized * bottomCapSize);
        }
        if (topCapSize > 0)
        {
            int lastI = allPoints.Count - 1;
            allPoints.Add(allPoints[lastI] + (allPoints[lastI] - allPoints[lastI - 1]).normalized * bottomCapSize);
        }

        Vector2 previous = allPoints[0];
        Vector2 cur = previous;
        Vector2 direction = Vector2.zero;
        Line2D line1 = default(Line2D), line2 = default(Line2D);

        float uvMultiplier = yUVSize / (points.Length - 1);
        int length = allPoints.Count;

        for (int i = 0; i < length; i++)
        {
            bool first = i <= 0;
            bool last = i + 1 >= length;

            Vector2 next = Vector2.zero;
            if (!last)
            {
                next = allPoints[i + 1];
                direction = next - cur;
            }
            Vector2 rightSide = new Vector2(direction.y, -direction.x).normalized * lineThickness * .5f;

            Vector2 v1 = cur - rightSide, v2 = cur + rightSide;
            Line2D prevLine1 = line1, prevLine2 = line2;
            line1 = new Line2D(v1, direction, true);
            line2 = new Line2D(v2, direction, true);
            
            if (!first && !last && 
                Mathf.Abs(Vector2.Angle(prevLine1.Direction, line1.Direction) - 90) < maxAngleDiffAngleJoin)
            {
                Vector2 getV;
                if (prevLine1.TryGetIntersectionPoint(line1, out getV))
                {
                    v1 = getV;
                }
                if (prevLine2.TryGetIntersectionPoint(line2, out getV))
                {
                    v2 = getV;
                }
            }

            float currentUV = 0f;
            if (last && topCapSize > 0)
            {
                currentUV = 1f;
            }
            else if (!first && bottomCapSize > 0)
            {
                currentUV = yUV;
                yUV += uvMultiplier;
            }

            vh.AddVert(v1, color, new Vector2(0, currentUV));
            vh.AddVert(v2, color, new Vector2(1, currentUV));

            if (!first)
            {
                int startI = i * 2;
                vh.AddTriangle(startI - 2, startI + 1, startI - 1);
                vh.AddTriangle(startI - 2, startI, startI + 1);
            }

            previous = cur;
            cur = next;
        }
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        UpdateGeometry();
    }
#endif

    /*
    [System.Obsolete]
    protected override void OnPopulateMesh(Mesh toFill)
    {
        // requires sets of quads
        if (points == null || points.Length < 2)
            points = new[] { new Vector2(0, 0), new Vector2(1, 1) };
        var capSize = 0;
        var sizeX = rectTransform.rect.width;
        var sizeY = rectTransform.rect.height;
        var offsetX = -rectTransform.pivot.x * rectTransform.rect.width;
        var offsetY = -rectTransform.pivot.y * rectTransform.rect.height;

        // don't want to scale based on the size of the rect, so this is switchable now
        if (!relativeSize)
        {
            sizeX = 1;
            sizeY = 1;
        }
        // build a new set of points taking into account the cap sizes.
        // would be cool to support corners too, but that might be a bit tough :)
        var pointList = new List<Vector2>
        {
            points[0]
        };
        var capPoint = points[0] + (points[1] - points[0]).normalized * capSize;
        pointList.Add(capPoint);

        // should bail before the last point to add another cap point
        for (int i = 1; i < points.Length - 1; i++)
        {
            pointList.Add(points[i]);
        }
        capPoint = points[points.Length - 1] - (points[points.Length - 1] - points[points.Length - 2]).normalized * capSize;
        pointList.Add(capPoint);
        pointList.Add(points[points.Length - 1]);

        var TempPoints = pointList.ToArray();
        if (useMargins)
        {
            sizeX -= margin.x;
            sizeY -= margin.y;
            offsetX += margin.x / 2f;
            offsetY += margin.y / 2f;
        }

        toFill.Clear();
        var vbo = new VertexHelper(toFill);

        Vector2 prevV1 = Vector2.zero;
        Vector2 prevV2 = Vector2.zero;

        for (int i = 1; i < TempPoints.Length; i++)
        {
            var prev = TempPoints[i - 1];
            var cur = TempPoints[i];
            prev = new Vector2(prev.x * sizeX + offsetX, prev.y * sizeY + offsetY);
            cur = new Vector2(cur.x * sizeX + offsetX, cur.y * sizeY + offsetY);

            float angle = Mathf.Atan2(cur.y - prev.y, cur.x - prev.x) * 180f / Mathf.PI;

            var v1 = prev + new Vector2(0, -lineThickness / 2);
            var v2 = prev + new Vector2(0, +lineThickness / 2);
            var v3 = cur + new Vector2(0, +lineThickness / 2);
            var v4 = cur + new Vector2(0, -lineThickness / 2);

            v1 = RotatePointAroundPivot(v1, prev, new Vector3(0, 0, angle));
            v2 = RotatePointAroundPivot(v2, prev, new Vector3(0, 0, angle));
            v3 = RotatePointAroundPivot(v3, cur, new Vector3(0, 0, angle));
            v4 = RotatePointAroundPivot(v4, cur, new Vector3(0, 0, angle));

            Vector2 uvTopLeft = Vector2.zero;
            Vector2 uvBottomLeft = new Vector2(0, 1);

            Vector2 uvTopCenter = new Vector2(0.5f, 0);
            Vector2 uvBottomCenter = new Vector2(0.5f, 1);

            Vector2 uvTopRight = new Vector2(1, 0);
            Vector2 uvBottomRight = new Vector2(1, 1);

            Vector2[] uvs = new[] { uvTopCenter, uvBottomCenter, uvBottomCenter, uvTopCenter };

            if (i > 1)
                SetVbo(vbo, new[] { prevV1, prevV2, v1, v2 }, uvs);

            if (i == 1)
                uvs = new[] { uvTopLeft, uvBottomLeft, uvBottomCenter, uvTopCenter };
            else if (i == TempPoints.Length - 1)
                uvs = new[] { uvTopCenter, uvBottomCenter, uvBottomRight, uvTopRight };

            //SetVbo(vbo, new[] { v1, v2, v3, v4 }, uvs, toFill);
            vbo.AddUIVertexQuad(SetVbo(vbo, new[] { v1, v2, v3, v4 }, uvs));
            vbo.FillMesh(toFill);

            prevV1 = v3;
            prevV2 = v4;
        }
    }

    //protected void SetVbo(UIVertex vbo, Vector2[] vertices, Vector2[] uvs)
    protected UIVertex[] SetVbo(VertexHelper vbo, Vector2[] vertices, Vector2[] uvs)
    {
        UIVertex[] VboVertices = new UIVertex[4];

        for (int i = 0; i < vertices.Length; i++)
        {
            var vert = UIVertex.simpleVert;
            vert.color = color;
            vert.position = vertices[i];
            vert.uv0 = uvs[i];
            VboVertices[i] = vert;
        }

        return VboVertices;
    }

    public Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles)
    {
        Vector3 dir = point - pivot; // get point direction relative to pivot
        dir = Quaternion.Euler(angles) * dir; // rotate it
        point = dir + pivot; // calculate rotated point
        return point; // return it
    }
    */

}
