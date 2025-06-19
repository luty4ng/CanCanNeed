using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;

public class CurveEditorWrapper
{
    private CurveEditor m_CurveEditor;
    private List<CurveWrapper> m_CurveWrappers = new List<CurveWrapper>();
    private bool[] m_SelectedCurves = new bool[0];

    private static readonly Color kRedCurveColor = new Color(0.9f, 0.3f, 0.2f, 1f);
    private static readonly Color kGreenCurveColor = new Color(0.25f, 0.7f, 0.2f, 1f);
    private static readonly Color kBlueCurveColor = new Color(0.25f, 0.55f, 0.95f, 1f);
    private static readonly Color kPurpleCurveColor = new Color(0.8f, 0.25f, 0.9f, 1f);
    private static readonly Color kYellowCurveColor = new Color(0.7f, 0.7f, 0.2f, 1f);

    private float currentTime = 0f;
    private const float timeLineWidth = 2f;
    private Color timeLineColor = new Color(1f, 0.3f, 0.3f, 0.8f);
    private bool isDraggingTimeline = false;
    private Rect timeSliderRect;
    private const float timeSliderHeight = 15f;

    private int instanceId;
    private static int activeInstanceId = -1;
    private static bool isAnyInstanceDragging = false;

    // Undo支持相关
    private AnimationClip m_CurvesContainer;
    private Dictionary<int, EditorCurveBinding> m_CurveBindings = new Dictionary<int, EditorCurveBinding>();
    private bool m_IsRegisteredForUndo = false;
    private bool m_WasInLiveEdit = false;

    public Rect EditorRect => m_CurveEditor.rect;
    public Rect DrawRect => m_CurveEditor.drawRect;
    public bool InLiveEdit => m_CurveEditor.InLiveEdit();
    public bool IsActive => instanceId == activeInstanceId;

    public float CurrentTime
    {
        get => currentTime;
        set
        {
            if (!Mathf.Approximately(currentTime, value))
            {
                currentTime = Mathf.Clamp01(value);
                OnTimeChanged?.Invoke(currentTime);
            }
        }
    }

    public event Action OnTimeSliderClicked;
    public event Action<float> OnTimeChanged;

    public CurveEditorWrapper()
    {
        instanceId = GetHashCode();
        m_CurvesContainer = new AnimationClip();
        m_CurvesContainer.name = "CurveEditorContainer";
        CurveEditorSettings settings = new CurveEditorSettings()
        {
            hRangeMin = 0.0f,
            vRangeMin = 0.0f,
            vRangeMax = 1.1f,
            hRangeMax = 1f,
            vSlider = false,
            hSlider = false,
            undoRedoSelection = true
        };
        settings.hTickStyle = new TickStyle()
        {
            tickColor = { color = new Color(0.0f, 0.0f, 0.0f, 0.15f) },
            distLabel = 30
        };

        settings.vTickStyle = new TickStyle()
        {
            tickColor = { color = new Color(0.0f, 0.0f, 0.0f, 0.15f) },
            distLabel = 20
        };
        settings.rectangleToolFlags = CurveEditorSettings.RectangleToolFlags.FullRectangleTool;
        m_CurveEditor = new CurveEditor(new Rect(0.0f, 0.0f, 1000f, 100f), new CurveWrapper[0], false);
        m_CurveEditor.settings = settings;
        m_CurveEditor.margin = 25f;
        m_CurveEditor.SetShownHRangeInsideMargins(0.0f, 1f);
        m_CurveEditor.SetShownVRangeInsideMargins(0.0f, 1.1f);
        m_CurveEditor.FrameSelected(true, true);
        m_CurveEditor.curvesUpdated = CheckCurveEditState;
        Undo.undoRedoEvent += UndoRedoPerformed;
    }

