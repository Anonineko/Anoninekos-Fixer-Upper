using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace BoobsRunnerMod
{
	/// <summary>
	/// Top-left HUD: "Stage: x/total" from PlayerState.Stages (kills / 10), starting at 0.
	/// </summary>
	internal static class StageHud
	{
		private static readonly FieldInfo GameStartField =
			AccessTools.Field(typeof(PlayerScript), "GameStart");

		private static GUIStyle _style;
		private static Texture2D _bg;
		private static int _cachedTotal = -1;

		internal static void Draw()
		{
			if (Plugin.ShowStageHud == null || !Plugin.ShowStageHud.Value)
				return;

			var player = PlayerAccess.FindPlayer();
			if (player == null)
				return;

			// Only during an active run (not title / before jump-out)
			if (GameStartField != null)
			{
				object gs = GameStartField.GetValue(player);
				if (gs is bool started && !started)
					return;
			}

			if (player.state == null)
				return;

			int stage = player.state.Stages;
			if (stage < 0)
				stage = 0;

			int total = GetTotalStages();
			// Keep showing real stage even if past total (e.g. Stage: 12/8 → still honest)
			string text = $"Stage: {stage}/{total}";

			EnsureStyle();

			const float pad = 10f;
			const float x = 12f;
			const float y = 10f;
			Vector2 size = _style.CalcSize(new GUIContent(text));
			float w = size.x + pad * 2f;
			float h = size.y + pad;

			var rect = new Rect(x, y, w, h);
			GUI.Box(rect, GUIContent.none, _bg != null
				? new GUIStyle(GUI.skin.box) { normal = { background = _bg } }
				: GUI.skin.box);
			GUI.Label(new Rect(x + pad, y + pad * 0.35f, w - pad, h), text, _style);
		}

		private static int GetTotalStages()
		{
			// Manual total when auto is off
			if (Plugin.StageHudAutoTotal == null || !Plugin.StageHudAutoTotal.Value)
				return Plugin.StageHudTotal != null && Plugin.StageHudTotal.Value > 0
					? Plugin.StageHudTotal.Value
					: 8;

			if (_cachedTotal >= 0)
				return _cachedTotal;

			// Discover highest existing stagesN sprite (game uses "stages" + index).
			int last = -1;
			for (int i = 0; i < 64; i++)
			{
				var spr = Resources.Load<Sprite>("Thingy/Character/Stages/stages" + i);
				if (spr == null)
					break;
				last = i;
			}

			// Fallback 8 if no sprites resolved (common for photo tier range).
			_cachedTotal = last >= 0 ? last : 8;
			return _cachedTotal;
		}

		private static void EnsureStyle()
		{
			if (_style != null)
				return;

			_bg = new Texture2D(2, 2, TextureFormat.RGBA32, false);
			var c = new Color(0f, 0f, 0f, 0.45f);
			_bg.SetPixels(new[] { c, c, c, c });
			_bg.Apply(false, true);

			_style = new GUIStyle(GUI.skin.label)
			{
				fontSize = 20,
				fontStyle = FontStyle.Bold,
				normal = { textColor = Color.white },
				alignment = TextAnchor.MiddleLeft,
			};
		}

		internal static void ResetCache()
		{
			_cachedTotal = -1;
		}
	}
}
