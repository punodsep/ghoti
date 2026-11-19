using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GachaUI : MonoBehaviour
{
    public const int MultiPullCount = 10;

    [SerializeField] private Button pullButton;
    [SerializeField] private TMP_Text pullButtonLabel;
    [SerializeField] private TMP_Text pityText;
    [SerializeField] private Image resultImage;
    [SerializeField] private TMP_Text resultText;

    [Header("สุ่ม 10 ครั้ง (ว่างได้)")]
    [SerializeField] private Button pullTenButton;
    [SerializeField] private TMP_Text pullTenButtonLabel;
    [Tooltip("ที่แสดงผล 10 ตัว (ควรมี GridLayoutGroup)")]
    [SerializeField] private Transform multiResultGrid;

    private void Awake()
    {
        pullButton.onClick.AddListener(PullOne);
        if (pullTenButton != null) pullTenButton.onClick.AddListener(PullTen);
    }

    private void OnEnable()
    {
        ShowSingle(null);
        ClearGrid();
        resultText.text = "";
        Refresh();
    }

    private void Refresh()
    {
        int coins = SaveSystem.Profile.coins;
        bool hasPool = GachaSystem.GetPool().Count > 0;

        pullButtonLabel.text = $"1 PULL\n<size=60%>{GachaSystem.PullCost} เหรียญ</size>";
        pullButton.interactable = GachaSystem.CanPull;

        if (pullTenButton != null)
        {
            int cost = GachaSystem.PullCost * MultiPullCount;
            pullTenButtonLabel.text = $"{MultiPullCount} PULLS\n<size=60%>{cost} เหรียญ</size>";
            pullTenButton.interactable = hasPool && coins >= cost;
        }

        pityText.text = $"อีก {GachaSystem.PullsUntilPity} ครั้ง การันตี Epic ขึ้นไป";
    }

    private void PullOne()
    {
        if (!GachaSystem.TryPull(out GachaResult r)) return;

        ClearGrid();
        ShowSingle(r.fish);
        resultText.text = r.isNew
            ? $"ได้ปลาใหม่!\n{r.fish.displayName} [{r.fish.rarity}]"
            : $"{r.fish.displayName} [{r.fish.rarity}]\nซ้ำ คืน {r.refund} เหรียญ";

        Refresh();
    }

    private void PullTen()
    {
        if (SaveSystem.Profile.coins < GachaSystem.PullCost * MultiPullCount) return;

        List<GachaResult> results = new List<GachaResult>();
        for (int i = 0; i < MultiPullCount; i++)
        {
            if (!GachaSystem.TryPull(out GachaResult r)) break;
            results.Add(r);
        }
        if (results.Count == 0) return;

        ShowSingle(null);
        ClearGrid();

        int newCount = 0, refund = 0;
        foreach (GachaResult r in results)
        {
            if (r.isNew) newCount++;
            refund += r.refund;
            AddGridIcon(r);
        }

        resultText.text = $"ได้ปลาใหม่ {newCount} ตัว" + (refund > 0 ? $"  ·  ซ้ำ คืน {refund} เหรียญ" : "");
        Refresh();
    }

    private void ShowSingle(FishData fish)
    {
        resultImage.gameObject.SetActive(fish != null);
        if (fish == null) return;
        resultImage.sprite = fish.sprite;
        resultImage.preserveAspect = true;
    }

    private void ClearGrid()
    {
        if (multiResultGrid == null) return;
        foreach (Transform child in multiResultGrid)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }

    /// ไอคอนผลสุ่ม 1 ช่อง: กรอบสีตามความแรร์ + รูปปลา + ป้าย NEW
    private void AddGridIcon(GachaResult r)
    {
        if (multiResultGrid == null) return;

        GameObject cell = new GameObject(r.fish.displayName, typeof(RectTransform));
        cell.layer = multiResultGrid.gameObject.layer;
        cell.transform.SetParent(multiResultGrid, false);
        cell.AddComponent<Image>().color = FishCardUI.RarityColor(r.fish.rarity);

        GameObject pic = new GameObject("Icon", typeof(RectTransform));
        pic.layer = cell.layer;
        pic.transform.SetParent(cell.transform, false);
        Image img = pic.AddComponent<Image>();
        img.sprite = r.fish.sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
        RectTransform rt = (RectTransform)pic.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(8f, 8f);
        rt.offsetMax = new Vector2(-8f, -8f);

        if (r.isNew)
        {
            GameObject tag = new GameObject("New", typeof(RectTransform));
            tag.layer = cell.layer;
            tag.transform.SetParent(cell.transform, false);
            TextMeshProUGUI t = tag.AddComponent<TextMeshProUGUI>();
            t.font = resultText.font;
            t.text = "NEW";
            t.fontSize = 26f;
            t.fontStyle = FontStyles.Bold;
            t.color = new Color(1f, 0.3f, 0.35f);
            t.alignment = TextAlignmentOptions.TopRight;
            t.raycastTarget = false;
            RectTransform trt = (RectTransform)tag.transform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(4f, 4f);
            trt.offsetMax = new Vector2(-6f, -2f);
        }
    }
}
