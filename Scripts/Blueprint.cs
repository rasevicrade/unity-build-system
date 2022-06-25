using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Blueprint : MonoBehaviour
{
    public float activeScale = 1;
    public float activeBaseHeight = 0;
    public float floorHeight = 6;
    public GameObject PlaceGameObject(GameObject activeObject, Vector3 position, Quaternion? rotation)
    {
        if (position == Vector3.zero)
            return null;

        var instantiatedGO = Instantiate(activeObject, position, rotation != null ? rotation.Value : Quaternion.Euler(0,0,0));
        instantiatedGO.transform.parent = transform;
        instantiatedGO.transform.localScale = new Vector3(activeScale, activeScale, activeScale);
        instantiatedGO.gameObject.layer = LayerMask.NameToLayer("Default");
        instantiatedGO.name = instantiatedGO.name.Replace("(Clone)", "") + "-Placed";      
        
        CheckIfSnappable(instantiatedGO);
        return instantiatedGO;
    }

    private void CheckIfSnappable(GameObject instantiatedGO)
    {
        bool isSnappable = false;
        foreach(Transform t in instantiatedGO.transform)
        {
            if (t.gameObject.layer == LayerMask.NameToLayer("Snappable"))
            {
                isSnappable = true;
            }
        }
        if (!isSnappable)   
            Debug.LogWarning("Object cannot be snapped to, because it has no snappable edges: " + instantiatedGO.name);
    }
}