    public void OnDisable()
    {
        if (isDraggingTimeline && IsActive)
        {
            isDraggingTimeline = false;
            isAnyInstanceDragging = false;
        }

        m_CurveEditor.OnDisable();
        m_CurvesContainer = null;
        Undo.undoRedoEvent -= UndoRedoPerformed;
    }

    private void UndoRedoPerformed(in UndoRedoInfo info)
    {
        ContainerToWrapper();
    }

    private void CheckCurveEditState()
    {
        bool isInLiveEdit = InLiveEdit;
        if (isInLiveEdit && !m_WasInLiveEdit)
        {
            WrapperToContainer();
            RegisterUndo("Edit Curve");
            m_IsRegisteredForUndo = true;
        }
        else if (!isInLiveEdit && m_WasInLiveEdit)
        {
            WrapperToContainer();
            m_IsRegisteredForUndo = false;
        }

        m_WasInLiveEdit = isInLiveEdit;
    }

    private void RegisterUndo(string undoName)
    {
        Undo.RegisterCompleteObjectUndo(m_CurvesContainer, undoName);
    }

    private void ContainerToWrapper()
    {
        foreach (var binding in m_CurveBindings)
        {
            int curveId = binding.Key;
            EditorCurveBinding curveBinding = binding.Value;

            AnimationCurve containerCurve = AnimationUtility.GetEditorCurve(m_CurvesContainer, curveBinding);
            if (containerCurve == null)
            {
                Debug.Log("CurveEditorWrapper.ContainerToWrapper: containerCurve is null");
                continue;
            }

            CurveWrapper wrapper = m_CurveEditor.GetCurveWrapperFromID(curveId);
            if (wrapper == null)
            {
                Debug.Log($"CurveEditorWrapper.ContainerToWrapper: wrapper {curveId}  is null");
                continue;
            }

            AnimationCurve wrapperCurve = wrapper.renderer.GetCurve();
            if (wrapperCurve != null)
            {
                wrapperCurve.ClearKeys();
                wrapperCurve.keys = containerCurve.keys;
            }

            wrapper.renderer.FlushCache();
        }
    }

    private void WrapperToContainer()
    {
        foreach (var binding in m_CurveBindings)
        {
            int curveId = binding.Key;
            EditorCurveBinding curveBinding = binding.Value;
            AnimationCurve wrapperCurve = GetWrapperCurve(curveId);
            AnimationUtility.SetEditorCurve(m_CurvesContainer, curveBinding, wrapperCurve);
        }

        EditorUtility.SetDirty(m_CurvesContainer);
    }

    public void UpdateCurveEditor()
    {
        if (m_CurveEditor.InLiveEdit())
            return;
        m_CurveEditor.animationCurves = m_CurveWrappers.ToArray();
    }

    public void AddCurve(AnimationCurve curve, string name, Color color, int curveId)
    {
        if (curve == null || curve.length == 0)
        {
            Debug.LogError($"曲线 {name} 为空或没有关键帧！");
            return;
        }

        EditorCurveBinding binding = new EditorCurveBinding
        {
            path = "",
            propertyName = name,
            type = typeof(Transform)
        };
        m_CurveBindings[curveId] = binding;
        AnimationUtility.SetEditorCurve(m_CurvesContainer, binding, curve);

        CurveWrapper curveWrapper = new CurveWrapper
        {
            id = curveId,
            groupId = -1,
            color = color * (EditorGUIUtility.isProSkin ? 1f : 0.9f),
            hidden = false,
            readOnly = false,
            renderer = new NormalCurveRenderer(curve),
            useScalingInKeyEditor = true,
            xAxisLabel = "X轴",
            yAxisLabel = name
        };
        curveWrapper.renderer.SetCustomRange(0.0f, 1f);
        m_CurveWrappers.Add(curveWrapper);

        UpdateCurveEditor();

        ResizeSelectedCurves();
    }

