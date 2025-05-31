using System;
using System.Collections.Generic;
using System.Text;

using XRL.World;
using XRL.Rules;
using XRL.UI;
using XRL.World.Capabilities;
using XRL.World.Tinkering;

using UD_Ductape_Mod;
using static UD_Ductape_Mod.Const;

using Debug = UD_Ductape_Mod.Debug;
using Options = UD_Ductape_Mod.Options;

namespace XRL.World.Parts
{
    [Serializable]
    public class Mod_UD_Ductape : IModification
    {
        private static bool DebugDuctapeModDescriptions => Options.DebugDuctapeModDescriptions;
        private static bool AnyNumberOfMods => Options.AnyNumberOfMods;
        private static bool ScalingDamageChance => Options.ScalingDamageChance;

        private List<string> JostleSources = new();

        public static readonly string DeathReason = "jostled apart";

        public int DamageOneIn = 75;

        public int PassiveFactor = 5;

        public bool Jostled = false;

        private double StoredTimeTick = 0; 

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
                    Indent: Debug.LastIndent + 1, Toggle: true
                    );
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
            return "Held together by utilitape: this item can be modified one additional time (excluding this modification) but has a small chance whenever it's used to take damage equal to 1/4 of its max HP.";
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

        public static int GetDamageOneIn(GameObject Object, int DamageOneIn, int PassiveFactor, bool IsPassive)
        {
            int output = DamageOneIn * (IsPassive ? PassiveFactor : 1);
            if (ScalingDamageChance)
            {
                int modCount = Object.GetModificationSlotsUsed();
                int multiplier = 1;
                if (modCount < 0)
                {
                    multiplier = 0;
                }
                else
                {
                    multiplier += 4 - Math.Min(4, Object.GetModificationSlotsUsed());
                }
                output *= multiplier;
            }
            return output;
        }
        public int GetDamageOneIn(bool IsPassive)
        {
            return GetDamageOneIn(ParentObject, DamageOneIn, PassiveFactor, IsPassive);
        }

        public static bool Jostle(GameObject Object, int DamageOneIn, int PassiveFactor, out int JostledDamage, bool IsPassive = false, MinEvent FromEvent = null, Event FromSEvent = null)
        {
            int indent = Debug.LastIndent + 1;
            Debug.Entry(4, 
                $"* {nameof(Jostle)}("
                + $" FromEvent: {FromEvent?.GetType()?.Name ?? NULL},"
                + $" FromSEvent: {FromSEvent?.ID ?? NULL})",
                Indent: indent, Toggle: true);

            JostledDamage = 0;
            if (Object != null)
            {
                int damageOneIn = GetDamageOneIn(Object, DamageOneIn, PassiveFactor, IsPassive);
                int damageOneInPadding = damageOneIn.ToString().Length;
                int roll = Stat.Roll($"1d{damageOneIn}");
                string rollString = roll.ToString().PadLeft(damageOneInPadding, ' ');
                bool byChance = roll == damageOneIn;
                if (byChance)
                {
                    int damageAmount = JostledDamage = (int)Math.Ceiling(Object.GetStat("Hitpoints").BaseValue * 0.25);
                    Debug.Entry(4,
                        $"({rollString}/{damageOneIn})" +
                        $" {Object?.DebugName ?? NULL} took" +
                        $" {damageAmount} damage from being knocked around!",
                        Indent: indent + 1, Toggle: true
                        );

                    return Object.TakeDamage(
                        Amount: damageAmount,
                        Message: "from being {{y|jostled}}!",
                        Attributes: "Disintigrate",
                        DeathReason: DeathReason,
                        ThirdPersonDeathReason: DeathReason,
                        Owner: null,
                        Environmental: false,
                        Attacker: Object?.equippedOrSelf(),
                        Source: Object?.equippedOrSelf(),
                        Perspective: Object?.equippedOrSelf(),
                        DescribeAsFrom: null,
                        Accidental: true,
                        Indirect: true,
                        ShowUninvolved: false,
                        IgnoreVisibility: false,
                        ShowForInanimate: true,
                        SilentIfNoDamage: true);
                }
                else
                {
                    Debug.Entry(4, 
                        $"({rollString}/{damageOneIn})" +
                        $" {Object?.DebugName ?? NULL}" +
                        $" was Jostled! ", 
                        Indent: indent + 1, Toggle: true);
                }
            }
            return false;
        }
        public bool Jostle(out int JostledDamage, bool IsPassive = false, MinEvent FromEvent = null, Event FromSEvent = null)
        {
            return Jostle(ParentObject, DamageOneIn, PassiveFactor, out JostledDamage, IsPassive, FromEvent, FromSEvent);
        }
        public static bool TryJostle(GameObject Object, int DamageOneIn, int PassiveFactor, out bool Jostled, out int JostledDamage, bool CanJostle = true, bool IsPassive = false, MinEvent FromEvent = null, Event FromSEvent = null)
        {
            Jostled = false;
            JostledDamage = 0;
            if (Object != null && CanJostle)
            {
                Jostled = true;
                return Jostle(Object, DamageOneIn, PassiveFactor, out JostledDamage, IsPassive, FromEvent, FromSEvent);
            }
            return false;
        }
        public bool TryJostle(out bool Jostled, out int JostledDamage, bool IsPassive = false, MinEvent FromEvent = null, Event FromSEvent = null)
        {
            if (TryJostle(ParentObject, DamageOneIn, PassiveFactor, out Jostled, out JostledDamage, !this.Jostled, IsPassive, FromEvent, FromSEvent))
            {
                LastJostledDamage = JostledDamage;
                GotJostled(JostledDamage, FromEvent, FromSEvent);
                return true;
            }
            return false;
        }
        public bool TryJostle(out bool Jostled, bool IsPassive = false, MinEvent FromEvent = null, Event FromSEvent = null)
        {
            JostleSources ??= new();
            JostleSources.TryAdd(FromEvent.GetType().Name);
            return TryJostle(out Jostled, out _, IsPassive, FromEvent, FromSEvent);
        }

