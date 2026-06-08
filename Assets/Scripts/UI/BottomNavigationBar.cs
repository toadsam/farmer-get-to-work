using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FarmerGetToWork
{
    /// <summary>
    /// 하단 네비게이션 프리팹 루트에 붙는 스크립트입니다.
    /// 현재 씬에 해당하는 버튼을 하이라이트하고, 각 버튼의 씬 이동을 담당합니다.
    /// </summary>
    public class BottomNavigationBar : MonoBehaviour
    {
        private const string PersistenceKey = "BottomNavigationBar";

        [SerializeField] private Image barSkinImage;
        [SerializeField] private BottomNavButton homeButton;
        [SerializeField] private BottomNavButton goalButton;
        [SerializeField] private BottomNavButton shopButton;
        [SerializeField] private BottomNavButton collectionButton;
        [SerializeField] private BottomNavButton recordButton;

        private bool isChromeInstance = true;

        private void Awake()
        {
            isChromeInstance = global::SceneChromePersistence.KeepSingleInstance(
                gameObject,
                PersistenceKey
            );

            if (!isChromeInstance)
                return;

            Bind();
            ConfigureButtons();
            SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
            SceneManager.activeSceneChanged += HandleActiveSceneChanged;
        }

        private void Start()
        {
            if (!isChromeInstance)
                return;

            HighlightCurrentScene();
        }

        private void OnDestroy()
        {
            if (!isChromeInstance)
                return;

            SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
            global::SceneChromePersistence.Release(gameObject, PersistenceKey);
        }

        private void OnValidate()
        {
            Bind();
        }

        public void NavigateHome()
        {
            SceneLoader.LoadScene(SceneLoader.HomeScene);
        }

        public void NavigateGoal()
        {
            SceneLoader.LoadScene(SceneLoader.GoalScene);
        }

        public void NavigateShop()
        {
            SceneLoader.LoadScene(SceneLoader.ShopScene);
        }

        public void NavigateCollection()
        {
            SceneLoader.LoadScene(SceneLoader.CollectionScene);
        }

        public void NavigateRecord()
        {
            SceneLoader.LoadScene(SceneLoader.RecordScene);
        }

        private void ConfigureButtons()
        {
            homeButton?.Configure(SceneLoader.HomeScene, "홈");
            goalButton?.Configure(SceneLoader.GoalScene, "목표");
            shopButton?.Configure(SceneLoader.ShopScene, "상점");
            collectionButton?.Configure(SceneLoader.CollectionScene, "도감");
            recordButton?.Configure(SceneLoader.RecordScene, "기록");
        }

        private void HighlightCurrentScene()
        {
            HighlightScene(SceneManager.GetActiveScene().name);
        }

        private void HighlightScene(string sceneName)
        {
            homeButton?.SetHighlighted(sceneName == SceneLoader.HomeScene);
            goalButton?.SetHighlighted(sceneName == SceneLoader.GoalScene);
            shopButton?.SetHighlighted(sceneName == SceneLoader.ShopScene);
            collectionButton?.SetHighlighted(sceneName == SceneLoader.CollectionScene);
            recordButton?.SetHighlighted(sceneName == SceneLoader.RecordScene);
        }

        private void HandleActiveSceneChanged(Scene oldScene, Scene newScene)
        {
            ConfigureButtons();
            HighlightScene(newScene.name);
        }

        private void Bind()
        {
            if (barSkinImage == null)
            {
                barSkinImage = UIBinder.FindImage(transform, "Img_Background");
            }

            if (homeButton == null)
            {
                homeButton = UIBinder.FindComponent<BottomNavButton>(transform, "Btn_Home");
            }

            if (goalButton == null)
            {
                goalButton = UIBinder.FindComponent<BottomNavButton>(transform, "Btn_Goal");
            }

            if (shopButton == null)
            {
                shopButton = UIBinder.FindComponent<BottomNavButton>(transform, "Btn_Shop");
            }

            if (collectionButton == null)
            {
                collectionButton = UIBinder.FindComponent<BottomNavButton>(transform, "Btn_Collection");
            }

            if (recordButton == null)
            {
                recordButton = UIBinder.FindComponent<BottomNavButton>(transform, "Btn_Record");
            }
        }
    }
}

public static class SceneChromePersistence
{
    private const string BottomNavigationKey = "BottomNavigationBar";
    private const string RootName = "[PersistentSceneChrome]";
    private const string TopStatusKey = "TopStatusBar";

