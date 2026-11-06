using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GachaUI : MonoBehaviour
{
    [SerializeField] private Button pullButton;
    [SerializeField] private TMP_Text pullButtonLabel;
    [SerializeField] private TMP_Text pityText;
    [SerializeField] private Image resultImage;
    [SerializeField] private TMP_Text resultText;

    private void Awake()
    {
        pullButton.onClick.AddListener(Pull);
    }

    private void OnEnable()
    {
        resultImage.gameObject.SetActive(false);
        resultText.text = "";
        Refresh();
    }

    private void Refresh()
    {
        pullButtonLabel.text = $"สุ่ม ({GachaSystem.PullCost} เหรียญ)";
        pullButton.interactable = GachaSystem.CanPull;
        pityText.text = $"อีก {GachaSystem.PullsUntilPity} ครั้ง การันตี Epic ขึ้นไป";
    }

    private void Pull()
    {
        if (!GachaSystem.TryPull(out GachaResult r)) return;

        resultImage.sprite = r.fish.sprite;
        resultImage.preserveAspect = true;
        resultImage.gameObject.SetActive(true);

        resultText.text = r.isNew
            ? $"ได้ปลาใหม่\n{r.fish.displayName} [{r.fish.rarity}]"
            : $"{r.fish.displayName} [{r.fish.rarity}]\nซ้ำ คืน {r.refund} เหรียญ";

        Refresh();
    }
}