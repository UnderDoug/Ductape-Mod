using System;
using System.Collections.Generic;
using System.Text;
using UD_Ductape_Mod;
using XRL.Language;
using XRL.Rules;
using XRL.UI;
using XRL.World;
using XRL.World.Capabilities;
using XRL.World.Parts;
using XRL.World.Tinkering;
using static UD_Ductape_Mod.Const;
using static UD_Ductape_Mod.Options;
using static UD_Ductape_Mod.Utils;
using Debug = UD_Ductape_Mod.Debug;
using Options = UD_Ductape_Mod.Options;

namespace XRL.World
{
    [GameEvent(Cascade = CASCADE_ALL, Cache = Cache.Pool)]
    public class UD_JostleObjectEvent : ModPooledEvent<UD_JostleObjectEvent>
    {
        private static bool doDebug => getClassDoDebug(nameof(UD_JostleObjectEvent));
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

        public new static readonly int CascadeLevel = CASCADE_ALL;

        public static string RegisteredEventID => nameof(UD_JostleObjectEvent);

        public GameObject Item;
        public int Activity;

        public override int GetCascadeLevel()
        {
            return CascadeLevel;
        }

        public virtual string GetRegisteredEventID()
        {
            return RegisteredEventID;
        }

        public override void Reset()
        {
            base.Reset();
            Item = null;
            Activity = 0;
        }

        public static bool CheckFor(GameObject Item, int Activity)
        {
            Debug.Entry(4,
                $"! {nameof(UD_JostleObjectEvent)}."
                + $"{nameof(CheckFor)}"
                + $"(Item: {Item?.DebugName ?? NULL},"
                + $" Amount: {Activity})",
                Indent: 0, Toggle: doDebug);

            UD_JostleObjectEvent E = FromPool();

            E.Item = Item;
            E.Activity = Activity;

            bool jostle = Item != null;

            if (jostle && Item.WantEvent(ID, E.GetCascadeLevel()))
            {
                jostle = Item.HandleEvent(E);
            }
            if (jostle && Item.HasRegisteredEvent(E.GetRegisteredEventID()))
            {
                Event @event = Event.New(E.GetRegisteredEventID());
                @event.SetParameter(nameof(Item), E.Item);
                @event.SetParameter(nameof(Activity), E.Activity);
                jostle = Item.FireEvent(@event);
            }
            return jostle;
        }
    }
}

