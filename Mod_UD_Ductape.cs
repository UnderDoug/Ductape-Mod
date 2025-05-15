using HarmonyLib;

using System;
using System.Collections.Generic;
using System.Text;

using XRL.World;

using UD_Ductape_Mod;
using XRL.Rules;
using XRL.UI;
using XRL.World.Capabilities;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XRL.World.Tinkering;

namespace XRL.World.Parts
{
    [Serializable]
    public class Mod_UD_Ductape : IModification
    {
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
            return Object != null && Object.HasStat("Hitpoints") && Object.GetModificationSlotsUsed() == RuleSettings.MAXIMUM_ITEM_MODS
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
            if (ParentObject != null && ParentObject.HasStringProperty("EquipmentFrameColors"))
            {
                ParentObject.SetStringProperty("EquipmentFrameColors", "yKyK");
            }
            base.ApplyModification();
        }

        public static bool Jostle(GameObject Object, int DamageOneIn, out int JostledDamage, bool IsPassive = false)
        {
            JostledDamage = 0;
            if (Object != null)
            {
                DamageOneIn *= IsPassive ? 10 : 1;
                int roll = Stat.Roll(1, DamageOneIn);
                bool byChance = roll == 1;
                if (byChance)
                {
                    int damageAmount = JostledDamage = (int)Math.Ceiling(Object.GetStat("Hitpoints").BaseValue * 0.25);
                    UnityEngine.Debug.LogError($"{Object?.ShortDisplayNameStripped} took {damageAmount} damage being knocked around!");
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
                    UnityEngine.Debug.LogError($"{Object?.ShortDisplayNameStripped} Jostled! ({roll})");
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
            if (TryJostle(ParentObject, DamageOneIn, out Jostled, out JostledDamage, !this.Jostled, IsPassive) && Equipper != null && Equipper.IsPlayerControlled())
            {
                string message = $"Your {ParentObject.ShortDisplayName} took {JostledDamage} from being knocked around!";
                if (Hitpoints.Value <= (int)Math.Ceiling(Hitpoints.BaseValue * 0.25))
                {
                    message += $" It looks about ready to fall apart!";
                }
                Popup.Show(message);
                AutoAct.Interrupt();
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
            base.Register(Object, Registrar);
            // Registrar.Register(Event.ID, EventOrder.EXTREMELY_EARLY);
        }
        public override bool WantEvent(int ID, int cascade)
        {
            bool wantGetWeaponMeleePenetration =
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
                || (wantGetWeaponMeleePenetration && ID == GetWeaponMeleePenetrationEvent.ID);
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
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(ChargeUsedEvent E)
        {
            TryJostle(out Jostled, !isProperlyEquipped);
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetWeaponMeleePenetrationEvent E)
        {
            TryJostle(out Jostled);
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(AfterShieldBlockEvent E)
        {
            TryJostle(out Jostled);
            return base.HandleEvent(E);
        }

        [HarmonyPrefix]
        [HarmonyPatch(
            declaringType: typeof(ItemModding),
            methodName: nameof(ItemModding.ModificationApplicable),
            argumentTypes: new Type[] { typeof(string), typeof(GameObject), typeof(GameObject), typeof(string) },
            argumentVariations: new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal }
            )]
        public static void ModificationApplicable_AllowFourth_Prefix(string Name, GameObject Object, GameObject Actor = null, string Key = null)
        {
            if (Name == nameof(Mod_UD_Ductape))
            {
                Key = nameof(Mod_UD_Ductape);
            }
        }
    }
}
