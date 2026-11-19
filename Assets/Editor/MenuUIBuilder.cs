using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// สร้าง UI เมนูตาม wireframe (หน้าแรก / Deck / Gacha + Leaderboard, Shop แบบ "TO BE CONTINUE")
/// Tools/Ghoti/Build Menu UI — ลบของเดิมใต้ SafePanel แล้วสร้างใหม่ทั้งหมด (Undo ได้) แล้วเซฟฉาก
/// ขนาดอ้างอิง 1179 x 2556 (iPhone 15/16)
public static class MenuUIBuilder
{
    private const string ScenePath = "Assets/Scenes/Menu.unity";
    private const string CardPrefabPath = "Assets/Prefabs/FishCardV2.prefab";
    private const string CirclePath = "Assets/Art/UI/Circle.png";

    private static readonly Color White = Color.white;
    private static readonly Color Grey = new Color32(0xD6, 0xD6, 0xD6, 0xFF);
    private static readonly Color PanelGrey = new Color32(0xDA, 0xDA, 0xDA, 0xFF);
    private static readonly Color DarkGrey = new Color32(0xB3, 0xB3, 0xB3, 0xFF);
    private static readonly Color Pink = new Color32(0xFF, 0x9A, 0x9A, 0xFF);
    private static readonly Color Ink = new Color32(0x1E, 0x1E, 0x24, 0xFF);
    private static readonly Color Dim = new Color(0f, 0f, 0f, 0.55f);

    private const float NavY = 80f;           // ขอบล่างของแถบล่าง
    private const float ContentBottom = 380f;  // พื้นที่เนื้อหาเหนือแถบล่าง
    private const float ContentTop = 270f;     // พื้นที่เนื้อหาใต้แถบบน (+ แถวค้นหา)

    private static TMP_FontAsset font;
    private static Sprite circle;

