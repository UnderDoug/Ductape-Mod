using System;
using System.Collections.Generic;
using System.Text;

using XRL.Language;
using XRL.Rules;
using XRL.UI;
using XRL.Wish;
using XRL.World;
using XRL.World.Capabilities;
using XRL.World.Tinkering;

using UD_Ductape_Mod;

using static UD_Ductape_Mod.Const;
using static UD_Ductape_Mod.Options;
using static UD_Ductape_Mod.Utils;
using Debug = UD_Ductape_Mod.Debug;
using Options = UD_Ductape_Mod.Options;

using SerializeField = UnityEngine.SerializeField;

namespace XRL.World.Parts
{
    [HasWishCommand]
    [Serializable]
    public class Mod_UD_Ductape : IModification, IModEventHandler<UD_JostleObjectEvent>
    {
        private static bool doDebug => getClassDoDebug(nameof(Mod_UD_Ductape));
        private static bool getDoDebug(object what = null)
        {
            List<object> doList = new()
            {
                'V',    // Vomit
                'X',    // Trace
                "TT",   // TurnTick
            };
            List<object> dontList = new()
            {
            };

            if (what != null && doList.Contains(what))
                return true;

            if (what != null && dontList.Contains(what))
                return false;

            return doDebug;
        }

        private static bool DebugDuctapeModDescriptions => Options.DebugDuctapeModDescriptions;
        private static bool AnyNumberOfMods => Options.AnyNumberOfMods;
        private static bool ScalingDamageChance => Options.ScalingDamageChance;
        private static float ActivityMultiplier => Options.ActivityMultiplier == 0 ? 1f : Options.ActivityMultiplier;

        public static int TurnsBetweenJostle = 0;

        [SerializeField]
        private int StoredTurns = 0;

        public const int CHANCE_IN = 10000;

        public const int ACTIVE_EXTREMELY = 625;
        public const int ACTIVE_VERY = 350;
        public const int ACTIVE = 175;
        public const int PASSIVE = 75;
        public const int PASSIVE_VERY = 25;
        public const int PASSIVE_EXTREMELY = 5;

        public static readonly string DeathReason = "jostled apart";

        public const string MOD_NAME = "held together by utilitape";
        public const string MOD_NAME_COLORED = "held together by {{utilitape|utilitape}}";

        public int Activity = 0;

        public List<string> ActivityAdded;

        public List<string> AllJostleSources = new();
        private List<string> CurrentJostleSources = new();
        private List<string> LastJostleSources = new();

        [SerializeField]
        private int TotalActivity = 0;
        [SerializeField]
        private int TimesActive = 0;
        [SerializeField]
        private int TimesJostled = 0;

        private float AverageActivity => (float)TotalActivity / (float)TimesActive;
        private float ActivityPerJostle => (float)TotalActivity / (float)TimesJostled;

        public int CumulativeJostledDamage = 0;
        public int LastJostledDamage = 0;

        private bool isMeleeWeapon => ParentObject != null && ParentObject.TryGetPart(out MeleeWeapon meleeWeapon) && !meleeWeapon.IsImprovised();
        private bool isMissileWeapon => ParentObject != null && ParentObject.HasPart<MissileWeapon>();
        private bool isThrownWeapon => ParentObject != null && ParentObject.TryGetPart(out ThrownWeapon thrownWeapon) && !thrownWeapon.IsImprovised();
        private bool isShield => ParentObject != null && ParentObject.HasPart<Shield>();
        private bool isArmor => ParentObject != null && ParentObject.TryGetPart(out Armor armor) && !armor.WornOn.IsNullOrEmpty();
        private bool isEquipment => isMeleeWeapon || isMissileWeapon || isThrownWeapon || isShield || isArmor;
        private bool isPowerDrawing => ParentObject != null && ParentObject.UsesCharge();
        private bool isPowering => ParentObject != null && ParentObject.HasPartDescendedFrom<IEnergyCell>();
        private bool isProperlyEquipped => ParentObject != null && ParentObject.IsEquippedProperly();
        private bool isWorn => ParentObject != null && ParentObject.IsWorn();

        private Statistic Hitpoints => ParentObject?.GetStat("Hitpoints");

        private GameObject Equipper => ParentObject?.Equipped;

        private GameObject Holder => ParentObject?.Holder;

        public Mod_UD_Ductape()
            : base()
        {
        }

        public Mod_UD_Ductape(int Tier)
            : base(Tier)
        {
        }

        public override bool AllowStaticRegistration()
        {
            return true;
        }
        public override int GetModificationSlotUsage()
        {
            return -1;
        }
        public override void Configure()
        {
            base.Configure();
            WorksOnSelf = true;
            WorksOnWearer = true;
            WorksOnHolder = true;
            WorksOnEquipper = true;
            WorksOnImplantee = true;
        }
        public override bool ModificationApplicable(GameObject Object)
        {
            return CanTape(Object);
        }

