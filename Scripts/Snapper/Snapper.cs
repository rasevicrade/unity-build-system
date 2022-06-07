using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[ExecuteInEditMode]
public partial class Snapper : MonoBehaviour
{
    public PrefabType prefabType;
    public List<PrefabType> allowedTargets;
    public float snapDistance = 1f; 
    public bool isPreview;
    public bool canShiftWhenSnapped;
    public Bounds snapRail;
    public Transform snappedEdge;

    private PreviewController previewController;

    private float currentPreviewHalfSize;
    private Transform activeSnapTarget;
    private float targetHalfSize;
   
    

    #region Lifecycle methods
    private void OnEnable()
    {
        previewController = FindObjectOfType<PreviewController>();
        if (previewController == null)
            Debug.LogError("No preview controller available in scene");
        if (allowedTargets == null)
            allowedTargets = new List<PrefabType>();
    }

    void Update()
    {
        // If I am a preview, find edges to snap to
        if (isPreview && !previewController.isSnapped)
        {
            snappedEdge = FindClosestOverlappingEdge();
            if (snappedEdge != null)
            {
                activeSnapTarget = SnapTarget();
                snapRail = GetTransformBounds(snappedEdge);
                Snap(snappedEdge);
            }
        }
    }
    #endregion

    private void Snap(Transform edge)
    {
        previewController.UpdateRotation(GetRotationFromEdge(edge), true);
        previewController.UpdatePosition(PositionFromEdge(edge), true, snapDistance);            
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
        return edge.forward * ShiftDistance();
    }
    /// <summary>
    /// Depending on prefab type, return how much active object needs to move horizontally
    /// to fit the target object
    /// </summary>
    /// <returns></returns>
    private float ShiftDistance()
    {
        var targetBounds = GetTransformBounds(activeSnapTarget);
        targetHalfSize = targetBounds.ShorterSideLength() / 2; //TODO Temporary solution for walls, to get front facing size
        currentPreviewHalfSize = GetTransformBounds(transform).size.x / 2;

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
        else if (IsCurrentPrefabOfType(PrefabType.SideRoof))
        {
            if (IsTargetPrefabOfType(PrefabType.Beam))
            {
                return GetTransformBounds(transform).ShorterSideLength() / 2 - GetTransformBounds(activeSnapTarget).ShorterSideLength() / 2;
            }
        }
        return 0f;
    }
    private Vector3 SnapTargetPosition() => new Vector3(activeSnapTarget.position.x, transform.position.y, activeSnapTarget.position.z);
    private Transform SnapTarget() => snappedEdge.GetComponent<Snapper>() != null ? snappedEdge : snappedEdge.parent;
    
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
                var renderers = t.GetComponentsInChildren<Renderer>();
                if (renderers != null && renderers.Length > 0)
                {
                    var childBounds = renderers[0].bounds;
                    foreach(Renderer r in renderers)
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
    private bool IsTargetPrefabOfType(PrefabType type) => GetTargetSnapper().prefabType == type;
    private Snapper GetTargetSnapper() => activeSnapTarget.GetComponent<Snapper>();
    #endregion

    #region Vertical shift
    private Vector3 VerticalShift()
    {
        return RequiresVerticalShift() ? ShiftDownByHalfHeight() + ShiftUpBySmallDelta() : Vector3.zero;
    }

    /// <summary>
    /// If an object is placed on an upper floor and it's kind of object that needs to snap within walls, we need to pull it down vertically
    /// </summary>
    /// <returns></returns>
    private bool RequiresVerticalShift()
    {
        return (prefabType == PrefabType.Floor || prefabType == PrefabType.Seam) && !IsGroundFloor();
    }
    private bool IsGroundFloor() => transform.position.y == 0;
    private Vector3 ShiftDownByHalfHeight() => -Vector3.up * GetTransformBounds(transform).size.y / 2;
    private Vector3 ShiftUpBySmallDelta() => Vector3.up * 0.005f;// In order to keep floor above wall
    #endregion

    #region Rotation from edge
    private Quaternion GetRotationFromEdge(Transform edge)
    {
        switch (prefabType)
        {
            case PrefabType.Floor: return transform.rotation;
            case PrefabType.Seam:
            case PrefabType.Beam: return edge.rotation;
            case PrefabType.SideRoof: return edge.parent.rotation * Quaternion.Euler(0, 90, 0); ;
            case PrefabType.Window: return edge.parent.rotation;
            
            case PrefabType.Wall:
            default: return edge.rotation * Quaternion.Euler(0, 90, 0); 
        }
    }
    #endregion

    #endregion

    #region Find edges to snap to

    private Transform FindClosestOverlappingEdge()
    {
        var target = Physics.OverlapBox(transform.position, GetTransformBounds(transform).extents, transform.rotation, LayerMask.GetMask("Snappable"))
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
        return parentSnapper != null && allowedTargets.Contains(parentSnapper.prefabType);
    }
    #endregion

    public enum PrefabType
    {
        Floor,
        Wall,
        Window,
        Seam,
        Beam,
        SideRoof
    }
}
