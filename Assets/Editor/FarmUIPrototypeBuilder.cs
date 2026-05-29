using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FarmerGetToWork;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

/// <summary>
/// Builds the mobile UI prototype as editable Unity UI objects.
/// 목업을 한 장짜리 이미지로 넣지 않고, 교체 가능한 Img_/Btn_/Txt_ 슬롯 Hierarchy를 생성합니다.
/// </summary>
public static class FarmUIPrototypeBuilder
{
    private const float ReferenceWidth = 1080f;
    private const float ReferenceHeight = 1920f;
    private const string PlaceholderPath = "Assets/Sprites/Placeholder/Placeholder_White.png";
    private const string TMPSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
    private const string DefaultTMPFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/MobileDefaultTMPFont.asset";
    private const string KoreanFontSourcePath = "Assets/TextMesh Pro/Fonts/MalgunGothic.ttf";
    private const string AutoRunRequestPath = "Assets/Editor/FarmUIPrototypeBuilder.autorun";

    private static Sprite placeholderSprite;
    private static TMP_FontAsset defaultFontAsset;

    private static readonly Color BackgroundColor = new Color(0.82f, 0.91f, 0.77f, 1f);
    private static readonly Color PanelColor = new Color(0.96f, 0.93f, 0.82f, 1f);
    private static readonly Color ButtonColor = new Color(0.35f, 0.72f, 0.38f, 1f);
    private static readonly Color SecondaryButtonColor = new Color(0.86f, 0.88f, 0.80f, 1f);
    private static readonly Color CardColor = new Color(1f, 0.98f, 0.90f, 1f);
    private static readonly Color TextColor = new Color(0.18f, 0.22f, 0.18f, 1f);

    private sealed class SceneContext
    {
        public Scene scene;
        public GameObject root;
        public RectTransform safeArea;
    }

    [MenuItem("Tools/Farmer Get To Work/Build UI Prototype")]
    public static void BuildAll()
    {
        EnsureFolders();
        EnsureTextMeshPro();
        placeholderSprite = EnsurePlaceholderSprite();
        defaultFontAsset = EnsureDefaultTMPFontAsset();

        CreateCommonPrefabs();
        BuildTitleScene();
        BuildTutorialScene();
        BuildHomeScene();
        BuildGoalScene();
        BuildFocusScene();
        BuildSuccessScene();
        BuildFailScene();
        BuildShopScene();
        BuildCollectionScene();
        BuildRecordScene();
        UpdateBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("일해라농장주 UI 프로토타입 생성 완료");
    }

    [InitializeOnLoadMethod]
    private static void BuildIfAutoRunRequested()
    {
        if (!File.Exists(AutoRunRequestPath))
        {
            return;
        }

        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(AutoRunRequestPath))
            {
                return;
            }

