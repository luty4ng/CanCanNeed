using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Audio2Face
{
    public static class GUIStyleHelper
    {
        public static Color[]  s_Colors = new Color[]
        {
            new Color(1f, 0.5f, 0.5f),
            new Color(0.5f, 1f, 0.5f),
            new Color(0.5f, 0.5f, 1f),
            new Color(1f, 1f, 0.5f),
            new Color(1f, 0.5f, 1f),
            new Color(0.5f, 1f, 1f)
        };

        public static void Refresh()
        {
            s_centeredLabel = null;
            s_tabDefaultBtnStyle = null;
            s_tabActiveBtnStyle = null;
            s_tabContentStyle = null;
        }
        
        private static GUIStyle s_centeredLabel;
        public static GUIStyle CenteredLabel
        {
            get
            {
                if (s_centeredLabel == null)
                {
                    s_centeredLabel = new GUIStyle(GUI.skin.label);
                    s_centeredLabel.alignment = TextAnchor.MiddleCenter;
                    s_centeredLabel.fontStyle = FontStyle.Bold;
                }
                return s_centeredLabel;
            }
        }
        
        private static GUIStyle s_tabDefaultBtnStyle;
        public static GUIStyle TabDefaultBtnStyle
        {
            get
            {
                if (s_tabDefaultBtnStyle == null)
                {
                    s_tabDefaultBtnStyle = new GUIStyle(EditorStyles.toolbarButton);
                    s_tabDefaultBtnStyle.normal.background = CreateColorTexture(new Color(0.2f, 0.2f, 0.2f));
                    s_tabDefaultBtnStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
                    s_tabDefaultBtnStyle.fixedHeight = 25;
                    s_tabDefaultBtnStyle.margin = new RectOffset(0, 0, 0, 0);
                    s_tabDefaultBtnStyle.padding = new RectOffset(10, 10, 5, 5);
                    s_tabDefaultBtnStyle.alignment = TextAnchor.MiddleCenter;
                    s_tabDefaultBtnStyle.fontStyle = FontStyle.Normal;
                    s_tabDefaultBtnStyle.fontSize = 12;
                }
                return s_tabDefaultBtnStyle;
            }
        }
        
        private static GUIStyle s_tabActiveBtnStyle;
        public static GUIStyle TabActiveBtnStyle
        {
            get
            {
                if (s_tabActiveBtnStyle == null)
                {
                    s_tabActiveBtnStyle = new GUIStyle(TabDefaultBtnStyle);
                    s_tabActiveBtnStyle.normal.background = CreateColorTexture(new Color(0.25f, 0.25f, 0.25f));
                    s_tabActiveBtnStyle.normal.textColor = Color.white;
                    s_tabActiveBtnStyle.fontStyle = FontStyle.Normal;
                }
                return s_tabActiveBtnStyle;
            }
        }

        private static GUIStyle s_tabContentStyle;
        public static GUIStyle TabContentStyle
        {
            get
            {

                if (s_tabContentStyle == null)
                {
                    s_tabContentStyle = new GUIStyle(GUI.skin.box);
                    s_tabContentStyle.normal.background = CreateColorTexture(new Color(0.25f, 0.25f, 0.25f));
                    s_tabContentStyle.margin = new RectOffset(0, 0, 0, 0);
                    s_tabContentStyle.padding = new RectOffset(5, 5, 5, 5);
                }
                return s_tabContentStyle;
            }
        }
        
        private static Texture2D CreateColorTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}