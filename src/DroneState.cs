using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace BoobsRunnerMod
{
	/// <summary>
	/// Tracks the recruited following photo drone so photo Interact works
	/// after the selector trigger loses overlap (vanilla bug).
	/// </summary>
	internal static class DroneState
	{
		private static readonly FieldInfo InteractedField =
			AccessTools.Field(typeof(Interactable), "interacted");
		private static readonly FieldInfo DroneFollowField =
			AccessTools.Field(typeof(Interactable), "droneFollow");

		public static Interactable ActiveDrone { get; private set; }

		public static bool HasFollowingDrone
		{
			get
			{
				Cleanup();
				return ActiveDrone != null;
			}
		}

		public static void SetActive(Interactable drone)
		{
			ActiveDrone = drone;
		}

		public static void Clear()
		{
			ActiveDrone = null;
		}

		public static void ClearIf(Interactable drone)
		{
			if (ActiveDrone == drone)
				ActiveDrone = null;
		}

		public static void Cleanup()
		{
			if (ActiveDrone == null)
				return;

			// Unity fake-null for destroyed objects
			if (!ActiveDrone)
			{
				ActiveDrone = null;
				return;
			}

			if (!ActiveDrone.enabled || !ActiveDrone.gameObject.activeInHierarchy)
			{
				ActiveDrone = null;
				return;
			}

			if (!IsFollowing(ActiveDrone) && !IsRecruited(ActiveDrone))
			{
				// Still allow recruited drones even if follow flag flaked
			}
		}

		public static bool IsDrone(GameObject go)
		{
			if (go == null || !go)
				return false;
			try
			{
				return go.CompareTag("Enemy Drone");
			}
			catch
			{
				return go.name.IndexOf("Drone", System.StringComparison.OrdinalIgnoreCase) >= 0;
			}
		}

		public static bool IsBench(GameObject go)
		{
			if (go == null || !go)
				return false;
			try
			{
				if (go.CompareTag("Bench"))
					return true;
			}
			catch
			{
				// tag missing
			}
			return go.name.IndexOf("Bench", System.StringComparison.OrdinalIgnoreCase) >= 0;
		}

		public static bool IsRecruited(Interactable ia)
		{
			if (ia == null || !ia || InteractedField == null)
				return false;
			return (bool)InteractedField.GetValue(ia);
		}

		public static bool IsFollowing(Interactable ia)
		{
			if (ia == null || !ia || DroneFollowField == null)
				return false;
			return (bool)DroneFollowField.GetValue(ia);
		}

		public static Interactable GetInteractable(GameObject go)
		{
			if (go == null || !go)
				return null;
			return go.GetComponent<Interactable>()
			       ?? go.GetComponentInParent<Interactable>();
		}

		/// <summary>
		/// Priority: selector bench (or other non-drone) &gt; selector drone &gt; following active drone.
		/// </summary>
		public static Interactable ResolveTarget(GameObject selectorTarget)
		{
			var touchIa = GetInteractable(selectorTarget);

			if (touchIa != null)
			{
				var go = touchIa.gameObject;
				if (IsBench(go))
					return touchIa;

				if (!IsDrone(go))
					return touchIa;

				// Selector is on a drone — use that drone (recruit or photo)
				return touchIa;
			}

			// No selector hit — photo via following drone only
			Cleanup();
			if (ActiveDrone != null && IsRecruited(ActiveDrone))
				return ActiveDrone;

			return null;
		}
	}
}
