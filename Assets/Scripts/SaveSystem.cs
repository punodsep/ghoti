using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    public static event Action Changed;

    private static PlayerProfile profile;

    public static string FilePath =>
        Path.Combine(Application.persistentDataPath, "profile.json");

    public static PlayerProfile Profile
    {
        get
        {
            if (profile == null) Load();
            return profile;
        }
    }

    public static void Load()
    {
        PlayerProfile loaded = null;

        try
        {
            if (File.Exists(FilePath))
            {
                loaded = JsonUtility.FromJson<PlayerProfile>(File.ReadAllText(FilePath));
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveSystem] อ่านไฟล์เซฟไม่ได้ จะเริ่มใหม่: {e.Message}");
            try { File.Copy(FilePath, FilePath + ".bak", true); } catch { }
            loaded = null;
        }

        bool isNew = loaded == null;
        profile = loaded ?? new PlayerProfile();

        Sanitize(profile);
        if (isNew) Save();
    }

    public static void Save()
    {
        if (profile == null) return;

        try
        {
            string json = JsonUtility.ToJson(profile, true);
            string tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, json);
            if (File.Exists(FilePath)) File.Delete(FilePath);
            File.Move(tmp, FilePath);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] เซฟไม่สำเร็จ: {e.Message}");
        }

        Changed?.Invoke();
    }

    /// ทำให้ข้อมูลที่โหลดมาถูกต้องเสมอ (ปลาที่ถูกลบ, Deck เกิน, ปลาที่ยังไม่ปลดล็อก ฯลฯ)
    private static void Sanitize(PlayerProfile p)
    {
        FishDatabase db = FishDatabase.Instance;
        if (db == null) return;

        // ปลา Default ต้องปลดล็อกเสมอ (รวมกรณีเพิ่มปลา Default ใหม่ทีหลัง)
        foreach (FishData f in db.allFish)
        {
            if (f != null && f.unlockType == UnlockType.Default) p.Unlock(f.id);
        }

        p.unlockedFishIds.RemoveAll(id => db.Get(id) == null);

        HashSet<string> seen = new HashSet<string>();
        p.deckFishIds.RemoveAll(id =>
            db.Get(id) == null || !p.IsUnlocked(id) || !seen.Add(id));

        if (p.deckFishIds.Count > GameSession.MaxDeck)
        {
            p.deckFishIds.RemoveRange(
                GameSession.MaxDeck, p.deckFishIds.Count - GameSession.MaxDeck);
        }

        if (p.deckFishIds.Count < GameSession.MinDeck)
        {
            foreach (FishData f in db.allFish)
            {
                if (p.deckFishIds.Count >= GameSession.MaxDeck) break;

                if (f != null && p.IsUnlocked(f.id) && !p.deckFishIds.Contains(f.id))
                {
                    p.deckFishIds.Add(f.id);
                }
            }
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        profile = null;
        Changed = null;
    }
}