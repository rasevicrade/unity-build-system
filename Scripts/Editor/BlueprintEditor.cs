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
    private string[] toolbarStrings = { "Prefabs", "Settings"  };
    private MaterialEditor[] materialEditors;
    private Dictionary<string, Material> groupMaterials;
    private Renderer selectedRenderer;

    protected void OnEnable()
    {
        blueprint = (Blueprint)target;
        materialEditors = new MaterialEditor[blueprint.prefabGroups.Count];
        groupMaterials = new Dictionary<string, Material>();
        previewController = FindObjectOfType<PreviewController>();
        gridPlacer = FindObjectOfType<GridPlacer>();
        
        RefreshPrefabs();
        RefreshMaterials();
    }

    public override void OnInspectorGUI()
    {
        //base.OnInspectorGUI();
        Handles.BeginGUI();

        toolbarInt = GUILayout.Toolbar(toolbarInt, toolbarStrings);
        if (toolbarInt == 1)
        {
            ShowSettings();
        } else
        {
            ShowPrefabs();
        }

        
    }

    private void ShowPrefabs()
    {
        if (blueprint.prefabGroups.Count != materialEditors.Length)
        {
            materialEditors = new MaterialEditor[blueprint.prefabGroups.Count];
        }

        labelStyle = new GUIStyle(EditorStyles.label);
        for (int i = 0; i < blueprint.prefabGroups.Count; i++)
        {
            var prefabGroup = blueprint.prefabGroups[i];
            labelStyle.normal.textColor = i == blueprint.activePrefabGroupIndex ? Color.green : Color.white;

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(prefabGroup.Name, labelStyle);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();
            prefabGroup.activePrefabIndex = EditorGUILayout.Popup(prefabGroup.activePrefabIndex, Array.ConvertAll(prefabGroup.Prefabs, x => x.name));
            prefabGroup.Material = (Material)EditorGUILayout.ObjectField(prefabGroup.Material, typeof(Material), true);

            if (EditorGUI.EndChangeCheck())
            {
                SetMaterialEditor(i);
            }

            if (materialEditors[i] != null && selectedRenderer != null)
            {
                materialEditors[i].DrawHeader();
            }

            

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
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
            EditorGUILayout.EndVertical();
           
        }

    }

    private void SetMaterialEditor(int prefabGroupIndex)
    {
        Editor tmpEditor = null;
        var prefabGroup = blueprint.prefabGroups[prefabGroupIndex];
        if (prefabGroup.activePrefabIndex < prefabGroup.Prefabs.Length)
        {
            if (prefabGroup.Material != null)
            {
                tmpEditor = Editor.CreateEditor(prefabGroup.Material);
            } 
            else
            {
                var selectedPrefab = prefabGroup.Prefabs[prefabGroup.activePrefabIndex];
                selectedRenderer = selectedPrefab.GetComponent<Renderer>();
                if (selectedRenderer != null)
                {
                    var sharedMat = selectedRenderer.sharedMaterial;
                    tmpEditor = Editor.CreateEditor(sharedMat);
                }
            }
            
        }

        if (materialEditors[prefabGroupIndex] != null)
        {
            DestroyImmediate(materialEditors[prefabGroupIndex]);
        }

        materialEditors[prefabGroupIndex] = (MaterialEditor)tmpEditor;
    }

    private void ShowSettings()
    {
        EditorGUILayout.BeginVertical();
        
       
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

    private void RefreshMaterials()
    {
        var materialsPath = Application.dataPath + "/BuildSystem/Resources/Materials/";
        var dirInfo = new DirectoryInfo(materialsPath);
        blueprint.availableMaterials = new List<Material>();
        int i = 0;
        foreach(var dir in dirInfo.GetDirectories())
        {
            blueprint.availableMaterials.AddRange(Resources.LoadAll<Material>("Materials/" + dir.Name).OrderBy(x => x.name).ToArray());
            i++;
        }
    }
}
