using System;
using System.Collections;
using System.IO;
using UnityEngine;

#region Extensions
/// <summary>
/// Rect transform extensions.
/// </summary>
public static class RectTransformUtilityExtensions {
	/// <summary>
	/// Gets the percentaged rect.
	/// </summary>
	/// <returns>The percentaged rect.</returns>
	/// <param name="rectTransform">Rect transform.</param>
	public static Rect GetPercentagedRect(this RectTransform rectTransform) {
		var r = rectTransform.GetWorldRect();
		var canvasRect = rectTransform.GetRootRectTransform().GetWorldRect();
		return r.ScaleDevide(canvasRect.width, canvasRect.height);
	}

	/// <summary>
	/// Gets the canvas.
	/// </summary>
	/// <returns>The canvas.</returns>
	/// <param name="rectTransform">Rect transform.</param>
	public static RectTransform GetRootRectTransform(this RectTransform rectTransform) {
		var canvas = rectTransform.GetComponentInParent<Canvas>();
		return canvas ? canvas.transform as RectTransform : null;
	}

	/// <summary>
	/// Gets the percentaged rect.
	/// </summary>
	/// <returns>The percentaged rect.</returns>
	/// <param name="rectTransform">Rect transform.</param>
	public static Rect GetWorldRect(this RectTransform rectTransform) {
		var corners = new Vector3[4];
		rectTransform.GetWorldCorners(corners);
		var tl = corners [0];
		var sub = corners [2] - tl;
		return new Rect(tl.x, tl.y, sub.x, sub.y);
	}
}

/// <summary>
/// Rect extensions.
/// </summary>
public static class RectScalingExtensions {
	/// <summary>
	/// Scale the specified rect.
	/// </summary>
	/// <param name="r">this</param>
	/// <param name="x">The horizontal scaling.</param>
	/// <param name="y">The y virtical scaling.</param>
	public static Rect Scale(this Rect r, float x, float y) {
		return new Rect(r.x * x, r.y * y, r.width * x, r.height * y);
	}

	/// <summary>
	/// Scale the specified rect.
	/// </summary>
	/// <param name="r">this</param>
	/// <param name="x">The horizontal scaling.</param>
	/// <param name="y">The y virtical scaling.</param>
	public static Rect ScaleDevide(this Rect r, float x, float y) {
		return new Rect(r.x / x, r.y / y, r.width / x, r.height / y);
	}
}

/// <summary>
/// Texture crop extentions.
/// </summary>
public static class TextureCropExtentions {
	/// <summary>
	/// Crop the specified texture with percentagedRect.
	/// </summary>
	/// <param name="texture">Texture.</param>
	/// <param name="percentagedRect">Percentaged rect.</param>
	/// <param name="act">callback</param>
	public static IEnumerator Crop(this Texture texture, Rect percentagedRect, Action<Texture> act) {
		var w = texture.width;
		var h = texture.height;
		var snap = new Texture2D((int)(percentagedRect.width * w), (int)(percentagedRect.height * h), TextureFormat.ARGB32, false);
		yield return new WaitForEndOfFrame(); // We should only read the screen buffer after rendering is complete
		snap.ReadPixels(percentagedRect.Scale(w, h), 0, 0);
		snap.Apply();
		act(snap);
	}

	/// <summary>
	/// Crop the specified texture, with percentagedRect.
	/// </summary>
	/// <param name="texture">Texture.</param>
	/// <param name="coroutineOwner">Coroutine owner.</param>
	/// <param name="percentagedRect">Percentaged rect.</param>
	/// <param name="act">callback</param>
	public static void Crop(this Texture texture, MonoBehaviour coroutineOwner, Rect percentagedRect, Action<Texture> act) {
		coroutineOwner.StartCoroutine(texture.Crop(percentagedRect, act));
	}
}
#endregion
/// <summary>
/// Capture snapshot behaviour
/// </summary>
public class CaptureSnapshot : MonoBehaviour {
	[SerializableAttribute]
	public enum Mode {
		Recommended,
		ForceUseScreenshot,
		ForceUseRenderTexture,
	}

	[SerializableAttribute]
	public enum UIHideModeWhenUsingScreenshot {
		DontHide,
		InvisibleCanvas,
		InvisibleTargetRectUI,
	}

	private static readonly float DelayAfterApplicationScreenshotTaken = .3f;
	public RectTransform targetArea;
	public Camera sourceCamera;
	public Mode mode = Mode.Recommended;
	public UIHideModeWhenUsingScreenshot uiHideModeWhenUsingScreenshot;
	private Action<Texture> onMyPostRender;
	private bool processing;

	void Start() {
		Camera.onPostRender += MyPostRender;
	}

	void OnDestroy() {
		Camera.onPostRender -= MyPostRender;
	}

	private void MyPostRender(Camera camera) {
		if (onMyPostRender != null) {
			UseRenderTexture(onMyPostRender);
			onMyPostRender = null; // null as flag
		}
	}