        public static bool CanTape(GameObject Object, string Context = "")
        {
            int indent = Debug.LastIndent;
            bool doDebug = Context == "Internal" && getDoDebug();

            Debug.Entry(2,
                $"{nameof(Mod_UD_Ductape)}." +
                $"{nameof(CanTape)}(" +
                $"{Object?.DebugName ?? NULL})",
                Indent: indent + 1, Toggle: doDebug);

            if (Object == null)
            {
                Debug.CheckNah(3, $"{nameof(Object)} null", Indent: indent + 2, Toggle: doDebug);
                Debug.LastIndent = indent;
                return false;
            }
            if (!Object.Understood())
            {
                Debug.CheckNah(3, $"{nameof(Object)} not understood", Indent: indent + 2, Toggle: doDebug);
                Debug.LastIndent = indent;
                return false;
            }

            if (!Object.HasStat("Hitpoints"))
            {
                Debug.CheckNah(3, $"{nameof(Object)} lacks Hitpoints", Indent: indent + 2, Toggle: doDebug);
                Debug.LastIndent = indent;
                return false;
            }

            if (!AnyNumberOfMods && Object.GetModificationSlotsUsed() != RuleSettings.MAXIMUM_ITEM_MODS)
            {
                Debug.CheckNah(3, $"{nameof(Object)} doesn't have otherwise max mods or {nameof(AnyNumberOfMods)} set to disabled",
                    Indent: indent + 2, Toggle: doDebug);
                Debug.LastIndent = indent;
                return false;
            }

            Debug.Entry(3, $"Checking whether {nameof(Object)} has any relevant parts...", Indent: indent + 2, Toggle: doDebug);
            Debug.LastIndent = indent;
            return Object.UsesCharge()
                || Object.HasPart<MeleeWeapon>()
                || Object.HasPart<MissileWeapon>()
                || Object.HasPart<ThrownWeapon>()
                || Object.HasPart<Shield>()
                || Object.HasPart<Armor>();
        }
        public static string GetDescription()
        {
            return Grammar.InitCap(MOD_NAME_COLORED) + ": this item can be modified one additional time (excluding this modification) but has a small chance whenever it's used to take damage equal to 1/4 of its max hitpoints.";
        }
        public override void ApplyModification()
        {
            if (ParentObject != null && !ParentObject.HasTagOrProperty("EquipmentFrameColors"))
            {
                ParentObject.SetStringProperty("EquipmentFrameColors", "yKyK");
            }
            base.ApplyModification();
        }
        public override void Attach()
        {
            if (ParentObject != null && !ParentObject.HasTagOrProperty("EquipmentFrameColors"))
            {
                ParentObject.SetStringProperty("EquipmentFrameColors", "yKyK");
            }
            base.Attach();
        }

        public static int AdjustActivity(GameObject Object, int Activity)
        {
            int output = Activity;
            if (ScalingDamageChance)
            {
                int modCount = Object.GetModificationSlotsUsed();
                float divisor = 1;
                divisor += 3 - Math.Max(0, Math.Min(modCount, 3));
                output = (int)(output * (1.0f / Math.Max(divisor, 1)));
            }
            return (int)(output * ActivityMultiplier);
        }
        public int AdjustActivity(int Activity)
        {
            return AdjustActivity(ParentObject, Activity);
        }

        public static bool Jostle(GameObject Item, int Activity, out int JostledDamage)
        {
            int indent = Debug.LastIndent;
            Debug.Entry(4, 
                $"* {nameof(Jostle)}(" 
                + $"Item: {Item?.DebugName ?? NULL}, " 
                + $"Activity: {Activity})",
                Indent: indent, Toggle: getDoDebug());

            JostledDamage = 0;
            if (UD_JostleObjectEvent.CheckFor(Item, Activity))
            {
                int activityOneInPadding = CHANCE_IN.ToString().Length;
                string activityString = Activity.ToString().PadLeft(activityOneInPadding, ' ');
                if (Activity.ChanceIn(CHANCE_IN))
                {
                    JostledDamage = (int)Math.Ceiling(Item.GetStat("Hitpoints").BaseValue * 0.25);
                    
                    Debug.Entry(4,
                        $"({activityString} in {CHANCE_IN})" +
                        $" {Item?.DebugName ?? NULL} took" +
                        $" {JostledDamage} damage from being knocked around!",
                        Indent: indent + 1, Toggle: getDoDebug());

                    Debug.LastIndent = indent;
                    return Item.TakeDamage(
                        Amount: JostledDamage,
                        Message: "from being {{utilitape|jostled}}!",
                        Attributes: "Disintigrate,Jostle",
                        DeathReason: $"You were {DeathReason}",
                        ThirdPersonDeathReason: $"{Item.It + Item.GetVerb("were")} {DeathReason}",
                        Attacker: Item?.equippedOrSelf(),
                        Source: Item?.equippedOrSelf(),
                        Perspective: Item?.equippedOrSelf(),
                        Accidental: true,
                        Indirect: true,
                        ShowForInanimate: true,
                        SilentIfNoDamage: true);
                }
                else
                {
                    Debug.Entry(4, 
                        $"({activityString} in {CHANCE_IN})" +
                        $" {Item?.DebugName ?? NULL}" +
                        $" was Jostled!", 
                        Indent: indent + 1, Toggle: getDoDebug());
                }
            }
            Debug.LastIndent = indent;
            return false;
        }
        public bool Jostle(int Activity, out int JostledDamage)
        {
            bool jostled = Jostle(ParentObject, Activity, out JostledDamage);
            TimesJostled++;
            if (jostled)
            {
                CumulativeJostledDamage += JostledDamage;
                LastJostledDamage = JostledDamage; 
                GotJostled(JostledDamage);
            }
            return jostled;
        }
        public bool Jostle(int Activity)
        {
            return Jostle(Activity, out _);
        }

        public void GotJostled(int JostledDamage = 0, MinEvent FromEvent = null, Event FromSEvent = null)
        {
            if (Holder != null && Holder.IsPlayerControlled() && JostledDamage != 0)
            {
                bool isEquipped = Equipper != null;
                string equipped = isEquipped ? "equipped " : "";
                string damageSource = "from being {{utilitape|jostled}}!";
                string message = $"=object.T's= {equipped}{ParentObject.BaseDisplayName} took {JostledDamage} damage {damageSource}";

                bool objectIsAtRisk = false;

                if (Hitpoints.Value <= (int)Math.Ceiling(Hitpoints.BaseValue * 0.25) && Hitpoints.Value > 0)
                {
                    objectIsAtRisk = true;
                    if (ParentObject.IsBroken())
                    {
                        message += $" {ParentObject.Itis} " + " {{r|busted}}!";
                    }
                    else
                    {
                        message += " =pronouns.Subjective= looks about ready to fall apart!";
                    }
                }
                if (FromEvent != null)
                {
                    if (FromEvent is BeforeDestroyObjectEvent E && (E.Reason.EndsWith(DeathReason) || E.ThirdPersonReason.EndsWith(DeathReason)))
                    {
                        message += " =pronouns.Subjective==verb:'ve:afterpronoun= been {{utilitape|jostled}} into useless pieces!";
                    }
                }
                if (EnableJostledPopups || (ParentObject.IsImportant() && EnableJostledPopupsForImportant) || objectIsAtRisk)
                {
                    Popup.Show(GameText.VariableReplace(message, Subject: ParentObject, Object: Holder), LogMessage: false);
                    AutoAct.Interrupt();
                }
            }
        }

