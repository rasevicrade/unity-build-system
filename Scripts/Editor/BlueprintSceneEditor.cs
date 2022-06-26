using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Handler for user input over scene for Blueprint prefab
/// </summary>
public partial class BlueprintEditor : Editor
{
    private PreviewController previewController;

    void OnSceneGUI()
    {
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        if (GetMousePosition(out RaycastHit hitInfo))
        {
            blueprint.activeMousePosition = hitInfo.point;
            if (Event.current.control && Event.current.isScrollWheel)
            {
                ChangeActiveGroupIndex();
                Event.current.Use();
            }
            else if (Event.current.shift)
            {
                if (Event.current.isScrollWheel)
                    ChangeActivePrefabIndex();

                if (IsLeftMouseButtonClicked(Event.current))
                    BeginGridPlacement(hitInfo);
                else if (Event.current.button == 0 && Event.current.type == EventType.MouseUp)
                    FinishGridPlacement(hitInfo);

                Event.current.Use();
            }
           
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

    private void FinishGridPlacement(RaycastHit hitInfo)
    {
        blueprint.floorEndPosition = hitInfo.point;
        PlaceGrid();
        blueprint.showGridPreview = false;
    }

    private void BeginGridPlacement(RaycastHit hitInfo)
    {
        blueprint.floorStartPosition = previewController.isSnapped ? previewController.GetPosition() : hitInfo.point;
        blueprint.showGridPreview = true;
    }

    private void PlaceGrid()
    { 
        var roomGO = new GameObject("Room");
        roomGO.transform.parent = blueprint.transform;
        Undo.IncrementCurrentGroup();
        Undo.RegisterCreatedObjectUndo(roomGO, roomGO.name);
        Undo.SetCurrentGroupName("Room placed");
        
        var floorsGO = new GameObject("Floors");
        floorsGO.transform.parent = roomGO.transform;
        var wallsGO = new GameObject("Walls");
        wallsGO.transform.parent = roomGO.transform;
        var activeGroup = prefabGroups[activePrefabGroupIndex];
        if (activeGroup != null)
        {
            var currentPosition = blueprint.floorStartPosition;
            var direction = (blueprint.floorEndPosition - blueprint.floorStartPosition).normalized;
            var totalX = Math.Round(Math.Abs(blueprint.floorStartPosition.x - blueprint.floorEndPosition.x) / 4);
            var totalZ = Math.Round(Math.Abs(blueprint.floorStartPosition.z - blueprint.floorEndPosition.z) / 4);
            var fx = (float)Math.Round(direction.x, 0);
            var fz = (float)Math.Round(direction.z, 0);

            var wallPrefab = prefabGroups.FirstOrDefault(x => x.Name == "Walls").Prefabs.FirstOrDefault(x => x.name == "Wall");
            for (int x = 0; x <= totalX; x++)
            {
                for (int z = 0; z <= totalZ; z++)
                {
                    var floor = blueprint.PlaceGameObject(activeGroup.Prefabs[activeGroup.activePrefabIndex], new Vector3(currentPosition.x, blueprint.activeBaseHeight * blueprint.activeScale, currentPosition.z), previewController.GetRotation());
                    floor.transform.parent = floorsGO.transform;
                    if (x == 0)
                    {
                        var wall = blueprint.PlaceGameObject(wallPrefab, new Vector3(currentPosition.x - fx * 2, blueprint.activeBaseHeight * blueprint.activeScale, currentPosition.z), previewController.GetRotation() * Quaternion.Euler(0, -90, 0));
                        wall.transform.parent = wallsGO.transform;
                    }
                    if (x == totalX)
                    {
                        var wall = blueprint.PlaceGameObject(wallPrefab, new Vector3(currentPosition.x + fx * 2, blueprint.activeBaseHeight * blueprint.activeScale, currentPosition.z), previewController.GetRotation() * Quaternion.Euler(0, -90, 0));
                        wall.transform.parent = wallsGO.transform;
                    }

                    if (z == 0)
                    {
                        var wall = blueprint.PlaceGameObject(wallPrefab, new Vector3(currentPosition.x, blueprint.activeBaseHeight * blueprint.activeScale, currentPosition.z - fz * 2), previewController.GetRotation());
                        wall.transform.parent = wallsGO.transform;
                    }

                    if (z == totalZ)
                    {
                        var wall = blueprint.PlaceGameObject(wallPrefab, new Vector3(currentPosition.x, blueprint.activeBaseHeight * blueprint.activeScale, currentPosition.z + fz * 2), previewController.GetRotation());
                        wall.transform.parent = wallsGO.transform;
                    }


                    currentPosition = new Vector3(currentPosition.x, blueprint.activeBaseHeight * blueprint.activeScale, currentPosition.z + 4 * fz);
                }

                currentPosition = new Vector3(currentPosition.x + 4 * fx, blueprint.activeBaseHeight * blueprint.activeScale, blueprint.floorStartPosition.z);
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
