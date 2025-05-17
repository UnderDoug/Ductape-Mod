using HarmonyLib;

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

        public int DamageOneIn = 500; // 2000

        public bool Jostled = false;

        public int LastJostledDamage = 0;

        private bool isMeleeWeapon => ParentObject != null && ParentObject.TryGetPart(out MeleeWeapon meleeWeapon) && !meleeWeapon.IsImprovised();

        private bool isMissileWeapon => ParentObject != null && ParentObject.HasPart<MissileWeapon>();

        private bool isThrownWeapon => ParentObject != null && ParentObject.TryGetPart(out ThrownWeapon thrownWeapon) && !thrownWeapon.IsImprovised();

        private bool isShield => ParentObject != null && ParentObject.HasPart<Shield>();

        private bool isArmor => ParentObject != null && ParentObject.TryGetPart(out Armor armor) && !armor.WornOn.IsNullOrEmpty();

        private bool isEquipment => isMeleeWeapon || isMissileWeapon || isThrownWeapon || isShield || isArmor;

        private bool isPoweredDrawing => ParentObject != null && ParentObject.UsesCharge();

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
            if (ParentObject != null && !ParentObject.HasStringProperty("EquipmentFrameColors"))
            {
                ParentObject.SetStringProperty("EquipmentFrameColors", "yKyK");
            }
            base.ApplyModification();
        }
        public override void Attach()
        {
            if (ParentObject != null && !ParentObject.HasStringProperty("EquipmentFrameColors"))
            {
                ParentObject.SetStringProperty("EquipmentFrameColors", "yKyK");
            }
            base.Attach();
        }

        public static bool Jostle(GameObject Object, int DamageOneIn, out int JostledDamage, bool IsPassive = false, MinEvent FromEvent = null, Event FromSEvent = null)
        {
            Debug.Entry(4, 
                $"static " + 
                $"{nameof(Jostle)}(" + 
                $"{nameof(Object)}, " + 
                $"${nameof(DamageOneIn)}, " +
                $"out {nameof(JostledDamage)}, " +
                $"{nameof(IsPassive)} = false" +
                $" FromEvent: {FromEvent?.GetType()?.Name ?? NULL}," +
                $" FromSEvent: {FromSEvent?.ID ?? NULL})",
                Indent: Debug.LastIndent + 1, Toggle: true);

            JostledDamage = 0;
            if (Object != null)
            {
                DamageOneIn *= IsPassive ? 10 : 1;
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
                    DamageOneIn *= multiplier;
                }
                int roll = Stat.Roll(1, DamageOneIn * 7) % DamageOneIn;
                bool byChance = roll == 1;
                if (byChance)
                {
                    int damageAmount = JostledDamage = (int)Math.Ceiling(Object.GetStat("Hitpoints").BaseValue * 0.25);
                    Debug.Entry(4,
                        $"{Object?.ShortDisplayNameStripped} took " +
                        $"{damageAmount} damage from being knocked around!",
                        Indent: Debug.LastIndent + 1, Toggle: true
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
                    Debug.Entry(4, $"{Object?.ShortDisplayNameStripped} Jostled! ({roll})", Indent: Debug.LastIndent + 1, Toggle: true);
                }
            }
            return false;
        }
        public bool Jostle(out int JostledDamage, bool IsPassive = false, MinEvent FromEvent = null, Event FromSEvent = null)
        {
            return Jostle(ParentObject, DamageOneIn, out JostledDamage, IsPassive, FromEvent, FromSEvent);
        }
        public static bool TryJostle(GameObject Object, int DamageOneIn, out bool Jostled, out int JostledDamage, bool CanJostle = true, bool IsPassive = false, MinEvent FromEvent = null, Event FromSEvent = null)
        {
            Debug.Entry(4,
                $"static " +
                $"{nameof(TryJostle)}(" +
                $"{nameof(Object)}, " +
                $"{nameof(DamageOneIn)}, " +
                $"out {nameof(Jostled)}, " +
                $"out {nameof(JostledDamage)}, " +
                $"{nameof(CanJostle)} = true, " +
                $"{nameof(IsPassive)} = false" +
                $" FromEvent: {FromEvent?.GetType()?.Name ?? NULL}," +
                $" FromSEvent: {FromSEvent?.ID ?? NULL})",
                Indent: 0, Toggle: true);

            Jostled = false;
            JostledDamage = 0;
            if (Object != null && CanJostle)
            {
                Jostled = true;
                return Jostle(Object, DamageOneIn, out JostledDamage, IsPassive, FromEvent, FromSEvent);
            }
            return false;
        }
        public bool TryJostle(out bool Jostled, out int JostledDamage, bool IsPassive = false, MinEvent FromEvent = null, Event FromSEvent = null)
        {
            if (TryJostle(ParentObject, DamageOneIn, out Jostled, out JostledDamage, !this.Jostled, IsPassive, FromEvent, FromSEvent))
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
                string message = $"=object.T= {equipped}=subject.Name= took {JostledDamage} from being knocked around!";

                if (Hitpoints.Value <= (int)Math.Ceiling(Hitpoints.BaseValue * 0.25) && Hitpoints.Value > 0)
                {
                    if (ParentObject.IsBroken())
                    {
                        message += " =pronouns.Subjective==verb:'s:afterpronoun= {{r|busted}}!";
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
            if (Hitpoints.Value > 0 && !ParentObject.IsInGraveyard())
            {
                Jostled = false;
            }
            base.TurnTick(TimeTick, Amount);
        }
        private static List<string> StringyJostleEventIDs => new()
        {
            "CommandFireMissile",
            "BeforeThrown",
        };
        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register(BeforeDestroyObjectEvent.ID, EventOrder.EXTREMELY_EARLY);
            foreach (string eventID in StringyJostleEventIDs)
            {
                Registrar.Register(eventID);
            }
            base.Register(Object, Registrar);
        }
        public override bool WantEvent(int ID, int cascade)
        {
            bool hasEquipper =
                Equipper != null
             && isEquipment
             && isProperlyEquipped;

            bool wantChargeUsed =
                !Jostled
             && !hasEquipper
             && isPoweredDrawing;

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
                || ID == GetDisplayNameEvent.ID
                || ID == GetShortDescriptionEvent.ID
                || (hasEquipper && ID == EquippedEvent.ID)
                || (hasEquipper && ID == UnequippedEvent.ID)
                || (wantChargeUsed && ID == ChargeUsedEvent.ID)
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
                E.AddClause("{{y|held together by utilitape}}", PRIORITY_ADJUST_VERY_LARGE);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetShortDescriptionEvent E)
        {
            E.Postfix.AppendRules(GetDescription());

            if (DebugDuctapeModDescriptions)
            {
                StringBuilder SB = Event.NewStringBuilder();

                string equipmentFrame = ParentObject.GetStringProperty("EquipmentFrameColors");

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
                
                SB.AppendColored("M", $"Utilitape").Append(": ");
                SB.AppendLine();
                SB.AppendColored("W", $"Options").AppendLine();
                SB.Append(VANDR).Append($"[{AnyNumberOfMods.YehNah()}]{HONLY}{nameof(AnyNumberOfMods)}: ")
                    .AppendColored("B", $"{AnyNumberOfMods}");
                SB.AppendLine();
                SB.Append(TANDR).Append($"[{ScalingDamageChance.YehNah()}]{HONLY}{nameof(ScalingDamageChance)}: ")
                    .AppendColored("B", $"{ScalingDamageChance}");
                SB.AppendLine();
                SB.AppendColored("W", $"State").AppendLine();
                SB.Append(VANDR).Append($"[{Jostled.YehNah(true)}]{HONLY}{nameof(Jostled)}: ")
                    .AppendColored("B", $"{Jostled}");
                SB.AppendLine();
                SB.Append(VANDR).Append("(").AppendColored("g", $"{DamageOneIn * (!isProperlyEquipped ? 10 : 1)}")
                    .Append($"){HONLY}{nameof(DamageOneIn)}");
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
                SB.AppendColored("W", $"Bools").AppendLine();
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
                SB.Append(VANDR).Append($"[{isPoweredDrawing.YehNah()}]{HONLY}{nameof(isPoweredDrawing)}: ")
                    .AppendColored("B", $"{isPoweredDrawing}");
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
            if (ParentObject.EquippedProperlyBy() == E.Actor && isArmor)
            {
                E.Actor.RegisterEvent(this, ObjectEnteredCellEvent.ID);
                E.Actor.RegisterEvent(this, GetDefenderMeleePenetrationEvent.ID);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(UnequippedEvent E)
        {
            if (isArmor)
            {
                E.Actor.UnregisterEvent(this, ObjectEnteredCellEvent.ID);
                E.Actor.UnregisterEvent(this, GetDefenderMeleePenetrationEvent.ID);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(ChargeUsedEvent E)
        {
            Debug.Entry(4,
                $"{nameof(Mod_UD_Ductape)}." +
                $"{nameof(HandleEvent)}(6" +
                $"{nameof(ChargeUsedEvent)} E)",
                Indent: 0, Toggle: true);

            TryJostle(out Jostled, !isProperlyEquipped, FromEvent: E);
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
                $"{nameof(GetMissileWeaponPerformanceEvent)} E)",
                Indent: 0, Toggle: true);

            TryJostle(out Jostled, FromEvent: E);
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetThrownWeaponPerformanceEvent E)
        {
            Debug.Entry(4,
                $"{nameof(Mod_UD_Ductape)}." +
                $"{nameof(HandleEvent)}(" +
                $"{nameof(GetThrownWeaponPerformanceEvent)} E)",
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
        public override bool HandleEvent(ObjectEnteredCellEvent E)
        {
            Debug.Entry(4,
                $"{nameof(Mod_UD_Ductape)}." +
                $"{nameof(HandleEvent)}(" +
                $"{nameof(ObjectEnteredCellEvent)} E)",
                Indent: 0, Toggle: true);

            TryJostle(out Jostled, FromEvent: E);
            return base.HandleEvent(E);
        }
        public override bool FireEvent(Event E)
        {
            if (!Jostled && StringyJostleEventIDs.Contains(E.ID))
            {
                TryJostle(out Jostled);
            }
            return base.FireEvent(E);
        }
    }
}
