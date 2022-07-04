using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class Blueprint : MonoBehaviour
{
    public Material activeTargetMaterial;
    public List<PrefabGroup> prefabGroups = new List<PrefabGroup>();
    public int activePrefabGroupIndex = 0;
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

    private void OnEnable()
    {
        lineRenderer = GetComponent<LineRenderer>();
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

    public PrefabGroup ActivePrefabGroup
    {
        get
        {
            try
            {
                return prefabGroups[activePrefabGroupIndex];
            }
            catch
            {
                return null;
            }

        }
    }
    public GameObject ActivePrefab
    {
        get
        {
            try
            {
                return prefabGroups[activePrefabGroupIndex].Prefabs[prefabGroups[activePrefabGroupIndex].activePrefabIndex];
            }
            catch
            {
                return null;
            }
            
        }
    }

    public GameObject PlaceGameObject(GameObject activeObject, Vector3 position, Quaternion? rotation, GameObject parent)
    {
        if (position == Vector3.zero || PositionTakenByAnotherObjectOfSameType(activeObject, position, rotation))
            return null;

        var instantiatedGO = Instantiate(activeObject, position, rotation != null ? rotation.Value : Quaternion.Euler(0,0,0));
        instantiatedGO.transform.localScale = new Vector3(activeScale, activeScale, activeScale);
        instantiatedGO.gameObject.layer = LayerMask.NameToLayer("Default");
        instantiatedGO.name = activeObject.name;      
        
        if (!instantiatedGO.transform.IsSnappable())
            Debug.LogWarning("Object cannot be snapped to, because it has no snappable edges: " + instantiatedGO.name);

        SetParent(instantiatedGO, parent);
        
        return instantiatedGO;
    }

    private bool PositionTakenByAnotherObjectOfSameType(GameObject prefab, Vector3 position, Quaternion? rotation)
    {
        // We create temp to get bounds, then delete it right away
        var temp = Instantiate(prefab, position, rotation.HasValue ? rotation.Value : Quaternion.identity);
        Vector3 bounds = new Vector3(temp.transform.GetBounds().extents.x, temp.transform.GetBounds().extents.y, temp.transform.GetBounds().extents.z);
        DestroyImmediate(temp);

        var overlappingList = Physics.OverlapBox(
            position + Vector3.up * bounds.y,
            bounds / 2,
            rotation.HasValue ? rotation.Value : Quaternion.identity, LayerMask.GetMask("Default"))
        .Where(x => x.GetComponent<Snapper>() != null && x.GetComponent<Snapper>().defaults.prefabType == prefab.GetComponent<Snapper>().defaults.prefabType).ToList();

        if (overlappingList.Count > 0)
        {
            var tar = overlappingList[0];
        }
        
        return overlappingList.Count > 0;
    }



    private void SetParent(GameObject instantiatedGO, GameObject parent)
    {
        instantiatedGO.transform.parent = parent.transform;
    }
}
