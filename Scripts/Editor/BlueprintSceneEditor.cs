using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Handler for user input over scene for Blueprint prefab
/// </summary>
public partial class BlueprintEditor : Editor
{
    private PreviewController previewController;
    private bool isDrag;
    private bool isGridPlacement;

    void OnSceneGUI()
    {
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        if (GetMousePosition(out RaycastHit hitInfo))
        {
            blueprint.activeMousePosition = hitInfo.point;
            if (Event.current.isScrollWheel)
            {
                if (Event.current.control)
                {
                    ChangeActiveGroupIndex();
                    Event.current.Use();
                }
                else if (Event.current.shift)
                {
                    ChangeActivePrefabIndex();
                    Event.current.Use();
                }
            }

            if (Event.current.shift)
            {
                if (IsLeftMouseButtonClicked(Event.current))
                {
                    BeginGridPlacement(hitInfo);
                    isDrag = false;
                    Event.current.Use();
                }    
                else if (IsLeftMouseButtonReleased(Event.current) && isDrag)
                {
                    FinishGridPlacement(hitInfo);
                    isDrag = false;
                    Event.current.Use();   
                }
                if (Event.current.type == EventType.MouseDrag)
                {
                    isDrag = true;
                    Event.current.Use();
                }
            }
            blueprint.showGridPreview = isGridPlacement && isDrag;
           
            if (previewController != null && preview != null)
            {
                SetFloor();
                previewController.UpdatePosition(new Vector3(hitInfo.point.x, blueprint.activeBaseHeight, hitInfo.point.z));


                if (IsLeftMouseButtonClicked(Event.current))
                {
                    Event.current.Use();
                    var activeGroup = prefabGroups[activePrefabGroupIndex];
                    if (activeGroup != null)
                    {
                        Undo.IncrementCurrentGroup();
                        var placedGO = blueprint.PlaceGameObject(activeGroup.Prefabs[activeGroup.activePrefabIndex], previewController.GetPosition(), previewController.GetRotation());
                        Undo.RegisterCreatedObjectUndo(placedGO, placedGO.name);
                        Undo.SetCurrentGroupName("Place blueprint GO");
                    }

                }
                if (Event.current.shift && IsRightMouseButtonClicked(Event.current))
                {
                    preview.transform.Rotate(0, 90, 0);
                }

            }
        }


        if (Event.current.keyCode == KeyCode.Tab)
        {
            if (!Event.current.shift)
                DestroyImmediate(preview);
            else
                SetActivePreview();
        }
    }

  

    private void BeginGridPlacement(RaycastHit hitInfo)
    {
        blueprint.floorStartPosition = previewController.isSnapped ? previewController.GetPosition() : hitInfo.point;
        isGridPlacement = true;
    }

    private void FinishGridPlacement(RaycastHit hitInfo)
    {
        blueprint.floorEndPosition = hitInfo.point;
        if (previewController.currentPrefabPreview != null)
        {
            GameObject wallPrefab = null;
            if (blueprint.addWallsToRooms)
                wallPrefab = prefabGroups.FirstOrDefault(x => x.Name == "Walls").Prefabs.FirstOrDefault(x => x.name == "Wall");

            var roomGO = GridPlacer.Instance.PlaceGrid(blueprint, previewController.currentPrefabPreview, wallPrefab);
            roomGO.transform.parent = blueprint.transform;
            Undo.IncrementCurrentGroup();
            Undo.RegisterCreatedObjectUndo(roomGO, roomGO.name);
            Undo.SetCurrentGroupName("Room placed");
           
        }
        blueprint.showGridPreview = false;
    }

    #region Prefab selection
    private void ChangeActiveGroupIndex()
    {
        if (Event.current.delta.y < 0)
            activePrefabGroupIndex -= 1;
        else if (Event.current.delta.y > 0)
            activePrefabGroupIndex += 1;
        if (activePrefabGroupIndex < 0)
            activePrefabGroupIndex = 0;
        if (activePrefabGroupIndex >= prefabGroups.Count)
            activePrefabGroupIndex = 0;

        SetActivePreview();
    }

