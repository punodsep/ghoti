using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject homePanel;
    [SerializeField] private GameObject deckPanel;
    [SerializeField] private GameObject gachaPanel;
    [SerializeField] private GameObject questPanel;

    [Header("Header")]
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text bestScoreText;

    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button deckButton;
    [SerializeField] private Button gachaButton;
    [SerializeField] private Button questButton;
    [SerializeField] private Button[] backButtons;

    private void Start()
    {
        playButton.onClick.AddListener(Play);
        deckButton.onClick.AddListener(() => Show(deckPanel));
        gachaButton.onClick.AddListener(() => Show(gachaPanel));
        questButton.onClick.AddListener(() => Show(questPanel));

        foreach (Button b in backButtons)
        {
            b.onClick.AddListener(() => Show(homePanel));
        }

        SaveSystem.Changed += RefreshHeader;
        RefreshHeader();
        Show(homePanel);
    }

    private void OnDestroy()
    {
        SaveSystem.Changed -= RefreshHeader;
    }

    private void Show(GameObject panel)
    {
        homePanel.SetActive(panel == homePanel);
        deckPanel.SetActive(panel == deckPanel);
        gachaPanel.SetActive(panel == gachaPanel);
        questPanel.SetActive(panel == questPanel);
    }

    private void RefreshHeader()
    {
        PlayerProfile p = SaveSystem.Profile;
        coinsText.text = p.coins.ToString("N0");
        bestScoreText.text = $"Best: {p.bestScore:N0}";
    }

    private void Play()
    {
        var deck = SaveSystem.Profile.GetDeck();
        if (deck.Count < GameSession.MinDeck || deck.Count > GameSession.MaxDeck) return;

        GameSession.SelectedDeck = deck;
        SceneManager.LoadScene("Game");
    }

    [ContextMenu("DEBUG: เพิ่ม 1000 เหรียญ")]
    private void DebugAddCoins()
    {
        SaveSystem.Profile.coins += 1000;
        SaveSystem.Save();
    }
}