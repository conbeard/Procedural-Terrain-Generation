using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.TerrainAPI;

[CustomEditor(typeof(UpdatableData), true)]
public class NewBehaviourScript : UnityEditor.Editor {

    public override void OnInspectorGUI() {
        base.OnInspectorGUI();
        UpdatableData updatableData = (UpdatableData) target;

        if (GUILayout.Button("Update")) {
            updatableData.NotifyUpdatedValues();
            EditorUtility.SetDirty(target);
        }
    }
}
