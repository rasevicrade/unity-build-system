using UnityEngine;
using System.Linq;

public partial class BlueprintEditor
{
    public Snapper targetObject; 
    private Material targetObjectOriginalMaterial;
    private Material targetMaterial;

    private void ManageTarget(RaycastHit hitInfo)
    {
        if (targetObject != null)
            ClearTargetObject();

        if (HitinfoIsBlueprintObject(hitInfo))
            SetTargetObject(hitInfo);

        if (IsReplacementModeActive()) // If replacement mode is active, and we clicked, replace the object
            ReplaceActiveObject();
    }

    private void SetTargetObject(RaycastHit hitInfo)
    {
        targetObject = hitInfo.transform.GetComponent<Snapper>();
        SetTargetObjectColor();
        SetActivePrefabGroupToTargetObjectGroup();
    }
    private void SetActivePrefabGroupToTargetObjectGroup()
    {
        var group = prefabGroups.FirstOrDefault(x => x.Prefabs.Any(p => p.name == targetObject.name));
        if (group != null)
        {
            activePrefabGroupIndex = prefabGroups.IndexOf(group);
        }
    }

    private void ReplaceActiveObject()
    {
        var activeGroup = prefabGroups[activePrefabGroupIndex];
        blueprint.PlaceGameObject(activeGroup.Prefabs[activeGroup.activePrefabIndex], targetObject.transform.position, targetObject.transform.rotation, GetParent(activeGroup));
        DestroyImmediate(targetObject);
    }

    private void SetTargetObjectColor()
    {
        targetObjectOriginalMaterial = targetObject.gameObject.GetComponent<Renderer>().sharedMaterial;
        targetObject.gameObject.GetComponent<Renderer>().sharedMaterial = targetMaterial;
    }

    private void ClearTargetObject()
    {
        targetObject.gameObject.SetActive(true);
        targetObject.gameObject.GetComponent<Renderer>().sharedMaterial = targetObjectOriginalMaterial;
        targetObject = null;
    }

    private bool IsReplacementModeActive()
    {
        var activeGroup = prefabGroups[activePrefabGroupIndex];
        return targetObject != null && activeGroup.Prefabs[activeGroup.activePrefabIndex] != null;
    }

    private bool HitinfoIsBlueprintObject(RaycastHit hitInfo)
    {
        return hitInfo.transform.GetComponent<Snapper>();
    }
    private void PrepareTargetMaterial()
    {
        targetMaterial = new Material(Shader.Find("HDRP/Lit"));
        targetMaterial.color = Color.green;
        targetMaterial.SetColor("_BaseColor", Color.green);
    }
}
