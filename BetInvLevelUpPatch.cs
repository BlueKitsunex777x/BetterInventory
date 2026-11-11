using System;
using BlockStoryMod;
using HarmonyLib;
using UnityEngine;

// the game unlocks 5 slots a level up, one row of its own 5 wide bag. with more columns that leaves every row
// short by the difference, the level banners slide out of line and overlap the slots until the next load.
// this unlocks up to the same count a fresh load would give for the level
[HarmonyPatch(typeof(StorageLockedRows), "CheckUnlock", new Type[] { typeof(Experience), typeof(bool) })]
internal static class BetInvLevelUpPatch
{
	private static bool Prefix(StorageLockedRows __instance, Experience xp)
	{
		if (!BetterInventoryPlugin.Active || xp == null || __instance.storage == null || !BetterInventoryPlugin.IsBag(__instance.storage))
		{
			return true;
		}
		try
		{
			int want = BetterInventoryPlugin.UnlockSize(0);
			for (int i = 0; i < BetterInventoryPlugin.Rows - 1 && xp.curLvl >= BetterInventoryPlugin.UnlockLevel(i); i++)
			{
				want = BetterInventoryPlugin.UnlockSize(i + 1);
			}
			want = Mathf.Min(want, BetterInventoryPlugin.Slots, __instance.storage.size);
			if (__instance.storage.unlocked < want)
			{
				__instance.storage.Unlock(want - __instance.storage.unlocked);
				L.Info("unlocked to " + __instance.storage.unlocked + " at level " + xp.curLvl);
			}
			__instance.Refresh();
		}
		catch (Exception e)
		{
			L.Err("BetInvLevelUpPatch", e);
		}
		return false;
	}
}
