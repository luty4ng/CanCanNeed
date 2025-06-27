using UnityEngine;
using UnityEditor;

namespace Audio2Face
{
    public static class HandlesUtils
    {
        public static void DrawRect(Vector2 minPos, Vector2 maxPos, float thickness = 1f)
        {
            Vector2 topLeft = new Vector2(minPos.x, maxPos.y);
            Vector2 topRight = new Vector2(maxPos.x, maxPos.y);
            Vector2 bottomRight = new Vector2(maxPos.x, minPos.y);
            Vector2 bottomLeft = new Vector2(minPos.x, minPos.y);
            Handles.DrawLine(topLeft, bottomLeft, thickness);
            Handles.DrawLine(topRight, bottomRight, thickness);
            Handles.DrawLine(topLeft, topRight, thickness);
            Handles.DrawLine(bottomLeft, bottomRight, thickness);
        }

        public static void DrawEllipse(Vector3 center, float width, float height, int segments)
        {
            Vector3[] points = new Vector3[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * 2f * Mathf.PI;
                points[i] = center + new Vector3(Mathf.Cos(angle) * width, Mathf.Sin(angle) * height, 0f);
            }
            Handles.DrawAAPolyLine(points);
        }

        public static void DrawRoundRect(Vector3 center, float width, float height, float radius, float thickness = 1f)
        {
            Vector3 topLeft = new Vector3(center.x - width / 2 + radius, center.y + height / 2, center.z);
            Vector3 topRight = new Vector3(center.x + width / 2 - radius, center.y + height / 2, center.z);
            Vector3 bottomRight = new Vector3(center.x + width / 2 - radius, center.y - height / 2, center.z);
            Vector3 bottomLeft = new Vector3(center.x - width / 2 + radius, center.y - height / 2, center.z);

            Handles.DrawWireArc(topLeft, Vector3.forward, Vector3.up, 90, radius, thickness);
            Handles.DrawWireArc(topRight, Vector3.forward, Vector3.right, 90, radius, thickness);
            Handles.DrawWireArc(bottomRight, Vector3.forward, Vector3.down, 90, radius, thickness);
            Handles.DrawWireArc(bottomLeft, Vector3.forward, Vector3.left, 90, radius, thickness);

            Handles.DrawLine(topLeft + Vector3.left * radius, bottomLeft + Vector3.left * radius, thickness);
            Handles.DrawLine(topRight + Vector3.right * radius, bottomRight + Vector3.right * radius, thickness);
            Handles.DrawLine(topLeft + Vector3.up * radius, topRight + Vector3.up * radius, thickness);
            Handles.DrawLine(bottomLeft + Vector3.down * radius, bottomRight + Vector3.down * radius, thickness);
        }
    }
}