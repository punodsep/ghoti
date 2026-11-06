using UnityEngine;

[CreateAssetMenu(menuName = "Fish/Abilities/Score Bonus", fileName = "Ability_ScoreBonus")]
public class ScoreBonusAbility : FishAbility
{
    public float multiplier = 1.5f;

    public override int ModifyMergeScore(int score)
    {
        return Mathf.RoundToInt(score * multiplier);
    }
}