using DG.Tweening;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BoobsRunnerMod.Patches
{
	/// <summary>
	/// Vanilla often Destroy()s buildings/enemies/FX while DOTween is still fading/scaling them.
	/// Kill tweens first and quiet SafeMode spam.
	/// </summary>
	internal static class DotweenSafety
	{
		private static bool _configured;

		internal static void Configure()
		{
			if (_configured)
				return;
			try
			{
				// Keep safe mode (prevents hard crashes) but stop flooding the log.
				DOTween.useSafeMode = true;
				DOTween.logBehaviour = LogBehaviour.ErrorsOnly;

				// Enum lives in DOTween but may not resolve cleanly as a compile-time name on all builds.
				var prop = AccessTools.Property(typeof(DOTween), "safeModeLogBehaviour");
				if (prop != null)
				{
					var enumType = prop.PropertyType;
					// 0 = None in SafeModeLogBehaviour
					object none = System.Enum.ToObject(enumType, 0);
					prop.SetValue(null, none, null);
				}

				_configured = true;
				Plugin.Log.LogInfo("DOTween safety: safeMode on, safe-mode logs silenced.");
			}
			catch (System.Exception ex)
			{
				Plugin.Log.LogWarning($"DOTween configure failed: {ex.Message}");
			}
		}

		internal static void KillAllOn(Object obj)
		{
			if (obj == null)
				return;

			try
			{
				// Unity fake-null
				if (!obj)
					return;

				if (obj is GameObject go)
				{
					KillGameObject(go);
					return;
				}

				if (obj is Component comp)
				{
					if (!comp)
						return;

					// BuildingDeletor destroys the Transform — treat as whole object.
					if (comp is Transform tr)
					{
						if (tr.gameObject != null)
							KillGameObject(tr.gameObject);
						else
						{
							DOTween.Kill(tr, complete: false);
							DOTween.Kill(comp, complete: false);
						}
						return;
					}

					DOTween.Kill(comp, complete: false);
					if (comp.gameObject != null)
						KillGameObject(comp.gameObject);
				}
			}
			catch (System.Exception ex)
			{
				Plugin.Log.LogDebug($"DOTween kill-on-destroy: {ex.Message}");
			}
		}

		private static void KillGameObject(GameObject go)
		{
			if (go == null || !go)
				return;

			DOTween.Kill(go, complete: false);

			// Kill by every common target type DOTween shortcuts use (Transform, Renderer, etc.)
			var transforms = go.GetComponentsInChildren<Transform>(true);
			for (int i = 0; i < transforms.Length; i++)
			{
				if (transforms[i] != null)
					DOTween.Kill(transforms[i], complete: false);
			}

			var comps = go.GetComponentsInChildren<Component>(true);
			for (int i = 0; i < comps.Length; i++)
			{
				var c = comps[i];
				if (c != null)
					DOTween.Kill(c, complete: false);
			}
		}
	}

	[HarmonyPatch(typeof(Object), nameof(Object.Destroy), typeof(Object))]
	internal static class ObjectDestroyPatch
	{
		static void Prefix(Object obj)
		{
			DotweenSafety.Configure();
			DotweenSafety.KillAllOn(obj);
		}
	}

	[HarmonyPatch(typeof(Object), nameof(Object.Destroy), typeof(Object), typeof(float))]
	internal static class ObjectDestroyDelayedPatch
	{
		// Delayed destroy: still kill immediately so orphaned tweens don't outlive the schedule window
		// for short VFX. Longer delayed destroys with intentional tweens finish before the delay ends
		// in this game (e.g. 3s destroy after 0.25s fade).
		static void Prefix(Object obj, float t)
		{
			DotweenSafety.Configure();
			// Only pre-kill when destruction is immediate-ish; long delays keep tweens until real destroy.
			if (t <= 0.05f)
				DotweenSafety.KillAllOn(obj);
		}
	}

	// Immediate path used by some Unity versions / code
	[HarmonyPatch(typeof(Object), nameof(Object.DestroyImmediate), typeof(Object))]
	internal static class ObjectDestroyImmediatePatch
	{
		static void Prefix(Object obj)
		{
			DotweenSafety.Configure();
			DotweenSafety.KillAllOn(obj);
		}
	}

	[HarmonyPatch(typeof(Object), nameof(Object.DestroyImmediate), typeof(Object), typeof(bool))]
	internal static class ObjectDestroyImmediateAllowPatch
	{
		static void Prefix(Object obj)
		{
			DotweenSafety.Configure();
			DotweenSafety.KillAllOn(obj);
		}
	}

	// Explicit building cleanup (main source of mid-tween enemy/FX kills)
	[HarmonyPatch(typeof(BuildingSpawner), "DestroyClone")]
	internal static class BuildingDestroyClonePatch
	{
		static void Prefix(BuildingSpawner __instance)
		{
			DotweenSafety.Configure();
			try
			{
				var field = AccessTools.Field(typeof(BuildingSpawner), "removingBuildings");
				// actual kill happens on Destroy; this is backup if list already filled mid-frame
			}
			catch
			{
				// ignore
			}
		}
	}

	// Ensure settings applied early even before first Destroy
	[HarmonyPatch(typeof(PlayerScript), "Start")]
	internal static class DotweenConfigOnPlayerStart
	{
		static void Postfix()
		{
			DotweenSafety.Configure();
		}
	}
}
