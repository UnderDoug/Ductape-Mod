using HarmonyLib;

using System;
using System.Collections.Generic;
using System.Text;
using XRL.Rules;
using XRL.World;
using XRL.World.Parts;
using XRL.World.Tinkering;
using static UD_Ductape_Mod.Const;

using Debug = UD_Ductape_Mod.Debug;
using Options = UD_Ductape_Mod.Options;

namespace UD_Ductape_Mod.Harmony
{
    [HarmonyPatch]
    public static class ItemModding_Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(
            declaringType: typeof(ItemModding),
            methodName: nameof(ItemModding.ModKey),
            argumentTypes: new Type[] { typeof(GameObject) },
            argumentVariations: new ArgumentType[] { ArgumentType.Normal }
            )]
        public static void ModKey_IgnoreMaxMods_Prefix(ref string __result, GameObject Object)
        {
            string propertyOrTag = Object.GetPropertyOrTag("Mods");

            Debug.Entry(4,
                $"{nameof(ItemModding)}." +
                $"{nameof(ItemModding.ModKey)}(" +
                $"Object: {Object?.ShortDisplayNameWithoutTitlesStripped ?? NULL}, " +
                $"propertyOrTag: {propertyOrTag?.Quote()})",
                Indent: Debug.LastIndent, Toggle: true
                );

            if (!propertyOrTag.IsNullOrEmpty() && propertyOrTag != "None")
            {
                __result = propertyOrTag;
                return;
            }
        }
    }
}
