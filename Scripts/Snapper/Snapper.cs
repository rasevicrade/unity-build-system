using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;
using System;

[ExecuteInEditMode]
public partial class Snapper : MonoBehaviour
{
    public PrefabType prefabType;
    public float snapDistance = 2f;

    public HorizontalFrontShiftDirection horizontalFrontShiftDirection;
    public HorizontalShiftDistance myFrontShiftDistance;
    public List<TargetPrefabShiftDistancePair> targetsWithShift;

    public RotationType rotationType;

    public bool shiftSideways;
    public HorizontalShiftDistance mySideWaysShiftDistance;
    public HorizontalShiftDistance targetSideWaysShiftDistance;

    public bool shiftDown;
    public bool isPreview;

    private Blueprint blueprint;
    private PreviewController previewController;
    private Transform snappedEdge;
    private Transform snappedTarget;
    private Snapper snappedTargetSnapper;
    private float currentPreviewHalfSize;
    private float targetHalfSize;
    private Vector3 originalPrefabPosition;

    #region Debug
    private Vector3 textLocation;
    private string text;
    private void OnDrawGizmos()
    {
        textLocation = transform.position;
        Handles.Label(textLocation, text);
    }
    #endregion

    #region Lifecycle methods
    private void OnEnable()
    {
        previewController = FindObjectOfType<PreviewController>();
        if (previewController == null)
            Debug.LogError("No preview controller available in scene");
        blueprint = FindObjectOfType<Blueprint>();
        if (blueprint == null)
            Debug.LogError("No blueprint available in scene");
        if (targetsWithShift == null)
            targetsWithShift = new List<TargetPrefabShiftDistancePair>();
    }

    void Update()
    {
        text = "";
        originalPrefabPosition = transform.position;
        // If I am a preview, find edges to snap to
        if (isPreview)
        {
            if (!previewController.isSnapped)
            {
                snappedEdge = FindClosestOverlappingEdge();
                if (snappedEdge != null)
                {
                    text = snappedEdge.name + " - " + snappedEdge.parent.name;
                    snappedTarget = SnapTarget();
                    snappedTargetSnapper = snappedTarget.GetComponent<Snapper>();
                    Snap(snappedEdge);
                    snappedTarget = null;
                    snappedEdge = null;
                }
            }
        }
    }
    #endregion

    private void Snap(Transform edge)
    {
        previewController.UpdateRotation(GetRotationFromEdge(edge));
        previewController.UpdatePosition(PositionFromEdge(edge), snap: true);
    }

    #region Position from edge
    /// <summary>
    /// When we hit an edge of a placed object, depending of target object and current object types
    /// we will move the preview by first setting it to same location as placed object
    /// and then shifting it first horizontally to the edge of placed object
    /// and then vertically if needed (eg. on floor type when above ground, needs to shift down
    /// to fit within wall)
    /// </summary>
    /// <param name="edge"></param>
    /// <returns></returns>
    private Vector3 PositionFromEdge(Transform edge)
    {
        return SnapTargetPosition() + HorizontalShift(edge) + VerticalShift();
    }

    #region Horizontal shift
    private Vector3 HorizontalShift(Transform edge)
    {
        return FrontShift(edge) + SideWaysShift(edge);
    }

    private Vector3 SideWaysShift(Transform edge)
    {
        if (IsCurrentPrefabOfType(PrefabType.Door))
        {
            return -edge.right * GetTransformBounds(transform).LongerSideLength() / 2;
        }
        return (shiftSideways && IsTargetLongerThanCurrent(edge)) ? SideDirection(edge) * SideShitfDistance(edge) : Vector3.zero;
    }

    private bool IsTargetLongerThanCurrent(Transform edge)
    {
        var isTargetLonger = GetTransformBounds(edge.parent).LongerSideLength() - GetTransformBounds(transform).ShorterSideLength() > 0.5f; // We use shorterside length for current because we need shorter side of bounrs for sstable support, maybe find a better way to solve this

        if (isTargetLonger)
        {
            Debug.Log("Current" + GetTransformBounds(transform).ShorterSideLength());
            Debug.Log("Target" + GetTransformBounds(edge.parent).LongerSideLength());
        }
        return isTargetLonger;
    }

