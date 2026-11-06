using System.Linq;
using UnityEngine;

public class QuestListUI : MonoBehaviour
{
    [SerializeField] private QuestRowUI rowPrefab;
    [SerializeField] private Transform container;

    private void OnEnable()
    {
        foreach (Transform child in container)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        PlayerProfile p = SaveSystem.Profile;

        var quests = FishDatabase.Instance.allFish
            .Where(f => f != null && f.unlockType == UnlockType.Quest)
            .OrderBy(f => p.IsUnlocked(f.id))      // ที่ยังไม่สำเร็จขึ้นก่อน
            .ThenBy(f => f.questTarget);

        foreach (FishData fish in quests)
        {
            Instantiate(rowPrefab, container).Setup(fish);
        }
    }
}