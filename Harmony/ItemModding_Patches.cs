using HarmonyLib;

using System;
using System.Collections.Generic;
using System.Text;

using XRL.World;
using XRL.World.Parts;
using XRL.World.Tinkering;

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
        public static bool ModificationApplicable_AllowFourth_Prefix(string Name, ref string Key)
        {
            UnityEngine.Debug.LogError($"{nameof(ItemModding_Patches)}.{nameof(ModificationApplicable_AllowFourth_Prefix)}(string Name: {Name})");
            if (Name == nameof(Mod_UD_Ductape))
            {
                Key = nameof(Mod_UD_Ductape);
                UnityEngine.Debug.LogError($"    {nameof(Key)} set to {nameof(Mod_UD_Ductape)}");
            }
            return true;
        }
    }
}
