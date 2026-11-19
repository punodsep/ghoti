using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class Fish : MonoBehaviour
{
    [SerializeField] private SpriteRenderer visual;

    private static int nextUid;
    private int uid;
    private CircleCollider2D circle;
    private Vector3 visualScale = Vector3.one;
    private float baseLinearDamping;
    private float baseAngularDamping;
    private float slowUntil;
    private Coroutine slowRoutine;

    public Rigidbody2D Body { get; private set; }
    public FishData Data { get; private set; }
    public FishAbility Ability => Data != null ? Data.ability : null;
    public int Tier { get; private set; }
    public float Diameter { get; private set; }
    public float Radius => Diameter * 0.5f;
    public bool IsDropped { get; private set; }
    public float DroppedTime { get; private set; }
    public bool IsMerged { get; private set; }

    /// ลำดับการเข้าโหล (น้อย = เข้าก่อน) ใช้ตัดสินกรณีเกิดพร้อมกัน
    public int SpawnOrder => uid;

    /// ปลาที่อยู่ในโหล ปล่อยแล้ว และยังไม่ถูกรวม/ลบ
    public bool IsInPlay => IsDropped && !IsMerged && gameObject.activeInHierarchy;

    /// สถานะของ ability แบบครั้งเดียวต่อปลา 1 ตัว (เช่น Manta Ray สลับที่ได้ครั้งเดียว)
    public bool AbilityUsed { get; set; }

    private void Awake()
    {
        Body = GetComponent<Rigidbody2D>();
        circle = GetComponent<CircleCollider2D>();
        uid = nextUid++;
        baseLinearDamping = Body.linearDamping;
        baseAngularDamping = Body.angularDamping;
    }

    public void Init(FishData data, int tier, float diameter, bool held)
    {
        Data = data;
        Tier = tier;
        Diameter = diameter;

        // Hitbox = ขนาดของ layer (diameter) ส่วนรูปถูกขยาย/เลื่อนให้ "ตัวปลาจริง" พอดีกับวงกลมนั้น
        visual.sprite = data.sprite;
        Vector2 spriteSize = data.sprite.bounds.size;
        float spriteMax = Mathf.Max(spriteSize.x, spriteSize.y);
        float bodyRadius = Mathf.Max(0.01f, data.hitboxRadius) * spriteMax;
        float s = diameter * 0.5f / bodyRadius;
        visualScale = Vector3.one * s;
        visual.transform.localScale = visualScale;
        visual.transform.localPosition = -(Vector3)(data.hitboxOffset * spriteMax * s);

        circle.radius = diameter * 0.5f;
        circle.offset = Vector2.zero;

        if (held)
        {
            Body.simulated = false;   // ถือค้างอยู่: ไม่ชน ไม่ตก
        }

        if (Ability != null)
        {
            Ability.OnSpawned(this);
        }
    }

    public void Release()
    {
        Body.simulated = true;
        Body.linearVelocity = Vector2.zero;
        IsDropped = true;
        DroppedTime = Time.time;
    }

    public void MarkMerged()
    {
        IsMerged = true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsInPlay) return;
        if (!collision.collider.TryGetComponent(out Fish other)) return;
        if (!other.IsInPlay) return;

        GameManager gm = GameManager.Instance;
        if (gm == null || gm.IsGameOver) return;

        // 1) On Collision
        if (Ability != null)
        {
            Ability.OnCollision(this, other, gm);
            if (!IsInPlay || !other.IsInPlay) return;   // ability อาจรวม/ลบไปแล้ว
        }

        // 2) Merge — ทั้งสองตัวได้ callback นี้ ให้ตัวที่เข้าโหลก่อนเป็นคนจัดการ
        if (uid > other.uid) return;
        if (!gm.CanMerge(this, other)) return;

        gm.MergeFish(this, other, false);
    }

    /// ทำให้ปลาช้าลง (ความเร็ว x factor) และหน่วงไว้ช่วงหนึ่ง เรียกซ้ำ = รีเฟรชเวลา
    public void Slow(float factor, float duration, float extraDamping = 6f)
    {
        Body.linearVelocity *= factor;
        Body.angularVelocity *= factor;
        Body.linearDamping = baseLinearDamping + extraDamping;
        Body.angularDamping = baseAngularDamping + extraDamping;

        slowUntil = Time.time + duration;
        if (slowRoutine == null)
        {
            slowRoutine = StartCoroutine(SlowRoutine());
        }
    }

    private IEnumerator SlowRoutine()
    {
        while (Time.time < slowUntil)
        {
            yield return null;
        }
        Body.linearDamping = baseLinearDamping;
        Body.angularDamping = baseAngularDamping;
        slowRoutine = null;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterFish(this);
        }
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
