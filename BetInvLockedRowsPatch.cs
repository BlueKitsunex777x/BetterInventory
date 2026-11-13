using System;
using HarmonyLib;
using UnityEngine;

//
// -------------- Patches method for getting the amount of unlocked slots based off of current player level
//
[HarmonyPatch(typeof(PlayerInventoryStorage), "SlotsForLevel")]
internal static class BetInvUnlockedSlotsPatch
{
    private static bool Prefix(int level, ref int __result)
    {
        if (!BetterInventoryPlugin.Active) return true;
        int slots = BetterInventoryPlugin.UnlockSize(0);

        for (int row = 0; row < BetterInventoryPlugin.Rows - 1; row++)
        {
            if (level < BetterInventoryPlugin.UnlockLevel(row)) break;
            slots = BetterInventoryPlugin.UnlockSize(row + 1);
        }
        __result = Mathf.Min(slots, BetterInventoryPlugin.Slots);
        return false;
    }
}

//
// -------------- Patches method to check if int slots is a valid unlock amount
//
[HarmonyPatch(typeof(PlayerInventoryStorage), "IsUnlockStep")]
internal static class BetInvIsUnlockStepPatch
{
    private static void Prefix(int slots, ref bool __result)
    {
        if (!BetterInventoryPlugin.Active) return;

        try
        {
            int cols = BetterInventoryPlugin.Cols;
            __result = slots >= BetterInventoryPlugin.UnlockSize(0)
                && slots <= BetterInventoryPlugin.Slots
                && (slots - 4) % cols == 0;
        }
        catch (Exception e) { L.Err("BetInvIsUnlockStepPatch", e); }
    }
}

//
// -------------- Patching the amount of unlocked rows
//
[HarmonyPatch(typeof(PlayerInventoryStorage), "GetUnlockCount")]
internal static class BetInvUnlockPatch
{
    private static void Postfix(PlayerInventoryStorage __instance, ref int __result)
    {
        if (!BetterInventoryPlugin.Active) return;

        try
        {
            if (BetterInventoryPlugin.Unlock && BetterInventoryPlugin.IsBag(__instance))
            {
                __result = __instance.size;
            }
        }
        catch (Exception e) { L.Err("BetInvUnlockPatch", e); }
    }
}

//
// -------------- Patching total inventory rows
//
[HarmonyPatch(typeof(StorageLockedRows), "Init", new Type[] { typeof(Experience) })]
internal static class BetInvLockedRowsPatch
{
    //
    // ---------- Update rows
    //
    private static void Prefix(StorageLockedRows __instance)
    {
        if (__instance == null || __instance.storageRows == null || !BetterInventoryPlugin.Active) return;

        int totalRows = BetterInventoryPlugin.Rows - 1;
        var oldRows = __instance.storageRows;

        if (oldRows.Length >= totalRows) { UpdateExistingRows(__instance, oldRows); return; }

        // ---------- 1. Create a bigger array ----------
        var newRows = new StorageLockedRows.StorageRow[totalRows];

        // ---------- 1.1 Copy original banner into new array
        for (int i = 0; i < oldRows.Length; i++)
            newRows[i] = oldRows[i];

        // ---------- 2. Clone the missing ones ----------
        // We use the last existing banner as a template
        var template = oldRows[^1];

        var ui = __instance.GetComponent<UIItemStorage>();

        for (int i = oldRows.Length; i < totalRows; i++)
        {
            // Clone the visual objects
            var newRow = new StorageLockedRows.StorageRow { levelRow = UnityEngine.Object.Instantiate(template.levelRow, template.levelRow.transform.parent) };
            newRow.levelRow.gameObject.name = $"StorageLockedRow {i}";
            newRow.levelLabel = newRow.levelRow.GetComponentInChildren<UILabel>(); // or however the label is found

            // Get currect rows position
            Transform _rowTrans = newRow.levelRow.transform;
            Vector3 _pos = _rowTrans.localPosition;

            // Set correct Y position
            _pos.y -= ui.spacing;
            _rowTrans.localPosition = _pos;

            newRow.row = template.row; // usually null or shared, keep whatever the original has
            newRows[i] = newRow;
            template = newRow;
        }

        // ---------- 3. Replace the array ----------
        __instance.storageRows = newRows;

        // ---------- 4. Set data + positioning ----------
        UpdateExistingRows(__instance, newRows);
    }

    //
    // ---------- Helper method for updating rows
    //
    private static void UpdateExistingRows(StorageLockedRows __instance, StorageLockedRows.StorageRow[] rows)
    {
        var ui      = __instance.GetComponent<UIItemStorage>();
        int spacing = ui.spacing;

        // the game's banners are made for 5 slots, so grow them by one spacing per extra column
        int extraCols  = BetterInventoryPlugin.Cols - 5;
        int extraWidth = spacing * extraCols;

        // Take the first row as style reference
        Transform firstBack  = rows[0].levelRow.transform.Find("Back");
        Transform firstLabel = rows[0].levelLabel.transform;

        Vector3 newScale = firstBack.localScale;
        newScale.x += extraWidth;

        // old NGUI sprites grow from the middle, so shift half the extra to keep the left edge put
        Vector3 backPos  = firstBack.localPosition;
        Vector3 labelPos = firstLabel.localPosition;

        // Adjust for wider columns (your previous logic)
        backPos.x  -= extraWidth / 2f + 20 * extraCols;
        labelPos.x -= extraWidth / 2f + 20 * extraCols;

        for (int i = 0; i < rows.Length; i++)
        {
            rows[i].unlockLevel = BetterInventoryPlugin.UnlockLevel(i);
            rows[i].unlockSize  = BetterInventoryPlugin.UnlockSize(i);

            if (rows[i].levelLabel == null) continue;

            // Position this banner on the correct row
            // (assuming original rows are spaced by `spacing`)
            float y = backPos.y - i * spacing;

            Transform _back  = rows[i].levelRow.transform.Find("Back");
            Transform _label = rows[i].levelLabel.transform;

            if (_back == null) continue;

            _back.localScale     = newScale;
            _back.localPosition  = backPos;
            _label.localPosition = labelPos;
        }
    }
}