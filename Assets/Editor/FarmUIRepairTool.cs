using System;
using System.IO;
using System.Linq;
using FarmerGetToWork;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

/// <summary>
/// Repairs generated UI assets after prototype generation.
/// It assigns a valid placeholder sprite to Image slots and a Korean-capable TMP font to every TMP text.
/// </summary>
public static class FarmUIRepairTool
{
    private const string PlaceholderPath = "Assets/Sprites/Placeholder/Placeholder_White.png";
    private const string KoreanFontSourcePath = "Assets/TextMesh Pro/Fonts/MalgunGothic.ttf";
    private const string KoreanFontAssetPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/MalgunGothic_Dynamic.asset";

    [MenuItem("Tools/Farmer Get To Work/Repair Korean Font And UI Sprites")]
    public static void RepairAll()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        Sprite placeholderSprite = EnsurePlaceholderSprite();
        TMP_FontAsset koreanFont = EnsureKoreanFontAsset();

        RepairPrefabs(placeholderSprite, koreanFont);
        RepairScenes(placeholderSprite, koreanFont);
        RepairTMPSettings(koreanFont);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("UI sprite/font repair complete.");
    }

    public static void RepairAllFromCommandLine()
    {
        try
        {
            RepairAll();
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static Sprite EnsurePlaceholderSprite()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(PlaceholderPath));

        if (!File.Exists(PlaceholderPath))
        {
            Texture2D texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            Color[] pixels = Enumerable.Repeat(Color.white, 64).ToArray();
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(PlaceholderPath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }

        AssetDatabase.ImportAsset(PlaceholderPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(PlaceholderPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderPath);
    }

    private static TMP_FontAsset EnsureKoreanFontAsset()
    {
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontAssetPath);
        if (existing != null)
        {
            existing.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            existing.isMultiAtlasTexturesEnabled = true;
            EnsureEmbeddedFontResources(existing, "MalgunGothic");
            EditorUtility.SetDirty(existing);
            return existing;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(KoreanFontSourcePath));
        Directory.CreateDirectory(Path.GetDirectoryName(KoreanFontAssetPath));

        string windowsFontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts", "malgun.ttf");
        if (File.Exists(windowsFontPath) && !File.Exists(KoreanFontSourcePath))
        {
            File.Copy(windowsFontPath, KoreanFontSourcePath);
        }

        AssetDatabase.ImportAsset(KoreanFontSourcePath, ImportAssetOptions.ForceSynchronousImport);

        TMP_FontAsset fontAsset = null;
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(KoreanFontSourcePath);
        if (sourceFont != null)
        {
            fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
        }

        if (fontAsset == null)
        {
            fontAsset = TMP_FontAsset.CreateFontAsset("Malgun Gothic", "Regular", 90);
        }

        if (fontAsset == null)
        {
            throw new InvalidOperationException("Failed to create a Korean TMP font asset. Check C:/Windows/Fonts/malgun.ttf.");
        }

        fontAsset.name = "MalgunGothic_Dynamic";
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        fontAsset.isMultiAtlasTexturesEnabled = true;

        AssetDatabase.CreateAsset(fontAsset, KoreanFontAssetPath);
        EnsureEmbeddedFontResources(fontAsset, "MalgunGothic");
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(KoreanFontAssetPath, ImportAssetOptions.ForceSynchronousImport);

        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontAssetPath);
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

    private static void RepairTMPSettings(TMP_FontAsset fontAsset)
    {
        if (fontAsset == null)
        {
            return;
        }

        TMP_Settings.defaultFontAsset = fontAsset;
        if (TMP_Settings.instance != null)
        {
            EditorUtility.SetDirty(TMP_Settings.instance);
        }
    }

    private static void RepairPrefabs(Sprite placeholderSprite, TMP_FontAsset fontAsset)
    {
        string[] prefabPaths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(path => path)
            .ToArray();

        foreach (string path in prefabPaths)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            RepairHierarchy(root, placeholderSprite, fontAsset);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void RepairScenes(Sprite placeholderSprite, TMP_FontAsset fontAsset)
    {
        if (!Directory.Exists("Assets/Scenes"))
        {
            return;
        }

        string[] scenePaths = Directory.GetFiles("Assets/Scenes", "*.unity", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path)
            .ToArray();

        foreach (string path in scenePaths)
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                RepairHierarchy(root, placeholderSprite, fontAsset);
            }

            EditorSceneManager.SaveScene(scene);
        }
    }

    private static void RepairHierarchy(GameObject root, Sprite placeholderSprite, TMP_FontAsset fontAsset)
    {
        foreach (Image image in root.GetComponentsInChildren<Image>(true))
        {
            if (placeholderSprite != null && image.sprite == null)
            {
                image.sprite = placeholderSprite;
            }

            if (image.name.StartsWith("Img_", StringComparison.Ordinal))
            {
                ImageSlot slot = image.GetComponent<ImageSlot>();
                if (slot == null)
                {
                    slot = image.gameObject.AddComponent<ImageSlot>();
                }

                slot.slotName = image.name;
                slot.targetImage = image;
                slot.placeholderSprite = placeholderSprite;
                EditorUtility.SetDirty(slot);
            }

            EditorUtility.SetDirty(image);
        }

        foreach (TextMeshProUGUI text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (fontAsset != null)
            {
                text.font = fontAsset;
                text.fontSharedMaterial = fontAsset.material;
            }

            text.characterSpacing = 0f;
            text.SetAllDirty();
            EditorUtility.SetDirty(text);
        }
    }
}
