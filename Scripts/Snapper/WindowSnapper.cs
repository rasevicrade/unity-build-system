using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Snapper : MonoBehaviour
{
    private Transform FindEdgeBottomUp()
    {
        var ray = new Ray(transform.position, transform.up);
        Debug.DrawRay(ray.origin, ray.direction, Color.black, 10);
        if (Physics.Raycast(ray, out var hitInfo))
        {
            var edge = hitInfo.transform.GetComponent<EdgePosition>();
            Debug.Log(hitInfo.transform.name + " " + hitInfo.transform.parent.name);
            if (edge != null && edge.edge == EdgePosition.Edge.WallHole)
            {
                var parentSnapper = edge.transform.parent.GetComponent<Snapper>();
                if (parentSnapper != null)
                {
                    return hitInfo.transform;
                }
            }
        }
        return null;
    }
}
