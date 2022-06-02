using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Blueprint : MonoBehaviour
{
    public float activeScale = 1;
    public GameObject PlaceGameObject(GameObject activeObject, Vector3 position, Quaternion? rotation)
    {
        if (position == Vector3.zero)
            return null;

        var instantiatedGO = Instantiate(activeObject, position, rotation != null ? rotation.Value : Quaternion.Euler(0,0,0));
        instantiatedGO.transform.parent = transform;
        instantiatedGO.transform.localScale = new Vector3(activeScale, activeScale, activeScale);
        instantiatedGO.gameObject.layer = LayerMask.NameToLayer("Default");
        instantiatedGO.name = instantiatedGO.name.Replace("(Clone)", "") + "-Placed";      
        
        SetEdgeLayerToDefault(instantiatedGO);
        return instantiatedGO;
    }

    private void SetEdgeLayerToDefault(GameObject instantiatedGO)
    {
        foreach(Transform t in instantiatedGO.transform)
        {
            t.gameObject.layer = LayerMask.NameToLayer("Default");
        }
    }
}
