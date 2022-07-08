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
    
    private MaterialEditor[] materialEditors;
    private Renderer selectedRenderer;

    #region Lifecycle
    protected void OnEnable()
    {
        blueprint = (Blueprint)target;
        materialEditors = new MaterialEditor[blueprint.prefabGroups.Count];
        previewController = FindObjectOfType<PreviewController>();
        gridPlacer = FindObjectOfType<GridPlacer>();

        RefreshPrefabs();
        RefreshMaterials();
    }

    public override void OnInspectorGUI()
    {
        Handles.BeginGUI();
        SetTab();  
    }
    #endregion

    #region Tab selection
    private string[] tabNames = { "Prefabs", "Settings" };
    private int activeTab = 0;
    private void SetTab()
    {
        activeTab = GUILayout.Toolbar(activeTab, tabNames);
        if (activeTab == 0)
            PrefabsTab();
        else
            SettingsTab();
    }
    #endregion

    #region Prefabs tab and methods
    private void PrefabsTab()
    {
        if (!MaterialEditorArrayLongEnough())
            InitMaterialEditorArray();
        
        for (int groupIndex = 0; groupIndex < blueprint.prefabGroups.Count; groupIndex++)
            AddPrefabGroupMenu(groupIndex);
    }

    private void AddPrefabGroupMenu(int groupIndex)
    {
        var prefabGroup = blueprint.prefabGroups[groupIndex];

        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField(prefabGroup.Name, GroupLabelStyle(groupIndex));

        EditorGUI.BeginChangeCheck();
        AddActivePrefabSelector(blueprint.prefabGroups[groupIndex], groupIndex);
        AddCustomMaterialSelector(blueprint.prefabGroups[groupIndex], groupIndex);
   
        DrawUILine(Color.gray);
        EditorGUILayout.EndVertical();
    }

    private void AddCustomMaterialSelector(PrefabGroup prefabGroup, int groupIndex)
    {
        // Active custom material row
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Set custom material: ");
        prefabGroup.Material = (Material)EditorGUILayout.ObjectField(prefabGroup.Material, typeof(Material), true);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (EditorGUI.EndChangeCheck())
            SetMaterialEditor(groupIndex);

        if (IsMaterialEditorSet(groupIndex))
            ShowMaterialEditor(groupIndex);

        EditorGUILayout.EndHorizontal();
    }

    private void AddActivePrefabSelector(PrefabGroup prefabGroup, int groupIndex)
    {
        // Active prefab row
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Active prefab: ");
        prefabGroup.activePrefabIndex = EditorGUILayout.Popup(prefabGroup.activePrefabIndex, Array.ConvertAll(prefabGroup.Prefabs, x => x.name));
        if (GUILayout.Button("+"))
        {
            blueprint.activePrefabGroupIndex = groupIndex;
            CreatePreview();
        }
        if (GUILayout.Button("*"))
        {
            AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefabGroup.Prefabs[prefabGroup.activePrefabIndex])));
        }
        EditorGUILayout.EndHorizontal();
    }

    private void ShowMaterialEditor(int groupIndex)
    {
        EditorGUILayout.LabelField("Active prefab material: ");
        materialEditors[groupIndex].DrawHeader();
    }

    private bool IsMaterialEditorSet(int groupIndex)
    {
        return materialEditors[groupIndex] != null && selectedRenderer != null;
    }

    private bool IsActiveGroup(int groupIndex)
    {
        return groupIndex == blueprint.activePrefabGroupIndex;
    }

    private bool MaterialEditorArrayLongEnough()
    {
        return blueprint.prefabGroups.Count <= materialEditors.Length;
    }
   
    private void InitMaterialEditorArray()
    {
        materialEditors = new MaterialEditor[blueprint.prefabGroups.Count];
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
    #endregion
    
    #region Settings tab and methods
    private void SettingsTab()
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
    #endregion

    #region UI Helpers
    private GUIStyle labelStyle;
    private GUIStyle GroupLabelStyle(int groupIndex)
    {
        if (labelStyle == null)
            labelStyle = new GUIStyle(EditorStyles.label);
        labelStyle.normal.textColor = IsActiveGroup(groupIndex) ? Color.green : Color.white;
        labelStyle.alignment = TextAnchor.MiddleCenter;
        return labelStyle;
    }

    private static void DrawUILine(Color color, int thickness = 2, int padding = 10)
    {
        Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
        r.height = thickness;
        r.y += padding / 2;
        r.x -= 2;
        r.width += 6;
        EditorGUI.DrawRect(r, color);
    }
    #endregion
}