    private void ReplaceTargetSnappedObject(Snapper snapper)
    {
        //var targetGroup = prefabGroups.FirstOrDefault(x => x.Prefabs.FirstOrDefault(p => p.name + "-Placed" == snapper.transform.name) != null);

        //if (targetGroup != null)
        //{
        //    activePrefabGroupIndex = prefabGroups.IndexOf(targetGroup);
        //    ChangeActivePrefabIndex();

        //    var placedGO = blueprint.PlaceGameObject(targetGroup.Prefabs[prefabGroups[activePrefabGroupIndex].activePrefabIndex], snapper.transform.position, snapper.transform.rotation);
        //    DestroyImmediate(snapper.gameObject);
        //}

        ChangeActivePrefabIndex();
        var placedGO = blueprint.PlaceGameObject(prefabGroups[activePrefabGroupIndex].Prefabs[prefabGroups[activePrefabGroupIndex].activePrefabIndex], snapper.transform.position, snapper.transform.rotation);
        DestroyImmediate(snapper.gameObject);
    }

    private void ChangeActivePrefabIndex()
    {
        if (Event.current.delta.y < 0)
            prefabGroups[activePrefabGroupIndex].activePrefabIndex -= 1;
        else if (Event.current.delta.y > 0)
            prefabGroups[activePrefabGroupIndex].activePrefabIndex += 1;
        if (prefabGroups[activePrefabGroupIndex].activePrefabIndex < 0)
            prefabGroups[activePrefabGroupIndex].activePrefabIndex = 0;
        if (prefabGroups[activePrefabGroupIndex].activePrefabIndex >= prefabGroups[activePrefabGroupIndex].Prefabs.Length)
            prefabGroups[activePrefabGroupIndex].activePrefabIndex = 0;

        SetActivePreview();
    }

    private void SetActivePreview()
    {
        if (preview != null)
            DestroyImmediate(preview);

        preview = previewController.CreatePreview(blueprint, Vector3.zero, prefabGroups[activePrefabGroupIndex].Prefabs[prefabGroups[activePrefabGroupIndex].activePrefabIndex], blueprint.activeScale);
    }
    #endregion

    private void SetFloor()
    {
        if (Event.current.shift)
        {
            if (Event.current.keyCode == KeyCode.Alpha0)
            {
                blueprint.activeBaseHeight = 0;
            }
            else if (Event.current.keyCode == KeyCode.Alpha1)
            {
                blueprint.activeBaseHeight = blueprint.floorHeight * blueprint.activeScale;
            }
            else if (Event.current.keyCode == KeyCode.Alpha2)
            {
                blueprint.activeBaseHeight = blueprint.floorHeight * 2 * blueprint.activeScale;
            }
            else if (Event.current.keyCode == KeyCode.Alpha3)
            {
                blueprint.activeBaseHeight = blueprint.floorHeight * 3 * blueprint.activeScale;
            }
            else if (Event.current.keyCode == KeyCode.Alpha4)
            {
                blueprint.activeBaseHeight = blueprint.floorHeight * 4 * blueprint.activeScale;
            }
            else if (Event.current.keyCode == KeyCode.Alpha5)
            {
                blueprint.activeBaseHeight = (blueprint.floorHeight / 2) * blueprint.activeScale;
            }
            else if (Event.current.keyCode == KeyCode.Alpha6)
            {
                blueprint.activeBaseHeight = (blueprint.floorHeight + (blueprint.floorHeight / 2)) * blueprint.activeScale;
            }
        }
    }

    #region Mouse handling
    private bool IsLeftMouseButtonClicked(Event current) => current.button == 0 && current.type == EventType.MouseDown;
    private bool IsLeftMouseButtonReleased(Event current) => current.button == 0 && current.type == EventType.MouseUp;

    private bool IsRightMouseButtonClicked(Event current) => current.button == 1 && current.type == EventType.MouseDown;

    private bool GetMousePosition(out RaycastHit hitInfo)
    {
        if (!IsMouseOverSceneView(out Vector2 mousePos))
        {
            hitInfo = new RaycastHit();
            return false;
        }

        Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);
        return Physics.Raycast(ray, out hitInfo, 10000f, LayerMask.GetMask("Default"));
    }

    private bool IsMouseOverSceneView(out Vector2 mousePos)
    {
        mousePos = Event.current.mousePosition;
        if (mousePos.x < 0f || mousePos.y < 0f)
            return false;
        Rect swPos = SceneView.lastActiveSceneView.position;
        return !(mousePos.x > swPos.width) &&
               !(mousePos.y > swPos.height);
    }
    #endregion
}