    public void ClearCurves()
    {
        if (m_CurvesContainer != null)
        {
            AnimationClip emptyClip = new AnimationClip();
            EditorUtility.CopySerialized(emptyClip, m_CurvesContainer);
            UnityEngine.Object.DestroyImmediate(emptyClip);
            EditorUtility.SetDirty(m_CurvesContainer);
        }

        m_SelectedCurves = new bool[0];
        m_CurveBindings.Clear();
        m_CurveWrappers.Clear();
        UpdateCurveEditor();
    }

    public void FrameSelected()
    {
        m_CurveEditor.FrameSelected(true, true);
    }

    private void ResizeSelectedCurves()
    {
        if (m_CurveWrappers.Count != m_SelectedCurves.Length)
        {
            bool[] newArray = new bool[m_CurveWrappers.Count];
            for (int i = 0; i < newArray.Length; i++)
            {
                newArray[i] = i < m_SelectedCurves.Length ? m_SelectedCurves[i] : true;
            }

            m_SelectedCurves = newArray;
            SyncSelectedCurvesToEditor();
        }
    }

    private void SyncSelectedCurvesToEditor()
    {
        for (int i = 0; i < m_CurveWrappers.Count; i++)
        {
            CurveWrapper wrapper = m_CurveEditor.GetCurveWrapperFromID(m_CurveWrappers[i].id);
            if (wrapper != null)
            {
                wrapper.hidden = !m_SelectedCurves[i];
            }
        }

        m_CurveEditor.animationCurves = m_CurveEditor.animationCurves;
    }

    public void OnGUI(Rect rect)
    {
        Event e = Event.current;
        bool isMouseOverEditor = rect.Contains(e.mousePosition);

        if (isMouseOverEditor && e.type == EventType.MouseDown && !isAnyInstanceDragging)
        {
            activeInstanceId = instanceId;
        }

        if (e.type != EventType.Layout && e.type != EventType.Used)
            m_CurveEditor.rect = new Rect(rect.x, rect.y, rect.width, rect.height);

        GUI.Label(m_CurveEditor.drawRect, GUIContent.none, "TextField");

        if (isMouseOverEditor || IsActive)
        {
            m_CurveEditor.hRangeLocked = e.shift;
            m_CurveEditor.vRangeLocked = EditorGUI.actionKey;

            if (isMouseOverEditor && e.keyCode == KeyCode.F && e.type == EventType.KeyDown)
            {
                m_CurveEditor.FrameSelected(true, true);
                GUI.changed = true;
                e.Use();
            }
        }

        using (new EditorGUI.DisabledScope(!isMouseOverEditor && !IsActive && isAnyInstanceDragging))
        {
            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            if (e.GetTypeForControl(controlID) == EventType.MouseDown && isMouseOverEditor)
            {
                GUIUtility.hotControl = controlID;
            }

            EditorGUI.BeginChangeCheck();
            m_CurveEditor.OnGUI();
            if (EditorGUI.EndChangeCheck())
            {
                CheckCurveEditState();
            }

            if (e.GetTypeForControl(controlID) == EventType.MouseUp && GUIUtility.hotControl == controlID)
            {
                GUIUtility.hotControl = 0;
            }
        }

        DrawTimeLine();

        if (isMouseOverEditor || IsActive)
            HandleKeyframeNavigate(e);
    }