    [MenuItem("Tools/Ghoti/Build Menu UI")]
    private static void Build()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("[MenuUIBuilder] ออกจาก Play mode ก่อน");
            return;
        }

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            scene = EditorSceneManager.OpenScene(ScenePath);
        }

        Canvas canvas = null;
        foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
        {
            if (c.isRootCanvas) { canvas = c; break; }
        }
        if (canvas == null)
        {
            Debug.LogError("[MenuUIBuilder] ไม่พบ Canvas ในฉาก Menu");
            return;
        }

        Transform safe = canvas.transform.Find("SafePanel");
        if (safe == null)
        {
            Debug.LogError("[MenuUIBuilder] ไม่พบ Canvas/SafePanel");
            return;
        }

        TMP_Text anyText = canvas.GetComponentInChildren<TMP_Text>(true);
        font = anyText != null ? anyText.font : TMP_Settings.defaultFontAsset;
        circle = AssetDatabase.LoadAssetAtPath<Sprite>(CirclePath);

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Build Menu UI");

        for (int i = safe.childCount - 1; i >= 0; i--)
        {
            Undo.DestroyObjectImmediate(safe.GetChild(i).gameObject);
        }
        Transform oldBg = canvas.transform.Find("Background");
        if (oldBg != null) Undo.DestroyObjectImmediate(oldBg.gameObject);

        // พื้นหลังขาวเต็มจอ (รวมรอยบาก)
        RectTransform bg = Node("Background", canvas.transform);
        Stretch(bg);
        bg.SetAsFirstSibling();
        Img(bg, White);
        Undo.RegisterCreatedObjectUndo(bg.gameObject, "Build Menu UI");

        FishCardUI cardPrefab = BuildCardPrefab();

        MenuController mc = Object.FindAnyObjectByType<MenuController>(FindObjectsInactive.Include);
        if (mc == null)
        {
            mc = new GameObject("MenuController").AddComponent<MenuController>();
            Undo.RegisterCreatedObjectUndo(mc.gameObject, "Build Menu UI");
        }

        List<Button> settingsButtons = new List<Button>();
        List<Button> closeButtons = new List<Button>();

        GameObject title = BuildTitle(safe, settingsButtons, out Button start, out Button howTo);
        GameObject deck = BuildDeck(safe, cardPrefab, settingsButtons);
        GameObject gacha = BuildGacha(safe, settingsButtons, out TMP_Text coins);
        GameObject leaderboard = BuildComingSoon(safe, "LeaderboardScreen", "LEADERBOARD", settingsButtons);
        GameObject shop = BuildComingSoon(safe, "ShopScreen", "SHOP", settingsButtons);
        GameObject nav = BuildNav(safe, out Button nGacha, out Button nDeck, out Button nGo, out Button nBoard, out Button nShop);

        GameObject howToPopup = BuildPopup(safe, "HowToPlayPopup", "How to play",
            "• ลากนิ้วซ้าย-ขวาเพื่อเล็ง ปล่อยนิ้วเพื่อทิ้งปลา\n" +
            "• ปลา layer เดียวกันชนกัน = รวมเป็น layer ถัดไป\n" +
            "• ปลา layer 7 สองตัวรวมกัน = Top Merge คะแนน x2\n" +
            "• ปลาค้างเหนือเส้นแดงนาน 2 วินาที = จบเกม\n" +
            "• จัด Deck 7 ช่อง (layer 1–7) ปลาแต่ละตัวมีความสามารถต่างกัน",
            closeButtons);
        GameObject settingsPopup = BuildPopup(safe, "SettingsPopup", "Settings",
            "เร็วๆ นี้: เสียง, เพลง, การสั่น, ภาษา", closeButtons);

        Wire(mc, "titleScreen", title);
        Wire(mc, "deckScreen", deck);
        Wire(mc, "gachaScreen", gacha);
        Wire(mc, "leaderboardScreen", leaderboard);
        Wire(mc, "shopScreen", shop);
        Wire(mc, "startButton", start);
        Wire(mc, "howToPlayButton", howTo);
        Wire(mc, "bottomNav", nav);
        Wire(mc, "navGacha", nGacha);
        Wire(mc, "navDeck", nDeck);
        Wire(mc, "navGo", nGo);
        Wire(mc, "navLeaderboard", nBoard);
        Wire(mc, "navShop", nShop);
        Wire(mc, "howToPlayPopup", howToPopup);
        Wire(mc, "settingsPopup", settingsPopup);
        WireArray(mc, "settingsButtons", settingsButtons.ToArray());
        WireArray(mc, "popupCloseButtons", closeButtons.ToArray());
        Wire(mc, "coinsText", coins);
        Wire(mc, "bestScoreText", null);

        for (int i = 0; i < safe.childCount; i++)
        {
            Undo.RegisterCreatedObjectUndo(safe.GetChild(i).gameObject, "Build Menu UI");
        }

        // เริ่มที่หน้าแรก
        deck.SetActive(false);
        gacha.SetActive(false);
        leaderboard.SetActive(false);
        shop.SetActive(false);
        nav.SetActive(false);
        howToPopup.SetActive(false);
        settingsPopup.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[MenuUIBuilder] สร้าง UI เมนูใหม่เรียบร้อย (Undo ได้ด้วย Ctrl+Z)");
    }

    // ================================================================ screens

    private static GameObject BuildTitle(Transform safe, List<Button> settings, out Button start, out Button howTo)
    {
        RectTransform root = Screen("TitleScreen", safe);

        settings.Add(SettingsButton(root));

        RectTransform logo = Node("Logo", root);
        Place(logo, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -200f), new Vector2(1000f, 420f));
        Img(logo, Grey);
        Label(logo, "LOGO", 120f);

        howTo = Btn(root, "HowToPlayButton", "How to play", Grey, 44f);
        Place((RectTransform)howTo.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 520f), new Vector2(540f, 90f));

        start = Btn(root, "StartButton", "START", Grey, 110f);
        Place((RectTransform)start.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 280f), new Vector2(1000f, 200f));

        return root.gameObject;
    }

    private static GameObject BuildDeck(Transform safe, FishCardUI cardPrefab, List<Button> settings)
    {
        RectTransform root = Screen("DeckScreen", safe);
        TopBar(root, settings, false, out _);

        // แถว [ค้นหา][Layer] — DeckBuilderUI สร้างของข้างในตอนรัน
        RectTransform toolbar = Node("SearchToolbar", root);
        toolbar.anchorMin = new Vector2(0f, 1f);
        toolbar.anchorMax = new Vector2(1f, 1f);
        toolbar.pivot = new Vector2(0.5f, 1f);
        toolbar.offsetMin = new Vector2(40f, -ContentTop + 10f);
        toolbar.offsetMax = new Vector2(-280f, -165f);

        // คอลเลกชัน (ซ้าย)
        RectTransform panel = Node("CollectionPanel", root);
        panel.anchorMin = Vector2.zero;
        panel.anchorMax = Vector2.one;
        panel.offsetMin = new Vector2(0f, ContentBottom);
        panel.offsetMax = new Vector2(-270f, -ContentTop - 10f);
        Img(panel, PanelGrey);

        TMP_Text header = Label(panel, "COLLECTION", 54f);
        header.alignment = TextAlignmentOptions.MidlineLeft;
        RectTransform hrt = (RectTransform)header.transform;
        hrt.anchorMin = new Vector2(0f, 1f);
        hrt.anchorMax = new Vector2(1f, 1f);
        hrt.pivot = new Vector2(0.5f, 1f);
        hrt.offsetMin = new Vector2(50f, -110f);
        hrt.offsetMax = new Vector2(-20f, -20f);

        RectTransform content = ScrollArea(panel, "ScrollView", new Vector2(20f, 20f), new Vector2(-20f, -115f));
        GridLayoutGroup grid = content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(250f, 300f);
        grid.spacing = new Vector2(26f, 30f);
        grid.padding = new RectOffset(20, 20, 10, 30);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.childAlignment = TextAnchor.UpperCenter;
        ContentSizeFitter fit = content.gameObject.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Deck 7 ช่อง (ขวา) — layer 1 บนสุด → 7 ล่างสุด
        RectTransform deckCol = Node("DeckColumn", root);
        deckCol.anchorMin = new Vector2(1f, 0f);
        deckCol.anchorMax = new Vector2(1f, 1f);
        deckCol.pivot = new Vector2(1f, 0.5f);
        deckCol.offsetMin = new Vector2(-250f, ContentBottom);
        deckCol.offsetMax = new Vector2(-30f, -ContentTop - 10f);
        VerticalLayoutGroup v = deckCol.gameObject.AddComponent<VerticalLayoutGroup>();
        v.spacing = 14f;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = true;

        // ป๊อปอัปรายละเอียด
        RectTransform popup = Node("FishDetailPopup", root);
        Stretch(popup);
        Img(popup, Dim);

        RectTransform box = Node("Panel", popup);
        Place(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(920f, 1150f));
        Img(box, White);

        RectTransform iconBg = Node("IconFrame", box);
        Place(iconBg, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(440f, 440f));
        Img(iconBg, Grey);
        RectTransform iconRt = Node("Icon", iconBg);
        Stretch(iconRt, 20f);
        Image icon = Img(iconRt, White);
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        TMP_Text info = Label(box, "", 40f);
        info.alignment = TextAlignmentOptions.TopLeft;
        info.textWrappingMode = TextWrappingModes.Normal;
        RectTransform irt = (RectTransform)info.transform;
        irt.anchorMin = new Vector2(0f, 1f);
        irt.anchorMax = new Vector2(1f, 1f);
        irt.pivot = new Vector2(0.5f, 1f);
        irt.offsetMin = new Vector2(60f, -920f);
        irt.offsetMax = new Vector2(-60f, -540f);

        Button put = Btn(box, "PutInDeckButton", "ใส่ใน Deck", Pink, 42f);
        Place((RectTransform)put.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(780f, 130f));
        TMP_Text putLabel = put.GetComponentInChildren<TMP_Text>();
        putLabel.enableAutoSizing = true;
        putLabel.fontSizeMin = 24f;
        putLabel.fontSizeMax = 42f;

        Button close = Btn(box, "CloseButton", "X", DarkGrey, 44f, circle);
        Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(96f, 96f), new Vector2(1f, 1f));

        popup.gameObject.SetActive(false);

        DeckBuilderUI ui = root.gameObject.AddComponent<DeckBuilderUI>();
        Wire(ui, "cardPrefab", cardPrefab);
        Wire(ui, "deckContainer", deckCol);
        Wire(ui, "collectionContainer", content);
        Wire(ui, "deckCountText", null);
        Wire(ui, "infoText", info);
        Wire(ui, "toggleButton", put);
        Wire(ui, "toggleButtonLabel", putLabel);
        Wire(ui, "detailPopup", popup.gameObject);
        Wire(ui, "detailIcon", icon);
        Wire(ui, "detailCloseButton", close);
        Wire(ui, "toolbarParent", toolbar);
        WireFloat(ui, "toolbarHeight", 95f);
        WireFloat(ui, "layerButtonWidth", 300f);

        return root.gameObject;
    }

    private static GameObject BuildGacha(Transform safe, List<Button> settings, out TMP_Text coins)
    {
        RectTransform root = Screen("GachaScreen", safe);
        TopBar(root, settings, true, out coins);

        RectTransform box = Node("GachaBox", root);
        Place(box, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -300f), new Vector2(720f, 840f));
        Img(box, Grey);
        TMP_Text placeholder = Label(box, "GACHA", 110f);
        placeholder.color = new Color(Ink.r, Ink.g, Ink.b, 0.25f);

        RectTransform resultRt = Node("ResultImage", box);
        Place(resultRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(540f, 540f));
        Image result = Img(resultRt, White);
        result.preserveAspect = true;
        result.raycastTarget = false;

        RectTransform gridRt = Node("MultiResultGrid", box);
        Stretch(gridRt, 30f);
        GridLayoutGroup grid = gridRt.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(124f, 124f);
        grid.spacing = new Vector2(14f, 14f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;
        grid.childAlignment = TextAnchor.MiddleCenter;

        TMP_Text resultText = Label(root, "", 40f);
        Place((RectTransform)resultText.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -1160f), new Vector2(1000f, 140f));

        TMP_Text pity = Label(root, "", 30f);
        pity.color = new Color(Ink.r, Ink.g, Ink.b, 0.6f);
        Place((RectTransform)pity.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 640f), new Vector2(1000f, 60f), new Vector2(0.5f, 0f));

        Button one = Btn(root, "Pull1Button", "1 PULL", Grey, 50f);
        Place((RectTransform)one.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-245f, 470f), new Vector2(460f, 150f), new Vector2(0.5f, 0f));
        Button ten = Btn(root, "Pull10Button", "10 PULLS", Grey, 50f);
        Place((RectTransform)ten.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(245f, 470f), new Vector2(460f, 150f), new Vector2(0.5f, 0f));

        GachaUI ui = root.gameObject.AddComponent<GachaUI>();
        Wire(ui, "pullButton", one);
        Wire(ui, "pullButtonLabel", one.GetComponentInChildren<TMP_Text>());
        Wire(ui, "pityText", pity);
        Wire(ui, "resultImage", result);
        Wire(ui, "resultText", resultText);
        Wire(ui, "pullTenButton", ten);
        Wire(ui, "pullTenButtonLabel", ten.GetComponentInChildren<TMP_Text>());
        Wire(ui, "multiResultGrid", gridRt);

        return root.gameObject;
    }

    private static GameObject BuildComingSoon(Transform safe, string name, string title, List<Button> settings)
    {
        RectTransform root = Screen(name, safe);
        settings.Add(SettingsButton(root));

        TMP_Text t = Label(root, title, 84f);
        Place((RectTransform)t.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(1000f, 130f));

        RectTransform box = Node("Box", root);
        box.anchorMin = Vector2.zero;
        box.anchorMax = Vector2.one;
        box.offsetMin = new Vector2(70f, ContentBottom);
        box.offsetMax = new Vector2(-70f, -300f);
        Img(box, Grey);
        Label(box, "TO BE CONTINUE", 76f);

        return root.gameObject;
    }

    // ================================================================ shared parts

    private static void TopBar(RectTransform screen, List<Button> settings, bool showMoney, out TMP_Text coins)
    {
        RectTransform logo = Node("Logo", screen);
        Place(logo, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -50f), new Vector2(330f, 90f), new Vector2(0f, 1f));
        Img(logo, Grey);
        Label(logo, "LOGO", 50f);

        settings.Add(SettingsButton(screen));

        coins = null;
        if (!showMoney) return;

        RectTransform money = Node("Money", screen);
        Place(money, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-170f, -50f), new Vector2(330f, 90f), new Vector2(1f, 1f));
        Img(money, Grey);
        coins = Label(money, "0", 42f);
        coins.enableAutoSizing = true;
        coins.fontSizeMin = 20f;
        coins.fontSizeMax = 42f;
    }

    private static Button SettingsButton(RectTransform screen)
    {
        Button b = Btn(screen, "SettingsButton", "SETTING", Grey, 17f, circle);
        Place((RectTransform)b.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-50f, -50f), new Vector2(92f, 92f), new Vector2(1f, 1f));
        return b;
    }

    private static GameObject BuildNav(Transform safe, out Button gacha, out Button deck, out Button go, out Button board, out Button shop)
    {
        RectTransform nav = Node("BottomNav", safe);
        Place(nav, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, NavY), new Vector2(1060f, 280f), new Vector2(0.5f, 0f));

        Vector2 size = new Vector2(190f, 170f);
        gacha = NavButton(nav, "NavGacha", "GACHA", -431f, size);
        deck = NavButton(nav, "NavDeck", "DECK\nBUILD", -233f, size);
        board = NavButton(nav, "NavLeaderboard", "LEADER\nBOARD", 233f, size);
        shop = NavButton(nav, "NavShop", "SHOP", 431f, size);

        go = Btn(nav, "NavGo", "GO", DarkGrey, 120f, circle);
        Place((RectTransform)go.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(270f, 270f));

        return nav.gameObject;
    }

    private static Button NavButton(RectTransform nav, string name, string text, float x, Vector2 size)
    {
        Button b = Btn(nav, name, text, Grey, 36f);
        Place((RectTransform)b.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, 0f), size);
        TMP_Text t = b.GetComponentInChildren<TMP_Text>();
        t.lineSpacing = -20f;
        return b;
    }

    private static GameObject BuildPopup(Transform safe, string name, string title, string body, List<Button> closeButtons)
    {
        RectTransform root = Node(name, safe);
        Stretch(root);
        Img(root, Dim);

        RectTransform box = Node("Panel", root);
        Place(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(960f, 1100f));
        Img(box, White);

        TMP_Text t = Label(box, title, 70f);
        Place((RectTransform)t.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(860f, 110f));

        TMP_Text b = Label(box, body, 40f);
        b.alignment = TextAlignmentOptions.TopLeft;
        b.textWrappingMode = TextWrappingModes.Normal;
        RectTransform brt = (RectTransform)b.transform;
        brt.anchorMin = Vector2.zero;
        brt.anchorMax = Vector2.one;
        brt.offsetMin = new Vector2(60f, 220f);
        brt.offsetMax = new Vector2(-60f, -190f);

        Button close = Btn(box, "CloseButton", "ปิด", Grey, 46f);
        Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(420f, 120f), new Vector2(0.5f, 0f));
        closeButtons.Add(close);

        return root.gameObject;
    }

    private static RectTransform ScrollArea(RectTransform parent, string name, Vector2 offsetMin, Vector2 offsetMax)
    {
        RectTransform sv = Node(name, parent);
        sv.anchorMin = Vector2.zero;
        sv.anchorMax = Vector2.one;
        sv.offsetMin = offsetMin;
        sv.offsetMax = offsetMax;
        ScrollRect scroll = sv.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.scrollSensitivity = 30f;

        RectTransform viewport = Node("Viewport", sv);
        Stretch(viewport);
        viewport.gameObject.AddComponent<RectMask2D>();
        Img(viewport, new Color(1f, 1f, 1f, 0f));   // รับการลากในช่องว่าง

        RectTransform content = Node("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;

        scroll.viewport = viewport;
        scroll.content = content;
        return content;
    }

    // ================================================================ fish card prefab

    private static FishCardUI BuildCardPrefab()
    {
        RectTransform root = Node("FishCardV2", null);
        root.sizeDelta = new Vector2(250f, 300f);
        Image frame = Img(root, Grey);
        Button button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = frame;
        LayoutElement le = root.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = 250f;
        le.preferredHeight = 300f;

        RectTransform sel = Node("SelectedFrame", root);
        Stretch(sel, -10f);
        Img(sel, new Color32(0xFF, 0xD2, 0x3F, 0xFF)).raycastTarget = false;

        RectTransform inner = Node("Inner", root);
        inner.anchorMin = Vector2.zero;
        inner.anchorMax = Vector2.one;
        inner.offsetMin = new Vector2(16f, 62f);
        inner.offsetMax = new Vector2(-16f, -40f);
        Img(inner, new Color32(0xEE, 0xEE, 0xEE, 0xFF)).raycastTarget = false;

        RectTransform iconRt = Node("Icon", inner);
        Stretch(iconRt, 8f);
        Image icon = Img(iconRt, White);
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        TMP_Text locked = Label(inner, "?", 110f);
        locked.color = White;
        locked.name = "LockedOverlay";

        RectTransform nameBar = Node("NameBar", root);
        nameBar.anchorMin = new Vector2(0f, 0f);
        nameBar.anchorMax = new Vector2(1f, 0f);
        nameBar.pivot = new Vector2(0.5f, 0f);
        nameBar.offsetMin = new Vector2(16f, 12f);
        nameBar.offsetMax = new Vector2(-16f, 54f);
        Img(nameBar, White).raycastTarget = false;
        TMP_Text nameText = Label(nameBar, "NAME", 26f);
        nameText.enableAutoSizing = true;
        nameText.fontSizeMin = 14f;
        nameText.fontSizeMax = 26f;

        RectTransform inDeck = Node("InDeckBadge", root);
        inDeck.anchorMin = new Vector2(0f, 0f);
        inDeck.anchorMax = new Vector2(1f, 0f);
        inDeck.pivot = new Vector2(0.5f, 0f);
        inDeck.offsetMin = new Vector2(16f, 62f);
        inDeck.offsetMax = new Vector2(-16f, 96f);
        Img(inDeck, Pink).raycastTarget = false;
        Label(inDeck, "IN DECK", 22f);

        TMP_Text rarity = Label(root, "Common", 22f);
        rarity.color = White;
        rarity.fontStyle = FontStyles.Bold;
        rarity.alignment = TextAlignmentOptions.MidlineRight;
        RectTransform rrt = (RectTransform)rarity.transform;
        rrt.anchorMin = new Vector2(0f, 1f);
        rrt.anchorMax = new Vector2(1f, 1f);
        rrt.pivot = new Vector2(0.5f, 1f);
        rrt.offsetMin = new Vector2(80f, -40f);
        rrt.offsetMax = new Vector2(-16f, -4f);

        RectTransform badge = Node("LayerBadge", root);
        Place(badge, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(-6f, 6f), new Vector2(76f, 76f), new Vector2(0f, 1f));
        Image badgeImg = Img(badge, new Color32(0x55, 0x55, 0x5E, 0xFF), circle);
        badgeImg.raycastTarget = false;
        TMP_Text order = Label(badge, "1", 38f);
        order.color = White;
        order.fontStyle = FontStyles.Bold;

        FishCardUI ui = root.gameObject.AddComponent<FishCardUI>();
        Wire(ui, "button", button);
        Wire(ui, "icon", icon);
        Wire(ui, "nameText", nameText);
        Wire(ui, "orderText", order);
        Wire(ui, "lockedOverlay", locked.gameObject);
        Wire(ui, "inDeckBadge", inDeck.gameObject);
        Wire(ui, "selectedFrame", sel.gameObject);
        Wire(ui, "frame", frame);
        Wire(ui, "rarityText", rarity);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, CardPrefabPath);
        Object.DestroyImmediate(root.gameObject);
        return prefab.GetComponent<FishCardUI>();
    }

    // ================================================================ tiny helpers

    private static RectTransform Screen(string name, Transform safe)
    {
        RectTransform rt = Node(name, safe);
        Stretch(rt);
        return rt;
    }

    private static RectTransform Node(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = 5; // UI
        if (parent != null) go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static void Stretch(RectTransform rt, float inset = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
    }

    private static void Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, Vector2? pivot = null)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot ?? anchorMin;
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
    }

    private static Image Img(RectTransform rt, Color color, Sprite sprite = null)
    {
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        if (sprite != null) img.sprite = sprite;
        return img;
    }

    private static TMP_Text Label(RectTransform parent, string text, float size)
    {
        RectTransform rt = Node("Text", parent);
        Stretch(rt);
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = Ink;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        return t;
    }

    private static Button Btn(RectTransform parent, string name, string text, Color color, float fontSize, Sprite sprite = null)
    {
        RectTransform rt = Node(name, parent);
        Image img = Img(rt, color, sprite);
        Button b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        Label(rt, text, fontSize);
        return b;
    }

    private static void Wire(Object target, string field, Object value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null)
        {
            Debug.LogError($"[MenuUIBuilder] {target.GetType().Name} ไม่มีฟิลด์ {field}");
            return;
        }
        p.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void WireArray(Object target, string field, Object[] values)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null)
        {
            Debug.LogError($"[MenuUIBuilder] {target.GetType().Name} ไม่มีฟิลด์ {field}");
            return;
        }
        p.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void WireFloat(Object target, string field, float value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null) return;
        p.floatValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
