using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CaptureSnapshotHelper))]
public class CaptureSnapshotHelperEditor : Editor {
	private CaptureSnapshotHelper behaviour;
	private SerializedProperty callbackProp;

	void OnEnable() {
		behaviour = target as CaptureSnapshotHelper;
		callbackProp = serializedObject.FindProperty("callback");
	}

	public override void OnInspectorGUI() {
		// draw 'callback' property
		EditorGUIUtility.LookLikeControls();
		EditorGUILayout.PropertyField(callbackProp);
		if (GUI.changed)
			serializedObject.ApplyModifiedProperties();
		// draw tips
		EditorGUILayout.HelpBox("You can invoke Snap() from other scripts or click next button.", MessageType.Info, true);
		GUI.enabled = Application.isPlaying;
		// draw button
		if (GUILayout.Button("Call Snap()")) {
			behaviour.Snap();
		}
		GUI.enabled = true;		
	}
}
