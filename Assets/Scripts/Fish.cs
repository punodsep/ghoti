using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class Fish : MonoBehaviour
{
    [SerializeField] private SpriteRenderer visual;

    private static int nextUid;
    private int uid;
    private bool merged;
    private CircleCollider2D circle;
    private Vector3 visualScale = Vector3.one;

    public Rigidbody2D Body { get; private set; }
    public FishData Data { get; private set; }
    public int Tier { get; private set; }
    public float Diameter { get; private set; }
    public bool IsDropped { get; private set; }
    public float DroppedTime { get; private set; }

    private void Awake()
    {
        Body = GetComponent<Rigidbody2D>();
        circle = GetComponent<CircleCollider2D>();
        uid = nextUid++;
    }

    public void Init(FishData data, int tier, float diameter, bool held)
    {
        Data = data;
        Tier = tier;
        Diameter = diameter;

        visual.sprite = data.sprite;
        Vector2 spriteSize = data.sprite.bounds.size;
        float s = diameter / Mathf.Max(spriteSize.x, spriteSize.y);
        visualScale = Vector3.one * s;
        visual.transform.localScale = visualScale;

        circle.radius = diameter * 0.5f;

        if (data.ability != null)
        {
            data.ability.OnSpawned(this);
        }

        if (held)
        {
            Body.simulated = false;   // ถือค้างอยู่: ไม่ชน ไม่ตก
        }
    }

    public void Release()
    {
        Body.simulated = true;
        Body.linearVelocity = Vector2.zero;
        IsDropped = true;
        DroppedTime = Time.time;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (merged || !IsDropped) return;

        if (!collision.collider.TryGetComponent(out Fish other)) return;
        if (other.merged || !other.IsDropped || other.Tier != Tier) return;

        // ให้ตัวที่ uid ต่ำกว่าเป็นคนจัดการ กันรวมซ้ำสองรอบ
        if (uid > other.uid) return;

        merged = true;
        other.merged = true;
        GameManager.Instance.MergeFish(this, other);
    }

    public void PlayPop()
    {
        StartCoroutine(PopRoutine());
    }

    private IEnumerator PopRoutine()
    {
        const float duration = 0.18f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float eased = 1f - (1f - k) * (1f - k);
            visual.transform.localScale = visualScale * Mathf.Lerp(0.5f, 1f, eased);
            yield return null;
        }
        visual.transform.localScale = visualScale;
    }
}