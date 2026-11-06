using System.Collections.Generic;

public static class GameSession
{
    public const int MinDeck = 7;
    public const int MaxDeck = 7;

    // หน้าจัด Deck (เฟสหน้า) จะเขียนค่าลงตรงนี้ก่อนโหลดซีนเกม
    public static List<FishData> SelectedDeck { get; set; } = new List<FishData>();

    public static bool HasValidDeck =>
        SelectedDeck != null &&
        SelectedDeck.Count >= MinDeck &&
        SelectedDeck.Count <= MaxDeck;
}