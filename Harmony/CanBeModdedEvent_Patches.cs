using HarmonyLib;

using System;
using System.Collections.Generic;
using System.Text;
using XRL;
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
    public static class CanBeModdedEvent_Patches
    {
        private static bool doDebug => false;

        [HarmonyPostfix]
        [HarmonyPatch(
            declaringType: typeof(CanBeModdedEvent),
            methodName: nameof(CanBeModdedEvent.Check),
            argumentTypes: new Type[] { typeof(GameObject), typeof(GameObject), typeof(string) },
            argumentVariations: new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal }
            )]
        public static void ModificationApplicable_AllowFourth_Postfix(ref bool __result, GameObject Actor, GameObject Item, string ModName)
        {
            Debug.Entry(4,
                $"{nameof(CanBeModdedEvent)}." + 
                $"{nameof(CanBeModdedEvent.Check)}(" +
                $"Actor: {Actor?.ShortDisplayNameWithoutTitlesStripped ?? NULL}, " +
                $"Item: {Item?.ShortDisplayNameWithoutTitlesStripped ?? NULL}, " +
                $"ModName: {ModName ?? NULL})",
                Indent: Debug.LastIndent, Toggle: doDebug
                );

            if (ModName != null)
            {
                Type type = ModManager.ResolveType("XRL.World.Parts." + ModName);
                if (type != null && Activator.CreateInstance(type) is IModification modPart)
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
