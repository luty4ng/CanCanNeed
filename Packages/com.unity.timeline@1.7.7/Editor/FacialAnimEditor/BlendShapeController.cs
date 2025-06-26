﻿using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Audio2Face
{
    public class BlendShapeController
    {
        private static List<List<string>> s_groupShapes = new List<List<string>>()
        {
            new List<string>() { "mouthLeft", "mouthRight" },
            new List<string>() { "jawOpen", "jawLeft", "jawRight" },
            new List<string>() { "eyeLookOutRight", "eyeLookInRight", "eyeLookUpRight", "eyeLookDownRight" },
            new List<string>() { "eyeLookOutLeft", "eyeLookInLeft", "eyeLookUpLeft", "eyeLookDownLeft" },
        };

        private readonly List<BlendShapeHandler> m_handlers;
        private readonly List<BlendShapeHandler> m_selectedHandlers;
        private readonly List<FacialLayoutRect> m_rectsLayout;
        private readonly List<FacialLayoutEllipse> m_ellipsesLayout;
        private readonly List<FacialLayoutSlider> m_slidersLayout;

        public Action<BlendShapeHandler[]> OnSelectedHandlerChanged;
        public Action<BlendShapeHandler[], float[][]> OnSelectedHandlerValuesChanged;
        private Rect m_rect;

        // Rect Selection
        private bool m_isRectSelecting;
        private float m_minWidth;
        private float m_minHeight;
        private Rect m_rectSelection;
        private readonly Color m_colorSelection = new Color(0.3f, 0.5f, 0.8f, 0.3f);
        private readonly Color m_colorSelectionBorder = new Color(0.3f, 0.5f, 0.8f, 0.8f);
        
        // 用于记录选择状态的 ScriptableObject
        private SelectionState m_selectionState;
        
        public BlendShapeController()
        {
            // 创建选择状态对象
            m_selectionState = ScriptableObject.CreateInstance<SelectionState>();
            m_selectionState.hideFlags = HideFlags.HideAndDontSave;
            
            string path = $"{Application.streamingAssetsPath}/Audio2Face/facial_layout.json";
            try
            {
                string json = File.ReadAllText(path);
                FacialUISettings cStyle = JsonConvert.DeserializeObject<FacialUISettings>(json);
                m_rectsLayout = cStyle.rects;
                m_slidersLayout = cStyle.lines;
                m_ellipsesLayout = cStyle.ellipses;
                m_handlers = new List<BlendShapeHandler>();
                m_selectedHandlers = new List<BlendShapeHandler>();

                var groupSliders = new Dictionary<int, List<FacialLayoutSlider>>();
                for (int i = 0; i < s_groupShapes.Count; i++)
                    groupSliders[i] = new List<FacialLayoutSlider>();
                
                for (int i = 0; i < m_slidersLayout.Count; i++)
                {
                    bool inGroup = false;
                    var currentSlider = m_slidersLayout[i];
                    for (int groupIndex = 0; groupIndex < s_groupShapes.Count; groupIndex++)
                    {
                        if (s_groupShapes[groupIndex].Contains(currentSlider.bindingId))
                        {
                            groupSliders[groupIndex].Add(currentSlider);
                            inGroup = true;
                            break; // 假设一个滑块只属于一个组
                        }
                    }

                    // 如果不在任何组中，创建单独的Handler
                    if (!inGroup)
                    {
                        var handler = BlendShapeHandler.Create(
                            currentSlider.bindingId,
                            currentSlider.startPosition,
                            currentSlider.endPosition
                        );
                        handler.OnClickHandler += OnHandlerSelected;
                        handler.OnDragHandler += OnHandlerValuesChanged;
                        handler.OnResetHandler += OnHandlerValuesChanged;
                        m_handlers.Add(handler);
                    }
                }

                // 为每个组创建Handler
                for (int groupIndex = 0; groupIndex < s_groupShapes.Count; groupIndex++)
                {
                    var slidersInGroup = groupSliders[groupIndex];
                    if (slidersInGroup.Count > 0)
                    {
                        // 计算组内所有滑块的最大边界框
                        Vector2 minPoint = slidersInGroup[0].startPosition;
                        Vector2 maxPoint = slidersInGroup[0].endPosition;
                        foreach (var slider in slidersInGroup)
                        {
                            minPoint.x = Mathf.Min(minPoint.x, Mathf.Min(slider.startPosition.x, slider.endPosition.x));
                            minPoint.y = Mathf.Min(minPoint.y, Mathf.Min(slider.startPosition.y, slider.endPosition.y));
                            maxPoint.x = Mathf.Max(maxPoint.x, Mathf.Max(slider.startPosition.x, slider.endPosition.x));
                            maxPoint.y = Mathf.Max(maxPoint.y, Mathf.Max(slider.startPosition.y, slider.endPosition.y));
                        }

                        // 创建组合Handler，包含多个BoneId和Value
                        var handler = BlendShapeHandler.Create(
                            s_groupShapes[groupIndex].ToArray(),
                            minPoint,
                            maxPoint
                        );
                        handler.OnClickHandler += OnHandlerSelected;
                        handler.OnDragHandler += OnHandlerValuesChanged;
                        handler.OnResetHandler += OnHandlerValuesChanged;
                        m_handlers.Add(handler);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw;
            }
        }

        ~BlendShapeController()
        {
            // 清理资源
            if (m_selectionState != null)
                ScriptableObject.DestroyImmediate(m_selectionState);
                
            foreach (var handler in m_handlers)
            {
                if (handler != null)
                    ScriptableObject.DestroyImmediate(handler);
            }
            
            m_handlers.Clear();
            m_rectsLayout.Clear();
            m_slidersLayout.Clear();
            m_ellipsesLayout.Clear();
        }

        public void SetHandleValue(Dictionary<string, float> boneValues)
        {
            // 对每个 handler 记录 Undo
            foreach (var handler in m_handlers)
            {
                bool needsUndo = false;
                for (int j = 0; j < handler.BoneIds.Length; j++)
                {
                    string boneId = handler.BoneIds[j];
                    if (boneValues.TryGetValue(boneId, out float value))
                    {
                        needsUndo = true;
                        break;
                    }
                }
                
                if (needsUndo)
                    Undo.RecordObject(handler, "Set BlendShape Values");
            }
            
            // 设置值
            for (int i = 0; i < m_handlers.Count; i++)
            {
                for (int j = 0; j < m_handlers[i].BoneIds.Length; j++)
                {
                    string boneId = m_handlers[i].BoneIds[j];
                    if (boneValues.TryGetValue(boneId, out float value))
                        m_handlers[i].SetValue(Mathf.Clamp01(value), j);
                }
            }
        }

        public void UnSelectAllHandlers() => OnHandlerUnSelected();

        public void Draw(float minWidth, float minHeight, params GUILayoutOption[] options)
        {
            m_rect = EditorGUILayout.BeginVertical(options);
            m_minWidth = minWidth;
            m_minHeight = minHeight;
            EditorGUI.DrawRect(m_rect, new Color(0.2f, 0.2f, 0.2f));
            float scaleX = m_rect.width / minWidth;
            float scaleY = m_rect.height / minHeight;
            float scale = Mathf.Min(scaleX, scaleY);

            for (int i = 0; i < m_ellipsesLayout.Count; i++)
            {
                var ellipse = m_ellipsesLayout[i];
                Handles.color = Color.white;
                HandlesUtils.DrawEllipse(
                    ellipse.center * scale + m_rect.center,
                    ellipse.width * scale,
                    ellipse.height * scale,
                    ellipse.segments);
            }

            for (int i = 0; i < m_rectsLayout.Count; i++)
            {
                var fRect = m_rectsLayout[i];
                Handles.color = Color.white;
                HandlesUtils.DrawRoundRect(
                    fRect.center * scale + m_rect.center,
                    fRect.width * scale,
                    fRect.height * scale,
                    fRect.radius * scale);
            }

            EditorGUILayout.EndVertical();

            Event e = Event.current;
            
            DrawSelectRect();
            for (int i = 0; i < m_handlers.Count; i++)
            {
                m_handlers[i].Draw(scale, m_rect);
                m_handlers[i].HandleEvent(e, m_rect);
            }
            HandleEvents(e, m_rect);
        }
        
        private void HandleEvents(Event e, Rect rect)
        {
            if (!rect.Contains(e.mousePosition))
                return;

            if (e.button == 0)
            {
                if (e.type == EventType.MouseDown)
                {
                    if (!e.control)
                        OnHandlerUnSelected();
                    
                    if (e.shift)
                    {
                        m_isRectSelecting = true;
                        m_rectSelection = new Rect(e.mousePosition, Vector2.zero);
                    }
                }
                else if (e.type == EventType.MouseDrag)
                {
                    if (m_isRectSelecting)
                    {
                        float width = e.mousePosition.x - m_rectSelection.x;
                        float height = e.mousePosition.y - m_rectSelection.y;
                        m_rectSelection.width = width;
                        m_rectSelection.height = height;
                        e.Use();
                        GUI.changed = true;
                    }
                }
                else if (e.type == EventType.MouseUp)
                {
                    if (m_isRectSelecting)
                    {
                        m_isRectSelecting = false;
                        OnRectSelection(m_rectSelection, e.shift);
                        e.Use();
                        GUI.changed = true;
                    }
                }
            }
        }

        private void OnRectSelection(Rect rectSelectionArea, bool isAdditive)
        {
            // 记录选择状态用于 Undo
            Undo.RecordObject(m_selectionState, "Change Selection");
            m_selectionState.SelectedHandlers = new List<BlendShapeHandler>(m_selectedHandlers);
            
            Rect normalizedRect = new Rect(
                Mathf.Min(rectSelectionArea.x, rectSelectionArea.x + rectSelectionArea.width),
                Mathf.Min(rectSelectionArea.y, rectSelectionArea.y + rectSelectionArea.height),
                Mathf.Abs(rectSelectionArea.width),
                Mathf.Abs(rectSelectionArea.height)
            );

            if (!isAdditive)
            {
                // 记录所有将被取消选择的 handler
                foreach (var handler in m_selectedHandlers)
                {
                    Undo.RecordObject(handler, "Change Selection");
                }
                
                m_selectedHandlers.ForEach(s => s.IsSelected = false);
                m_selectedHandlers.Clear();
            }

            float scale = Mathf.Min(m_rect.width / m_minWidth, m_rect.height / m_minHeight);
            foreach (var handler in m_handlers)
            {
                Vector2 handlePos = handler.GetHandlePosition(scale, m_rect);
                if (normalizedRect.Contains(handlePos))
                {
                    if (!m_selectedHandlers.Contains(handler))
                    {
                        // 记录将被选择的 handler
                        Undo.RecordObject(handler, "Change Selection");
                        handler.IsSelected = true;
                        m_selectedHandlers.Add(handler);
                    }
                }
            }

            if (m_selectedHandlers.Count > 0)
                OnSelectedHandlerChanged?.Invoke(m_selectedHandlers.ToArray());
        }

        private void DrawSelectRect()
        {
            if (m_isRectSelecting)
            {
                EditorGUI.DrawRect(m_rectSelection, m_colorSelection);
                Handles.color = m_colorSelectionBorder;
                Handles.DrawPolyLine(
                    new Vector3(m_rectSelection.x, m_rectSelection.y),
                    new Vector3(m_rectSelection.x + m_rectSelection.width, m_rectSelection.y),
                    new Vector3(m_rectSelection.x + m_rectSelection.width, m_rectSelection.y + m_rectSelection.height),
                    new Vector3(m_rectSelection.x, m_rectSelection.y + m_rectSelection.height),
                    new Vector3(m_rectSelection.x, m_rectSelection.y)
                );
            }
        }

        private void OnHandlerSelected(BlendShapeHandler handler, bool keepSelected)
        {
            if (handler != null)
            {
                // 记录选择状态用于 Undo
                Undo.RecordObject(m_selectionState, "Change Selection");
                m_selectionState.SelectedHandlers = new List<BlendShapeHandler>(m_selectedHandlers);
                
                if (!keepSelected)
                {
                    if (!handler.IsSelected)
                    {
                        // 记录所有 handler 的选择状态
                        Undo.RecordObject(handler, "Change Selection");
                        foreach (var h in m_selectedHandlers)
                        {
                            Undo.RecordObject(h, "Change Selection");
                        }
                        
                        handler.IsSelected = true;
                        m_selectedHandlers.ForEach(s => s.IsSelected = false);
                        m_selectedHandlers.Clear();
                        m_selectedHandlers.Add(handler);
                    }
                }
                else
                {
                    // 记录当前 handler 的选择状态
                    Undo.RecordObject(handler, "Change Selection");
                    
                    handler.IsSelected = !handler.IsSelected;
                    if (handler.IsSelected && !m_selectedHandlers.Contains(handler))
                        m_selectedHandlers.Add(handler);
                    else if (!handler.IsSelected && m_selectedHandlers.Contains(handler))
                        m_selectedHandlers.Remove(handler);
                }
                OnSelectedHandlerChanged?.Invoke(m_selectedHandlers.ToArray());
            }
        }
        
        private void OnHandlerUnSelected()
        {
            if (m_selectedHandlers.Count > 0)
            {
                // 记录选择状态用于 Undo
                Undo.RecordObject(m_selectionState, "Clear Selection");
                m_selectionState.SelectedHandlers = new List<BlendShapeHandler>(m_selectedHandlers);
                
                // 记录所有将被取消选择的 handler
                foreach (var handler in m_selectedHandlers)
                {
                    Undo.RecordObject(handler, "Clear Selection");
                }
                
                m_selectedHandlers.ForEach(s => s.IsSelected = false);
                m_selectedHandlers.Clear();
            }
            OnSelectedHandlerChanged?.Invoke(m_selectedHandlers.ToArray());
        }

        private void OnHandlerValuesChanged(BlendShapeHandler handler, float[] values)
        {
            if (m_selectedHandlers != null)
            {
                float[][] valuesMap = new float[m_selectedHandlers.Count][];
                for (int i = 0; i < m_selectedHandlers.Count; i++)
                {
                    BlendShapeHandler.TryReplaceValuesWithControl(m_selectedHandlers[i]);
                    valuesMap[i] = m_selectedHandlers[i].Values;
                }
                OnSelectedHandlerValuesChanged?.Invoke(m_selectedHandlers.ToArray(), valuesMap);
            }
        }
    }
    
    // 用于记录选择状态的类
    public class SelectionState : ScriptableObject
    {
        public List<BlendShapeHandler> SelectedHandlers = new List<BlendShapeHandler>();
    }
}