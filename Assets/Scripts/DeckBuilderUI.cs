using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// หน้าจัด Deck: ช่องที่ N ของ Deck = merging layer N และปลาแต่ละตัวใส่ได้เฉพาะ layer ของตัวเอง
/// แถวเครื่องมือเหนือรายการปลา: [ ค้นหา........ ] [ Layer ▼ ]  (เลือกได้หลาย layer เช่น 1, 3, 4)
public class DeckBuilderUI : MonoBehaviour
{
    [SerializeField] private FishCardUI cardPrefab;
    [SerializeField] private Transform deckContainer;
    [SerializeField] private Transform collectionContainer;

    [SerializeField] private TMP_Text deckCountText;
    [SerializeField] private TMP_Text infoText;

    [SerializeField] private Button toggleButton;
    [SerializeField] private TMP_Text toggleButtonLabel;
    [SerializeField] private Button leftButton;    // ไม่ใช้แล้ว (layer ล็อกตายตัว) — ถูกซ่อน
    [SerializeField] private Button rightButton;

    [Header("ป๊อปอัปรายละเอียดปลา (ว่างได้ = ใช้ infoText ที่อยู่ในหน้าเลย)")]
    [SerializeField] private GameObject detailPopup;
    [SerializeField] private Image detailIcon;
    [SerializeField] private Button detailCloseButton;

    [Header("แถวค้นหา/กรอง (สร้างอัตโนมัติ)")]
    [Tooltip("ที่วางแถว [ค้นหา][Layer] — ว่าง = แทรกเหนือรายการปลาใน Layout Group ของหน้านี้")]
    [SerializeField] private RectTransform toolbarParent;
    [SerializeField] private float toolbarHeight = 64f;
    [SerializeField] private float layerButtonWidth = 220f;
    [SerializeField] private Color chipOnColor = new Color(0.22f, 0.56f, 0.95f);
    [SerializeField] private Color chipOffColor = new Color32(0xD6, 0xD6, 0xD6, 0xFF);

    private FishData selected;     // ปลาที่แตะเลือกไว้ (ไฮไลต์) — แตะช่อง Deck เพื่อใส่
    private FishData detailFish;   // ปลาที่เปิดป๊อปอัปรายละเอียดอยู่
    private string query = "";
    private readonly SortedSet<int> layerFilter = new SortedSet<int>();   // ว่าง = ทุก layer
    private readonly HashSet<FishRarity> rarityFilter = new HashSet<FishRarity>();   // ว่าง = ทุกความแรร์

    private readonly List<FishCardUI> deckCards = new List<FishCardUI>();
    private readonly List<FishCardUI> collectionCards = new List<FishCardUI>();

    private TMP_InputField searchInput;
    private TMP_Text layerButtonLabel;
    private RectTransform layerButtonRect;
    private GameObject popupRoot;
    private RectTransform popupPanel;
    private readonly List<(int layer, Image bg, TMP_Text label)> chips = new List<(int, Image, TMP_Text)>();
    private readonly List<(FishRarity rarity, Image bg, TMP_Text label)> rarityChips = new List<(FishRarity, Image, TMP_Text)>();
    private TMP_FontAsset font;

    private CanvasGroup toast;
    private TMP_Text toastText;
    private Coroutine toastRoutine;

    private TMP_Text resetLabel;
    private float resetConfirmUntil;
    private const float ResetConfirmWindow = 3f;

    private void Awake()
    {
        font = infoText != null ? infoText.font : null;

        toggleButton.onClick.AddListener(OnPutDetailFish);
        if (detailCloseButton != null) detailCloseButton.onClick.AddListener(CloseDetail);
        if (leftButton != null) leftButton.gameObject.SetActive(false);
        if (rightButton != null) rightButton.gameObject.SetActive(false);

        BuildToolbar();
        BuildLayerPopup();
        BuildToast();
        BuildResetButton();
    }

    private void Update()
    {
        // หมดเวลายืนยัน → คืนข้อความปุ่ม
        if (resetLabel != null && resetConfirmUntil > 0f && Time.unscaledTime > resetConfirmUntil)
        {
            resetConfirmUntil = 0f;
            resetLabel.text = "Reset Deck";
        }
    }

