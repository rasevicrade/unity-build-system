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
    private bool isLeftMouseClicked;
    private Snapper targetObject;
    private Material targetMaterial;
    private Material originalTargetMaterial;

    void OnSceneGUI()
    {
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        if (GetMousePosition(out RaycastHit hitInfo))
        {
            blueprint.activeMousePosition = hitInfo.point;
            OnCtrlHandling();
            OnShiftHandling(hitInfo);
            OnKeyUpHandling();
                        
            blueprint.showGridPreview = isLeftMouseClicked && isDrag;

            if (previewController != null && preview != null)
            {
                SetFloor();
                if (targetObject == null)
                    previewController.UpdatePosition(new Vector3(hitInfo.point.x, blueprint.activeBaseHeight, hitInfo.point.z));
                else
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
        if (Event.current.shift)
        {
            if (Event.current.isScrollWheel)
            {
                ChangeActivePrefabIndex();
                Event.current.Use();
            }
            if (IsLeftMouseButtonClicked(Event.current))
            {
                Event.current.Use();
                blueprint.floorStartPosition = previewController.isSnapped ? previewController.GetPosition() : hitInfo.point;
                isLeftMouseClicked = true;
                isDrag = false;


                if (IsReplacementModeActive()) // If we already are replacing, we can't set another active object
                {
                    ReplaceActiveObject();
                }
                else
                {
                    SetTargetObject(hitInfo);
                }
            }
            else if (IsLeftMouseButtonReleased(Event.current) && isDrag)
            {
                Event.current.Use();
                FinishGridPlacement(hitInfo);
                isDrag = false;
            }
            if (Event.current.type == EventType.MouseDrag)
            {
                Event.current.Use();
                isDrag = true;
            }

        }
    }


    private void OnKeyUpHandling()
    {
        if (Event.current.type == EventType.KeyUp)
        {
            if (targetObject != null)
                targetObject.gameObject.SetActive(true);
        }
    }

    private void OnTabHandling()
    {
        if (Event.current.keyCode == KeyCode.Tab)
        {
            if (!Event.current.shift)
                DestroyImmediate(preview);
            else
                SetActivePreview();
        }
    }

    private bool IsReplacementModeActive()
    {
        var activeGroup = prefabGroups[activePrefabGroupIndex];
        return targetObject != null && activeGroup.Prefabs[activeGroup.activePrefabIndex] != null;
    }

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

    private void OnDestroy()
    {
        DestroyImmediate(targetMaterial);
    }

    private void SetTargetObject(RaycastHit hitInfo)
    {
        if (targetObject != null)
        {
            targetObject.gameObject.GetComponent<Renderer>().sharedMaterial = originalTargetMaterial;
            targetObject = null;
        } 
        else
        {
            targetObject = hitInfo.transform.GetComponent<Snapper>();
            blueprint.selectedObject = targetObject;
            if (targetObject != null)
            {
                // Remember the original material and set color to green
                targetMaterial.color = Color.green;
                targetMaterial.SetColor("_BaseColor", Color.green);
                originalTargetMaterial = targetObject.gameObject.GetComponent<Renderer>().sharedMaterial;
                targetObject.gameObject.GetComponent<Renderer>().sharedMaterial = targetMaterial;

                // Set active group to the group of target object
                var group = prefabGroups.FirstOrDefault(x => x.Prefabs.Any(p => p.name == targetObject.name));
                if (group != null)
                {
                    activePrefabGroupIndex = prefabGroups.IndexOf(group); 
                }
            }
        }
        
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

    private void ReplaceActiveObject()
    {
        var activeGroup = prefabGroups[activePrefabGroupIndex];
        blueprint.PlaceGameObject(activeGroup.Prefabs[activeGroup.activePrefabIndex], targetObject.transform.position, targetObject.transform.rotation, GetParent(activeGroup));
        DestroyImmediate(targetObject); 
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
