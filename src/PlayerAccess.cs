using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace BoobsRunnerMod
{
	internal static class PlayerAccess
	{
		private static readonly FieldInfo LivesField = AccessTools.Field(typeof(PlayerScript), "lives");
		private static readonly FieldInfo MaxLivesField = AccessTools.Field(typeof(PlayerScript), "maxLives");
		private static readonly FieldInfo SelectorField = AccessTools.Field(typeof(PlayerScript), "selector");
		private static readonly FieldInfo CurrentPhotField = AccessTools.Field(typeof(PlayerScript), "currentPhot");
		private static readonly FieldInfo OutPhotField = AccessTools.Field(typeof(PlayerScript), "outPhot");
		private static readonly FieldInfo RbField = AccessTools.Field(typeof(PlayerScript), "rb");
		private static readonly FieldInfo NormalGravityField = AccessTools.Field(typeof(PlayerScript), "normalGravity");
		private static readonly FieldInfo AnimsField = AccessTools.Field(typeof(PlayerScript), "anims");
		private static readonly FieldInfo RootField = AccessTools.Field(typeof(PlayerScript), "root");
		private static readonly FieldInfo LedgeGrabTimerField = AccessTools.Field(typeof(PlayerScript), "ledgeGrabTimer");
		private static readonly FieldInfo TouchingObjectField = AccessTools.Field(typeof(PlayerScript), "touchingObject");

		public static PlayerScript FindPlayer()
		{
			var go = GameObject.Find("Player");
			return go != null ? go.GetComponent<PlayerScript>() : null;
		}

		public static int GetLives(PlayerScript p)
		{
			if (p == null || LivesField == null)
				return 3;
			return (int)LivesField.GetValue(p);
		}

		public static int GetMaxLives(PlayerScript p)
		{
			if (p == null || MaxLivesField == null)
				return 3;
			return (int)MaxLivesField.GetValue(p);
		}

		public static bool IsFullHp(PlayerScript p)
		{
			if (p == null)
				return true;
			return GetLives(p) >= GetMaxLives(p);
		}

		public static bool IsLowHp(PlayerScript p)
		{
			if (p == null)
				return false;
			int max = Mathf.Max(1, GetMaxLives(p));
			int lives = GetLives(p);
			return lives <= Mathf.Max(1, Mathf.FloorToInt(max * Plugin.LowHpRatio.Value));
		}

		public static SelectorScript GetSelector(PlayerScript p)
		{
			if (p == null || SelectorField == null)
				return null;
			return (SelectorScript)SelectorField.GetValue(p);
		}

		public static GameObject GetCurrentPhot(PlayerScript p)
		{
			if (p == null || CurrentPhotField == null)
				return null;
			return (GameObject)CurrentPhotField.GetValue(p);
		}

		public static void SetOutPhotActive(PlayerScript p, bool active)
		{
			if (p == null || OutPhotField == null)
				return;
			var go = (GameObject)OutPhotField.GetValue(p);
			if (go != null)
				go.SetActive(active);
		}

		public static Rigidbody2D GetRb(PlayerScript p) =>
			p == null || RbField == null ? null : (Rigidbody2D)RbField.GetValue(p);

		public static float GetNormalGravity(PlayerScript p) =>
			p == null || NormalGravityField == null ? 2f : (float)NormalGravityField.GetValue(p);

		public static void SetLedgeGrabTimer(PlayerScript p, float v)
		{
			LedgeGrabTimerField?.SetValue(p, v);
		}

		public static void TryResetLedgeAnims(PlayerScript p)
		{
			try
			{
				var anims = AnimsField?.GetValue(p);
				if (anims != null)
				{
					var setAll = AccessTools.Method(anims.GetType(), "SetAllBodyBool",
						new[] { typeof(string), typeof(bool) });
					setAll?.Invoke(anims, new object[] { "Ledge Grab", false });
					setAll?.Invoke(anims, new object[] { "Wall Hugging", false });
				}

				var root = RootField?.GetValue(p);
				if (root != null)
				{
					var normal = AccessTools.Method(root.GetType(), "NormalTransform");
					normal?.Invoke(root, null);
				}
			}
			catch (System.Exception ex)
			{
				Plugin.Log.LogDebug($"TryResetLedgeAnims: {ex.Message}");
			}
		}
	}
}
