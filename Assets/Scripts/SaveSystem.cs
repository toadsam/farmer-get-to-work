using System.Collections;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance { get; private set; }

    [Header("Save File")]
    public string saveFileName = "farm_save.json";

    [Header("Auto")]
    public bool autoLoadOnStart = true;
    public bool autoSaveOnSessionFinished = true;
    public bool autoSaveOnApplicationPause = true;
    public bool autoSaveOnApplicationQuit = true;

    [Header("Runtime")]
    public GameSaveData currentSaveData;

    private string SavePath
    {
        get
        {
            return Path.Combine(Application.persistentDataPath, saveFileName);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;

        BindFocusSessionService();

        if (autoLoadOnStart)
            LoadGame();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        FocusSessionService focusService = FocusSessionService.Instance;
        if (focusService != null)
            focusService.OnSessionFinished -= HandleSessionFinished;
    }

    private void BindFocusSessionService()
    {
        FocusSessionService focusService = FocusSessionService.Instance;

        if (focusService == null)
            focusService = FindAnyObjectByType<FocusSessionService>();

        if (focusService == null)
            return;

        focusService.OnSessionFinished -= HandleSessionFinished;
        focusService.OnSessionFinished += HandleSessionFinished;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindFocusSessionService();

        if (currentSaveData != null)
            StartCoroutine(ApplyLoadedDataNextFrame());
    }

    private IEnumerator ApplyLoadedDataNextFrame()
    {
        yield return null;
        ApplyLoadedDataToSceneManagers();
    }

    private void HandleSessionFinished(
        FocusSessionResult result,
        RewardResultData rewardResult
    )
    {
        if (!autoSaveOnSessionFinished)
            return;

        StartCoroutine(SaveNextFrame());
    }

    private IEnumerator SaveNextFrame()
    {
        yield return null;
        SaveGame();
    }

    private void OnApplicationPause(bool pause)
    {
        if (!pause)
            return;

        if (autoSaveOnApplicationPause)
            SaveGame();
    }

    private void OnApplicationQuit()
    {
        if (autoSaveOnApplicationQuit)
            SaveGame();
    }

    public void SaveGame()
    {
        currentSaveData = CaptureCurrentData();

        if (currentSaveData == null)
        {
            Debug.LogWarning("[SaveSystem] 저장할 데이터가 없습니다.", this);
            return;
        }

        currentSaveData.savedAtTicks = GameDataUtility.NowTicks();

        string json = JsonUtility.ToJson(currentSaveData, true);

        string directory = Path.GetDirectoryName(SavePath);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(SavePath, json);

        Debug.Log($"[SaveSystem] 저장 완료: {SavePath}");
    }

    public bool LoadGame()
    {
        if (!File.Exists(SavePath))
        {
            currentSaveData = GameSaveData.CreateNew();
            Debug.Log("[SaveSystem] 저장 파일이 없어 새 데이터를 생성했습니다.");
            ApplyLoadedDataToRuntimeManagers();
            ApplyLoadedDataToSceneManagers();
            return false;
        }

        string json = File.ReadAllText(SavePath);

        if (string.IsNullOrEmpty(json))
        {
            currentSaveData = GameSaveData.CreateNew();
            Debug.LogWarning("[SaveSystem] 저장 파일이 비어 있어 새 데이터를 생성했습니다.");
            return false;
        }

        currentSaveData = JsonUtility.FromJson<GameSaveData>(json);

        if (currentSaveData == null)
        {
            currentSaveData = GameSaveData.CreateNew();
            Debug.LogWarning("[SaveSystem] 저장 데이터 파싱에 실패해 새 데이터를 생성했습니다.");
            return false;
        }

        ApplyLoadedDataToRuntimeManagers();
        ApplyLoadedDataToSceneManagers();

        Debug.Log($"[SaveSystem] 불러오기 완료: {SavePath}");
        return true;
    }

    public GameSaveData CaptureCurrentData()
    {
        GameSaveData data = currentSaveData != null
            ? CloneSaveData(currentSaveData)
            : GameSaveData.CreateNew();

        FarmManager farmManager = GetFarmManager();
        UnlockManager unlockManager = GetUnlockManager();
        SessionRecordManager recordManager = SessionRecordManager.Instance;

        FocusMusicPlayer musicPlayer = FocusMusicPlayer.Instance;

        if (musicPlayer == null)
            musicPlayer = FindAnyObjectByType<FocusMusicPlayer>();

        if (musicPlayer != null)
        {
            data.unlockedMusicTrackIds = musicPlayer.CaptureUnlockedTrackIds();
        }

        if (recordManager == null)
            recordManager = FindAnyObjectByType<SessionRecordManager>();

        if (farmManager != null)
        {
            data.gold = farmManager.gold;
            data.stamina = farmManager.stamina;
            data.maxStamina = farmManager.maxStamina;

            IslandStateData farmIslandData = farmManager.CaptureIslandStateData();
            UpsertIslandData(data, farmIslandData);
        }

        IslandSetManager islandSetManager = IslandSetManager.Instance;

        if (islandSetManager == null)
            islandSetManager = FindAnyObjectByType<IslandSetManager>();

        if (islandSetManager != null)
        {
            List<IslandStateData> islandStates = islandSetManager.CaptureIslandStates();

            foreach (IslandStateData islandState in islandStates)
            {
                UpsertIslandData(data, islandState);
            }
        }

        if (unlockManager != null)
        {
            data.totalUnlockProgress = unlockManager.totalProgress;
        }

        if (recordManager != null)
        {
            data.sessionRecords = recordManager.GetAllRecords();
        }

        return data;
    }

    private void ApplyLoadedDataToRuntimeManagers()
    {
        if (currentSaveData == null)
            return;

        SessionRecordManager recordManager = SessionRecordManager.Instance;

        if (recordManager == null)
            recordManager = FindAnyObjectByType<SessionRecordManager>();

        if (recordManager != null)
            recordManager.SetRecords(currentSaveData.sessionRecords);

        FocusMusicPlayer musicPlayer = FocusMusicPlayer.Instance;

        if (musicPlayer == null)
            musicPlayer = FindAnyObjectByType<FocusMusicPlayer>();

        if (musicPlayer != null)
        {
            musicPlayer.ApplyUnlockedTrackIds(currentSaveData.unlockedMusicTrackIds);
        }
    }

    private void ApplyLoadedDataToSceneManagers()
    {
        if (currentSaveData == null)
            return;

        FarmManager farmManager = GetFarmManager();
        UnlockManager unlockManager = GetUnlockManager();

        if (farmManager != null)
        {
            farmManager.SetResources(
                currentSaveData.gold,
                currentSaveData.stamina,
                currentSaveData.maxStamina
            );
        }

        if (unlockManager != null)
        {
            unlockManager.SetProgress(
                currentSaveData.totalUnlockProgress,
                recalculateUnlocks: true
            );
        }

        if (farmManager != null)
        {
            IslandStateData islandData = FindIslandData(
                currentSaveData,
                farmManager.farmIslandId
            );

            if (islandData != null)
                farmManager.ApplyIslandStateData(islandData);
        }

        IslandSetManager islandSetManager = IslandSetManager.Instance;

        if (islandSetManager == null)
            islandSetManager = FindAnyObjectByType<IslandSetManager>();

        if (islandSetManager != null)
        {
            islandSetManager.ApplyIslandStates(currentSaveData.islands);
        }
    }

    private FarmManager GetFarmManager()
    {
        if (FarmManager.Instance != null)
            return FarmManager.Instance;

        return FindAnyObjectByType<FarmManager>();
    }

    private UnlockManager GetUnlockManager()
    {
        if (UnlockManager.Instance != null)
            return UnlockManager.Instance;

        return FindAnyObjectByType<UnlockManager>();
    }

    private IslandStateData FindIslandData(GameSaveData data, string islandId)
    {
        if (data == null || data.islands == null)
            return null;

        foreach (IslandStateData island in data.islands)
        {
            if (island == null)
                continue;

            if (island.islandId == islandId)
                return island;
        }

        return null;
    }

    private void UpsertIslandData(GameSaveData data, IslandStateData islandData)
    {
        if (data == null || islandData == null)
            return;

        if (data.islands == null)
            data.islands = new List<IslandStateData>();

        int index = data.islands.FindIndex(
            island => island != null && island.islandId == islandData.islandId
        );

        if (index >= 0)
            data.islands[index] = islandData;
        else
            data.islands.Add(islandData);
    }

    private GameSaveData CloneSaveData(GameSaveData source)
    {
        if (source == null)
            return GameSaveData.CreateNew();

        string json = JsonUtility.ToJson(source);
        return JsonUtility.FromJson<GameSaveData>(json);
    }

    public bool HasSaveFile()
    {
        return File.Exists(SavePath);
    }

    public void DeleteSaveFile()
    {
        if (File.Exists(SavePath))
            File.Delete(SavePath);

        currentSaveData = GameSaveData.CreateNew();

        FarmManager farmManager = GetFarmManager();
        UnlockManager unlockManager = GetUnlockManager();

        SessionRecordManager recordManager = SessionRecordManager.Instance;
        if (recordManager == null)
            recordManager = FindAnyObjectByType<SessionRecordManager>();

        IslandSetManager islandSetManager = IslandSetManager.Instance;
        if (islandSetManager == null)
            islandSetManager = FindAnyObjectByType<IslandSetManager>();

        if (farmManager != null)
            farmManager.ResetFarmForNewGame();

        if (unlockManager != null)
            unlockManager.ResetProgressForNewGame();

        if (recordManager != null)
            recordManager.ClearRecords();

        if (islandSetManager != null)
            islandSetManager.ResetAllIslandsForNewGame();

        SaveGame();

        Debug.Log("[SaveSystem] 저장 데이터 초기화 완료");
    }

    [ContextMenu("Debug Save Game")]
    public void DebugSaveGame()
    {
        SaveGame();
    }

    [ContextMenu("Debug Load Game")]
    public void DebugLoadGame()
    {
        LoadGame();
    }

    [ContextMenu("Debug Delete Save File")]
    public void DebugDeleteSaveFile()
    {
        DeleteSaveFile();
    }

    [ContextMenu("Debug Print Save Path")]
    public void DebugPrintSavePath()
    {
        Debug.Log($"[SaveSystem] Save Path: {SavePath}");
    }
}