using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace BoobsRunnerMod.Patches
{
	/// <summary>
	/// Gallery fixes:
	/// 1) Detail viewer: square / preserveAspect.
	/// 2) Grid ScrollRect: grow content height for all rows without killing elastic overscroll.
	/// </summary>
	internal static class GalleryFixes
	{
		// Vanilla ImageRearrager.Rearrage first-row Y
		private const float VanillaFirstRowY = 100f;
		private const float BottomPad = 64f;

		internal static void FixDetailImage(Image img)
		{
			if (img == null || img.sprite == null)
				return;

			img.preserveAspect = true;

			var rt = img.rectTransform;
			if (rt == null)
				return;

			float w = rt.rect.width;
			float h = rt.rect.height;
			if (w <= 1f || h <= 1f)
			{
				w = Mathf.Abs(rt.sizeDelta.x);
				h = Mathf.Abs(rt.sizeDelta.y);
			}
			if (w <= 1f && h <= 1f)
				return;

			float side = Mathf.Min(w, h);
			Vector2 pivot = rt.pivot;
			Vector2 anchored = rt.anchoredPosition;

			bool stretchedX = !Mathf.Approximately(rt.anchorMin.x, rt.anchorMax.x);
			bool stretchedY = !Mathf.Approximately(rt.anchorMin.y, rt.anchorMax.y);

			if (stretchedX || stretchedY)
			{
				rt.anchorMin = new Vector2(0.5f, 0.5f);
				rt.anchorMax = new Vector2(0.5f, 0.5f);
				rt.pivot = new Vector2(0.5f, 0.5f);
				rt.sizeDelta = new Vector2(side, side);
				rt.anchoredPosition = Vector2.zero;
			}
			else
			{
				rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, side);
				rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, side);
				rt.pivot = pivot;
				rt.anchoredPosition = anchored;
			}
		}

		internal static void FixScrollContent(ImageRearrager rearrager)
		{
			if (rearrager == null)
				return;

			var content = rearrager.transform as RectTransform;
			if (content == null)
				return;

			var children = rearrager.children;
			if (children == null || children.Count == 0)
				return;

			float gapY = rearrager.gapY;
			const int cols = 4;
			int rows = (children.Count + cols - 1) / cols;

			// Measure cells (local to content)
			float minBottom = float.MaxValue;
			float maxTop = float.MinValue;
			float cellH = 0f;

			foreach (var t in children)
			{
				if (t == null)
					continue;
				var rt = t as RectTransform ?? t.GetComponent<RectTransform>();
				if (rt == null)
					continue;

				float h = rt.rect.height;
				if (h < 1f)
					h = Mathf.Abs(rt.sizeDelta.y);
				if (h < 1f)
					h = 100f;
				if (h > cellH)
					cellH = h;

				float top = rt.anchoredPosition.y + h * (1f - rt.pivot.y);
				float bottom = rt.anchoredPosition.y - h * rt.pivot.y;
				if (top > maxTop)
					maxTop = top;
				if (bottom < minBottom)
					minBottom = bottom;
			}

			if (minBottom > 0f && maxTop < minBottom)
				return;

			// Formula matching vanilla placement:
			// row i at y = -100 - gapY*i ; bottom of last row ≈ that - cellH*pivot (~ half if centered)
			float formulaHeight = VanillaFirstRowY
			                      + gapY * Mathf.Max(0, rows - 1)
			                      + cellH
			                      + BottomPad;

			// Bounds height: distance from content top (y=0) to lowest bottom
			// Children are laid out with negative Y under a top origin.
			float boundsHeight = -minBottom + BottomPad;
			if (maxTop > 0f)
				boundsHeight += maxTop; // rare: something above origin

			float neededHeight = Mathf.Max(formulaHeight, boundsHeight);

			var scroll = content.GetComponentInParent<ScrollRect>();
			RectTransform viewport = null;
			if (scroll != null)
			{
				viewport = scroll.viewport != null
					? scroll.viewport
					: scroll.transform as RectTransform;
			}
			if (viewport == null && content.parent is RectTransform parentRt)
				viewport = parentRt;

			float viewH = viewport != null ? viewport.rect.height : 0f;
			// Must exceed viewport to allow scrolling; keep a little slack for elastic bounce
			if (viewH > 1f)
				neededHeight = Mathf.Max(neededHeight, viewH + 1f);

			// --- Size only; do not clamp movement (keeps rubber-band Elastic) ---
			ApplyContentHeight(content, neededHeight);

			if (scroll != null)
			{
				if (scroll.content == null)
					scroll.content = content;

				scroll.vertical = true;
				// Restore / keep rubber-band overscroll (previous fix wrongly used Clamped)
				scroll.movementType = ScrollRect.MovementType.Elastic;
				scroll.elasticity = Mathf.Max(scroll.elasticity, 0.1f);

				Canvas.ForceUpdateCanvases();

				// Only snap to top when opening-ish (near top or first layout)
				// Avoid yanking if user already scrolled mid-list during a re-layout.
				if (scroll.verticalNormalizedPosition > 0.95f || scroll.verticalNormalizedPosition < 0f)
				{
					scroll.velocity = Vector2.zero;
					scroll.verticalNormalizedPosition = 1f;
				}
			}

			Plugin.Log.LogDebug(
				$"Gallery scroll height={neededHeight:0.#} (formula={formulaHeight:0.#}, bounds={boundsHeight:0.#}, view={viewH:0.#}, rows={rows})");
		}

		/// <summary>
		/// Grow content height without flipping anchors mid-scroll incorrectly.
		/// Top-pivot preferred so extra pixels extend downward (top rows stay reachable).
		/// </summary>
		private static void ApplyContentHeight(RectTransform content, float neededHeight)
		{
			if (neededHeight < 1f)
				return;

			// Prefer top-anchored vertical so sizeDelta.y == height and growth is downward.
			// Keep horizontal anchors/size as-is.
			float anchorMinX = content.anchorMin.x;
			float anchorMaxX = content.anchorMax.x;
			float pivotX = content.pivot.x;
			float posX = content.anchoredPosition.x;
			float sizeX = content.sizeDelta.x;

			bool wasVertStretch = content.anchorMin.y < 0.99f && content.anchorMax.y > 0.01f
			                      && !Mathf.Approximately(content.anchorMin.y, content.anchorMax.y);

			// Top-stretch width if it was full-width stretch; else keep X anchors
			bool fullWidth = Mathf.Approximately(anchorMinX, 0f) && Mathf.Approximately(anchorMaxX, 1f);

			content.anchorMin = new Vector2(fullWidth ? 0f : anchorMinX, 1f);
			content.anchorMax = new Vector2(fullWidth ? 1f : anchorMaxX, 1f);
			content.pivot = new Vector2(fullWidth ? 0.5f : pivotX, 1f);

			// Stick top edge to top of viewport/parent
			content.anchoredPosition = new Vector2(fullWidth ? 0f : posX, 0f);

			if (fullWidth)
			{
				// sizeDelta.x preserved as left/right inset (often 0)
				content.sizeDelta = new Vector2(sizeX, neededHeight);
			}
			else
			{
				content.sizeDelta = new Vector2(sizeX, neededHeight);
				content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, neededHeight);
			}

			// If we converted away from vertical stretch, ensure height actually applied
			if (wasVertStretch && content.rect.height + 1f < neededHeight)
			{
				content.sizeDelta = new Vector2(content.sizeDelta.x, neededHeight);
			}
		}
	}

	[HarmonyPatch(typeof(Immage), nameof(Immage.SetImage))]
	internal static class ImmageSetImagePatch
	{
		static void Postfix(Immage __instance)
		{
			try
			{
				GalleryFixes.FixDetailImage(__instance.GetComponent<Image>());
			}
			catch (System.Exception ex)
			{
				Plugin.Log.LogWarning($"Gallery detail aspect fix failed: {ex.Message}");
			}
		}
	}

	[HarmonyPatch(typeof(Immage), nameof(Immage.ChangeImage))]
	internal static class ImmageChangeImagePatch
	{
		static void Postfix(Immage __instance)
		{
			try
			{
				GalleryFixes.FixDetailImage(__instance.GetComponent<Image>());
			}
			catch (System.Exception ex)
			{
				Plugin.Log.LogWarning($"Gallery detail aspect fix failed: {ex.Message}");
			}
		}
	}

	[HarmonyPatch(typeof(ImageRearrager), nameof(ImageRearrager.Rearrage))]
	internal static class ImageRearrangePatch
	{
		static void Postfix(ImageRearrager __instance)
		{
			try
			{
				GalleryFixes.FixScrollContent(__instance);
			}
			catch (System.Exception ex)
			{
				Plugin.Log.LogWarning($"Gallery scroll fix failed: {ex.Message}");
			}
		}
	}

	[HarmonyPatch(typeof(ButtonScript), "OnEnable")]
	internal static class GalleryButtonOnEnablePatch
	{
		static void Postfix(ButtonScript __instance)
		{
			try
			{
				if (__instance == null || __instance.name != "Gallery")
					return;
				if (__instance.Images == null)
					return;
				var ir = __instance.Images.GetComponent<ImageRearrager>();
				if (ir == null)
					return;

				// Vanilla already calls Rearrage in OnEnable; we only need a late size pass.
				// Use end-of-frame so viewport.rect.height is valid.
				__instance.StartCoroutine(LateFix(ir));
			}
			catch (System.Exception ex)
			{
				Plugin.Log.LogDebug($"Gallery OnEnable: {ex.Message}");
			}
		}

		private static System.Collections.IEnumerator LateFix(ImageRearrager ir)
		{
			yield return null; // one frame — layout/viewport ready
			if (ir != null)
				GalleryFixes.FixScrollContent(ir);
		}
	}
}
