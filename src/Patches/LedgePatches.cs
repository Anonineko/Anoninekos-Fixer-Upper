using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BoobsRunnerMod.Patches
{
	[HarmonyPatch(typeof(PlayerScript), nameof(PlayerScript.Slide))]
	internal static class SlideLedgeDropPatch
	{
		static bool Prefix(PlayerScript __instance, InputAction.CallbackContext ctx)
		{
			if (!Plugin.GetPref(ModPrefs.DownDropsLedge, Plugin.DownDropsLedge.Value))
				return true;

			if (!ctx.performed)
				return true;

			var state = __instance.state;
			if (state == null)
				return true;

			// Ledge grab (pulled onto lip) OR edge hang (right wall, no overhead)
			bool hangingOnEdge = state.RightWalled && !state.LedgeGrab && !state.FastFall;
			bool ledgeGrabbing = state.LedgeGrab;

			if (!hangingOnEdge && !ledgeGrabbing)
				return true;

			ReleaseLedge(__instance, state);
			// Still allow vanilla Slide to apply fast-fall / slide if applicable
			return true;
		}

		private static void ReleaseLedge(PlayerScript player, PlayerState state)
		{
			state.LedgeGrab = false;
			state.RightWalled = false;
			state.FastFall = true;
			state.Aired = true;
			state.Walking = false;
			state.Jumpable = false;

			PlayerAccess.SetLedgeGrabTimer(player, 0f);
			PlayerAccess.TryResetLedgeAnims(player);

			var rb = PlayerAccess.GetRb(player);
			if (rb != null)
			{
				float g = PlayerAccess.GetNormalGravity(player);
				// Use a snappy drop similar to fast-fall
				rb.gravityScale = g * 2.5f;
				if (rb.linearVelocityY > 0f)
					rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocityY * 0.3f);
			}

			Plugin.Log.LogDebug("Released ledge via Down/Slide.");
		}
	}
}
