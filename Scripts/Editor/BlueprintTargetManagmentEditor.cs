using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;

public partial class BlueprintEditor
{
    public TargetObjectInfo activeTarget;

    
    private void ManageTarget(RaycastHit hitInfo)
    {
        if (Event.current.type == EventType.MouseUp && Event.current.button == 0)// We try to set target object only if it is clicked
        {
            if (IsPreviewMode())
            {
                if (PreviewReplacesActiveTarget())
                    ReplaceActiveObject();
                else
                    AppendToActiveObject();
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

    private void AppendToActiveObject()
    {
        blueprint.PlaceGameObject(blueprint.ActivePrefab, activeTarget.target.transform.position, activeTarget.target.transform.rotation, GetParent(blueprint.ActivePrefabGroup));
        ClearPreviousTarget();

    }

    private bool PreviewReplacesActiveTarget()
    {
        return blueprint.ActivePrefab.GetComponent<Snapper>().defaults.replacesActiveTarget;
    }

    private void SetTargetObject(Snapper targetSnapper)
    {
        activeTarget = new TargetObjectInfo(targetSnapper);
        activeTarget.SetTargetObjectColor(blueprint.activeTargetMaterial);
        activeTarget.target.layer = LayerMask.NameToLayer("Ignore Raycast");
        SetActivePrefabGroupToTargetObjectGroup(targetSnapper);
    }
    private void SetActivePrefabGroupToTargetObjectGroup(Snapper target)
    {
        var group = blueprint.prefabGroups.FirstOrDefault(x => x.Prefabs.Any(p => p.name == target.gameObject.name));
        if (group != null)
        {
            blueprint.activePrefabGroupIndex = blueprint.prefabGroups.IndexOf(group);
        }
    }

    private void ReplaceActiveObject()
    {
        blueprint.PlaceGameObject(blueprint.ActivePrefab, activeTarget.target.transform.position, activeTarget.target.transform.rotation, GetParent(blueprint.ActivePrefabGroup));
        DestroyImmediate(activeTarget.target);
        activeTarget = null;
    }


    private void ClearPreviousTarget()
    {
        activeTarget.target.layer = LayerMask.NameToLayer("Default");
        activeTarget.target.GetComponent<Renderer>().sharedMaterial = activeTarget.material;
        activeTarget = null;
    }

    private bool IsPreviewMode()
    {
        return activeTarget != null && previewController.ActivePreview != null;
    }

    public class TargetObjectInfo
    {
        public GameObject target;
        public Snapper snapper;
        public Material material;
        public string group { get; private set; }

        public TargetObjectInfo(Snapper targetSnapper)
        {
            target = targetSnapper.gameObject;
            snapper = targetSnapper;
            material = targetSnapper.gameObject.GetComponent<Renderer>().sharedMaterial;
            group = snapper.GetGroup();
        }

        public void SetTargetObjectColor(Material targetMaterial)
        {
            target.GetComponent<Renderer>().sharedMaterial = targetMaterial;
        }
    }
}
