using System;
using System.Collections.Generic;
using System.Linq;

using ConsoleLib.Console;

using Kobold;

using XRL;
using XRL.UI;
using XRL.Rules;
using XRL.World;
using XRL.World.Anatomy;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;
using XRL.World.Tinkering;
using XRL.World.ObjectBuilders;
using XRL.Language;

using XRL.World.Text.Delegates;
using XRL.World.Text.Attributes;

using static UD_Ductape_Mod.Const;

using Debug = UD_Ductape_Mod.Debug;
using Options = UD_Ductape_Mod.Options;

namespace UD_Ductape_Mod
{
    [HasVariableReplacer]
    public static class Utils
    {
        private static bool doDebug => true;
        private static bool getDoDebug (string MethodName)
        {
            if (MethodName == nameof(TryGetTilePath))
                return false;

            if (MethodName == nameof(ExplodingDie))
                return false;

            if (MethodName == nameof(Rumble))
                return false;

            return doDebug;
        }

        public static ModInfo ThisMod => ModManager.GetMod(MOD_ID);

        public static bool RegisterGameLevelEventHandlers()
        {
            Debug.Entry(1, $"Registering XRLGame Event Handlers...", Indent: 1);
            bool flag = The.Game != null;
            if (flag)
            {

                // ExampleHandler.Register();
            }
            else
            {
                Debug.Entry(2, $"The.Game is null, unable to register any events.", Indent: 2);
            }
            Debug.LoopItem(1, $"Event Handler Registration Finished", Indent: 1, Good: flag);
            return flag;
        }

        [ModSensitiveStaticCache(CreateEmptyInstance = true)]
        private static Dictionary<string, string> _TilePathCache = new();
        private static readonly List<string> TileSubfolders = new()
        {
            "",
            "Abilities",
            "Assets",
            "Blueprints",
            "Creatures",
            "Items",
            "Mutations",
            "NaturalWeapons",
            "Terrain",
            "Tiles",
            "Tiles2",
            "Walls",
            "Walls2",
            "Widgets",
        };
        public static string BuildCustomTilePath(string DisplayName)
        {
            return Grammar.MakeTitleCase(ColorUtility.StripFormatting(DisplayName)).Replace(" ", "");
        }
        public static bool TryGetTilePath(string TileName, out string TilePath, bool IsWholePath = false)
        {
            Debug.Entry(3, $"@ Utils.TryGetTilePath(string TileName: {TileName}, out string TilePath)", Indent: 2, Toggle: getDoDebug(nameof(TryGetTilePath)));

            bool wasFound = false;
            bool inCache;
            Debug.Entry(4, $"? if (_TilePathCache.TryGetValue(TileName, out TilePath))", Indent: 2, Toggle: getDoDebug(nameof(TryGetTilePath)));
            if (inCache = !_TilePathCache.TryGetValue(TileName, out TilePath))
            {
                Debug.Entry(4, $"_TilePathCache does not contain {TileName}", Indent: 3, Toggle: getDoDebug(nameof(TryGetTilePath)));
                Debug.Entry(4, $"x if (_TilePathCache.TryGetValue(TileName, out TilePath)) ?//", Indent: 2, Toggle: getDoDebug(nameof(TryGetTilePath)));

                Debug.Entry(4, $"Attempting to add \"{TileName}\" to _TilePathCache", Indent: 3, Toggle: getDoDebug(nameof(TryGetTilePath)));
                if (!wasFound && !_TilePathCache.TryAdd(TileName, TilePath))
                    Debug.Entry(3, $"!! Adding \"{TileName}\" to _TilePathCache failed", Indent: 3, Toggle: getDoDebug(nameof(TryGetTilePath)));

                if (IsWholePath)
                {
                    if (SpriteManager.HasTextureInfo(TileName))
                    {
                        TilePath = TileName;
                        _TilePathCache[TileName] = TileName;
                        Debug.CheckYeh(4, $"Tile: \"{TileName}\", Added entry to _TilePathCache", Indent: 3, Toggle: getDoDebug(nameof(TryGetTilePath)));
                    }
                    else
                    {
                        Debug.CheckNah(4, $"Tile: \"{TileName}\"", Indent: 3, Toggle: getDoDebug(nameof(TryGetTilePath)));
                    }
                }
                else
                {
                    Debug.Entry(4, $"Listing subfolders", Indent: 2, Toggle: getDoDebug(nameof(TryGetTilePath)));
                    Debug.Entry(4, $"> foreach (string subfolder  in TileSubfolders)", Indent: 2, Toggle: getDoDebug(nameof(TryGetTilePath)));
                    foreach (string subfolder in TileSubfolders)
                    {
                        Debug.LoopItem(4, $" \"{subfolder}\"", Indent: 3, Toggle: getDoDebug(nameof(TryGetTilePath)));
                    }
                    Debug.Entry(4, $"x foreach (string subfolder  in TileSubfolders) >//", Indent: 2, Toggle: getDoDebug(nameof(TryGetTilePath)));

                    Debug.Entry(4, $"> foreach (string subfolder in TileSubfolders)", Indent: 2, Toggle: getDoDebug(nameof(TryGetTilePath)));
                    Debug.Divider(3, "-", Count: 25, Indent: 2, Toggle: getDoDebug(nameof(TryGetTilePath)));
                    foreach (string subfolder in TileSubfolders)
                    {
                        string path = subfolder;
                        if (path != "") path += "/";
                        path += TileName;
                        if (SpriteManager.HasTextureInfo(path))
                        {
                            TilePath = path;
                            _TilePathCache[TileName] = TilePath;
                            Debug.CheckYeh(4, $"Tile: \"{path}\", Added entry to _TilePathCache", Indent: 3, Toggle: getDoDebug(nameof(TryGetTilePath)));
                        }
                        else
                        {
                            Debug.CheckNah(4, $"Tile: \"{path}\"", Indent: 3, Toggle: getDoDebug(nameof(TryGetTilePath)));
                        }
                    }
                    Debug.Divider(3, "-", Count: 25, Indent: 2, Toggle: getDoDebug(nameof(TryGetTilePath)));
                    Debug.Entry(4, $"x foreach (string subfolder in TileSubfolders) >//", Indent: 2, Toggle: getDoDebug(nameof(TryGetTilePath)));
                }
            }
            else
            {
                Debug.Entry(3, $"_TilePathCache contains {TileName}", TilePath ?? "null", Indent: 3, Toggle: getDoDebug(nameof(TryGetTilePath)));
            }
            string foundLocation = 
                inCache 
                ? "_TilePathCache" 
                : IsWholePath 
                    ? "files" 
                    : "supplied subfolders";

            Debug.Entry(3, $"Tile \"{TileName}\" {(TilePath == null ? "not" : "was")} found in {foundLocation}", Indent: 2, Toggle: getDoDebug(nameof(TryGetTilePath)));

            wasFound = TilePath != null;
            Debug.Entry(3, $"x Utils.TryGetTilePath(string TileName: {TileName}, out string TilePath) @//", Indent: 2, Toggle: getDoDebug(nameof(TryGetTilePath)));
            return wasFound;
        }

