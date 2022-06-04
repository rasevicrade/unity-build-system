using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public partial class Snapper : MonoBehaviour
{
    private Transform FindEdgeBottomUp()
    {
        var ray = new Ray(transform.position, transform.up);
        if (Physics.Raycast(ray, out var hitInfo))
        {
            var parentSnapper = hitInfo.transform.parent.GetComponent<Snapper>();
            if (parentSnapper != null)
            {
                if (allowedTargets.Count == 0 || allowedTargets.Any(x => x == parentSnapper.prefabType)) // Can snap to anything
                    return hitInfo.transform;
            }
            
        }
        return null;
    }
}
