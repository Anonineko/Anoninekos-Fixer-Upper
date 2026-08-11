using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace BoobsRunnerMod.Patches
{
	[HarmonyPatch(typeof(EnemySpawner), "Start")]
	internal static class EnemySpawnerStartPatch
	{
		private static readonly FieldInfo SpawnChanceField =
			AccessTools.Field(typeof(EnemySpawner), "spawnChance");
		private static readonly FieldInfo EnemyField =
			AccessTools.Field(typeof(EnemySpawner), "enemy");

		static bool Prefix(EnemySpawner __instance)
		{
			var sr = __instance.GetComponent<SpriteRenderer>();
			if (sr != null)
				sr.enabled = false;

			var enemy = EnemyField?.GetValue(__instance) as GameObject;
			if (enemy == null)
				return false;

			bool isDrone = DroneState.IsDrone(enemy)
			               || enemy.name.IndexOf("Drone", System.StringComparison.OrdinalIgnoreCase) >= 0
			               || enemy.name.IndexOf("Phot", System.StringComparison.OrdinalIgnoreCase) >= 0;

			if (isDrone)
			{
				DroneState.Cleanup();
				bool have = DroneState.HasFollowingDrone;

				if (Plugin.GetPref(ModPrefs.NoDroneWhenHaveOne, Plugin.NoDroneWhenHaveOne.Value) && have)
				{
					Plugin.Log.LogDebug("Skip photo-drone spawn (already have following drone).");
					return false;
				}

				if (Plugin.GetPref(ModPrefs.ForceDroneWhenNone, Plugin.ForceDroneWhenNone.Value) && !have)
				{
					SpawnEnemy(__instance, enemy);
					Plugin.Log.LogDebug("Forced photo-drone spawn (none following).");
					return false;
				}
			}

			// Vanilla chance roll
			float chance = SpawnChanceField != null ? (float)SpawnChanceField.GetValue(__instance) : 100f;
			if (Random.Range(0f, 100f) > chance)
				return false;

			SpawnEnemy(__instance, enemy);
			return false;
		}

		private static void SpawnEnemy(EnemySpawner spawner, GameObject enemy)
		{
			var parent = new GameObject("Enemies");
			parent.transform.parent = spawner.transform;
			float half = spawner.transform.lossyScale.x / 2f;
			float minX = spawner.transform.position.x - half;
			float maxX = spawner.transform.position.x + half;
			float x = Random.Range(minX, maxX);
			Object.Instantiate(
				enemy,
				new Vector2(x, spawner.transform.position.y),
				Quaternion.Euler(Vector3.zero),
				parent.transform);
		}
	}

	[HarmonyPatch(typeof(BuildingSpawner), "Update")]
	internal static class BuildingSpawnerUpdatePatch
	{
		private static readonly FieldInfo BuildingsField =
			AccessTools.Field(typeof(BuildingSpawner), "buildingsGenerated");
		private static readonly HashSet<int> Processed = new HashSet<int>();

		static void Postfix(BuildingSpawner __instance)
		{
			bool noFull = Plugin.GetPref(ModPrefs.NoBenchWhenFullHp, Plugin.NoBenchWhenFullHp.Value);
			bool highLow = Plugin.GetPref(ModPrefs.BenchHighWhenLowHp, Plugin.BenchHighWhenLowHp.Value);
			if (!noFull && !highLow)
				return;

			var list = BuildingsField?.GetValue(__instance) as List<GameObject>;
			if (list == null || list.Count == 0)
				return;

			var player = PlayerAccess.FindPlayer();

			for (int i = 0; i < list.Count; i++)
			{
				var b = list[i];
				if (b == null || !b)
					continue;

				int id = b.GetHashCode();
				if (!Processed.Add(id))
					continue;

				ProcessBuildingBenches(b, player, noFull, highLow);
			}

			// Bound growth across long runs / scene reloads
			if (Processed.Count > 256)
				Processed.Clear();
		}

		private static void ProcessBuildingBenches(GameObject building, PlayerScript player, bool noFull, bool highLow)
		{
			bool full = PlayerAccess.IsFullHp(player);
			bool low = PlayerAccess.IsLowHp(player);

			float keepChance = Plugin.NormalBenchChance.Value;

			if (noFull && full)
				keepChance = 0f;
			else if (highLow && low)
				keepChance = Plugin.LowHpBenchChance.Value;

			// Vanilla-ish when neither option applies to this HP state
			if (!(noFull && full) && !(highLow && low))
				keepChance = Plugin.NormalBenchChance.Value;

			var transforms = building.GetComponentsInChildren<Transform>(true);
			foreach (var t in transforms)
			{
				if (t == null || t.gameObject == building)
					continue;

				if (!DroneState.IsBench(t.gameObject))
					continue;

				if (Random.Range(0f, 100f) > keepChance)
				{
					Object.Destroy(t.gameObject);
					Plugin.Log.LogDebug($"Removed bench '{t.name}' keepChance={keepChance}");
				}
			}
		}
	}
}
