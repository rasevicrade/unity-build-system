using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public partial class Snapper : MonoBehaviour
{
    private Transform FindVerticalCorner()
    {
        var ray = new Ray(new Vector3(GetTransformBounds(transform).min.x, GetTransformBounds(transform).max.y, GetTransformBounds(transform).min.z), -transform.up);
        //positionBeamSnapper = ray.origin;
        //if (Physics.SphereCast(ray.origin, GetTransformBounds(transform).size.z, ray.direction, out var hitInfo))
        if (Physics.Raycast(ray, out var hitInfo))
        {
            var edgePosition = hitInfo.transform.GetComponent<EdgePosition>();
            if (edgePosition != null && edgePosition.edge == EdgePosition.Edge.VerticalSide)
            {
                
                return hitInfo.transform;
            }

        }
        return null;
    }

    private float BeamShiftDistance()
    {
        if (IsTargetPrefabOfType(PrefabType.Floor))
        {
            return 2f;// GetTransformBounds(SnapTarget()).size.x / 2;
        }
        return 0f;
    }
}
