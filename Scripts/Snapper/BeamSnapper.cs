using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Snapper : MonoBehaviour
{
    private Transform FindVerticalCorner()
    {
        var ray = new Ray(new Vector3(transform.position.x, transform.position.y + GetTransformBounds(transform).center.y, GetTransformBounds(transform).min.z), -transform.forward);
        Debug.DrawRay(ray.origin, ray.direction, Color.cyan);
        if (Physics.SphereCast(ray.origin, GetTransformBounds(transform).size.z, -transform.forward, out var hitInfo))
        //if (Physics.Raycast(ray, out var hitInfo))
        {
            var edgePosition = hitInfo.transform.GetComponent<EdgePosition>();
            if (edgePosition != null && edgePosition.edge == EdgePosition.Edge.VerticalSide)
            {
                Debug.Log(hitInfo.transform.name);
                return hitInfo.transform;
            }

        }
        return null;
    }

    private float BeamShiftDistance()
    {
        if (IsTargetPrefabOfType(PrefabType.Wall))
        {
            return 0f;
        }
        return 0f;
    }
}
