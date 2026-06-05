using UnityEngine;

public class GameRoot : MonoBehaviour
{
    public static GameRoot Instance { get; private set; }

    public SceneFlowManager SceneFlow { get; private set; }
    public GameStateManager GameState { get; private set; }
    public AppFocusTracker AppFocus { get; private set; }
    public FocusSessionService FocusSession { get; private set; }
    public RewardProcessor RewardProcessor { get; private set; }
    public SessionRecordManager SessionRecord { get; private set; }
    public SaveSystem SaveSystem { get; private set; }
    public FocusMusicPlayer FocusMusic { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
            return;

        GameObject rootObject = new GameObject("[GameRoot]");
        rootObject.AddComponent<GameRoot>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneFlow = EnsureComponent<SceneFlowManager>();
        GameState = EnsureComponent<GameStateManager>();
        AppFocus = EnsureComponent<AppFocusTracker>();
        FocusSession = EnsureComponent<FocusSessionService>();
        RewardProcessor = EnsureComponent<RewardProcessor>();
        SessionRecord = EnsureComponent<SessionRecordManager>();
        SaveSystem = EnsureComponent<SaveSystem>();
        FocusMusic = EnsureComponent<FocusMusicPlayer>();

        Debug.Log("[GameRoot] 생성 완료");
    }

    private T EnsureComponent<T>() where T : Component
    {
        T component = GetComponent<T>();

        if (component == null)
            component = gameObject.AddComponent<T>();

        return component;
    }
}