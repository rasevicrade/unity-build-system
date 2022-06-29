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
    private GridPlacer gridPlacer;
    private bool isDrag;

    #region Lifecycle
    void OnSceneGUI()
    {
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        if (GetMousePosition(out RaycastHit hitInfo))
        {
            blueprint.activeMousePosition = hitInfo.point;
            OnShiftHandling(hitInfo);
            OnCtrlHandling(); 
            OnKeyUpHandling();

            if (previewController != null && preview != null)
            {
                SetFloor();
                if (targetObject == null)
                    previewController.UpdatePosition(new Vector3(hitInfo.point.x, blueprint.activeBaseHeight, hitInfo.point.z));
                else // If we have an active object, we show preview in place of it
                    previewController.UpdatePosition(targetObject.transform.position);

                if (IsLeftMouseButtonClicked(Event.current))
                {
                    Event.current.Use();
                    var activeGroup = prefabGroups[activePrefabGroupIndex];
                    if (activeGroup != null)
                    { 
                        var placedGO = blueprint.PlaceGameObject(activeGroup.Prefabs[activeGroup.activePrefabIndex], previewController.GetPosition(), previewController.GetRotation(), GetParent(activeGroup));
                        if (placedGO != null)
                        {
                            Undo.IncrementCurrentGroup();
                            Undo.RegisterCreatedObjectUndo(placedGO, placedGO.name);
                            Undo.SetCurrentGroupName("Place blueprint GO");
                        }
                        
                    }

                }
                if (Event.current.shift && IsRightMouseButtonClicked(Event.current))
                {
                    preview.transform.Rotate(0, 90, 0);
                }

            }
        }
       

        OnTabHandling();
    }
    #endregion

    private GameObject GetParent(PrefabGroup activeGroup)
    {
        var childGroup = blueprint.transform.Find(activeGroup.Name);
        if (childGroup != null)
        {
            return childGroup.gameObject;
        } 
        else
        {
            var newGroup = new GameObject(activeGroup.Name);
            newGroup.transform.parent = blueprint.transform;
            return newGroup;
        }
    }

    private void StartGridPlacement(RaycastHit hitInfo)
    {
        blueprint.floorStartPosition = previewController.isSnapped ? previewController.GetPosition() : hitInfo.point;
    }
    private void FinishGridPlacement(RaycastHit hitInfo)
    {
        blueprint.floorEndPosition = hitInfo.point;
        if (previewController.currentPrefabPreview != null)
        {
            GameObject wallPrefab = null;
            if (blueprint.addWallsToRooms)
                wallPrefab = prefabGroups.FirstOrDefault(x => x.Name == "Walls").Prefabs.FirstOrDefault(x => x.name == "Wall");

            SetActivePreview();
            var activeGroup = prefabGroups[activePrefabGroupIndex];
            var roomGO = gridPlacer.PlaceGrid(blueprint, activeGroup.Prefabs[activeGroup.activePrefabIndex], wallPrefab);
            roomGO.transform.parent = blueprint.transform;
            Undo.IncrementCurrentGroup();
            Undo.RegisterCreatedObjectUndo(roomGO, roomGO.name);
            Undo.SetCurrentGroupName("Room placed");
           
        }
        blueprint.showGridPreview = false;
    }
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
        if (targetObject != null)
        {
            targetObject.gameObject.SetActive(false);
            //previewController.UpdatePosition(targetObject.transform.position, true);
            //previewController.UpdateRotation(targetObject.transform.rotation);
        }
    }

 

    private void SetActivePreview()
    {
        if (preview != null)
            DestroyImmediate(preview);

        preview = previewController.CreatePreview(blueprint, Vector3.zero, prefabGroups[activePrefabGroupIndex].Prefabs[prefabGroups[activePrefabGroupIndex].activePrefabIndex], blueprint.activeScale);
       
    }
    #endregion


    #region Keyboard handling
    private void OnCtrlHandling()
    {
        if (Event.current.control)
        {
            if (Event.current.isScrollWheel)
            {
                ChangeActiveGroupIndex();
                Event.current.Use();
            }
        }
    }

    /// <summary>
    /// Shift + Mouse scroll changes active preview prefab within group (if there's an active object, it will replace that object)
    /// Shift + LMB drag starts a grid placement of selected prefab type, eg. floor
    /// </summary>
    /// <param name="hitInfo"></param>
    private void OnShiftHandling(RaycastHit hitInfo)
    {
        if (!Event.current.shift)
            return;

        if (Event.current.isScrollWheel)
        {
            ChangeActivePrefabIndex();
            Event.current.Use();
        }
        if (Event.current.type == EventType.MouseDrag)
        {
            isDrag = true;
            blueprint.showGridPreview = true;
        }
        if (Event.current.type == EventType.MouseDown)
        {
            StartGridPlacement(hitInfo);
            Event.current.Use();
        }
        else if (Event.current.type == EventType.MouseUp)
        {
            if (isDrag)
            {
                FinishGridPlacement(hitInfo);
                isDrag = false;
            }
                
        }
        
    }

    private void OnKeyUpHandling()
    {
        if (Event.current.type == EventType.KeyUp)
        {
            if (targetObject != null)
            {

                //ClearTargetObject();
            }

        }
    }

    private void OnTabHandling()
    {
        if (Event.current.keyCode == KeyCode.Tab)
        {
            if (!Event.current.shift)
            {
                DestroyImmediate(preview);
            }
            else
            {
                SetActivePreview();
            }

        }
    }
    #endregion

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
