using System;
using System.Collections.Generic;
using System.Text;

using XRL.UI;
using XRL.Rules;
using XRL.Language;
using XRL.World;
using XRL.World.Capabilities;
using XRL.World.Tinkering;
using XRL.Wish;

using UD_Ductape_Mod;
using static UD_Ductape_Mod.Const;

using Debug = UD_Ductape_Mod.Debug;
using Options = UD_Ductape_Mod.Options;

namespace XRL.World.Parts
{
    [HasWishCommand]
    [Serializable]
    public class Mod_UD_Ductape : IModification
    {
        private static bool doDebug = true;

        private static bool DebugDuctapeModDescriptions => Options.DebugDuctapeModDescriptions;
        private static bool AnyNumberOfMods => Options.AnyNumberOfMods;
        private static bool ScalingDamageChance => Options.ScalingDamageChance;

        public static int TurnsBetweenJostle = 1;

        public const int ACTIVE_EXTREMELY = 625;
        public const int ACTIVE_VERY = 350;
        public const int ACTIVE = 175;
        public const int PASSIVE = 75;
        public const int PASSIVE_VERY = 25;
        public const int PASSIVE_EXTREMELY = 5;

        public static readonly string DeathReason = "jostled apart";

        public static readonly string MOD_NAME = "held together by utilitape";
        public static readonly string MOD_NAME_COLORED = "held together by {{utilitape|utilitape}}";

        public int Activity = 0;

        public List<string> ActivityAdded;

        public List<string> AllJostleSources = new();
        private List<string> CurrentJostleSources = new();
        private List<string> LastJostleSources = new();

        private double StoredTimeTick = 0;

        private int TotalActivity = 0;
        private int TimesActive = 0;
        private int TimesJostled = 0;

        private float AverageActivity => (float)TotalActivity / (float)TimesActive;
        private float ActivityPerTurn => (float)TotalActivity / (float)TimesJostled;

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
            if (Context == "Internal" && Object != null)
            {
                Debug.Entry(4,
                    $"{nameof(Mod_UD_Ductape)}." +
                    $"{nameof(CanTape)}(" +
                    $"{Object.ShortDisplayNameStripped})",
                    Indent: Debug.LastIndent + 1, Toggle: doDebug);
            }
            
            // AnyNum || ModsIsMax  ? Result
            //   true || true       ? true
            //   true || false      ? true
            //  false || true       ? true
            //  false || false      ? false

            bool notNull = Object != null;

            bool hasHitpoints =
                notNull
             && Object.HasStat("Hitpoints");

            bool correctSlotsUsed =
                notNull
             && (AnyNumberOfMods || (Object.GetModificationSlotsUsed() == RuleSettings.MAXIMUM_ITEM_MODS));

