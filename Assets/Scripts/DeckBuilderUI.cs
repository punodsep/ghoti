using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeckBuilderUI : MonoBehaviour
{
    [SerializeField] private FishCardUI cardPrefab;
    [SerializeField] private Transform deckContainer;
    [SerializeField] private Transform collectionContainer;

    [SerializeField] private TMP_Text deckCountText;
    [SerializeField] private TMP_Text infoText;

    [SerializeField] private Button toggleButton;
    [SerializeField] private TMP_Text toggleButtonLabel;
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;

    private FishData selected;
    private FishData swapTarget;   // ปลาใน Deck ที่ผู้เล่นเลือกไว้เพื่อให้ถูกแทนที่

    private void Awake()
    {
        toggleButton.onClick.AddListener(OnToggle);
        leftButton.onClick.AddListener(() => Move(-1));
        rightButton.onClick.AddListener(() => Move(+1));
    }

    private void OnEnable()
    {
        selected = null;
        swapTarget = null;   // <-- เพิ่ม
        Refresh();
    }

    private void Refresh()
    {
        PlayerProfile profile = SaveSystem.Profile;
        FishDatabase db = FishDatabase.Instance;

        Clear(deckContainer);
        Clear(collectionContainer);

        // แถว Deck (เล็กสุด -> ใหญ่สุด)
        for (int i = 0; i < profile.deckFishIds.Count; i++)
        {
            FishData f = db.Get(profile.deckFishIds[i]);
            if (f == null) continue;

            FishCardUI card = Instantiate(cardPrefab, deckContainer);
            card.Setup(f, true, false, f == swapTarget, OnCardClicked, i + 1);
        }

        // คอลเลกชันทั้งหมด
        foreach (FishData f in db.allFish)
        {
            if (f == null) continue;

            bool unlocked = profile.IsUnlocked(f.id);
            bool inDeck = profile.deckFishIds.Contains(f.id);

            FishCardUI card = Instantiate(cardPrefab, collectionContainer);
            card.Setup(f, unlocked, inDeck, f == selected, OnCardClicked);
        }

        deckCountText.text =
            $"Deck {profile.deckFishIds.Count}/{GameSession.MaxDeck}   (small > big)";

        UpdateInfo();
    }

    private void Clear(Transform container)
    {
        foreach (Transform child in container)
        {
            child.gameObject.SetActive(false);   // ให้ Layout ไม่นับทันที
            Destroy(child.gameObject);
        }
    }

    private void OnCardClicked(FishData fish)
    {
        selected = fish;

        // ถ้าแตะปลาที่อยู่ใน Deck ให้จำไว้เป็นตัวที่จะถูกสลับออก
        if (SaveSystem.Profile.deckFishIds.Contains(fish.id))
        {
            swapTarget = fish;
        }

        Refresh();
    }

    private void UpdateInfo()
    {
        PlayerProfile profile = SaveSystem.Profile;

        if (selected == null)
        {
            infoText.text = "แตะที่ปลาเพื่อดูรายละเอียด";
            toggleButton.interactable = false;
            leftButton.interactable = false;
            rightButton.interactable = false;
            toggleButtonLabel.text = "ใส่ใน Deck";
            return;
        }

        bool unlocked = profile.IsUnlocked(selected.id);
        int index = profile.deckFishIds.IndexOf(selected.id);
        bool inDeck = index >= 0;
        int count = profile.deckFishIds.Count;

        string ability = selected.ability != null
            ? $"{selected.ability.abilityName}: {selected.ability.description}"
            : "ไม่มีความสามารถพิเศษ";

        string title = unlocked ? selected.displayName : "ปลาที่ยังไม่ปลดล็อก";
        string body = unlocked ? ability : ProgressService.UnlockInfo(selected);
        infoText.text = $"{title}  [{selected.rarity}]\n{body}";

        // ปลาใน Deck: ไม่มีปุ่มเอาออก (ต้องครบ 7 เสมอ) ให้เลือกปลานอก Deck มาสลับแทน
        toggleButton.interactable = unlocked && !inDeck;
        toggleButtonLabel.text = inDeck
            ? "อยู่ใน Deck แล้ว"
            : (swapTarget != null
                ? $"สลับกับ {swapTarget.displayName}"
                : "แตะปลาใน Deck ที่จะสลับ");

        leftButton.interactable = inDeck && index > 0;
        rightButton.interactable = inDeck && index < count - 1;
    }

    private void OnToggle()
    {
        if (selected == null || swapTarget == null) return;

        PlayerProfile profile = SaveSystem.Profile;
        if (!profile.IsUnlocked(selected.id)) return;

        int index = profile.deckFishIds.IndexOf(swapTarget.id);
        if (index < 0 || profile.deckFishIds.Contains(selected.id)) return;

        profile.deckFishIds[index] = selected.id;
        swapTarget = selected;

        SaveSystem.Save();
        Refresh();
    }

    private void Move(int direction)
    {
        if (selected == null) return;

        var ids = SaveSystem.Profile.deckFishIds;
        int index = ids.IndexOf(selected.id);
        int target = index + direction;

        if (index < 0 || target < 0 || target >= ids.Count) return;

        (ids[index], ids[target]) = (ids[target], ids[index]);

        SaveSystem.Save();
        Refresh();
    }
}