            File.Delete(AutoRunRequestPath);
            BuildAll();
        };
    }

    public static void BuildAllFromCommandLine()
    {
        try
        {
            BuildAll();
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static void EnsureFolders()
    {
        foreach (string path in new[]
        {
            "Assets/Scenes",
            "Assets/Scripts/Core",
            "Assets/Scripts/UI",
            "Assets/Scripts/Scenes",
            "Assets/Scripts/Focus",
            "Assets/Scripts/Shop",
            "Assets/Prefabs/Common",
            "Assets/Prefabs/Cards",
            "Assets/Prefabs/Panels",
            "Assets/Sprites/Placeholder",
            "Assets/Sprites/UI",
            "Assets/Sprites/Icons",
            "Assets/Sprites/Farm",
            "Assets/Sprites/Characters",
            "Assets/Materials"
        })
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }

    private static void EnsureTextMeshPro()
    {
        bool missingSettings = !File.Exists(TMPSettingsPath);
        bool missingShaders = !Directory.Exists("Assets/TextMesh Pro/Shaders");

        if (missingSettings || missingShaders)
        {
            string packagePath = Path.Combine(
                EditorApplication.applicationContentsPath,
                "Resources/PackageManager/BuiltInPackages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage");

            if (File.Exists(packagePath))
            {
                AssetDatabase.ImportPackage(packagePath, false);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
            else
            {
                TMP_PackageResourceImporter.ImportResources(true, false, false);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
        }

        if (!File.Exists(TMPSettingsPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(TMPSettingsPath));
            TMP_Settings settings = ScriptableObject.CreateInstance<TMP_Settings>();
            settings.name = "TMP Settings";
            AssetDatabase.CreateAsset(settings, TMPSettingsPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(TMPSettingsPath);
        }
    }

    private static Sprite EnsurePlaceholderSprite()
    {
        if (!File.Exists(PlaceholderPath))
        {
            Texture2D texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            Color[] pixels = Enumerable.Repeat(Color.white, 64).ToArray();
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(PlaceholderPath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(PlaceholderPath);
        }

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(PlaceholderPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderPath);
    }

    private static TMP_FontAsset EnsureDefaultTMPFontAsset()
    {
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DefaultTMPFontPath);
        if (existing != null)
        {
            return existing;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(DefaultTMPFontPath));

        try
        {
            // Windows 기준 한글 표시를 위해 맑은 고딕을 프로젝트 내부 폰트 소스로 복사해 사용합니다.
            // Dynamic Atlas로 생성해야 한글 문자가 런타임에 추가되어 네모 박스로 보이지 않습니다.
            TMP_FontAsset fontAsset = null;
            string windowsFontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts", "malgun.ttf");
            if (File.Exists(windowsFontPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(KoreanFontSourcePath));
                if (!File.Exists(KoreanFontSourcePath))
                {
                    File.Copy(windowsFontPath, KoreanFontSourcePath);
                }

                AssetDatabase.ImportAsset(KoreanFontSourcePath, ImportAssetOptions.ForceSynchronousImport);
                Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(KoreanFontSourcePath);
                if (sourceFont != null)
                {
                    fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
                }
            }

            if (fontAsset == null)
            {
                fontAsset = TMP_FontAsset.CreateFontAsset("Malgun Gothic", "Regular", 90);
            }

            if (fontAsset == null)
            {
                string interFontPath = Path.Combine(EditorApplication.applicationContentsPath, "Resources/Fonts/Inter-Regular.ttf");
                fontAsset = TMP_FontAsset.CreateFontAsset(interFontPath, 0, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048);
            }

            fontAsset.name = "MobileDefaultTMPFont";
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            fontAsset.isMultiAtlasTexturesEnabled = true;

            AssetDatabase.CreateAsset(fontAsset, DefaultTMPFontPath);
            EnsureEmbeddedFontResources(fontAsset, "MobileDefaultTMPFont");
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(DefaultTMPFontPath);
            TMP_Settings.defaultFontAsset = fontAsset;
            EditorUtility.SetDirty(TMP_Settings.instance);
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DefaultTMPFontPath);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"TMP FontAsset 자동 생성은 건너뜁니다. TextMeshProUGUI 슬롯은 생성되며, 필요한 경우 TMP FontAsset을 나중에 지정하세요. {exception.GetType().Name}: {exception.Message}");
        }

        return null;
    }

    private static void EnsureEmbeddedFontResources(TMP_FontAsset fontAsset, string resourceName)
    {
        if (fontAsset == null)
        {
            return;
        }

        Texture2D atlasTexture = null;
        if (fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0)
        {
            atlasTexture = fontAsset.atlasTextures[0];
        }

        if (atlasTexture == null)
        {
            atlasTexture = new Texture2D(1, 1, TextureFormat.Alpha8, false);
        }

        atlasTexture.name = resourceName + " Atlas";
        if (fontAsset.atlasTextures == null || fontAsset.atlasTextures.Length == 0)
        {
            fontAsset.atlasTextures = new[] { atlasTexture };
        }
        else
        {
            fontAsset.atlasTextures[0] = atlasTexture;
        }

        if (!AssetDatabase.Contains(atlasTexture))
        {
            AssetDatabase.AddObjectToAsset(atlasTexture, fontAsset);
        }

        Material material = fontAsset.material;
        if (material == null)
        {
            Shader shader = Shader.Find("TextMeshPro/Mobile/Distance Field");
            if (shader == null)
            {
                shader = Shader.Find("TextMeshPro/Distance Field");
            }

            if (shader != null)
            {
                material = new Material(shader);
                fontAsset.material = material;
            }
        }

        if (material != null)
        {
            material.name = resourceName + " Material";
            material.SetTexture(ShaderUtilities.ID_MainTex, atlasTexture);
            material.SetFloat(ShaderUtilities.ID_TextureWidth, fontAsset.atlasWidth);
            material.SetFloat(ShaderUtilities.ID_TextureHeight, fontAsset.atlasHeight);
            material.SetFloat(ShaderUtilities.ID_GradientScale, fontAsset.atlasPadding + 1);
            material.SetFloat(ShaderUtilities.ID_WeightNormal, fontAsset.normalStyle);
            material.SetFloat(ShaderUtilities.ID_WeightBold, fontAsset.boldStyle);

            if (!AssetDatabase.Contains(material))
            {
                AssetDatabase.AddObjectToAsset(material, fontAsset);
            }

            EditorUtility.SetDirty(material);
        }

        EditorUtility.SetDirty(atlasTexture);
        EditorUtility.SetDirty(fontAsset);
    }

    private static void CreateCommonPrefabs()
    {
        CreateTopStatusBarPrefab();
        CreateBottomNavigationBarPrefab();
        CreateButtonPrefab("PrimaryButton", ButtonColor);
        CreateButtonPrefab("SecondaryButton", SecondaryButtonColor);
        CreateImageSlotPrefab();
        CreatePopupPanelPrefab();
        CreateCardPrefab("GoalCard", typeof(GoalCardUI));
        CreateCardPrefab("ShopItemCard", typeof(ShopItemCardUI));
        CreateCardPrefab("CollectionItemCard", typeof(CollectionItemCardUI));
        CreateCardPrefab("AchievementCard", typeof(AchievementCardUI));
        CreateStatusPanelPrefab();
        CreateRewardPanelPrefab();
        CreateResultPanelPrefab();
    }

    private static void CreateTopStatusBarPrefab()
    {
        GameObject root = CreatePrefabRoot("TopStatusBar", new Vector2(1000f, 128f));
        AddStatPanel(root.transform, "Panel_Gold", "Img_CoinIcon", "골드", "12,345", StatPanelKind.Gold, new Vector2(-340f, 0f));
        AddStatPanel(root.transform, "Panel_FocusTime", "Img_ClockIcon", "오늘 집중 시간", "2시간 35분", StatPanelKind.TodayFocus, Vector2.zero);
        AddStatPanel(root.transform, "Panel_Streak", "Img_TrophyIcon", "연속 성공", "7일", StatPanelKind.Streak, new Vector2(340f, 0f));
        SavePrefab(root, "Assets/Prefabs/Common/TopStatusBar.prefab");
    }

    private static void CreateBottomNavigationBarPrefab()
    {
        GameObject root = CreatePrefabRoot("BottomNavigationBar", new Vector2(1080f, 168f));
        AddImage(root.transform, "Img_Background", Vector2.zero, new Vector2(1080f, 168f), new Color(0.91f, 0.93f, 0.84f, 1f));

        string[] names = { "Btn_Home", "Btn_Goal", "Btn_Shop", "Btn_Collection", "Btn_Record" };
        string[] labels = { "홈", "목표", "상점", "도감", "기록" };
        string[] scenes = { SceneLoader.HomeScene, SceneLoader.GoalScene, SceneLoader.ShopScene, SceneLoader.CollectionScene, SceneLoader.RecordScene };

        for (int i = 0; i < names.Length; i++)
        {
            GameObject button = AddButton(root.transform, names[i], new Vector2(-432f + 216f * i, 0f), new Vector2(188f, 124f), labels[i], "Txt_Label", true, SecondaryButtonColor);
            BottomNavButton navButton = button.AddComponent<BottomNavButton>();
            navButton.Configure(scenes[i], labels[i]);
        }

        root.AddComponent<BottomNavigationBar>();
        SavePrefab(root, "Assets/Prefabs/Common/BottomNavigationBar.prefab");
    }

    private static void CreateButtonPrefab(string prefabName, Color skinColor)
    {
        GameObject root = CreatePrefabRoot(prefabName, new Vector2(420f, 112f));
        root.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
        root.AddComponent<Button>();
        Image skin = AddImage(root.transform, "Img_ButtonSkin", Vector2.zero, new Vector2(420f, 112f), skinColor);
        root.GetComponent<Button>().targetGraphic = skin;
        AddImage(root.transform, "Img_Icon", new Vector2(-145f, 0f), new Vector2(58f, 58f), new Color(1f, 1f, 1f, 0.8f));
        AddText(root.transform, "Txt_ButtonLabel", new Vector2(30f, 0f), new Vector2(260f, 80f), "버튼", 34f, TextAlignmentOptions.Center, Color.white);
        SavePrefab(root, $"Assets/Prefabs/Common/{prefabName}.prefab");
    }

    private static void CreateImageSlotPrefab()
    {
        GameObject root = CreatePrefabRoot("ImageSlot", new Vector2(200f, 200f));
        AddImage(root.transform, "Img_Icon", Vector2.zero, new Vector2(200f, 200f), new Color(0.72f, 0.82f, 0.88f, 1f));
        SavePrefab(root, "Assets/Prefabs/Common/ImageSlot.prefab");
    }

    private static void CreatePopupPanelPrefab()
    {
        GameObject root = CreatePrefabRoot("PopupPanel", new Vector2(860f, 620f));
        AddImage(root.transform, "Img_PanelSkin", Vector2.zero, new Vector2(860f, 620f), PanelColor);
        AddText(root.transform, "Txt_Title", new Vector2(0f, 210f), new Vector2(760f, 80f), "팝업", 42f, TextAlignmentOptions.Center, TextColor);
        AddText(root.transform, "Txt_Description", new Vector2(0f, 40f), new Vector2(720f, 240f), "내용", 30f, TextAlignmentOptions.Center, TextColor);
        SavePrefab(root, "Assets/Prefabs/Common/PopupPanel.prefab");
    }

    private static void CreateCardPrefab(string prefabName, Type componentType)
    {
        GameObject root = CreatePrefabRoot(prefabName, new Vector2(300f, 330f));
        Image raycastImage = root.AddComponent<Image>();
        raycastImage.color = new Color(1f, 1f, 1f, 0f);
        root.AddComponent<Button>();

        AddImage(root.transform, "Img_CardSkin", Vector2.zero, new Vector2(300f, 330f), CardColor);
        AddImage(root.transform, "Img_Icon", new Vector2(0f, 78f), new Vector2(110f, 110f), new Color(0.62f, 0.82f, 0.56f, 1f));
        AddText(root.transform, "Txt_Title", new Vector2(0f, -18f), new Vector2(250f, 52f), "카드 제목", 27f, TextAlignmentOptions.Center, TextColor);
        AddText(root.transform, "Txt_Description", new Vector2(0f, -72f), new Vector2(250f, 58f), "설명", 23f, TextAlignmentOptions.Center, new Color(0.34f, 0.38f, 0.34f, 1f));
        AddText(root.transform, "Txt_Value", new Vector2(0f, -130f), new Vector2(220f, 44f), "+120", 25f, TextAlignmentOptions.Center, new Color(0.70f, 0.45f, 0.12f, 1f));

        Image selected = AddImage(root.transform, "Img_SelectedOutline", Vector2.zero, new Vector2(316f, 346f), new Color(0.23f, 0.62f, 0.23f, 0.36f));
        selected.gameObject.SetActive(false);
        Image lockOverlay = AddImage(root.transform, "Img_LockOverlay", Vector2.zero, new Vector2(300f, 330f), new Color(0f, 0f, 0f, 0.42f));
        lockOverlay.gameObject.SetActive(false);

        root.AddComponent(componentType);
        SavePrefab(root, $"Assets/Prefabs/Cards/{prefabName}.prefab");
    }

    private static void CreateStatusPanelPrefab()
    {
        GameObject root = CreatePrefabRoot("StatusPanel", new Vector2(320f, 118f));
        AddStatPanelContent(root.transform, "Img_Icon", "라벨", "값");
        root.AddComponent<StatPanelUI>();
        SavePrefab(root, "Assets/Prefabs/Panels/StatusPanel.prefab");
    }

    private static void CreateRewardPanelPrefab()
    {
        GameObject root = CreatePrefabRoot("RewardPanel", new Vector2(880f, 190f));
        AddImage(root.transform, "Img_PanelSkin", Vector2.zero, new Vector2(880f, 190f), PanelColor);
        AddImage(root.transform, "Img_ItemIcon", new Vector2(-330f, 0f), new Vector2(110f, 110f), new Color(0.80f, 0.66f, 0.30f, 1f));
        AddText(root.transform, "Txt_Label", new Vector2(-40f, 38f), new Vector2(520f, 48f), "보상", 30f, TextAlignmentOptions.Left, TextColor);
        AddText(root.transform, "Txt_Value", new Vector2(-40f, -32f), new Vector2(520f, 60f), "+120 골드", 35f, TextAlignmentOptions.Left, new Color(0.70f, 0.45f, 0.12f, 1f));
        SavePrefab(root, "Assets/Prefabs/Panels/RewardPanel.prefab");
    }

    private static void CreateResultPanelPrefab()
    {
        GameObject root = CreatePrefabRoot("ResultPanel", new Vector2(900f, 260f));
        AddImage(root.transform, "Img_PanelSkin", Vector2.zero, new Vector2(900f, 260f), PanelColor);
        AddText(root.transform, "Txt_Title", new Vector2(0f, 62f), new Vector2(760f, 70f), "결과", 38f, TextAlignmentOptions.Center, TextColor);
        AddText(root.transform, "Txt_Description", new Vector2(0f, -36f), new Vector2(780f, 110f), "결과 설명", 28f, TextAlignmentOptions.Center, TextColor);
        SavePrefab(root, "Assets/Prefabs/Panels/ResultPanel.prefab");
    }

    private static void BuildTitleScene()
    {
        SceneContext context = CreateBaseScene("TitleScene");
        AddImage(context.safeArea, "Img_Background", Vector2.zero, new Vector2(ReferenceWidth, ReferenceHeight), BackgroundColor);
        AddImage(context.safeArea, "Img_FarmIslandBackground", new Vector2(0f, 150f), new Vector2(960f, 760f), new Color(0.68f, 0.86f, 0.62f, 1f));

        RectTransform logo = AddGroup(context.safeArea, "Img_Logo", new Vector2(0f, 620f), new Vector2(760f, 220f));
        Image logoImage = logo.gameObject.AddComponent<Image>();
        logoImage.sprite = placeholderSprite;
        logoImage.color = new Color(1f, 0.95f, 0.72f, 1f);
        ImageSlot logoSlot = logo.gameObject.AddComponent<ImageSlot>();
        logoSlot.slotName = "Img_Logo";
        logoSlot.targetImage = logoImage;
        logoSlot.placeholderSprite = placeholderSprite;
        AddText(logo, "Txt_Title", Vector2.zero, new Vector2(720f, 160f), "일해라농장주", 70f, TextAlignmentOptions.Center, TextColor);

        RectTransform characters = AddGroup(context.safeArea, "Group_Characters", new Vector2(0f, 150f), new Vector2(900f, 420f));
        AddImage(characters, "Img_FarmerCharacter", new Vector2(0f, 0f), new Vector2(230f, 330f), new Color(0.92f, 0.72f, 0.42f, 1f));
        AddImage(characters, "Img_Cow", new Vector2(-290f, -70f), new Vector2(210f, 170f), new Color(0.95f, 0.95f, 0.90f, 1f));
        AddImage(characters, "Img_Pig", new Vector2(290f, -76f), new Vector2(200f, 150f), new Color(0.95f, 0.62f, 0.68f, 1f));

        RectTransform buttonArea = AddGroup(context.safeArea, "Group_ButtonArea", new Vector2(0f, -610f), new Vector2(880f, 420f));
        AddButton(buttonArea, "Btn_Start", new Vector2(0f, 120f), new Vector2(700f, 118f), "게임 시작", "Txt_ButtonLabel", true, ButtonColor);
        AddButton(buttonArea, "Btn_Continue", new Vector2(0f, -25f), new Vector2(700f, 108f), "이어하기", "Txt_ButtonLabel", false, SecondaryButtonColor);

        RectTransform utilities = AddGroup(buttonArea, "Group_UtilityButtons", new Vector2(0f, -165f), new Vector2(760f, 108f));
        AddButton(utilities, "Btn_Setting", new Vector2(-195f, 0f), new Vector2(340f, 88f), "설정", "Txt_Label", true, SecondaryButtonColor);
        AddButton(utilities, "Btn_Sync", new Vector2(195f, 0f), new Vector2(340f, 88f), "데이터 동기화", "Txt_Label", true, SecondaryButtonColor);

        AddController<TitleSceneController>(context.root);
        SaveScene(context, "Assets/Scenes/00_TitleScene.unity");
    }

    private static void BuildTutorialScene()
    {
        SceneContext context = CreateBaseScene("TutorialScene");
        AddImage(context.safeArea, "Img_Background", Vector2.zero, new Vector2(ReferenceWidth, ReferenceHeight), new Color(0.84f, 0.91f, 0.86f, 1f));

        RectTransform header = AddPanel(context.safeArea, "Panel_Header", new Vector2(0f, 760f), new Vector2(940f, 140f));
        AddText(header, "Txt_Title", Vector2.zero, new Vector2(780f, 90f), "게임 방법", 46f, TextAlignmentOptions.Center, TextColor);

        RectTransform cards = AddGroup(context.safeArea, "Group_TutorialCards", new Vector2(0f, 130f), new Vector2(920f, 1060f));
        AddTutorialCard(cards, "TutorialCard_01", new Vector2(0f, 335f), "1", "목표 설정", "공부, 독서, 운동 중 오늘의 목표를 선택하고 집중 시간을 정해요.");
        AddTutorialCard(cards, "TutorialCard_02", new Vector2(0f, 0f), "2", "스마트폰 내려놓기", "타이머가 시작되면 스마트폰을 내려놓고 목표에 집중해요.");
        AddTutorialCard(cards, "TutorialCard_03", new Vector2(0f, -335f), "3", "농장 성장", "집중이 끝나면 농작물이 자라고 골드와 보상을 획득해요.");

        RectTransform indicator = AddGroup(context.safeArea, "Group_PageIndicator", new Vector2(0f, -520f), new Vector2(360f, 80f));
        AddImage(indicator, "Img_Dot_01", new Vector2(-80f, 10f), new Vector2(28f, 28f), ButtonColor);
        AddImage(indicator, "Img_Dot_02", new Vector2(0f, 10f), new Vector2(28f, 28f), SecondaryButtonColor);
        AddImage(indicator, "Img_Dot_03", new Vector2(80f, 10f), new Vector2(28f, 28f), SecondaryButtonColor);
        AddText(indicator, "Txt_Page", new Vector2(0f, -28f), new Vector2(240f, 38f), "1 / 3", 24f, TextAlignmentOptions.Center, TextColor);

        AddButton(context.safeArea, "Btn_Next", new Vector2(0f, -735f), new Vector2(760f, 110f), "다음", "Txt_ButtonLabel", false, ButtonColor);
        AddController<TutorialSceneController>(context.root);
        SaveScene(context, "Assets/Scenes/01_TutorialScene.unity");
    }

    private static void BuildHomeScene()
    {
        SceneContext context = CreateBaseScene("HomeScene");
        AddImage(context.safeArea, "Img_Background", Vector2.zero, new Vector2(ReferenceWidth, ReferenceHeight), BackgroundColor);
        AddTopStatusBar(context.safeArea);

        RectTransform farm = AddGroup(context.safeArea, "Group_FarmArea", new Vector2(0f, 220f), new Vector2(1000f, 760f));
        AddImage(farm, "Img_FarmBackground", Vector2.zero, new Vector2(1000f, 720f), new Color(0.62f, 0.84f, 0.52f, 1f));
        AddImage(farm, "Img_Barn", new Vector2(-300f, 110f), new Vector2(220f, 200f), new Color(0.70f, 0.22f, 0.18f, 1f));
        AddImage(farm, "Img_Windmill", new Vector2(270f, 135f), new Vector2(190f, 270f), new Color(0.78f, 0.76f, 0.66f, 1f));
        AddImage(farm, "Img_House", new Vector2(0f, 170f), new Vector2(250f, 210f), new Color(0.82f, 0.55f, 0.35f, 1f));
        AddImage(farm, "Img_Field", new Vector2(0f, -160f), new Vector2(760f, 230f), new Color(0.55f, 0.42f, 0.22f, 1f));
        AddImage(farm, "Img_Cow", new Vector2(-260f, -160f), new Vector2(150f, 110f), new Color(0.95f, 0.95f, 0.90f, 1f));
        AddImage(farm, "Img_Pig", new Vector2(250f, -170f), new Vector2(140f, 100f), new Color(0.95f, 0.62f, 0.68f, 1f));
        AddImage(farm, "Img_Farmer", new Vector2(0f, -50f), new Vector2(120f, 190f), new Color(0.92f, 0.72f, 0.42f, 1f));
        AddImage(farm, "Img_Decorations", new Vector2(0f, -285f), new Vector2(820f, 70f), new Color(0.50f, 0.70f, 0.42f, 1f));

        RectTransform speech = AddPanel(context.safeArea, "Panel_SpeechBubble", new Vector2(0f, -205f), new Vector2(920f, 128f));
        AddImage(speech, "Img_FarmerFaceIcon", new Vector2(-390f, 0f), new Vector2(72f, 72f), new Color(0.92f, 0.72f, 0.42f, 1f));
        AddText(speech, "Txt_Message", new Vector2(50f, 0f), new Vector2(760f, 72f), "오늘도 좋은 하루! 집중으로 농장을 키우자!", 29f, TextAlignmentOptions.Left, TextColor);

        AddButton(context.safeArea, "Btn_FocusStart", new Vector2(0f, -365f), new Vector2(760f, 112f), "집중 시작", "Txt_ButtonLabel", true, ButtonColor);

        RectTransform recommended = AddPanel(context.safeArea, "Panel_RecommendedGoals", new Vector2(0f, -590f), new Vector2(980f, 330f));
        AddText(recommended, "Txt_Title", new Vector2(-260f, 105f), new Vector2(420f, 54f), "오늘의 추천 목표", 31f, TextAlignmentOptions.Left, TextColor);
        AddButton(recommended, "Btn_ChangeGoal", new Vector2(350f, 105f), new Vector2(220f, 62f), "목표 변경", "Txt_ButtonLabel", false, SecondaryButtonColor);
        AddGoalCard(recommended, "GoalCard_Study", new Vector2(-310f, -42f), new Vector2(285f, 210f), "공부하기", "90분", 120);
        AddGoalCard(recommended, "GoalCard_Read", new Vector2(0f, -42f), new Vector2(285f, 210f), "독서하기", "60분", 80);
        AddGoalCard(recommended, "GoalCard_Exercise", new Vector2(310f, -42f), new Vector2(285f, 210f), "운동하기", "45분", 60);

        AddBottomNavigationBar(context.safeArea);
        AddController<HomeSceneController>(context.root);
        SaveScene(context, "Assets/Scenes/02_HomeScene.unity");
    }

    private static void BuildGoalScene()
    {
        SceneContext context = CreateBaseScene("GoalScene");
        AddImage(context.safeArea, "Img_Background", Vector2.zero, new Vector2(ReferenceWidth, ReferenceHeight), new Color(0.87f, 0.92f, 0.82f, 1f));
        AddTopStatusBar(context.safeArea);

        RectTransform main = AddPanel(context.safeArea, "Panel_Main", new Vector2(0f, 20f), new Vector2(990f, 1390f));
        AddText(main, "Txt_Title", new Vector2(0f, 610f), new Vector2(860f, 70f), "목표와 집중 시간 설정", 42f, TextAlignmentOptions.Center, TextColor);

        RectTransform activityGroup = AddGroup(main, "Group_ActivityCards", new Vector2(0f, 300f), new Vector2(880f, 560f));
        AddGoalCard(activityGroup, "ActivityCard_Study", new Vector2(-230f, 145f), new Vector2(400f, 250f), "공부하기", "집중력 향상", 120);
        AddGoalCard(activityGroup, "ActivityCard_Read", new Vector2(230f, 145f), new Vector2(400f, 250f), "독서하기", "지식 성장", 80);
        AddGoalCard(activityGroup, "ActivityCard_Exercise", new Vector2(-230f, -145f), new Vector2(400f, 250f), "운동하기", "체력 단련", 60);
        AddGoalCard(activityGroup, "ActivityCard_Sleep", new Vector2(230f, -145f), new Vector2(400f, 250f), "수면 준비", "숙면 습관", 70);

        RectTransform timeButtons = AddGroup(main, "Group_TimeButtons", new Vector2(0f, -55f), new Vector2(900f, 105f));
        AddButton(timeButtons, "Btn_Time_10", new Vector2(-360f, 0f), new Vector2(160f, 76f), "10분", "Txt_ButtonLabel", false, SecondaryButtonColor);
        AddButton(timeButtons, "Btn_Time_20", new Vector2(-180f, 0f), new Vector2(160f, 76f), "20분", "Txt_ButtonLabel", false, SecondaryButtonColor);
        AddButton(timeButtons, "Btn_Time_30", Vector2.zero, new Vector2(160f, 76f), "30분", "Txt_ButtonLabel", false, ButtonColor);
        AddButton(timeButtons, "Btn_Time_60", new Vector2(180f, 0f), new Vector2(160f, 76f), "60분", "Txt_ButtonLabel", false, SecondaryButtonColor);
        AddButton(timeButtons, "Btn_Time_Custom", new Vector2(380f, 0f), new Vector2(190f, 76f), "직접 설정", "Txt_ButtonLabel", false, SecondaryButtonColor);

        RectTransform custom = AddPanel(main, "Panel_CustomTime", new Vector2(0f, -180f), new Vector2(580f, 100f));
        AddButton(custom, "Btn_Minus", new Vector2(-210f, 0f), new Vector2(92f, 72f), "-", "Txt_ButtonLabel", false, SecondaryButtonColor);
        AddText(custom, "Txt_TimeValue", Vector2.zero, new Vector2(220f, 70f), "30분", 36f, TextAlignmentOptions.Center, TextColor);
        AddButton(custom, "Btn_Plus", new Vector2(210f, 0f), new Vector2(92f, 72f), "+", "Txt_ButtonLabel", false, SecondaryButtonColor);

        RectTransform reward = AddPanel(main, "Panel_ExpectedReward", new Vector2(-245f, -325f), new Vector2(430f, 128f));
        AddImage(reward, "Img_Icon", new Vector2(-160f, 0f), new Vector2(66f, 66f), new Color(0.90f, 0.68f, 0.20f, 1f));
        AddText(reward, "Txt_Label", new Vector2(40f, 24f), new Vector2(280f, 36f), "예상 보상", 24f, TextAlignmentOptions.Left, TextColor);
        AddText(reward, "Txt_Value", new Vector2(40f, -20f), new Vector2(280f, 42f), "+120 골드", 28f, TextAlignmentOptions.Left, TextColor);

        RectTransform growth = AddPanel(main, "Panel_ExpectedGrowth", new Vector2(245f, -325f), new Vector2(430f, 128f));
        AddImage(growth, "Img_Icon", new Vector2(-160f, 0f), new Vector2(66f, 66f), new Color(0.42f, 0.78f, 0.36f, 1f));
        AddText(growth, "Txt_Label", new Vector2(40f, 24f), new Vector2(280f, 36f), "예상 성장", 24f, TextAlignmentOptions.Left, TextColor);
        AddText(growth, "Txt_Value", new Vector2(40f, -20f), new Vector2(280f, 42f), "30분 집중 성장", 28f, TextAlignmentOptions.Left, TextColor);

        AddButton(main, "Btn_StartSession", new Vector2(0f, -520f), new Vector2(760f, 110f), "집중 세션 시작", "Txt_ButtonLabel", true, ButtonColor);

        AddBottomNavigationBar(context.safeArea);
        AddController<GoalSceneController>(context.root);
        SaveScene(context, "Assets/Scenes/03_GoalScene.unity");
    }

    private static void BuildFocusScene()
    {
        SceneContext context = CreateBaseScene("FocusScene");
        AddImage(context.safeArea, "Img_Background", Vector2.zero, new Vector2(ReferenceWidth, ReferenceHeight), new Color(0.80f, 0.90f, 0.84f, 1f));
        AddTopStatusBar(context.safeArea);

        RectTransform farm = AddGroup(context.safeArea, "Group_ActiveFarmArea", new Vector2(0f, 420f), new Vector2(980f, 420f));
        AddImage(farm, "Img_FarmBackground", Vector2.zero, new Vector2(980f, 390f), new Color(0.58f, 0.80f, 0.48f, 1f));
        AddImage(farm, "Img_FarmerWorking", new Vector2(-180f, -40f), new Vector2(160f, 220f), new Color(0.92f, 0.72f, 0.42f, 1f));
        AddImage(farm, "Img_Worker", new Vector2(120f, -40f), new Vector2(130f, 200f), new Color(0.70f, 0.60f, 0.45f, 1f));
        AddImage(farm, "Img_GrowingCrops", new Vector2(0f, -160f), new Vector2(620f, 110f), new Color(0.40f, 0.70f, 0.28f, 1f));
        AddImage(farm, "Img_Animals", new Vector2(330f, -110f), new Vector2(180f, 120f), new Color(0.95f, 0.86f, 0.76f, 1f));
        AddImage(farm, "Img_Decorations", new Vector2(-320f, -150f), new Vector2(210f, 80f), new Color(0.48f, 0.64f, 0.36f, 1f));

        RectTransform current = AddPanel(context.safeArea, "Panel_CurrentGoal", new Vector2(0f, 145f), new Vector2(900f, 120f));
        AddImage(current, "Img_GoalIcon", new Vector2(-380f, 0f), new Vector2(70f, 70f), new Color(0.55f, 0.72f, 0.92f, 1f));
        AddText(current, "Txt_Label", new Vector2(-190f, 22f), new Vector2(280f, 36f), "현재 목표", 24f, TextAlignmentOptions.Left, TextColor);
        AddText(current, "Txt_GoalTitle", new Vector2(110f, -18f), new Vector2(600f, 50f), "공부하기 30분", 34f, TextAlignmentOptions.Left, TextColor);

        RectTransform timer = AddGroup(context.safeArea, "Group_TimerArea", new Vector2(0f, -155f), new Vector2(660f, 560f));
        AddImage(timer, "Img_ProgressRingBackground", Vector2.zero, new Vector2(440f, 440f), new Color(0.90f, 0.92f, 0.86f, 1f));
        Image fill = AddImage(timer, "Img_ProgressRingFill", Vector2.zero, new Vector2(440f, 440f), new Color(0.34f, 0.72f, 0.38f, 1f));
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Radial360;
        fill.fillAmount = 1f;
        AddImage(timer, "Img_SproutIcon", new Vector2(0f, 84f), new Vector2(82f, 82f), new Color(0.42f, 0.78f, 0.36f, 1f));
        AddText(timer, "Txt_TimerLabel", new Vector2(0f, 18f), new Vector2(260f, 46f), "집중 시간", 28f, TextAlignmentOptions.Center, TextColor);
        AddText(timer, "Txt_Timer", new Vector2(0f, -54f), new Vector2(360f, 100f), "24:18", 76f, TextAlignmentOptions.Center, TextColor);
        AddText(timer, "Txt_EndTime", new Vector2(0f, -140f), new Vector2(420f, 44f), "12:30 종료 예정", 25f, TextAlignmentOptions.Center, new Color(0.40f, 0.44f, 0.40f, 1f));

        AddButton(context.safeArea, "Btn_Pause", new Vector2(0f, -475f), new Vector2(360f, 82f), "일시정지", "Txt_ButtonLabel", false, SecondaryButtonColor);

        RectTransform message = AddPanel(context.safeArea, "Panel_Message", new Vector2(0f, -615f), new Vector2(920f, 160f));
        AddImage(message, "Img_FarmerFaceIcon", new Vector2(-390f, 18f), new Vector2(72f, 72f), new Color(0.92f, 0.72f, 0.42f, 1f));
        AddText(message, "Txt_MainMessage", new Vector2(70f, 35f), new Vector2(760f, 50f), "지금은 집중 시간! 스마트폰을 내려놓으면 농장이 성장해요", 27f, TextAlignmentOptions.Left, TextColor);
        AddText(message, "Txt_WarningMessage", new Vector2(70f, -35f), new Vector2(760f, 46f), "앱을 떠나면 세션이 실패해요", 25f, TextAlignmentOptions.Left, new Color(0.72f, 0.22f, 0.20f, 1f));

        AddButton(context.safeArea, "Btn_GiveUp", new Vector2(-230f, -785f), new Vector2(360f, 88f), "포기하기", "Txt_ButtonLabel", false, new Color(0.72f, 0.28f, 0.26f, 1f));
        AddButton(context.safeArea, "Btn_NatureSound", new Vector2(230f, -785f), new Vector2(360f, 88f), "자연의 소리", "Txt_ButtonLabel", false, SecondaryButtonColor);

        GameObject controller = AddController<FocusSessionManager>(context.root);
        controller.AddComponent<AppPauseDetector>();
        SaveScene(context, "Assets/Scenes/04_FocusScene.unity");
    }

    private static void BuildSuccessScene()
    {
        SceneContext context = CreateBaseScene("SuccessScene");
        AddImage(context.safeArea, "Img_Background", Vector2.zero, new Vector2(ReferenceWidth, ReferenceHeight), new Color(0.86f, 0.94f, 0.80f, 1f));

        RectTransform farm = AddGroup(context.safeArea, "Group_SuccessFarmArea", new Vector2(0f, 520f), new Vector2(980f, 360f));
        AddImage(farm, "Img_FarmBackground", Vector2.zero, new Vector2(980f, 320f), new Color(0.58f, 0.84f, 0.50f, 1f));
        AddImage(farm, "Img_Confetti", new Vector2(0f, 100f), new Vector2(860f, 160f), new Color(1f, 0.82f, 0.35f, 0.72f));
        AddImage(farm, "Img_HappyFarmer", new Vector2(0f, -40f), new Vector2(170f, 230f), new Color(0.92f, 0.72f, 0.42f, 1f));

        RectTransform header = AddPanel(context.safeArea, "Panel_Header", new Vector2(0f, 245f), new Vector2(880f, 120f));
        AddText(header, "Txt_Title", Vector2.zero, new Vector2(780f, 70f), "집중 성공!", 46f, TextAlignmentOptions.Center, TextColor);

        RectTransform message = AddPanel(context.safeArea, "Panel_Message", new Vector2(0f, 105f), new Vector2(900f, 120f));
        AddText(message, "Txt_Message", Vector2.zero, new Vector2(780f, 68f), "정말 잘했어요! 농장이 더욱 풍성해졌어요!", 30f, TextAlignmentOptions.Center, TextColor);

        AddRewardPanel(context.safeArea, "Panel_GoldReward", new Vector2(0f, -70f), "Img_CoinIcon", "획득 골드", "+120 골드", "");
        AddRewardPanel(context.safeArea, "Panel_UnlockReward", new Vector2(0f, -290f), "Img_ItemIcon", "새로 해금된 요소", "닭장", "농장에 새 배치 요소가 열렸어요.");
        AddRewardPanel(context.safeArea, "Panel_RandomReward", new Vector2(0f, -510f), "Img_ItemIcon", "랜덤 발견 보상", "물뿌리개(중)", "다음 집중에 도움을 주는 아이템이에요.");
        AddButton(context.safeArea, "Btn_BackToFarm", new Vector2(0f, -775f), new Vector2(760f, 110f), "농장으로 돌아가기", "Txt_ButtonLabel", false, ButtonColor);

        AddController<SuccessSceneController>(context.root);
        SaveScene(context, "Assets/Scenes/05_SuccessScene.unity");
    }

    private static void BuildFailScene()
    {
        SceneContext context = CreateBaseScene("FailScene");
        AddImage(context.safeArea, "Img_Background", Vector2.zero, new Vector2(ReferenceWidth, ReferenceHeight), new Color(0.43f, 0.47f, 0.48f, 1f));

        RectTransform failedFarm = AddGroup(context.safeArea, "Group_FailedFarmArea", new Vector2(0f, 500f), new Vector2(980f, 360f));
        AddImage(failedFarm, "Img_DarkSky", Vector2.zero, new Vector2(980f, 320f), new Color(0.28f, 0.32f, 0.36f, 1f));
        AddImage(failedFarm, "Img_WiltedCrops", new Vector2(-220f, -100f), new Vector2(280f, 110f), new Color(0.35f, 0.28f, 0.16f, 1f));
        AddImage(failedFarm, "Img_SadFarmer", Vector2.zero, new Vector2(170f, 230f), new Color(0.62f, 0.48f, 0.34f, 1f));
        AddImage(failedFarm, "Img_StoppedWindmill", new Vector2(280f, -20f), new Vector2(190f, 240f), new Color(0.46f, 0.46f, 0.42f, 1f));

        RectTransform header = AddPanel(context.safeArea, "Panel_Header", new Vector2(0f, 240f), new Vector2(880f, 120f));
        AddText(header, "Txt_Title", Vector2.zero, new Vector2(760f, 70f), "집중 실패", 46f, TextAlignmentOptions.Center, new Color(0.72f, 0.22f, 0.20f, 1f));

        RectTransform reason = AddPanel(context.safeArea, "Panel_Reason", new Vector2(0f, 80f), new Vector2(900f, 150f));
        AddImage(reason, "Img_PhoneIcon", new Vector2(-360f, 0f), new Vector2(70f, 100f), new Color(0.24f, 0.28f, 0.30f, 1f));
        AddImage(reason, "Img_XIcon", new Vector2(-288f, 0f), new Vector2(54f, 54f), new Color(0.82f, 0.22f, 0.20f, 1f));
        AddText(reason, "Txt_Reason", new Vector2(90f, 0f), new Vector2(700f, 90f), "앱을 벗어났거나 다른 앱을 사용해서 세션이 중단되었어요.", 29f, TextAlignmentOptions.Left, TextColor);

        RectTransform result = AddPanel(context.safeArea, "Panel_Result", new Vector2(0f, -115f), new Vector2(900f, 145f));
        AddImage(result, "Img_DisabledCoinIcon", new Vector2(-360f, 0f), new Vector2(76f, 76f), new Color(0.48f, 0.48f, 0.44f, 1f));
        AddText(result, "Txt_Label", new Vector2(45f, 26f), new Vector2(690f, 42f), "획득 보상 없음", 31f, TextAlignmentOptions.Left, TextColor);
        AddText(result, "Txt_Description", new Vector2(45f, -30f), new Vector2(690f, 52f), "집중을 끝까지 유지하면 보상을 받을 수 있어요!", 27f, TextAlignmentOptions.Left, TextColor);

        RectTransform encourage = AddPanel(context.safeArea, "Panel_Encourage", new Vector2(0f, -300f), new Vector2(900f, 145f));
        AddImage(encourage, "Img_FarmerFaceIcon", new Vector2(-360f, 0f), new Vector2(76f, 76f), new Color(0.62f, 0.48f, 0.34f, 1f));
        AddText(encourage, "Txt_Message", new Vector2(65f, 0f), new Vector2(700f, 86f), "괜찮아요! 작은 집중이 큰 변화를 만들어요.\n다시 도전해볼까요?", 28f, TextAlignmentOptions.Left, TextColor);

        AddButton(context.safeArea, "Btn_Restart", new Vector2(-230f, -500f), new Vector2(360f, 96f), "다시 시작", "Txt_ButtonLabel", false, ButtonColor);
        AddButton(context.safeArea, "Btn_Home", new Vector2(230f, -500f), new Vector2(360f, 96f), "홈으로", "Txt_ButtonLabel", false, SecondaryButtonColor);

        RectTransform tip = AddPanel(context.safeArea, "Panel_Tip", new Vector2(0f, -670f), new Vector2(900f, 120f));
        AddText(tip, "Txt_Tip", Vector2.zero, new Vector2(780f, 78f), "팁: 방해 요소를 줄이고, 목표를 떠올리면 더 집중할 수 있어요!", 27f, TextAlignmentOptions.Center, TextColor);

        AddController<FailSceneController>(context.root);
        SaveScene(context, "Assets/Scenes/06_FailScene.unity");
    }

    private static void BuildShopScene()
    {
        SceneContext context = CreateBaseScene("ShopScene");
        AddImage(context.safeArea, "Img_Background", Vector2.zero, new Vector2(ReferenceWidth, ReferenceHeight), new Color(0.84f, 0.90f, 0.82f, 1f));
        AddTopStatusBar(context.safeArea);

        RectTransform preview = AddGroup(context.safeArea, "Group_FarmPreviewArea", new Vector2(0f, 390f), new Vector2(980f, 420f));
        AddImage(preview, "Img_FarmBackground", Vector2.zero, new Vector2(980f, 390f), new Color(0.60f, 0.82f, 0.52f, 1f));
        AddImage(preview, "Img_PlacementGrid", Vector2.zero, new Vector2(820f, 300f), new Color(1f, 1f, 1f, 0.20f));
        AddImage(preview, "Img_SelectedObjectPreview", new Vector2(0f, -30f), new Vector2(160f, 160f), new Color(0.82f, 0.56f, 0.32f, 1f));
        AddImage(preview, "Img_GlowOutline", new Vector2(0f, -30f), new Vector2(200f, 200f), new Color(1f, 0.90f, 0.24f, 0.45f));

        RectTransform header = AddPanel(context.safeArea, "Panel_Header", new Vector2(0f, 120f), new Vector2(900f, 90f));
        AddText(header, "Txt_Title", Vector2.zero, new Vector2(780f, 58f), "상점", 40f, TextAlignmentOptions.Center, TextColor);

        RectTransform guide = AddPanel(context.safeArea, "Panel_GuideBubble", new Vector2(0f, 10f), new Vector2(900f, 95f));
        AddImage(guide, "Img_FarmerFaceIcon", new Vector2(-380f, 0f), new Vector2(58f, 58f), new Color(0.92f, 0.72f, 0.42f, 1f));
        AddText(guide, "Txt_Message", new Vector2(40f, 0f), new Vector2(760f, 54f), "원하는 위치에 배치해 보세요!", 28f, TextAlignmentOptions.Left, TextColor);

        RectTransform buttons = AddGroup(context.safeArea, "Group_PlacementButtons", new Vector2(0f, -105f), new Vector2(900f, 88f));
        AddButton(buttons, "Btn_Cancel", new Vector2(-300f, 0f), new Vector2(240f, 74f), "취소", "Txt_ButtonLabel", false, SecondaryButtonColor);
        AddButton(buttons, "Btn_Buy", Vector2.zero, new Vector2(240f, 74f), "구매", "Txt_ButtonLabel", false, ButtonColor);
        AddButton(buttons, "Btn_Place", new Vector2(300f, 0f), new Vector2(240f, 74f), "배치", "Txt_ButtonLabel", false, ButtonColor);

        RectTransform shop = AddPanel(context.safeArea, "Panel_Shop", new Vector2(0f, -425f), new Vector2(980f, 520f));
        RectTransform tabs = AddGroup(shop, "Group_CategoryTabs", new Vector2(0f, 190f), new Vector2(900f, 82f));
        AddButton(tabs, "Btn_Tab_Building", new Vector2(-300f, 0f), new Vector2(260f, 66f), "건물", "Txt_ButtonLabel", false, ButtonColor);
        AddButton(tabs, "Btn_Tab_Animal", Vector2.zero, new Vector2(260f, 66f), "동물", "Txt_ButtonLabel", false, SecondaryButtonColor);
        AddButton(tabs, "Btn_Tab_Decoration", new Vector2(300f, 0f), new Vector2(260f, 66f), "장식", "Txt_ButtonLabel", false, SecondaryButtonColor);

        RectTransform grid = AddGroup(shop, "Group_ItemGrid", new Vector2(0f, -55f), new Vector2(900f, 350f));
        for (int i = 0; i < 8; i++)
        {
            float x = -330f + (i % 4) * 220f;
            float y = 86f - (i / 4) * 175f;
            AddShopItemCard(grid, $"ShopItemCard_{i + 1:00}", new Vector2(x, y), new Vector2(200f, 160f));
        }

        AddBottomNavigationBar(context.safeArea);
        AddController<ShopSceneController>(context.root);
        AddController<ShopManager>(context.root);
        SaveScene(context, "Assets/Scenes/07_ShopScene.unity");
    }

    private static void BuildCollectionScene()
    {
        SceneContext context = CreateBaseScene("CollectionScene");
        AddImage(context.safeArea, "Img_Background", Vector2.zero, new Vector2(ReferenceWidth, ReferenceHeight), new Color(0.86f, 0.91f, 0.84f, 1f));

        RectTransform header = AddPanel(context.safeArea, "Panel_Header", new Vector2(0f, 770f), new Vector2(940f, 120f));
        AddText(header, "Txt_Title", Vector2.zero, new Vector2(780f, 70f), "도감", 46f, TextAlignmentOptions.Center, TextColor);

        RectTransform progress = AddPanel(context.safeArea, "Panel_CollectionProgress", new Vector2(0f, 610f), new Vector2(940f, 160f));
        AddText(progress, "Txt_Label", new Vector2(-300f, 42f), new Vector2(260f, 40f), "수집률", 28f, TextAlignmentOptions.Left, TextColor);
        AddText(progress, "Txt_Percent", new Vector2(290f, 42f), new Vector2(260f, 40f), "42%", 28f, TextAlignmentOptions.Right, TextColor);
        AddImage(progress, "Img_ProgressBarBackground", new Vector2(0f, -8f), new Vector2(760f, 28f), SecondaryButtonColor);
        Image fill = AddImage(progress, "Img_ProgressBarFill", new Vector2(-220f, -8f), new Vector2(320f, 28f), ButtonColor);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillAmount = 0.42f;
        AddImage(progress, "Img_Chest_25", new Vector2(-260f, -55f), new Vector2(46f, 46f), new Color(0.77f, 0.54f, 0.27f, 1f));
        AddImage(progress, "Img_Chest_50", new Vector2(-80f, -55f), new Vector2(46f, 46f), new Color(0.77f, 0.54f, 0.27f, 1f));
        AddImage(progress, "Img_Chest_75", new Vector2(100f, -55f), new Vector2(46f, 46f), new Color(0.77f, 0.54f, 0.27f, 1f));
        AddImage(progress, "Img_Chest_100", new Vector2(280f, -55f), new Vector2(46f, 46f), new Color(0.77f, 0.54f, 0.27f, 1f));

        RectTransform tabs = AddGroup(context.safeArea, "Group_CategoryTabs", new Vector2(0f, 470f), new Vector2(960f, 86f));
        AddButton(tabs, "Btn_Tab_Building", new Vector2(-360f, 0f), new Vector2(205f, 68f), "건물", "Txt_ButtonLabel", false, ButtonColor);
        AddButton(tabs, "Btn_Tab_Animal", new Vector2(-120f, 0f), new Vector2(205f, 68f), "동물", "Txt_ButtonLabel", false, SecondaryButtonColor);
        AddButton(tabs, "Btn_Tab_Crop", new Vector2(120f, 0f), new Vector2(205f, 68f), "작물", "Txt_ButtonLabel", false, SecondaryButtonColor);
        AddButton(tabs, "Btn_Tab_Decoration", new Vector2(360f, 0f), new Vector2(205f, 68f), "장식", "Txt_ButtonLabel", false, SecondaryButtonColor);

        RectTransform scroll = AddScrollView(context.safeArea, "ScrollView_Collection", new Vector2(0f, -80f), new Vector2(960f, 980f));
        RectTransform content = AddGroup(scroll, "Content", new Vector2(0f, 0f), new Vector2(900f, 1900f));
        content.pivot = new Vector2(0.5f, 1f);
        content.anchorMin = new Vector2(0.5f, 1f);
        content.anchorMax = new Vector2(0.5f, 1f);
        content.anchoredPosition = new Vector2(0f, -20f);
        scroll.GetComponent<ScrollRect>().content = content;

        AddCollectionSection(content, "Section_Building", new Vector2(0f, -90f), new[] { "농장 집", "헛간", "풍차", "우물", "치즈 공방 잠금", "가공소 잠금", "온실 잠금", "사료 창고 잠금" });
        AddCollectionSection(content, "Section_Animal", new Vector2(0f, -550f), new[] { "젖소", "돼지", "닭", "양", "잠금 항목", "잠금 항목", "잠금 항목", "잠금 항목" });
        AddCollectionSection(content, "Section_Crop", new Vector2(0f, -1010f), new[] { "당근", "토마토", "밀", "양배추", "딸기", "잠금 항목", "잠금 항목", "잠금 항목" });
        AddCollectionSection(content, "Section_Decoration", new Vector2(0f, -1470f), new[] { "나무 울타리", "우편함", "꽃밭", "잠금 항목", "잠금 항목", "잠금 항목", "잠금 항목", "잠금 항목" });

        AddBottomNavigationBar(context.safeArea);
        AddController<CollectionSceneController>(context.root);
        SaveScene(context, "Assets/Scenes/08_CollectionScene.unity");
    }

    private static void BuildRecordScene()
    {
        SceneContext context = CreateBaseScene("RecordScene");
        AddImage(context.safeArea, "Img_Background", Vector2.zero, new Vector2(ReferenceWidth, ReferenceHeight), new Color(0.86f, 0.90f, 0.86f, 1f));

        RectTransform header = AddPanel(context.safeArea, "Panel_Header", new Vector2(0f, 770f), new Vector2(940f, 110f));
        AddText(header, "Txt_Title", Vector2.zero, new Vector2(800f, 68f), "기록", 46f, TextAlignmentOptions.Center, TextColor);

        RectTransform summary = AddGroup(context.safeArea, "Group_SummaryStats", new Vector2(0f, 600f), new Vector2(960f, 190f));
        AddStatPanel(summary, "Panel_TodayFocus", "Img_ClockIcon", "오늘 집중 시간", "2시간 35분", StatPanelKind.TodayFocus, new Vector2(-255f, 48f), new Vector2(450f, 82f));
        AddStatPanel(summary, "Panel_WeeklyFocus", "Img_ClockIcon", "이번 주 누적 시간", "14시간 20분", StatPanelKind.WeeklyFocus, new Vector2(255f, 48f), new Vector2(450f, 82f));
        AddStatPanel(summary, "Panel_Streak", "Img_TrophyIcon", "연속 성공", "7일", StatPanelKind.Streak, new Vector2(-255f, -52f), new Vector2(450f, 82f));
        AddStatPanel(summary, "Panel_TotalGold", "Img_CoinIcon", "총 획득 골드", "12,345", StatPanelKind.TotalGold, new Vector2(255f, -52f), new Vector2(450f, 82f));

        RectTransform chart = AddPanel(context.safeArea, "Panel_WeeklyChart", new Vector2(0f, 250f), new Vector2(940f, 410f));
        AddText(chart, "Txt_Title", new Vector2(-285f, 155f), new Vector2(330f, 45f), "주간 집중 그래프", 30f, TextAlignmentOptions.Left, TextColor);
        AddText(chart, "Txt_TotalTime", new Vector2(290f, 155f), new Vector2(350f, 45f), "이번 주 14시간 20분", 26f, TextAlignmentOptions.Right, TextColor);
        RectTransform bars = AddGroup(chart, "Group_Bars", new Vector2(0f, -10f), new Vector2(820f, 270f));
        string[] barNames = { "Img_Bar_Mon", "Img_Bar_Tue", "Img_Bar_Wed", "Img_Bar_Thu", "Img_Bar_Fri", "Img_Bar_Sat", "Img_Bar_Sun" };
        string[] barLabels = { "월\n1h 30m", "화\n2h 10m", "수\n3h 05m", "목\n2h 45m", "금\n2h 20m", "토\n1h 50m", "일\n1h 20m" };
        for (int i = 0; i < barNames.Length; i++)
        {
            float x = -360f + i * 120f;
            Image bar = AddImage(bars, barNames[i], new Vector2(x, -30f), new Vector2(56f, 120f + i * 16f), ButtonColor);
            bar.rectTransform.pivot = new Vector2(0.5f, 0f);
            AddText(bars, $"Txt_BarLabel_{i + 1}", new Vector2(x, -145f), new Vector2(96f, 58f), barLabels[i], 18f, TextAlignmentOptions.Center, TextColor);
        }

        RectTransform comment = AddPanel(chart, "Panel_Comment", new Vector2(0f, -160f), new Vector2(840f, 78f));
        AddText(comment, "Txt_Comment", Vector2.zero, new Vector2(760f, 54f), "수요일이 가장 집중력이 좋았어요!\n좋은 페이스예요! 계속해봐요!", 23f, TextAlignmentOptions.Center, TextColor);

        RectTransform calendar = AddPanel(context.safeArea, "Panel_StreakCalendar", new Vector2(0f, -115f), new Vector2(940f, 240f));
        AddText(calendar, "Txt_Month", new Vector2(-300f, 82f), new Vector2(260f, 42f), "5월", 30f, TextAlignmentOptions.Left, TextColor);
        RectTransform days = AddGroup(calendar, "Group_DayCells", new Vector2(0f, -25f), new Vector2(820f, 130f));
        for (int i = 0; i < 21; i++)
        {
            float x = -360f + (i % 7) * 120f;
            float y = 44f - (i / 7) * 44f;
            AddImage(days, $"Img_DayCell_{i + 1:00}", new Vector2(x, y), new Vector2(36f, 36f), i < 7 ? ButtonColor : SecondaryButtonColor);
        }

        RectTransform achievements = AddPanel(context.safeArea, "Panel_Achievements", new Vector2(0f, -390f), new Vector2(940f, 280f));
        for (int i = 0; i < 5; i++)
        {
            AddAchievementCard(achievements, $"AchievementCard_{i + 1:00}", new Vector2(-360f + i * 180f, 0f), new Vector2(160f, 210f));
        }

        RectTransform growth = AddPanel(context.safeArea, "Panel_GrowthRecord", new Vector2(0f, -650f), new Vector2(940f, 180f));
        AddStatPanel(growth, "Panel_FarmLevel", "Img_Icon", "농장 레벨", "Lv.12", StatPanelKind.FarmLevel, new Vector2(-300f, 0f), new Vector2(270f, 112f));
        AddStatPanel(growth, "Panel_UnlockedItems", "Img_Icon", "해제한 아이템", "24/78", StatPanelKind.UnlockedItems, Vector2.zero, new Vector2(270f, 112f));
        AddStatPanel(growth, "Panel_BestSession", "Img_Icon", "최고 집중 세션", "3시간 05분", StatPanelKind.BestSession, new Vector2(300f, 0f), new Vector2(270f, 112f));

        AddBottomNavigationBar(context.safeArea);
        AddController<RecordSceneController>(context.root);
        SaveScene(context, "Assets/Scenes/09_RecordScene.unity");
    }

    private static SceneContext CreateBaseScene(string rootName)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new GameObject(rootName);
        root.AddComponent<SceneLoader>();
        root.AddComponent<UIManager>();

        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(root.transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = false;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform safeArea = AddGroup(canvasObject.transform, "SafeArea", Vector2.zero, new Vector2(ReferenceWidth, ReferenceHeight));
        safeArea.anchorMin = Vector2.zero;
        safeArea.anchorMax = Vector2.one;
        safeArea.pivot = new Vector2(0.5f, 0.5f);
        safeArea.anchoredPosition = Vector2.zero;
        safeArea.sizeDelta = Vector2.zero;

        CreateEventSystem(root.transform);

        return new SceneContext
        {
            scene = scene,
            root = root,
            safeArea = safeArea
        };
    }

    private static void CreateEventSystem(Transform parent)
    {
        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
        eventSystem.transform.SetParent(parent, false);

        Type inputSystemModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputSystemModuleType != null)
        {
            eventSystem.AddComponent(inputSystemModuleType);
        }
        else
        {
            eventSystem.AddComponent<StandaloneInputModule>();
        }
    }

    private static GameObject AddController<T>(GameObject sceneRoot) where T : Component
    {
        GameObject controller = new GameObject(typeof(T).Name);
        controller.transform.SetParent(sceneRoot.transform, false);
        controller.AddComponent<T>();
        return controller;
    }

    private static RectTransform AddGroup(Transform parent, string name, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject group = new GameObject(name, typeof(RectTransform));
        RectTransform rect = group.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return rect;
    }

    private static Image AddImage(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        RectTransform rect = AddGroup(parent, name, anchoredPosition, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = placeholderSprite;
        image.color = color;
        image.raycastTarget = false;

        if (name.StartsWith("Img_", StringComparison.Ordinal))
        {
            ImageSlot slot = rect.gameObject.AddComponent<ImageSlot>();
            slot.slotName = name;
            slot.targetImage = image;
            slot.placeholderSprite = placeholderSprite;
        }

        return image;
    }

    private static TextMeshProUGUI AddText(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, string text, float fontSize, TextAlignmentOptions alignment, Color color)
    {
        RectTransform rect = AddGroup(parent, name, anchoredPosition, size);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        if (defaultFontAsset != null)
        {
            label.font = defaultFontAsset;
        }

        label.fontSize = fontSize;
        label.enableAutoSizing = true;
        label.fontSizeMin = Mathf.Max(12f, fontSize * 0.55f);
        label.fontSizeMax = fontSize;
        label.color = color;
        label.alignment = alignment;
        label.raycastTarget = false;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.characterSpacing = 0f;
        return label;
    }

    private static RectTransform AddPanel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size)
    {
        RectTransform panel = AddGroup(parent, name, anchoredPosition, size);
        Image skin = AddImage(panel, "Img_PanelSkin", Vector2.zero, size, PanelColor);
        skin.rectTransform.anchorMin = Vector2.zero;
        skin.rectTransform.anchorMax = Vector2.one;
        skin.rectTransform.sizeDelta = Vector2.zero;
        skin.rectTransform.anchoredPosition = Vector2.zero;
        return panel;
    }

    private static GameObject AddButton(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, string label, string labelObjectName, bool withIcon, Color skinColor)
    {
        RectTransform rect = AddGroup(parent, name, anchoredPosition, size);
        Image raycastImage = rect.gameObject.AddComponent<Image>();
        raycastImage.color = new Color(1f, 1f, 1f, 0f);
        raycastImage.raycastTarget = true;

        Button button = rect.gameObject.AddComponent<Button>();
        Image skin = AddImage(rect, "Img_ButtonSkin", Vector2.zero, size, skinColor);
        skin.rectTransform.anchorMin = Vector2.zero;
        skin.rectTransform.anchorMax = Vector2.one;
        skin.rectTransform.sizeDelta = Vector2.zero;
        button.targetGraphic = skin;

        float textOffset = withIcon ? 42f : 0f;
        float textWidth = withIcon ? size.x - 150f : size.x - 48f;
        if (withIcon)
        {
            AddImage(rect, "Img_Icon", new Vector2(-size.x * 0.36f, 0f), new Vector2(Mathf.Min(58f, size.y * 0.56f), Mathf.Min(58f, size.y * 0.56f)), new Color(1f, 1f, 1f, 0.75f));
        }

        AddText(rect, labelObjectName, new Vector2(textOffset, 0f), new Vector2(textWidth, size.y * 0.72f), label, Mathf.Clamp(size.y * 0.34f, 20f, 36f), TextAlignmentOptions.Center, Color.white);
        return rect.gameObject;
    }

    private static void AddStatPanel(Transform parent, string panelName, string iconName, string label, string value, StatPanelKind kind, Vector2 anchoredPosition)
    {
        AddStatPanel(parent, panelName, iconName, label, value, kind, anchoredPosition, new Vector2(310f, 106f));
    }

    private static void AddStatPanel(Transform parent, string panelName, string iconName, string label, string value, StatPanelKind kind, Vector2 anchoredPosition, Vector2 size)
    {
        RectTransform panel = AddGroup(parent, panelName, anchoredPosition, size);
        AddStatPanelContent(panel, iconName, label, value);
        StatPanelUI stat = panel.gameObject.AddComponent<StatPanelUI>();
        stat.SetKind(kind);
    }

    private static void AddStatPanelContent(Transform parent, string iconName, string label, string value)
    {
        Vector2 size = parent.GetComponent<RectTransform>().sizeDelta;
        AddImage(parent, "Img_PanelSkin", Vector2.zero, size, PanelColor);
        AddImage(parent, iconName, new Vector2(-size.x * 0.36f, 0f), new Vector2(52f, 52f), new Color(0.86f, 0.66f, 0.22f, 1f));
        AddText(parent, "Txt_Label", new Vector2(36f, 18f), new Vector2(size.x * 0.64f, 32f), label, 21f, TextAlignmentOptions.Left, TextColor);
        AddText(parent, "Txt_Value", new Vector2(36f, -18f), new Vector2(size.x * 0.64f, 36f), value, 25f, TextAlignmentOptions.Left, TextColor);
    }

    private static void AddTopStatusBar(Transform parent)
    {
        GameObject status = InstantiatePrefab("Assets/Prefabs/Common/TopStatusBar.prefab", parent, "TopStatusBar");
        RectTransform rect = status.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -24f);
        rect.sizeDelta = new Vector2(1000f, 128f);
    }

    private static void AddBottomNavigationBar(Transform parent)
    {
        GameObject nav = InstantiatePrefab("Assets/Prefabs/Common/BottomNavigationBar.prefab", parent, "BottomNavigationBar");
        RectTransform rect = nav.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(1080f, 168f);
    }

    private static GoalCardUI AddGoalCard(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, string title, string description, int reward)
    {
        GameObject card = InstantiatePrefab("Assets/Prefabs/Cards/GoalCard.prefab", parent, name);
        RectTransform rect = card.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        ScaleCardChildren(rect, size);
        GoalCardUI ui = card.GetComponent<GoalCardUI>();
        ui.SetData(title, description, reward);
        ui.SetSelected(false);
        return ui;
    }

    private static ShopItemCardUI AddShopItemCard(Transform parent, string name, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject card = InstantiatePrefab("Assets/Prefabs/Cards/ShopItemCard.prefab", parent, name);
        RectTransform rect = card.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        ScaleCardChildren(rect, size);
        return card.GetComponent<ShopItemCardUI>();
    }

    private static CollectionItemCardUI AddCollectionItemCard(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, string itemName, bool unlocked)
    {
        GameObject card = InstantiatePrefab("Assets/Prefabs/Cards/CollectionItemCard.prefab", parent, name);
        RectTransform rect = card.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        ScaleCardChildren(rect, size);
        CollectionItemCardUI ui = card.GetComponent<CollectionItemCardUI>();
        ui.SetData(itemName, unlocked, unlocked ? 3 : 0);
        return ui;
    }

    private static AchievementCardUI AddAchievementCard(Transform parent, string name, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject card = InstantiatePrefab("Assets/Prefabs/Cards/AchievementCard.prefab", parent, name);
        RectTransform rect = card.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        ScaleCardChildren(rect, size);
        return card.GetComponent<AchievementCardUI>();
    }

    private static void ScaleCardChildren(RectTransform card, Vector2 size)
    {
        Image skin = UIBinder.FindImage(card, "Img_CardSkin");
        if (skin != null)
        {
            skin.rectTransform.sizeDelta = size;
        }

        Image outline = UIBinder.FindImage(card, "Img_SelectedOutline");
        if (outline != null)
        {
            outline.rectTransform.sizeDelta = size + new Vector2(16f, 16f);
        }

        Image lockOverlay = UIBinder.FindImage(card, "Img_LockOverlay");
        if (lockOverlay != null)
        {
            lockOverlay.rectTransform.sizeDelta = size;
        }
    }

    private static void AddTutorialCard(Transform parent, string name, Vector2 anchoredPosition, string badge, string title, string description)
    {
        RectTransform card = AddGroup(parent, name, anchoredPosition, new Vector2(880f, 300f));
        AddImage(card, "Img_CardSkin", Vector2.zero, new Vector2(880f, 300f), CardColor);
        AddImage(card, "Img_NumberBadge", new Vector2(-360f, 78f), new Vector2(72f, 72f), ButtonColor);
        AddText(card, "Txt_Number", new Vector2(-360f, 78f), new Vector2(60f, 60f), badge, 30f, TextAlignmentOptions.Center, Color.white);
        AddImage(card, "Img_Illustration", new Vector2(-250f, -35f), new Vector2(210f, 170f), new Color(0.58f, 0.80f, 0.48f, 1f));
        AddText(card, "Txt_Title", new Vector2(160f, 62f), new Vector2(500f, 60f), title, 34f, TextAlignmentOptions.Left, TextColor);
        AddText(card, "Txt_Description", new Vector2(160f, -45f), new Vector2(500f, 130f), description, 27f, TextAlignmentOptions.Left, TextColor);
    }

    private static void AddRewardPanel(Transform parent, string panelName, Vector2 anchoredPosition, string iconName, string label, string itemName, string description)
    {
        RectTransform panel = AddPanel(parent, panelName, anchoredPosition, new Vector2(900f, 190f));
        AddImage(panel, iconName, new Vector2(-350f, 0f), new Vector2(98f, 98f), new Color(0.84f, 0.65f, 0.28f, 1f));
        AddText(panel, "Txt_Label", new Vector2(-55f, 46f), new Vector2(590f, 42f), label, 27f, TextAlignmentOptions.Left, TextColor);
        AddText(panel, panelName == "Panel_GoldReward" ? "Txt_Value" : "Txt_ItemName", new Vector2(-55f, 2f), new Vector2(590f, 48f), itemName, 32f, TextAlignmentOptions.Left, TextColor);
        if (!string.IsNullOrEmpty(description))
        {
            AddText(panel, "Txt_Description", new Vector2(-55f, -54f), new Vector2(590f, 48f), description, 23f, TextAlignmentOptions.Left, TextColor);
        }
    }

    private static RectTransform AddScrollView(Transform parent, string name, Vector2 anchoredPosition, Vector2 size)
    {
        RectTransform scroll = AddGroup(parent, name, anchoredPosition, size);
        Image image = scroll.gameObject.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.08f);
        ScrollRect scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        return scroll;
    }

    private static void AddCollectionSection(Transform parent, string sectionName, Vector2 anchoredPosition, string[] itemNames)
    {
        RectTransform section = AddGroup(parent, sectionName, anchoredPosition, new Vector2(900f, 420f));
        string title = sectionName.Replace("Section_", string.Empty);
        AddText(section, "Txt_Title", new Vector2(-330f, 175f), new Vector2(240f, 44f), title, 30f, TextAlignmentOptions.Left, TextColor);

        for (int i = 0; i < itemNames.Length; i++)
        {
            bool unlocked = !itemNames[i].Contains("잠금");
            float x = -330f + (i % 4) * 220f;
            float y = 55f - (i / 4) * 175f;
            AddCollectionItemCard(section, $"CollectionItemCard_{i + 1:00}", new Vector2(x, y), new Vector2(190f, 150f), itemNames[i], unlocked);
        }
    }

    private static GameObject CreatePrefabRoot(string name, Vector2 size)
    {
        GameObject root = new GameObject(name, typeof(RectTransform));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        return root;
    }

    private static GameObject InstantiatePrefab(string path, Transform parent, string name)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
        {
            throw new InvalidOperationException($"Prefab not found: {path}");
        }

        instance.name = name;
        instance.transform.SetParent(parent, false);
        return instance;
    }

    private static void SavePrefab(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void SaveScene(SceneContext context, string path)
    {
        EditorSceneManager.SaveScene(context.scene, path);
    }

    private static void UpdateBuildSettings()
    {
        string[] scenePaths =
        {
            "Assets/Scenes/00_TitleScene.unity",
            "Assets/Scenes/01_TutorialScene.unity",
            "Assets/Scenes/02_HomeScene.unity",
            "Assets/Scenes/03_GoalScene.unity",
            "Assets/Scenes/04_FocusScene.unity",
            "Assets/Scenes/05_SuccessScene.unity",
            "Assets/Scenes/06_FailScene.unity",
            "Assets/Scenes/07_ShopScene.unity",
            "Assets/Scenes/08_CollectionScene.unity",
            "Assets/Scenes/09_RecordScene.unity"
        };

        EditorBuildSettings.scenes = scenePaths
            .Select(path => new EditorBuildSettingsScene(path, true))
            .ToArray();
    }
}
