using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public partial class Snapper : MonoBehaviour
{
    private float BeamShiftDistance()
    {
        if (IsTargetPrefabOfType(PrefabType.Floor))
        {
            return GetTransformBounds(SnapTarget()).size.x / 2;
        }
        return 0f;
    }
}
