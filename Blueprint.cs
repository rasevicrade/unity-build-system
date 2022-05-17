using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Blueprint : MonoBehaviour
{
    public GameObject PlaceGameObject(GameObject activeObject, Vector3 position, Quaternion? rotation)
    {
        if (position == Vector3.zero)
            return null;

        var instantiatedGO = Instantiate(activeObject, position, rotation != null ? rotation.Value : Quaternion.Euler(0,0,0));
        instantiatedGO.transform.parent = transform;
        instantiatedGO.name = instantiatedGO.name.Replace("(Clone)", "") + "-Placed";      
        
        RemoveSnapper(instantiatedGO);
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

    private void RemoveSnapper(GameObject placedObject)
    {
        if (Application.isEditor)
        {
            DestroyImmediate(placedObject.GetComponent<Snapper>());
        }
        else
        {
            Destroy(placedObject.GetComponent<Snapper>());
        }
    }
}
