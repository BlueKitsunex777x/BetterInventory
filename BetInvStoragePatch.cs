using System;
using BlockStoryMod;
using HarmonyLib;

[HarmonyPatch(typeof(ItemStorage), "Init")]
internal static class BetInvStoragePatch
{
    // sets the total slots of the player inventory to the slots from the config (Cols * Rows + 4(2x2 crafting grid))
    private static void Prefix(ItemStorage __instance)
    {
        try
        {
            if (!BetterInventoryPlugin.Active || !BetterInventoryPlugin.IsBag(__instance)) return;
            __instance.size = BetterInventoryPlugin.Slots;
            L.Info("inventory size " + __instance.size + " -> " + BetterInventoryPlugin.Slots);
        }
        catch (Exception e) { L.Err("BetterInventory: size", e); }
    }
}