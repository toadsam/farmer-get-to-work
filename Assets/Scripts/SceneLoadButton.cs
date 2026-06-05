using UnityEngine;
using UnityEngine.UI;

public enum GameSceneTarget
{
    Title,
    Tutorial,
    Home,
    Goal,
    Focus,
    Success,
    Fail,
    Shop,
    Collection,
    Record,
    Custom
}

[RequireComponent(typeof(Button))]
public class SceneLoadButton : MonoBehaviour
{
    [Header("Target")]
    public GameSceneTarget targetScene;

    [Tooltip("Target Scene이 Custom일 때만 사용합니다.")]
    public string customSceneName;

    [Header("Button")]
    public bool autoBindOnAwake = true;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (autoBindOnAwake && button != null)
        {
            button.onClick.RemoveListener(LoadTargetScene);
            button.onClick.AddListener(LoadTargetScene);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(LoadTargetScene);
    }

    public void LoadTargetScene()
    {
        SceneFlowManager flow = SceneFlowManager.Instance;

        if (flow == null)
        {
            Debug.LogError("[SceneLoadButton] SceneFlowManager.Instance가 없습니다.");
            return;
        }

        switch (targetScene)
        {
            case GameSceneTarget.Title:
                flow.GoTitle();
                break;

            case GameSceneTarget.Tutorial:
                flow.GoTutorial();
                break;

            case GameSceneTarget.Home:
                flow.GoHome();
                break;

            case GameSceneTarget.Goal:
                flow.GoGoal();
                break;

            case GameSceneTarget.Focus:
                flow.GoFocus();
                break;

            case GameSceneTarget.Success:
                flow.GoSuccess();
                break;

            case GameSceneTarget.Fail:
                flow.GoFail();
                break;

            case GameSceneTarget.Shop:
                flow.GoShop();
                break;

            case GameSceneTarget.Collection:
                flow.GoCollection();
                break;

            case GameSceneTarget.Record:
                flow.GoRecord();
                break;

            case GameSceneTarget.Custom:
                flow.LoadScene(customSceneName);
                break;
        }
    }
}
