using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Extension interface for righthand side menu of Blueprint prefab
/// </summary>
[CustomEditor(typeof(Blueprint))]
public partial class BlueprintEditor : Editor
{
    private Blueprint blueprint;
    
    private GameObject preview;
    private GUIStyle labelStyle;
    private int toolbarInt = 0;
    private MaterialEditor materialEditor;
    private string[] toolbarStrings = { "Settings", "Prefabs" };

    protected void OnEnable()
    {
        blueprint = (Blueprint)target;
        previewController = FindObjectOfType<PreviewController>();
        gridPlacer = FindObjectOfType<GridPlacer>();
        RefreshPrefabs();
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        Handles.BeginGUI();

        toolbarInt = GUILayout.Toolbar(toolbarInt, toolbarStrings);
        if (toolbarInt == 0)
        {
            EditorGUILayout.BeginVertical();
            labelStyle = new GUIStyle(EditorStyles.label);
            for (int i = 0; i < blueprint.prefabGroups.Count; i++)
            {
                var prefabGroup = blueprint.prefabGroups[i];
                labelStyle.normal.textColor = i == blueprint.activePrefabGroupIndex ? Color.green : Color.white;

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(prefabGroup.Name, labelStyle);
                prefabGroup.activePrefabIndex = EditorGUILayout.Popup(prefabGroup.activePrefabIndex, Array.ConvertAll(prefabGroup.Prefabs, x => x.name));

                if (prefabGroup.Material != null)
                {
                    if (materialEditor != null)
                    {
                        DestroyImmediate(materialEditor);
                    }
                    materialEditor = (MaterialEditor)Editor.CreateEditor(prefabGroup.Material);
                        if (materialEditor != null)
                        {
                            materialEditor.DrawHeader();
                        }
                    
                   

                }
                
                prefabGroup.Material = (Material)EditorGUILayout.ObjectField(prefabGroup.Material, typeof(Material), true);
                if (GUILayout.Button("+"))
                {
                    blueprint.activePrefabGroupIndex = i;
                    CreatePreview();
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
                foreach (var group in blueprint.prefabGroups)
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
            EditorGUILayout.LabelField("--------------------------------------------");
            EditorGUILayout.LabelField("Hold Shift and right click to place a room");
            EditorGUILayout.EndVertical();
            Handles.EndGUI();
        }

        
    }

    private void RefreshPrefabs()
    {
        var objectsPath = Application.dataPath + "/BuildSystem/Resources/Objects/";
        var dirInfo = new DirectoryInfo(objectsPath);
        blueprint.prefabGroups = new List<PrefabGroup>();
        int i = 0;
        foreach (var dir in dirInfo.GetDirectories())
        {
            blueprint.prefabGroups.Add(new PrefabGroup
            {
                Name = dir.Name,
                Prefabs = Resources.LoadAll<GameObject>("Objects/" + dir.Name).Where(x => x.GetComponent<Snapper>() != null).OrderBy(x => x.name).ToArray()
            });
            i++;
        }
    }
}
