using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Snapper : MonoBehaviour
{
    private Transform FindWall()
    {
        if (Physics.SphereCast(transform.position, GetTransformBounds(transform).size.z, transform.forward, out var hit))
        {
            var snapper = hit.transform.GetComponent<Snapper>();
            if (snapper != null && snapper.prefabType == PrefabType.Wall)
            {
                return hit.transform;
            }
        }
        return null;
    }
}
