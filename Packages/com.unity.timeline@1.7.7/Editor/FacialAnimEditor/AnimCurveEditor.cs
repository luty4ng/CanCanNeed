using System;
using System.Collections.Generic;
using UnityEngine;

namespace Audio2Face
{
    public class AnimCurveEditor
    {
        private CurveEditorWrapper m_CurveEditorWrapper;
        public event Action<float> OnTimeChanged
        {
            add { m_CurveEditorWrapper.OnTimeChanged += value; }
            remove { m_CurveEditorWrapper.OnTimeChanged -= value; }
        }
        
        public event Action OnTimeSliderClicked
        {
            add { m_CurveEditorWrapper.OnTimeSliderClicked += value; }
            remove { m_CurveEditorWrapper.OnTimeSliderClicked -= value; }
        }
        
        public event Action<float, float> OnLocalIntervalChanged
        {
            add { m_CurveEditorWrapper.OnLocalIntervalChanged += value; }
            remove { m_CurveEditorWrapper.OnLocalIntervalChanged -= value; }
        }
        
        public event Action OnCurvesModified
        {
            add { m_CurveEditorWrapper.OnCurvesModified += value; }
            remove { m_CurveEditorWrapper.OnCurvesModified -= value; }
        }

        public float CurrentTime
        {
            get => m_CurveEditorWrapper.CurrentTime;
            set => m_CurveEditorWrapper.CurrentTime = value;
        }
        
        public int CurrentFrame
        {
            get => m_CurveEditorWrapper.CurrentFrame;
            set => m_CurveEditorWrapper.CurrentFrame = value;
        }
        
        public float Duration
        {
            get => m_CurveEditorWrapper.Duration;
            set => m_CurveEditorWrapper.Duration = value;
        }
        
        public bool InLiveEdit => m_CurveEditorWrapper.InLiveEdit;
        public bool IsActive => m_CurveEditorWrapper.IsActive;
        
        public AnimCurveEditor(float frameRate = 30f)
        {
            m_CurveEditorWrapper = new CurveEditorWrapper(frameRate);
        }
        public void OnDisable()
        {
            m_CurveEditorWrapper.OnDisable();
            m_CurveEditorWrapper.CurrentTime = 0;
        }

        public void OnGUI()
        {
            m_CurveEditorWrapper.OnGUI();
        }
        
        public void AddCurve(AnimationCurve curve, string name, Color color, int id)
        {
            m_CurveEditorWrapper.AddCurve(curve, name, color, id);
        }

        public void ClearCurves()
        {
            m_CurveEditorWrapper.ClearCurves();
        }

        public AnimationCurve GetCurve(int id)
        {
            return m_CurveEditorWrapper.GetWrapperCurve(id);
        }
        
        public Dictionary<string, float> GetCurrentValues()
        {
            return m_CurveEditorWrapper.GetCurrentValues();
        }
        
        public void FrameSelected()
        {
            m_CurveEditorWrapper.FrameSelected();
        }

        public void UpdateCurveEditor()
        {
            m_CurveEditorWrapper.UpdateCurveEditor();
        }
    }
}