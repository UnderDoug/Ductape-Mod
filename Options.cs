using System;
using System.Collections.Generic;
using System.IO;
using XRL;

using static UD_Ductape_Mod.Const;

namespace UD_Ductape_Mod
{
    [HasModSensitiveStaticCache]
    [HasOptionFlagUpdate(Prefix = "Option_UD_Ductape_Mod_")]
    public static class Options
    {
        // Debug Settings
        [OptionFlag] public static int DebugVerbosity;
        [OptionFlag] public static bool DebugIncludeInMessage;
        [OptionFlag] public static bool DebugDuctapeModDescriptions;

        // Balance Settings
        [OptionFlag] public static bool AnyNumberOfMods;
        [OptionFlag] public static bool ScalingDamageChance;

    } //!-- public static class Options
}
