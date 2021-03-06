using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;

[ExecuteInEditMode]
public partial class Snapper : MonoBehaviour
{
    public SnapperDefaultsScriptable defaults;

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
    public Vector3 textLocation;
    public string text;
    private void OnDrawGizmos()
    {
        textLocation = transform.position;
        //Handles.Label(textLocation, text);
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
        if (defaults != null && defaults.targetsWithShift == null)
            defaults.targetsWithShift = new List<TargetPrefabShiftDistancePair>();
    }

    void Update()
    {
        if (defaults == null)
            return;
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
            return -edge.right * transform.GetBounds().LongerSideLength() / 2;
        }
        return (defaults.shiftSideways && IsTargetLongerThanCurrent(edge)) ? SideDirection(edge) * SideShitfDistance(edge) : Vector3.zero;
    }

    private bool IsTargetLongerThanCurrent(Transform edge)
    {
        return edge.parent.GetBounds().LongerSideLength() - transform.GetBounds().ShorterSideLength() > 0.5f; // We use shorterside length for current because we need shorter side of bounrs for sstable support, maybe find a better way to solve this
    }

    /// <summary>
    /// Depending on prefab type, return how much active object needs to move horizontally
    /// to fit the target object
    /// </summary>
    /// <returns></returns>
    private Vector3 FrontShift(Transform edge)
    {
        Vector3 direction = Vector3.zero;
        if (defaults.horizontalFrontShiftDirection == HorizontalFrontShiftDirection.Forward)
            direction = edge.forward;
        else if (defaults.horizontalFrontShiftDirection == HorizontalFrontShiftDirection.Backward)
            direction = -edge.forward;

        targetHalfSize = snappedTarget.GetBounds().ShorterSideLength() / 2; //TODO Temporary solution for walls, to get front facing size
        currentPreviewHalfSize = transform.GetBounds().size.x / 2;

        var shiftDistance = 0f;
        if (defaults.myFrontShiftDistance == HorizontalShiftDistance.Half)
            shiftDistance += currentPreviewHalfSize;
        else if (defaults.myFrontShiftDistance == HorizontalShiftDistance.Full)
            shiftDistance += currentPreviewHalfSize * 2;
        else if (defaults.myFrontShiftDistance == HorizontalShiftDistance.ShorterHalf)
            shiftDistance += transform.GetBounds().ShorterSideLength() / 2;

        var targetShiftDistance = defaults.targetsWithShift.First(x => x.prefabType == snappedTargetSnapper.defaults.prefabType).targetsShiftDistance;
        if (targetShiftDistance == HorizontalShiftDistance.Half)
            shiftDistance += targetHalfSize;
        else if (targetShiftDistance == HorizontalShiftDistance.Full)
            shiftDistance += targetHalfSize * 2;
        else if (targetShiftDistance == HorizontalShiftDistance.ShorterHalf)
            shiftDistance += snappedTarget.GetBounds().ShorterSideLength() / 2;
        else if (targetShiftDistance == HorizontalShiftDistance.NegativeShorterHalf)
            shiftDistance -= snappedTarget.GetBounds().ShorterSideLength() / 2;

        return direction * shiftDistance;
    }

    private float SideShitfDistance(Transform edge)
    {
        float sidewayShift = 0f;
        if (defaults.mySideWaysShiftDistance == HorizontalShiftDistance.Half)
            sidewayShift += transform.GetBounds().ShorterSideLength() / 2;
        else if (defaults.mySideWaysShiftDistance == HorizontalShiftDistance.Full)
            sidewayShift += transform.GetBounds().ShorterSideLength();


        if (defaults.targetSideWaysShiftDistance == HorizontalShiftDistance.Half)
            sidewayShift += edge.GetBounds().LongerSideLength() / 2;
        else if (defaults.targetSideWaysShiftDistance == HorizontalShiftDistance.Full)
            sidewayShift += edge.GetBounds().LongerSideLength();

        return sidewayShift;
    }
    private Vector3 SideDirection(Transform edge)
    {
        var min = edge.GetBounds().min.HeightIgnored();
        var max = edge.GetBounds().max.HeightIgnored();

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

    private bool IsCurrentPrefabOfType(PrefabType type) => defaults.prefabType == type;
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
        return !IsGroundFloor() && defaults.shiftDown;
    }
    private bool IsGroundFloor() => transform.position.y == 0;
    private Vector3 ShiftDownByHeight() => -Vector3.up * (defaults.shiftDownDistance != 0f ? defaults.shiftDownDistance : transform.GetBounds().size.y) ;
    private Vector3 ShiftUpBySmallDelta() => Vector3.up * 0.005f;// In order to keep floor above wall
    #endregion

    #region Rotation from edge
    private Quaternion GetRotationFromEdge(Transform edge)
    {
        if (defaults.rotationType == RotationType.TargetEdge)
            return edge.rotation;
        else if (defaults.rotationType == RotationType.TargetRoot)
            return edge.parent.rotation;
        else
            return transform.rotation;
    }
 
    #endregion

    #endregion

    #region Find edges to snap to
    private Transform FindClosestOverlappingEdge()
    {
        var target = Physics.OverlapBox(transform.position + Vector3.up * transform.GetBounds().size.y / 2, transform.GetBounds().extents, transform.rotation, LayerMask.GetMask("Snappable"))
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
        return parentSnapper != null && defaults.targetsWithShift.Any(x => x.prefabType == parentSnapper.defaults.prefabType);
    }
    #endregion

    #region Public helpers
    public string GetGroup()
    {
        var group = blueprint.prefabGroups.FirstOrDefault(x => x.Prefabs.Any(p => p.name == gameObject.name));
        return group.Name;
    }
    #endregion
}
