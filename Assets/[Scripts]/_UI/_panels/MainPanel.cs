using UnityEngine.UI;
using UnityEngine;
namespace GAITemplate
{
    public class MainPanel : Panel
    {
        public Text levelText;
        public Text moneyText;

        private void Start()
        {
            levelText.text = GameManager.instance.Data.FormatLevelText(GameManager.instance.level);
            moneyText.text = GameManager.instance.money.ToString();

        }
    }
}