using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// เพิ่มขนาด Game view = iPhone 15 / 16 (1179 x 2556, แนวตั้ง) แล้วเลือกให้อัตโนมัติ
/// ตั้งครั้งแรกตอนเปิดโปรเจกต์ (ครั้งเดียว) และกดเองได้ที่ Tools/Fish/Game View: iPhone 15-16
[InitializeOnLoad]
public static class GameViewIPhoneSize
{
    private const int Width = 1179;
    private const int Height = 2556;
    private const string Label = "iPhone 15/16";
    private const string DoneKey = "Ghoti.GameViewIPhoneSize.Applied";

    static GameViewIPhoneSize()
    {
        if (EditorPrefs.GetBool(DoneKey, false)) return;
        EditorApplication.delayCall += () =>
        {
            if (Apply()) EditorPrefs.SetBool(DoneKey, true);
        };
    }

    [MenuItem("Tools/Fish/Game View: iPhone 15-16 (1179x2556)")]
    private static void ApplyFromMenu()
    {
        if (!Apply())
        {
            Debug.LogWarning("[GameViewIPhoneSize] ตั้งขนาด Game view ไม่สำเร็จ — เพิ่มเองที่ Game view > Free Aspect > + (1179 x 2556)");
        }
    }

    private static bool Apply()
    {
        try
        {
            Assembly asm = typeof(Editor).Assembly;
            Type sizesType = asm.GetType("UnityEditor.GameViewSizes");
            Type sizeType = asm.GetType("UnityEditor.GameViewSize");
            Type sizeKind = asm.GetType("UnityEditor.GameViewSizeType");
            Type gameViewType = asm.GetType("UnityEditor.GameView");

            object sizes = typeof(ScriptableSingleton<>).MakeGenericType(sizesType)
                .GetProperty("instance").GetValue(null);
            object group = sizesType.GetProperty("currentGroup").GetValue(sizes);
            Type groupType = group.GetType();

            int index = FindSize(group, groupType);
            if (index < 0)
            {
                object size = sizeType
                    .GetConstructor(new[] { sizeKind, typeof(int), typeof(int), typeof(string) })
                    .Invoke(new[] { Enum.Parse(sizeKind, "FixedResolution"), (object)Width, Height, Label });
                groupType.GetMethod("AddCustomSize").Invoke(group, new[] { size });
                sizesType.GetMethod("SaveToHDD")?.Invoke(sizes, null);
                index = FindSize(group, groupType);
            }
            if (index < 0) return false;

            EditorWindow gameView = EditorWindow.GetWindow(gameViewType, false, null, false);
            MethodInfo select = gameViewType.GetMethod("SizeSelectionCallback",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (select != null)
            {
                select.Invoke(gameView, new object[] { index, null });
            }
            else
            {
                gameViewType.GetProperty("selectedSizeIndex",
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    ?.SetValue(gameView, index);
            }
            gameView.Repaint();
            Debug.Log($"[GameViewIPhoneSize] Game view = {Label} ({Width} x {Height})");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[GameViewIPhoneSize] {e.GetType().Name}: {e.Message}");
            return false;
        }
    }

    private static int FindSize(object group, Type groupType)
    {
        int total = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null);
        MethodInfo get = groupType.GetMethod("GetGameViewSize");
        for (int i = 0; i < total; i++)
        {
            object s = get.Invoke(group, new object[] { i });
            Type t = s.GetType();
            int w = (int)t.GetProperty("width").GetValue(s);
            int h = (int)t.GetProperty("height").GetValue(s);
            if (w == Width && h == Height) return i;
        }
        return -1;
    }
}
