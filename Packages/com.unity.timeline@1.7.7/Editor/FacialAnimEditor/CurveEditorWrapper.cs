using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;

public class CurveEditorWrapper
{
    private CurveEditor m_curveEditor;
    private List<CurveWrapper> m_curveWrappers = new List<CurveWrapper>();
    private bool[] m_selectedCurves = new bool[0];

    private const float m_timeLineWidth = 2f;
    private readonly Color kTimeLineColor = new Color(1f, 0.3f, 0.3f, 0.8f);
    private readonly Color kTimeStampColor = new Color(0.3f, 0.3f, 1f, 0.8f);
    private bool m_isDraggingTimeline = false;
    private Rect m_timeSliderRect;

    private int m_instanceId;
    private static int m_activeInstanceId = -1;
    private static bool m_isAnyInstanceDragging = false;

    // 帧率相关
    private float m_frameRate = 30f;
    public float FrameRate
    {
        get { return m_frameRate; }
        set 
        { 
            m_frameRate = value;
            if (m_curveEditor != null)
            {
                m_curveEditor.invSnap = m_frameRate;
                m_curveEditor.hTicks.SetTickModulosForFrameRate(m_frameRate);
            }
        }
    }
    
    // Undo支持相关
    private AnimationClip m_curvesContainer;
    private AnimationClip CurvesContainer
    {
        get
        {
            if (m_curvesContainer == null)
            {
                m_curvesContainer = new AnimationClip();
                m_curvesContainer.name = "CurveEditorContainer";
            }

            return m_curvesContainer;
        }
        set
        {
            if (value == null)
            {
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(m_curvesContainer);
                else
                    UnityEngine.Object.DestroyImmediate(m_curvesContainer);
            }

            m_curvesContainer = value;
        }
    }

    private Dictionary<int, EditorCurveBinding> m_curveBindings = new Dictionary<int, EditorCurveBinding>();
    private bool m_isRegisteredForUndo = false;
    private bool m_wasInLiveEdit = false;
    public bool InLiveEdit => m_curveEditor.InLiveEdit();
    public bool IsActive => m_instanceId == m_activeInstanceId;
    
    private int m_currentFrame = 0;
    public int CurrentFrame
    {
        get => Mathf.RoundToInt(CurrentTime * m_frameRate);
        set
        {
            m_currentFrame = Mathf.Max(0, value);
            CurrentTime = (float)m_currentFrame / m_frameRate;
        }
    }

    private float m_currentTime = 0f;
    public float CurrentTime
    {
        get => m_currentTime;
        set
        {
            float alignedTime = AlignTimeToFrame(value);
            
            if (!Mathf.Approximately(m_currentTime, alignedTime))
            {
                m_currentTime = Mathf.Clamp(alignedTime, 0, m_duration);
                OnTimeChanged?.Invoke(m_currentTime);
            }
        }
    }

    private float m_duration = 1f;

    public float Duration
    {
        get => m_duration;
        set
        {
            m_duration = value;
            if (m_curveEditor != null)
            {
                m_curveEditor.settings.hRangeMin = 0;
                m_curveEditor.settings.hRangeMax = m_duration;
                m_curveEditor.settings = m_curveEditor.settings;
                m_curveEditor.SetShownHRangeInsideMargins(0, m_duration);
                foreach (var wrapper in m_curveWrappers)
                    wrapper.renderer.SetCustomRange(0f, m_duration);
                UpdateCurveEditor();
                GUI.changed = true;
                CurrentTime = Mathf.Clamp(m_currentTime, 0, m_duration);
            }
        }
    }

    public event Action OnTimeSliderClicked;
    public event Action<float> OnTimeChanged;
    public event Action<float, float> OnLocalIntervalChanged;
    public event Action OnCurvesModified;
    
