using HarmonyLib;
using UnityEngine;

[HarmonyPatch(typeof(UIStorageSlot), "UpdateItem")]
internal static class BetInvLockedSlotsScrollPatch
{
	private static void Postfix(UIStorageSlot __instance)
	{
		if (__instance.slot != null && __instance.slot.Locked)
		{
			Collider _collider = __instance.GetComponentInChildren<Collider>();
			_collider?.enabled = true;
			
			Transform _highlight = __instance.transform.Find("Highlighted");
			_highlight?.gameObject.SetActive(false);
		}
	}
}