    private void HandleKeyframeNavigate(Event e)
    {
        if (e.type == EventType.KeyDown)
        {
            bool handled = false;

            if (e.keyCode == KeyCode.Comma) // 后退一帧
            {
                if (e.alt)
                {
                    // 跳转到前一个关键帧
                    float prevKeyTime = 0;
                    bool foundKey = false;

                    foreach (var wrapper in m_CurveWrappers)
                    {
                        AnimationCurve curve = GetWrapperCurve(wrapper.id);
                        if (curve != null)
                        {
                            for (int i = curve.length - 1; i >= 0; i--)
                            {
                                float keyTime = curve.keys[i].time;
                                if (keyTime < CurrentTime && (!foundKey || keyTime > prevKeyTime))
                                {
                                    prevKeyTime = keyTime;
                                    foundKey = true;
                                }
                            }
                        }
                    }

                    if (foundKey)
                        CurrentTime = prevKeyTime;
                }
                else if (e.control)
                {
                    CurrentTime = 0f;
                }
                else
                {
                    CurrentTime = Mathf.Max(0f, CurrentTime - 0.01f);
                }
                handled = true;
            }
            else if (e.keyCode == KeyCode.Period) // 前进一帧
            {
                if (e.alt)
                {
                    // 跳转到下一个关键帧
                    float nextKeyTime = 1f;
                    bool foundKey = false;

                    foreach (var wrapper in m_CurveWrappers)
                    {
                        AnimationCurve curve = GetWrapperCurve(wrapper.id);
                        if (curve != null)
                        {
                            for (int i = 0; i < curve.length; i++)
                            {
                                float keyTime = curve.keys[i].time;
                                if (keyTime > CurrentTime && (!foundKey || keyTime < nextKeyTime))
                                {
                                    nextKeyTime = keyTime;
                                    foundKey = true;
                                }
                            }
                        }
                    }

                    if (foundKey)
                        CurrentTime = nextKeyTime;
                }
                else if (e.control)
                {
                    CurrentTime = 1f;
                }
                else
                {
                    CurrentTime = Mathf.Min(1f, CurrentTime + 0.01f);
                }

                handled = true;
            }

            if (handled)
            {
                GUI.changed = true;
                e.Use();
            }
        }
    }

    private void DrawTimeLine()
    {
        if (!m_CurveEditor.drawRect.Contains(new Vector2(m_CurveEditor.rect.x, m_CurveEditor.rect.y)))
            return;

        float xMin = m_CurveEditor.drawRect.xMin + m_CurveEditor.leftmargin;
        float xMax = m_CurveEditor.drawRect.xMax - m_CurveEditor.rightmargin;
        float yMin = m_CurveEditor.drawRect.yMin;
        float yMax = m_CurveEditor.drawRect.yMax;

        float hRangeMin = m_CurveEditor.shownAreaInsideMargins.xMin;
        float hRangeMax = m_CurveEditor.shownAreaInsideMargins.xMax;

        if (CurrentTime < hRangeMin || CurrentTime > hRangeMax)
            return;

        float normalizedTime = Mathf.InverseLerp(hRangeMin, hRangeMax, CurrentTime);
        float xPos = Mathf.Lerp(xMin, xMax, normalizedTime);

        Handles.color = timeLineColor;
        Handles.DrawAAPolyLine(timeLineWidth, new Vector3(xPos, yMin), new Vector3(xPos, yMax));
    }

    public void DrawTimeSlider(Rect sliderRect)
    {
        timeSliderRect = sliderRect;
        EditorGUI.DrawRect(timeSliderRect, new Color(0.2f, 0.2f, 0.2f));
        float xMin = timeSliderRect.xMin + m_CurveEditor.leftmargin;
        float xMax = timeSliderRect.xMax - m_CurveEditor.rightmargin;
        float yMin = timeSliderRect.yMin;
        float yMax = timeSliderRect.yMax;

        float currentX = Mathf.Lerp(xMin, xMax, CurrentTime);
        Handles.color = timeLineColor;
        Handles.DrawAAPolyLine(timeLineWidth, new Vector3(currentX, yMin), new Vector3(currentX, yMax));

        Vector3[] trianglePoints = new Vector3[]
        {
            new Vector3(currentX - 5, yMin),
            new Vector3(currentX + 5, yMin),
            new Vector3(currentX, yMin + 5)
        };
        Handles.DrawAAConvexPolygon(trianglePoints);

        GUI.color = Color.white;
        GUI.Label(new Rect(currentX + 8, yMin, 50, 15), $"{CurrentTime:F2}");

        HandleTimelineInteraction();
    }

