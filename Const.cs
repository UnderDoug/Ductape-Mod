using System;
using System.Collections.Generic;

using Debug = UD_Ductape_Mod.Debug;
using Options = UD_Ductape_Mod.Options;

namespace UD_Ductape_Mod
{
    public static class Const
    {
        public const string MOD_ID = "Ductape Mod";

        public const string DEBUG_OBJECT_CREATION_ANALYSIS = "UD_Debug_DoObjectCreationAnalysis";
        public const string DEBUG_HIGHLIGHT_CELLS = "UD_Debug_HighlightCells";

        public const string NULL = "\"null\"";

        public const string TICK = "\u221A";  // √
        public const string CROSS = "\u0058"; // X
        public const string BLOCK = "\u2588"; // █
        public const string STAR = "\u263C";  // ☼
        public const string SMLE = "\u263A";  // ☺︎
        public const string SMLE2 = "\u263B"; // ☻

        public const string VANDR = "\u251C"; // ├
        public const string VONLY = "\u2502"; // │
        public const string TANDR = "\u2514"; // └
        public const string HONLY = "\u2500"; // ─
        public const string SPACE = "\u0020"; //" "
        public const string NBSP  = "\u00A0"; //" " (NBSP)

        public const string ITEM = VANDR + HONLY + HONLY + SPACE; // "├── "
        public const string BRAN = VONLY + SPACE + SPACE + SPACE; // "│   "
        public const string LAST = TANDR + HONLY + HONLY + SPACE; // "└── "
        public const string DIST = SPACE + SPACE + SPACE + SPACE; // "    "
        public const string NACE = NBSP + NBSP + NBSP + NBSP;     // "    " (NBSP)

        public const int MAX_DIST = 9999999;

    } //!-- public static class Const
}