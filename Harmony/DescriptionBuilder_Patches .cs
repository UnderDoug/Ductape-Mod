using HarmonyLib;

using System;
using System.Collections.Generic;
using System.Text;

using XRL.Language;
using XRL.World;

using static UD_Ductape_Mod.Const;

using Debug = UD_Ductape_Mod.Debug;
using Options = UD_Ductape_Mod.Options;

namespace UD_Ductape_Mod.Harmony
{
    [HarmonyPatch]
    public static class DescriptionBuilder_Patches
    {
        private static bool doDebug => false;

        [HarmonyPatch(
            declaringType: typeof(DescriptionBuilder),
            methodName: nameof(DescriptionBuilder.Resolve)
            )]
        [HarmonyPrefix]
        public static bool Resolve_DontSortWithCLauses_Prefix(ref DescriptionBuilder __instance, ref List<string> ___WithClauses)
        {
            Debug.Entry(4,
                $"# {nameof(DescriptionBuilder)}."
                + $"{nameof(DescriptionBuilder.Resolve)}()",
                Indent: Debug.LastIndent, Toggle: doDebug);

            if (___WithClauses != null && ___WithClauses.Count > 0)
            {
                string withClauses = Grammar.MakeAndList(___WithClauses);
                ___WithClauses.Clear();
                __instance.AddWithClause(withClauses);
            }
            return true;
        }
    }
}
