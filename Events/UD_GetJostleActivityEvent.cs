using System;
using System.Collections.Generic;
using System.Text;

using XRL.UI;
using XRL.Rules;
using XRL.Language;
using XRL.World;
using XRL.World.Capabilities;
using XRL.World.Tinkering;

using UD_Ductape_Mod;

using static UD_Ductape_Mod.Const;
using static UD_Ductape_Mod.Options;
using static UD_Ductape_Mod.Utils;

using Debug = UD_Ductape_Mod.Debug;
using Options = UD_Ductape_Mod.Options;

namespace XRL.World
{
    [GameEvent(Cascade = CASCADE_ALL, Cache = Cache.Pool)]
    public class UD_GetJostleActivityEvent : ModPooledEvent<UD_GetJostleActivityEvent>
    {
        private static bool doDebug => getClassDoDebug(nameof(UD_GetJostleActivityEvent));
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

        public static string RegisteredEventID => nameof(UD_GetJostleActivityEvent);

        public GameObject Item;
        public int Activity;
        public int OriginalActivity;
        public MinEvent FromEvent;
        public Event FromSEvent;

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
            OriginalActivity = 0;
            FromEvent = null;
            FromSEvent = null;
        }

        public static int For(GameObject Item, int Activity, MinEvent FromEvent = null, Event FromSEvent = null)
        {
            Debug.Entry(4,
                $"! {nameof(UD_GetJostleActivityEvent)}."
                + $"{nameof(For)}"
                + $"(Item: {Item?.DebugName ?? NULL},"
                + $" Amount: {Activity})",
                Indent: 0, Toggle: getDoDebug());

            UD_GetJostleActivityEvent E = FromPool(Item, Activity, FromEvent, FromSEvent);

            bool proceed = E != null;

            if (proceed && Item.WantEvent(ID, E.GetCascadeLevel()))
            {
                proceed = Item.HandleEvent(E);
            }
            if (proceed && Item.HasRegisteredEvent(E.GetRegisteredEventID()))
            {
                Event @event = Event.New(E.GetRegisteredEventID());
                @event.SetParameter(nameof(E.Item), E.Item);
                @event.SetParameter(nameof(E.Activity), E.Activity);
                @event.SetParameter(nameof(E.OriginalActivity), E.Activity);
                @event.SetParameter(nameof(E.FromEvent), E.FromEvent);
                @event.SetParameter(nameof(E.FromSEvent), E.FromSEvent);
                proceed = Item.FireEvent(@event);
                E.Activity = @event.GetIntParameter(nameof(E.Activity));
            }

            int activity = E.Activity;
            E.Reset();
            return activity;
        }

        public static UD_GetJostleActivityEvent FromPool(GameObject Item, int Activity, MinEvent FromEvent = null, Event FromSEvent = null)
        {
            UD_GetJostleActivityEvent E = FromPool();
            if (Item != null)
            {
                E.Item = Item;
                E.Activity = Activity;
                E.OriginalActivity = Activity;
                E.FromEvent = FromEvent;
                E.FromSEvent = FromSEvent;
                return E;
            }
            E.Reset();
            return null;
        }
    }
}

