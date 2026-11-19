using System.IO;
using UnityEditor;
using UnityEngine;

/// คำนวณ hitbox วงกลมของปลาแต่ละตัวจากรูปจริง (พื้นที่ส่วนทึบ → วงกลมพื้นที่เท่ากัน, ศูนย์กลาง = จุดกึ่งกลางมวล)
/// ใช้หลังเปลี่ยนรูปปลา: Tools/Fish/Fit Hitboxes To Sprites
public static class FishHitboxTools
{
    private const float AlphaThreshold = 0.5f;

    [MenuItem("Tools/Fish/Fit Hitboxes To Sprites")]
    private static void FitAll()
    {
        int done = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:FishData"))
        {
            FishData fish = AssetDatabase.LoadAssetAtPath<FishData>(AssetDatabase.GUIDToAssetPath(guid));
            if (fish != null && Fit(fish)) done++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[FishHitboxTools] ปรับ hitbox แล้ว {done} ตัว");
    }

    public static bool Fit(FishData fish)
    {
        if (fish.sprite == null) return false;

        string path = AssetDatabase.GetAssetPath(fish.sprite.texture);
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return false;

        // อ่านไฟล์ตรงๆ เพื่อไม่ต้องเปิด Read/Write ของ texture
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(File.ReadAllBytes(path))) return false;

        // สไปรต์อาจเป็นส่วนหนึ่งของ sheet → ใช้ rect ของสไปรต์ (สเกลตามขนาดไฟล์จริง)
        Rect r = fish.sprite.rect;
        float kx = (float)tex.width / fish.sprite.texture.width;
        float ky = (float)tex.height / fish.sprite.texture.height;
        int x0 = Mathf.RoundToInt(r.x * kx), y0 = Mathf.RoundToInt(r.y * ky);
        int w = Mathf.RoundToInt(r.width * kx), h = Mathf.RoundToInt(r.height * ky);

        Color32[] px = tex.GetPixels32();
        long n = 0;
        double sx = 0, sy = 0;
        byte threshold = (byte)(AlphaThreshold * 255);

        for (int y = y0; y < y0 + h; y++)
        {
            for (int x = x0; x < x0 + w; x++)
            {
                if (px[y * tex.width + x].a <= threshold) continue;
                n++;
                sx += x - x0;
                sy += y - y0;
            }
        }
        Object.DestroyImmediate(tex);
        if (n == 0) return false;

        float side = Mathf.Max(w, h);
        Vector2 pivot = fish.sprite.pivot / new Vector2(r.width, r.height);   // 0..1
        float cx = (float)(sx / n), cy = (float)(sy / n);                     // y ขึ้น (GetPixels เริ่มจากล่าง)

        Undo.RecordObject(fish, "Fit Fish Hitbox");
        fish.hitboxRadius = Mathf.Sqrt(n / Mathf.PI) / side;
        fish.hitboxOffset = new Vector2((cx - pivot.x * w) / side, (cy - pivot.y * h) / side);
        EditorUtility.SetDirty(fish);
        return true;
    }
}
