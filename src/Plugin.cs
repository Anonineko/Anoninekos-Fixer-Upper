using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.Mono;
using HarmonyLib;
using UnityEngine;

namespace BoobsRunnerMod
{
	[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
	public class Plugin : BaseUnityPlugin
	{
		public const string PluginGuid = "com.anonineko.boobsrunnerfixes";
		public const string PluginName = "Anonineko's Fixer Upper";
		public const string PluginVersion = "1.0.0";

		internal static ManualLogSource Log;
		internal static Plugin Instance;

		internal static ConfigEntry<bool> FixDroneInteract;
		internal static ConfigEntry<bool> NoBenchWhenFullHp;
		internal static ConfigEntry<bool> BenchHighWhenLowHp;
		internal static ConfigEntry<bool> ForceDroneWhenNone;
		internal static ConfigEntry<bool> NoDroneWhenHaveOne;
		internal static ConfigEntry<bool> DownDropsLedge;
		internal static ConfigEntry<float> LowHpRatio;
		internal static ConfigEntry<float> LowHpBenchChance;
		internal static ConfigEntry<float> NormalBenchChance;

		private void Awake()
		{
			Instance = this;
			Log = Logger;

			FixDroneInteract = Config.Bind("Fixes", "FixDroneInteract", true,
				"Fix photo-drone second interact (follow then photo without selector) and failed-pose outPhot leak.");
			NoBenchWhenFullHp = Config.Bind("Gameplay", "NoBenchWhenFullHp", false,
				"Do not spawn/keep benches on new buildings when HP is full.");
			BenchHighWhenLowHp = Config.Bind("Gameplay", "BenchHighWhenLowHp", false,
				"When low HP, keep benches much more often on new buildings.");
			ForceDroneWhenNone = Config.Bind("Gameplay", "ForceDroneWhenNone", false,
				"If you have no following photo drone, force EnemySpawner drones to always spawn.");
			NoDroneWhenHaveOne = Config.Bind("Gameplay", "NoDroneWhenHaveOne", false,
				"If you already have a following photo drone, skip new photo-drone spawns.");
			DownDropsLedge = Config.Bind("Gameplay", "DownDropsLedge", false,
				"Down/Slide releases ledge hang / ledge grab instead of only fast-falling.");
			LowHpRatio = Config.Bind("Gameplay", "LowHpRatio", 0.5f,
				"HP fraction at or below which counts as low HP (lives/maxLives).");
			LowHpBenchChance = Config.Bind("Gameplay", "LowHpBenchChance", 100f,
				"When BenchHighWhenLowHp is on and HP is low: % chance to keep each bench (0-100).");
			NormalBenchChance = Config.Bind("Gameplay", "NormalBenchChance", 100f,
				"When not full-HP-no-bench and not low-HP boost: % chance to keep each bench (0-100). Vanilla keep-all is 100.");

			// Mirror into PlayerPrefs so in-game toggles and config stay aligned after load.
			SyncPrefsFromConfig();

			var harmony = new Harmony(PluginGuid);
			harmony.PatchAll(Assembly.GetExecutingAssembly());
			Log.LogInfo($"{PluginName} v{PluginVersion} loaded.");
		}

		/// <summary>
		/// IMGUI settings panel — draws while the game Settings screen is open.
		/// Survives pause (timeScale 0) and does not depend on TMP fonts / layout groups.
		/// </summary>
		private void OnGUI()
		{
			try
			{
				ModSettingsUi.DrawImgui();
			}
			catch (System.Exception ex)
			{
				// Avoid spamming every frame
				if (Time.frameCount % 120 == 0)
					Log.LogError($"OnGUI settings draw failed: {ex.Message}");
			}
		}

		internal static void SyncPrefsFromConfig()
		{
			WritePref(ModPrefs.NoBenchWhenFullHp, NoBenchWhenFullHp.Value);
			WritePref(ModPrefs.BenchHighWhenLowHp, BenchHighWhenLowHp.Value);
			WritePref(ModPrefs.ForceDroneWhenNone, ForceDroneWhenNone.Value);
			WritePref(ModPrefs.NoDroneWhenHaveOne, NoDroneWhenHaveOne.Value);
			WritePref(ModPrefs.DownDropsLedge, DownDropsLedge.Value);
			WritePref(ModPrefs.FixDroneInteract, FixDroneInteract.Value);
		}

		internal static void WritePref(string key, bool value)
		{
			PlayerPrefs.SetInt(key, value ? 1 : 0);
		}

		internal static bool GetPref(string key, bool fallback)
		{
			if (!PlayerPrefs.HasKey(key))
				return fallback;
			return PlayerPrefs.GetInt(key, fallback ? 1 : 0) != 0;
		}

		internal static void SetOption(string key, bool value, ConfigEntry<bool> entry)
		{
			entry.Value = value;
			WritePref(key, value);
			PlayerPrefs.Save();
		}
	}

	internal static class ModPrefs
	{
		public const string NoBenchWhenFullHp = "BRMod_NoBenchWhenFullHp";
		public const string BenchHighWhenLowHp = "BRMod_BenchHighWhenLowHp";
		public const string ForceDroneWhenNone = "BRMod_ForceDroneWhenNone";
		public const string NoDroneWhenHaveOne = "BRMod_NoDroneWhenHaveOne";
		public const string DownDropsLedge = "BRMod_DownDropsLedge";
		public const string FixDroneInteract = "BRMod_FixDroneInteract";
	}
}
