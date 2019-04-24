using UnityEngine;

namespace Catneep.Utils
{
    public static class DebugUtils
    {

        public static void Draw2DXGizmo(Vector2 position, float size)
        {
            Gizmos.DrawLine(position + new Vector2(-size, size), position + new Vector2(size, -size));
            Gizmos.DrawLine(position + new Vector2(size, size), position + new Vector2(-size, -size));
        }

    }
}
