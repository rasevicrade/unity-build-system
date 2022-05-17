using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Snapper : MonoBehaviour
{
    public float snapDistance = 1f;
    public PrefabType prefabType;
    private PreviewController previewController;

    #region Lifecycle methods
    private void OnEnable()
    {
        previewController = FindObjectOfType<PreviewController>();
    }

    void Update()
    {
        var edge = FindPlacedObjectsToSnap();
        if (edge != null)
        {
            Snap(edge);
        }
    }
    #endregion

    #region Snap to edges
    private void Snap(Transform edge)
    {
        var updatedPosition = GetPositionFromEdge(edge);
        var updatedRotation = GetRotationFromEdge(edge);
        if (previewController != null)
        {
            previewController.UpdatePosition(updatedPosition, true);
            previewController.UpdateRotation(updatedRotation, true);
        }
    }

    private Vector3 GetPositionFromEdge(Transform edge)
    {
        switch (prefabType)
        {
            case PrefabType.Floor: return ShiftPreview(edge);
            case PrefabType.Wall:
            default: return edge.position;
        }
    }

    private Vector3 ShiftPreview(Transform edge)
    {
        var snapTargetPosition = new Vector3(edge.parent.position.x, edge.transform.position.y, edge.parent.position.z);
        var targetHalfBound = (Application.isEditor ? edge.parent.GetComponent<MeshFilter>().sharedMesh.bounds.size.x / 2 : edge.parent.GetComponent<MeshFilter>().mesh.bounds.size.x / 2);
        var currentPreviewHalfBound = (Application.isEditor ? transform.GetComponent<MeshFilter>().sharedMesh.bounds.size.x / 2 : transform.GetComponent<MeshFilter>().mesh.bounds.size.x / 2);
        return snapTargetPosition + edge.forward * (targetHalfBound + currentPreviewHalfBound);
    }

    private Quaternion GetRotationFromEdge(Transform edge)
    {
        switch (prefabType)
        {
            case PrefabType.Floor: return edge.parent.rotation;
            case PrefabType.Wall:
            default: return edge.rotation * Quaternion.Euler(0, 90, 0); ;
        }

    }
    #endregion

    #region Find edges to snap to
    private Transform FindPlacedObjectsToSnap()
    {
        switch (prefabType)
        {
            case PrefabType.Floor: return FindFloorEdgeSideways();
            case PrefabType.Wall: return FindFloorEdgeFromTop();
            default: return null;
        }

    }
    private Transform FindFloorEdgeSideways()
    {
        foreach (Transform t in gameObject.transform)
        {
            t.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            var ray = new Ray(t.position, t.forward);
            Debug.DrawRay(ray.origin, ray.direction, Color.red);
            if (Physics.Raycast(ray, out var hitInfo, snapDistance))
            {
                Debug.Log("Floor snapped");
                if (hitInfo.transform.GetComponent<EdgePosition>() != null)
                    return hitInfo.transform;
            }
        }
        return null;
    }
    private Transform FindFloorEdgeFromTop()
    {
        var ray = new Ray(transform.position + Vector3.up * 10, -transform.up);
        Debug.DrawRay(ray.origin, ray.direction, Color.cyan);
        if (Physics.Raycast(ray, out var hitInfo))
        {
            if (hitInfo.transform.GetComponent<EdgePosition>() != null)
            {
                Debug.Log("Edge: " + hitInfo.transform.name);
                return hitInfo.transform;
            }
                
        }
        return null;
    }
    #endregion

    public enum PrefabType
    {
        Floor,
        Wall
    }
}
