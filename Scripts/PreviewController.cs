using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static BoundExtensions;
using static Vector3Extensions;

[ExecuteInEditMode]
public class PreviewController : MonoBehaviour
{
    public float baseHeight;
    public bool isSnapped;

    private Blueprint blueprint;
    private GameObject currentPrefabPreview;
    private Snapper currentPreviewSnapper;  

    protected void OnEnable()
    {
        isSnapped = false;
    }

    public GameObject CreatePreview(Blueprint blueprint, Vector3 position, GameObject currentPrefab, float scale)
    {
        this.blueprint = blueprint;
        if (currentPrefab != null && currentPrefabPreview == null)
        {
            currentPrefabPreview = Instantiate(currentPrefab, position, Quaternion.identity);
            CheckAllowedPrefabsAreSet(currentPrefabPreview);
            currentPrefabPreview.name = currentPrefabPreview.name + "-Preview";
            currentPrefabPreview.transform.parent = blueprint.transform;
            currentPrefabPreview.transform.localScale = new Vector3(scale, scale, scale);
            currentPreviewSnapper = currentPrefabPreview.GetComponent<Snapper>();
            currentPreviewSnapper.isPreview = true;
            //currentPrefabPreview.transform.LookAt(Camera.main.transform.position);
            SceneVisibilityManager.instance.DisablePicking(currentPrefabPreview, true);

            currentPrefabPreview.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            foreach (Transform t in currentPrefabPreview.transform)
            {
                if (t.gameObject.layer != LayerMask.NameToLayer("Colliders"))
                    t.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            }
            return currentPrefabPreview;
        }
        return null;
    }

    private void CheckAllowedPrefabsAreSet(GameObject currentPrefabPreview)
    {
        if (currentPrefabPreview.transform.GetComponent<Snapper>().allowedTargets == null || currentPrefabPreview.transform.GetComponent<Snapper>().allowedTargets.Count == 0)
        {
            Debug.LogError("Allowed target not set for current prefab: " + currentPrefabPreview.name);
        }
    }

    public void UpdatePosition(Vector3 position, bool snap = false)
    {
        if (currentPrefabPreview == null || (snap && isSnapped)) // Can't snap again if already snapped
            return;

        position = new Vector3(position.x, (currentPreviewSnapper.canBeStacked ? baseHeight + position.y : baseHeight), position.z);

        var unsnapDistance = currentPreviewSnapper.snapDistance * blueprint.activeScale;
        if (isSnapped)  
        {
            if (Vector3.Distance(position, currentPrefabPreview.transform.position) > unsnapDistance)
            {
                isSnapped = false;
                currentPrefabPreview.transform.position = position;
            }
        }
        else
        {
            currentPrefabPreview.transform.position = position;
            isSnapped = snap;
        }
    }

    public void UpdateRotation(Quaternion updatedRotation)
    {
        if (currentPrefabPreview == null) 
            return;

        currentPrefabPreview.transform.rotation = updatedRotation;
    }

    public Vector3 GetPosition()
    {
        return currentPrefabPreview != null ? currentPrefabPreview.transform.position : Vector3.zero;
    }

    public Quaternion? GetRotation()
    {
        return currentPrefabPreview?.transform.rotation;
    }
}
