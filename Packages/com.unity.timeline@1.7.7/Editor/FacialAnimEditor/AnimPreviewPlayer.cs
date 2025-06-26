using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Audio2Face
{
    public class AnimPreviewPlayer
    {
        private float m_previewTime = 0f;
        private Animator m_animator;
        private AnimationClip m_previewClip;
        private AnimationClip m_sourceClip;
        private AudioSource m_audioSource;
        private AudioClip m_audioClip;
        private PlayableGraph m_playableGraph;
        private AnimationPlayableOutput m_playableOutput;
        private AnimationClipPlayable m_clipPlayable;
        
        private float m_duration = 1.0f;
        private float m_intervalStart = 0f;
        private float m_intervalEnd = 0f;
        private bool m_intervalStartSetted = false;
        private bool m_intervalEndSetted = false;
        
        public event Action<float> OnTimeChanged;
        private bool m_isPlaying = false;
        public bool IsPlaying => m_isPlaying;
        
        // 初始化预览播放器
        public void ReBind(Animator animator, AnimationClip sourceClip, AudioSource audioSource = null, AudioClip audioClip = null)
        {
            m_animator = animator;
            m_sourceClip = sourceClip;
            m_audioSource = audioSource;
            m_audioClip = audioClip;
            m_duration = sourceClip.length;
            m_intervalStart = 0;
            m_intervalEnd = m_duration;
            
            if (m_previewClip == null)
            {
                m_previewClip = new AnimationClip();
                if (sourceClip != null)
                {
                    m_previewClip.legacy = sourceClip.legacy;
                    m_previewClip.frameRate = sourceClip.frameRate;
                }
            }
            
            if (m_playableGraph.IsValid())
                m_playableGraph.Destroy();
            m_playableGraph = PlayableGraph.Create("PreviewGraph");
            m_playableGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            
            if (m_animator != null)
                m_playableOutput = AnimationPlayableOutput.Create(m_playableGraph, "PreviewOutput", m_animator);
            EditorApplication.update += OnUpdate;
        }
        
        // 清理资源
        public void Cleanup()
        {
            Stop();
            if (m_playableGraph.IsValid())
            {
                m_playableGraph.Destroy();
                m_playableGraph = default;
            }

            if (m_previewClip != null)
            {
                UnityEngine.Object.DestroyImmediate(m_previewClip);
                m_previewClip = null;
            }
            
            EditorApplication.update -= OnUpdate;
        }

        public void UpdateLocalPlayInterval(float start, float end)
        {
            m_intervalStartSetted = start >= 0;
            m_intervalEndSetted = end >= 0;
            m_intervalStart = m_intervalStartSetted ? start : 0;
            m_intervalEnd = m_intervalEndSetted ? end : m_duration;
        }
        
        // 重建预览动画片段
        public void RebuildAnimClip(Dictionary<string, AnimationCurve> idToCurves)
        {
            if (m_previewClip == null)
                return;
                
            m_previewClip.ClearCurves();
            foreach (var idToCurve in idToCurves)
            {
                string id = idToCurve.Key;
                EditorCurveBinding binding = new EditorCurveBinding
                {
                    path = id,
                    propertyName = "m_LocalPosition.x",
                    type = typeof(Transform)
                };
                AnimationUtility.SetEditorCurve(m_previewClip, binding, idToCurve.Value);
            }
        }
        
        private void RebuildPlayableGraph(float playStartTime)
        {
            if(m_clipPlayable.IsValid() && m_clipPlayable.CanDestroy())
                m_clipPlayable.Destroy();
            m_clipPlayable = AnimationClipPlayable.Create(m_playableGraph, m_previewClip);
            m_clipPlayable.SetSpeed(1);
            m_clipPlayable.SetTime(playStartTime);
            m_playableOutput.SetSourcePlayable(m_clipPlayable);
            m_playableGraph.Evaluate(0);
            m_playableGraph.Play();
        }
        
        public void Seek(float time, Dictionary<string, AnimationCurve> newCurveMap = null)
        {
            if(!m_playableGraph.IsValid() || m_previewClip == null)
                return;
            if (newCurveMap != null)
                RebuildAnimClip(newCurveMap);
            Stop();
            m_previewTime = time;
            RebuildPlayableGraph(m_previewTime);
            OnTimeChanged?.Invoke(m_previewTime);
        }
        
        public void Play(float time, Dictionary<string, AnimationCurve> newCurveMap = null)
        {
            if (!m_playableGraph.IsValid() || m_previewClip == null)
                return;
            if (newCurveMap != null)
                RebuildAnimClip(newCurveMap);
            m_previewTime = time;
            PlayAudioIfPossible(m_previewTime);
            RebuildPlayableGraph(m_previewTime);
            m_isPlaying = true;
        }

        private void PlayAudioIfPossible(float time)
        {
            if (m_audioClip != null && m_audioSource != null)
            {
                m_audioSource.clip = m_audioClip;
                m_audioSource.time = time;
                m_audioSource.Play();
            }
        }
        
        public void Stop()
        {
            m_isPlaying = false;
            if (m_audioSource != null)
                m_audioSource.Stop();
        }
        
        public void PlayOrStop(Dictionary<string, AnimationCurve> newCurveMap = null)
        {
            if (m_isPlaying)
                Stop();
            else
                Play(m_intervalStartSetted ? m_intervalStart : m_previewTime, newCurveMap);
        }
        
        private void OnUpdate()
        {
            if (m_sourceClip ==null || !m_isPlaying || !m_playableGraph.IsValid())
                return;
            
            m_previewTime += Time.deltaTime;
            m_clipPlayable.SetTime(m_previewTime);
            m_playableGraph.Evaluate(0);
            if (m_previewTime >= m_intervalEnd)
            {
                if(!m_intervalEndSetted)
                    Stop();
                m_previewTime = m_intervalStart;
            }
            OnTimeChanged?.Invoke(m_previewTime);
        }
    }
}