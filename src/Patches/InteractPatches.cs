using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BoobsRunnerMod.Patches
{
	[HarmonyPatch(typeof(PlayerScript), nameof(PlayerScript.Interact))]
	internal static class PlayerInteractPatch
	{
		// Replace interact routing so following drones stay usable without selector overlap.
		static bool Prefix(PlayerScript __instance, InputAction.CallbackContext ctx)
		{
			if (!Plugin.GetPref(ModPrefs.FixDroneInteract, Plugin.FixDroneInteract.Value))
				return true;

			if (!ctx.performed)
				return false;

			var selector = PlayerAccess.GetSelector(__instance);
			GameObject touching = selector != null ? selector.touchingGO : null;

			// Prefer bench (or any non-drone) under selector over the following drone.
			var target = DroneState.ResolveTarget(touching);
			if (target == null)
				return false;

			// Guard missing component / destroyed
			if (!target || !target.enabled)
				return false;

			target.Interact();
			return false;
		}
	}

	[HarmonyPatch(typeof(Interactable), nameof(Interactable.Interact))]
	internal static class InteractableInteractPatch
	{
		static void Postfix(Interactable __instance)
		{
			if (!Plugin.GetPref(ModPrefs.FixDroneInteract, Plugin.FixDroneInteract.Value))
				return;

			if (!DroneState.IsDrone(__instance.gameObject))
				return;

			// After first recruit, track as active following drone
			if (DroneState.IsRecruited(__instance) || DroneState.IsFollowing(__instance))
				DroneState.SetActive(__instance);
		}
	}

	[HarmonyPatch(typeof(PlayerScript), nameof(PlayerScript.TakePhot))]
	internal static class TakePhotPatch
	{
		static void Postfix(PlayerScript __instance)
		{
			if (!Plugin.GetPref(ModPrefs.FixDroneInteract, Plugin.FixDroneInteract.Value))
				return;

			// Vanilla enables outPhot even when pose is invalid. Undo that leak.
			var phot = PlayerAccess.GetCurrentPhot(__instance);
			if (phot == null || !phot)
				PlayerAccess.SetOutPhotActive(__instance, false);
		}
	}

	[HarmonyPatch(typeof(PlayerScript), nameof(PlayerScript.AfterPhot), typeof(InputAction.CallbackContext))]
	internal static class AfterPhotCtxPatch
	{
		static void Postfix(PlayerScript __instance)
		{
			// Input overload forgot to hide outPhot
			PlayerAccess.SetOutPhotActive(__instance, false);
		}
	}

	[HarmonyPatch(typeof(EnemyScript), nameof(EnemyScript.Die))]
	internal static class EnemyDiePatch
	{
		static void Prefix(EnemyScript __instance)
		{
			var ia = __instance.GetComponent<Interactable>();
			if (ia != null)
				DroneState.ClearIf(ia);
		}
	}

	// Prevent unrelated trigger exits from wiping selector when the active target is still valid.
	[HarmonyPatch(typeof(SelectorScript), "OnTriggerExit2D")]
	internal static class SelectorExitPatch
	{
		static bool Prefix(SelectorScript __instance, Collider2D collision)
		{
			if (!Plugin.GetPref(ModPrefs.FixDroneInteract, Plugin.FixDroneInteract.Value))
				return true;

			// Only clear when the *current* target leaves (vanilla always cleared).
			if (collision != null && __instance.touchingGO != null
			    && collision.gameObject == __instance.touchingGO)
			{
				__instance.ResetSelector();
				__instance.touchingGO = null;
			}

			return false;
		}
	}
}
