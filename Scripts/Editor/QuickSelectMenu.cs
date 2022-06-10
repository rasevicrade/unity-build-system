using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class QuickSelectMenu : Editor
{
    protected void OnSceneGUI()
    {
        float btnHeight = 50;
        float btnPadding = 10;
        float btnY = btnPadding;

        Rect GetRect(float y) => new Rect(btnPadding, y, 100, 50);

        void AddBtn(string v, Action pressed)
        {
            if (GUI.Button(GetRect(btnY), v))
            {
                pressed();
            }
            btnY += (btnHeight + btnPadding);
        }

        Handles.BeginGUI();
        AddBtn("Select Foo", () => Select<GameObject>());
        AddBtn("Select Bar", () => Select<GameObject>());
        Handles.EndGUI();
    }

    private void Select<T>() where T : UnityEngine.Object
    {
        {
            Selection.activeObject = FindObjectOfType<T>();
        }
    }
}