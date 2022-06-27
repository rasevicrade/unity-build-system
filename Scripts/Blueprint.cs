using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class Blueprint : MonoBehaviour
{
    public float activeScale = 1;
    public float activeBaseHeight = 0;
    public float floorHeight = 6;
    public bool showGridPreview;
    public bool addWallsToRooms = true;
    public Vector3 floorStartPosition;
    public Vector3 floorEndPosition;
    public Vector3 activeMousePosition;
    public Snapper selectedObject;
    private LineRenderer lineRenderer;
    private Dictionary<string, GameObject> prefabHolers;

    private void OnEnable()
    {
        lineRenderer = GetComponent<LineRenderer>();
        prefabHolers = new Dictionary<string, GameObject>();
    }

    private void Update()
    {
        if (showGridPreview)
        {
            lineRenderer.positionCount = 4;
            lineRenderer.SetPosition(0, floorStartPosition);
            lineRenderer.SetPosition(1, new Vector3(floorStartPosition.x, 0, activeMousePosition.z));
            lineRenderer.SetPosition(2, activeMousePosition);
            lineRenderer.SetPosition(3, new Vector3(activeMousePosition.x, 0, floorStartPosition.z));
        } 
        else
        {
            lineRenderer.positionCount = 0;
        }
        
    }

    public GameObject PlaceGameObject(GameObject activeObject, Vector3 position, Quaternion? rotation)
    {
        if (position == Vector3.zero)
            return null;

        var instantiatedGO = Instantiate(activeObject, position, rotation != null ? rotation.Value : Quaternion.Euler(0,0,0));
        //instantiatedGO.transform.parent = transform;
        instantiatedGO.transform.localScale = new Vector3(activeScale, activeScale, activeScale);
        instantiatedGO.gameObject.layer = LayerMask.NameToLayer("Default");
        instantiatedGO.name = instantiatedGO.name.Replace("(Clone)", "") + "-Placed";      
        
        if (!IsSnappable(instantiatedGO))
            Debug.LogWarning("Object cannot be snapped to, because it has no snappable edges: " + instantiatedGO.name);

        SetParent(instantiatedGO, activeObject.name);
        
        return instantiatedGO;
    }

    private void SetParent(GameObject instantiatedGO, string prefabType)
    {
        var groupName = prefabType + " - Group";
        if (!prefabHolers.ContainsKey(groupName))
        {
            var holderObject = new GameObject(groupName);
            holderObject.transform.parent = transform;
            prefabHolers[groupName] = holderObject;
        }

        instantiatedGO.transform.parent = prefabHolers[groupName].transform;
    }

    private bool IsSnappable(GameObject instantiatedGO)
    {
        bool isSnappable = false;
        foreach(Transform t in instantiatedGO.transform)
        {
            if (t.gameObject.layer == LayerMask.NameToLayer("Snappable"))
            {
                isSnappable = true;
            }
        }
        return isSnappable;
    }
}
