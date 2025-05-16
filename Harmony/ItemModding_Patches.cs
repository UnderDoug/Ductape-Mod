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
        [HarmonyPrefix]
        [HarmonyPatch(
            declaringType: typeof(ItemModding),
            methodName: nameof(ItemModding.ModificationApplicable),
            argumentTypes: new Type[] { typeof(string), typeof(GameObject), typeof(GameObject), typeof(string) },
            argumentVariations: new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal }
            )]
        public static bool ModificationApplicable_AllowFourth_Prefix(ref bool __result, string Name, GameObject Object, string Key, GameObject Actor = null)
        {
            if(false && Name == nameof(Mod_UD_Ductape) && Key != null)
            {
                Debug.Entry(4,
                    $"{nameof(ItemModding)}." + 
                    $"{nameof(ItemModding.ModificationApplicable)}(" +
                    $"Name: {Name ?? NULL}, " +
                    $"Key: {Key ?? NULL})",
                    Indent: Debug.LastIndent + 1, Toggle: true
                    );
            }
            if (Name == nameof(Mod_UD_Ductape) && Key == null)
            {
                Debug.Entry(4,
                    $"{nameof(ItemModding)}." +
                    $"{nameof(ItemModding.ModificationApplicable)}(" +
                    $"Name: {Name ?? NULL}, " +
                    $"Key: {Key ?? NULL})",
                    Indent: 0, Toggle: true
                    );
                // __result = ItemModding.ModificationApplicable(Name, Object, Actor, nameof(Mod_UD_Ductape));
                // return false;
            }
            return true;
        }

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

            __result = null;
        }
    }
}