    private void OnEnable()
    {
        selected = null;
        detailFish = null;
        query = "";
        layerFilter.Clear();
        rarityFilter.Clear();
        if (searchInput != null) searchInput.SetTextWithoutNotify("");
        ClosePopup();
        if (toast != null) toast.alpha = 0f;
        RefreshAll();
    }

    // ---------------------------------------------------------------- refresh

    private void RefreshAll()
    {
        RefreshDeck();
        RefreshCollection();
        RefreshFilterUI();
        UpdateInfo();
    }

    private void RefreshDeck()
    {
        PlayerProfile profile = SaveSystem.Profile;
        FishDatabase db = FishDatabase.Instance;

        Clear(deckContainer);
        deckCards.Clear();

        // แถว Deck: ช่อง 1 (เล็กสุด) → 7 (ใหญ่สุด) ช่องว่างแสดงเป็น "ว่าง"
        int filled = 0;
        for (int layer = 1; layer <= GameSession.MaxDeck; layer++)
        {
            string id = layer - 1 < profile.deckFishIds.Count ? profile.deckFishIds[layer - 1] : "";
            FishData f = db.Get(id);

            FishCardUI card = Instantiate(cardPrefab, deckContainer);
            if (f != null)
            {
                card.Setup(f, true, false, false, null, layer);
                card.WithInfo(OpenDetail);
                filled++;
            }
            else
            {
                card.SetupEmptySlot(layer);
            }
            card.AsSlot(layer, PutInLayer, OnSlotClicked, OnSlotDoubleClicked);
            deckCards.Add(card);
        }

        if (deckCountText != null) deckCountText.text = $"Deck {filled}/{GameSession.MaxDeck}   (layer 1 > 7)";
        UpdateSelectionVisuals();
    }

    private void RefreshCollection()
    {
        PlayerProfile profile = SaveSystem.Profile;
        FishDatabase db = FishDatabase.Instance;

        Clear(collectionContainer);
        collectionCards.Clear();

        foreach (FishData f in FishSearch.Filter(db.allFish, query))
        {
            if (layerFilter.Count > 0 && !layerFilter.Contains(f.layer)) continue;
            if (rarityFilter.Count > 0 && !rarityFilter.Contains(f.rarity)) continue;

            bool unlocked = profile.IsUnlocked(f.id);
            bool inDeck = profile.deckFishIds.Contains(f.id);

            FishCardUI card = Instantiate(cardPrefab, collectionContainer);
            card.Setup(f, unlocked, inDeck, false, OnCollectionCardClicked, f.layer);
            card.WithInfo(OpenDetail);
            if (unlocked && !inDeck) card.AsDraggable(OnDragStarted);
            collectionCards.Add(card);
        }

        UpdateSelectionVisuals();
    }

    /// ไฮไลต์ปลาที่เลือก + ช่อง Deck ที่มันใส่ได้ (ไม่สร้างการ์ดใหม่ จึงลากต่อได้)
    private void UpdateSelectionVisuals()
    {
        foreach (FishCardUI c in collectionCards)
        {
            if (c != null) c.SetSelected(c.Fish == selected);
        }
        foreach (FishCardUI c in deckCards)
        {
            if (c != null) c.SetSelected(selected != null && c.SlotLayer == selected.layer);
        }
    }

    private void RefreshFilterUI()
    {
        if (layerButtonLabel != null)
        {
            int active = layerFilter.Count + rarityFilter.Count;
            layerButtonLabel.text = active == 0 ? "Filter" : $"Filter ({active})";
        }

        foreach (var chip in chips)
        {
            bool on = layerFilter.Contains(chip.layer);
            chip.bg.color = on ? chipOnColor : chipOffColor;
            chip.label.color = on ? Color.white : new Color(0.15f, 0.15f, 0.2f);
        }

        foreach (var chip in rarityChips)
        {
            bool on = rarityFilter.Contains(chip.rarity);
            chip.bg.color = on ? FishCardUI.RarityColor(chip.rarity) : chipOffColor;
            chip.label.color = on ? Color.white : new Color(0.15f, 0.15f, 0.2f);
        }
    }

    private void Clear(Transform container)
    {
        foreach (Transform child in container)
        {
            child.gameObject.SetActive(false);   // ให้ Layout ไม่นับทันที
            Destroy(child.gameObject);
        }
    }

    // ---------------------------------------------------------------- input

    private void OnSearchChanged(string text)
    {
        query = text ?? "";
        RefreshCollection();
    }

