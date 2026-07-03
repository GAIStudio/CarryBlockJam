using TMPro;
using UnityEngine;

namespace GAITemplate
{
    public class TutorialPanel : Panel
    {
        public TextMeshProUGUI instruction;
        public RectTransform   hand;
        public RectTransform   handVisual;
        public RectTransform   circleVisual;

        // Tutorial manager runtime'da set ederek paneli kontrol eder.
        // Bus/event system varsa OnEnable / OnDisable ile entegre edilir.
    }
}