	/// <summary>
	/// Takes a snapshot.
	/// </summary>
	/// <param name="onSnapshotTaken">On snapshot taken delegate.</param>
	public void TakeSnapshot(Action<Texture> onSnapshotTaken) {
		// check state
		if (processing) {
			Debug.LogWarning("CaptureSnapshot behaviour has been processing now...");
			return; // failed to start new task
		} 
		processing = true;
		// request to start new task
		Action<Texture> act = t => t.Crop(this, targetArea.GetPercentagedRect(), cropped => {
			onSnapshotTaken(cropped);
			processing = false;
		});
		if (ShouldUseRenderTexture())
			onMyPostRender = act;
		else
			StartCoroutine(UseApplicationScreenshot(act));
	}

	private void UseRenderTexture(Action<Texture> action) {
		Debug.Log("Using RenderTexture API");
		var canvasRT = targetArea.GetRootRectTransform();
		var sw = canvasRT.rect.width;
		var sh = canvasRT.rect.height;

		// renderer entire screen (sight of the camera) to a texture
		var renderTexture = new RenderTexture((int)sw, (int)sh, 24, RenderTextureFormat.ARGB32);
		if (!renderTexture.IsCreated())
			renderTexture.Create();
		
		// set a RenderTexture to the camera rendering target
		RenderTexture.active = renderTexture;
		sourceCamera.targetTexture = renderTexture;
		sourceCamera.Render();
		action.Invoke(renderTexture);
		// release
		RenderTexture.active = null;
		sourceCamera.targetTexture = null;
		Destroy(renderTexture);
	}

	private IEnumerator UseApplicationScreenshot(Action<Texture> action) {
		var canvasRT = targetArea.GetRootRectTransform();
		var sw = canvasRT.rect.width;
		var sh = canvasRT.rect.height;

		// invisible UI temporary
		var canvas = canvasRT.gameObject.GetComponent<Canvas>();
		var defaultVisibility = true;
		var defaultAlpha = 0f;
		switch (uiHideModeWhenUsingScreenshot) {
			case UIHideModeWhenUsingScreenshot.InvisibleCanvas:
				if (canvas != null) {
					defaultVisibility = canvas.enabled;
					canvas.enabled = false;
				}
				break;
			case UIHideModeWhenUsingScreenshot.InvisibleTargetRectUI:
				var canvasRenderer = targetArea.GetComponent<CanvasRenderer>();
				if (canvasRenderer) {
					defaultAlpha = canvasRenderer.GetAlpha();
					canvasRenderer.SetAlpha(0f);
				}
				break;
		}

		// create png image of entire screen
		var name = "temp_screenshot.png";
		var path = string.Format("{0}/{1}", Application.persistentDataPath, name);
		// doubly sure, check temp location
		if (File.Exists(path))
			File.Delete(path);
		while (File.Exists(path))
			yield return new WaitForEndOfFrame();
		// write temp image file
		#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
		Application.CaptureScreenshot(name);
		#else
		Application.CaptureScreenshot(path);
		#endif
		Debug.Log("saving file: " + path);
		while (!File.Exists(path))
			yield return new WaitForEndOfFrame();
		yield return new WaitForSeconds(DelayAfterApplicationScreenshotTaken); // delay

		// revert UI visibility
		switch (uiHideModeWhenUsingScreenshot) {
			case UIHideModeWhenUsingScreenshot.InvisibleCanvas:
				if (canvas != null)
					canvas.enabled = defaultVisibility;
				break;
			case UIHideModeWhenUsingScreenshot.InvisibleTargetRectUI:
				var canvasRenderer = targetArea.GetComponent<CanvasRenderer>();
				if (canvasRenderer) {
					canvasRenderer.SetAlpha(defaultAlpha);
				}
				break;
		}

		// load that to a texture
		var screenshotTexture = new Texture2D((int)sw, (int)sh, TextureFormat.ARGB32, false);
		screenshotTexture.LoadImage(File.ReadAllBytes(path));
		screenshotTexture.Apply();

		action.Invoke(screenshotTexture);
		// cleanup
		Destroy(screenshotTexture);
		File.Delete(path);
		Debug.Log("removing file: " + path);
		while (File.Exists(path))
			yield return new WaitForEndOfFrame();
	}

	/// <summary>
	/// Should use RenderTexture.
	/// RenderTexture doesn't work on some platform. (e.g. on Android device)
	/// </summary>
	/// <returns><c>true</c>, if should use render texture, <c>false</c> otherwise.</returns>
	private bool ShouldUseRenderTexture() {
		var result = true;
		switch (mode) {
			case Mode.ForceUseRenderTexture:
				result = true;
				break;
			case Mode.ForceUseScreenshot:
				result = false;
				break;
			case Mode.Recommended:
				#if UNITY_EDITOR
				result = true;
				#elif UNITY_ANDROID || UNITY_IOS
				result = false;
				#else
				result = true;
				#endif
				break;
		}
		return result;
	}

	void OnDrawGizmos() {
		if (targetArea != null)
			Gizmos.DrawIcon(targetArea.GetWorldRect().center, "snap_gizmo.png", false);
	}
}
