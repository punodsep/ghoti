using UnityEngine;

/// Fish_18 Manta Ray — On Collision
/// ครั้งแรกที่ตกลงบนปลาที่ใหญ่กว่า จะมุดลงไปสลับที่กับปลาตัวนั้น (ครั้งเดียวต่อ Manta 1 ตัว)
[CreateAssetMenu(menuName = "Fish/Abilities/Undertow", fileName = "Ability_Undertow")]
public class UndertowAbility : FishAbility
{
    [Tooltip("Manta ต้องอยู่สูงกว่าปลาใหญ่อย่างน้อยเท่านี้ (หน่วย) ถึงนับว่า 'ตกลงบน'")]
    public float minHeightAbove = 0.1f;

    public override void OnCollision(Fish self, Fish other, GameManager gm)
    {
        if (self.AbilityUsed) return;
        if (other.Tier <= self.Tier) return;

        Vector2 mine = self.Body.position;
        Vector2 theirs = other.Body.position;
        if (mine.y - theirs.y < minHeightAbove) return;

        self.AbilityUsed = true;

        self.Body.position = theirs;
        other.Body.position = mine;
        self.transform.position = theirs;
        other.transform.position = mine;

        self.Body.linearVelocity = Vector2.zero;
        other.Body.linearVelocity = Vector2.zero;
        self.Body.angularVelocity = 0f;
        other.Body.angularVelocity = 0f;

        gm.RaiseAbilityTriggered(this, theirs);
    }
}
