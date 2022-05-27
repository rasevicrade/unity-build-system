using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PreviewController : MonoBehaviour
{
    private GameObject currentPrefabPreview;
    private Vector3 currentRotation;
    public bool isSnapped;

    protected void OnEnable()
    {
        isSnapped = false;
        currentRotation = Quaternion.identity.eulerAngles;
    }

    public GameObject CreatePreview(Blueprint blueprint, Vector3 position, GameObject currentPrefab)
    {
        if (currentPrefab != null && currentPrefabPreview == null)
        {
            currentPrefabPreview = Instantiate(currentPrefab, position, Quaternion.identity);
            currentPrefabPreview.name = currentPrefabPreview.name + "-Preview";
            currentPrefabPreview.transform.parent = blueprint.transform;
            currentPrefabPreview.GetComponent<Snapper>().isPreview = true;
            SceneVisibilityManager.instance.DisablePicking(currentPrefabPreview, true);

            currentPrefabPreview.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            foreach (Transform t in currentPrefabPreview.transform)
            {
                t.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            }
            return currentPrefabPreview;
        }
        return null;
    }



    public void UpdatePosition(Vector3 position, bool snap = false)
    {
        if (currentPrefabPreview == null || (snap && isSnapped)) // Can't snap again if already snapped
            return;

        if (isSnapped)
        {
            if (Vector3.Distance(position, currentPrefabPreview.transform.position) > 5f)
            {
                isSnapped = false;
                currentPrefabPreview.transform.position = position;
            }
        }
        else
        {
            currentPrefabPreview.transform.position = position;
            currentPrefabPreview.transform.localEulerAngles = currentRotation;
            isSnapped = snap;
            
        }
    }

    internal void UpdateRotation(Quaternion updatedRotation, bool snap = false)
    {
        if (currentPrefabPreview == null) // Can't snap again if already snapped
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
