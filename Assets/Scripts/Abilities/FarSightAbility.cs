using UnityEngine;

/// Fish_8 Angelfish — On Spawn (while in tank)
/// Angelfish แต่ละตัวในโหล ทำให้เห็นปลาตัวถัดไปเพิ่ม 1 ตัว (สูงสุดตามที่ GameManager กำหนด)
[CreateAssetMenu(menuName = "Fish/Abilities/Far Sight", fileName = "Ability_FarSight")]
public class FarSightAbility : FishAbility
{
    public int previewsPerFish = 1;

    public override int ExtraUpcomingPreviews => previewsPerFish;
}
