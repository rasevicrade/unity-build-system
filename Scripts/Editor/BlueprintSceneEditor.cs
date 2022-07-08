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
            blueprint.activeMousePosition = new Vector3(hitInfo.point.x, blueprint.activeBaseHeight, hitInfo.point.z);
            ManageTarget(hitInfo);
            OnShiftHandling(hitInfo);
            OnCtrlHandling(); 

            if (previewController != null && preview != null)
            {
                SetFloor();
                if (activeTarget != null)
                {
                    previewController.UpdateRotation(activeTarget.target.transform.rotation);
                    previewController.UpdatePosition(activeTarget.target.transform.position, true);                   
                    
                }
                else
                {
                    previewController.UpdatePosition(blueprint.activeMousePosition);
                }
                    


                if (IsLeftMouseButtonClicked(Event.current))
                {
                    Event.current.Use();
                    var activeGroup = blueprint.prefabGroups[blueprint.activePrefabGroupIndex];
                    if (activeGroup != null)
                    { 
                        var placedGO = blueprint.PlaceGameObject(activeGroup.Prefabs[activeGroup.activePrefabIndex], previewController.GetPosition(), previewController.GetRotation(), GetParent(activeGroup), activeGroup.Material);
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
        if (previewController.IsPreviewActive())
        {
            var activeGroup = blueprint.prefabGroups[blueprint.activePrefabGroupIndex];
            gridPlacer.SetBaseObject(activeGroup.Prefabs[activeGroup.activePrefabIndex], activeGroup.Material);

            GameObject wallPrefab = null;
            if (blueprint.addWallsToRooms)
            {
                var wallGroup = blueprint.prefabGroups.FirstOrDefault(x => x.Name == "Walls");
                wallPrefab = wallGroup.Prefabs[wallGroup.activePrefabIndex];
                if (wallPrefab != null)
                    gridPlacer.SetSurroundingObject(wallPrefab, wallGroup.Material);
            }
               
            CreatePreview();
            var roomGO = gridPlacer.PlaceGrid(blueprint);
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
            blueprint.activePrefabGroupIndex -= 1;
        else if (Event.current.delta.y > 0)
            blueprint.activePrefabGroupIndex += 1;
        if (blueprint.activePrefabGroupIndex < 0)
            blueprint.activePrefabGroupIndex = 0;
        if (blueprint.activePrefabGroupIndex >= blueprint.prefabGroups.Count)
            blueprint.activePrefabGroupIndex = 0;

        CreatePreview();
    }

    private void ChangeActivePrefabIndex()
    {
        if (Event.current.delta.y < 0)
            blueprint.prefabGroups[blueprint.activePrefabGroupIndex].activePrefabIndex -= 1;
        else if (Event.current.delta.y > 0)
            blueprint.prefabGroups[blueprint.activePrefabGroupIndex].activePrefabIndex += 1;
        if (blueprint.prefabGroups[blueprint.activePrefabGroupIndex].activePrefabIndex < 0)
            blueprint.prefabGroups[blueprint.activePrefabGroupIndex].activePrefabIndex = 0;
        if (blueprint.prefabGroups[blueprint.activePrefabGroupIndex].activePrefabIndex >= blueprint.prefabGroups[blueprint.activePrefabGroupIndex].Prefabs.Length)
            blueprint.prefabGroups[blueprint.activePrefabGroupIndex].activePrefabIndex = 0;

        if (activeTarget != null)
            CreatePreview(activeTarget.target.transform.position, true);
        else
            CreatePreview();

        SetMaterialEditor(blueprint.activePrefabGroupIndex);
    }

    private void CreatePreview(Vector3? position = null, bool ignoreSnap = false)
    {
        if (preview != null)
            DestroyImmediate(preview);

        preview = previewController.CreatePreview(blueprint, position.HasValue ? position.Value : blueprint.activeMousePosition, blueprint.prefabGroups[blueprint.activePrefabGroupIndex].Prefabs[blueprint.prefabGroups[blueprint.activePrefabGroupIndex].activePrefabIndex], blueprint.activeScale, ignoreSnap);
        var renderer = preview.GetComponent<Renderer>();
        var customMaterial = blueprint.prefabGroups[blueprint.activePrefabGroupIndex].Material;
        if (renderer != null && customMaterial != null)
            preview.GetComponent<Renderer>().sharedMaterial = blueprint.prefabGroups[blueprint.activePrefabGroupIndex].Material;
    }

    private void DeletePreview()
    {
        DestroyImmediate(preview);
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

    private void OnTabHandling()
    {
        if (Event.current.keyCode == KeyCode.Tab)
        {
            if (!Event.current.shift)
            {
                DeletePreview();
            }
            else
            {
                CreatePreview();
            }

        }
    }
    #endregion

    #region Mouse handling
    private bool IsLeftMouseButtonClicked(Event current) => current.button == 0 && current.type == EventType.MouseDown;

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