            return notNull && hasHitpoints && correctSlotsUsed
                && (Object.UsesCharge()
                || Object.HasPart<MeleeWeapon>()
                || Object.HasPart<MissileWeapon>()
                || Object.HasPart<ThrownWeapon>()
                || Object.HasPart<Shield>()
                || Object.HasPart<Armor>());
        }
        public static string GetDescription()
        {
            return Grammar.InitCap(MOD_NAME_COLORED) + ": this item can be modified one additional time (excluding this modification) but has a small chance whenever it's used to take damage equal to 1/4 of its max HP.";
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

        public static int AdjustActivty(GameObject Object, int Activity)
        {
            int output = Activity;
            if (ScalingDamageChance)
            {
                int modCount = Object.GetModificationSlotsUsed();
                int multiplier = 1;
                if (modCount > 0)
                {
                    multiplier += 4 - Math.Min(4, Object.GetModificationSlotsUsed());
                }
                output *= (int)(1.0f / multiplier);
            }
            return output;
        }
        public int AdjustActivty(int Activity)
        {
            return AdjustActivty(ParentObject, Activity);
        }

        public static bool Jostle(GameObject Item, int Activity, out int JostledDamage)
        {
            int indent = Debug.LastIndent;
            Debug.Entry(4, 
                $"* {nameof(Jostle)}(" 
                + $"Item: {Item?.DebugName ?? NULL}, " 
                + $"Activity: {Activity})",
                Indent: indent, Toggle: doDebug);

            JostledDamage = 0;
            if (UD_JostleObjectEvent.CheckFor(Item, Activity))
            {
                int activity = AdjustActivty(Item, Activity);
                int activityOneInPadding = 10000.ToString().Length;
                string activityString = activity.ToString().PadLeft(activityOneInPadding, ' ');
                if (activity.in10000())
                {
                    JostledDamage = (int)Math.Ceiling(Item.GetStat("Hitpoints").BaseValue * 0.25);
                    
                    Debug.Entry(4,
                        $"({activityString} in {10000})" +
                        $" {Item?.DebugName ?? NULL} took" +
                        $" {JostledDamage} damage from being knocked around!",
                        Indent: indent + 1, Toggle: doDebug);

                    Debug.LastIndent = indent;
                    return Item.TakeDamage(
                        Amount: JostledDamage,
                        Message: "from being {{utilitape|jostled}}!",
                        Attributes: "Disintigrate,Jostle",
                        DeathReason: DeathReason,
                        ThirdPersonDeathReason: DeathReason,
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
                        $"({activityString} in {10000})" +
                        $" {Item?.DebugName ?? NULL}" +
                        $" was Jostled! ", 
                        Indent: indent + 1, Toggle: doDebug);
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
                string message = $"=object.T's= {equipped}{ParentObject.BaseDisplayName} took {JostledDamage} from being knocked around!";

                if (Hitpoints.Value <= (int)Math.Ceiling(Hitpoints.BaseValue * 0.25) && Hitpoints.Value > 0)
                {
                    if (ParentObject.IsBroken())
                    {
                        message += " =pronouns.Subjective='s {{r|busted}}!";
                    }
                    else
                    {
                        message += " =pronouns.Subjective= looks about ready to fall apart!";
                    }
                }
                if (FromEvent != null)
                {
                    if (FromEvent is BeforeDestroyObjectEvent E && (E.Reason == DeathReason || E.ThirdPersonReason == DeathReason))
                    {
                        message += " =pronouns.Subjective==verb:'ve:afterpronoun= been {{utilitape|jostled}} into useless pieces!";
                    }
                }
                Popup.Show(GameText.VariableReplace(message, Subject: ParentObject, Object: Holder));
                AutoAct.Interrupt();
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
            Activity += Amount;
            TotalActivity += Amount;
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

        public void ResetActivity()
        {
            Activity = 0;
            LastJostleSources = new(CurrentJostleSources);
            CurrentJostleSources = new();
            ActivityAdded = new();
            Activity = 0;
        }

        public override bool WantTurnTick()
        {
            return true;
        }
        public override void TurnTick(long TimeTick, int Amount)
        {
            if (Activity > 0 && TimeTick - StoredTimeTick > TurnsBetweenJostle && Hitpoints.Value > 0 && !ParentObject.IsInGraveyard())
            {
                Jostle(Activity);
                ResetActivity();
                StoredTimeTick = TimeTick;
            }
            base.TurnTick(TimeTick, Amount);
        }
        private Dictionary<string, int> StringyJostleEventIDs => new()
        {
            { "CommandFireMissile", ACTIVE_VERY },
            { "BeforeThrown", ACTIVE_EXTREMELY },
        };
        private Dictionary<Func<bool>, int> EquipperJostleEventIDs => new()
        {
            { delegate(){ return isProperlyEquipped; }, EnteredCellEvent.ID },
            { delegate(){ return isArmor && isProperlyEquipped; }, GetDefenderHitDiceEvent.ID },
        };
        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
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
                || ID == EquippedEvent.ID
                || ID == UnequippedEvent.ID
                || (wantGetWeaponHitDice && ID == GetWeaponHitDiceEvent.ID)
                || (wantBeforeFireMissileWeapons && ID == BeforeFireMissileWeaponsEvent.ID)
                || (wantGetThrownWeaponFlexPhaseProvider && ID == GetThrownWeaponFlexPhaseProviderEvent.ID)
                || (wantShieldBlock && ID == AfterShieldBlockEvent.ID)
                || (wantObjectEnteredCell && ID == ObjectEnteredCellEvent.ID);
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

                SB.AppendColored("M", MOD_NAME).Append(": ");
                SB.AppendLine();

                SB.AppendColored("W", $"Options")
                    .AppendLine();
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
                SB.Append(VANDR).Append("(").AppendColored("G", $"{AdjustActivty(Activity)}")
                    .Append($"){HONLY}{nameof(AdjustActivty)} ({ParentObject.GetModificationSlotsUsed()})");
                SB.AppendLine();
                SB.Append(VONLY).Append(VANDR).Append("(").AppendColored("R", $"{AdjustActivty(ACTIVE_EXTREMELY)}")
                    .Append($"){HONLY}{nameof(ACTIVE_EXTREMELY)}");
                SB.AppendLine();
                SB.Append(VONLY).Append(VANDR).Append("(").AppendColored("r", $"{AdjustActivty(ACTIVE_VERY)}")
                    .Append($"){HONLY}{nameof(ACTIVE_VERY)}");
                SB.AppendLine();
                SB.Append(VONLY).Append(VANDR).Append("(").AppendColored("w", $"{AdjustActivty(ACTIVE)}")
                    .Append($"){HONLY}{nameof(ACTIVE)}");
                SB.AppendLine();
                SB.Append(VONLY).Append(VANDR).Append("(").AppendColored("W", $"{AdjustActivty(PASSIVE)}")
                    .Append($"){HONLY}{nameof(PASSIVE)}");
                SB.AppendLine();
                SB.Append(VONLY).Append(VANDR).Append("(").AppendColored("g", $"{AdjustActivty(PASSIVE_VERY)}")
                    .Append($"){HONLY}{nameof(PASSIVE_VERY)}");
                SB.AppendLine();
                SB.Append(VONLY).Append(TANDR).Append("(").AppendColored("G", $"{AdjustActivty(PASSIVE_EXTREMELY)}")
                    .Append($"){HONLY}{nameof(PASSIVE_EXTREMELY)}");
                SB.AppendLine();
                SB.Append(VANDR).Append("(").AppendColored("M", $"{AverageActivity}")
                    .Append($"){HONLY}{nameof(AverageActivity)}");
                SB.AppendLine();
                SB.Append(VANDR).Append("(").AppendColored("M", $"{ActivityPerTurn}")
                    .Append($"){HONLY}{nameof(ActivityPerTurn)}");
                SB.AppendLine();
                SB.Append(VANDR).Append("(").AppendColored("M", $"{10000 / ActivityPerTurn}")
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

                SB.AppendColored("W", $"TimeTick")
                    .AppendLine();
                SB.Append(VANDR).Append("(").AppendColored("y", $"{TurnsBetweenJostle}")
                    .Append($"){HONLY}Current{nameof(TurnsBetweenJostle)}");
                SB.AppendLine();
                SB.Append(VANDR).Append("(").AppendColored("y", $"{The.Game.TimeTicks}")
                    .Append($"){HONLY}Current{nameof(The.Game.TimeTicks)}");
                SB.AppendLine();
                SB.Append(VANDR).Append("(").AppendColored("y", $"{StoredTimeTick}")
                    .Append($"){HONLY}{nameof(StoredTimeTick)}");
                SB.AppendLine();
                SB.Append(TANDR).Append("(").AppendColored("y", $"{The.Game.TimeTicks - StoredTimeTick}")
                    .Append($"){HONLY}Difference");
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
        public override bool HandleEvent(BeforeDestroyObjectEvent E)
        {
            GotJostled(LastJostledDamage, FromEvent: E);
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(EquippedEvent E)
        {
            foreach ((Func<bool> check, int eventID) in  EquipperJostleEventIDs)
            {
                if (check.Invoke()) E.Actor.RegisterEvent(this, eventID);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(UnequippedEvent E)
        {
            foreach ((Func<bool> _, int eventID) in EquipperJostleEventIDs)
            {
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

                Debug.Entry(4,
                    $"{nameof(Mod_UD_Ductape)}." +
                    $"{nameof(HandleEvent)}(" +
                    $"{nameof(ChargeUsedEvent)} E) " +
                    $"{ParentObject?.BaseDisplayName} " +
                    $"{activity}",
                    Indent: 0, Toggle: doDebug);

                AddActivity(activity, FromEvent: E);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(UseChargeEvent E)
        {
            if (E.Source == ParentObject && isPowering && CanAddActivity(FromEvent: E)) // Energy Cells
            {
                Debug.Entry(4,
                    $"{nameof(Mod_UD_Ductape)}." +
                    $"{nameof(HandleEvent)}(" +
                    $"{nameof(UseChargeEvent)} E) " +
                    $"{ParentObject?.BaseDisplayName} " +
                    $"{PASSIVE_VERY}",
                    Indent: 0, Toggle: doDebug);

                AddActivity(PASSIVE_VERY, FromEvent: E);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetWeaponHitDiceEvent E)
        {
            if(E.Weapon == ParentObject && CanAddActivity(FromEvent: E))
            {
                Debug.Entry(4,
                    $"{nameof(Mod_UD_Ductape)}." +
                    $"{nameof(HandleEvent)}(" +
                    $"{nameof(GetWeaponHitDiceEvent)} E) " +
                    $"{ParentObject?.BaseDisplayName} " +
                    $"{ACTIVE}",
                    Indent: 0, Toggle: doDebug);

                AddActivity(ACTIVE, FromEvent: E);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(BeforeFireMissileWeaponsEvent E)
        {
            if (isMissileWeapon && CanAddActivity(FromEvent: E))
            {
                Debug.Entry(4,
                    $"{nameof(Mod_UD_Ductape)}." +
                    $"{nameof(HandleEvent)}(" +
                    $"{nameof(BeforeFireMissileWeaponsEvent)} E) " +
                    $"{ParentObject?.BaseDisplayName} " +
                    $"{ACTIVE_VERY}",
                    Indent: 0, Toggle: doDebug);

                AddActivity(ACTIVE_VERY, FromEvent: E);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetThrownWeaponFlexPhaseProviderEvent E)
        {
            if (isThrownWeapon && CanAddActivity(FromEvent: E))
            {
                Debug.Entry(4,
                    $"{nameof(Mod_UD_Ductape)}." +
                    $"{nameof(HandleEvent)}(" +
                    $"{nameof(GetThrownWeaponFlexPhaseProviderEvent)} E) " +
                    $" {ParentObject?.BaseDisplayName}" +
                    $"{ACTIVE_VERY}",
                    Indent: 0, Toggle: doDebug);

                AddActivity(ACTIVE_VERY, FromEvent: E);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(AfterShieldBlockEvent E)
        {
            if (isShield && CanAddActivity(FromEvent: E))
            {
                Debug.Entry(4,
                    $"{nameof(Mod_UD_Ductape)}." +
                    $"{nameof(HandleEvent)}(" +
                    $"{nameof(AfterShieldBlockEvent)} E) " +
                    $"{ParentObject?.BaseDisplayName} " +
                    $"{ACTIVE}",
                    Indent: 0, Toggle: doDebug);

                AddActivity(ACTIVE, FromEvent: E);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(EnteredCellEvent E)
        {
            if (E.Object == ParentObject && CanAddActivity(FromEvent: E))
            {
                int activity = isArmor ? PASSIVE : PASSIVE_VERY;

                Debug.Entry(4,
                    $"{nameof(Mod_UD_Ductape)}." +
                    $"{nameof(HandleEvent)}(" +
                    $"{nameof(ObjectEnteredCellEvent)} E) " +
                    $"{ParentObject?.BaseDisplayName} " +
                    $"{activity}",
                    Indent: 0, Toggle: doDebug);

                AddActivity(activity, FromEvent: E);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetDefenderHitDiceEvent E)
        {
            if (E.Defender == Equipper && CanAddActivity(FromEvent: E))
            {
                Debug.Entry(4,
                    $"{nameof(Mod_UD_Ductape)}." +
                    $"{nameof(HandleEvent)}(" +
                    $"{nameof(GetDefenderHitDiceEvent)} E) " +
                    $"{ParentObject?.BaseDisplayName} " +
                    $"{ACTIVE_VERY}",
                    Indent: 0, Toggle: doDebug);

                AddActivity(ACTIVE_VERY, FromEvent: E);
            }
            return base.HandleEvent(E);
        }
        public override bool FireEvent(Event E)
        {
            if (StringyJostleEventIDs.ContainsKey(E.ID) && CanAddActivity(FromSEvent: E))
            {
                int activity = StringyJostleEventIDs[E.ID];
                AddActivity(activity, FromSEvent: E);
            }
            return base.FireEvent(E);
        }
        
        public override bool SameAs(IPart p)
        {
            return this == p;
        }

        [WishCommand(Command = "utilitape test kit")]
        public static void UtilitapeTestKitWishHandler()
        {
            GameObject player = The.Player;
            
            for (int i = 0; i < 50; i++)
            {
                GameObject fixitSpray = GameObjectFactory.Factory.CreateObject("Fixit Spray", BonusModChance: -9999, Context: "Wish");
                player.ReceiveObject(fixitSpray);

                GameObject utilitape = GameObjectFactory.Factory.CreateObject("Utilitape", BonusModChance: -9999, Context: "Wish");
                player.ReceiveObject(fixitSpray);

                if (i < 10)
                {
                    GameObject antimatterCell = GameObjectFactory.Factory.CreateObject("Antimatter Cell", BonusModChance: -9999, Context: "Wish");
                    antimatterCell.ApplyModification("ModRadioPowered", Actor: player);
                    antimatterCell.ApplyModification("ModHighCapacity", Actor: player);
                    if(!antimatterCell.ApplyModification("Mod_UD_RegenNanobots", Actor: player))
                    {
                        antimatterCell.ApplyModification("ModMetered", Actor: player);
                    }
                    antimatterCell.ApplyModification("ModGigantic", Actor: player);
                    player.ReceiveObject(antimatterCell);
                }
            }
            GameObject visage = GameObjectFactory.Factory.CreateObject("VISAGE", BonusModChance: -9999, Context: "Wish");
            visage.ApplyModification("ModNav", Actor: player);
            visage.ApplyModification("ModPolarized", Actor: player);
            visage.ApplyModification("Mod_UD_RegenNanobots", Actor: player);
            player.ReceiveObject(visage);

            GameObject zetachromeDagger = GameObjectFactory.Factory.CreateObject("Dagger8", BonusModChance: -9999, Context: "Wish");
            zetachromeDagger.ApplyModification("ModCounterweighted", Actor: player);
            zetachromeDagger.ApplyModification("ModSharp", Actor: player);
            zetachromeDagger.ApplyModification("Mod_UD_RegenNanobots", Actor: player);
            zetachromeDagger.ApplyModification("Mod_UD_Ductape", Actor: player);
            zetachromeDagger.ApplyModification("ModFlaming", Actor: player);
            player.ReceiveObject(zetachromeDagger);

            GameObject phaseCannon = GameObjectFactory.Factory.CreateObject("Phase Cannon", BonusModChance: -9999, Context: "Wish");
            phaseCannon.ApplyModification("ModScoped", Actor: player);
            phaseCannon.ApplyModification("ModNanon", Actor: player);
            phaseCannon.ApplyModification("Mod_UD_RegenNanobots", Actor: player);
            phaseCannon.ApplyModification("Mod_UD_Ductape", Actor: player);
            phaseCannon.ApplyModification("ModWillowy", Actor: player);
            player.ReceiveObject(phaseCannon);

            GameObject antigravBoots = GameObjectFactory.Factory.CreateObject("Anti-Gravity Boots", BonusModChance: -9999, Context: "Wish");
            antigravBoots.ApplyModification("ModSpringLoaded", Actor: player);
            antigravBoots.ApplyModification("ModCleated", Actor: player);
            antigravBoots.ApplyModification("Mod_UD_RegenNanobots", Actor: player);
            antigravBoots.ApplyModification("Mod_UD_Ductape", Actor: player);
            antigravBoots.ApplyModification("ModRefractive", Actor: player);
            player.ReceiveObject(antigravBoots);

            GameObject flawlessCrysteelShield = GameObjectFactory.Factory.CreateObject("Flawless Crysteel Shield", BonusModChance: -9999, Context: "Wish");
            flawlessCrysteelShield.ApplyModification("ModSpiked", Actor: player);
            flawlessCrysteelShield.ApplyModification("ModWillowy", Actor: player);
            flawlessCrysteelShield.ApplyModification("Mod_UD_RegenNanobots", Actor: player);
            flawlessCrysteelShield.ApplyModification("Mod_UD_Ductape", Actor: player);
            flawlessCrysteelShield.ApplyModification("ModRefractive", Actor: player);
            player.ReceiveObject(flawlessCrysteelShield);

            GameObject transkineticCuffs = GameObjectFactory.Factory.CreateObject("Transkinetic Cuffs", BonusModChance: -9999, Context: "Wish");
            transkineticCuffs.ApplyModification("ModSturdy", Actor: player);
            transkineticCuffs.ApplyModification("ModOverloaded", Actor: player);
            transkineticCuffs.ApplyModification("Mod_UD_RegenNanobots", Actor: player);
            player.ReceiveObject(transkineticCuffs);

            player.RequirePart<BitLocker>().AddAllBits(6000);

            player.RequirePart<Mutations>().AddMutation("MultipleLegs", 10);

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
