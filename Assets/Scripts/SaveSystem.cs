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

        // Deck: ช่องที่ i = layer i+1 และต้องเป็นปลาของ layer นั้นที่ปลดล็อกแล้ว
        // ช่องว่างเก็บเป็น "" — เซฟเก่า (ก่อนมีช่องว่าง) หรือโปรไฟล์ใหม่ เติมปลาเริ่มต้นให้
        List<string> previous = new List<string>(p.deckFishIds);
        bool fillMissing = p.version < 2 || previous.Count == 0;
        p.deckFishIds.Clear();

        for (int layer = 1; layer <= GameSession.MaxDeck; layer++)
        {
            string pick = previous.Find(id =>
            {
                FishData f = db.Get(id);
                return f != null && f.layer == layer && p.IsUnlocked(id);
            });

            if (pick == null && fillMissing) pick = DefaultForLayer(db, p, layer);
            p.deckFishIds.Add(pick ?? "");
        }

        p.version = 2;
    }

    /// ล้าง Deck ทุกช่อง (ว่างทั้ง 7 layer) แล้วเซฟ
    public static void ClearDeck()
    {
        PlayerProfile p = Profile;
        p.deckFishIds.Clear();
        for (int layer = 1; layer <= GameSession.MaxDeck; layer++) p.deckFishIds.Add("");
        Save();
    }

    /// ปลาสำหรับ layer ที่ว่าง: ปลา Default ก่อน แล้วค่อยตัวอื่นที่ปลดล็อกแล้ว
    private static string DefaultForLayer(FishDatabase db, PlayerProfile p, int layer)
    {
        string fallback = null;
        foreach (FishData f in db.allFish)
        {
            if (f == null || f.layer != layer || !p.IsUnlocked(f.id)) continue;
            if (f.unlockType == UnlockType.Default) return f.id;
            if (fallback == null) fallback = f.id;
        }
        return fallback;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        profile = null;
        Changed = null;
    }
}