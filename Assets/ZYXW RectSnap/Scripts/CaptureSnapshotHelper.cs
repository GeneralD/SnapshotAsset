using System;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(CaptureSnapshot))]
public class CaptureSnapshotHelper : MonoBehaviour {
	[Serializable]
	public class SnapshotTakenCallback : UnityEvent<Texture> {
	}

	public SnapshotTakenCallback callback;
	private CaptureSnapshot captureSnapshot;

	void Start() {
		captureSnapshot = GetComponent<CaptureSnapshot>();
	}

	/// <summary>
	/// Please add callback and call this method.
	/// </summary>
	public void Snap() {
		captureSnapshot.TakeSnapshot(callback.Invoke);
	}
}
