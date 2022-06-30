using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public partial class BlueprintEditor
{
    public TargetObjectInfo activeTarget;

    
    private void ManageTarget(RaycastHit hitInfo)
    {
        if (Event.current.type == EventType.MouseUp && Event.current.button == 0)// We try to set target object only if it is clicked
        {
            if (IsReplacementModeActive())
            {
                ReplaceActiveObject();
                DeletePreview();
                return;
            }
            var targetSnapper = hitInfo.transform.GetComponent<Snapper>();
            if (targetSnapper == null)
                return;

            if (activeTarget != null)
                ClearPreviousTarget();
            SetTargetObject(targetSnapper);
        }
        if (Event.current.keyCode == KeyCode.Tab)
        {
            if (activeTarget != null)
                ClearPreviousTarget();
        }
    }

    private void SetTargetObject(Snapper targetSnapper)
    {
        activeTarget = new TargetObjectInfo
        {
            target = targetSnapper.gameObject,
            snapper = targetSnapper,
            material = targetSnapper.gameObject.GetComponent<Renderer>().sharedMaterial
        };
        activeTarget.SetTargetObjectColor(blueprint.activeTargetMaterial);
        activeTarget.target.layer = LayerMask.NameToLayer("Ignore Raycast");
        SetActivePrefabGroupToTargetObjectGroup(targetSnapper);
    }
    private void SetActivePrefabGroupToTargetObjectGroup(Snapper target)
    {
        var group = prefabGroups.FirstOrDefault(x => x.Prefabs.Any(p => p.name == target.gameObject.name));
        if (group != null)
        {
            activePrefabGroupIndex = prefabGroups.IndexOf(group);
        }
    }

    private void ReplaceActiveObject()
    {
        var activeGroup = prefabGroups[activePrefabGroupIndex];
        blueprint.PlaceGameObject(activeGroup.Prefabs[activeGroup.activePrefabIndex], activeTarget.target.transform.position, activeTarget.target.transform.rotation, GetParent(activeGroup));
        DestroyImmediate(activeTarget.target);
        activeTarget = null;
    }


    private void ClearPreviousTarget()
    {
        activeTarget.target.layer = LayerMask.NameToLayer("Default");
        activeTarget.target.GetComponent<Renderer>().sharedMaterial = activeTarget.material;
        activeTarget = null;
    }

    private bool IsReplacementModeActive()
    {
        var activeGroup = prefabGroups[activePrefabGroupIndex];
        return activeTarget != null && activeGroup.Prefabs[activeGroup.activePrefabIndex] != null;
    }

    public class TargetObjectInfo
    {
        public GameObject target;
        public Snapper snapper;
        public Material material;

        public void SetTargetObjectColor(Material targetMaterial)
        {
            target.GetComponent<Renderer>().sharedMaterial = targetMaterial;
        }
    }
}