        public void GotJostled(int JostledDamage = 0, MinEvent FromEvent = null, Event FromSEvent = null)
        {
            if (Holder != null && Holder.IsPlayerControlled() && JostledDamage != 0)
            {
                bool isEquipped = Equipper != null;
                string equipped = isEquipped ? "equipped " : "";
                string message = $"=object.T's= {equipped}{ParentObject?.BaseDisplayName} took {JostledDamage} from being knocked around!";

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
                        message += $" =pronouns.Subjective==verb:'ve:afterpronoun= been jostled into useless pieces!";
                    }
                }
                Popup.Show(GameText.VariableReplace(message, Subject: ParentObject, Object: Holder));
                AutoAct.Interrupt();
            }
        }

        public override bool WantTurnTick()
        {
            return base.WantTurnTick()
                || Jostled;
        }
        public override void TurnTick(long TimeTick, int Amount)
        {
            if (TimeTick - StoredTimeTick > 3 || (Hitpoints.Value > 0 && !ParentObject.IsInGraveyard()))
            {
                Jostled = false;
                StoredTimeTick = TimeTick;
            }
            base.TurnTick(TimeTick, Amount);
        }
        private List<string> StringyJostleEventIDs => new()
        {
            "CommandFireMissile",
            "BeforeThrown",
        };
        private Dictionary<Func<bool>, int> EquipperJostleEventIDs => new()
        {
            { delegate(){ return isArmor && isProperlyEquipped; }, EnteredCellEvent.ID },
            { delegate(){ return isArmor && isProperlyEquipped; }, GetDefenderHitDiceEvent.ID },
        };
        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register(BeforeDestroyObjectEvent.ID, EventOrder.EXTREMELY_EARLY);
            Registrar.Register(ChargeUsedEvent.ID, EventOrder.EXTREMELY_EARLY);
            Registrar.Register(UseChargeEvent.ID, EventOrder.EXTREMELY_LATE);
            foreach (string eventID in StringyJostleEventIDs)
            {
                Registrar.Register(eventID);
            }
            Registrar.Register(GetDisplayNameEvent.ID, EventOrder.EXTREMELY_LATE);
            base.Register(Object, Registrar);
        }
        public override bool WantEvent(int ID, int cascade)
        {
            bool wantGetWeaponHitDice =
                !Jostled
             && isProperlyEquipped
             && isMeleeWeapon;

            bool wantBeforeFireMissileWeapons =
                !Jostled
             && isProperlyEquipped
             && isMissileWeapon;

            bool wantShieldBlock =
                !Jostled
             && isWorn
             && isProperlyEquipped
             && isShield;

            bool wantObjectEnteredCell =
                !Jostled
             && isWorn
             && isProperlyEquipped
             && isArmor;

            bool wantGetThrownWeaponFlexPhaseProvider =
                !Jostled
             && isThrownWeapon;

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

                string clause = "{{y|held together by utilitape}}";

                if (hasWiths)
                {
                    List<string> withClauses = new(E.DB.WithClauses)
                    {
                        clause
                    };
                    E.DB.WithClauses.Clear();
                    if (!withClauses.IsNullOrEmpty())
                    {
                        foreach (string entry in withClauses)
                        {
                            E.AddWithClause(entry);
                        }
                    }
                }
                else
                {
                    E.AddClause(clause, -4);
                }
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

                JostleSources ??= new();
                string jostleSources = "";
                if (!JostleSources.IsNullOrEmpty())
                {
                    foreach (string source in JostleSources)
                    {
                        if (!jostleSources.IsNullOrEmpty())
                        {
                            jostleSources += ", ";
                        }
                        jostleSources += source;
                    }
                }

                bool hasEquipper =
                    Equipper != null
                 && isEquipment
                 && isProperlyEquipped;

                SB.AppendColored("M", $"Utilitape").Append(": ");
                SB.AppendLine();

                SB.AppendColored("W", $"Options")
                    .AppendLine();
                SB.Append(VANDR).Append($"[{AnyNumberOfMods.YehNah()}]{HONLY}{nameof(AnyNumberOfMods)}: ")
                    .AppendColored("B", $"{AnyNumberOfMods}");
                SB.AppendLine();
                SB.Append(TANDR).Append($"[{ScalingDamageChance.YehNah()}]{HONLY}{nameof(ScalingDamageChance)}: ")
                    .AppendColored("B", $"{ScalingDamageChance}");
                SB.AppendLine();

                SB.AppendColored("W", $"State")
                    .AppendLine();
                SB.Append(VANDR).Append($"[{Jostled.YehNah(true)}]{HONLY}{nameof(Jostled)}: ")
                    .AppendColored("B", $"{Jostled}");
                SB.AppendLine();
                SB.Append(VANDR).Append("(").AppendColored("g", $"{GetDamageOneIn(!hasEquipper)}")
                    .Append($"){HONLY}{nameof(DamageOneIn)}");
                SB.AppendLine();
                SB.Append(VANDR).Append($"[{isProperlyEquipped.YehNah(true)}]{HONLY}isPassive: ")
                    .AppendColored("B", $"{!hasEquipper}");
                SB.AppendLine();
                SB.Append(VANDR).Append("(").AppendColored("c", $"{LastJostledDamage}")
                    .Append($"){HONLY}{nameof(LastJostledDamage)}");
                SB.AppendLine();
                SB.Append(VANDR).Append("(").AppendColored("o", $"{jostleSources.Quote()}")
                    .Append($"){HONLY}{nameof(JostleSources)}");
                SB.AppendLine();
                SB.Append(TANDR).Append("(").AppendColored("y", $"{equipmentFrame}")
                    .Append($"){HONLY}EquipmentFrameColors");
                SB.AppendLine();

                SB.AppendColored("W", $"TimeTick")
                    .AppendLine();
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
                SB.Append(VANDR).Append($"[{isMeleeWeapon.YehNah()}]{HONLY}{nameof(isMeleeWeapon)}: ")
                    .AppendColored("B", $"{isMeleeWeapon}");
                SB.AppendLine();
                SB.Append(VANDR).Append($"[{isMissileWeapon.YehNah()}]{HONLY}{nameof(isMissileWeapon)}: ")
                    .AppendColored("B", $"{isMissileWeapon}");
                SB.AppendLine();
                SB.Append(VANDR).Append($"[{isThrownWeapon.YehNah()}]{HONLY}{nameof(isThrownWeapon)}: ")
                    .AppendColored("B", $"{isThrownWeapon}");
                SB.AppendLine();
                SB.Append(VANDR).Append($"[{isShield.YehNah()}]{HONLY}{nameof(isShield)}: ")
                    .AppendColored("B", $"{isShield}");
                SB.AppendLine();
                SB.Append(VANDR).Append($"[{isArmor.YehNah()}]{HONLY}{nameof(isArmor)}: ")
                    .AppendColored("B", $"{isArmor}");
                SB.AppendLine();
                SB.Append(VANDR).Append($"[{isPowerDrawing.YehNah()}]{HONLY}{nameof(isPowerDrawing)}: ")
                    .AppendColored("B", $"{isPowerDrawing}");
                SB.AppendLine();
                SB.Append(VANDR).Append($"[{isPowering.YehNah()}]{HONLY}{nameof(isPowering)}: ")
                    .AppendColored("B", $"{isPowering}");
                SB.AppendLine();
                SB.Append(TANDR).Append($"[{isProperlyEquipped.YehNah()}]{HONLY}{nameof(isProperlyEquipped)}: ")
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

            bool wantChargeUsed =
                !Jostled
             && isPowerDrawing;

            if (wantChargeUsed)
            {
                Debug.Entry(4,
                $"{nameof(Mod_UD_Ductape)}." +
                $"{nameof(HandleEvent)}(" +
                $"{nameof(ChargeUsedEvent)} E)",
                Indent: 0, Toggle: true);

                TryJostle(out Jostled, !hasEquipper, FromEvent: E);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(UseChargeEvent E)
        {
            bool wantUseCharge =
                !Jostled
             && isPowering;

            if (wantUseCharge)
            {
                Debug.Entry(4,
                $"{nameof(Mod_UD_Ductape)}." +
                $"{nameof(HandleEvent)}(" +
                $"{nameof(UseChargeEvent)} E)",
                Indent: 0, Toggle: true);

                TryJostle(out Jostled, true, FromEvent: E);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetWeaponHitDiceEvent E)
        {
            Debug.Entry(4,
                $"{nameof(Mod_UD_Ductape)}." +
                $"{nameof(HandleEvent)}(" +
                $"{nameof(GetWeaponHitDiceEvent)} E)",
                Indent: 0, Toggle: true);

            TryJostle(out Jostled, FromEvent: E);
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(BeforeFireMissileWeaponsEvent E)
        {
            Debug.Entry(4,
                $"{nameof(Mod_UD_Ductape)}." +
                $"{nameof(HandleEvent)}(" +
                $"{nameof(BeforeFireMissileWeaponsEvent)} E)",
                Indent: 0, Toggle: true);

            TryJostle(out Jostled, FromEvent: E);
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetThrownWeaponFlexPhaseProviderEvent E)
        {
            Debug.Entry(4,
                $"{nameof(Mod_UD_Ductape)}." +
                $"{nameof(HandleEvent)}(" +
                $"{nameof(GetThrownWeaponFlexPhaseProviderEvent)} E)",
                Indent: 0, Toggle: true);

            TryJostle(out Jostled, FromEvent: E);
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(AfterShieldBlockEvent E)
        {
            Debug.Entry(4,
                $"{nameof(Mod_UD_Ductape)}." +
                $"{nameof(HandleEvent)}(" +
                $"{nameof(AfterShieldBlockEvent)} E)",
                Indent: 0, Toggle: true);

            TryJostle(out Jostled, FromEvent: E);
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(EnteredCellEvent E)
        {
            Debug.Entry(4,
                $"{nameof(Mod_UD_Ductape)}." +
                $"{nameof(HandleEvent)}(" +
                $"{nameof(ObjectEnteredCellEvent)} E)",
                Indent: 0, Toggle: true);

            TryJostle(out Jostled, FromEvent: E);
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetDefenderHitDiceEvent E)
        {
            Debug.Entry(4,
                $"{nameof(Mod_UD_Ductape)}." +
                $"{nameof(HandleEvent)}(" +
                $"{nameof(GetDefenderHitDiceEvent)} E)",
                Indent: 0, Toggle: true);

            TryJostle(out Jostled, FromEvent: E);
            return base.HandleEvent(E);
        }
        public override bool FireEvent(Event E)
        {
            if (!Jostled && StringyJostleEventIDs.Contains(E.ID))
            {
                TryJostle(out Jostled, FromSEvent: E);
            }
            return base.FireEvent(E);
        }
    }
}
