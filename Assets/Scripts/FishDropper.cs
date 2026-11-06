using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(10)]
public class FishDropper : MonoBehaviour
{
    [Header("ขอบด้านในของโหลทั้งสองข้าง")]
    [SerializeField] private float minX = -3f;
    [SerializeField] private float maxX = 3f;

    [Header("จุดปล่อย / คูลดาวน์")]
    [SerializeField] private float spawnY = 6.2f;
    [SerializeField] private float dropCooldown = 0.6f;

    private Camera cam;
    private Fish held;
    private int nextTier;
    private float currentX;
    private bool ready;
    private bool dragging;

    public event Action<int> NextTierChanged;
    public int NextTier => nextTier;

    private void Start()
    {
        cam = Camera.main;
        nextTier = GameManager.Instance.RollTier();
        SpawnHeld();
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm.IsGameOver || !ready || held == null) return;

        Pointer pointer = Pointer.current;
        if (pointer == null) return;

        if (pointer.press.wasPressedThisFrame)
        {
            dragging = !IsPointerOverUI();
            if (dragging) MoveToScreenPos(pointer.position.ReadValue());
        }
        else if (dragging && pointer.press.isPressed)
        {
            MoveToScreenPos(pointer.position.ReadValue());
        }

        if (dragging && pointer.press.wasReleasedThisFrame)
        {
            dragging = false;
            Drop();
        }
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private void MoveToScreenPos(Vector2 screenPos)
    {
        Vector3 world = cam.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z));
        SetX(world.x);
    }

    private void SetX(float x)
    {
        if (held == null) return;

        float half = held.Diameter * 0.5f;
        currentX = Mathf.Clamp(x, minX + half, maxX - half);
        held.transform.position = new Vector3(currentX, spawnY, 0f);
    }

    private void Drop()
    {
        held.Release();
        held = null;
        ready = false;
        StartCoroutine(CooldownThenSpawn());
    }

    private IEnumerator CooldownThenSpawn()
    {
        yield return new WaitForSeconds(dropCooldown);
        if (GameManager.Instance.IsGameOver) yield break;
        SpawnHeld();
    }

    private void SpawnHeld()
    {
        GameManager gm = GameManager.Instance;

        held = gm.SpawnFish(nextTier, new Vector2(currentX, spawnY), true);
        SetX(currentX);

        nextTier = gm.RollTier();
        NextTierChanged?.Invoke(nextTier);
        ready = true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(minX, spawnY, 0), new Vector3(maxX, spawnY, 0));
    }
}