    private void ToggleLayer(int layer)
    {
        if (!layerFilter.Remove(layer)) layerFilter.Add(layer);
        RefreshCollection();
        RefreshFilterUI();
    }

    private void ToggleRarity(FishRarity rarity)
    {
        if (!rarityFilter.Remove(rarity)) rarityFilter.Add(rarity);
        RefreshCollection();
        RefreshFilterUI();
    }

    /// ปุ่ม "ล้างตัวกรอง" ในป๊อปอัป: ล้างทั้ง layer และความแรร์
    private void ClearLayers()
    {
        layerFilter.Clear();
        rarityFilter.Clear();
        RefreshCollection();
        RefreshFilterUI();
    }

    /// ปุ่ม Clear: ล้างทั้งคำค้นหาและตัวกรอง layer
    private void ClearAllFilters()
    {
        query = "";
        if (searchInput != null) searchInput.SetTextWithoutNotify("");
        layerFilter.Clear();
        rarityFilter.Clear();
        RefreshCollection();
        RefreshFilterUI();
    }

    /// แตะรูปปลาในคอลเลกชัน = เลือก/ยกเลิกเลือก (ไฮไลต์เท่านั้น)
    private void OnCollectionCardClicked(FishData fish)
    {
        selected = selected == fish ? null : fish;
        UpdateSelectionVisuals();
    }

    private void OnDragStarted(FishData fish)
    {
        selected = fish;
        UpdateSelectionVisuals();
    }

    /// แตะช่อง Deck 1 ครั้ง = ใส่ปลาที่เลือกไว้ลงช่องนี้
    private void OnSlotClicked(int layer)
    {
        if (selected == null) return;
        PutInLayer(selected, layer);
    }

    /// แตะช่อง Deck 2 ครั้ง = กรองคอลเลกชันให้เหลือ layer นี้
    private void OnSlotDoubleClicked(int layer)
    {
        query = "";
        if (searchInput != null) searchInput.SetTextWithoutNotify("");
        layerFilter.Clear();
        rarityFilter.Clear();
        layerFilter.Add(layer);
        RefreshCollection();
        RefreshFilterUI();
    }

    /// ใส่ปลาลงช่อง layer (จากการแตะช่อง, ลากมาวาง หรือปุ่มในป๊อปอัป)
    private void PutInLayer(FishData fish, int slotLayer)
    {
        PlayerProfile profile = SaveSystem.Profile;

        if (!profile.IsUnlocked(fish.id))
        {
            ShowToast($"ยังไม่ได้ปลดล็อก {fish.displayName}");
            return;
        }
        if (fish.layer != slotLayer)
        {
            ShowToast($"{fish.displayName} ใส่ได้เฉพาะ Layer {fish.layer}");
            return;
        }
        if (profile.deckFishIds.Contains(fish.id))
        {
            ShowToast($"{fish.displayName} อยู่ใน Deck แล้ว");
            return;
        }

        int slot = slotLayer - 1;
        if (slot < 0 || slot >= GameSession.MaxDeck) return;
        while (profile.deckFishIds.Count < GameSession.MaxDeck) profile.deckFishIds.Add("");

        profile.deckFishIds[slot] = fish.id;
        SaveSystem.Save();

        selected = null;
        RefreshAll();
        ShowToast($"ใส่ {fish.displayName} ใน Layer {slotLayer} แล้ว");
    }

    // ---------------------------------------------------------------- detail popup (แตะชื่อปลา)

    private void OpenDetail(FishData fish)
    {
        detailFish = fish;
        UpdateInfo();
    }

    private void CloseDetail()
    {
        detailFish = null;
        UpdateInfo();
    }

    private void OnPutDetailFish()
    {
        if (detailFish == null) return;
        FishData fish = detailFish;
        detailFish = null;
        PutInLayer(fish, fish.layer);
        UpdateInfo();
    }

