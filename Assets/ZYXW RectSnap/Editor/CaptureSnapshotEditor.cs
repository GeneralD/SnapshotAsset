using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CaptureSnapshot))]
public sealed class CaptureSnapshotEditor : Editor {
	private CaptureSnapshot behaviour;
	private GameObject gameObject;
	private int radioIndex;
	private bool foldOut;
	private Texture texture;

	void OnEnable() {
		behaviour = target as CaptureSnapshot;
		gameObject = behaviour.gameObject;
	}

	public override void OnInspectorGUI() {
//		base.OnInspectorGUI();
	
		// Target Area
		behaviour.targetArea = behaviour.targetArea ?? gameObject.GetComponent<RectTransform>(); // default value
		behaviour.targetArea = EditorGUILayout.ObjectField("Target Area", behaviour.targetArea, typeof(RectTransform), true) as RectTransform;
				
		// Mode
		behaviour.mode = (CaptureSnapshot.Mode)EditorGUILayout.EnumPopup("Mode", behaviour.mode);
		// show tips
		#if UNITY_ANDROID || UNITY_IOS
		if (behaviour.mode == CaptureSnapshot.Mode.ForceUseRenderTexture)
			EditorGUILayout.HelpBox("RenderTexture API is not compatible with iOS or Android.", MessageType.Warning, true);
		#endif
		
		if (behaviour.mode == CaptureSnapshot.Mode.Recommended)
			radioIndex = GUILayout.Toolbar(radioIndex, new []{ "On Unity Editor", "On Target Platform" });
		
		// other settings
		if (behaviour.sourceCamera == null)
			behaviour.sourceCamera = Camera.main; // default value
		if (DisplayedSettingFlag(behaviour.mode)) {
			EditorGUILayout.HelpBox("'Source Camera' renders to a RenderTexture to create a snapshot. UI elements are not in the snapshot.", MessageType.Info, true);
			behaviour.sourceCamera = EditorGUILayout.ObjectField("Source Camera", behaviour.sourceCamera, typeof(Camera), true) as Camera;
		} else {
			EditorGUILayout.HelpBox("Creates a snapshot using Application.CaptureScreenshot().", MessageType.Info, true);
			behaviour.uiHideModeWhenUsingScreenshot = (CaptureSnapshot.UIHideModeWhenUsingScreenshot)EditorGUILayout.EnumPopup("UI Hidden Mode", behaviour.uiHideModeWhenUsingScreenshot);
			switch (behaviour.uiHideModeWhenUsingScreenshot) {
				case CaptureSnapshot.UIHideModeWhenUsingScreenshot.DontHide:
					EditorGUILayout.HelpBox("UI elements are in the snapshot. However, you can ignore them with change 'UI Hidden Mode' setting.", MessageType.Info, true);
					break;
				case CaptureSnapshot.UIHideModeWhenUsingScreenshot.InvisibleTargetRectUI:
					EditorGUILayout.HelpBox("Hides only 'Target Area' while taking a snapshot.", MessageType.Info, true);
					break;
				case CaptureSnapshot.UIHideModeWhenUsingScreenshot.InvisibleCanvas:
					EditorGUILayout.HelpBox("Hides entire canvas which 'Target Area' belongs to while taking a snapshot.", MessageType.Info, true);
					break;
			}
		}
		
		
		// Test functions
		EditorGUILayout.Space();
		if (foldOut = EditorGUILayout.Foldout(foldOut, "Test Functions")) {
			if (Application.isPlaying) {
				if (GUILayout.Button("Test Snap!"))
					behaviour.TakeSnapshot(t => {
						texture = t;
						Repaint(); // force occur OnInspectorGUI to draw texture
					});
				if (texture != null)
					EditorGUILayout.ObjectField("Test Snap", texture, typeof(Texture), false);
			} else
				EditorGUILayout.HelpBox("Test functions is available on playing.", MessageType.Info, true);
		} 
		
		if (gameObject.GetComponent<CaptureSnapshotHelper>() == null) {
			EditorGUILayout.Space();
			if (GUILayout.Button("Add Helper Component")) {
				gameObject.AddComponent<CaptureSnapshotHelper>();
			}
		}
	}

	private bool DisplayedSettingFlag(CaptureSnapshot.Mode mode) {
		var result = true;
		switch (mode) {
			case CaptureSnapshot.Mode.ForceUseRenderTexture:
				result = true;
				break;
			case CaptureSnapshot.Mode.ForceUseScreenshot:
				result = false;
				break;
			case CaptureSnapshot.Mode.Recommended:
				if (radioIndex == 0) {	
					#if UNITY_EDITOR
					result = true;
					#elif UNITY_ANDROID || UNITY_IOS
					result = false;
					#else
					result = true;
					#endif
				} else {
					#if UNITY_ANDROID || UNITY_IOS
					result = false;
					#else
					result = true;
					#endif
				}
				break;
		}
		return result;
	}
}
