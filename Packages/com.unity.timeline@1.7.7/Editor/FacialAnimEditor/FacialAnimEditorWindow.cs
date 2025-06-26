using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace Audio2Face
{
    public class FacialAnimationEditorWindow : EditorWindow
    {
        private enum BlendMode
        {
            Collapse,
            Weights,
            Curves
        }

        [MenuItem("Window/Audio2Face/Facial Animation Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<FacialAnimationEditorWindow>("Facial Animation Editor");
            window.minSize = new Vector2(800, 600);
        }

        // Context properties
        private GameObject m_faceBoneRoot;
        private AnimationClip m_exportAnimClip;
        private AudioClip m_curAudioClip;
        private bool m_isAnimClipDirty = false;

        // UI state
        private bool m_isFacialControlFoldout = true;
        private bool m_isLayerSidebarDisplay = true;
        private bool m_isEditing = false;
        private Vector2 m_scrollPosition;

        // Controllers and editors
        private BlendShapeController m_blendShapeController;
        private AnimCurveEditor m_layeredCurveEditor;
        private AnimCurveEditor m_blendCurveEditor;
        private readonly GUILayoutOption[] m_squareLayout = new GUILayoutOption[] { GUILayout.Width(22), GUILayout.Height(18) };
        
        // Time tracking
        private float m_layerCurveTime = 0;
        private float m_blendCurveTime = 0;

        // Animation layers
        private AnimationClip m_targetAnimClip = null;
        private int m_selectedLayerIndex = 0;
        private readonly List<string> m_animationLayers = new();
        private readonly HashSet<string> m_boneIds = new();
        private readonly HashSet<string> m_selectedBoneId = new();
        private readonly Dictionary<string, Dictionary<string, AnimationCurve>> m_layerToBoneIdToCurve = new();
        private ReorderableList m_reorderableAnimLayerList;

        // Animation layer blending
        private readonly List<string> m_blendedLayers = new();
        private readonly Dictionary<string, float> m_blendLayerWeights = new();
        private readonly Dictionary<string, AnimationCurve> m_blendLayerCurves = new();
        private int m_blendLayerMask = 0;
        private BlendMode m_blendMode = BlendMode.Collapse;
        private bool CanOutput => m_blendedLayers.Count > 0;
        private bool CanBlend => m_blendedLayers.Count > 1;

        // Preview players
        private AnimPreviewPlayer m_layeredPreviewPlayer;
        private AnimPreviewPlayer m_blendedPreviewPlayer;
        private Dictionary<string, AnimationCurve> m_boneIdToLayeredCurve = new();
        private Dictionary<string, AnimationCurve> m_boneIdToBlendedCurve = new();

        private void OnEnable()
        {
            m_blendShapeController = new BlendShapeController();
            m_blendShapeController.OnSelectedHandlerValuesChanged += OnHandlerValueChanged;
            m_blendShapeController.OnSelectedHandlerChanged += OnSelectedHandlerChanged;

            m_blendCurveEditor = new AnimCurveEditor();
            m_layeredCurveEditor = new AnimCurveEditor();
            m_layeredPreviewPlayer = new AnimPreviewPlayer();
            m_blendedPreviewPlayer = new AnimPreviewPlayer();
            
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            InitReorderableAnimLayerList();
        }

        private void OnDisable()
        {
            CleanupCache();
            m_blendShapeController.OnSelectedHandlerValuesChanged -= OnHandlerValueChanged;
            m_blendShapeController.OnSelectedHandlerChanged -= OnSelectedHandlerChanged;
            m_blendCurveEditor.OnDisable();
            m_layeredCurveEditor.OnDisable();
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void InitReorderableAnimLayerList()
        {
            m_reorderableAnimLayerList = new ReorderableList(
                m_animationLayers,
                typeof(string),
                true,
                true,
                true,
                true
            );
            m_reorderableAnimLayerList.drawHeaderCallback = (Rect rect) => { EditorGUI.LabelField(rect, $"Animation Layers", EditorStyles.boldLabel); };
            m_reorderableAnimLayerList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                if (index < 0 || index >= m_animationLayers.Count)
                    return;
                EditorGUI.LabelField(rect, $"{m_animationLayers[index].ToString()}");
            };
            m_reorderableAnimLayerList.onSelectCallback = (ReorderableList list) =>
            {
                m_selectedLayerIndex = list.index;
                OnSelectedLayerChanged(m_selectedLayerIndex);
            };

            m_reorderableAnimLayerList.onAddCallback = (ReorderableList list) => { AddNewAnimationLayer(); };
            m_reorderableAnimLayerList.onRemoveCallback = (ReorderableList list) => { RemoveCurrentAnimationLayer(); };
        }

        private void OnGUI()
        {
            m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);
            
            // Draw context properties at the top
            DrawContextProperties();
            
            // Draw main content
            if (m_faceBoneRoot == null)
            {
                EditorGUILayout.HelpBox("Please set a Face Bone Root first", MessageType.Info);
            }
            else
            {
                DrawBlendShapeController();
                DrawAnimationClipEdit();
                DrawCurveEdit();
                HandlePlayEvents();
            }
            
            EditorGUILayout.EndScrollView();
        }

        private void DrawContextProperties()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("Facial Animation Settings", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            m_faceBoneRoot = (GameObject)EditorGUILayout.ObjectField("Face Bone Root", m_faceBoneRoot, typeof(GameObject), true);
            
            m_curAudioClip = (AudioClip)EditorGUILayout.ObjectField("Audio Clip", m_curAudioClip, typeof(AudioClip), false);
            
            AnimationClip newAnimClip = (AnimationClip)EditorGUILayout.ObjectField("Animation Clip", m_exportAnimClip, typeof(AnimationClip), false);
            if (EditorGUI.EndChangeCheck())
            {
                if (newAnimClip != m_exportAnimClip)
                {
                    m_exportAnimClip = newAnimClip;
                    m_isAnimClipDirty = true;
                }
                
                if (m_isEditing)
                {
                    CleanupCache();
                    m_isEditing = false;
                }
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawLayerSidebar()
        {
            if (!m_isLayerSidebarDisplay)
                return;
            EditorGUILayout.BeginVertical(GUILayout.Width(120));
            m_reorderableAnimLayerList.DoLayoutList();
            EditorGUILayout.EndVertical();
        }

        private void OnHandlerValueChanged(BlendShapeHandler[] selectedSliders, float[][] values)
        {
            if (!m_isEditing)
                return;

            m_selectedBoneId.Clear();
            for (int i = 0; i < selectedSliders.Length; i++)
            {
                for (int j = 0; j < selectedSliders[i].BoneIds.Length; j++)
                {
                    m_selectedBoneId.Add(selectedSliders[i].BoneIds[j]);
                    if (GetSelectedCurve(selectedSliders[i].BoneIds[j], m_selectedLayerIndex, out var curve))
                        AnimationCurveUtility.UpdateKeyValue(curve, m_layerCurveTime, values[i][j]);
                }
            }

            m_layeredCurveEditor.UpdateCurveEditor();
        }

        private void OnSelectedHandlerChanged(BlendShapeHandler[] selectedSliders)
        {
            if (!m_isEditing)
                return;

            m_selectedBoneId.Clear();
            m_layeredCurveEditor.ClearCurves();
            for (int i = 0; i < selectedSliders.Length; i++)
            {
                for (int j = 0; j < selectedSliders[i].BoneIds.Length; j++)
                {
                    m_selectedBoneId.Add(selectedSliders[i].BoneIds[j]);
                    if (GetSelectedCurve(selectedSliders[i].BoneIds[j], m_selectedLayerIndex, out var curve))
                    {
                        int curveId = i * selectedSliders.Length + j;
                        Color curveColor = GUIStyleHelper.s_Colors[curveId % GUIStyleHelper.s_Colors.Length];
                        m_layeredCurveEditor.AddCurve(curve, selectedSliders[i].BoneIds[j], curveColor, curveId);
                    }
                }
            }

            m_layeredCurveEditor.UpdateCurveEditor();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
                CleanupCache();
        }

        private void OnSliderClicked()
        {
            GetSelectedCurves(m_selectedLayerIndex, out m_boneIdToLayeredCurve);
            m_layeredPreviewPlayer.RebuildAnimClip(m_boneIdToLayeredCurve);
        }

        private void OnCurvesModified()
        {
            // if (!m_layeredPreviewPlayer.IsPlaying)
            //     m_layeredPreviewPlayer.Seek(m_layerCurveTime);
            // UpdateHandlerValues(m_boneIdToLayeredCurve, m_layerCurveTime);
        }

        private void OnSliderTimeChanged(float time)
        {
            if (m_faceBoneRoot == null)
                return;
            if (Mathf.Abs(time - m_layerCurveTime) > 0.001f)
                m_layerCurveTime = time;

            if (!m_layeredPreviewPlayer.IsPlaying)
                m_layeredPreviewPlayer.Seek(m_layerCurveTime);
            UpdateHandlerValues(m_boneIdToLayeredCurve, m_layerCurveTime);
        }

        private void UpdateHandlerValues(Dictionary<string, AnimationCurve> curves, float time)
        {
            Dictionary<string, float> boneValues = new Dictionary<string, float>();
            foreach (var kvPair in curves)
            {
                var value = kvPair.Value.Evaluate(time);
                boneValues.Add(kvPair.Key, value);
            }

            m_blendShapeController.SetHandleValue(boneValues);
        }

        private void DrawBlendShapeController()
        {
            m_isFacialControlFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_isFacialControlFoldout, "Facial Control Panel");
            if (m_isFacialControlFoldout)
                m_blendShapeController.Draw(240, 300, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawCurveEdit()
        {
            if (m_isEditing)
            {
                EditorGUILayout.BeginVertical();
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    {
                        DrawLayerSidebar();
                        EditorGUILayout.BeginVertical();
                        {
                            DrawAnimationLayerPanel();
                            m_layeredCurveEditor.OnGUI();
                        }
                        EditorGUILayout.EndVertical();
                    }
                    EditorGUILayout.EndHorizontal();
                    DrawBlendLayerPanel();
                }
                EditorGUILayout.EndVertical();
            }
        }

        private void HandlePlayEvents()
        {
            Event e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Space)
            {
                if (m_layeredCurveEditor.IsActive)
                {
                    GetSelectedCurves(m_selectedLayerIndex, out m_boneIdToLayeredCurve);
                    m_layeredPreviewPlayer.PlayOrStop(m_boneIdToLayeredCurve);
                }
                else if (m_blendCurveEditor.IsActive)
                {
                    GetBlendCurves(out m_boneIdToBlendedCurve);
                    m_blendedPreviewPlayer.PlayOrStop(m_boneIdToBlendedCurve);
                }
                e.Use();
            }
        }

        private void DrawAnimationClipEdit()
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("Target Animation", GUILayout.Width(100));
                EditorGUI.BeginDisabledGroup(m_isEditing);
                EditorGUI.BeginChangeCheck();
                m_targetAnimClip = (AnimationClip)EditorGUILayout.ObjectField(m_targetAnimClip, typeof(AnimationClip), false);
                if (EditorGUI.EndChangeCheck())
                    m_layeredCurveEditor.Duration = m_blendCurveEditor.Duration = m_targetAnimClip.length;
                EditorGUI.EndDisabledGroup();
                EditorGUI.BeginDisabledGroup(m_targetAnimClip == null);
                {
                    string editBtnName = m_isEditing ? (CanOutput ? "Save" : "Exit") : "Edit";
                    if (GUILayout.Button(editBtnName, GUILayout.Width(60)))
                    {
                        if (!m_isEditing)
                        {
                            BuildCurveMap(m_targetAnimClip);
                            m_isEditing = true;
                        }
                        else
                        {
                            SaveCurveMap(m_targetAnimClip);
                            m_isEditing = false;
                        }
                    }
                }
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAnimationLayerPanel()
        {
            EditorGUILayout.BeginHorizontal();
            {
                if (m_animationLayers.Count > m_selectedLayerIndex)
                {
                    var iconContent = EditorGUIUtility.IconContent(m_isLayerSidebarDisplay ? "Toolbar Minus" : "Toolbar Plus");
                    if (GUILayout.Button(iconContent, m_squareLayout))
                        m_isLayerSidebarDisplay = !m_isLayerSidebarDisplay;
                    m_layeredCurveEditor.CurrentFrame = EditorGUILayout.IntField(m_layeredCurveEditor.CurrentFrame, GUILayout.Width(30));
                    EditorGUILayout.LabelField("Selected Layer", GUILayout.Width(80));
                    int newSelectedIndex = EditorGUILayout.Popup(m_selectedLayerIndex, m_animationLayers.ToArray());
                    if (newSelectedIndex != m_selectedLayerIndex)
                    {
                        m_selectedLayerIndex = Mathf.Clamp(newSelectedIndex, 0, m_animationLayers.Count - 1);
                        OnSelectedLayerChanged(m_selectedLayerIndex);
                    }

                    if (GUILayout.Button("+", m_squareLayout))
                    {
                        AddNewAnimationLayer();
                    }

                    if (GUILayout.Button("-", m_squareLayout))
                    {
                        RemoveCurrentAnimationLayer();
                    }

                    if (GUILayout.Button(m_layeredPreviewPlayer.IsPlaying ? "Pause" : "Play", GUILayout.Width(60)))
                    {
                        GetSelectedCurves(m_selectedLayerIndex, out m_boneIdToLayeredCurve);
                        m_layeredPreviewPlayer.PlayOrStop(m_boneIdToLayeredCurve);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void AddNewAnimationLayer()
        {
            string baseName = "Layer";
            int layerIndex = 1;
            string newLayerName = $"{baseName}{layerIndex}";

            while (m_animationLayers.Contains(newLayerName))
            {
                layerIndex++;
                newLayerName = $"{baseName}{layerIndex}";
            }

            m_animationLayers.Add(newLayerName);
            m_layerToBoneIdToCurve.Add(newLayerName, new Dictionary<string, AnimationCurve>());

            if (m_layerToBoneIdToCurve.ContainsKey("Base"))
            {
                var baseCurves = m_layerToBoneIdToCurve["Base"];
                foreach (var kvp in baseCurves)
                {
                    AnimationCurve newCurve = new AnimationCurve();
                    foreach (var key in kvp.Value.keys)
                        newCurve.AddKey(key);
                    m_layerToBoneIdToCurve[newLayerName].Add(kvp.Key, newCurve);
                }
            }

            m_selectedLayerIndex = m_animationLayers.Count - 1;
            OnSelectedLayerChanged(m_selectedLayerIndex);

            Debug.Log($"Added new animation layer: {newLayerName}");
        }

        private void RemoveCurrentAnimationLayer()
        {
            if (m_animationLayers.Count <= 1)
            {
                Debug.LogWarning("At least one animation layer must be kept");
                return;
            }

            if (m_animationLayers[m_selectedLayerIndex] == "Base")
            {
                EditorUtility.DisplayDialog("Cannot Delete", "The base layer cannot be deleted", "OK");
                return;
            }

            string layerToRemove = m_animationLayers[m_selectedLayerIndex];
            bool confirmed = EditorUtility.DisplayDialog(
                "Delete Animation Layer",
                $"Are you sure you want to delete the animation layer '{layerToRemove}'? This action cannot be undone.",
                "Delete",
                "Cancel");

            if (!confirmed)
                return;
            m_layerToBoneIdToCurve.Remove(layerToRemove);
            m_animationLayers.RemoveAt(m_selectedLayerIndex);
            m_selectedLayerIndex = Mathf.Clamp(m_selectedLayerIndex, 0, m_animationLayers.Count - 1);
            if (m_animationLayers.Count > 0)
                OnSelectedLayerChanged(m_selectedLayerIndex);
            else
                m_layeredCurveEditor.ClearCurves();
            Debug.Log($"Deleted animation layer: {layerToRemove}");
        }

        private void BuildDefaultLayersIfNeeded()
        {
            if (m_layerToBoneIdToCurve.Count == 0)
            {
                m_layerToBoneIdToCurve.Add("Base", new Dictionary<string, AnimationCurve>());
                m_layerToBoneIdToCurve.Add("Layer1", new Dictionary<string, AnimationCurve>());
            }

            if (m_animationLayers.Count == 0)
            {
                m_animationLayers.Add("Base");
                m_animationLayers.Add("Layer1");
            }

            m_selectedLayerIndex = 0;
        }

        private void OnSelectedLayerChanged(int newIndex)
        {
            m_layeredCurveEditor.ClearCurves();
            int index = 0;
            foreach (var boneId in m_selectedBoneId)
            {
                if (GetSelectedCurve(boneId, newIndex, out var curve))
                {
                    Color curveColor = GUIStyleHelper.s_Colors[index % GUIStyleHelper.s_Colors.Length];
                    m_layeredCurveEditor.AddCurve(curve, boneId, curveColor, index);
                    index++;
                }
            }

            GetSelectedCurves(m_selectedLayerIndex, out m_boneIdToLayeredCurve);
            m_layeredPreviewPlayer.Seek(m_layerCurveTime, m_boneIdToLayeredCurve);
        }

        #region Build And Save

        private void BuildCurveMap(AnimationClip sourceClip)
        {
            BuildDefaultLayersIfNeeded();
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(sourceClip);
            foreach (var curveBinding in bindings)
            {
                if (!string.IsNullOrEmpty(curveBinding.path) && curveBinding.propertyName == "m_LocalPosition.x")
                {
                    var boneId = curveBinding.path.Split("/").Last();
                    var curve = AnimationUtility.GetEditorCurve(sourceClip, curveBinding);
                    foreach (var bondCurveMap in m_layerToBoneIdToCurve)
                        bondCurveMap.Value.TryAdd(boneId, AnimationCurveUtility.GetCopiedCurve(curve));
                    m_boneIds.Add(boneId);
                }
            }
            
            RebuildCache(sourceClip);
            GetSelectedCurves(m_selectedLayerIndex, out m_boneIdToLayeredCurve);
            m_layeredPreviewPlayer.Seek(m_layerCurveTime, m_boneIdToLayeredCurve);
            Debug.Log($"Bound {m_boneIds.Count} handlers to facial editor");
        }

        private void RebuildCache(AnimationClip sourceClip)
        {
            // Player
            var animator = m_faceBoneRoot.GetOrAddComponent<Animator>();
            var audioSource = m_faceBoneRoot.GetOrAddComponent<AudioSource>();
            m_layeredPreviewPlayer.ReBind(animator, sourceClip, audioSource, m_curAudioClip);
            m_blendedPreviewPlayer.ReBind(animator, sourceClip, audioSource, m_curAudioClip);
            m_layeredPreviewPlayer.OnTimeChanged += OnLayeredPreviewTimeChanged;
            m_blendedPreviewPlayer.OnTimeChanged += OnBlendedPreviewTimeChanged;
            
            // Curve Panel
            m_layeredCurveEditor.OnLocalIntervalChanged += m_layeredPreviewPlayer.UpdateLocalPlayInterval;
            m_blendCurveEditor.OnLocalIntervalChanged += m_blendedPreviewPlayer.UpdateLocalPlayInterval;
            m_layeredCurveEditor.Duration = sourceClip.length;
            m_blendCurveEditor.Duration = sourceClip.length;
            m_layeredCurveEditor.OnTimeChanged += OnSliderTimeChanged;
            m_layeredCurveEditor.OnTimeSliderClicked += OnSliderClicked;
            m_layeredCurveEditor.OnCurvesModified += OnCurvesModified;
        }

        private void CleanupCache()
        {
            // DataCache and States
            m_isEditing = false;
            m_blendLayerMask = 0;
            m_selectedLayerIndex = 0;
            m_boneIdToLayeredCurve.Clear();
            m_boneIdToBlendedCurve.Clear();
            m_selectedBoneId.Clear();
            m_layerToBoneIdToCurve.Clear();
            m_boneIds.Clear();
            m_animationLayers.Clear();
            m_blendedLayers.Clear();
            m_blendLayerWeights.Clear();
            m_blendLayerCurves.Clear();
            
            // BlendShape Controller
            m_blendShapeController.UnSelectAllHandlers();

            // CurvePanel
            m_layeredCurveEditor.ClearCurves();
            m_blendCurveEditor.ClearCurves();
            m_layeredCurveEditor.OnTimeChanged -= OnSliderTimeChanged;
            m_layeredCurveEditor.OnTimeSliderClicked -= OnSliderClicked;
            m_layeredCurveEditor.OnCurvesModified -= OnCurvesModified;
            m_layeredCurveEditor.OnLocalIntervalChanged -= m_layeredPreviewPlayer.UpdateLocalPlayInterval;
            m_blendCurveEditor.OnLocalIntervalChanged -= m_blendedPreviewPlayer.UpdateLocalPlayInterval;
            
            // Player
            m_layeredPreviewPlayer.OnTimeChanged -= OnLayeredPreviewTimeChanged;
            m_blendedPreviewPlayer.OnTimeChanged -= OnBlendedPreviewTimeChanged;
            m_layeredPreviewPlayer.Cleanup();
            m_blendedPreviewPlayer.Cleanup();
        }

        private void SaveCurveMap(AnimationClip targetClip)
        {
            if (targetClip == null)
                return;

            if (CanOutput)
            {
                string originalPath = AssetDatabase.GetAssetPath(targetClip);
                string directory = Path.GetDirectoryName(originalPath);
                string fileName = Path.GetFileNameWithoutExtension(originalPath);
                string newFileName = $"{fileName}_Blended.anim";
                string newPath = Path.Combine(directory, newFileName);

                AnimationClip newClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(newPath);
                if (newClip == null)
                {
                    newClip = new AnimationClip();
                    AssetDatabase.CreateAsset(newClip, newPath);
                }

                AnimationClipSettings clipSettings = AnimationUtility.GetAnimationClipSettings(targetClip);
                AnimationUtility.SetAnimationClipSettings(newClip, clipSettings);

                newClip.frameRate = targetClip.frameRate;
                newClip.legacy = targetClip.legacy;
                newClip.wrapMode = targetClip.wrapMode;

                GetBlendCurves(out var boneIdToCurve);
                EditorCurveBinding[] existingBindings = AnimationUtility.GetCurveBindings(newClip);
                foreach (var binding in existingBindings)
                    AnimationUtility.SetEditorCurve(newClip, binding, null);

                foreach (var kvPair in boneIdToCurve)
                {
                    string boneId = kvPair.Key;
                    EditorCurveBinding binding = new EditorCurveBinding
                    {
                        path = $"{m_faceBoneRoot.name}/{boneId}",
                        propertyName = "m_LocalPosition.x",
                        type = typeof(Transform)
                    };
                    AnimationUtility.SetEditorCurve(newClip, binding, kvPair.Value);
                }

                EditorUtility.SetDirty(newClip);
                AssetDatabase.SaveAssets();
                Selection.activeObject = newClip;
                EditorGUIUtility.PingObject(newClip);
                Debug.Log($"Successfully saved {boneIdToCurve.Count} animation curves to {newClip.name}");
            }
            CleanupCache();
        }

        private void GetBlendCurves(out Dictionary<string, AnimationCurve> boneIdToBlendedCurve)
        {
            boneIdToBlendedCurve = new Dictionary<string, AnimationCurve>();
            foreach (string boneId in m_boneIds)
            {
                List<AnimationCurve> curvesToBlend = new List<AnimationCurve>();
                foreach (string layer in m_blendedLayers)
                    if (m_layerToBoneIdToCurve[layer].TryGetValue(boneId, out AnimationCurve curve))
                        curvesToBlend.Add(curve);

                if (curvesToBlend.Count == 1)
                {
                    boneIdToBlendedCurve[boneId] = curvesToBlend[0];
                    continue;
                }

                AnimationCurve resultCurve = null;
                if (m_blendMode == BlendMode.Curves)
                {
                    AnimationCurveUtility.NormalizeCurvesValue(m_blendLayerCurves, out var weightCurveList);
                    resultCurve = AnimationCurveUtility.BlendCurve(curvesToBlend, weightCurveList);
                }
                else if (m_blendMode == BlendMode.Weights)
                {
                    var weightValueList = m_blendLayerWeights.Values.ToList();
                    resultCurve = AnimationCurveUtility.BlendCurve(curvesToBlend, weightValueList);
                }
                else if (m_blendMode == BlendMode.Collapse)
                {
                    resultCurve = new AnimationCurve();

                    // Create a dictionary to store keyframes at each time point, with later layers overriding earlier ones
                    // Use a small threshold to merge very close time points
                    Dictionary<float, Keyframe> timeToKeyframe = new Dictionary<float, Keyframe>();
                    const float timeEpsilon = 0.0001f; // Time precision threshold

                    // Iterate from first layer to last (order matters, later layers override earlier ones)
                    for (int i = 0; i < curvesToBlend.Count; i++)
                    {
                        AnimationCurve curve = curvesToBlend[i];
                        foreach (Keyframe key in curve.keys)
                        {
                            // Check if a very close time point already exists
                            bool foundSimilarTime = false;
                            foreach (float existingTime in timeToKeyframe.Keys.ToList())
                            {
                                if (Mathf.Abs(existingTime - key.time) < timeEpsilon)
                                {
                                    // If a close time point is found, override it
                                    timeToKeyframe.Remove(existingTime);
                                    timeToKeyframe.Add(key.time, key);
                                    foundSimilarTime = true;
                                    break;
                                }
                            }

                            // If no close time point is found, add a new one
                            if (!foundSimilarTime)
                            {
                                timeToKeyframe.Add(key.time, key);
                            }
                        }
                    }

                    // Add all collected keyframes to the result curve
                    foreach (var keyframe in timeToKeyframe.Values.OrderBy(k => k.time))
                    {
                        resultCurve.AddKey(keyframe);
                    }
                }

                boneIdToBlendedCurve[boneId] = resultCurve;
            }
        }

        private bool GetSelectedCurve(string handlerId, int layerIndex, out AnimationCurve curve)
        {
            curve = null;
            if (layerIndex < 0 || layerIndex >= m_animationLayers.Count)
            {
                Debug.LogError($"No valid layer index selected, error value: {layerIndex}");
                return false;
            }

            var layerName = m_animationLayers[layerIndex];
            m_layerToBoneIdToCurve.TryGetValue(layerName, out var boneCurveMap);
            if (boneCurveMap == null)
            {
                Debug.LogError($"Animation layer data not found for {layerName}");
                return false;
            }

            boneCurveMap.TryGetValue(handlerId, out curve);
            if (curve == null)
            {
                Debug.LogError($"Curve data not found for bone {handlerId}");
                return false;
            }
            return true;
        }

        private bool GetSelectedCurves(int layerIndex, out Dictionary<string, AnimationCurve> boneCurveMap)
        {
            boneCurveMap = new Dictionary<string, AnimationCurve>();
            if (layerIndex < 0 || layerIndex >= m_animationLayers.Count)
            {
                Debug.LogError($"No valid layer index selected, error value: {layerIndex}");
                return false;
            }

            var layerName = m_animationLayers[layerIndex];
            m_layerToBoneIdToCurve.TryGetValue(layerName, out boneCurveMap);
            return true;
        }

        #endregion

        #region Blend Layer Panel

        private void DrawBlendLayerPanel()
        {
            if (m_animationLayers.Count == 0)
                return;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("Output Layer", GUILayout.Width(80));
                int mask = EditorGUILayout.MaskField(m_blendLayerMask, m_animationLayers.ToArray(), GUILayout.ExpandWidth(true));
                if (mask != m_blendLayerMask)
                {
                    m_blendLayerMask = mask;
                    m_blendedLayers.Clear();
                    for (int i = 0; i < m_animationLayers.Count; i++)
                        if ((m_blendLayerMask & (1 << i)) != 0)
                            m_blendedLayers.Add(m_animationLayers[i]);
                    m_blendLayerWeights.Clear();
                    m_blendLayerCurves.Clear();
                    foreach (var layerName in m_blendedLayers)
                    {
                        if (!m_blendLayerWeights.ContainsKey(layerName))
                            m_blendLayerWeights[layerName] = 1f / m_blendedLayers.Count;
                        if (!m_blendLayerCurves.ContainsKey(layerName))
                        {
                            AnimationCurve curve = new AnimationCurve();
                            curve.AddKey(0f, m_blendLayerWeights[layerName]);
                            curve.AddKey(m_layeredCurveEditor.Duration, m_blendLayerWeights[layerName]);
                            m_blendLayerCurves[layerName] = curve;
                        }
                    }

                    RefreshBlendCurvePanel();
                }
            }

            EditorGUI.BeginChangeCheck();
            m_blendMode = (BlendMode)EditorGUILayout.EnumPopup(m_blendMode, GUILayout.Width(100));
            if (EditorGUI.EndChangeCheck())
            {
                if (m_blendMode == BlendMode.Curves)
                    RefreshBlendCurvePanel();
            }

            EditorGUI.BeginDisabledGroup(!CanOutput);
            if (GUILayout.Button(m_blendedPreviewPlayer.IsPlaying ? "Pause" : "Play", GUILayout.Width(60)))
            {
                GetBlendCurves(out m_boneIdToBlendedCurve);
                m_blendedPreviewPlayer.PlayOrStop(m_boneIdToLayeredCurve);
            }

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            if (CanOutput)
            {
                if (CanBlend)
                {
                    if (m_blendMode == BlendMode.Curves)
                        m_blendCurveEditor.OnGUI();
                    else if (m_blendMode == BlendMode.Weights)
                        DrawWeightBlendPanel();
                }
                else
                    EditorGUILayout.HelpBox("Select two or more animation layers to blend", MessageType.Info);
            }
            else
                EditorGUILayout.HelpBox("Select at least one animation layer for output", MessageType.Info);

            EditorGUILayout.EndVertical();
        }

        private void DrawWeightBlendPanel()
        {
            if (m_blendLayerWeights.Count > 0)
            {
                float totalWeight = 0f;
                m_blendCurveTime = EditorGUILayout.Slider(m_blendCurveTime, 0, m_layeredCurveEditor.Duration);
                foreach (var layerName in m_blendedLayers)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(layerName, GUILayout.Width(150));
                    EditorGUI.BeginChangeCheck();
                    float newWeight = EditorGUILayout.Slider(m_blendLayerWeights[layerName], 0f, 1f);
                    if (EditorGUI.EndChangeCheck() && !Mathf.Approximately(newWeight, m_blendLayerWeights[layerName]))
                        m_blendLayerWeights[layerName] = newWeight;
                    totalWeight += m_blendLayerWeights[layerName];
                    EditorGUILayout.EndHorizontal();
                }

                if (Mathf.Abs(totalWeight - 1f) > 0.001f)
                    NormalizeBlendLayerWeights();
            }
        }

        private void RefreshBlendCurvePanel()
        {
            if (m_blendLayerCurves.Count == 0)
                return;
            m_blendCurveEditor.ClearCurves();
            int colorIndex = 0;
            foreach (var layerName in m_blendedLayers)
            {
                Color curveColor = GUIStyleHelper.s_Colors[colorIndex % GUIStyleHelper.s_Colors.Length];
                m_blendCurveEditor.AddCurve(m_blendLayerCurves[layerName], layerName, curveColor, colorIndex);
                colorIndex++;
            }
        }

        private void NormalizeBlendLayerWeights()
        {
            float totalWeight = 0f;
            foreach (var layerName in m_blendedLayers)
            {
                totalWeight += m_blendLayerWeights[layerName];
            }

            if (totalWeight > 0)
            {
                foreach (var layerName in m_blendedLayers)
                {
                    m_blendLayerWeights[layerName] /= totalWeight;
                }
            }
        }

        #endregion

        #region Preview Player Callbacks

        private void OnLayeredPreviewTimeChanged(float time)
        {
            m_layeredCurveEditor.CurrentTime = m_layerCurveTime = time;
            UpdateHandlerValues(m_boneIdToLayeredCurve, time);
        }

        private void OnBlendedPreviewTimeChanged(float time)
        {
            m_blendCurveEditor.CurrentTime = m_blendCurveTime = time;
            UpdateHandlerValues(m_boneIdToBlendedCurve, time);
        }

        #endregion
    }
}