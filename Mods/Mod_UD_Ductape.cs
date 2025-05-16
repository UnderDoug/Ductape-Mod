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
        private static bool AnyNumberOfMods => Options.AnyNumberOfMods;
        private static bool ScalingDamageChance => Options.ScalingDamageChance;

        public int DamageOneIn = 500; // 2000

        public bool Jostled = false;

        private bool isMeleeWeapon => ParentObject != null && ParentObject.HasPart<MeleeWeapon>();

        private bool isMissileWeapon => ParentObject != null && ParentObject.HasPart<MissileWeapon>();

        private bool isThrownWeapon => ParentObject != null && ParentObject.HasPart<ThrownWeapon>();

        private bool isShield => ParentObject != null && ParentObject.HasPart<Shield>();

        private bool isArmor => ParentObject != null && ParentObject.HasPart<Armor>();

        private bool isPoweredDrawing => ParentObject != null && ParentObject.UsesCharge();

        private bool isProperlyEquipped => ParentObject != null && ParentObject.IsEquippedProperly();

        private Statistic Hitpoints => ParentObject?.GetStat("Hitpoints");

        private GameObject Equipper => ParentObject?.Equipped;

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
            WorksOnEquipper = true;
            WorksOnImplantee = true;
        }
        public override bool ModificationApplicable(GameObject Object)
        {
            return CanTape(Object);
        }
        public bool CanTape(GameObject Object, string Context = "")
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
             &&  true || (AnyNumberOfMods || (Object.GetModificationSlotsUsed() == RuleSettings.MAXIMUM_ITEM_MODS));

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

        public static bool Jostle(GameObject Object, int DamageOneIn, out int JostledDamage, bool IsPassive = false)
        {
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
                int roll = Stat.Roll(1, DamageOneIn * 7) % 7;
                bool byChance = roll == 1;
                if (byChance)
                {
                    int damageAmount = JostledDamage = (int)Math.Ceiling(Object.GetStat("Hitpoints").BaseValue * 0.25);
                    Debug.Entry(4,
                        $"{Object?.ShortDisplayNameStripped} took " +
                        $"{damageAmount} damage from being knocked around!",
                        Indent: 0, Toggle: true
                        );

                    return Object.TakeDamage(
                        Amount: damageAmount,
                        Message: "from being {{y|jostled}}!",
                        Attributes: "Disintigrate",
                        DeathReason: "fell apart",
                        ThirdPersonDeathReason: "fell apart",
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
                        SilentIfNoDamage: true); ;
                }
                else
                {
                    Debug.Entry(4, $"{Object?.ShortDisplayNameStripped} Jostled! ({roll})", Indent: 0, Toggle: true);
                }
            }
            return false;
        }
        public bool Jostle(out int JostledDamage, bool IsPassive = false)
        {
            return Jostle(ParentObject, DamageOneIn, out JostledDamage, IsPassive);
        }
        public static bool TryJostle(GameObject Object, int DamageOneIn, out bool Jostled, out int JostledDamage, bool CanJostle = true, bool IsPassive = false)
        {
            Jostled = false;
            JostledDamage = 0;
            if (Object != null && CanJostle)
            {
                Jostled = true;
                return Jostle(Object, DamageOneIn, out JostledDamage, IsPassive);
            }
            return false;
        }
        public bool TryJostle(out bool Jostled, out int JostledDamage, bool IsPassive = false)
        {
            if (TryJostle(ParentObject, DamageOneIn, out Jostled, out JostledDamage, !this.Jostled, IsPassive))
            {
                bool isEquipped = Equipper != null;
                if (isEquipped && Equipper.IsPlayerControlled())
                {
                    string message = $"Your {(isEquipped ? "equipped " : "")}{ParentObject.ShortDisplayName} took {JostledDamage} from being knocked around!";
                    if (Hitpoints.Value <= (int)Math.Ceiling(Hitpoints.BaseValue * 0.25))
                    {
                        if (ParentObject.IsBroken())
                        {
                            message += " It's {{r|busted}}!";
                        }
                        else
                        {
                            message += $" It looks about ready to fall apart!";
                        }
                    }
                    Popup.Show(message);
                    AutoAct.Interrupt();
                }
                return true;
            }
            return false;
        }
        public bool TryJostle(out bool Jostled, bool IsPassive = false)
        {
            return TryJostle(out Jostled, out _, IsPassive);
        }

        public override bool WantTurnTick()
        {
            return base.WantTurnTick()
                || Jostled;
        }
        public override void TurnTick(long TimeTick, int Amount)
        {
            Jostled = false;
            base.TurnTick(TimeTick, Amount);
        }
        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            // Registrar.Register(CanBeModdedEvent.ID, EventOrder.EXTREMELY_EARLY);
            base.Register(Object, Registrar);
        }
        public override bool WantEvent(int ID, int cascade)
        {
            bool wantGetAttackerHitDice =
                isProperlyEquipped
             && isMeleeWeapon;

            bool wantShieldBlock =
                isProperlyEquipped
             && isShield;

            return base.WantEvent(ID, cascade)
                || ID == GetShortDescriptionEvent.ID
                || ID == GetDisplayNameEvent.ID
                || (wantShieldBlock && ID == AfterShieldBlockEvent.ID)
                || (isPoweredDrawing && ID == ChargeUsedEvent.ID)
                || (wantGetAttackerHitDice && ID == GetAttackerHitDiceEvent.ID);
        }
        public override bool HandleEvent(GetShortDescriptionEvent E)
        {
            E.Postfix.AppendRules(GetDescription());
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetDisplayNameEvent E)
        {
            if (E.Understood() && !E.Object.HasProperName)
            {
                E.AddClause("{{y|held together by utilitape}}", PRIORITY_ADJUST_VERY_LARGE);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(ChargeUsedEvent E)
        {
            TryJostle(out Jostled, !isProperlyEquipped);
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetDefenderHitDiceEvent E)
        {
            TryJostle(out Jostled);
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(AfterShieldBlockEvent E)
        {
            TryJostle(out Jostled);
            return base.HandleEvent(E);
        }
    }
}