        public static string WeaponDamageString(int DieSize, int DieCount, int Bonus)
        {
            string output = $"{DieSize}d{DieCount}";

            if (Bonus > 0)
            {
                output += $"+{Bonus}";
            }
            else if (Bonus < 0)
            {
                output += Bonus;
            }

            return output;
        }

        // Ripped wholesale from ModGigantic.
        public static string GetProcessedItem(List<string> item, bool second, List<List<string>> items, GameObject obj)
        {
            if (item[0] == "")
            {
                if (second && item == items[0])
                {
                    return obj.It + " " + item[1];
                }
                return item[1];
            }
            if (item[0] == null)
            {
                if (second && item == items[0])
                {
                    return obj.Itis + " " + item[1];
                }
                if (item != items[0])
                {
                    bool flag = true;
                    foreach (List<string> item2 in items)
                    {
                        if (item2[0] != null)
                        {
                            flag = false;
                            break;
                        }
                    }
                    if (flag)
                    {
                        return item[1];
                    }
                }
                return obj.GetVerb("are", PrependSpace: false) + " " + item[1];
            }
            if (second && item == items[0])
            {
                return obj.It + obj.GetVerb(item[0]) + " " + item[1];
            }
            return obj.GetVerb(item[0], PrependSpace: false) + " " + item[1];
        } //!-- public static string GetProcessedItem(List<string> item, bool second, List<List<string>> items, GameObject obj)

