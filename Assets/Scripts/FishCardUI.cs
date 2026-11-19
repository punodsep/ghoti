using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// การ์ดปลา 1 ใบ (ใช้ทั้งในคอลเลกชันและช่อง Deck)
///   แตะรูป      → onSelect        แตะชื่อ → onInfo (ดูความสามารถ)
///   แตะ 2 ครั้ง → onDoubleClick    ลาก (แนวนอน) → ลากปลาไปวางในช่อง Deck  (ลากแนวตั้ง = เลื่อนรายการ)
///   ช่อง Deck รับการวางด้วย onDropFish
public class FishCardUI : MonoBehaviour,
    IPointerClickHandler, IInitializePotentialDragHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text orderText;       // เลข layer (วงกลมมุมซ้ายบน)
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private GameObject inDeckBadge;
    [SerializeField] private GameObject selectedFrame;

    [Header("สีการ์ดบอกความแรร์ (ว่างได้)")]
    [SerializeField] private Image frame;
    [SerializeField] private TMP_Text rarityText;

    public FishData Fish { get; private set; }
    public int SlotLayer { get; private set; }          // > 0 = เป็นช่อง Deck ของ layer นี้

    private Action<FishData> onSelect;
    private Action<FishData> onInfo;
    private Action<int> onSlotClick;
    private Action<int> onSlotDoubleClick;
    private Action<FishData> onDragStarted;
    private Action<FishData, int> onDropFish;
    private bool draggable;

    private static FishCardUI dragging;
    private ScrollRect parentScroll;
    private bool forwardingToScroll;
    private RectTransform ghost;
    private Canvas rootCanvas;

    public static Color RarityColor(FishRarity rarity)
    {
        switch (rarity)
        {
            case FishRarity.Rare: return new Color32(0x4F, 0x8D, 0xF0, 0xFF);       // ฟ้า
            case FishRarity.Epic: return new Color32(0x9B, 0x5D, 0xE5, 0xFF);       // ม่วง
            case FishRarity.Legendary: return new Color32(0xF2, 0xB8, 0x2E, 0xFF);  // ทอง
            default: return new Color32(0xB0, 0xB0, 0xB0, 0xFF);                    // เทา
        }
    }

    public void Setup(FishData fish, bool unlocked, bool inDeck, bool selected,
                      Action<FishData> onClick, int order = 0)
    {
        Fish = fish;
        onSelect = onClick;
        onInfo = null;
        onSlotClick = null;
        onSlotDoubleClick = null;
        onDragStarted = null;
        onDropFish = null;
        draggable = false;
        SlotLayer = 0;

        icon.sprite = fish.sprite;
        icon.preserveAspect = true;
        icon.color = unlocked ? Color.white : new Color(0f, 0f, 0f, 0.85f);

        nameText.text = unlocked ? fish.displayName : "???";

        lockedOverlay.SetActive(!unlocked);
        inDeckBadge.SetActive(inDeck);
        SetSelected(selected);

        orderText.gameObject.SetActive(order > 0);
        if (order > 0) orderText.text = order.ToString();

        if (frame != null)
        {
            frame.color = unlocked ? RarityColor(fish.rarity) : new Color32(0x6A, 0x6A, 0x6A, 0xFF);
        }
        if (rarityText != null)
        {
            rarityText.gameObject.SetActive(unlocked);
            rarityText.text = fish.rarity.ToString();
        }

        // ปุ่มใช้แค่เอฟเฟกต์กดเท่านั้น การแตะจริงจัดการใน OnPointerClick
        if (button != null) button.onClick.RemoveAllListeners();
    }

    public FishCardUI WithInfo(Action<FishData> info)
    {
        onInfo = info;
        return this;
    }

    public FishCardUI AsDraggable(Action<FishData> dragStarted)
    {
        draggable = true;
        onDragStarted = dragStarted;
        return this;
    }

    /// ทำให้การ์ดเป็นช่อง Deck ของ layer นี้: แตะ = click(layer), แตะ 2 ครั้ง = doubleClick(layer), รับการลากมาวาง
    public FishCardUI AsSlot(int layer, Action<FishData, int> dropFish, Action<int> click, Action<int> doubleClick)
    {
        SlotLayer = layer;
        onDropFish = dropFish;
        onSlotClick = click;
        onSlotDoubleClick = doubleClick;
        onSelect = null;
        return this;
    }

    /// ช่อง Deck ที่ยังว่าง (ไม่มีปลา)
    public void SetupEmptySlot(int layer)
    {
        Fish = null;
        onSelect = null;
        onInfo = null;
        onDragStarted = null;
        draggable = false;

        icon.sprite = null;
        icon.color = new Color(1f, 1f, 1f, 0f);
        nameText.text = "ว่าง";
        lockedOverlay.SetActive(false);
        inDeckBadge.SetActive(false);
        SetSelected(false);

        orderText.gameObject.SetActive(true);
        orderText.text = layer.ToString();

        if (frame != null) frame.color = new Color32(0xE4, 0xE4, 0xE4, 0xFF);
        if (rarityText != null) rarityText.gameObject.SetActive(false);
        if (button != null) button.onClick.RemoveAllListeners();
    }

    public void SetSelected(bool selected)
    {
        if (selectedFrame != null) selectedFrame.SetActive(selected);
    }

    // ---------------------------------------------------------------- tap

    public void OnPointerClick(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Left) return;
        if (Fish == null && SlotLayer <= 0) return;

        if (SlotLayer > 0 && e.clickCount >= 2 && onSlotDoubleClick != null)
        {
            onSlotDoubleClick(SlotLayer);
            return;
        }

        if (Fish != null && onInfo != null && nameText != null &&
            RectTransformUtility.RectangleContainsScreenPoint(nameText.rectTransform, e.position, e.pressEventCamera))
        {
            onInfo(Fish);
            return;
        }

        if (SlotLayer > 0)
        {
            onSlotClick?.Invoke(SlotLayer);
            return;
        }

        onSelect?.Invoke(Fish);
    }

    // ---------------------------------------------------------------- drag (ลากแนวตั้ง = ส่งต่อให้ ScrollRect)

    public void OnInitializePotentialDrag(PointerEventData e)
    {
        parentScroll = GetComponentInParent<ScrollRect>();
        if (parentScroll != null) parentScroll.OnInitializePotentialDrag(e);
    }

    public void OnBeginDrag(PointerEventData e)
    {
        Vector2 d = e.position - e.pressPosition;
        forwardingToScroll = !draggable || Mathf.Abs(d.x) < Mathf.Abs(d.y) * 0.8f;

        if (forwardingToScroll)
        {
            if (parentScroll != null) parentScroll.OnBeginDrag(e);
            return;
        }

        dragging = this;
        CreateGhost();
        MoveGhost(e);
        onDragStarted?.Invoke(Fish);
    }

    public void OnDrag(PointerEventData e)
    {
        if (forwardingToScroll)
        {
            if (parentScroll != null) parentScroll.OnDrag(e);
            return;
        }
        MoveGhost(e);
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (forwardingToScroll)
        {
            if (parentScroll != null) parentScroll.OnEndDrag(e);
            forwardingToScroll = false;
            return;
        }
        EndDrag();
    }

    public void OnDrop(PointerEventData e)
    {
        if (SlotLayer <= 0 || dragging == null || dragging == this || dragging.Fish == null) return;
        FishData fish = dragging.Fish;
        dragging.EndDrag();
        onDropFish?.Invoke(fish, SlotLayer);
    }

    private void CreateGhost()
    {
        Canvas c = GetComponentInParent<Canvas>();
        rootCanvas = c != null ? c.rootCanvas : null;
        if (rootCanvas == null) return;

        GameObject go = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        go.layer = gameObject.layer;
        go.transform.SetParent(rootCanvas.transform, false);
        go.transform.SetAsLastSibling();

        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.alpha = 0.85f;

        Image img = go.GetComponent<Image>();
        img.sprite = Fish.sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;

        ghost = (RectTransform)go.transform;
        ghost.sizeDelta = icon.rectTransform.rect.size * 1.15f;
    }

    private void MoveGhost(PointerEventData e)
    {
        if (ghost == null || rootCanvas == null) return;
        Camera cam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)rootCanvas.transform, e.position, cam, out Vector2 local))
        {
            ghost.localPosition = local;
        }
    }

    private void EndDrag()
    {
        if (ghost != null) Destroy(ghost.gameObject);
        ghost = null;
        if (dragging == this) dragging = null;
    }

    private void OnDisable()
    {
        // การ์ดถูกสร้างใหม่ระหว่างลาก (เช่น วางสำเร็จแล้วรีเฟรช) → เก็บกวาดผีที่ลากอยู่
        EndDrag();
    }
}
