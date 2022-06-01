using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public partial class Snapper : MonoBehaviour
{
    private Vector3 positionBeamSnapper;
    private Vector3 positionSnapper;
    void OnDrawGizmosSelected()
    {
        Handles.Label(positionBeamSnapper, positionBeamSnapper.ToString());
        Handles.Label(positionSnapper, positionSnapper.ToString());
    }
    private Transform FindVerticalCorner()
    {
        var ray = new Ray(new Vector3(GetTransformBounds(transform).max.x, GetTransformBounds(transform).max.y, GetTransformBounds(transform).min.z), -transform.up);
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
        if (IsTargetPrefabOfType(PrefabType.Wall))
        {
            return GetTransformBounds(SnapTarget()).size.x;
        }
        return 0f;
    }
}