    private static readonly HashSet<string> BottomNavigationScenes = new HashSet<string>
    {
        FarmerGetToWork.SceneLoader.HomeScene,
        FarmerGetToWork.SceneLoader.GoalScene,
        "GoalScene"
    };

    private static readonly HashSet<string> TopStatusScenes = new HashSet<string>
    {
        FarmerGetToWork.SceneLoader.HomeScene,
        FarmerGetToWork.SceneLoader.GoalScene,
        FarmerGetToWork.SceneLoader.FocusScene,
        "GoalScene",
        "FocusScene"
    };

    private static readonly Dictionary<string, GameObject> Instances =
        new Dictionary<string, GameObject>();

    private static RectTransform root;
    private static bool subscribedToSceneChanges;

    public static bool KeepSingleInstance(GameObject target, string key)
    {
        if (target == null || string.IsNullOrEmpty(key))
            return false;

        if (!ShouldPersistForCurrentScene(key))
            return true;

        GameObject existing;
        if (Instances.TryGetValue(key, out existing) && existing != null)
        {
            if (existing == target)
                return true;

            Object.Destroy(target);
            return false;
        }

        Instances[key] = target;
        MoveToPersistentCanvas(target);
        return true;
    }

    public static void Release(GameObject target, string key)
    {
        if (target == null || string.IsNullOrEmpty(key))
            return;

        GameObject existing;
        if (Instances.TryGetValue(key, out existing) && existing == target)
            Instances.Remove(key);
    }

    private static bool ShouldPersistForCurrentScene(string key)
    {
        Scene scene = SceneManager.GetActiveScene();
        return ShouldPersist(key, scene.name);
    }

    private static bool ShouldPersist(string key, string sceneName)
    {
        if (key == BottomNavigationKey)
            return BottomNavigationScenes.Contains(sceneName);

        if (key == TopStatusKey)
            return TopStatusScenes.Contains(sceneName);

        return false;
    }

    private static void MoveToPersistentCanvas(GameObject target)
    {
        RectTransform targetRect = target.transform as RectTransform;
        if (targetRect == null)
            return;

        EnsureRoot(target);
        targetRect.SetParent(root, false);
    }

    private static void EnsureRoot(GameObject source)
    {
        if (root != null)
            return;

        GameObject rootObject = new GameObject(
            RootName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        root = rootObject.GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        Canvas canvas = rootObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;

        CopyCanvasScaler(source, rootObject.GetComponent<CanvasScaler>());

        Object.DontDestroyOnLoad(rootObject);
        SubscribeToSceneChanges();
    }

    private static void CopyCanvasScaler(GameObject source, CanvasScaler targetScaler)
    {
        if (source == null || targetScaler == null)
            return;

        CanvasScaler sourceScaler = source.GetComponentInParent<CanvasScaler>();
        if (sourceScaler == null)
            return;

        targetScaler.uiScaleMode = sourceScaler.uiScaleMode;
        targetScaler.referencePixelsPerUnit = sourceScaler.referencePixelsPerUnit;
        targetScaler.scaleFactor = sourceScaler.scaleFactor;
        targetScaler.referenceResolution = sourceScaler.referenceResolution;
        targetScaler.screenMatchMode = sourceScaler.screenMatchMode;
        targetScaler.matchWidthOrHeight = sourceScaler.matchWidthOrHeight;
        targetScaler.physicalUnit = sourceScaler.physicalUnit;
        targetScaler.fallbackScreenDPI = sourceScaler.fallbackScreenDPI;
        targetScaler.defaultSpriteDPI = sourceScaler.defaultSpriteDPI;
        targetScaler.dynamicPixelsPerUnit = sourceScaler.dynamicPixelsPerUnit;
    }

    private static void SubscribeToSceneChanges()
    {
        if (subscribedToSceneChanges)
            return;

        SceneManager.activeSceneChanged += HandleActiveSceneChanged;
        subscribedToSceneChanges = true;
    }

    private static void HandleActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        List<string> keysToRelease = new List<string>();
        foreach (KeyValuePair<string, GameObject> instance in Instances)
        {
            if (!ShouldPersist(instance.Key, newScene.name))
                keysToRelease.Add(instance.Key);
        }

        foreach (string key in keysToRelease)
        {
            GameObject instance = Instances[key];
            Instances.Remove(key);

            if (instance != null)
                Object.Destroy(instance);
        }

        if (Instances.Count > 0)
            return;

        if (root != null)
        {
            Object.Destroy(root.gameObject);
            root = null;
        }
    }
}
