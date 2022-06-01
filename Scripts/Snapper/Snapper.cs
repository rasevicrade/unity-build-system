using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static EdgePosition;

[ExecuteInEditMode]
public partial class Snapper : MonoBehaviour
{
    public PrefabType prefabType;
    public float snapDistance = 1f; 
    public bool isPreview;

    private PreviewController previewController;
    private Transform snappedEdge;
    private float targetHalfSize;
    private float currentPreviewHalfSize;

    #region Lifecycle methods
    private void OnEnable()
    {
        previewController = FindObjectOfType<PreviewController>();
    }

    void Update()
    {
        foreach(Transform transform in transform)
        {
            if (transform.GetComponent<EdgePosition>() != null && transform.GetComponent<EdgePosition>().edge == Edge.VerticalSide)
            {
                var pos = transform.GetComponent<EdgePosition>();
                Debug.DrawRay(transform.position, transform.forward, Color.yellow, 10);
            }
        }
        // If I am a preview, find edges to snap to
        if (isPreview && !previewController.isSnapped)
        {
            snappedEdge = FindPlacedObjectsToSnap();
            if (snappedEdge != null)
            {
                Snap(snappedEdge);
            }
        }     
    }
    #endregion
   
    #region Find edges to snap to
    private Transform FindPlacedObjectsToSnap()
    {
        switch (prefabType)
        {
            case PrefabType.Floor:
            case PrefabType.Seam: return FindEdgeSideways();
            case PrefabType.Wall: return FindEdgeFromAbove();
            case PrefabType.Window: return FindWall();
            case PrefabType.Beam: return FindVerticalCorner();
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
                if (hitInfo.transform.GetComponent<EdgePosition>() != null && CanSnap(prefabType, hitInfo.transform.parent.GetComponent<Snapper>().prefabType))
                {
                    return hitInfo.transform;
                }
                    
            }
        }
        return null;
    }

    private bool CanSnap(PrefabType activePrefab, PrefabType targetPrefab)
    {
        switch (activePrefab)
        {
            case PrefabType.Floor:
                return targetPrefab == PrefabType.Floor || targetPrefab == PrefabType.Wall;
            case PrefabType.Wall:
                return targetPrefab == PrefabType.Floor;
            case PrefabType.Seam:
                return targetPrefab == PrefabType.Wall;
            default:
                return false; 
        }
    }
    private Transform FindEdgeFromAbove()
    {
        var ray = new Ray(transform.position + Vector3.up * GetTransformBounds(transform).size.y / 2, -transform.up);
            
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

    #region Snap to edges
    private void Snap(Transform edge)
    {
        if (previewController != null)
        {
            positionBeamSnapper = GetPositionFromEdge(edge);
            previewController.UpdatePosition(GetPositionFromEdge(edge), true, snapDistance);
            previewController.UpdateRotation(GetRotationFromEdge(edge), true);
        }
    }
    #region Position from edge
    private Vector3 GetPositionFromEdge(Transform edge) => GetHorizontalShift(edge) + GetVerticalShift();
    private Vector3 GetHorizontalShift(Transform edge) => GetSnapTargetPosition(edge) + edge.forward * ShiftDistance();
    private Vector3 GetVerticalShift() => RequiresVerticalShift() ? ShiftDownByHalfHeight() + ShiftUpBySmallDelta() : Vector3.zero;
    /// <summary>
    /// If an object is placed on an upper floor and it's kind of object that needs to snap within walls, we need to pull it down vertically
    /// </summary>
    /// <returns></returns>
    private bool RequiresVerticalShift() => (prefabType == PrefabType.Floor || prefabType == PrefabType.Seam) && !IsGroundFloor() && IsTargetPrefabOfType(PrefabType.Wall);
    #region Horizontal shift
    private Vector3 GetSnapTargetPosition(Transform edge) => new Vector3(SnapTarget().position.x, edge.transform.position.y, SnapTarget().position.z);
    private Transform SnapTarget() => snappedEdge.GetComponent<Snapper>() != null ? snappedEdge : snappedEdge.parent;
    
    /// <summary>
    /// Depending on prefab type, return how much active object needs to move horizontally
    /// to fit the target object
    /// </summary>
    /// <returns></returns>
    private float ShiftDistance()
    {
        targetHalfSize = GetTransformBounds(SnapTarget()).size.z / 2;
        currentPreviewHalfSize = GetTransformBounds(transform).size.x / 2;
        if (IsCurrentPrefabOfType(PrefabType.Beam))
        {
            return BeamShiftDistance();
        }
        // TODO Move each prefab type to their specific partial class, to clean up this method
        
        if (IsCurrentPrefabOfType(PrefabType.Wall) && IsTargetPrefabOfType(PrefabType.Floor) || (IsCurrentPrefabOfType(PrefabType.Seam) && IsTargetPrefabOfType(PrefabType.Wall)))
        {
            return targetHalfSize;
        }
        else if (IsCurrentPrefabOfType(PrefabType.Floor))
        {
            if (IsTargetPrefabOfType(PrefabType.Wall))
            {
                return currentPreviewHalfSize;
            }
            else if (IsTargetPrefabOfType(PrefabType.Floor))
            {
                return currentPreviewHalfSize + targetHalfSize;
            }
        }
        return 0f;
    }

    private Bounds GetTransformBounds(Transform t)
    {
        var meshRenderer = t.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            return meshRenderer.bounds;
        }
        else
        {
            var boxCollider = t.GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                return t.GetComponent<BoxCollider>().bounds;
            }
            else
            {
                var bounds = new Bounds(transform.position, Vector3.one);
                foreach (Transform childTransform in t.transform)
                {
                    var childBounds = childTransform.GetComponent<BoxCollider>();
                    if (childBounds != null)
                        bounds.Encapsulate(childTransform.GetComponent<BoxCollider>().bounds);
                }
                return bounds;
            }

        }
    }

    private bool IsCurrentPrefabOfType(PrefabType type) => prefabType == type;
    private bool IsTargetPrefabOfType(PrefabType type) => GetTargetSnapper().prefabType == type;
    private Snapper GetTargetSnapper() => SnapTarget().GetComponent<Snapper>();
    #endregion

    #region Vertical shift
    private bool IsGroundFloor() => transform.position.y == 0;
    private Vector3 ShiftDownByHalfHeight() => -Vector3.up * GetTransformBounds(transform).size.y / 2;
    private Vector3 ShiftUpBySmallDelta() => Vector3.up * 0.005f;// In order to keep floor above wall
    #endregion

    #endregion

    #region Rotation from edge
    private Quaternion GetRotationFromEdge(Transform edge)
    {
        switch (prefabType)
        {
            
            case PrefabType.Floor: return transform.rotation;
            case PrefabType.Seam:
            case PrefabType.Beam:
            case PrefabType.Window: return edge.rotation;
            case PrefabType.Wall:
            default: return edge.rotation * Quaternion.Euler(0, 90, 0); ;
        }
    }
    #endregion

    #endregion

    public enum PrefabType
    {
        Floor,
        Wall,
        Window,
        Seam,
        Beam
    }
}
