using System;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using BlockStoryCore;
using BlockStoryMod;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

[BepInPlugin("com.BlueKitsunex777x.blockstory.BetterInventory", "Better Inventory", "1.1.2")]
[BepInDependency("com.mrc.blockstory.core")]
public class BetterInventoryPlugin : BaseUnityPlugin
{
	public static bool Enabled = PlayerPrefs.GetInt("BetterInventory_Enabled", 1) != 0;

	public static ModInfo MoreSlots;

	public static bool MoreSlotsOn;

	public static int Cols = PlayerPrefs.GetInt("BetterInventory_Cols", 6);

	public static int Rows = PlayerPrefs.GetInt("BetterInventory_Rows", 12);

	public static bool Unlock = PlayerPrefs.GetInt("BetterInventory_Unlock", 0) != 0;

	// the bag is sized when a world loads, so a change made in a world is saved here and used from the next load
	private static bool _savedEnabled = Enabled;

	private static int _savedCols = Cols;

	private static int _savedRows = Rows;

	private static bool _savedUnlock = Unlock;

	// how far left the grid moves per extra column, so the last column clears the scrollbar. 1.15 was the old fixed value
	public static float Shift = PlayerPrefs.GetFloat("BetterInventory_Shift", 1.15f);

	public static readonly int[] UnlockLevels = new int[11]
	{
		0, 9, 19, 29, 39, 54, 69, 84, 99, 109,
		119
	};

	public static bool Active => Enabled;

	public static int Slots => Cols * Rows + 4;

	public static int UnlockSize(int row)
	{
		return 4 + Cols * (row + 1);
	}

	private void Awake()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
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
		if (log != null)
		{
			log.LogInfo((object)"BetterInventory loaded.");
		}
	}

	private void Start()
	{
		MoreSlots = ModRegistry.All.FirstOrDefault((ModInfo m) => m.Name == "More Inventory Slots");
		MoreSlotsOn = MoreSlots != null && MoreSlots.GetEnabled();
		if (MoreSlotsOn)
		{
			Enabled = _savedEnabled = false;
			L.Warn("MoreSlots is on, Better Inventory stays off");
		}
	}

	private bool _wasInWorld;

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

	public static int UnlockLevel(int row)
	{
		if (row < UnlockLevels.Length)
		{
			return UnlockLevels[row];
		}
		return UnlockLevels[^1] + 10 * (row - UnlockLevels.Length + 1);
	}

	public static bool IsBag(ItemStorage storage)
	{
		if (Enabled && storage is InventoryStorage && !(storage is QuickSlotStorage))
		{
			return (Object)(object)((Component)storage).GetComponent<InvEquipment>() != (Object)null;
		}
		return false;
	}
}
