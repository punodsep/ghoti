using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FishCardUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text orderText;       // เลขลำดับ (เฉพาะแถว Deck)
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private GameObject inDeckBadge;
    [SerializeField] private GameObject selectedFrame;

    public void Setup(FishData fish, bool unlocked, bool inDeck, bool selected,
                      Action<FishData> onClick, int order = 0)
    {
        icon.sprite = fish.sprite;
        icon.preserveAspect = true;
        icon.color = unlocked ? Color.white : new Color(0f, 0f, 0f, 0.85f);

        nameText.text = unlocked ? fish.displayName : "???";

        lockedOverlay.SetActive(!unlocked);
        inDeckBadge.SetActive(inDeck);
        selectedFrame.SetActive(selected);

        orderText.gameObject.SetActive(order > 0);
        if (order > 0) orderText.text = order.ToString();

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick(fish));
    }
}