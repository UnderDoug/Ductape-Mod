using HarmonyLib;

using System;
using System.Collections.Generic;
using System.Text;

using XRL.Rules;
using XRL.World;
using XRL.World.Parts;
using XRL.World.Tinkering;

using static UD_Ductape_Mod.Options;
using static UD_Ductape_Mod.Const;
using static UD_Ductape_Mod.Utils;

using Debug = UD_Ductape_Mod.Debug;
using Options = UD_Ductape_Mod.Options;

namespace UD_Ductape_Mod.Harmony
{
    [HarmonyPatch]
    public static class ItemModding_Patches
    {
        private static bool doDebug => getClassDoDebug(nameof(ItemModding_Patches));
        private static bool getDoDebug(object what = null)
        {
            List<object> doList = new()
            {
                'V',    // Vomit
            };
            List<object> dontList = new()
            {
                'X',    // Trace
            };

            if (what != null && doList.Contains(what))
                return true;

            if (what != null && dontList.Contains(what))
                return false;

            return doDebug;
        }

        [HarmonyPatch(
            declaringType: typeof(ItemModding),
            methodName: nameof(ItemModding.ModKey),
            argumentTypes: new Type[] { typeof(GameObject) },
            argumentVariations: new ArgumentType[] { ArgumentType.Normal }
            )]
        [HarmonyPostfix]
        public static void ModKey_IgnoreMaxMods_Prefix(ref string __result, GameObject Object)
        {
            string propertyOrTag = Object.GetPropertyOrTag("Mods");

            Debug.Entry(4,
                $"{nameof(ItemModding)}." +
                $"{nameof(ItemModding.ModKey)}(" +
                $"Object: {Object?.ShortDisplayNameWithoutTitlesStripped ?? NULL}, " +
                $"propertyOrTag: {propertyOrTag?.Quote()})",
                Indent: Debug.LastIndent, Toggle: getDoDebug('X'));

            if (!propertyOrTag.IsNullOrEmpty() && propertyOrTag != "None")
            {
                __result = propertyOrTag;
            }
        }
    }
}
