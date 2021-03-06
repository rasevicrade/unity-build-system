using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static BoundExtensions;
using static Vector3Extensions;

[ExecuteInEditMode]
public class PreviewController : MonoBehaviour
{
    public bool isSnapped;
    public bool stack;
    private bool ignoreSnap;
    private GameObject currentPrefabPreview;
    private Snapper currentPreviewSnapper;

    private Blueprint blueprint;
    

    protected void OnEnable()
    {
        isSnapped = false;

    }

    public bool IsPreviewActive()
    {
        return currentPrefabPreview != null;
    }

    public GameObject ActivePreview
    {
        get
        {
            return currentPrefabPreview;
        }
    }

    public GameObject CreatePreview(Blueprint blueprint, Vector3 position, GameObject currentPrefab, float scale, bool ignoreSnap)
    {
        this.blueprint = blueprint;
        this.ignoreSnap = ignoreSnap;
        if (currentPrefabPreview != null)
            DestroyImmediate(currentPrefabPreview);
        if (currentPrefab != null)
        {
            currentPrefabPreview = Instantiate(currentPrefab, position, Quaternion.identity);
            currentPrefabPreview.name = currentPrefabPreview.name + "-Preview";
            currentPrefabPreview.transform.parent = blueprint.transform;
            currentPrefabPreview.transform.localScale = new Vector3(scale, scale, scale);
            currentPreviewSnapper = currentPrefabPreview.GetComponent<Snapper>();
            currentPreviewSnapper.isPreview = true;
            SceneVisibilityManager.instance.DisablePicking(currentPrefabPreview, true);

            currentPrefabPreview.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            foreach (Transform t in currentPrefabPreview.transform)
            {
                if (t.gameObject.layer != LayerMask.NameToLayer("Colliders"))
                    t.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            }
            var renderer = currentPrefabPreview.gameObject.GetComponent<Renderer>();
            return currentPrefabPreview;
        }
        return null;
    }

    public void UpdatePosition(Vector3 position, bool snap = false)
    {
        if (currentPrefabPreview == null || (snap && isSnapped) || (snap && ignoreSnap) || currentPreviewSnapper.defaults == null) // Can't snap again if already snapped
            return;

        var unsnapDistance = currentPreviewSnapper.defaults.snapDistance * blueprint.activeScale;
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
