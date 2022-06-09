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
    private int prefabIndex;   
    private Blueprint blueprint;
    private PreviewController previewController;
    private float activeHeight;
    private GameObject preview;
    private Quaternion currentRotation;
    private Dictionary<string, GameObject[]> prefabGroups = new Dictionary<string, GameObject[]>();
    private string activePrefabGroup;

    protected void OnEnable()
    {
        blueprint = (Blueprint)target;
        previewController = FindObjectOfType<PreviewController>();
        currentRotation = Quaternion.identity;
        RefreshPrefabs();
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        Handles.BeginGUI();

        EditorGUILayout.BeginVertical();
        foreach (var prefabGroup in prefabGroups)
        {
            EditorGUILayout.BeginHorizontal();
            prefabIndex = EditorGUILayout.Popup(prefabIndex, Array.ConvertAll(prefabGroup.Value, x => x.name));
            if (GUILayout.Button("Create " + prefabGroup.Key.Substring(0, prefabGroup.Key.Length - 1)))
            {
                if (preview != null)
                    DestroyImmediate(preview);

                preview = previewController.CreatePreview(blueprint, Vector3.zero, prefabGroup.Value[prefabIndex], blueprint.activeScale);
                activePrefabGroup = prefabGroup.Key;
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
        EditorGUILayout.EndVertical();


        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Set layers")) //TODO Cleanup 
        {
            var layersSetup = new LayerSetup();
            if (!LayerSetup.LayerExists("Snappable"))
                layersSetup.AddNewLayer("Snappable");
        }
        EditorGUILayout.EndHorizontal();
        Handles.EndGUI();
    }

    private void RefreshPrefabs()
    {
        var objectsPath = Application.dataPath + "/BuildSystem/Resources/Objects/";
        var dirInfo = new DirectoryInfo(objectsPath);
        prefabGroups = new Dictionary<string, GameObject[]>();
        foreach (var dir in dirInfo.GetDirectories())
        {
            prefabGroups.Add(dir.Name, Resources.LoadAll<GameObject>("Objects/" + dir.Name).Where(x => x.GetComponent<Snapper>() != null).OrderBy(x => x.name).ToArray());
        }
    }

    void OnSceneGUI()
    {       
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        if (previewController != null && preview != null)
        {
            if (GetRayCast(out RaycastHit hitInfo))
            {
                SetFloor(hitInfo.point);
                previewController.UpdatePosition(new Vector3(hitInfo.point.x, activeHeight, hitInfo.point.z));


                if (IsLeftMouseButtonClicked(Event.current))
                {
                    Event.current.Use();
                    blueprint.PlaceGameObject(prefabGroups[activePrefabGroup][prefabIndex], previewController.GetPosition(), previewController.GetRotation());
                }
                if (IsRightMouseButtonClicked(Event.current))
                {
                    Event.current.Use();
                    currentRotation *= Quaternion.Euler(0, 90, 0);
                    previewController.UpdateRotation(currentRotation);
                }
            }
            if (Event.current.keyCode == KeyCode.LeftShift)
            {
                if (Input.GetAxis("Mouse ScrollWheel") != 0f) // forward
                {
                    Debug.Log(Input.GetAxis("Mouse ScrollWheel"));
                }
                
            }
        }
       

        if (Event.current.keyCode == KeyCode.Tab)
        {
            DestroyImmediate(preview);
        }
    }

    private void SetFloor(Vector3 position)
    {
        if (Event.current.shift)
        {
            if (Event.current.keyCode == KeyCode.Alpha0)
            {
                activeHeight = 0;
            } 
            else if (Event.current.keyCode == KeyCode.Alpha4)
            {
                activeHeight = 4 * blueprint.activeScale;
            }
            else if (Event.current.keyCode == KeyCode.Alpha6)
            {
                activeHeight = 6 * blueprint.activeScale;
            }
            else if (Event.current.keyCode == KeyCode.Alpha8)
            {
                activeHeight = 8 * blueprint.activeScale;
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
}
