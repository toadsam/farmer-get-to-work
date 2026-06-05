using System;
using System.Collections.Generic;

[Serializable]
public class IslandStateData
{
    public string islandId;
    public string displayName;

    public bool unlocked;
    public bool activated;

    public int level;
    public int growthPoints;

    public List<CropPlotStateData> cropPlots = new List<CropPlotStateData>();
    public List<string> unlockedObjectIds = new List<string>();
    public List<string> discoveredHiddenObjectIds = new List<string>();
}

[Serializable]
public class CropPlotStateData
{
    public string plotId;
    public string cropId;

    public string state;
    public int growthPoints;
}

[Serializable]
public class IslandUnlockData
{
    public string islandId;
    public string displayName;

    public int requiredUnlockProgress;
    public int activationGoldCost;

    public bool unlocked;
    public bool activated;
}