    public CurveEditorWrapper(float frameRate)
    {
        m_instanceId = GetHashCode();

        CurveEditorSettings settings = new CurveEditorSettings()
        {
            hRangeMin = 0.0f,
            vRangeMin = 0.0f,
            vRangeMax = 1.1f,
            hRangeMax = m_duration,
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
        m_curveEditor = new CurveEditor(new Rect(0.0f, 0.0f, 1000f, 100f), new CurveWrapper[0], false);
        m_curveEditor.settings = settings;
        m_curveEditor.margin = 25f;
        m_curveEditor.SetShownHRangeInsideMargins(0.0f, m_duration);
        m_curveEditor.SetShownVRangeInsideMargins(0.0f, 1f);
        m_curveEditor.curvesUpdated = SyncCurves;
        
        // 设置初始帧率
        FrameRate = frameRate;
        Undo.undoRedoEvent += UndoRedoPerformed;
    }
    
    public void OnDisable()
    {
        if (m_isDraggingTimeline && IsActive)
        {
            m_isDraggingTimeline = false;
            m_isAnyInstanceDragging = false;
        }
        
        m_curveEditor.OnDisable();
        CurvesContainer = null;
        m_startStampTime = -1;
        m_endStampTime = -1;
        Undo.undoRedoEvent -= UndoRedoPerformed;
    }

    private void UndoRedoPerformed(in UndoRedoInfo info)
    {
        ContainerToWrapper();
    }

    private void SyncCurves()
    {
        bool isInLiveEdit = InLiveEdit;
        if (isInLiveEdit && !m_wasInLiveEdit)
        {
            WrapperToContainer();
            RegisterUndo("Edit Curve");
            m_isRegisteredForUndo = true;
        }
        else if (!isInLiveEdit && m_wasInLiveEdit)
        {
            WrapperToContainer();
            m_isRegisteredForUndo = false;
        }
        else if (isInLiveEdit)
        {
            // 在拖动过程中也进行帧对齐
            foreach (var wrapper in m_curveWrappers)
            {
                AnimationCurve curve = GetWrapperCurve(wrapper.id);
                if (curve != null && curve.length > 0)
                {
                    for (int i = 0; i < curve.length; i++)
                    {
                        Keyframe key = curve[i];
                        key.time = AlignTimeToFrame(key.time);
                    }
                    wrapper.renderer.FlushCache();
                }
            }
        }

        m_wasInLiveEdit = isInLiveEdit;
    }

    private void RegisterUndo(string undoName)
    {
        Undo.RegisterCompleteObjectUndo(CurvesContainer, undoName);
    }

    // 将时间对齐到帧
    private float AlignTimeToFrame(float time)
    {
        if (m_frameRate <= 0)
            return time;
            
        float frameTime = Mathf.Round(time * m_frameRate) / m_frameRate;
        return frameTime;
    }

    private void ContainerToWrapper()
    {
        foreach (var binding in m_curveBindings)
        {
            int curveId = binding.Key;
            EditorCurveBinding curveBinding = binding.Value;
            AnimationCurve containerCurve = AnimationUtility.GetEditorCurve(CurvesContainer, curveBinding);
            if (containerCurve == null)
            {
                Debug.Log("CurveEditorWrapper.ContainerToWrapper: containerCurve is null");
                continue;
            }

            CurveWrapper wrapper = m_curveEditor.GetCurveWrapperFromID(curveId);
            if (wrapper == null)
            {
                Debug.Log($"CurveEditorWrapper.ContainerToWrapper: wrapper {curveId}  is null");
                continue;
            }

            AnimationCurve wrapperCurve = wrapper.renderer.GetCurve();
            if (wrapperCurve != null)
            {
                wrapperCurve.ClearKeys();
                
                // 将所有关键帧对齐到帧
                Keyframe[] alignedKeys = new Keyframe[containerCurve.keys.Length];
                for (int i = 0; i < containerCurve.keys.Length; i++)
                {
                    Keyframe key = containerCurve.keys[i];
                    key.time = AlignTimeToFrame(key.time);
                    alignedKeys[i] = key;
                }
                wrapperCurve.keys = alignedKeys;
            }

            wrapper.renderer.FlushCache();
        }
    }
    
    private void WrapperToContainer()
    {
        foreach (var binding in m_curveBindings)
        {
            int curveId = binding.Key;
            EditorCurveBinding curveBinding = binding.Value;
            AnimationCurve wrapperCurve = GetWrapperCurve(curveId);
            
            // 确保所有关键帧时间都对齐到帧
            if (wrapperCurve != null && wrapperCurve.length > 0)
            {
                for (int i = 0; i < wrapperCurve.length; i++)
                {
                    Keyframe key = wrapperCurve[i];
                    float alignedTime = AlignTimeToFrame(key.time);
                    
                    if (!Mathf.Approximately(key.time, alignedTime))
                    {
                        key.time = alignedTime;
                        wrapperCurve.MoveKey(i, key);
                    }
                }
            }
            
            CurvesContainer.SetCurve(curveBinding.path, curveBinding.type, curveBinding.propertyName, wrapperCurve);
        }
        OnCurvesModified?.Invoke();
        EditorUtility.SetDirty(CurvesContainer);
    }

    public void UpdateCurveEditor()
    {
        if (m_curveEditor.InLiveEdit())
            return;
        m_curveEditor.animationCurves = m_curveWrappers.ToArray();
    }

    public void AddCurve(AnimationCurve curve, string name, Color color, int curveId)
    {
        if (curve == null || curve.length == 0)
        {
            Debug.LogError($"曲线 {name} 为空或没有关键帧！");
            return;
        }

        // 确保所有关键帧时间都对齐到帧
        for (int i = 0; i < curve.length; i++)
        {
            Keyframe key = curve[i];
            key.time = AlignTimeToFrame(key.time);
            curve.MoveKey(i, key);
        }

        EditorCurveBinding binding = new EditorCurveBinding
        {
            path = name,
            propertyName = name,
            type = typeof(Transform)
        };
        m_curveBindings[curveId] = binding;
        CurvesContainer.SetCurve(name, typeof(Transform), name, curve);

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
        curveWrapper.renderer.SetCustomRange(0f, Duration);
        m_curveWrappers.Add(curveWrapper);

        UpdateCurveEditor();
        ResizeSelectedCurves();
    }

    public void ClearCurves()
    {
        m_selectedCurves = new bool[0];
        m_curveBindings.Clear();
        m_curveWrappers.Clear();
        CurvesContainer.ClearCurves();
        EditorUtility.SetDirty(CurvesContainer);
        UpdateCurveEditor();
    }

    public void FrameSelected()
    {
        m_curveEditor.FrameSelected(true, true);
    }

    private void ResizeSelectedCurves()
    {
        if (m_curveWrappers.Count != m_selectedCurves.Length)
        {
            bool[] newArray = new bool[m_curveWrappers.Count];
            for (int i = 0; i < newArray.Length; i++)
            {
                newArray[i] = i < m_selectedCurves.Length ? m_selectedCurves[i] : true;
            }

            m_selectedCurves = newArray;
            SyncSelectedCurvesToEditor();
        }
    }

    private void SyncSelectedCurvesToEditor()
    {
        for (int i = 0; i < m_curveWrappers.Count; i++)
        {
            CurveWrapper wrapper = m_curveEditor.GetCurveWrapperFromID(m_curveWrappers[i].id);
            if (wrapper != null)
            {
                wrapper.hidden = !m_selectedCurves[i];
            }
        }
        m_curveEditor.animationCurves = m_curveEditor.animationCurves;
    }

    public void OnGUI()
    {
        Rect sRect = EditorGUILayout.GetControlRect(false, 15);
        DrawTimeSlider(sRect);

        Rect curveRect = EditorGUILayout.GetControlRect(false, 200);
        OnGUI(curveRect);

        Rect lRect = GUILayoutUtility.GetRect(10f, 20f);
        DrawLegend(lRect);
    }

    public void OnGUI(Rect rect)
    {
        Event e = Event.current;
        bool isMouseOverCurvePanel = rect.Contains(e.mousePosition);
        if (isMouseOverCurvePanel && e.type == EventType.MouseDown && !m_isAnyInstanceDragging)
            m_activeInstanceId = m_instanceId;

        if (e.type != EventType.Layout && e.type != EventType.Used)
            m_curveEditor.rect = new Rect(rect.x, rect.y, rect.width, rect.height);

        GUI.Label(m_curveEditor.drawRect, GUIContent.none, "TextField");
        if (isMouseOverCurvePanel || IsActive)
        {
            m_curveEditor.hRangeLocked = e.shift;
            m_curveEditor.vRangeLocked = EditorGUI.actionKey;
        }

        if (e.type == EventType.KeyDown)
        {
            if(IsActive)
                HandleKeyframeNavigates(e);
            if (isMouseOverCurvePanel)
                HandleCurvePanelShortCuts(e);
        }
        
        using (new EditorGUI.DisabledScope(!isMouseOverCurvePanel && !IsActive && m_isAnyInstanceDragging))
        {
            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            if (e.GetTypeForControl(controlID) == EventType.MouseDown && isMouseOverCurvePanel)
                GUIUtility.hotControl = controlID;

            EditorGUI.BeginChangeCheck();
            m_curveEditor.OnGUI();
            if (EditorGUI.EndChangeCheck())
                SyncCurves();

            if (e.GetTypeForControl(controlID) == EventType.MouseUp && GUIUtility.hotControl == controlID)
                GUIUtility.hotControl = 0;
        }

        if (isMouseOverCurvePanel && e.type == EventType.MouseDown)
        {
            if (e.clickCount == 2 && e.button == 1)
            {
                CurrentTime = GetTimeByMousePositionInCurvePanel(e.mousePosition);
                GUI.changed = true;
                e.Use();
            }   
        }
        DrawTimeLine();
    }

    private float m_startStampTime = -1f;
    private float m_endStampTime = -1f;
    private void AddLocalPlayTimeStamp(Vector3 mousePosition)
    {
        if(m_startStampTime < 0 && m_endStampTime < 0)
            m_startStampTime = AlignTimeToFrame(GetTimeByMousePositionInCurvePanel(mousePosition));
        else if (m_startStampTime >= 0 && m_endStampTime < 0)
        {
            m_endStampTime = AlignTimeToFrame(GetTimeByMousePositionInCurvePanel(mousePosition));
            if (m_startStampTime > m_endStampTime)
            {
                float timeDiff = m_endStampTime;
                m_endStampTime = m_startStampTime;
                m_startStampTime = timeDiff;
            }
        }
        else if (m_startStampTime >= 0 && m_endStampTime >= 0)
            m_startStampTime = m_endStampTime = -1;
        OnLocalIntervalChanged?.Invoke(m_startStampTime, m_endStampTime);
    }

    private void HandleCurvePanelShortCuts(Event e)
    {
        bool handled = false;
        if (e.keyCode == KeyCode.F)
        {
            m_curveEditor.FrameSelected(!e.control, !e.shift);
            handled = true;
        }
        else if (e.keyCode == KeyCode.K)
        {
            AddKeyframesAtCurrentTime();
            handled = true;
        }
        else if (e.keyCode == KeyCode.C && e.control)
        {
            CopySelectedKeyframes();
            handled = true;
        }
        else if (e.keyCode == KeyCode.V && e.control)
        {
            PasteKeyframesAtCurrentTime();
            handled = true;
        }
        else if (e.keyCode == KeyCode.A)
        {
            AddLocalPlayTimeStamp(e.mousePosition);
            handled = true;
        }
        else if (e.keyCode == KeyCode.R)
        {
            CurrentTime = GetTimeByMousePositionInCurvePanel(e.mousePosition);
            handled = true;
        }  
        
        if (handled)
        {
            GUI.changed = true;
            e.Use();
        }
    }
    
    private void HandleKeyframeNavigates(Event e)
    {
        bool handled = false;
        if (e.keyCode == KeyCode.Comma)
        {
            if (e.shift)
            {
                float prevKeyTime = 0;
                bool foundKey = false;

                foreach (var wrapper in m_curveWrappers)
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
                // 按帧移动
                float frameDuration = 1f / m_frameRate;
                CurrentTime = Mathf.Max(0f, CurrentTime - frameDuration);
            }

            handled = true;
        }
        else if (e.keyCode == KeyCode.Period)
        {
            if (e.shift)
            {
                float nextKeyTime = Duration;
                bool foundKey = false;

                foreach (var wrapper in m_curveWrappers)
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
                CurrentTime = Duration;
            }
            else
            {
                // 按帧移动
                float frameDuration = 1f / m_frameRate;
                CurrentTime = Mathf.Min(Duration, CurrentTime + frameDuration);
            }

            handled = true;
        }
            
        if (handled)
        {
            GUI.changed = true;
            e.Use();
        }
    }

    private class KeyframeData
    {
        public float Time { get; set; }
        public float Value { get; set; }
        public float InTangent { get; set; }
        public float OutTangent { get; set; }
        public int LeftTangentMode { get; set; }
        public int RightTangentMode { get; set; }
    }

    private Dictionary<int, List<KeyframeData>> m_copiedKeyframes = new Dictionary<int, List<KeyframeData>>();
    private float m_copyReferenceTime = 0f; // 复制时的参考时间点

    private void CopySelectedKeyframes()
    {
        m_copiedKeyframes.Clear();
        m_copyReferenceTime = m_curveEditor.selectionBounds.min.x;
        bool anyKeyCopied = false;

        // 获取选中的关键帧
        for (int i = 0; i < m_curveWrappers.Count; i++)
        {
            if (!m_selectedCurves[i])
                continue;
            
            var wrapper = m_curveWrappers[i];
            var keyframes = new List<KeyframeData>();
            AnimationCurve curve = GetWrapperCurve(wrapper.id);
            foreach (var selectedCurves in m_curveEditor.selection.selectedCurves)
            {
                if (m_curveWrappers[i].id == selectedCurves.curveID && selectedCurves.key < curve.keys.Length)
                {
                    var keyIndex = selectedCurves.key;
                    var key = curve.keys[keyIndex];
                    keyframes.Add(new KeyframeData
                    {
                        Time = key.time,
                        Value = key.value,
                    });
                    anyKeyCopied = true;
                }
            }
            
            if (anyKeyCopied)
                m_copiedKeyframes[wrapper.id] = keyframes;
        }
    }
    
    // 在当前时间点粘贴关键帧
    private void PasteKeyframesAtCurrentTime()
    {
        if (m_copiedKeyframes.Count == 0)
        {
            Debug.Log("剪贴板为空，没有可粘贴的关键帧");
            return;
        }

        RegisterUndo("Paste Keyframes");
        float timeOffset = CurrentTime - m_copyReferenceTime;
        bool anyKeyPasted = false;

        foreach (var kvp in m_copiedKeyframes)
        {
            int curveId = kvp.Key;
            List<KeyframeData> keyframes = kvp.Value;

            // 确保曲线ID仍然有效
            CurveWrapper wrapper = m_curveEditor.GetCurveWrapperFromID(curveId);
            if (wrapper == null)
                continue;

            // 确保曲线被选中
            int curveIndex = m_curveWrappers.FindIndex(w => w.id == curveId);
            if (curveIndex < 0 || !m_selectedCurves[curveIndex])
                continue;

            AnimationCurve curve = GetWrapperCurve(curveId);
            if (curve != null)
            {
                foreach (var keyData in keyframes)
                {
                    float newTime = AlignTimeToFrame(keyData.Time + timeOffset);

                    // 确保时间在有效范围内
                    if (newTime < 0 || newTime > Duration)
                        continue;

                    // 检查是否已存在关键帧
                    bool keyExists = false;
                    for (int i = 0; i < curve.length; i++)
                    {
                        if (Mathf.Approximately(curve.keys[i].time, newTime))
                        {
                            keyExists = true;
                            break;
                        }
                    }

                    // 如果已存在关键帧，可以选择覆盖或跳过
                    if (keyExists)
                    {
                        // 这里选择覆盖现有的关键帧
                        for (int i = 0; i < curve.length; i++)
                        {
                            if (Mathf.Approximately(curve.keys[i].time, newTime))
                            {
                                Keyframe oldKey = curve.keys[i];
                                curve.RemoveKey(i);
                                break;
                            }
                        }
                    }

                    // 添加新的关键帧
                    int newKeyIndex = curve.AddKey(new Keyframe(
                        newTime,
                        keyData.Value,
                        keyData.InTangent,
                        keyData.OutTangent
                    ));

                    if (newKeyIndex >= 0)
                    {
                        // 设置切线模式
                        AnimationUtility.SetKeyLeftTangentMode(curve, newKeyIndex, (AnimationUtility.TangentMode)keyData.LeftTangentMode);
                        AnimationUtility.SetKeyRightTangentMode(curve, newKeyIndex, (AnimationUtility.TangentMode)keyData.RightTangentMode);
                        anyKeyPasted = true;
                    }
                }
            }
        }

        if (anyKeyPasted)
        {
            // 更新曲线编辑器
            WrapperToContainer();
            ContainerToWrapper();
            GUI.changed = true;
        }
    }

    private void AddKeyframesAtCurrentTime()
    {
        if (m_curveWrappers.Count == 0)
            return;

        RegisterUndo("Add Keyframes");
        foreach (var wrapper in m_curveWrappers)
        {
            if (!m_selectedCurves[m_curveWrappers.IndexOf(wrapper)])
                continue;

            AnimationCurve curve = GetWrapperCurve(wrapper.id);
            if (curve != null)
            {
                float value = curve.Evaluate(CurrentTime);

                // 检查是否已存在关键帧
                bool keyExists = false;
                for (int i = 0; i < curve.length; i++)
                {
                    if (Mathf.Approximately(curve.keys[i].time, CurrentTime))
                    {
                        keyExists = true;
                        break;
                    }
                }

                // 如果不存在关键帧，则添加一个
                if (!keyExists)
                {
                    Keyframe newKey = new Keyframe(CurrentTime, value);
                    curve.AddKey(newKey);
                }
            }
        }

        // 更新曲线编辑器
        WrapperToContainer();
        ContainerToWrapper();
        GUI.changed = true;
    }

    private void DrawTimeLine()
    {
        if (!m_curveEditor.drawRect.Contains(new Vector2(m_curveEditor.rect.x, m_curveEditor.rect.y)))
            return;

        float xMin = m_curveEditor.drawRect.xMin + m_curveEditor.leftmargin;
        float xMax = m_curveEditor.drawRect.xMax - m_curveEditor.rightmargin;
        float yMin = m_curveEditor.drawRect.yMin;
        float yMax = m_curveEditor.drawRect.yMax;

        float hRangeMin = m_curveEditor.shownAreaInsideMargins.xMin;
        float hRangeMax = m_curveEditor.shownAreaInsideMargins.xMax;
        
        // Draw TimeStamp
        Handles.color = kTimeStampColor;
        if (m_startStampTime >= 0 && m_startStampTime >= hRangeMin && m_startStampTime <= hRangeMax)
        {
            float stampPosX = Mathf.Lerp(xMin, xMax, Mathf.InverseLerp(hRangeMin, hRangeMax, m_startStampTime));
            Handles.DrawAAPolyLine(m_timeLineWidth, new Vector3(stampPosX, yMin), new Vector3(stampPosX, yMax));
        }
        
        if (m_endStampTime >= 0 && m_endStampTime >= hRangeMin && m_endStampTime <= hRangeMax)
        {
            float stampPosX = Mathf.Lerp(xMin, xMax, Mathf.InverseLerp(hRangeMin, hRangeMax, m_endStampTime));
            Handles.DrawAAPolyLine(m_timeLineWidth, new Vector3(stampPosX, yMin), new Vector3(stampPosX, yMax));
        }
        
        // Draw TimeSwapper
        Handles.color = kTimeLineColor;
        if (CurrentTime >= hRangeMin && CurrentTime <= hRangeMax)
        {
            float normalizedTime = Mathf.InverseLerp(hRangeMin, hRangeMax, CurrentTime);
            float xPos = Mathf.Lerp(xMin, xMax, normalizedTime);
            Handles.DrawAAPolyLine(m_timeLineWidth, new Vector3(xPos, yMin), new Vector3(xPos, yMax));
        }
    }

    public void DrawTimeSlider(Rect sliderRect)
    {
        m_timeSliderRect = sliderRect;
        EditorGUI.DrawRect(m_timeSliderRect, new Color(0.2f, 0.2f, 0.2f));
        float xMin = m_timeSliderRect.xMin + m_curveEditor.leftmargin;
        float xMax = m_timeSliderRect.xMax - m_curveEditor.rightmargin;
        float yMin = m_timeSliderRect.yMin;
        float yMax = m_timeSliderRect.yMax;

        // Draw Slider
        float normalizedTime = Mathf.InverseLerp(0, Duration, CurrentTime);
        float currentX = Mathf.Lerp(xMin, xMax, normalizedTime);
        Handles.color = kTimeLineColor;
        Handles.DrawAAPolyLine(m_timeLineWidth, new Vector3(currentX, yMin), new Vector3(currentX, yMax));

        // Draw Stamp
        Handles.color = kTimeStampColor;
        if (m_startStampTime >= 0)
        {
            float stampPosX = Mathf.Lerp(xMin, xMax, Mathf.InverseLerp(0, Duration, m_startStampTime));
            Handles.DrawAAPolyLine(m_timeLineWidth, new Vector3(stampPosX, yMin), new Vector3(stampPosX, yMax));
        }
        
        if (m_endStampTime >= 0)
        {
            float stampPosX = Mathf.Lerp(xMin, xMax, Mathf.InverseLerp(0, Duration, m_endStampTime));
            Handles.DrawAAPolyLine(m_timeLineWidth, new Vector3(stampPosX, yMin), new Vector3(stampPosX, yMax));
        }
        
        // Draw Triangle
        Vector3[] trianglePoints = new Vector3[]
        {
            new Vector3(currentX - 5, yMin),
            new Vector3(currentX + 5, yMin),
            new Vector3(currentX, yMin + 5)
        };
        Handles.color = kTimeLineColor;
        Handles.DrawAAConvexPolygon(trianglePoints);

        // Draw Label
        GUI.color = Color.white;
        string timeFormat = Duration > 100 ? "F0" : Duration > 10 ? "F1" : "F2";
        GUI.Label(new Rect(currentX + 8, yMin, 50, 15), $"{CurrentTime.ToString(timeFormat)}");
        
        // 显示帧信息
        if (m_frameRate > 0)
        {
            int frame = Mathf.RoundToInt(CurrentTime * m_frameRate);
            GUI.Label(new Rect(currentX + 8, yMin + 15, 50, 15), $"F:{frame}");
        }

        HandleTimelineInteraction();
    }
    
    private void HandleTimelineInteraction()
    {
        Event e = Event.current;
        bool isMouseOverSlider = m_timeSliderRect.Contains(e.mousePosition);

        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 0 && isMouseOverSlider && !m_isAnyInstanceDragging)
                {
                    m_isDraggingTimeline = true;
                    m_isAnyInstanceDragging = true;
                    m_activeInstanceId = m_instanceId;
                    CurrentTime = GetTimeByMousePositionInSlider(e.mousePosition);
                    OnTimeSliderClicked?.Invoke();
                    GUI.changed = true;
                    e.Use();
                }

                break;

            case EventType.MouseUp:
                if (m_isDraggingTimeline && IsActive)
                {
                    m_isDraggingTimeline = false;
                    m_isAnyInstanceDragging = false;
                    e.Use();
                }

                break;

            case EventType.MouseDrag:
                if (m_isDraggingTimeline && IsActive)
                {
                    CurrentTime = GetTimeByMousePositionInSlider(e.mousePosition);
                    GUI.changed = true;
                    e.Use();
                }
                break;
        }
    }

    private float GetTimeByMousePositionInSlider(Vector2 mousePosition)
    {
        float xMin = m_timeSliderRect.xMin + m_curveEditor.leftmargin;
        float xMax = m_timeSliderRect.xMax - m_curveEditor.rightmargin;
        float normalizedTime = Mathf.InverseLerp(xMin, xMax, mousePosition.x);
        return AlignTimeToFrame(Mathf.Lerp(0, Duration, normalizedTime));
    }
    
    private float GetTimeByMousePositionInCurvePanel(Vector2 mousePosition)
    {
        float xMin = m_curveEditor.drawRect.xMin + m_curveEditor.leftmargin;
        float xMax = m_curveEditor.drawRect.xMax - m_curveEditor.rightmargin;
        float hRangeMin = m_curveEditor.shownAreaInsideMargins.xMin;
        float hRangeMax = m_curveEditor.shownAreaInsideMargins.xMax;
        float normalizedPosition = Mathf.InverseLerp(xMin, xMax, mousePosition.x);
        return AlignTimeToFrame(Mathf.Lerp(hRangeMin, hRangeMax, normalizedPosition));
    }

    public void DrawLegend(Rect rect)
    {
        if (m_curveWrappers.Count == 0)
            return;

        List<Rect> legendRects = new List<Rect>();
        int width = Mathf.Min(120, Mathf.FloorToInt(rect.width / m_curveWrappers.Count));

        float totalWidth = width * m_curveWrappers.Count;

        float startX = rect.x + (rect.width - totalWidth) * 0.5f;

        for (int i = 0; i < m_curveWrappers.Count; i++)
        {
            legendRects.Add(new Rect(startX + width * i, rect.y, width, rect.height));
        }

        bool isMouseOverLegend = rect.Contains(Event.current.mousePosition);

        if (isMouseOverLegend || IsActive)
        {
            if (EditorGUIExt.DragSelection(legendRects.ToArray(), ref m_selectedCurves, GUIStyle.none))
            {
                bool anySelected = false;
                for (int i = 0; i < m_curveWrappers.Count; i++)
                {
                    if (m_selectedCurves[i])
                        anySelected = true;
                }

                if (!anySelected)
                {
                    for (int i = 0; i < m_curveWrappers.Count; i++)
                        m_selectedCurves[i] = true;
                }

                SyncSelectedCurvesToEditor();
                m_activeInstanceId = m_instanceId;
            }
        }

        for (int i = 0; i < m_curveWrappers.Count; i++)
        {
            var wrapper = m_curveEditor.GetCurveWrapperFromID(m_curveWrappers[i].id);
            if (wrapper != null)
            {
                EditorGUI.DrawLegend(
                    legendRects[i],
                    wrapper.color,
                    m_curveWrappers.Count > 6 ? null : wrapper.yAxisLabel,
                    m_selectedCurves[i]
                );
            }
        }
    }

    public AnimationCurve GetWrapperCurve(int id)
    {
        var wrapper = m_curveEditor.GetCurveWrapperFromID(id);
        return wrapper?.curve;
    }

    public Dictionary<string, float> GetCurrentValues()
    {
        Dictionary<string, float> values = new Dictionary<string, float>();

        foreach (var curveWrapper in m_curveWrappers)
        {
            var wrapper = m_curveEditor.GetCurveWrapperFromID(curveWrapper.id);
            if (wrapper != null && !wrapper.hidden)
            {
                values[wrapper.yAxisLabel] = wrapper.curve.Evaluate(CurrentTime);
            }
        }

        return values;
    }
}