using UnityEngine;

public abstract class FishAbility : ScriptableObject
{
    [Header("ข้อมูลที่แสดงใน UI")]
    public string abilityName;
    [TextArea] public string description;

    public virtual void OnSpawned(Fish fish) { }

    public virtual void OnMerged(Fish created, GameManager gm) { }

    public virtual int ModifyMergeScore(int score) => score;
}