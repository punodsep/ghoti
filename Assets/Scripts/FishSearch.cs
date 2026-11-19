using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

/// ค้นหาปลาในหน้าจัด Deck แบบ real-time
///   "jell", "jelly fish"  → ชื่อที่ใกล้ที่สุดขึ้นก่อน (พิมพ์ผิดนิดหน่อยก็เจอ)
///   "7", "layer 6", "l6" → ปลาทุกตัวของ layer นั้น
public static class FishSearch
{
    private static readonly Regex LayerQuery =
        new Regex(@"^\s*(?:layer|lv|lvl|l|ชั้น)?\s*(\d+)\s*$", RegexOptions.IgnoreCase);

    public static List<FishData> Filter(IReadOnlyList<FishData> all, string query)
    {
        List<(FishData fish, float score, int index)> hits = new List<(FishData, float, int)>();
        string q = Normalize(query);

        bool showAll = q.Length == 0 || "layer".StartsWith(q);   // ยังพิมพ์ "lay..." ไม่จบ
        bool byLayer = TryParseLayer(query, out int layer);

        for (int i = 0; i < all.Count; i++)
        {
            FishData f = all[i];
            if (f == null) continue;

            if (showAll)
            {
                hits.Add((f, 0f, i));
            }
            else if (byLayer)
            {
                if (f.layer == layer) hits.Add((f, 0f, i));
            }
            else
            {
                float score = NameScore(f, q);
                if (score >= 0f) hits.Add((f, score, i));
            }
        }

        hits.Sort((a, b) =>
        {
            int c = a.score.CompareTo(b.score);
            if (c != 0) return c;
            c = a.fish.layer.CompareTo(b.fish.layer);
            if (c != 0) return c;
            c = a.fish.rarity.CompareTo(b.fish.rarity);
            return c != 0 ? c : a.index.CompareTo(b.index);
        });

        List<FishData> result = new List<FishData>(hits.Count);
        foreach (var h in hits) result.Add(h.fish);
        return result;
    }

    public static bool TryParseLayer(string raw, out int layer)
    {
        layer = 0;
        if (string.IsNullOrEmpty(raw)) return false;
        Match m = LayerQuery.Match(raw);
        return m.Success && int.TryParse(m.Groups[1].Value, out layer);
    }

    /// คะแนนยิ่งน้อยยิ่งใกล้ (-1 = ไม่ตรงเลย)
    private static float NameScore(FishData f, string q)
    {
        string name = Normalize(f.displayName);
        if (name.Length == 0) return -1f;

        if (name == q) return 0f;
        if (name.StartsWith(q)) return 1f;

        foreach (string word in (f.displayName ?? "").ToLowerInvariant().Split(' ', '-', '_'))
        {
            if (word.Length > 0 && Normalize(word).StartsWith(q)) return 2f;
        }

        if (name.Contains(q)) return 3f;

        // พิมพ์ผิดเล็กน้อย: เทียบกับทุกช่วงของชื่อที่ยาวเท่าคำค้น
        int allowed = q.Length <= 2 ? 0 : (q.Length <= 5 ? 1 : 2);
        int best = BestWindowDistance(q, name);
        if (best <= allowed) return 4f + best;

        // ตัวอักษรเรียงตามลำดับ เช่น "jlyfsh"
        if (q.Length >= 3 && IsSubsequence(q, name)) return 7f;

        // ชื่อความสามารถ เช่น "zap", "rage"
        if (f.ability != null)
        {
            string ab = Normalize(f.ability.abilityName);
            if (ab.Length > 0 && (ab.StartsWith(q) || ab.Contains(q))) return 8f;
        }

        return -1f;
    }

    public static string Normalize(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        StringBuilder sb = new StringBuilder(s.Length);
        foreach (char c in s.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c)) sb.Append(c);
        }
        return sb.ToString();
    }

    private static int BestWindowDistance(string q, string name)
    {
        if (name.Length <= q.Length) return Levenshtein(q, name);

        int best = int.MaxValue;
        for (int len = q.Length - 1; len <= q.Length + 1; len++)
        {
            if (len <= 0 || len > name.Length) continue;
            for (int start = 0; start + len <= name.Length; start++)
            {
                int d = Levenshtein(q, name.Substring(start, len));
                if (d < best) best = d;
            }
        }
        return best;
    }

    private static bool IsSubsequence(string q, string name)
    {
        int i = 0;
        foreach (char c in name)
        {
            if (i < q.Length && q[i] == c) i++;
        }
        return i == q.Length;
    }

    private static int Levenshtein(string a, string b)
    {
        int[] prev = new int[b.Length + 1];
        int[] cur = new int[b.Length + 1];
        for (int j = 0; j <= b.Length; j++) prev[j] = j;

        for (int i = 1; i <= a.Length; i++)
        {
            cur[0] = i;
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                cur[j] = System.Math.Min(System.Math.Min(cur[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
            }
            (prev, cur) = (cur, prev);
        }
        return prev[b.Length];
    }
}
