using System.Collections;
using UnityEngine;

public class PendingRewardApplier : MonoBehaviour
{
    [Header("Apply")]
    public bool applyOnStart = true;
    public bool saveAfterApply = true;

    private IEnumerator Start()
    {
        if (!applyOnStart)
            yield break;

        // SaveSystem이 먼저 저장 데이터를 FarmManager/IslandSetManager에 적용할 시간을 줍니다.
        yield return null;
        yield return null;

        RewardProcessor rewardProcessor = RewardProcessor.Instance;

        if (rewardProcessor == null)
            rewardProcessor = FindAnyObjectByType<RewardProcessor>();

        if (rewardProcessor == null)
        {
            Debug.LogWarning("[PendingRewardApplier] RewardProcessor가 없습니다.", this);
            yield break;
        }

        bool applied = rewardProcessor.TryApplyLastRewardToCurrentWorld();

        if (applied && saveAfterApply && SaveSystem.Instance != null)
        {
            SaveSystem.Instance.SaveGame();
        }
    }
}