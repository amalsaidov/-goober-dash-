using System;
using System.Collections.Generic;

/// <summary>Block types available in the map constructor.</summary>
public enum ConstructBlockType
{
    Floor         = 0,
    Platform      = 1,
    ThinPlatform  = 2,
    Wall          = 3,
    BouncePad     = 4,
    SpeedPad      = 5,
    ConveyorLeft  = 6,
    ConveyorRight = 7,
    LowGravZone   = 8,
    Checkpoint    = 9,
    FinishLine    = 10,
}

[Serializable]
public class ConstructBlock
{
    public ConstructBlockType type;
    public float x, y;
}

[Serializable]
public class CustomMapData
{
    public string mapName = "My Map";
    public List<ConstructBlock> blocks = new List<ConstructBlock>();
}
