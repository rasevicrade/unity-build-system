using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SnapperData", menuName = "ScriptableObjects/SnapperDefaultsScriptable", order = 1)]
public class SnapperDefaultsScriptable : ScriptableObject
{
    public PrefabType prefabType;
    public float snapDistance = 2f;

    public HorizontalFrontShiftDirection horizontalFrontShiftDirection;
    public HorizontalShiftDistance myFrontShiftDistance;
    public List<TargetPrefabShiftDistancePair> targetsWithShift;

    public RotationType rotationType;

    public bool shiftSideways;
    public HorizontalShiftDistance mySideWaysShiftDistance;
    public HorizontalShiftDistance targetSideWaysShiftDistance;

    public bool shiftDown;
    
}
