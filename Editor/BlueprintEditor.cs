using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Blueprint))]
public class BlueprintEditor : Editor
{
    private GameObject[] prefabs;
    private int prefabIndex;   
    private Blueprint blueprint;
    private PreviewController previewController;
    private int activeHeight;
    
    private GameObject preview;

    protected void OnEnable()
    {
        blueprint = (Blueprint)target;
        previewController = FindObjectOfType<PreviewController>();
        prefabs = Resources.LoadAll<GameObject>("").Where(x => x.GetComponent<Snapper>() != null).ToArray();
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        Handles.BeginGUI();
        EditorGUILayout.BeginHorizontal();

        prefabIndex = EditorGUILayout.Popup(prefabIndex, Array.ConvertAll(prefabs, x => x.name));
        if (GUILayout.Button("Create preview"))
        {
            if (preview != null)
                DestroyImmediate(preview);
            preview = previewController.CreatePreview(blueprint, Vector3.zero, prefabs[prefabIndex]);
        }
        if (GUILayout.Button("Remove preview"))
        {
            DestroyImmediate(preview);
        }
        EditorGUILayout.EndHorizontal();
        Handles.EndGUI();
    }

    void OnSceneGUI()
    {       
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        if (previewController != null && preview != null && GetRayCast(out RaycastHit hitInfo))
        {
            SetFloor(hitInfo.point);
            previewController.UpdatePosition(new Vector3(hitInfo.point.x, activeHeight, hitInfo.point.z));
            

            if (IsLeftMouseButtonClicked(Event.current))
            {
                Event.current.Use();
                blueprint.PlaceGameObject(prefabs[prefabIndex], previewController.GetPosition(), previewController.GetRotation()); 
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
            else if (Event.current.keyCode == KeyCode.Alpha1)
            {
                activeHeight = 5;
            }
        }
    }

    private bool IsLeftMouseButtonClicked(Event current)
    {
        return current.button == 0 && current.type == EventType.MouseDown;
    }

    private bool GetRayCast(out RaycastHit hitInfo)
    {
        if (!IsMouseOverSceneView(out Vector2 mousePos))
        {
            hitInfo = new RaycastHit();
            return false;
        }

        Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);
        return Physics.Raycast(ray, out hitInfo, 10000f);
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