    private void UpdateInfo()
    {
        PlayerProfile profile = SaveSystem.Profile;
        FishData f = detailFish;

        if (detailPopup != null) detailPopup.SetActive(f != null);
        if (detailIcon != null && f != null)
        {
            detailIcon.sprite = f.sprite;
            detailIcon.preserveAspect = true;
            detailIcon.color = profile.IsUnlocked(f.id) ? Color.white : new Color(0f, 0f, 0f, 0.85f);
        }

        if (f == null)
        {
            infoText.text = "แตะชื่อปลาเพื่อดูความสามารถ";
            toggleButton.interactable = false;
            toggleButtonLabel.text = "ใส่ใน Deck";
            return;
        }

        bool unlocked = profile.IsUnlocked(f.id);
        bool inDeck = profile.deckFishIds.Contains(f.id);

        string ability = f.ability != null
            ? $"<b>{f.ability.abilityName}</b>\n{f.ability.description}"
            : "ไม่มีความสามารถพิเศษ";

        string title = unlocked ? f.displayName : "ปลาที่ยังไม่ปลดล็อก";
        string body = unlocked ? ability : ProgressService.UnlockInfo(f);
        infoText.text = $"<size=120%><b>{title}</b></size>\n{f.rarity}  ·  Layer {f.layer}\n\n{body}";

        int slot = f.layer - 1;
        FishData current = slot >= 0 && slot < profile.deckFishIds.Count
            ? FishDatabase.Instance.Get(profile.deckFishIds[slot])
            : null;

        toggleButton.interactable = unlocked && !inDeck;
        if (inDeck)
        {
            toggleButtonLabel.text = $"อยู่ใน Deck แล้ว (Layer {f.layer})";
        }
        else if (!unlocked)
        {
            toggleButtonLabel.text = "ยังไม่ปลดล็อก";
        }
        else
        {
            string replaced = current != null ? $" แทน {current.displayName}" : "";
            toggleButtonLabel.text = $"ใส่ Layer {f.layer}{replaced}";
        }
    }

    // ---------------------------------------------------------------- reset deck (แตะ 2 ครั้งเพื่อยืนยัน)

