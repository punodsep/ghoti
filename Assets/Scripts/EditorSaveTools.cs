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
}