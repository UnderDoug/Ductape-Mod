using HarmonyLib;

using System;
using System.Collections.Generic;
using System.Text;

using XRL;
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
    public static class CanBeModdedEvent_Patches
    {
        private static bool doDebug => getClassDoDebug(nameof(CanBeModdedEvent_Patches));
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
            declaringType: typeof(CanBeModdedEvent),
            methodName: nameof(CanBeModdedEvent.Check),
            argumentTypes: new Type[] { typeof(GameObject), typeof(GameObject), typeof(string) },
            argumentVariations: new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal }
            )]
        [HarmonyPostfix]
        public static void ModificationApplicable_AllowFourth_Postfix(ref bool __result, GameObject Actor, GameObject Item, string ModName)
        {
            Debug.Entry(4,
                $"{nameof(CanBeModdedEvent)}." + 
                $"{nameof(CanBeModdedEvent.Check)}(" +
                $"Actor: {Actor?.ShortDisplayNameWithoutTitlesStripped ?? NULL}, " +
                $"Item: {Item?.ShortDisplayNameWithoutTitlesStripped ?? NULL}, " +
                $"ModName: {ModName ?? NULL})",
                Indent: Debug.LastIndent, Toggle: getDoDebug('X'));

            if (ModName != null)
            {
                Type modType = ModManager.ResolveType("XRL.World.Parts." + ModName);
                if (modType != null && Activator.CreateInstance(modType) is IModification modPart)
                {
                    __result = __result && (Item.GetModificationSlotsUsed() + modPart.GetModificationSlotUsage()) <= RuleSettings.MAXIMUM_ITEM_MODS;
                }
                else
                {
                    __result = Item.GetModificationSlotsUsed() < RuleSettings.MAXIMUM_ITEM_MODS;
                }
            }
        }
    }
}
