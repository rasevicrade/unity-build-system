using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Snapper))]
public class SnapperEditor : Editor
{
    private Snapper snapper;

    private void OnEnable()
    {
        snapper = (Snapper)target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical();
        if (GUILayout.Button("Set defaults for prefab type"))
        {
            snapper.SetDefaults();
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

   
   
}