    private void HandleTimelineInteraction()
    {
        Event e = Event.current;
        bool isMouseOverSlider = timeSliderRect.Contains(e.mousePosition);

        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 0 && isMouseOverSlider && !isAnyInstanceDragging)
                {
                    isDraggingTimeline = true;
                    isAnyInstanceDragging = true;
                    activeInstanceId = instanceId;
                    UpdateTimeFromMousePosition(e.mousePosition.x);
                    OnTimeSliderClicked?.Invoke();
                    e.Use();
                }

                break;

            case EventType.MouseUp:
                if (isDraggingTimeline && IsActive)
                {
                    isDraggingTimeline = false;
                    isAnyInstanceDragging = false;
                    e.Use();
                }

                break;

            case EventType.MouseDrag:
                if (isDraggingTimeline && IsActive)
                {
                    UpdateTimeFromMousePosition(e.mousePosition.x);
                    e.Use();
                }

                break;
        }
    }

    private void UpdateTimeFromMousePosition(float mouseX)
    {
        float xMin = timeSliderRect.xMin + m_CurveEditor.leftmargin;
        float xMax = timeSliderRect.xMax - m_CurveEditor.rightmargin;
        CurrentTime = Mathf.Clamp01(Mathf.InverseLerp(xMin, xMax, mouseX));
        GUI.changed = true;
    }

    public void DrawLegend(Rect rect)
    {
        if (m_CurveWrappers.Count == 0)
            return;

        List<Rect> legendRects = new List<Rect>();
        int width = Mathf.Min(100, Mathf.FloorToInt(rect.width / m_CurveWrappers.Count));

        float totalWidth = width * m_CurveWrappers.Count;

        float startX = rect.x + (rect.width - totalWidth) * 0.5f;

        for (int i = 0; i < m_CurveWrappers.Count; i++)
        {
            legendRects.Add(new Rect(startX + width * i, rect.y, width, rect.height));
        }

        bool isMouseOverLegend = rect.Contains(Event.current.mousePosition);

        if (isMouseOverLegend || IsActive)
        {
            if (EditorGUIExt.DragSelection(legendRects.ToArray(), ref m_SelectedCurves, GUIStyle.none))
            {
                bool anySelected = false;
                for (int i = 0; i < m_CurveWrappers.Count; i++)
                {
                    if (m_SelectedCurves[i])
                        anySelected = true;
                }

                if (!anySelected)
                {
                    for (int i = 0; i < m_CurveWrappers.Count; i++)
                        m_SelectedCurves[i] = true;
                }

                SyncSelectedCurvesToEditor();
                activeInstanceId = instanceId;
            }
        }

        for (int i = 0; i < m_CurveWrappers.Count; i++)
        {
            var wrapper = m_CurveEditor.GetCurveWrapperFromID(m_CurveWrappers[i].id);
            if (wrapper != null)
            {
                EditorGUI.DrawLegend(
                    legendRects[i],
                    wrapper.color,
                    m_CurveWrappers.Count > 6 ? null : wrapper.yAxisLabel,
                    m_SelectedCurves[i]
                );
            }
        }
    }

    public AnimationCurve GetWrapperCurve(int id)
    {
        var wrapper = m_CurveEditor.GetCurveWrapperFromID(id);
        return wrapper?.curve;
    }

    public Dictionary<string, float> GetCurrentValues()
    {
        Dictionary<string, float> values = new Dictionary<string, float>();

        foreach (var curveWrapper in m_CurveWrappers)
        {
            var wrapper = m_CurveEditor.GetCurveWrapperFromID(curveWrapper.id);
            if (wrapper != null && !wrapper.hidden)
            {
                values[wrapper.yAxisLabel] = wrapper.curve.Evaluate(CurrentTime);
            }
        }

        return values;
    }
}