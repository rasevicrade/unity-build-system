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
        var ray = new Ray(new Vector3(transform.position.x, transform.position.y + GetTransformBounds(transform).center.y, transform.position.z), -transform.forward);
        //Debug.DrawRay(ray.origin, ray.direction, Color.cyan);
        //positionBeamSnapper = ray.origin;
        //if (Physics.SphereCast(ray.origin, GetTransformBounds(transform).size.z, ray.direction, out var hitInfo))
        if (Physics.Raycast(ray, out var hitInfo))
        {
            var edgePosition = hitInfo.transform.GetComponent<EdgePosition>();
            if (edgePosition != null && edgePosition.edge == EdgePosition.Edge.VerticalSide)
            {
                Debug.DrawRay(edgePosition.transform.position, edgePosition.transform.forward, Color.cyan);
                return hitInfo.transform;
            }

        }
        return null;
    }

    private float BeamShiftDistance()
    {
        if (IsTargetPrefabOfType(PrefabType.Wall))
        {
            return GetTransformBounds(SnapTarget()).size.x / 2;
        }
        return 0f;
    }
}
