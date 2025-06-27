using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Audio2Face
{
    public class BlendShapeHandler : ScriptableObject
    {
        private enum HandleType
        {
            OneValueSlider = 1,
            TwoValueSlider = 2,
            ThreeValueRect = 3,
            FourValueRect = 4,
        }

        private static BlendShapeHandler s_ControllingHandle;
        public static bool TryReplaceValuesWithControl(BlendShapeHandler handler)
        {
            if (handler != s_ControllingHandle &&
                handler.m_handleType == s_ControllingHandle.m_handleType)
            {
                for (int i = 0; i < s_ControllingHandle.m_values.Length; i++)
                    handler.SetValue(s_ControllingHandle.m_values[i], i);
                return true;
            }
            return false;
        }
        
        private const float kSelectedScale = 1.2f;
        private const float kHoverScale = 1.1f;
        private const float kDefaultHandleSize = 2.5f;
        
        [SerializeField] private HandleType m_handleType = HandleType.OneValueSlider;
        [SerializeField] private Vector2 m_oriStartPoint = Vector2.zero;
        [SerializeField] private Vector2 m_oriEndPoint = Vector2.zero;
        [SerializeField] private float m_handleSize;
        [SerializeField] private List<string> m_boneIds = new List<string>();
        [SerializeField] private float[] m_values;
        [SerializeField] private bool m_isSelected;
        
        // 非序列化字段，仅用于运行时
        [NonSerialized] private Vector2 m_startPoint;
        [NonSerialized] private Vector2 m_endPoint;
        [NonSerialized] private bool m_isHovering;
        [NonSerialized] private bool m_isDragging;
        
        // 事件
        public event Action<BlendShapeHandler, float[]> OnResetHandler;
        public event Action<BlendShapeHandler, float[]> OnDragHandler;
        public event Action<BlendShapeHandler, bool> OnClickHandler;
        
        public string[] BoneIds => m_boneIds.ToArray();
        public float[] Values => m_values;
        
        public bool IsSelected
        {
            get { return m_isSelected; }
            set { m_isSelected = value; }
        }

        // 初始化方法
        public static BlendShapeHandler Create(string boneId, Vector2 startPoint, Vector2 endPoint)
        {
            var handler = CreateInstance<BlendShapeHandler>();
            handler.m_boneIds = new List<string> { boneId };
            handler.m_values = new float[1];
            handler.m_handleType = HandleType.OneValueSlider;
            handler.m_oriStartPoint = startPoint;
            handler.m_oriEndPoint = endPoint;
            handler.m_startPoint = startPoint;
            handler.m_endPoint = endPoint;
            handler.m_handleSize = kDefaultHandleSize;
            handler.hideFlags = HideFlags.HideAndDontSave;
            return handler;
        }

        public static BlendShapeHandler Create(string[] boneIds, Vector2 startPoint, Vector2 endPoint)
        {
            var handler = CreateInstance<BlendShapeHandler>();
            handler.m_boneIds = boneIds.ToList();
            handler.m_values = new float[boneIds.Length];
            handler.m_handleType = (HandleType)boneIds.Length;
            handler.m_oriStartPoint = startPoint;
            handler.m_oriEndPoint = endPoint;
            handler.m_startPoint = startPoint;
            handler.m_endPoint = endPoint;
            handler.m_handleSize = kDefaultHandleSize;
            handler.hideFlags = HideFlags.HideAndDontSave;
            return handler;
        }

        public void Draw(float scale, Rect drawRect)
        {
            m_handleSize = kDefaultHandleSize * scale;
            m_startPoint = m_oriStartPoint * scale + drawRect.center;
            m_endPoint = m_oriEndPoint * scale + drawRect.center;
            Handles.BeginGUI();
            if (m_handleType == HandleType.OneValueSlider || m_handleType == HandleType.TwoValueSlider)
                DrawSliderHandle();
            else if (m_handleType == HandleType.ThreeValueRect || m_handleType == HandleType.FourValueRect)
                DrawRectHandle();
            Handles.EndGUI();
        }

        public Vector2 GetHandlePosition(float scale, Rect drawRect)
        {
            Vector2 scaledStartPoint = m_oriStartPoint * scale + drawRect.center;
            Vector2 scaledEndPoint = m_oriEndPoint * scale + drawRect.center;
            return GetHandlePositionByValue(scaledStartPoint, scaledEndPoint, m_values);
        }

        public void SetValue(float value, int index = 0)
        {
            if (index < 0 || index >= m_values.Length)
            {
                Debug.LogWarning("Index out of range, set handler value failed.");
                return;
            }
            
            // 记录 Undo
            Undo.RecordObject(this, "Change BlendShape Value");
            
            m_values[index] = value;
        }

        // 其余方法保持不变...
        
        private void DrawCrossByMode()
        {
            var centerPoint = (m_startPoint + m_endPoint) * 0.5f;
            switch (m_handleType)
            {
                case HandleType.OneValueSlider:
                    DrawCross(m_startPoint);
                    break;
                case HandleType.TwoValueSlider:
                case HandleType.FourValueRect:
                    DrawCross(centerPoint);
                    break;
                case HandleType.ThreeValueRect:
                    DrawCross(new Vector2(centerPoint.x, m_startPoint.y));
                    break;
            }
        }
        
        private void DrawCross(Vector2 point)
        {
            Handles.color = new Color(0.7f, 0.7f, 0.7f, 0.8f);
            float crossSize = m_handleSize * 0.8f;
            float discSize = m_handleSize * 0.3f;
            
            Handles.DrawLine(
                new Vector3(point.x - crossSize, point.y, 0),
                new Vector3(point.x + crossSize, point.y, 0),
                0.5f
            );
            
            Handles.DrawLine(
                new Vector3(point.x, point.y - crossSize, 0),
                new Vector3(point.x, point.y + crossSize, 0),
                0.5f
            );
            
            Handles.DrawSolidDisc(point, Vector3.forward, discSize);
        }
        
        private void DrawHandleDisc()
        {
            Vector2 handlePos = GetHandlePositionByValue(m_startPoint, m_endPoint, m_values);
            Color handleColor = Color.white;
            float curHandleSize = m_handleSize;
            if (m_isSelected)
            {
                curHandleSize = m_handleSize * kSelectedScale;
                handleColor = Color.yellow;
            }
            else if (m_isHovering)
            {
                curHandleSize = m_handleSize * kHoverScale;
                handleColor = new Color(1f, 0.9f, 0.5f);
            }

            Handles.color = handleColor;
            Handles.DrawSolidDisc(handlePos, Vector3.forward, curHandleSize);
        }

        private void DrawSliderHandle()
        {
            DrawCrossByMode();
            DrawHandleDisc();
            Handles.color = Color.white;
            Handles.DrawLine(m_startPoint, m_endPoint);
        }

        private void DrawRectHandle()
        {
            DrawCrossByMode();
            DrawHandleDisc();
            Handles.color = Color.white;
            HandlesUtils.DrawRect(m_startPoint, m_endPoint);
        }

        public void HandleEvent(Event evt, Rect controlRect)
        {
            if (!controlRect.Contains(evt.mousePosition))
                return;

            Vector2 handlePos = GetHandlePositionByValue(m_startPoint, m_endPoint, m_values);
            float distanceToHandle = Vector2.Distance(evt.mousePosition, handlePos);
            m_isHovering = distanceToHandle < m_handleSize;

            switch (evt.type)
            {
                case EventType.MouseDown:
                    if (evt.button == 0)
                    {
                        float curHandleSize = m_handleSize;
                        if (m_isSelected)
                            curHandleSize = m_handleSize * kSelectedScale;
                        else if (m_isHovering)
                            curHandleSize = m_handleSize * kHoverScale;
                        bool clickedHandle = distanceToHandle < curHandleSize;
                        bool clickedLine = HandleUtility.DistancePointToLineSegment(evt.mousePosition, m_startPoint, m_endPoint) < 5f;
                        if (clickedHandle || clickedLine)
                        {
                            s_ControllingHandle = this;
                            m_isDragging = true;
                            OnClickHandler?.Invoke(this, evt.control);
                            evt.Use();
                            GUI.changed = true;
                        }
                    }
                    break;

                case EventType.MouseDrag:
                    if (evt.button == 0)
                    {
                        if (m_isSelected && m_isDragging)
                        {
                            // 记录 Undo
                            Undo.RecordObject(this, "Change BlendShape Value");
                            
                            if(s_ControllingHandle == this)
                                UpdateValueFromMousePosition(evt.mousePosition);
                            OnDragHandler?.Invoke(this, m_values);
                            GUI.changed = true;
                        }
                    }
                    break;
                    
                case EventType.MouseUp:
                    if (evt.button == 0 && m_isDragging)
                    {
                        m_isDragging = false;
                        evt.Use();
                    }
                    break;
                    
                case EventType.KeyDown:
                    if (evt.keyCode == KeyCode.R && m_isSelected)
                    {
                        // 记录 Undo
                        Undo.RecordObject(this, "Reset BlendShape Value");
                        
                        for (int i = 0; i < m_values.Length; i++)
                            m_values[i] = 0f;
                        OnResetHandler?.Invoke(this, m_values);
                        GUI.changed = true;
                    }
                    break;
            }
        }
        
        public Vector2 GetHandlePositionByValue(Vector2 startPoint, Vector2 endPoint, float[] value)
        {
            Vector2 handlePos = Vector2.zero;
            Vector2 centerPoint = (endPoint + startPoint) * 0.5f;
            if (m_handleType == HandleType.OneValueSlider)
                handlePos = Vector2.Lerp(startPoint, endPoint, value[0]);
            else if (m_handleType == HandleType.TwoValueSlider)
            {
                float toStartOffset = Mathf.Lerp(centerPoint.x, startPoint.x, value[0]) - centerPoint.x;
                float toEndOffset = Mathf.Lerp(centerPoint.x, endPoint.x, value[1]) - centerPoint.x;
                handlePos = new Vector2(centerPoint.x + toStartOffset + toEndOffset, centerPoint.y);
            }
            else if (m_handleType == HandleType.ThreeValueRect)
            {
                float yPos = Mathf.Lerp(startPoint.y, endPoint.y, value[0]);
                float toStartOffset = Mathf.Lerp(centerPoint.x, startPoint.x, value[1]) - centerPoint.x;
                float toEndOffset = Mathf.Lerp(centerPoint.x, endPoint.x, value[2]) -  centerPoint.x;
                handlePos = new Vector2(centerPoint.x + toStartOffset + toEndOffset, yPos);
            }
            else if (m_handleType == HandleType.FourValueRect)
            {
                float toStartOffsetX = Mathf.Lerp(centerPoint.x, startPoint.x, value[0]) - centerPoint.x;
                float toEndOffsetX = Mathf.Lerp(centerPoint.x, endPoint.x, value[1]) - centerPoint.x;
                float toStartOffsetY = Mathf.Lerp(centerPoint.y, startPoint.y, value[2]) - centerPoint.y;
                float toEndOffsetY = Mathf.Lerp(centerPoint.y, endPoint.y, value[3]) - centerPoint.y;
                handlePos = new Vector2(centerPoint.x + toStartOffsetX + toEndOffsetX, centerPoint.y + toStartOffsetY + toEndOffsetY);
            }
            
            return handlePos;
        }

        public void UpdateValueFromMousePosition(Vector2 mousePosition)
        {
            // 记录 Undo
            Undo.RecordObject(this, "Change BlendShape Value");
            
            Vector2 centerPoint = (m_endPoint + m_startPoint) * 0.5f;
            Vector2 mouseToCenter = mousePosition - centerPoint;
            Vector2 line = m_endPoint - m_startPoint;
            Vector2 size = new Vector2(Mathf.Abs(line.x), Mathf.Abs(line.y));
            Vector2 offset = mouseToCenter / (size * 0.5f);
            if (m_handleType == HandleType.OneValueSlider)
            {
                float projection = Vector2.Dot(mousePosition - m_startPoint, line) / line.sqrMagnitude;
                m_values[0] = Mathf.Clamp01(projection);
            }
            else if (m_handleType == HandleType.TwoValueSlider)
            {
                m_values[0] = -Mathf.Clamp(offset.x, -1f, 0f);
                m_values[1] = Mathf.Clamp(offset.x, 0f, 1f);
            }
            else if (m_handleType == HandleType.ThreeValueRect)
            {
                m_values[0] = (mousePosition - m_startPoint).y / size.y;
                m_values[1] = -Mathf.Clamp(offset.x, -1f, 0f);
                m_values[2] = Mathf.Clamp(offset.x, 0f, 1f);
            }
            else if (m_handleType == HandleType.FourValueRect)
            {
                m_values[0] = -Mathf.Clamp(offset.x, -1f, 0f);
                m_values[1] = Mathf.Clamp(offset.x, 0f, 1f);
                m_values[2] = -Mathf.Clamp(offset.y, -1f, 0f);
                m_values[3] = Mathf.Clamp(offset.y, 0f, 1f);
            }
            GUI.changed = true;
        }
    }
}