    /// <summary>
    /// Depending on prefab type, return how much active object needs to move horizontally
    /// to fit the target object
    /// </summary>
    /// <returns></returns>
    private Vector3 FrontShift(Transform edge)
    {
        Vector3 direction = Vector3.zero;
        if (horizontalFrontShiftDirection == HorizontalFrontShiftDirection.Forward)
            direction = edge.forward;
        else if (horizontalFrontShiftDirection == HorizontalFrontShiftDirection.Backward)
            direction = -edge.forward;

        targetHalfSize = GetTransformBounds(snappedTarget).ShorterSideLength() / 2; //TODO Temporary solution for walls, to get front facing size
        currentPreviewHalfSize = GetTransformBounds(transform).size.x / 2;

        var shiftDistance = 0f;
        if (myFrontShiftDistance == HorizontalShiftDistance.Half)
            shiftDistance += currentPreviewHalfSize;
        else if (myFrontShiftDistance == HorizontalShiftDistance.Full)
            shiftDistance += currentPreviewHalfSize * 2;
        else if (myFrontShiftDistance == HorizontalShiftDistance.ShorterHalf)
            shiftDistance += GetTransformBounds(transform).ShorterSideLength() / 2;

        var targetShiftDistance = targetsWithShift.First(x => x.prefabType == snappedTargetSnapper.prefabType).targetsShiftDistance;
        if (targetShiftDistance == HorizontalShiftDistance.Half)
            shiftDistance += targetHalfSize;
        else if (targetShiftDistance == HorizontalShiftDistance.Full)
            shiftDistance += targetHalfSize * 2;
        else if (targetShiftDistance == HorizontalShiftDistance.ShorterHalf)
            shiftDistance += GetTransformBounds(snappedTarget).ShorterSideLength() / 2;
        else if (targetShiftDistance == HorizontalShiftDistance.NegativeShorterHalf)
            shiftDistance -= GetTransformBounds(snappedTarget).ShorterSideLength() / 2;

        return direction * shiftDistance;
    }

    private float SideShitfDistance(Transform edge)
    {
        float sidewayShift = 0f;
        if (mySideWaysShiftDistance == HorizontalShiftDistance.Half)
            sidewayShift += GetTransformBounds(transform).ShorterSideLength() / 2;
        else if (mySideWaysShiftDistance == HorizontalShiftDistance.Full)
            sidewayShift += GetTransformBounds(transform).ShorterSideLength();


        if (targetSideWaysShiftDistance == HorizontalShiftDistance.Half)
            sidewayShift += GetTransformBounds(edge).LongerSideLength() / 2;
        else if (targetSideWaysShiftDistance == HorizontalShiftDistance.Full)
            sidewayShift += GetTransformBounds(edge).LongerSideLength();

        return sidewayShift;
    }
    private Vector3 SideDirection(Transform edge)
    {
        var min = GetTransformBounds(edge).min.HeightIgnored();
        var max = GetTransformBounds(edge).max.HeightIgnored();

        if (Mathf.Abs(min.x - max.x) > Mathf.Abs(max.z - min.z))
        {
            return Vector3.Distance(new Vector3(min.x, 0, 0), originalPrefabPosition) < Vector3.Distance(new Vector3(max.x, 0, 0), originalPrefabPosition) ? edge.right : -edge.right;
        }
        else
        {
            return Vector3.Distance(new Vector3(0, 0, min.z), originalPrefabPosition) < Vector3.Distance(new Vector3(0, 0, max.z), originalPrefabPosition) ? -edge.right : edge.right;
        }
    }
    private Vector3 SnapTargetPosition() => new Vector3(snappedTarget.position.x, transform.position.y, snappedTarget.position.z);
    private Transform SnapTarget() => snappedEdge.GetComponent<Snapper>() != null ? snappedEdge : snappedEdge.parent;

