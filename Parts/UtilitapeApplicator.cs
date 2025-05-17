using System;
using System.Collections.Generic;
using System.Text;

using XRL.UI;
using XRL.World.Tinkering;

using UD_Ductape_Mod;
using static UD_Ductape_Mod.Const;

using Debug = UD_Ductape_Mod.Debug;
using Options = UD_Ductape_Mod.Options;

namespace XRL.World.Parts
{
    [Serializable]
    public class UtilitapeApplicator : IScribedPart
    {
        public static readonly string APPLIED_SOUND = "sfx_equip_material_generic_cloth"; // "Sounds/Interact/sfx_interact_bandage_apply";

        public override bool WantEvent(int ID, int Cascade)
        {
            return base.WantEvent(ID, Cascade)
                || ID == InventoryActionEvent.ID;
        }
        public override bool HandleEvent(InventoryActionEvent E)
        {
            if (E.Command == "Apply")
            {
                if (!E.Actor.CheckFrozen(Telepathic: false, Telekinetic: true))
                {
                    return false;
                }
                if (E.Item.IsBroken() || E.Item.IsRusted() || E.Item.IsEMPed())
                {
                    E.Actor.Fail(ParentObject.Does("do", int.MaxValue, null, null, null, AsIfKnown: false, Single: false, NoConfusion: false, NoColor: false, Stripped: false, WithoutTitles: true, Short: true, BaseOnly: false, WithIndefiniteArticle: false, null, IndicateHidden: false, Pronoun: true, SecondPerson: true, null) + " nothing.");
                    return false;
                }
                List<GameObject> objects = E.Actor.Inventory.GetObjects((GameObject o) => CanTape(o, E.Actor));
                if (objects.Count == 0)
                {
                    E.Actor.Fail("You have no items that need utilitape.");
                    return false;
                }
                GameObject gameObject = PickItem.ShowPicker(objects, null, PickItem.PickItemDialogStyle.SelectItemDialog, E.Actor);
                if (gameObject == null)
                {
                    return false;
                }
                gameObject.SplitFromStack();
                if (E.Actor.IsPlayer())
                {
                    ParentObject.MakeUnderstood();
                }
                string message = gameObject.Does("become", int.MaxValue, null, null, null, AsIfKnown: false, Single: false, NoConfusion: false, NoColor: false, Stripped: false, WithoutTitles: true, Short: true, BaseOnly: false, WithIndefiniteArticle: false, null, IndicateHidden: false, Pronoun: false, SecondPerson: true, null) + " held together by utilitape!";
                bool flag = gameObject.Understood();
                if (!ItemModding.ApplyModification(gameObject, nameof(Mod_UD_Ductape), DoRegistration: true, E.Actor))
                {
                    E.Actor.Fail("Nothing happens.");
                    gameObject.CheckStack();
                    return false;
                }
                E.Actor.PlayWorldOrUISound(APPLIED_SOUND, null);
                if (E.Actor.IsPlayer())
                {
                    Popup.Show(message);
                    Popup.Show(GameText.VariableReplace($"=object.T used the entire roll of =subject.name=!", ParentObject, The.Player));
                }
                if (flag && !gameObject.Understood())
                {
                    gameObject.MakeUnderstood();
                }
                ParentObject.Destroy();
                gameObject.CheckStack();
            }
            return base.HandleEvent(E);
        }

        private bool CanTape(GameObject Item, GameObject Actor)
        {
            return ItemModding.ModificationApplicable(nameof(Mod_UD_Ductape), Item, Actor);
        }
    }
}
