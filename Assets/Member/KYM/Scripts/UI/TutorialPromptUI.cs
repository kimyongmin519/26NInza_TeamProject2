using TMPro;
using UnityEngine;

namespace Member.KYM.Scripts.UI
{
    public class TutorialPromptUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text descriptionText;

        public void Show(string description)
        {
            if (descriptionText != null)
                descriptionText.SetText(description);

            if (panel != null)
                panel.SetActive(true);
        }

        public void Hide()
        {
            if (panel != null)
                panel.SetActive(false);
        }
    }
}
