using System;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using BlockStoryCore;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

[BepInPlugin("com.BlueKitsunex777x.blockstory.BetterInventory", "Better Inventory", "1.1.3")]
[BepInDependency("com.mrc.blockstory.core")]
public class BetterInventoryPlugin : BaseUnityPlugin
{
	public static bool Enabled = PlayerPrefs.GetInt("BetterInventory_Enabled", 1) != 0;
	public static bool Active => Enabled;

	// Set up variables need to check for the incompatible mod More Slots (More Slots has been integrated into the one)
	public static ModInfo MoreSlots;
	public static bool MoreSlotsOn;

	// Get save player prefrences
	public static int Cols = PlayerPrefs.GetInt("BetterInventory_Cols", 6);
	public static int Rows = PlayerPrefs.GetInt("BetterInventory_Rows", 12);
	public static bool Unlock = PlayerPrefs.GetInt("BetterInventory_Unlock", 0) != 0;

	// the bag is sized when a world loads, so a change made in a world is saved here and used from the next load
	private static bool _savedEnabled = Enabled;
	private static int _savedCols = Cols;
	private static int _savedRows = Rows;
	private static bool _savedUnlock = Unlock;
	private bool _wasInWorld;

	// how far left the grid moves per extra column, so the last column clears the scrollbar. 1.15 was the old fixed value
	public static float Shift = PlayerPrefs.GetFloat("BetterInventory_Shift", 1.15f);
	public static readonly int[] UnlockLevels = [0, 9, 19, 29, 39, 54, 69, 84, 99, 109, 119];
	public static int Slots => Cols * Rows + 4;
	public static int UnlockSize(int row) { return 4 + Cols * (row + 1); }

	// Runs on game startup, sets everything up for the mod to work and show in the BSMK core mod menu
	private void Awake()
	{
		if (!BSMKCheck.Passed(this)) return;   // added: only runs on the real BSMK Core
		try
		{
			new Harmony("com.BlueKitsunex777x.blockstory.betterinventory").PatchAll();
			L.Info("loading, better inventory");
		}
		catch (Exception e)
		{
			L.Err("PatchAll", e);
		}
		ModRegistry.Register(new ModInfo
		{
			Name = "Better Inventory",
			Description = "Changes the layout of the players inventory",
			GetEnabled = () => _savedEnabled,
			SetEnabled = delegate(bool on)
			{
				if (on && MoreSlotsOn)
				{
					MoreSlots.SetEnabled(obj: false);
					L.Warn("MoreSlots was on");
				}
				_savedEnabled = on;
				PlayerPrefs.SetInt("BetterInventory_Enabled", on ? 1 : 0);
				PlayerPrefs.Save();
				ApplySaved();
				L.Info("enabled = " + on);
			},
			Config = new ModConfig().Stepper("Columns", 5, 10, () => _savedCols, SetCols, 1, "Slots per row. Changed in a world, it shows next time you load one").Stepper("Rows", 12, 50, () => _savedRows, SetRows, 1, "Slots per column. Changed in a world, it shows next time you load one").Slider("Grid shift", 0f, 3f, () => Shift, SetShift, 0.05f, "0.00", "Moves the slots left so the last column isn't under the scrollbar. Updates straight away").Toggle("Unlock all rows", () => _savedUnlock, SetUnlock, "Removes the need to reach a certain level to unlock rows\nThey are all unlocked from the start. Changed in a world, it shows next time you load one")
		});
		ManualLogSource log = Core.Log;
		log?.LogInfo((object)"BetterInventory loaded.");
	}

	// Runs immediately after Awake() on game startup, so runs after mods have been loaded
	private void Start()
	{
		MoreSlots = ModRegistry.All.FirstOrDefault(m => m.Name == "More Inventory Slots");
		MoreSlotsOn = MoreSlots != null && MoreSlots.GetEnabled();
		if (MoreSlotsOn)
		{
			Enabled = _savedEnabled = false;
			L.Warn("MoreSlots is on, Better Inventory stays off");
		}
	}

	// Run on game update cycle, checking if player has just left a world
	private void Update()
	{
		// leaving a world is when saved changes can take over, the next one loads with them
		bool inWorld = BSWorld.InWorld;
		if (_wasInWorld && !inWorld)
		{
			ApplySaved();
		}
		_wasInWorld = inWorld;
	}

	// Saves any edit made to mods config while in-game?
	private static void ApplySaved()
	{
		if (BSWorld.InWorld)
		{
			L.Info("saved, used from the next world load");
			return;
		}
		Enabled = _savedEnabled;
		Cols = _savedCols;
		Rows = _savedRows;
		Unlock = _savedUnlock;
	}

	private static void SetShift(float value)
	{
		Shift = Mathf.Round(value * 100f) / 100f;
		PlayerPrefs.SetFloat("BetterInventory_Shift", Shift);
		PlayerPrefs.Save();
		foreach (var kv in BetInvGridPatch.Original)
		{
			if (kv.Key != null) BetInvGridPatch.Apply(kv.Key, kv.Value);
		}
	}

	private static void SetCols(int value)
	{
		_savedCols = value;
		PlayerPrefs.SetInt("BetterInventory_Cols", value);
		PlayerPrefs.Save();
		ApplySaved();
	}

	private static void SetRows(int value)
	{
		_savedRows = value;
		PlayerPrefs.SetInt("BetterInventory_Rows", value);
		PlayerPrefs.Save();
		ApplySaved();
	}

	private static void SetUnlock(bool value)
	{
		_savedUnlock = value;
		PlayerPrefs.SetInt("BetterInventory_Unlock", value ? 1 : 0);
		PlayerPrefs.Save();
		ApplySaved();
	}

	// Every extra inventory rows unlock every 10 levels after level 120
	public static int UnlockLevel(int row)
	{
		if (row < UnlockLevels.Length)
		{
			return UnlockLevels[row];
		}
		return UnlockLevels[^1] + 10 * (row - UnlockLevels.Length + 1);
	}

	// Check if current storage object is the players inventory and not their hotbar slots
	public static bool IsBag(ItemStorage storage)
	{
		if (Enabled && storage is InventoryStorage && storage is not QuickSlotStorage)
		{
			return storage.GetComponent<InvEquipment>() != null;
		}
		return false;
	}
}
