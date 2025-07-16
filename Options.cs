using System;
using System.Collections.Generic;
using System.IO;
using UD_Ductape_Mod.Harmony;
using XRL;
using XRL.World;
using XRL.World.Parts;

using static UD_Ductape_Mod.Const;
using static UD_Ductape_Mod.Utils;

namespace UD_Ductape_Mod
{
    [HasModSensitiveStaticCache]
    [HasOptionFlagUpdate(Prefix = "Option_UD_Ductape_Mod_")]
    public static class Options
    {
        public static bool doDebug = true;
        public static Dictionary<string, bool> classDoDebug = new()
        {
            // General
            { nameof(Mod_UD_Ductape), true },
            { nameof(UD_UtilitapeApplicator), true },

            // Harmony
            { nameof(ItemModding_Patches), true },
            { nameof(CanBeModdedEvent_Patches), true },

            // Events
            { nameof(UD_GetJostleActivityEvent), true },
            { nameof(UD_JostleObjectEvent), true },
        };

        public static bool getClassDoDebug(string Class)
        {
            if (classDoDebug.ContainsKey(Class))
            {
                return classDoDebug[Class];
            }
            return doDebug;
        }
        // Debug Settings
        [OptionFlag] public static int DebugVerbosity;
        [OptionFlag] public static bool DebugIncludeInMessage;
        [OptionFlag] public static bool DebugDuctapeModDescriptions;

        // Balance Settings
        [OptionFlag] public static float ActivityMultiplier;
        [OptionFlag] public static bool AnyNumberOfMods;
        [OptionFlag] public static bool ScalingDamageChance;

    } //!-- public static class Options
}
