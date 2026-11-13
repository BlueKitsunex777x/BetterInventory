using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

[HarmonyPatch(typeof(UIItemStorage), "Awake")]
internal static class BetInvGridPatch
{
	internal static readonly Dictionary<UIItemStorage, float> Fit = [];
	internal static readonly Dictionary<UIItemStorage, Layout> Original = [];

	private static void Prefix(UIItemStorage __instance, out int __state)
	{
		__state = 0;
		try
		{
			if (BetterInventoryPlugin.Active && __instance.storage is PlayerInventoryStorage && !((Object)(object)__instance.template == (Object)null))
			{
				Fit[__instance] = (float)__instance.maxColumns / (float)BetterInventoryPlugin.Cols;
				int num = Mathf.Min(__instance.maxItemCount, __instance.maxRows * __instance.maxColumns);
				L.Info("Inventory grid rows " + __instance.maxRows + " columns " + __instance.maxColumns + " slots " + __instance.maxItemCount + " spacing " + __instance.spacing + " padding " + __instance.padding + " offset " + __instance.offset + " slotIndex " + __instance.slotIndex);
				if (num < BetterInventoryPlugin.Slots)
				{
					__state = __instance.maxRows;
					__instance.maxRows = Mathf.Max(__instance.maxRows, BetterInventoryPlugin.Rows);
					__instance.maxColumns = Mathf.Max(__instance.maxColumns, BetterInventoryPlugin.Cols);
					__instance.maxItemCount = Mathf.Max(__instance.maxItemCount, BetterInventoryPlugin.Slots);
				}
			}
		}
		catch (Exception e)
		{
			L.Err("BetterInventory: grid", e);
		}
	}

	private static void Postfix(UIItemStorage __instance)
	{
		try
		{
			if (BetterInventoryPlugin.Active && __instance.storage is PlayerInventoryStorage && !(__instance.template == null))
			{
                Layout layout = new()
                {
                    fit = (Fit.TryGetValue(__instance, out var value) ? Mathf.Min(1f, value) : 1f),
                    scale = __instance.transform.localScale,
                    count = __instance.Count,
                    pos = new Vector3[__instance.Count]
                };

                if (__instance.background != null)
				{
					layout.bgScale = __instance.background.transform.localScale;
				}

				Original[__instance] = layout;
				Apply(__instance, layout);
				L.Info($"inventory grid built with {layout.count} slots");
			}
		}
		catch (Exception e)
		{
			L.Err("grid post", e);
		}
	}

	internal static void Apply(UIItemStorage ui, Layout lay)
	{
		if (ui == null || lay == null) { return; }

		int count = ui.Count;
		for (int i = 0; i < count; i++)
		{
			UIStorageSlot val = ui[i];
			bool flag = i < BetterInventoryPlugin.Slots;
			val.gameObject.SetActive(flag);
			if (flag)
			{
				int num = i / BetterInventoryPlugin.Cols;
				int num2 = i % BetterInventoryPlugin.Cols;
				float num3 = BetterInventoryPlugin.Cols - 5;
				val.transform.localPosition = new Vector3((float)ui.padding + ((float)num2 + ui.offset - num3 * BetterInventoryPlugin.Shift + 0.5f) * (float)ui.spacing, (float)(-ui.padding) - ((float)num + 0.5f) * (float)ui.spacing, 0f);
			}
		}
		ui.transform.localScale = lay.scale * lay.fit;
		ui.background?.transform.localScale = new Vector3((float)ui.padding * 2f + (float)(BetterInventoryPlugin.Cols * ui.spacing), (float)ui.padding * 2f + (float)(BetterInventoryPlugin.Rows * ui.spacing), lay.bgScale.z);
	}

	internal class Layout
	{
		public Vector3[] pos;
		public int count;
		public Vector3 bgScale;
		public Vector3 scale;
		public float fit = 1f;
	}
}
