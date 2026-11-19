using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// เมนูหลัก: หน้าแรก (START) → หน้าใน (Deck / Gacha / Leaderboard / Shop) + แถบล่างพร้อมปุ่ม GO
/// ฉากสร้างได้จาก Tools/Ghoti/Build Menu UI
public class MenuController : MonoBehaviour
{
    [Header("Screens")]
    [SerializeField] private GameObject titleScreen;
    [SerializeField] private GameObject deckScreen;
    [SerializeField] private GameObject gachaScreen;
    [SerializeField] private GameObject leaderboardScreen;
    [SerializeField] private GameObject shopScreen;

    [Header("Title")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button howToPlayButton;

    [Header("Bottom nav")]
    [SerializeField] private GameObject bottomNav;
    [SerializeField] private Button navGacha;
    [SerializeField] private Button navDeck;
    [SerializeField] private Button navGo;
    [SerializeField] private Button navLeaderboard;
    [SerializeField] private Button navShop;
    [SerializeField] private Color navNormalColor = new Color32(0xD6, 0xD6, 0xD6, 0xFF);
    [SerializeField] private Color navActiveColor = new Color32(0xFF, 0x9A, 0x9A, 0xFF);

    [Header("Popups")]
    [SerializeField] private GameObject howToPlayPopup;
    [SerializeField] private GameObject settingsPopup;
    [SerializeField] private Button[] settingsButtons;
    [SerializeField] private Button[] popupCloseButtons;

    [Header("Header")]
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text bestScoreText;

    /// กลับจากเกมแล้วข้ามหน้าแรก ไปหน้า Deck เลย
    private static bool returningFromGame;

    private void Start()
    {
        Hook(startButton, () => Show(deckScreen));
        Hook(howToPlayButton, () => SetPopup(howToPlayPopup, true));

        Hook(navGacha, () => Show(gachaScreen));
        Hook(navDeck, () => Show(deckScreen));
        Hook(navGo, Play);
        Hook(navLeaderboard, () => Show(leaderboardScreen));
        Hook(navShop, () => Show(shopScreen));

        if (settingsButtons != null)
        {
            foreach (Button b in settingsButtons) Hook(b, () => SetPopup(settingsPopup, true));
        }
        if (popupCloseButtons != null)
        {
            foreach (Button b in popupCloseButtons) Hook(b, CloseAllPopups);
        }

        SaveSystem.Changed += RefreshHeader;
        RefreshHeader();
        CloseAllPopups();
        Show(returningFromGame ? deckScreen : titleScreen);
    }

    private void OnDestroy()
    {
        SaveSystem.Changed -= RefreshHeader;
    }

    private static void Hook(Button b, UnityEngine.Events.UnityAction action)
    {
        if (b != null) b.onClick.AddListener(action);
    }

    private void Show(GameObject screen)
    {
        Set(titleScreen, screen);
        Set(deckScreen, screen);
        Set(gachaScreen, screen);
        Set(leaderboardScreen, screen);
        Set(shopScreen, screen);

        if (bottomNav != null) bottomNav.SetActive(screen != titleScreen);

        Highlight(navGacha, screen == gachaScreen);
        Highlight(navDeck, screen == deckScreen);
        Highlight(navLeaderboard, screen == leaderboardScreen);
        Highlight(navShop, screen == shopScreen);
    }

    private static void Set(GameObject go, GameObject active)
    {
        if (go != null) go.SetActive(go == active);
    }

    private void Highlight(Button b, bool active)
    {
        if (b != null && b.image != null) b.image.color = active ? navActiveColor : navNormalColor;
    }

    private static void SetPopup(GameObject popup, bool open)
    {
        if (popup != null) popup.SetActive(open);
    }

    private void CloseAllPopups()
    {
        SetPopup(howToPlayPopup, false);
        SetPopup(settingsPopup, false);
    }

    private void RefreshHeader()
    {
        PlayerProfile p = SaveSystem.Profile;
        if (coinsText != null) coinsText.text = $"{p.coins:N0} เหรียญ";
        if (bestScoreText != null) bestScoreText.text = $"Best: {p.bestScore:N0}";
    }

    private void Play()
    {
        var deck = SaveSystem.Profile.GetDeck();
        if (deck.Count < GameSession.MinDeck || deck.Count > GameSession.MaxDeck)
        {
            // Deck ยังมีช่องว่าง → พาไปหน้า Deck แล้วบอก
            Show(deckScreen);
            DeckBuilderUI ui = deckScreen != null ? deckScreen.GetComponent<DeckBuilderUI>() : null;
            if (ui != null) ui.ShowToast($"ใส่ปลาให้ครบ {GameSession.MaxDeck} layer ก่อนเล่น ({deck.Count}/{GameSession.MaxDeck})");
            return;
        }

        GameSession.SelectedDeck = deck;
        returningFromGame = true;
        SceneManager.LoadScene("Game");
    }

    [ContextMenu("DEBUG: เพิ่ม 1000 เหรียญ")]
    private void DebugAddCoins()
    {
        SaveSystem.Profile.coins += 1000;
        SaveSystem.Save();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        returningFromGame = false;
    }
}
