using UnityEngine;

public class IslandActivationInteractable : MonoBehaviour
{
    [Header("Target Island")]
    public IslandSetController islandSet;

    [Header("Message")]
    public string lockedMessage = "아직 해금되지 않은 섬입니다.";
    public string alreadyActivatedMessage = "이미 활성화된 섬입니다.";

    private void Awake()
    {
        if (islandSet == null)
            islandSet = GetComponentInParent<IslandSetController>();
    }

    private void OnValidate()
    {
        if (islandSet == null)
            islandSet = GetComponentInParent<IslandSetController>();
    }

    public bool CanShowActivationPopup(out string reason)
    {
        if (islandSet == null)
        {
            reason = "연결된 섬 정보가 없습니다.";
            return false;
        }

        if (!islandSet.unlocked)
        {
            reason = lockedMessage;
            return false;
        }

        if (islandSet.activated)
        {
            reason = alreadyActivatedMessage;
            return false;
        }

        reason = "";
        return true;
    }
}