        public static int ExplodingDie(int Number, DieRoll DieRoll, int Step = 1, int Limit = 0, int Indent = 0)
        {
            Debug.Entry(4,
                $"ExplodingDie(Number: {Number}, DieRoll: {DieRoll}, Step: {Step}, Limit: {Limit})",
                Indent: Indent, Toggle: getDoDebug(nameof(ExplodingDie)));

            int High = DieRoll.Max();
            int oldIndent = Indent;

            Debug.Entry(4, $"Explodes on High Roll: {High}", Indent: Indent, Toggle: getDoDebug(nameof(ExplodingDie)));
            Begin:
            if (Limit != 0 && High >= Limit)
            {
                Debug.Entry(4, "Limit 0 or DieRoll.Max() >= Limit", Indent: ++Indent, Toggle: getDoDebug(nameof(ExplodingDie)));
                Debug.Entry(4, "Exiting", Indent: Indent--, Toggle: getDoDebug(nameof(ExplodingDie)));
                Number = Limit;
                goto Exit;
            }

            Debug.Entry(4, $"Rollin' the Die!", Indent: Indent, Toggle: getDoDebug(nameof(ExplodingDie)));
            int Result = DieRoll.Resolve();
            if (Result == High)
            {
                Debug.Entry(4, $"continue: {Result}, Success!", Indent: ++Indent, Toggle: getDoDebug(nameof(ExplodingDie)));
                Debug.Entry(4, $"Increasing Number by {Step}", Indent: Indent, Toggle: getDoDebug(nameof(ExplodingDie)));
                Debug.Entry(4, $"Sending Number for another roll!", Indent: Indent, Toggle: getDoDebug(nameof(ExplodingDie)));
                Number += Step;
                Indent++;
                // Number = ExplodingDie(Number += Step, DieRoll, Step, Limit, ++Indent);
                goto Begin;
            }
            else
            {
                Debug.Entry(4, $"continue: {Result}, Failure!", Indent: ++Indent, Toggle: getDoDebug(nameof(ExplodingDie)));
            }

            Exit:
            Debug.Entry(4, $"Final Number: {Number}", Indent: oldIndent, Toggle: getDoDebug(nameof(ExplodingDie)));
            return Number;
        }

        public static int ExplodingDie(int Number, string DieRoll, int Step = 1, int Limit = 0, int Indent = 0)
        {
            DieRoll dieRoll = new(DieRoll);
            return ExplodingDie(Number, dieRoll, Step, Limit, Indent);
        }

        public static GameObjectBlueprint GetGameObjectBlueprint(string Blueprint)
        {
            GameObjectFactory.Factory.Blueprints.TryGetValue(Blueprint, out GameObjectBlueprint GameObjectBlueprint);
            return GameObjectBlueprint;
        }
        public static bool TryGetGameObjectBlueprint(string Blueprint, out GameObjectBlueprint GameObjectBlueprint)
        {
            GameObjectBlueprint = GetGameObjectBlueprint(Blueprint);
            return !GameObjectBlueprint.Is(null);
        }
        public static string MakeAndList(IReadOnlyList<string> Words, bool Serial = true, bool IgnoreCommas = false)
        {
            List<string> replacedList = new();
            foreach (string entry in Words)
            {
                replacedList.Add(entry.Replace(",", ";;"));
            }
            string andList = Grammar.MakeAndList(replacedList, Serial);
            return andList.Replace(";;", ",");
        }
        public static string Quote(string @string)
        {
            return $"\"{@string ?? "null"}\"";
        }

        public static BookInfo GetBook(string BookName)
        {
            BookUI.Books.TryGetValue(BookName, out BookInfo Book);
            return Book;
        }

        public static float Rumble(float Cause, float DurationFactor = 1.0f, float DurationMax = 1.0f, bool Async = false)
        {
            float duration = Math.Min(DurationMax, Cause * DurationFactor);
            CombatJuice.cameraShake(duration, Async: Async);
            Debug.Entry(4, 
                $"* {nameof(Rumble)}:"
                + $" Duration ({duration}),"
                + $" Cause ({Cause}),"
                + $" DurationFactor ({DurationFactor}), "
                + $"DurationMax({DurationMax})", 
                Toggle: getDoDebug(nameof(Rumble)));
            return duration;
        }
        public static float Rumble(double Cause, float DurationFactor = 1.0f, float DurationMax = 1.0f, bool Async = true)
        {
            return Rumble((float)Cause, DurationFactor, DurationMax, Async);
        }
        public static float Rumble(int Cause, float DurationFactor = 1.0f, float DurationMax = 1.0f, bool Async = true)
        {
            return Rumble((float)Cause, DurationFactor, DurationMax, Async);
        }

        public static bool IsMaxDistance(int Distance)
        {
            return Distance >= MAX_DIST;
        }

        public static string GetBoolString(List<string> list, bool @bool)
        {
            string trueString = null;
            string falseString = null;
            string output = "";

            foreach(string @string in list)
            {
                if (@string.StartsWith("true;;"))
                {
                    trueString = @string.Replace("true;;", "");
                }
                else if (@string.StartsWith("false;;"))
                {
                    falseString = @string.Replace("false;;", "");
                }
                else
                {
                    output += @string;
                }
            }
            return output.Replace("%s", @bool ? trueString : falseString);
        }

    } //!-- public static class Utils
}