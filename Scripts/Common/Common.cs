using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PrefabType
{
    Floor,
    Wall,
    Window,
    Seam,
    Beam,
    SideRoof,
    Door,
    Stairs,
    WallDecoration
}

public enum HorizontalFrontShiftDirection
{
    Forward,
    Backward,
    None
}

public enum HorizontalShiftDistance
{
    None,
    Half,
    Full,
    ShorterHalf,
    NegativeShorterHalf
}

[System.Serializable]
public class TargetPrefabShiftDistancePair
{
    public PrefabType prefabType;
    public HorizontalShiftDistance targetsShiftDistance;

    public TargetPrefabShiftDistancePair(PrefabType prefabType, HorizontalShiftDistance targetsShiftDistance = HorizontalShiftDistance.None)
    {
        this.prefabType = prefabType;
        this.targetsShiftDistance = targetsShiftDistance;
    }

}

public enum RotationType
{
    None,
    TargetRoot,
    TargetEdge
}