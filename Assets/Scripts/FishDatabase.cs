using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Fish/Fish Database", fileName = "FishDatabase")]
public class FishDatabase : ScriptableObject
{
    public List<FishData> allFish = new List<FishData>();

    private static FishDatabase instance;
    private Dictionary<string, FishData> lookup;

    public static FishDatabase Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<FishDatabase>("FishDatabase");
                if (instance == null)
                {
                    Debug.LogError("[FishDatabase] ไม่พบ Resources/FishDatabase.asset");
                }
            }
            return instance;
        }
    }

    public FishData Get(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        if (lookup == null)
        {
            lookup = new Dictionary<string, FishData>();
            foreach (FishData f in allFish)
            {
                if (f == null || string.IsNullOrEmpty(f.id)) continue;
                if (!lookup.TryAdd(f.id, f))
                {
                    Debug.LogError($"[FishDatabase] id ซ้ำ: {f.id}");
                }
            }
        }

        lookup.TryGetValue(id, out FishData result);
        return result;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }
}