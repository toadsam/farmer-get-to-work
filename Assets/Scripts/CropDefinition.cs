using UnityEngine;

[CreateAssetMenu(menuName = "Farm/Crop Definition")]
public class CropDefinition : ScriptableObject
{
    public string cropId;
    public string displayName;

    [Header("Growth")]
    public int requiredGrowthPoints = 100;

    [Header("Reward")]
    public int sellGold = 10;

    [Header("Visual Prefabs")]
    public GameObject seedPrefab;
    public GameObject growingPrefab;
    public GameObject readyPrefab;
}