        public bool CanAddActivity(MinEvent FromEvent = null, Event FromSEvent = null)
        {
            ActivityAdded ??= new();
            bool eventAdded = false;
            bool sEventAdded = false;
            string eventName;
            if (FromEvent != null)
            {
                eventName = FromEvent.GetType().Name;
                eventAdded = ActivityAdded.Contains(eventName);
            }
            if (FromSEvent != null)
            {
                eventName = FromSEvent.ID;
                sEventAdded = ActivityAdded.Contains(eventName);
            }
            return !eventAdded && !sEventAdded;
        }
        public bool CanAddActivity(string FromEvent = null)
        {
            ActivityAdded ??= new();
            return FromEvent.IsNullOrEmpty() || !ActivityAdded.Contains(FromEvent);
        }
        public bool CanAddActivity(Type FromEvent = null)
        {
            return CanAddActivity(FromEvent?.Name);
        }

        public void AddActivity(int Amount, MinEvent FromEvent = null, Event FromSEvent = null)
        {
            ActivityAdded ??= new();
            int activity = UD_GetJostleActivityEvent.For(ParentObject, AdjustActivity(Amount), FromEvent, FromSEvent);
            if (activity != 0)
            {
                Activity += activity;
                TotalActivity += activity;
                TimesActive++;
                if (FromEvent != null)
                {
                    string @event = FromEvent.GetType().Name;
                    ActivityAdded.TryAdd(@event);
                    AllJostleSources.TryAdd(@event);
                    CurrentJostleSources.TryAdd(@event);
                }
                if (FromSEvent != null)
                {
                    string sEvent = FromSEvent.ID;
                    ActivityAdded.TryAdd(sEvent);
                    AllJostleSources.TryAdd(sEvent);
                    CurrentJostleSources.TryAdd(sEvent);
                }
            }
        }

        public void ResetActivity()
        {
            Activity = 0;
            LastJostleSources = new(CurrentJostleSources);
            CurrentJostleSources = new();
            ActivityAdded = new();
            Activity = 0;
        }

