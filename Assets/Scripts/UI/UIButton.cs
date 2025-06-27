using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public class UIButton : Button
    {
        [SerializeField] private Image m_icon;
        [SerializeField] private TextMeshProUGUI m_textMeshUGUI;
        
        public bool ShowIcon
        {
            get => m_icon.gameObject.activeSelf;
            set
            {
                m_icon.gameObject.SetActive(value);
                m_textMeshUGUI.gameObject.SetActive(!value);
            }
        }

        public Sprite Icon
        {
            get => m_icon.sprite;
            set => m_icon.sprite = value;
        }
        
        public float FontSize
        {
            get => m_textMeshUGUI.fontSize;
            set => m_textMeshUGUI.fontSize = value;
        }

        public string Text
        {
            get => m_textMeshUGUI.text;
            set => m_textMeshUGUI.text = value;
        }
        
        public VerticalAlignmentOptions VerticalAlignment
        {
            get => m_textMeshUGUI.verticalAlignment;
            set => m_textMeshUGUI.verticalAlignment = value;
        }
        
        public HorizontalAlignmentOptions HorizontalAlignment
        {
            get => m_textMeshUGUI.horizontalAlignment;
            set => m_textMeshUGUI.horizontalAlignment = value;
        }
    }
}