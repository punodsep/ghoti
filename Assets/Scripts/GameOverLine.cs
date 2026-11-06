using UnityEngine;

public class GameOverLine : MonoBehaviour
{
    [SerializeField] private Vector2 size = new Vector2(6f, 0.3f);
    [SerializeField] private LayerMask fishMask;
    [SerializeField] private float graceAfterDrop = 1.5f;
    [SerializeField] private float timeToLose = 2f;

    private float dangerTimer;

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.IsGameOver) return;

        Collider2D[] hits = Physics2D.OverlapBoxAll(transform.position, size, 0f, fishMask);
        bool danger = false;

        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent(out Fish fish) &&
                fish.IsDropped &&
                Time.time - fish.DroppedTime > graceAfterDrop)
            {
                danger = true;
                break;
            }
        }

        dangerTimer = danger ? dangerTimer + Time.deltaTime : 0f;

        if (dangerTimer >= timeToLose)
        {
            gm.GameOver();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, size);
    }
}