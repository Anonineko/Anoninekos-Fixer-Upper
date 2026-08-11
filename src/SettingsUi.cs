using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace BoobsRunnerMod
{
	/// <summary>
	/// Mod options: single IMGUI panel while the game Settings screen is open.
	/// (No Unity UI cloning — that was doubled up / broken.)
	/// </summary>
	internal static class ModSettingsUi
	{
		internal static GameObject TrackedSettingsRoot;
		internal static bool SettingsPanelOpen;

		private static Texture2D _panelTex;
		private static GUIStyle _boxStyle;
		private static GUIStyle _titleStyle;
		private static GUIStyle _labelStyle;
		private static GUIStyle _toggleStyle;
		private static Vector2 _scroll;

		private static readonly List<(string key, string label, BepInEx.Configuration.ConfigEntry<bool> entry)> Options =
			new List<(string, string, BepInEx.Configuration.ConfigEntry<bool>)>();

		internal static void EnsureOptionsList()
		{
			if (Options.Count > 0)
				return;

			Options.Add((ModPrefs.NoBenchWhenFullHp, "No bench when full HP", Plugin.NoBenchWhenFullHp));
			Options.Add((ModPrefs.BenchHighWhenLowHp, "Bench spawn high when low HP", Plugin.BenchHighWhenLowHp));
			Options.Add((ModPrefs.ForceDroneWhenNone, "Force photo drone if none", Plugin.ForceDroneWhenNone));
			Options.Add((ModPrefs.NoDroneWhenHaveOne, "No photo drone if already have one", Plugin.NoDroneWhenHaveOne));
			Options.Add((ModPrefs.DownDropsLedge, "Down arrow lets go of ledge", Plugin.DownDropsLedge));
			Options.Add((ModPrefs.ShowStageHud, "Show stage counter HUD", Plugin.ShowStageHud));
			Options.Add((ModPrefs.ShowGalleryCgCounter, "Show gallery CG counter", Plugin.ShowGalleryCgCounter));
		}

		internal static void ResetBuildFlags()
		{
			TrackedSettingsRoot = null;
			SettingsPanelOpen = false;
			GalleryCgCounter.Reset();
		}

		internal static void OnSettingsToggled(GameObject settings)
		{
			if (settings == null)
				return;

			TrackedSettingsRoot = settings;
			SettingsPanelOpen = settings.activeSelf;
			Plugin.Log.LogInfo($"Settings panel active={settings.activeSelf}");

			if (settings.activeSelf)
			{
				Plugin.SyncPrefsFromConfig();
				// Remove any leftover cloned Unity panels from older mod builds
				DestroyLegacyUnityPanels(settings.transform);
			}
		}

		private static void DestroyLegacyUnityPanels(Transform settingsRoot)
		{
			if (settingsRoot == null)
				return;
			try
			{
				// Exact name from old injector
				var old = settingsRoot.Find("BRMod_Options_Panel");
				if (old != null)
					Object.Destroy(old.gameObject);

				// Any BRMod_* clones under settings
				var all = settingsRoot.GetComponentsInChildren<Transform>(true);
				for (int i = 0; i < all.Length; i++)
				{
					var t = all[i];
					if (t == null || t == settingsRoot)
						continue;
					if (t.name != null && t.name.StartsWith("BRMod_"))
						Object.Destroy(t.gameObject);
				}
			}
			catch (System.Exception ex)
			{
				Plugin.Log.LogDebug($"Legacy settings UI cleanup: {ex.Message}");
			}
		}

		internal static void DrawImgui()
		{
			if (!SettingsPanelOpen)
				return;
			if (TrackedSettingsRoot == null || !TrackedSettingsRoot)
			{
				SettingsPanelOpen = false;
				return;
			}
			if (!TrackedSettingsRoot.activeInHierarchy)
			{
				SettingsPanelOpen = false;
				return;
			}

			EnsureOptionsList();
			EnsureStyles();

			float width = Mathf.Min(420f, Screen.width * 0.42f);
			float height = Mathf.Min(360f, Screen.height * 0.6f);
			float x = Screen.width - width - 16f;
			float y = (Screen.height - height) * 0.5f;

			GUI.depth = -1000;
			GUILayout.BeginArea(new Rect(x, y, width, height), _boxStyle);
			GUILayout.Label("Anonineko's Fixer Upper", _titleStyle);
			GUILayout.Space(4f);

			_scroll = GUILayout.BeginScrollView(_scroll, false, true);

			foreach (var opt in Options)
			{
				bool cur = Plugin.GetPref(opt.key, opt.entry.Value);
				bool next = GUILayout.Toggle(cur, "  " + opt.label, _toggleStyle, GUILayout.Height(28f));
				if (next != cur)
				{
					Plugin.SetOption(opt.key, next, opt.entry);
					Plugin.Log.LogInfo($"Option {opt.key} = {next}");
				}
			}

			GUILayout.EndScrollView();
			GUILayout.Space(4f);
			GUILayout.Label("Saved to BepInEx config as well.", _labelStyle);
			GUILayout.EndArea();
		}

		private static void EnsureStyles()
		{
			if (_boxStyle != null)
				return;

			_panelTex = MakeTex(2, 2, new Color(0.08f, 0.08f, 0.1f, 0.92f));

			_boxStyle = new GUIStyle(GUI.skin.box)
			{
				normal = { background = _panelTex, textColor = Color.white },
				padding = new RectOffset(12, 12, 10, 10),
			};

			_titleStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize = 18,
				fontStyle = FontStyle.Bold,
				normal = { textColor = Color.white },
				alignment = TextAnchor.MiddleLeft,
			};

			_labelStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize = 12,
				normal = { textColor = new Color(0.8f, 0.8f, 0.8f) },
				wordWrap = true,
			};

			_toggleStyle = new GUIStyle(GUI.skin.toggle)
			{
				fontSize = 14,
				normal = { textColor = Color.white },
				onNormal = { textColor = Color.white },
				hover = { textColor = Color.white },
				onHover = { textColor = Color.white },
				active = { textColor = Color.white },
				onActive = { textColor = Color.white },
				padding = new RectOffset(4, 4, 4, 4),
				margin = new RectOffset(0, 0, 4, 4),
			};
		}

		private static Texture2D MakeTex(int w, int h, Color col)
		{
			var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
			var px = new Color[w * h];
			for (int i = 0; i < px.Length; i++)
				px[i] = col;
			tex.SetPixels(px);
			tex.Apply(false, true);
			return tex;
		}
	}

	[HarmonyPatch(typeof(ButtonScript), nameof(ButtonScript.Settings))]
	internal static class ButtonScriptSettingsPatch
	{
		static void Postfix(ButtonScript __instance)
		{
			try
			{
				if (__instance.settings != null)
					ModSettingsUi.OnSettingsToggled(__instance.settings);
			}
			catch (System.Exception ex)
			{
				Plugin.Log.LogError($"Settings open hook failed: {ex}");
			}
		}
	}

	[HarmonyPatch(typeof(ButtonScript), nameof(ButtonScript.Restart))]
	internal static class RestartPatch
	{
		static void Prefix()
		{
			ModSettingsUi.ResetBuildFlags();
			DroneState.Clear();
		}
	}
}
