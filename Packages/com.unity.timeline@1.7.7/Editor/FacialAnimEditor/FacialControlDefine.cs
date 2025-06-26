using System;
using System.Collections.Generic;
using UnityEngine;

namespace Audio2Face
{
    [Serializable]
    public class FacialLayoutRect
    {
        public Vector2 center;
        public float width;
        public float height;
        public float radius;
    }

    [Serializable]
    public class FacialUIPad
    {
        public Vector2 center;
        public Vector2 value;
        public float width;
        public float height;
        public float radius;
    }

    [Serializable]
    public class FacialLayoutSlider
    {
        public string bindingId;
        public Vector2 startPosition = Vector2.zero;
        public Vector2 endPosition = Vector2.one;
    }

    [Serializable]
    public class FacialLayoutEllipse
    {
        public Vector2 center;
        public float width = 1f;
        public float height = 1f;
        public int segments = 200;
    }

    [Serializable]
    public class FacialUISettings
    {
        public List<FacialLayoutRect> rects;
        public List<FacialLayoutSlider> lines;
        public List<FacialLayoutEllipse> ellipses;
    }
}