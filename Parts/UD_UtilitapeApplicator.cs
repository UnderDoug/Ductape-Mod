using System;
using System.Collections.Generic;
using System.Text;

using XRL.UI;
using XRL.World.Tinkering;

using UD_Ductape_Mod;

using static UD_Ductape_Mod.Options;
using static UD_Ductape_Mod.Const;
using static UD_Ductape_Mod.Utils;

using Debug = UD_Ductape_Mod.Debug;
using Options = UD_Ductape_Mod.Options;

namespace XRL.World.Parts
{
    [Serializable]
    public class UD_UtilitapeApplicator : IScribedPart
    {
        private static bool doDebug => getClassDoDebug(nameof(UD_UtilitapeApplicator));
        private static bool getDoDebug(object what = null)
        {
            List<object> doList = new()
            {
                'V',    // Vomit
                'X',    // Trace
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

        public static readonly string APPLIED_SOUND_TINKER = "Sounds/Abilities/sfx_ability_tinkerModItem";
        public static readonly string APPLIED_SOUND_CLOTH = "sfx_equip_material_generic_cloth";

        public override bool WantEvent(int ID, int Cascade)
        {
            return base.WantEvent(ID, Cascade)
                || ID == InventoryActionEvent.ID;
        }
        public override bool HandleEvent(InventoryActionEvent E)
        {
            if (E.Command == "Apply")
            {
                int indent = Debug.LastIndent;
                Debug.Entry(2,
                    $"{nameof(UD_UtilitapeApplicator)}." +
                    $"{nameof(HandleEvent)}(" +
                    $"{nameof(InventoryActionEvent)} E) " +
                    $"{nameof(E.Command)}: {E.Command}",
                    Indent: indent + 1, Toggle: getDoDebug());

                Debug.Entry(3, $"{nameof(E.Actor.CheckFrozen)} called on {nameof(E.Actor)}...", Indent: indent + 2, Toggle: getDoDebug());
                if (!E.Actor.CheckFrozen(Telepathic: false, Telekinetic: true))
                {
                    Debug.CheckNah(4, $"Failed {nameof(E.Actor.CheckFrozen)}", Indent: indent + 3, Toggle: getDoDebug());
                    Debug.LastIndent = indent;
                    return false;
                }

                Debug.Entry(3, 
                    $"Checking {nameof(E.Item)} for " +
                    $"{nameof(E.Item.IsBroken)}, " +
                    $"{nameof(E.Item.IsRusted)}, or " +
                    $"{nameof(E.Item.IsEMPed)}...",
                    Indent: indent + 2, Toggle: getDoDebug());
                if (E.Item.IsBroken() || E.Item.IsRusted() || E.Item.IsEMPed())
                {
                    Debug.LoopItem(4, $"{nameof(E.Item.IsBroken)}", $"{E.Item.IsBroken()}", 
                        Good: E.Item.IsBroken(), Indent: indent + 3, Toggle: getDoDebug());

                    Debug.LoopItem(4, $"{nameof(E.Item.IsRusted)}", $"{E.Item.IsRusted()}", 
                        Good: E.Item.IsRusted(), Indent: indent + 3, Toggle: getDoDebug());

                    Debug.LoopItem(4, $"{nameof(E.Item.IsEMPed)}", $"{E.Item.IsEMPed()}", 
                        Good: E.Item.IsEMPed(), Indent: indent + 3, Toggle: getDoDebug());

                    E.Actor.Fail(E.Item.Does("do") + " nothing.");
                    Debug.LastIndent = indent;
                    return false;
                }

                List<GameObject> inventoryAndEquipmentList = E.Actor.GetInventoryAndEquipment(GO => CanTape(GO, E.Actor));
                Debug.Entry(3, $"Compiling {nameof(inventoryAndEquipmentList)}...", Indent: indent + 2, Toggle: getDoDebug());
                if (inventoryAndEquipmentList.IsNullOrEmpty())
                {
                    Debug.CheckNah(4, $"{nameof(inventoryAndEquipmentList)} is empty", Indent: indent + 3, Toggle: getDoDebug());

                    E.Actor.Fail("=object.T= =verb:have:afterpronoun= no items that need {{utilitape|utilitape}}.");
                    Debug.LastIndent = indent;
                    return false;
                }

                Debug.Entry(3, $"Showing item Picker...", Indent: indent + 2, Toggle: getDoDebug());
                GameObject pickedGameObject = PickItem.ShowPicker(
                    Items: inventoryAndEquipmentList,
                    Style: PickItem.PickItemDialogStyle.SelectItemDialog,
                    Actor: E.Actor);
                if (pickedGameObject == null)
                {
                    Debug.CheckNah(4, $"{nameof(pickedGameObject)} is null", Indent: indent + 3, Toggle: getDoDebug());
                    Debug.LastIndent = indent;
                    return false;
                }
                else
                {
                    Debug.CheckYeh(4, $"{nameof(pickedGameObject)}: {pickedGameObject?.DebugName ?? NULL}",
                        Indent: indent + 3, Toggle: getDoDebug());
                }
                pickedGameObject.SplitFromStack();

                if (E.Actor.IsPlayerControlled() && !E.Item.Understood())
                {
                    Debug.Entry(3, $"Making {nameof(E.Item)} ({E.Item.BaseDisplayName}) Understood...", 
                        Indent: indent + 2, Toggle: getDoDebug());
                    E.Item.MakeUnderstood();
                }

                string appliedMessage = pickedGameObject.Does(Verb: "become") + " held together by {{utilitape|utilitape}}.";
                string consumedMessage = GameText.VariableReplace(
                        Message: "=object.T= used the entire roll of {{utilitape|=subject.name=}}!",
                        Subject: E.Item,
                        Object: E.Actor);

                if (!ItemModding.ApplyModification(Object: pickedGameObject, ModPartName: nameof(Mod_UD_Ductape), Actor: E.Actor))
                {
                    Debug.CheckNah(4, $"{nameof(pickedGameObject)} couldn't be modified", Indent: indent + 3, Toggle: getDoDebug());

                    E.Actor.Fail("Nothing happens.");
                    pickedGameObject.CheckStack();
                    Debug.LastIndent = indent;
                    return false;
                }
                else
                {
                    Debug.CheckYeh(4, $"Modification applied to {nameof(pickedGameObject)}", Indent: indent + 3, Toggle: getDoDebug());
                }

                Debug.Entry(3, $"Playing Sounds...", Indent: indent + 2, Toggle: getDoDebug());
                E.Actor.PlayWorldOrUISound(APPLIED_SOUND_CLOTH);
                E.Actor.PlayWorldOrUISound(APPLIED_SOUND_TINKER);

                Debug.Entry(3, $"Outputting messages...", Indent: indent + 2, Toggle: getDoDebug());
                Debug.LoopItem(3, $"{nameof(appliedMessage)}", appliedMessage, Indent: indent + 3, Toggle: getDoDebug());
                Debug.LoopItem(3, $"{nameof(consumedMessage)}", consumedMessage, Indent: indent + 3, Toggle: getDoDebug());
                if (E.Actor.IsPlayerControlled())
                {
                    Popup.Show(appliedMessage);
                    Popup.Show(consumedMessage);
                }
                else
                {
                    E.Actor.EmitMessage(appliedMessage);
                    E.Actor.EmitMessage(consumedMessage);
                }

                if (!pickedGameObject.Understood())
                {
                    Debug.Entry(3, $"Making {nameof(pickedGameObject)} ({pickedGameObject.BaseDisplayName}) Understood...",
                        Indent: indent + 2, Toggle: getDoDebug());

                    pickedGameObject.MakeUnderstood();
                }

                Debug.Entry(3, $"Tidying up...", Indent: indent + 2, Toggle: getDoDebug());
                E.Item.Destroy(Silent: true);
                pickedGameObject.CheckStack();
                Debug.LastIndent = indent;
                return true;
            }
            return base.HandleEvent(E);
        }

        private bool CanTape(GameObject Item, GameObject Actor)
        {
            return ItemModding.ModificationApplicable(nameof(Mod_UD_Ductape), Item, Actor);
        }
    }
}
