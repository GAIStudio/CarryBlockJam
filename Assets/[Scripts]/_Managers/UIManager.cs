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
            LevelManager.instance.startEvent.AddListener(StartGame);
            LevelManager.instance.endGameEvent.AddListener(EndGame);
        }

        public void StartGame()
        {
            gamePanel.ActiveSmooth(true);
            mainPanel.ActiveSmooth(false);
        }

        public void EndGame(bool success)
        {
            endPanel.ActiveSmooth(true);
            gamePanel.ActiveSmooth(false);

            if (success) endPanel.Success();
            else endPanel.Fail();
        }
    }
}