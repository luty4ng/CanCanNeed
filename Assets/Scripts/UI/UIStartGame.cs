using UnityEngine;
using Cysharp.Threading.Tasks;
using GameProcedure;
using UnityEngine.SceneManagement;

namespace GameLogic
{
    public class UIStartGame : UIViewModelBase
    {
        [SerializeField] private UIButton m_btnStartGame;
        [SerializeField] private UIButton m_btnOption;
        [SerializeField] private UIButton m_btnCredit;
        [SerializeField] private UIButton m_btnQuit;

        private void Awake()
        {
            m_btnStartGame.onClick.AddListener(UniTask.UnityAction(OnStartGameBtnClick));
            m_btnOption.onClick.AddListener(UniTask.UnityAction(OnOptionBtnClick));
            m_btnCredit.onClick.AddListener(UniTask.UnityAction(OnCreditBtnClick));
            m_btnQuit.onClick.AddListener(UniTask.UnityAction(OnQuitBtnClick));
            m_btnStartGame.Text = "Start Game";
            m_btnOption.Text = "Options";
            m_btnCredit.Text = "Credits";
            m_btnQuit.Text = "Quit";
        }

        private void OnDestroy()
        {
            m_btnStartGame.onClick.RemoveAllListeners();
            m_btnOption.onClick.RemoveAllListeners();
            m_btnCredit.onClick.RemoveAllListeners();
            m_btnQuit.onClick.RemoveAllListeners();
        }
        
        #region 事件
        private async UniTaskVoid OnStartGameBtnClick()
        {
            await UniTask.Yield();
            SceneManager.LoadScene("GameMain");
            GameManager.Instance.HideUI(GetType());
        }
        private async UniTaskVoid OnQuitBtnClick()
        {
            await UniTask.Yield();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private async UniTaskVoid OnCreditBtnClick()
        {
            await UniTask.Yield();
        }

        private async UniTaskVoid OnOptionBtnClick()
        {
            await UniTask.Yield();
        }
        #endregion
    }
}