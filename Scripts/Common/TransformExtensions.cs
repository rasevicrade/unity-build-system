using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TransformExtensions
{
    public static Bounds GetBounds(this Transform t)
    {
        var boxCollider = t.GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            return t.GetComponent<BoxCollider>().bounds;
        }
        else
        {
            var meshRenderer = t.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                return meshRenderer.bounds;
            }
            else
            {
                var renderers = t.GetComponentsInChildren<Renderer>();
                if (renderers != null && renderers.Length > 0)
                {
                    var childBounds = renderers[0].bounds;
                    foreach (Renderer r in renderers)
                    {
                        childBounds.Encapsulate(r.bounds);
                    }
                    return childBounds;
                }
                Debug.LogError("Couldn't find bounds for " + t.name);
                return new Bounds(t.position, Vector3.zero);

            }

        }
    }

    public static bool IsSnappable(this Transform t)
    {
        bool isSnappable = false;
        foreach (Transform c in t.transform)
        {
            if (c.gameObject.layer == LayerMask.NameToLayer("Snappable"))
            {
                isSnappable = true;
            }
        }
        return isSnappable;
    }

}
