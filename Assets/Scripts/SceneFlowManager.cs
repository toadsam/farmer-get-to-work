using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFlowManager : MonoBehaviour
{
    public static SceneFlowManager Instance { get; private set; }

    [Header("Scene Names")]
    public string titleSceneName = "00_TitleScene";
    public string tutorialSceneName = "01_TutorialScene";
    public string homeSceneName = "KBW";
    public string goalSceneName = "03_GoalScene";
    public string focusSceneName = "04_FocusScene";
    public string successSceneName = "05_SuccessScene";
    public string failSceneName = "06_FailScene";
    public string shopSceneName = "07_ShopScene";
    public string collectionSceneName = "08_CollectionScene";
    public string recordSceneName = "09_RecordScene";

    public bool IsLoading { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    public void GoTitle()
    {
        LoadScene(titleSceneName);
    }

    public void GoTutorial()
    {
        LoadScene(tutorialSceneName);
    }

    public void GoHome()
    {
        LoadScene(homeSceneName);
    }

    public void GoGoal()
    {
        LoadScene(goalSceneName);
    }

    public void GoFocus()
    {
        LoadScene(focusSceneName);
    }

    public void GoSuccess()
    {
        LoadScene(successSceneName);
    }

    public void GoFail()
    {
        LoadScene(failSceneName);
    }

    public void GoShop()
    {
        LoadScene(shopSceneName);
    }

    public void GoCollection()
    {
        LoadScene(collectionSceneName);
    }

    public void GoRecord()
    {
        LoadScene(recordSceneName);
    }

    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SceneFlow] 씬 이름이 비어 있습니다.");
            return;
        }

        if (IsLoading)
        {
            Debug.LogWarning("[SceneFlow] 이미 씬을 로딩 중입니다.");
            return;
        }

        if (SceneManager.GetActiveScene().name == sceneName)
            return;

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                $"[SceneFlow] '{sceneName}' 씬을 불러올 수 없습니다. Build Settings 또는 Build Profile의 Scene 목록에 추가되어 있는지 확인하세요."
            );
            return;
        }

        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        IsLoading = true;

        Debug.Log($"[SceneFlow] 씬 이동: {sceneName}");

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        while (!operation.isDone)
            yield return null;

        IsLoading = false;
    }

    public string GetCurrentSceneName()
    {
        return SceneManager.GetActiveScene().name;
    }
}