    /// ปุ่ม "Reset Deck" เหนือคอลัมน์ Deck (หรือท้ายแถวค้นหา ถ้าเป็น layout เก่า)
    private void BuildResetButton()
    {
        RectTransform deckRt = deckContainer as RectTransform;
        bool aboveDeck = toolbarParent != null && deckRt != null && deckRt.parent == transform;

        Button b = NewButton(aboveDeck ? transform : searchInput.transform.parent, "ResetDeckButton", "Reset Deck", out resetLabel);
        resetLabel.enableAutoSizing = true;
        resetLabel.fontSizeMin = 16f;
        resetLabel.fontSizeMax = toolbarHeight * 0.36f;
        b.onClick.AddListener(OnResetDeckPressed);

        if (aboveDeck)
        {
            b.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            RectTransform rt = (RectTransform)b.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 0f);
            float width = deckRt.offsetMax.x - deckRt.offsetMin.x;
            rt.sizeDelta = new Vector2(width, toolbarHeight);
            rt.anchoredPosition = new Vector2(deckRt.offsetMax.x, deckRt.offsetMax.y + 10f);
        }
        else
        {
            LayoutElement le = b.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = toolbarHeight * 2.4f;
            le.minWidth = toolbarHeight * 2.4f;
        }
    }

    private void OnResetDeckPressed()
    {
        if (Time.unscaledTime > resetConfirmUntil)
        {
            resetConfirmUntil = Time.unscaledTime + ResetConfirmWindow;
            resetLabel.text = "แน่ใจ? แตะอีกครั้ง";
            return;
        }

        resetConfirmUntil = 0f;
        resetLabel.text = "Reset Deck";
        SaveSystem.ClearDeck();
        selected = null;
        RefreshAll();
        ShowToast("ล้าง Deck แล้ว — ใส่ปลาให้ครบ 7 layer ก่อนเล่น");
    }

    // ---------------------------------------------------------------- toast

    private void BuildToast()
    {
        GameObject go = NewUI("Toast", transform);
        go.AddComponent<LayoutElement>().ignoreLayout = true;
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(900f, 96f);
        rt.anchoredPosition = new Vector2(0f, 420f);

        Image bg = go.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.14f, 0.85f);
        bg.raycastTarget = false;

        toast = go.AddComponent<CanvasGroup>();
        toast.blocksRaycasts = false;
        toast.alpha = 0f;

        toastText = NewLabel(go.transform, "", 36f);
        toastText.color = Color.white;
        toastText.enableAutoSizing = true;
        toastText.fontSizeMin = 18f;
        toastText.fontSizeMax = 36f;
        Stretch((RectTransform)toastText.transform);
    }

    public void ShowToast(string message)
    {
        if (toast == null) return;
        toast.transform.SetAsLastSibling();
        toastText.text = message;
        if (toastRoutine != null) StopCoroutine(toastRoutine);
        toastRoutine = StartCoroutine(ToastRoutine());
    }

    private System.Collections.IEnumerator ToastRoutine()
    {
        toast.alpha = 1f;
        yield return new WaitForSeconds(1.6f);
        for (float t = 0f; t < 0.3f; t += Time.deltaTime)
        {
            toast.alpha = 1f - t / 0.3f;
            yield return null;
        }
        toast.alpha = 0f;
        toastRoutine = null;
    }

    // ---------------------------------------------------------------- toolbar: [search][Layer ▼]

    private void BuildToolbar()
    {
        GameObject row = NewUI("SearchRow", toolbarParent != null ? toolbarParent : transform);
        HorizontalLayoutGroup h = row.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 12f;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = true;
        if (toolbarParent != null)
        {
            Stretch((RectTransform)row.transform);
        }
        else
        {
            LayoutElement rowLe = row.AddComponent<LayoutElement>();
            rowLe.minHeight = toolbarHeight;
            rowLe.preferredHeight = toolbarHeight;

            // วางไว้เหนือรายการปลา (ลูกตรงของ panel ที่มี collectionContainer อยู่ข้างใน)
            Transform anchor = collectionContainer;
            while (anchor != null && anchor.parent != transform) anchor = anchor.parent;
            if (anchor != null) row.transform.SetSiblingIndex(anchor.GetSiblingIndex());
        }

        // ช่องค้นหา (ยืดเต็มที่เหลือ)
        GameObject searchGo = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
        searchGo.name = "SearchField";
        searchGo.transform.SetParent(row.transform, false);
        searchGo.AddComponent<LayoutElement>().flexibleWidth = 1f;
        StyleBackground(searchGo.GetComponent<Image>(), chipOffColor);

        searchInput = searchGo.GetComponent<TMP_InputField>();
        searchInput.lineType = TMP_InputField.LineType.SingleLine;
        searchInput.onFocusSelectAll = true;   // แตะแล้วพิมพ์ทับได้เลย
        foreach (TMP_Text t in searchGo.GetComponentsInChildren<TMP_Text>(true))
        {
            if (font != null) t.font = font;
            t.fontSize = toolbarHeight * 0.42f;
            t.alignment = TextAlignmentOptions.MidlineLeft;
        }
        if (searchInput.placeholder is TMP_Text ph)
        {
            ph.text = "ค้นหาชื่อปลา...";
            ph.color = new Color(0f, 0f, 0f, 0.4f);
        }
        if (searchInput.textViewport != null)
        {
            searchInput.textViewport.offsetMin = new Vector2(18f, 4f);
            searchInput.textViewport.offsetMax = new Vector2(-18f, -4f);
        }
        searchInput.onValueChanged.AddListener(OnSearchChanged);

        // ปุ่ม Layer ▼
        Button layerButton = NewButton(row.transform, "FilterButton", "Filter", out layerButtonLabel);
        LayoutElement le = layerButton.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = layerButtonWidth;
        le.minWidth = layerButtonWidth;
        layerButtonRect = (RectTransform)layerButton.transform;
        layerButtonLabel.alignment = TextAlignmentOptions.MidlineLeft;
        layerButtonLabel.margin = new Vector4(18f, 0f, 46f, 0f);
        layerButtonLabel.enableAutoSizing = true;
        layerButtonLabel.fontSizeMax = toolbarHeight * 0.4f;
        layerButtonLabel.fontSizeMin = 16f;

        GameObject arrow = NewUI("Arrow", layerButton.transform);
        Image arrowImg = arrow.AddComponent<Image>();
        arrowImg.sprite = TriangleSprite();
        arrowImg.color = new Color(0.15f, 0.15f, 0.2f);
        arrowImg.raycastTarget = false;
        RectTransform art = (RectTransform)arrow.transform;
        art.anchorMin = art.anchorMax = new Vector2(1f, 0.5f);
        art.pivot = new Vector2(1f, 0.5f);
        art.sizeDelta = new Vector2(22f, 14f);
        art.anchoredPosition = new Vector2(-18f, 0f);

        layerButton.onClick.AddListener(TogglePopup);

        // ปุ่ม Clear: ล้างคำค้นหา + ตัวกรอง layer
        Button clear = NewButton(row.transform, "ClearButton", "Clear", out TMP_Text clearLabel);
        LayoutElement cle = clear.gameObject.AddComponent<LayoutElement>();
        cle.preferredWidth = toolbarHeight * 1.7f;
        cle.minWidth = toolbarHeight * 1.7f;
        clearLabel.fontSize = toolbarHeight * 0.38f;
        clear.onClick.AddListener(ClearAllFilters);
    }

    // ---------------------------------------------------------------- layer popup (multi-select)

    private void BuildLayerPopup()
    {
        // root ครอบทั้ง panel และวาดทับทุกอย่าง (Canvas แยก) — แตะพื้นที่ว่าง = ปิด
        popupRoot = NewUI("LayerFilterPopup", transform);
        popupRoot.AddComponent<LayoutElement>().ignoreLayout = true;
        Stretch((RectTransform)popupRoot.transform);

        Image blocker = popupRoot.AddComponent<Image>();
        blocker.color = new Color(0f, 0f, 0f, 0.25f);
        popupRoot.AddComponent<Button>().onClick.AddListener(ClosePopup);

        GameObject panel = NewUI("Panel", popupRoot.transform);
        popupPanel = (RectTransform)panel.transform;
        popupPanel.anchorMin = popupPanel.anchorMax = new Vector2(0.5f, 0.5f);
        popupPanel.pivot = new Vector2(1f, 1f);
        StyleBackground(panel.AddComponent<Image>(), new Color(0.97f, 0.98f, 1f));

        VerticalLayoutGroup v = panel.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(18, 18, 14, 18);
        v.spacing = 12f;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        ContentSizeFitter fit = panel.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TMP_Text title = NewLabel(panel.transform, "Layer", 26f);
        title.alignment = TextAlignmentOptions.MidlineLeft;
        title.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;

        // ปุ่ม 1–7 (กดเพื่อเปิด/ปิดแต่ละ layer)
        const int columns = 4;
        const float cell = 72f, gap = 10f;
        GameObject grid = NewUI("Layers", panel.transform);
        GridLayoutGroup g = grid.AddComponent<GridLayoutGroup>();
        g.cellSize = new Vector2(cell, cell * 0.85f);
        g.spacing = new Vector2(gap, gap);
        g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        g.constraintCount = columns;
        int rows = Mathf.CeilToInt(GameSession.MaxDeck / (float)columns);
        LayoutElement gle = grid.AddComponent<LayoutElement>();
        gle.preferredWidth = columns * cell + (columns - 1) * gap;
        gle.preferredHeight = rows * cell * 0.85f + (rows - 1) * gap;

        chips.Clear();
        for (int layer = 1; layer <= GameSession.MaxDeck; layer++)
        {
            int l = layer;
            Button b = NewButton(grid.transform, $"Layer{l}", l.ToString(), out TMP_Text label);
            label.fontSize = 30f;
            b.onClick.AddListener(() => ToggleLayer(l));
            chips.Add((l, b.image, label));
        }

        // ความแรร์ (เลือกได้หลายอัน) — สีปุ่มตอนเลือก = สีการ์ดของความแรร์นั้น
        TMP_Text rarityTitle = NewLabel(panel.transform, "Rarity", 26f);
        rarityTitle.alignment = TextAlignmentOptions.MidlineLeft;
        rarityTitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;

        const int rarityColumns = 2;
        FishRarity[] rarities = (FishRarity[])System.Enum.GetValues(typeof(FishRarity));
        float gridWidth = columns * cell + (columns - 1) * gap;
        float rarityCellW = (gridWidth - (rarityColumns - 1) * gap) / rarityColumns;
        float rarityCellH = cell * 0.7f;
        GameObject rarityGrid = NewUI("Rarities", panel.transform);
        GridLayoutGroup rg = rarityGrid.AddComponent<GridLayoutGroup>();
        rg.cellSize = new Vector2(rarityCellW, rarityCellH);
        rg.spacing = new Vector2(gap, gap);
        rg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        rg.constraintCount = rarityColumns;
        int rarityRows = Mathf.CeilToInt(rarities.Length / (float)rarityColumns);
        LayoutElement rle = rarityGrid.AddComponent<LayoutElement>();
        rle.preferredWidth = gridWidth;
        rle.preferredHeight = rarityRows * rarityCellH + (rarityRows - 1) * gap;

        rarityChips.Clear();
        foreach (FishRarity r in rarities)
        {
            FishRarity rr = r;
            Button b = NewButton(rarityGrid.transform, $"Rarity{rr}", rr.ToString(), out TMP_Text label);
            label.fontSize = 24f;
            b.onClick.AddListener(() => ToggleRarity(rr));
            rarityChips.Add((rr, b.image, label));
        }

        // แถวล่าง: ล้าง / เสร็จ
        GameObject actions = NewUI("Actions", panel.transform);
        HorizontalLayoutGroup ah = actions.AddComponent<HorizontalLayoutGroup>();
        ah.spacing = gap;
        ah.childControlWidth = true;
        ah.childControlHeight = true;
        ah.childForceExpandWidth = true;
        actions.AddComponent<LayoutElement>().preferredHeight = 56f;

        NewButton(actions.transform, "Clear", "ล้างตัวกรอง", out TMP_Text clearLabel).onClick.AddListener(ClearLayers);
        clearLabel.fontSize = 22f;
        Button done = NewButton(actions.transform, "Done", "เสร็จ", out TMP_Text doneLabel);
        done.image.color = chipOnColor;
        doneLabel.color = Color.white;
        doneLabel.fontSize = 22f;
        done.onClick.AddListener(ClosePopup);

        popupRoot.SetActive(false);
    }

    private void TogglePopup()
    {
        if (popupRoot.activeSelf) ClosePopup();
        else OpenPopup();
    }

    private void OpenPopup()
    {
        popupRoot.SetActive(true);
        popupRoot.transform.SetAsLastSibling();

        // วาดทับรายการปลา (Canvas ซ้อน + รับการแตะได้)
        Canvas c = popupRoot.GetComponent<Canvas>();
        if (c == null)
        {
            c = popupRoot.AddComponent<Canvas>();
            popupRoot.AddComponent<GraphicRaycaster>();
        }
        c.overrideSorting = true;
        c.sortingOrder = 100;

        // วางมุมขวาบนของ popup ไว้ใต้ปุ่ม Layer
        Canvas.ForceUpdateCanvases();
        Vector3[] corners = new Vector3[4];
        layerButtonRect.GetWorldCorners(corners);   // 0=BL 1=TL 2=TR 3=BR
        Vector3 local = popupPanel.parent.InverseTransformPoint(corners[3]);
        popupPanel.localPosition = local + new Vector3(0f, -8f, 0f);

        RefreshFilterUI();
    }

    private void ClosePopup()
    {
        if (popupRoot != null) popupRoot.SetActive(false);
    }

    // ---------------------------------------------------------------- tiny UI helpers

    private GameObject NewUI(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        return go;
    }

    private TMP_Text NewLabel(Transform parent, string text, float size)
    {
        GameObject go = NewUI("Label", parent);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = new Color(0.15f, 0.15f, 0.2f);
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        return t;
    }

    private Button NewButton(Transform parent, string name, string text, out TMP_Text label)
    {
        GameObject go = NewUI(name, parent);
        Image img = go.AddComponent<Image>();
        StyleBackground(img, chipOffColor);
        Button b = go.AddComponent<Button>();
        b.targetGraphic = img;

        label = NewLabel(go.transform, text, toolbarHeight * 0.4f);
        Stretch((RectTransform)label.transform);
        return b;
    }

    /// ใช้สไปรต์พื้นหลังเดียวกับปุ่มเดิมในฉาก ให้หน้าตาเข้าชุดกัน
    private void StyleBackground(Image img, Color color)
    {
        if (img == null) return;
        Image reference = toggleButton != null ? toggleButton.image : null;
        if (reference != null && reference.sprite != null)
        {
            img.sprite = reference.sprite;
            img.type = reference.type;
            img.pixelsPerUnitMultiplier = reference.pixelsPerUnitMultiplier;
        }
        img.color = color;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static Sprite triangle;

    private static Sprite TriangleSprite()
    {
        if (triangle != null) return triangle;

        const int w = 32, h = 20;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color32[] px = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            // จุดยอดชี้ลง: แถวบนกว้างสุด
            float half = (w * 0.5f) * (y + 0.5f) / h;
            for (int x = 0; x < w; x++)
            {
                float dx = Mathf.Abs(x + 0.5f - w * 0.5f);
                float a = Mathf.Clamp01(half - dx + 0.5f);
                px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        triangle = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        return triangle;
    }
}
