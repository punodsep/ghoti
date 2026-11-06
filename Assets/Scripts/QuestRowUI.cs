using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestRowUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private Slider bar;

    public void Setup(FishData fish)
    {
        bool done = SaveSystem.Profile.IsUnlocked(fish.id);
        int target = Mathf.Max(1, fish.questTarget);
        int progress = Mathf.Min(ProgressService.GetProgress(fish), target);

        icon.sprite = fish.sprite;
        icon.preserveAspect = true;
        icon.color = done ? Color.white : new Color(0.25f, 0.25f, 0.25f, 1f);

        titleText.text = done ? fish.displayName : "???";
        descText.text = ProgressService.QuestText(fish);

        bar.minValue = 0;
        bar.maxValue = target;
        bar.value = done ? target : progress;

        progressText.text = done ? "สำเร็จ" : $"{progress:N0}/{target:N0}";
    }
}