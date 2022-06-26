using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Blueprint))]
public class BlueprintEditor : Editor
{
    private List<PrefabGroup> prefabGroups = new List<PrefabGroup>();
    private int activePrefabGroupIndex = 0;
    private Blueprint blueprint;
    private PreviewController previewController;
    private GameObject preview;
    private GUIStyle labelStyle;

    protected void OnEnable()
    {
        blueprint = (Blueprint)target;
        previewController = FindObjectOfType<PreviewController>();

        RefreshPrefabs();
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        Handles.BeginGUI();

        EditorGUILayout.BeginVertical();
        labelStyle = new GUIStyle(EditorStyles.label);
        for (int i = 0; i < prefabGroups.Count; i++)
        {
            var prefabGroup = prefabGroups[i];
            labelStyle.normal.textColor = i == activePrefabGroupIndex ? Color.green : Color.white;

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(prefabGroup.Name, labelStyle);
            prefabGroup.activePrefabIndex = EditorGUILayout.Popup(prefabGroup.activePrefabIndex, Array.ConvertAll(prefabGroup.Prefabs, x => x.name));
            if (GUILayout.Button("+"))
            {
                activePrefabGroupIndex = i;
                SetActivePreview();
            }
            if (GUILayout.Button("*"))
            {
                AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefabGroup.Prefabs[prefabGroup.activePrefabIndex])));
            }
            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("Remove preview"))
        {
            DestroyImmediate(preview);
        }
        if (GUILayout.Button("Refresh"))
        {
            RefreshPrefabs();
        }
        if (GUILayout.Button("Delete snappables")) //TODO Cleanup 
        {
            foreach (Transform child in blueprint.transform)
            {
                foreach (Transform subCHild in child.transform)
                {
                    if (subCHild.gameObject.layer == LayerMask.NameToLayer("Snappable"))
                    {
                        DestroyImmediate(subCHild.gameObject);
                    }
                }
            }
        }
        if (GUILayout.Button("Set all prefab defaults")) //TODO Cleanup 
        {
            foreach (var group in prefabGroups)
            {
                foreach (var prefab in group.Prefabs)
                {
                    var snapper = prefab.transform.GetComponent<Snapper>();
                    if (snapper != null)
                    {
                        snapper.SetDefaults();
                    }
                }
            }

        }
        EditorGUILayout.EndVertical();


        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Set layers")) //TODO Cleanup 
        {
            var layersSetup = new LayerSetup();
            if (!LayerSetup.LayerExists("Snappable"))
                layersSetup.AddNewLayer("Snappable");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField("Press TAB to clear current preview");
        EditorGUILayout.LabelField("Press Shift + TAB to reactivate last preview");
        EditorGUILayout.LabelField("--------------------------------------------");
        EditorGUILayout.LabelField("Shift + Right click to rotate object");
        EditorGUILayout.LabelField("--------------------------------------------");
        EditorGUILayout.LabelField("Ctrl + Scroll to change active object group");
        EditorGUILayout.LabelField("Shift + Scroll to change active prefab within group");
        EditorGUILayout.EndVertical();
        Handles.EndGUI();
    }

    void OnSceneGUI()
    {
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        if (GetRayCast(out RaycastHit hitInfo))
        {
            blueprint.activeMousePosition = hitInfo.point;
            if (Event.current.control)
            {
                if (Event.current.isScrollWheel)
                {
                    Event.current.Use();
                    ChangeActiveGroupIndex();
                }

            }
            else if (Event.current.shift)
            {
                if (Event.current.isScrollWheel)
                {
                    Event.current.Use();
                    ChangeActivePrefabIndex();
                }
                if (IsLeftMouseButtonClicked(Event.current))
                {
                    Event.current.Use();
                    blueprint.floorStartPosition = previewController.isSnapped ? previewController.GetPosition() : hitInfo.point;
                    blueprint.showGridPreview = true;
                }
                else if (Event.current.button == 0 && Event.current.type == EventType.MouseUp)
                {
                    Event.current.Use();
                    blueprint.floorEndPosition = hitInfo.point;
                    PlaceGrid();
                    blueprint.showGridPreview = false;
                }
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

    private void PlaceGrid()
    {
        Undo.IncrementCurrentGroup();
        var roomGO = new GameObject("Room");
        Undo.RegisterCreatedObjectUndo(roomGO, roomGO.name);
        Undo.SetCurrentGroupName("Room placed");
        roomGO.transform.parent = blueprint.transform;
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

    private void RefreshPrefabs()
    {
        var objectsPath = Application.dataPath + "/BuildSystem/Resources/Objects/";
        var dirInfo = new DirectoryInfo(objectsPath);
        prefabGroups = new List<PrefabGroup>();
        int i = 0;
        foreach (var dir in dirInfo.GetDirectories())
        {
            prefabGroups.Add(new PrefabGroup
            {
                Name = dir.Name,
                Prefabs = Resources.LoadAll<GameObject>("Objects/" + dir.Name).Where(x => x.GetComponent<Snapper>() != null).OrderBy(x => x.name).ToArray()
            });
            i++;
        }
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

    private bool IsLeftMouseButtonClicked(Event current)
    {
        return current.button == 0 && current.type == EventType.MouseDown;
    }

    private bool IsRightMouseButtonClicked(Event current)
    {
        return current.button == 1 && current.type == EventType.MouseDown;
    }

    private bool GetRayCast(out RaycastHit hitInfo)
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

    class PrefabGroup
    {
        public string Name { get; set; }
        public GameObject[] Prefabs { get; set; }
        public int activePrefabIndex { get; set; } = 0;
    }
}
