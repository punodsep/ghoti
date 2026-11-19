#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class SaveTools
{
    [MenuItem("Tools/Fish/Delete Save File")]
    private static void DeleteSave()
    {
        if (File.Exists(SaveSystem.FilePath))
        {
            File.Delete(SaveSystem.FilePath);
            Debug.Log("ลบไฟล์เซฟแล้ว");
        }
        else
        {
            Debug.Log("ไม่มีไฟล์เซฟ");
        }
    }

    [MenuItem("Tools/Fish/Open Save Folder")]
    private static void OpenFolder()
    {
        EditorUtility.RevealInFinder(Application.persistentDataPath);
    }

    [MenuItem("Tools/Fish/Unlock All Fish (debug)")]
    private static void UnlockAll()
    {
        int count = 0;
        foreach (FishData fish in FishDatabase.Instance.allFish)
        {
            if (fish != null && SaveSystem.Profile.Unlock(fish.id)) count++;
        }
        SaveSystem.Save();
        Debug.Log($"ปลดล็อกปลาเพิ่ม {count} ตัว (ทั้งหมด {SaveSystem.Profile.unlockedFishIds.Count})");
    }
}
#endif