    private Bounds GetTransformBounds(Transform t)
    {
        var boxCollider = t.GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            return t.GetComponent<BoxCollider>().bounds;
        }

        else
        {
            var meshRenderer = t.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                return meshRenderer.bounds;
            }
            else
            {
                var renderers = t.GetComponentsInChildren<Renderer>();
                if (renderers != null && renderers.Length > 0)
                {
                    var childBounds = renderers[0].bounds;
                    foreach (Renderer r in renderers)
                    {
                        childBounds.Encapsulate(r.bounds);
                    }
                    return childBounds;
                }
                Debug.LogError("Couldn't find bounds for " + t.name);
                return new Bounds(transform.position, Vector3.zero);

            }

        }
    }

    private bool IsCurrentPrefabOfType(PrefabType type) => prefabType == type;
    #endregion

    #region Vertical shift
    private Vector3 VerticalShift()
    {
        return RequiresVerticalShift() ? ShiftDownByHeight() + ShiftUpBySmallDelta() : Vector3.zero;
    }

    /// <summary>
    /// If an object is placed on an upper floor and it's kind of object that needs to snap within walls, we need to pull it down vertically
    /// </summary>
    /// <returns></returns>
    private bool RequiresVerticalShift()
    {
        return !IsGroundFloor() && shiftDown;
    }
    private bool IsGroundFloor() => transform.position.y == 0;
    private Vector3 ShiftDownByHeight() => -Vector3.up * GetTransformBounds(transform).size.y;
    private Vector3 ShiftUpBySmallDelta() => Vector3.up * 0.005f;// In order to keep floor above wall
    #endregion

    #region Rotation from edge
    private Quaternion GetRotationFromEdge(Transform edge)
    {
        if (rotationType == RotationType.TargetEdge)
            return edge.rotation;
        else if (rotationType == RotationType.TargetRoot)
            return edge.parent.rotation;
        else
            return transform.rotation;
    }
    public enum RotationType
    {
        None,
        TargetRoot,
        TargetEdge
    }
    #endregion

    #endregion

    #region Find edges to snap to
    private Transform FindClosestOverlappingEdge()
    {
        var target = Physics.OverlapBox(transform.position + Vector3.up * GetTransformBounds(transform).size.y / 2, GetTransformBounds(transform).extents, transform.rotation, LayerMask.GetMask("Snappable"))
            .Where(x => CanSnap(x.transform))
            .OrderBy(x => Vector3.Distance(x.transform.position, transform.position))
            .FirstOrDefault();
        return target?.transform;
    }

    private bool CanSnap(Transform targetTransform)
    {
        if (targetTransform.parent == null)
            return false;
        var parentSnapper = targetTransform.parent.GetComponent<Snapper>();
        return parentSnapper != null && targetsWithShift.Any(x => x.prefabType == parentSnapper.prefabType);
    }
    #endregion

    public enum PrefabType
    {
        Floor,
        Wall,
        Window,
        Seam,
        Beam,
        SideRoof,
        Door,
        Stairs,
        WallDecoration
    }

    public enum HorizontalFrontShiftDirection
    {
        Forward,
        Backward,
        None
    }

    public enum HorizontalShiftDistance
    {
        None,
        Half,
        Full,
        ShorterHalf,
        NegativeShorterHalf
    }

    [System.Serializable]
    public class TargetPrefabShiftDistancePair
    {
        public PrefabType prefabType;
        public HorizontalShiftDistance targetsShiftDistance;

        public TargetPrefabShiftDistancePair(PrefabType prefabType, HorizontalShiftDistance targetsShiftDistance = HorizontalShiftDistance.None)
        {
            this.prefabType = prefabType;
            this.targetsShiftDistance = targetsShiftDistance;
        }

    }
}
