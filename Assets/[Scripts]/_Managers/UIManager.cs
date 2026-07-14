using UnityEngine;

namespace GAITemplate
{
    public class UIManager : MonoBehaviour
    {
        public MainPanel mainPanel;
        public GamePanel gamePanel;
        public EndPanel endPanel;
        public TutorialPanel tutorialPanel;

        #region Singleton
        public static UIManager instance = null;
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }

            // Panel initial state'lerini Awake'de set et — Start'lar başlamadan önce.
            // Bu sayede LevelManager.Start → BuildLevel → TutorialManager.StartTutorial
            // çalıştığında panel artık deaktive edilmez.
            if (gamePanel     != null) gamePanel.Active(true);
            if (endPanel      != null) endPanel.Active(false);
            if (tutorialPanel != null) tutorialPanel.Active(false);
        }
        #endregion

        private void Start()
        {
            if (LevelManager.instance == null)
            {
                Debug.LogWarning("[UIManager] LevelManager is missing. End/fail UI will not bind.");
                return;
            }

            LevelManager.instance.startEvent.AddListener(StartGame);
            LevelManager.instance.endGameEvent.AddListener(EndGame);
        }

        public void StartGame()
        {
            if (gamePanel != null)
                gamePanel.ActiveSmooth(true);
            if (mainPanel != null)
                mainPanel.ActiveSmooth(false);
        }

        public void EndGame(bool success)
        {
            if (endPanel == null)
            {
                Debug.LogWarning("[UIManager] EndPanel is not assigned. Cannot show success/fail UI.");
                return;
            }

            if (success)
            {
                if (endPanel.fail != null)
                    endPanel.fail.gameObject.SetActive(false);
                endPanel.ActiveSmooth(true);
                endPanel.Success();
            }
            else
            {
                if (endPanel.success != null)
                    endPanel.success.gameObject.SetActive(false);
                endPanel.ActiveSmooth(true);
                endPanel.Fail();
            }

            if (gamePanel != null)
                gamePanel.ActiveSmooth(false);
        }
    }
}