        private Dictionary<string, int> StringyJostleEventIDs => new()
        {
            { "CommandFireMissile", ACTIVE_VERY },
            { "BeforeThrown", ACTIVE_EXTREMELY },
        };
        private Dictionary<Func<bool>, (string, int)> EquipperJostleEventIDs => new()
        {
            { delegate(){ return isProperlyEquipped; }, (nameof(EnteredCellEvent), EnteredCellEvent.ID) },
            { delegate(){ return isArmor && isProperlyEquipped; }, (nameof(GetDefenderHitDiceEvent), GetDefenderHitDiceEvent.ID) },
        };
        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register(EndTurnEvent.ID, EventOrder.EXTREMELY_EARLY);
            Registrar.Register(BeforeDestroyObjectEvent.ID, EventOrder.EXTREMELY_EARLY);
            Registrar.Register(ChargeUsedEvent.ID, EventOrder.EXTREMELY_EARLY); // Powered Object
            Registrar.Register(UseChargeEvent.ID, EventOrder.EXTREMELY_LATE); // Energy Cells
            foreach ((string eventID, float _) in StringyJostleEventIDs)
            {
                Registrar.Register(eventID);
            }
            Registrar.Register(GetDisplayNameEvent.ID, EventOrder.EXTREMELY_LATE);
            base.Register(Object, Registrar);
        }
        public override bool WantEvent(int ID, int cascade)
        {
            bool wantGetWeaponHitDice =
                isProperlyEquipped
             && isMeleeWeapon;

            bool wantBeforeFireMissileWeapons =
                isProperlyEquipped
             && isMissileWeapon;

            bool wantShieldBlock =
                isWorn
             && isProperlyEquipped
             && isShield;

            bool wantObjectEnteredCell =
                isWorn
             && isProperlyEquipped
             && isArmor;

            bool wantGetThrownWeaponFlexPhaseProvider =
                isThrownWeapon;

            return base.WantEvent(ID, cascade)
                || ID == GetShortDescriptionEvent.ID
                || ID == GetDebugInternalsEvent.ID
                || ID == EquippedEvent.ID
                || ID == UnequippedEvent.ID
                || (wantGetWeaponHitDice && ID == GetWeaponHitDiceEvent.ID)
                || (wantBeforeFireMissileWeapons && ID == BeforeFireMissileWeaponsEvent.ID)
                || (wantGetThrownWeaponFlexPhaseProvider && ID == GetThrownWeaponFlexPhaseProviderEvent.ID)
                || (wantShieldBlock && ID == AfterShieldBlockEvent.ID)
                || (wantObjectEnteredCell && ID == ObjectEnteredCellEvent.ID);
        }
        public override bool HandleEvent(EndTurnEvent E)
        {

            if (Holder != null && Holder.CurrentZone == The.ActiveZone
                && Activity > 0 && ++StoredTurns > TurnsBetweenJostle
                && Hitpoints.Value > 0 && !ParentObject.IsInGraveyard())
            {
                Debug.Entry(4,
                    $"@ {nameof(Mod_UD_Ductape)}."
                    + $"{nameof(HandleEvent)}("
                    + $"{nameof(EndTurnEvent)} E) "
                    + $"{ParentObject?.BaseDisplayName}, "
                    + $"Activity: {Activity}",
                    Indent: 0, Toggle: getDoDebug());

                Jostle(Activity);
                ResetActivity();
                StoredTurns = 0;
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetDisplayNameEvent E)
        {
            if (E.Understood() && !E.Object.HasProperName)
            {
                bool hasWiths = !E.DB.WithClauses.IsNullOrEmpty() && E.DB.WithClauses.Count > 0;

                if (hasWiths)
                {
                    E.AddWithClause(MOD_NAME_COLORED);
                }
                else
                {
                    E.AddClause(MOD_NAME_COLORED, DescriptionBuilder.ORDER_ADJUST_EXTREMELY_LATE);
                }

                // E.AddTag(clause, DescriptionBuilder.ORDER_ADJUST_EXTREMELY_EARLY);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetShortDescriptionEvent E)
        {
            E.Postfix.AppendRules(GetDescription());

            if (DebugDuctapeModDescriptions)
            {
                StringBuilder SB = Event.NewStringBuilder();

                string equipmentFrame = ParentObject.GetPropertyOrTag("EquipmentFrameColors");

                if (equipmentFrame.IsNullOrEmpty())
                {
                    equipmentFrame = "none";
                }
                else
                {
                    string coloredEquipmentFrame = "{{y|";
                    foreach (char c in equipmentFrame)
                    {
                        coloredEquipmentFrame += $"&{c}{c}";
                    }
                    equipmentFrame = coloredEquipmentFrame += "}}"; 
                }

                LastJostleSources ??= new();
                string lastJostleSources = "";
                if (!LastJostleSources.IsNullOrEmpty())
                {
                    foreach (string source in LastJostleSources)
                    {
                        if (!lastJostleSources.IsNullOrEmpty())
                        {
                            lastJostleSources += ", ";
                        }
                        lastJostleSources += source;
                    }
                }

                AllJostleSources ??= new();
                string allJostleSources = "";
                if (!AllJostleSources.IsNullOrEmpty())
                {
                    foreach (string source in AllJostleSources)
                    {
                        if (!allJostleSources.IsNullOrEmpty())
                        {
                            allJostleSources += ", ";
                        }
                        allJostleSources += source;
                    }
                }

                bool hasEquipper =
                    Equipper != null
                 && isEquipment
                 && isProperlyEquipped;

                string activityMultiplierColor = ActivityMultiplier switch
                {
                    0.5f => "G",
                    0.67f => "g",
                    1 => "W",
                    1.5f => "r",
                    2f => "R",
                    _ => "K",
                };

                SB.AppendColored("M", nameof(Mod_UD_Ductape)).Append(": ");
                SB.AppendLine();

                SB.AppendColored("W", $"Options")
                    .AppendLine();
                SB.Append(VANDR).Append("(").AppendColored("activityMultiplierColor", $"{ActivityMultiplier}")
                    .Append($"){HONLY}{nameof(ActivityMultiplier)}");
                SB.AppendLine();
                SB.Append(VANDR).Append($"[").Append(AnyNumberOfMods.YehNah()).Append($"]")
                    .Append(HONLY).Append($"{nameof(AnyNumberOfMods)}: ")
                    .AppendColored("B", $"{AnyNumberOfMods}");
                SB.AppendLine();
                SB.Append(TANDR).Append($"[{ScalingDamageChance.YehNah()}]{HONLY}{nameof(ScalingDamageChance)}: ")
                    .AppendColored("B", $"{ScalingDamageChance}");
                SB.AppendLine();

                SB.AppendColored("W", $"State")
                    .AppendLine();
                SB.Append(VANDR).Append("(").AppendColored("G", $"{Activity}")
                    .Append($"){HONLY}{nameof(Activity)}");
                SB.AppendLine();
                SB.Append(VANDR).Append("(").AppendColored("G", $"{AdjustActivity(Activity)}")
                    .Append($"){HONLY}{nameof(AdjustActivity)} ({ParentObject.GetModificationSlotsUsed()})");
                SB.AppendLine();
                SB.Append(VONLY).Append(VANDR).Append("(").AppendColored("Y", $"{CHANCE_IN}")
                    .Append($"){HONLY}{nameof(CHANCE_IN)}");
                SB.AppendLine();
                SB.Append(VONLY).Append(VANDR).Append("(").AppendColored("R", $"{AdjustActivity(ACTIVE_EXTREMELY)}")
                    .Append("/").AppendColored("R", $"{ACTIVE_EXTREMELY}")
                    .Append($"){HONLY}{nameof(ACTIVE_EXTREMELY)}");
                SB.AppendLine();
                SB.Append(VONLY).Append(VANDR).Append("(").AppendColored("r", $"{AdjustActivity(ACTIVE_VERY)}")
                    .Append("/").AppendColored("r", $"{ACTIVE_VERY}")
                    .Append($"){HONLY}{nameof(ACTIVE_VERY)}");
                SB.AppendLine();
                SB.Append(VONLY).Append(VANDR).Append("(").AppendColored("w", $"{AdjustActivity(ACTIVE)}")
                    .Append("/").AppendColored("w", $"{ACTIVE}")
                    .Append($"){HONLY}{nameof(ACTIVE)}");
                SB.AppendLine();
                SB.Append(VONLY).Append(VANDR).Append("(").AppendColored("W", $"{AdjustActivity(PASSIVE)}")
                    .Append("/").AppendColored("W", $"{PASSIVE}")
                    .Append($"){HONLY}{nameof(PASSIVE)}");
                SB.AppendLine();
                SB.Append(VONLY).Append(VANDR).Append("(").AppendColored("g", $"{AdjustActivity(PASSIVE_VERY)}")
                    .Append("/").AppendColored("g", $"{PASSIVE_VERY}")
                    .Append($"){HONLY}{nameof(PASSIVE_VERY)}");
                SB.AppendLine();
                SB.Append(VONLY).Append(TANDR).Append("(").AppendColored("G", $"{AdjustActivity(PASSIVE_EXTREMELY)}")
                    .Append("/").AppendColored("G", $"{PASSIVE_EXTREMELY}")
                    .Append($"){HONLY}{nameof(PASSIVE_EXTREMELY)}");
                SB.AppendLine();
                SB.Append(VANDR).Append("(").AppendColored("M", $"{AverageActivity}")
                    .Append($"){HONLY}{nameof(AverageActivity)}");
                SB.AppendLine();
                SB.Append(VANDR).Append("(").AppendColored("M", $"{ActivityPerJostle}")
                    .Append($"){HONLY}{nameof(ActivityPerJostle)}");
                SB.AppendLine();
                SB.Append(VANDR).Append("(").AppendColored("M", $"{CHANCE_IN / ActivityPerJostle}")
                    .Append($"){HONLY}OneIn");
                SB.AppendLine();
                SB.Append(VONLY).Append(VANDR).Append("(").AppendColored("m", $"{TotalActivity}")
                    .Append($"){HONLY}{nameof(TotalActivity)}");
                SB.AppendLine();
                SB.Append(VONLY).Append(VANDR).Append("(").AppendColored("m", $"{TimesActive}")
                    .Append($"){HONLY}{nameof(TimesActive)}");
                SB.AppendLine();
                SB.Append(VONLY).Append(TANDR).Append("(").AppendColored("m", $"{TimesJostled}")
                    .Append($"){HONLY}{nameof(TimesJostled)}");
                SB.AppendLine();
                SB.Append(VANDR).Append("(").AppendColored("c", $"{CumulativeJostledDamage}")
                    .Append($"){HONLY}{nameof(CumulativeJostledDamage)}");
                SB.AppendLine();
                SB.Append(VANDR).Append("(").AppendColored("c", $"{LastJostledDamage}")
                    .Append($"){HONLY}{nameof(LastJostledDamage)}");
                SB.AppendLine();
                SB.Append(VANDR).Append("(").AppendColored("o", $"{lastJostleSources.Quote()}")
                    .Append($"){HONLY}{nameof(LastJostleSources)}");
                SB.AppendLine();
                SB.Append(VANDR).Append("(").AppendColored("o", $"{allJostleSources.Quote()}")
                    .Append($"){HONLY}{nameof(AllJostleSources)}");
                SB.AppendLine();
                SB.Append(TANDR).Append("(").AppendColored("y", $"{equipmentFrame}")
                    .Append($"){HONLY}EquipmentFrameColors");
                SB.AppendLine();

                SB.AppendColored("W", $"Turns")
                    .AppendLine();
                SB.Append(VANDR).Append("(").AppendColored("y", $"{TurnsBetweenJostle}")
                    .Append($"){HONLY}{nameof(TurnsBetweenJostle)}");
                SB.AppendLine();
                SB.Append(TANDR).Append("(").AppendColored("y", $"{StoredTurns}")
                    .Append($"){HONLY}{nameof(StoredTurns)}");
                SB.AppendLine();

                SB.AppendColored("W", $"Bools")
                    .AppendLine();
                SB.Append(VANDR).Append($"[").Append(isMeleeWeapon.YehNah()).Append($"]").Append(HONLY)
                    .Append($"{nameof(isMeleeWeapon)}: ")
                    .AppendColored("B", $"{isMeleeWeapon}");
                SB.AppendLine();
                SB.Append(VANDR).Append($"[").Append(isMissileWeapon.YehNah()).Append($"]").Append(HONLY)
                    .Append($"{nameof(isMissileWeapon)}: ")
                    .AppendColored("B", $"{isMissileWeapon}");
                SB.AppendLine();
                SB.Append(VANDR).Append($"[").Append(isThrownWeapon.YehNah()).Append($"]").Append(HONLY)
                    .Append($"{nameof(isThrownWeapon)}: ")
                    .AppendColored("B", $"{isThrownWeapon}");
                SB.AppendLine();
                SB.Append(VANDR).Append($"[").Append(isShield.YehNah()).Append($"]").Append(HONLY)
                    .Append($"{nameof(isShield)}: ")
                    .AppendColored("B", $"{isShield}");
                SB.AppendLine();
                SB.Append(VANDR).Append($"[").Append(isArmor.YehNah()).Append($"]").Append(HONLY)
                    .Append($"{nameof(isArmor)}: ")
                    .AppendColored("B", $"{isArmor}");
                SB.AppendLine();
                SB.Append(VANDR).Append($"[").Append(isPowerDrawing.YehNah()).Append($"]").Append(HONLY)
                    .Append($"{nameof(isPowerDrawing)}: ")
                    .AppendColored("B", $"{isPowerDrawing}");
                SB.AppendLine();
                SB.Append(VANDR).Append($"[").Append(isPowering.YehNah()).Append($"]").Append(HONLY)
                    .Append($"{nameof(isPowering)}: ")
                    .AppendColored("B", $"{isPowering}");
                SB.AppendLine();
                SB.Append(TANDR).Append($"[").Append(isProperlyEquipped.YehNah()).Append($"]").Append(HONLY)
                    .Append($"{nameof(isProperlyEquipped)}: ")
                    .AppendColored("B", $"{isProperlyEquipped}");

                E.Infix.AppendLine().AppendRules(Event.FinalizeString(SB));
            }

            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetDebugInternalsEvent E)
        {
            int oneIn = (int)(CHANCE_IN / ActivityPerJostle);

            LastJostleSources ??= new();
            string lastJostleSources = "";
            if (!LastJostleSources.IsNullOrEmpty())
            {
                foreach (string source in LastJostleSources)
                {
                    if (!lastJostleSources.IsNullOrEmpty())
                    {
                        lastJostleSources += ", ";
                    }
                    lastJostleSources += source;
                }
            }

            AllJostleSources ??= new();
            string allJostleSources = "";
            if (!AllJostleSources.IsNullOrEmpty())
            {
                foreach (string source in AllJostleSources)
                {
                    if (!allJostleSources.IsNullOrEmpty())
                    {
                        allJostleSources += ", ";
                    }
                    allJostleSources += source;
                }
            }

            string equipmentFrame = ParentObject.GetPropertyOrTag("EquipmentFrameColors") ?? "none";

            E.AddEntry(this, $"{nameof(ScalingDamageChance)}", $"{ScalingDamageChance}");
            E.AddEntry(this, $"{nameof(AnyNumberOfMods)}", $"{AnyNumberOfMods}");
            E.AddEntry(this, $"{nameof(ScalingDamageChance)}", $"{ScalingDamageChance}");
            E.AddEntry(this, $"{nameof(Activity)}", $"{Activity}");
            E.AddEntry(this, $"{nameof(AdjustActivity)}", $"{AdjustActivity(Activity)}");
            E.AddEntry(this, $"{nameof(ParentObject.GetModificationSlotsUsed)}", $"{ParentObject.GetModificationSlotsUsed()}");
            E.AddEntry(this, $"{nameof(CHANCE_IN)}", $"{CHANCE_IN}");
            E.AddEntry(this, $"{nameof(ACTIVE_EXTREMELY)}", $"{AdjustActivity(ACTIVE_EXTREMELY)}/{ACTIVE_EXTREMELY}");
            E.AddEntry(this, $"{nameof(ACTIVE_VERY)}", $"{AdjustActivity(ACTIVE_VERY)}/{ACTIVE_VERY}");
            E.AddEntry(this, $"{nameof(ACTIVE)}", $"{AdjustActivity(ACTIVE)}/{ACTIVE}");
            E.AddEntry(this, $"{nameof(PASSIVE)}", $"{AdjustActivity(PASSIVE)}/{PASSIVE}");
            E.AddEntry(this, $"{nameof(PASSIVE_VERY)}", $"{AdjustActivity(PASSIVE_VERY)}/{PASSIVE_VERY}");
            E.AddEntry(this, $"{nameof(PASSIVE_EXTREMELY)}", $"{AdjustActivity(PASSIVE_EXTREMELY)}/{PASSIVE_EXTREMELY}");
            E.AddEntry(this, $"{nameof(AverageActivity)}", $"{AverageActivity}");
            E.AddEntry(this, $"{nameof(ActivityPerJostle)}", $"{ActivityPerJostle}");
            E.AddEntry(this, $"{nameof(oneIn)}", $"{oneIn}");
            E.AddEntry(this, $"{nameof(TotalActivity)}", $"{TotalActivity}");
            E.AddEntry(this, $"{nameof(TimesActive)}", $"{TimesActive}");
            E.AddEntry(this, $"{nameof(TimesJostled)}", $"{TimesJostled}");
            E.AddEntry(this, $"{nameof(CumulativeJostledDamage)}", $"{CumulativeJostledDamage}");
            E.AddEntry(this, $"{nameof(LastJostledDamage)}", $"{LastJostledDamage}");
            E.AddEntry(this, $"{nameof(lastJostleSources)}", $"{lastJostleSources.Quote()}");
            E.AddEntry(this, $"{nameof(allJostleSources)}", $"{allJostleSources.Quote()}");
            E.AddEntry(this, $"{nameof(equipmentFrame)}", $"{equipmentFrame.Quote()}");
            E.AddEntry(this, $"{nameof(TurnsBetweenJostle)}", $"{TurnsBetweenJostle}");
            E.AddEntry(this, $"{nameof(StoredTurns)}", $"{StoredTurns}");
            E.AddEntry(this, $"{nameof(isMeleeWeapon)}", $"{isMeleeWeapon}");
            E.AddEntry(this, $"{nameof(isMissileWeapon)}", $"{isMissileWeapon}"); 
            E.AddEntry(this, $"{nameof(isThrownWeapon)}", $"{isThrownWeapon}"); 
            E.AddEntry(this, $"{nameof(isShield)}", $"{isShield}"); 
            E.AddEntry(this, $"{nameof(isArmor)}", $"{isArmor}"); 
            E.AddEntry(this, $"{nameof(isPowerDrawing)}", $"{isPowerDrawing}"); 
            E.AddEntry(this, $"{nameof(isPowering)}", $"{isPowering}"); 
            E.AddEntry(this, $"{nameof(isProperlyEquipped)}", $"{isProperlyEquipped}"); 
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(BeforeDestroyObjectEvent E)
        {
            GotJostled(LastJostledDamage, FromEvent: E);
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(EquippedEvent E)
        {
            Debug.Entry(3,
                $"@ {nameof(Mod_UD_Ductape)}."
                + $"{nameof(HandleEvent)}("
                + $"{nameof(EquippedEvent)} E) "
                + $"{ParentObject?.BaseDisplayName}",
                Indent: 0, Toggle: getDoDebug());

            foreach ((Func<bool> check, (string eventName,int eventID)) in EquipperJostleEventIDs)
            {
                Debug.LoopItem(4, $"{eventName}", Good: check.Invoke(), Indent: 1, Toggle: getDoDebug());
                if (check.Invoke())
                {
                    E.Actor.RegisterEvent(this, eventID);
                }
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(UnequippedEvent E)
        {
            Debug.Entry(3,
                $"@ {nameof(Mod_UD_Ductape)}."
                + $"{nameof(HandleEvent)}("
                + $"{nameof(EquippedEvent)} E) "
                + $"{ParentObject?.BaseDisplayName}",
                Indent: 0, Toggle: getDoDebug());

            foreach ((Func<bool> _, (string eventName, int eventID)) in EquipperJostleEventIDs)
            {
                Debug.CheckYeh(4, $"{eventName}", Indent: 1, Toggle: getDoDebug());
                E.Actor.UnregisterEvent(this, eventID);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(ChargeUsedEvent E)
        {
            bool hasEquipper =
                Equipper != null
             && isEquipment
             && isProperlyEquipped;

            if (E.Source == ParentObject && isPowerDrawing && CanAddActivity(FromEvent: E)) // Powered Objects
            {
                int activity = hasEquipper ? PASSIVE_VERY : PASSIVE_EXTREMELY;

                Debug.Entry(3,
                    $"@ {nameof(Mod_UD_Ductape)}."
                    + $"{nameof(HandleEvent)}("
                    + $"{nameof(ChargeUsedEvent)} E) "
                    + $"{ParentObject?.BaseDisplayName}, "
                    + $"Activity: {AdjustActivity(activity)}",
                    Indent: 0, Toggle: getDoDebug());

                AddActivity(activity, FromEvent: E);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(UseChargeEvent E)
        {
            if (E.Source == ParentObject && isPowering && CanAddActivity(FromEvent: E)) // Energy Cells
            {
                Debug.Entry(3,
                    $"@ {nameof(Mod_UD_Ductape)}."
                    + $"{nameof(HandleEvent)}("
                    + $"{nameof(UseChargeEvent)} E) "
                    + $"{E.Source?.BaseDisplayName}, "
                    + $"Activity: {AdjustActivity(PASSIVE_VERY)}",
                    Indent: 0, Toggle: getDoDebug());

                AddActivity(PASSIVE_VERY, FromEvent: E);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetWeaponHitDiceEvent E)
        {
            if(E.Weapon == ParentObject && CanAddActivity(FromEvent: E))
            {
                Debug.Entry(3,
                    $"@ {nameof(Mod_UD_Ductape)}."
                    + $"{nameof(HandleEvent)}("
                    + $"{nameof(GetWeaponHitDiceEvent)} E) "
                    + $"{E.Weapon?.BaseDisplayName}, "
                    + $"Activity: {AdjustActivity(ACTIVE)}",
                    Indent: 0, Toggle: getDoDebug());

                AddActivity(ACTIVE, FromEvent: E);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(BeforeFireMissileWeaponsEvent E)
        {
            if (E.Actor == Equipper && isMissileWeapon && CanAddActivity(FromEvent: E))
            {
                Debug.Entry(3,
                    $"@ {nameof(Mod_UD_Ductape)}."
                    + $"{nameof(HandleEvent)}("
                    + $"{nameof(BeforeFireMissileWeaponsEvent)} E) "
                    + $"{ParentObject?.BaseDisplayName}, "
                    + $"Activity: {AdjustActivity(ACTIVE_VERY)}",
                    Indent: 0, Toggle: getDoDebug());

                AddActivity(ACTIVE_VERY, FromEvent: E);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetThrownWeaponFlexPhaseProviderEvent E)
        {
            if (E.Actor == Equipper && isThrownWeapon && CanAddActivity(FromEvent: E))
            {
                bool isImprovised = !ParentObject.TryGetPart(out ThrownWeapon thrownWeapon) || thrownWeapon.IsImprovised();
                int activity = isImprovised ? ACTIVE_EXTREMELY : ACTIVE_VERY;
                Debug.Entry(3,
                    $"@ {nameof(Mod_UD_Ductape)}."
                    + $"{nameof(HandleEvent)}("
                    + $"{nameof(GetThrownWeaponFlexPhaseProviderEvent)} E) "
                    + $"{ParentObject?.BaseDisplayName}, "
                    + $"Activity: {AdjustActivity(activity)}",
                    Indent: 0, Toggle: getDoDebug());

                AddActivity(activity, FromEvent: E);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(AfterShieldBlockEvent E)
        {
            if (E.Shield == ParentObject && isShield && CanAddActivity(FromEvent: E))
            {
                Debug.Entry(3,
                    $"@ {nameof(Mod_UD_Ductape)}."
                    + $"{nameof(HandleEvent)}("
                    + $"{nameof(AfterShieldBlockEvent)} E) "
                    + $"{E.Shield?.BaseDisplayName}, "
                    + $"Activity: {AdjustActivity(ACTIVE)}",
                    Indent: 0, Toggle: getDoDebug());

                AddActivity(ACTIVE, FromEvent: E);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(EnteredCellEvent E)
        {
            if (E.Actor == Equipper && CanAddActivity(FromEvent: E))
            {
                int activity = isArmor ? PASSIVE : PASSIVE_VERY;

                Debug.Entry(3,
                    $"@ {nameof(Mod_UD_Ductape)}."
                    + $"{nameof(HandleEvent)}("
                    + $"{nameof(EnteredCellEvent)} E) "
                    + $"{ParentObject?.BaseDisplayName}, "
                    + $"Activity: {AdjustActivity(activity)}",
                    Indent: 0, Toggle: getDoDebug());

                AddActivity(activity, FromEvent: E);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(ObjectEnteredCellEvent E)
        {
            if (E.Object == ParentObject && Equipper == null && CanAddActivity(FromEvent: E))
            {
                Debug.Entry(3,
                    $"@ {nameof(Mod_UD_Ductape)}."
                    + $"{nameof(HandleEvent)}("
                    + $"{nameof(ObjectEnteredCellEvent)} E) "
                    + $"{E.Object?.BaseDisplayName}, "
                    + $"Activity: {AdjustActivity(PASSIVE_EXTREMELY)}",
                    Indent: 0, Toggle: getDoDebug());

                AddActivity(PASSIVE_EXTREMELY, FromEvent: E);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetDefenderHitDiceEvent E)
        {
            if (E.Defender == Equipper && CanAddActivity(FromEvent: E))
            {
                bool isBodyEquipment = ParentObject.TryGetPart(out Armor armor) && armor.WornOn.Contains("Body");
                int activity = isBodyEquipment ? ACTIVE_EXTREMELY : ACTIVE_VERY;

                Debug.Entry(3,
                    $"@ {nameof(Mod_UD_Ductape)}."
                    + $"{nameof(HandleEvent)}("
                    + $"{nameof(GetDefenderHitDiceEvent)} E) "
                    + $"{ParentObject?.BaseDisplayName}, "
                    + $"Activity: {AdjustActivity(activity)}",
                    Indent: 0, Toggle: getDoDebug());

                AddActivity(activity, FromEvent: E);
            }
            return base.HandleEvent(E);
        }
        public override bool FireEvent(Event E)
        {
            if (StringyJostleEventIDs.ContainsKey(E.ID) && CanAddActivity(FromSEvent: E))
            {
                int activity = StringyJostleEventIDs[E.ID];

                Debug.Entry(3,
                    $"@ {nameof(Mod_UD_Ductape)}."
                    + $"{nameof(FireEvent)}("
                    + $"{nameof(Event)} E.{nameof(E.ID)}: {E.ID}) "
                    + $"{ParentObject?.BaseDisplayName}, "
                    + $"Activity: {AdjustActivity(activity)}",
                    Indent: 0, Toggle: getDoDebug());

                AddActivity(activity, FromSEvent: E);
            }
            return base.FireEvent(E);
        }

        [WishCommand(Command = "utilitape test kit")]
        public static void UtilitapeTestKitWishHandler()
        {
            GameObject player = The.Player;

            GameObject item = null;
            for (int i = 0; i < 50; i++)
            {
                item = GameObjectFactory.Factory.CreateObject("Fixit Spray", BonusModChance: -9999, Context: "Wish");
                item.MakeUnderstood();
                player.ReceiveObject(item);

                item = GameObjectFactory.Factory.CreateObject("UD_Utilitape", BonusModChance: -9999, Context: "Wish");
                item.MakeUnderstood();
                player.ReceiveObject(item);

                if (i < 15)
                {
                    item = GameObjectFactory.Factory.CreateObject("Antimatter Cell", BonusModChance: -9999, Context: "Wish");
                    item.ApplyModification("ModRadioPowered", Actor: player);
                    item.ApplyModification("ModHighCapacity", Actor: player);
                    if (i < 10)
                    {
                        if (!item.ApplyModification("Mod_UD_RegenNanobots", Actor: player))
                        {
                            item.ApplyModification("ModMetered", Actor: player);
                        }
                    }
                    item.ApplyModification("ModGigantic", Actor: player);
                    item.MakeUnderstood();
                    player.ReceiveObject(item);
                }
            }
            item = GameObjectFactory.Factory.CreateObject("VISAGE", BonusModChance: -9999, Context: "Wish");
            item.ApplyModification("ModNav", Actor: player);
            item.ApplyModification("ModPolarized", Actor: player);
            if (!item.ApplyModification("Mod_UD_RegenNanobots", Actor: player))
            {
                item.ApplyModification("ModWillowy", Actor: player);
            }
            item.MakeUnderstood();
            player.ReceiveObject(item);

            item = GameObjectFactory.Factory.CreateObject("Dagger8", BonusModChance: -9999, Context: "Wish");
            item.ApplyModification("ModCounterweighted", Actor: player);
            item.ApplyModification("ModSharp", Actor: player);
            if (!item.ApplyModification("Mod_UD_RegenNanobots", Actor: player))
            {
                item.ApplyModification("ModWillowy", Actor: player);
            }
            item.ApplyModification("Mod_UD_Ductape", Actor: player);
            item.ApplyModification("ModFlaming", Actor: player);
            item.MakeUnderstood();
            player.ReceiveObject(item);

            for (int i = 0; i < 2; i++)
            {
                item = GameObjectFactory.Factory.CreateObject("Dagger8", BonusModChance: -9999, Context: "Wish");
                item.ApplyModification("ModCounterweighted", Actor: player);
                item.ApplyModification("ModSharp", Actor: player);
                if (!item.ApplyModification("Mod_UD_RegenNanobots", Actor: player))
                {
                    item.ApplyModification("ModWillowy", Actor: player);
                }
                item.ApplyModification("Mod_UD_Ductape", Actor: player);
                item.ApplyModification("ModMasterwork", Actor: player);
                item.MakeUnderstood();
                player.ReceiveObject(item);
            }

            item = GameObjectFactory.Factory.CreateObject("Phase Cannon", BonusModChance: -9999, Context: "Wish");
            item.ApplyModification("ModScoped", Actor: player);
            item.ApplyModification("ModNanon", Actor: player);
            if (!item.ApplyModification("Mod_UD_RegenNanobots", Actor: player))
            {
                item.ApplyModification("ModLacquered", Actor: player);
            }
            item.ApplyModification("Mod_UD_Ductape", Actor: player);
            item.ApplyModification("ModWillowy", Actor: player);
            item.MakeUnderstood();
            player.ReceiveObject(item);

            item = GameObjectFactory.Factory.CreateObject("Anti-Gravity Boots", BonusModChance: -9999, Context: "Wish");
            item.ApplyModification("ModSpringLoaded", Actor: player);
            item.ApplyModification("ModCleated", Actor: player);
            if (!item.ApplyModification("Mod_UD_RegenNanobots", Actor: player))
            {
                item.ApplyModification("ModWillowy", Actor: player);
            }
            item.ApplyModification("Mod_UD_Ductape", Actor: player);
            item.ApplyModification("ModRefractive", Actor: player);
            item.MakeUnderstood();
            player.ReceiveObject(item);

            item = GameObjectFactory.Factory.CreateObject("Flawless Crysteel Shield", BonusModChance: -9999, Context: "Wish");
            item.ApplyModification("ModSpiked", Actor: player);
            item.ApplyModification("ModWillowy", Actor: player);
            if (!item.ApplyModification("Mod_UD_RegenNanobots", Actor: player))
            {
                item.ApplyModification("ModWillowy", Actor: player);
            }
            item.ApplyModification("Mod_UD_Ductape", Actor: player);
            item.ApplyModification("ModRefractive", Actor: player);
            item.MakeUnderstood();
            player.ReceiveObject(item);

            item = GameObjectFactory.Factory.CreateObject("Transkinetic Cuffs", BonusModChance: -9999, Context: "Wish");
            item.ApplyModification("ModSturdy", Actor: player);
            item.ApplyModification("ModOverloaded", Actor: player);
            if (!item.ApplyModification("Mod_UD_RegenNanobots", Actor: player))
            {
                item.ApplyModification("ModWillowy", Actor: player);
            }
            item.MakeUnderstood();
            player.ReceiveObject(item);

            item = GameObjectFactory.Factory.CreateObject("Transkinetic Cuffs", BonusModChance: -9999, Context: "Wish");
            item.ApplyModification("ModSturdy", Actor: player);
            item.ApplyModification("ModWillowy", Actor: player);
            if (!item.ApplyModification("Mod_UD_RegenNanobots", Actor: player))
            {
                item.ApplyModification("ModLacquered", Actor: player);
            }
            item.MakeUnderstood();
            player.ReceiveObject(item);

            player.RequirePart<BitLocker>().AddAllBits(6000);

            player.RequirePart<Mutations>().AddMutation("MultipleLegs", 10);
            player.RequirePart<Mutations>().AddMutation("ElectricalGeneration", 10);

            Popup.Suppress = true;
            player.AwardXP(750000);
            try
            {
                player.AddSkill("Tinkering");
                player.AddSkill("Tinkering_GadgetInspector");
                player.AddSkill("Tinkering_Repair");
                player.AddSkill("Tinkering_ReverseEngineer");
                player.AddSkill("Tinkering_Scavenger");
                player.AddSkill("Tinkering_Disassemble");
                player.AddSkill("Tinkering_LayMine");
                player.AddSkill("Tinkering_DeployTurret");
                player.AddSkill("Tinkering_Tinker1");
                player.AddSkill("Tinkering_Tinker2");
                player.AddSkill("Tinkering_Tinker3");
                foreach (TinkerData tinkerRecipe in TinkerData.TinkerRecipes)
                {
                    if (!TinkerData.KnownRecipes.CleanContains(tinkerRecipe))
                    {
                        TinkerData.KnownRecipes.Add(tinkerRecipe);
                    }
                }
                return;
            }
            finally
            {
                Popup.Suppress = false;
            }
        }
    }
}
