using HarmonyLib;
using UnityEngine;

namespace BoobsRunnerMod
{
	/// <summary>
	/// Shows "CGs: x/y" while the gallery panel is open.
	/// Unlock keys match vanilla: Running_|Sliding_|Ledge_|Game Over_ + 0..8.
	/// </summary>
	internal static class GalleryCgCounter
	{
		private static readonly string[] Prefixes =
		{
			"Running_",
			"Sliding_",
			"Ledge_",
			"Game Over_",
		};

		// Vanilla PlayerScript.Start: for (i = 0; i <= 8; i++)
		private const int IndexMin = 0;
		private const int IndexMax = 8;

		internal static bool GalleryOpen;
		internal static GameObject GalleryRoot;

		private static GUIStyle _style;
		private static Texture2D _bg;

		internal static int TotalCount => Prefixes.Length * (IndexMax - IndexMin + 1);

		internal static int CountUnlocked()
		{
			int n = 0;
			for (int i = IndexMin; i <= IndexMax; i++)
			{
				foreach (var p in Prefixes)
				{
					string key = p + i;
					if (PlayerPrefs.HasKey(key) && PlayerPrefs.GetInt(key, 0) != 0)
						n++;
				}
			}
			return n;
		}

		internal static void OnGalleryToggled(GameObject gallery)
		{
			if (gallery == null)
				return;
			GalleryRoot = gallery;
			GalleryOpen = gallery.activeSelf;
		}

		internal static void Draw()
		{
			if (Plugin.ShowGalleryCgCounter == null || !Plugin.ShowGalleryCgCounter.Value)
				return;

			if (!GalleryOpen)
				return;

			if (GalleryRoot != null && GalleryRoot && !GalleryRoot.activeInHierarchy)
			{
				GalleryOpen = false;
				return;
			}

			int unlocked = CountUnlocked();
			int total = TotalCount;
			string text = $"CGs: {unlocked}/{total}";

			EnsureStyle();

			const float pad = 10f;
			Vector2 size = _style.CalcSize(new GUIContent(text));
			float w = size.x + pad * 2f;
			float h = size.y + pad;

			// Top-center of screen (gallery is full-ish UI; stays visible while open)
			float x = (Screen.width - w) * 0.5f;
			float y = 18f;

			var boxStyle = new GUIStyle(GUI.skin.box) { normal = { background = _bg } };
			GUI.Box(new Rect(x, y, w, h), GUIContent.none, boxStyle);
			GUI.Label(new Rect(x + pad, y + pad * 0.35f, w - pad, h), text, _style);
		}

		private static void EnsureStyle()
		{
			if (_style != null)
				return;

			_bg = new Texture2D(2, 2, TextureFormat.RGBA32, false);
			var c = new Color(0f, 0f, 0f, 0.55f);
			_bg.SetPixels(new[] { c, c, c, c });
			_bg.Apply(false, true);

			_style = new GUIStyle(GUI.skin.label)
			{
				fontSize = 22,
				fontStyle = FontStyle.Bold,
				normal = { textColor = Color.white },
				alignment = TextAnchor.MiddleCenter,
			};
		}

		internal static void Reset()
		{
			GalleryOpen = false;
			GalleryRoot = null;
		}
	}

	[HarmonyPatch(typeof(ButtonScript), nameof(ButtonScript.Gallery))]
	internal static class ButtonScriptGalleryPatch
	{
		static void Postfix(ButtonScript __instance)
		{
			try
			{
				if (__instance != null && __instance.gallery != null)
					GalleryCgCounter.OnGalleryToggled(__instance.gallery);
			}
			catch (System.Exception ex)
			{
				Plugin.Log.LogDebug($"Gallery toggle track: {ex.Message}");
			}
		}
	}

	// If the Gallery panel itself is the ButtonScript host that OnEnables
	[HarmonyPatch(typeof(ButtonScript), "OnEnable")]
	internal static class GalleryPanelOnEnableCgPatch
	{
		static void Postfix(ButtonScript __instance)
		{
			try
			{
				if (__instance == null)
					return;
				// When the gallery root enables, some builds put ButtonScript on "Gallery"
				if (__instance.name == "Gallery" || (__instance.gallery != null && __instance.gallery.activeInHierarchy))
				{
					var root = __instance.gallery != null ? __instance.gallery : __instance.gameObject;
					if (root != null && root.activeInHierarchy)
					{
						GalleryCgCounter.GalleryRoot = root;
						GalleryCgCounter.GalleryOpen = true;
					}
				}
			}
			catch
			{
				// ignore
			}
		}
	}
}
