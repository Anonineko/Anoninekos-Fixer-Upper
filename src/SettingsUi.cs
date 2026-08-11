using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BoobsRunnerMod
{
	/// <summary>
	/// Settings options for Anonineko's Fixer Upper.
	/// Primary UI: IMGUI panel while the game Settings object is open (always visible).
	/// Secondary: optional Unity UI rows under the settings root when possible.
	/// </summary>
	internal static class ModSettingsUi
	{
		internal static GameObject TrackedSettingsRoot;
		/// <summary>True only while the game Settings panel toggled by ButtonScript is open.</summary>
		internal static bool SettingsPanelOpen;
		private static bool _unityUiBuilt;
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
		}

		internal static void ResetBuildFlags()
		{
			_unityUiBuilt = false;
			TrackedSettingsRoot = null;
			SettingsPanelOpen = false;
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
				TryBuildUnityUi(settings.transform);
			}
		}

		internal static void DrawImgui()
		{
			// Only while pause Settings is open (not whenever SettingsInit exists).
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
			float height = Mathf.Min(320f, Screen.height * 0.55f);
			float x = Screen.width - width - 16f;
			float y = (Screen.height - height) * 0.5f;

			// Unscaled so it works while the game is paused (timeScale 0).
			var prev = Time.timeScale;
			// GUI is real-time; no need to change timeScale.

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
			GUILayout.Label("Also saved to BepInEx config.", _labelStyle);
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

		/// <summary>
		/// Best-effort Unity UI under the settings object (may still be clipped by game layout).
		/// IMGUI is the reliable surface; this is extra.
		/// </summary>
		private static void TryBuildUnityUi(Transform settingsRoot)
		{
			if (_unityUiBuilt)
			{
				// Re-show if we already built under this root
				var existing = settingsRoot.Find("BRMod_Options_Panel");
				if (existing != null)
				{
					existing.gameObject.SetActive(true);
					return;
				}
				_unityUiBuilt = false;
			}

			try
			{
				EnsureOptionsList();

				// Destroy stale panels anywhere under settings
				var old = settingsRoot.Find("BRMod_Options_Panel");
				if (old != null)
					Object.Destroy(old.gameObject);

				var panelGo = new GameObject("BRMod_Options_Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
				var panelRt = panelGo.GetComponent<RectTransform>();
				panelRt.SetParent(settingsRoot, false);
				panelRt.SetAsLastSibling();

				// Stretch to lower portion of settings root — high z-order sibling
				panelRt.anchorMin = new Vector2(0.05f, 0.02f);
				panelRt.anchorMax = new Vector2(0.95f, 0.42f);
				panelRt.offsetMin = Vector2.zero;
				panelRt.offsetMax = Vector2.zero;
				panelRt.localScale = Vector3.one;
				panelRt.localRotation = Quaternion.identity;

				var panelImg = panelGo.GetComponent<Image>();
				panelImg.color = new Color(0.05f, 0.05f, 0.08f, 0.88f);
				panelImg.raycastTarget = true;

				var vlg = panelGo.AddComponent<VerticalLayoutGroup>();
				vlg.padding = new RectOffset(10, 10, 8, 8);
				vlg.spacing = 6f;
				vlg.childAlignment = TextAnchor.UpperLeft;
				vlg.childControlWidth = true;
				vlg.childControlHeight = true;
				vlg.childForceExpandWidth = true;
				vlg.childForceExpandHeight = false;

				AddUiText(panelGo.transform, "Anonineko's Fixer Upper", 20, FontStyle.Bold, 28f);

				// Prefer cloning a real game Toggle if present (looks native)
				var template = settingsRoot.GetComponentInChildren<Toggle>(true);

				foreach (var opt in Options)
				{
					if (template != null)
						AddClonedToggle(panelGo.transform, template, opt.key, opt.label, opt.entry);
					else
						AddSimpleToggleRow(panelGo.transform, opt.key, opt.label, opt.entry);
				}

				_unityUiBuilt = true;
				Plugin.Log.LogInfo("Built Unity UI options panel under Settings.");
			}
			catch (System.Exception ex)
			{
				Plugin.Log.LogWarning($"Unity UI settings inject failed (IMGUI still works): {ex.Message}");
			}
		}

		private static Font GetUiFont()
		{
			// Unity 6 builtin font name varies; try common ones then any loaded font.
			Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
			if (f == null)
				f = Resources.GetBuiltinResource<Font>("Arial.ttf");
			if (f == null)
			{
				var fonts = Resources.FindObjectsOfTypeAll<Font>();
				if (fonts != null && fonts.Length > 0)
					f = fonts[0];
			}
			return f;
		}

		private static void AddUiText(Transform parent, string msg, int size, FontStyle style, float height)
		{
			var go = new GameObject("BRMod_Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
			go.transform.SetParent(parent, false);
			var le = go.AddComponent<LayoutElement>();
			le.minHeight = height;
			le.preferredHeight = height;

			var text = go.GetComponent<Text>();
			text.font = GetUiFont();
			text.text = msg;
			text.fontSize = size;
			text.fontStyle = style;
			text.color = Color.white;
			text.alignment = TextAnchor.MiddleLeft;
			text.horizontalOverflow = HorizontalWrapMode.Wrap;
			text.verticalOverflow = VerticalWrapMode.Truncate;
			text.raycastTarget = false;
		}

		private static void AddClonedToggle(
			Transform parent,
			Toggle template,
			string key,
			string label,
			BepInEx.Configuration.ConfigEntry<bool> entry)
		{
			var row = Object.Instantiate(template.gameObject, parent, false);
			row.name = "BRMod_" + key;
			row.SetActive(true);

			var rt = row.GetComponent<RectTransform>();
			if (rt != null)
			{
				rt.localScale = Vector3.one;
				rt.localRotation = Quaternion.identity;
				rt.anchorMin = new Vector2(0f, 1f);
				rt.anchorMax = new Vector2(1f, 1f);
			}

			var le = row.GetComponent<LayoutElement>() ?? row.AddComponent<LayoutElement>();
			le.minHeight = 34f;
			le.preferredHeight = 36f;

			// Kill components that re-bind vanilla prefs
			foreach (var ss in row.GetComponentsInChildren<StagesSettings>(true))
				Object.Destroy(ss);
			foreach (var ac in row.GetComponentsInChildren<AndroidControl>(true))
			{
				// leave object; just avoid accidental use
			}

			var toggle = row.GetComponent<Toggle>();
			if (toggle == null)
				toggle = row.GetComponentInChildren<Toggle>(true);
			if (toggle == null)
			{
				Object.Destroy(row);
				AddSimpleToggleRow(parent, key, label, entry);
				return;
			}

			toggle.onValueChanged = new Toggle.ToggleEvent();
			bool cur = Plugin.GetPref(key, entry.Value);
			toggle.SetIsOnWithoutNotify(cur);

			foreach (var t in row.GetComponentsInChildren<Text>(true))
				t.text = label;
			foreach (var t in row.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
			{
				t.text = label;
				// If font asset missing, TMP draws nothing — force a fallback via Unity Text if empty
				if (t.font == null)
					t.enabled = false;
			}

			// Ensure some readable label exists
			if (row.GetComponentInChildren<Text>(true) == null)
			{
				var labelGo = new GameObject("BRMod_Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
				labelGo.transform.SetParent(row.transform, false);
				var lrt = labelGo.GetComponent<RectTransform>();
				lrt.anchorMin = new Vector2(0.2f, 0f);
				lrt.anchorMax = new Vector2(1f, 1f);
				lrt.offsetMin = new Vector2(8f, 0f);
				lrt.offsetMax = Vector2.zero;
				var ut = labelGo.GetComponent<Text>();
				ut.font = GetUiFont();
				ut.text = label;
				ut.fontSize = 16;
				ut.color = Color.white;
				ut.alignment = TextAnchor.MiddleLeft;
				ut.raycastTarget = false;
			}

			string k = key;
			var e = entry;
			toggle.onValueChanged.AddListener(new UnityAction<bool>(v =>
			{
				Plugin.SetOption(k, v, e);
				Plugin.Log.LogInfo($"Option {k} = {v}");
			}));
		}

		private static void AddSimpleToggleRow(
			Transform parent,
			string key,
			string label,
			BepInEx.Configuration.ConfigEntry<bool> entry)
		{
			var row = new GameObject("BRMod_" + key, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
			row.transform.SetParent(parent, false);
			var le = row.AddComponent<LayoutElement>();
			le.minHeight = 34f;
			le.preferredHeight = 36f;
			row.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.22f, 0.9f);

			var hlg = row.AddComponent<HorizontalLayoutGroup>();
			hlg.padding = new RectOffset(8, 8, 4, 4);
			hlg.spacing = 8f;
			hlg.childAlignment = TextAnchor.MiddleLeft;
			hlg.childForceExpandWidth = false;
			hlg.childForceExpandHeight = true;
			hlg.childControlWidth = true;
			hlg.childControlHeight = true;

			// Checkbox
			var box = new GameObject("Check", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Toggle));
			box.transform.SetParent(row.transform, false);
			var boxLe = box.AddComponent<LayoutElement>();
			boxLe.minWidth = 28f;
			boxLe.preferredWidth = 28f;
			boxLe.minHeight = 28f;
			var boxImg = box.GetComponent<Image>();
			boxImg.color = new Color(0.85f, 0.85f, 0.85f, 1f);

			var checkGo = new GameObject("Checkmark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
			checkGo.transform.SetParent(box.transform, false);
			var crt = checkGo.GetComponent<RectTransform>();
			crt.anchorMin = new Vector2(0.15f, 0.15f);
			crt.anchorMax = new Vector2(0.85f, 0.85f);
			crt.offsetMin = Vector2.zero;
			crt.offsetMax = Vector2.zero;
			var checkImg = checkGo.GetComponent<Image>();
			checkImg.color = new Color(0.2f, 0.75f, 0.35f, 1f);

			var toggle = box.GetComponent<Toggle>();
			toggle.targetGraphic = boxImg;
			toggle.graphic = checkImg;
			toggle.isOn = Plugin.GetPref(key, entry.Value);

			// Label
			var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
			labelGo.transform.SetParent(row.transform, false);
			var labelLe = labelGo.AddComponent<LayoutElement>();
			labelLe.flexibleWidth = 1f;
			var ut = labelGo.GetComponent<Text>();
			ut.font = GetUiFont();
			ut.text = label;
			ut.fontSize = 15;
			ut.color = Color.white;
			ut.alignment = TextAnchor.MiddleLeft;
			ut.raycastTarget = false;

			string k = key;
			var e = entry;
			toggle.onValueChanged.AddListener(new UnityAction<bool>(v =>
			{
				Plugin.SetOption(k, v, e);
				Plugin.Log.LogInfo($"Option {k} = {v}");
			}));
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

	[HarmonyPatch(typeof(SettingsInit), "Start")]
	internal static class SettingsInitPatch
	{
		static void Postfix(SettingsInit __instance)
		{
			try
			{
				// Track nearest canvas / settings-looking ancestor for IMGUI + UI
				Transform t = __instance.transform;
				GameObject root = __instance.gameObject;
				// Walk up a few levels for a reasonable settings container
				for (int i = 0; i < 6 && t != null; i++)
				{
					if (t.name.IndexOf("Setting", System.StringComparison.OrdinalIgnoreCase) >= 0
					    || t.name.IndexOf("Pause", System.StringComparison.OrdinalIgnoreCase) >= 0)
					{
						root = t.gameObject;
					}
					t = t.parent;
				}

				// Cache only — do not open IMGUI until ButtonScript.Settings toggles the panel.
				if (ModSettingsUi.TrackedSettingsRoot == null)
					ModSettingsUi.TrackedSettingsRoot = root;
				Plugin.Log.LogInfo($"SettingsInit found on '{__instance.name}' (ancestor '{root.name}').");
			}
			catch (System.Exception ex)
			{
				Plugin.Log.LogError($"SettingsInit hook failed: {ex}");
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
