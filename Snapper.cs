using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static EdgePosition;

[ExecuteInEditMode]
public class Snapper : MonoBehaviour
{
    public float snapDistance = 1f;
    public PrefabType prefabType;
    private PreviewController previewController;
    public bool isPreview;

    #region Lifecycle methods
    private void OnEnable()
    {
        previewController = FindObjectOfType<PreviewController>();
    }

    void Update()
    {
        // If I am a preview, find edges to snap to
        if (isPreview)
        {
            var edge = FindPlacedObjectsToSnap();
            if (edge != null)
            {
                Snap(edge);
            }
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
        return ShiftPreview(edge);
        //switch (prefabType)
        //{
        //    case PrefabType.Floor: ;
        //    case PrefabType.Wall:
        //    default: return edge.position;
        //}
    }

    private Vector3 ShiftPreview(Transform edge)
    {
        var snapTarget = edge.parent;
        // Whatever current preview is, we first move it to center of snapped target - snapTargetPosition
        var snapTargetPosition = new Vector3(snapTarget.position.x, edge.transform.position.y, snapTarget.position.z);

        var targetHalfSize = GetTargetBounds(snapTarget).size.x / 2;
        var currentPreviewHalfSize = GetTargetBounds(transform).size.x / 2;

        Vector3 shiftedPosition = Vector3.zero;
        bool isCurrentWall = prefabType == PrefabType.Wall;
        bool isTargetWall = snapTarget.GetComponent<Snapper>().prefabType == PrefabType.Wall;
        bool isCurrentFloor = prefabType == PrefabType.Floor;
        bool isTargetFloor = snapTarget.GetComponent<Snapper>().prefabType == PrefabType.Floor;

        if (isCurrentWall && isTargetFloor) // If I am a wall, snapping to a floor, only shift by half size of the floor
        {
            shiftedPosition = snapTargetPosition + edge.forward * targetHalfSize;
        } 
        else if (isCurrentFloor && isTargetWall) // If I am a floor, snapping to a wall, don't add target half size, only mine
        {
            shiftedPosition = snapTargetPosition + edge.forward * currentPreviewHalfSize;
        } 
        else if (isCurrentFloor && isTargetFloor)
        {
            shiftedPosition = snapTargetPosition + edge.forward * (currentPreviewHalfSize + targetHalfSize);
        }
        var isFloorSnappingToWallTop = edge.GetComponent<EdgePosition>().edge == Edge.Top;

        if (isFloorSnappingToWallTop)
        {
            var shiftDownByFloorHeight = shiftedPosition - Vector3.up * transform.GetComponent<BoxCollider>().bounds.size.y / 2; // Edge position is at wall top, we need to push floor down by floor height to have it inside wall
            var keepFloorAboveWall = shiftDownByFloorHeight + Vector3.up * 0.005f; // Since wall has advantage in z fight, we still want to have floor above it, so we cheat a bit by moving it up slightly
            return keepFloorAboveWall;
        }
        return shiftedPosition;
    }

    private Quaternion GetRotationFromEdge(Transform edge)
    {
        switch (prefabType)
        {
            case PrefabType.Floor: return transform.rotation;
            case PrefabType.Wall:
            default: return edge.rotation * Quaternion.Euler(0, 90, 0); ;
        }

    }

    private Bounds GetTargetBounds(Transform target)
    {
        return (Application.isEditor ? target.GetComponent<MeshFilter>().sharedMesh.bounds : target.GetComponent<MeshFilter>().mesh.bounds);
    }

    #endregion

    #region Find edges to snap to
    private Transform FindPlacedObjectsToSnap()
    {
        switch (prefabType)
        {
            case PrefabType.Floor: return FindEdgeSideways();
            case PrefabType.Wall: return FindEdgeFromAbove();
            default: return null;
        }

    }
    private Transform FindEdgeSideways()
    {
        foreach (Transform t in gameObject.transform)
        {
            var ray = new Ray(t.position, t.forward);
            if (Physics.Raycast(ray, out var hitInfo, snapDistance))
            {
                if (hitInfo.transform.GetComponent<EdgePosition>() != null)
                    return hitInfo.transform;
            }
        }
        return null;
    }
    private Transform FindEdgeFromAbove()
    {
        var ray = new Ray(transform.position + Vector3.up * 10, -transform.up);
            
        if (Physics.Raycast(ray, out var hitInfo))
        {
            if (hitInfo.transform.GetComponent<EdgePosition>() != null)
            {
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
