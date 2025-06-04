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

using Debug = UD_Ductape_Mod.Debug;
using Options = UD_Ductape_Mod.Options;

namespace XRL.World
{
    [GameEvent(Cascade = CASCADE_ALL, Cache = Cache.Pool)]
    public class UD_JostleObjectEvent : ModPooledEvent<UD_JostleObjectEvent>
    {
        private static bool doDebug = true;

        public new static readonly int CascadeLevel = CASCADE_ALL;

        public GameObject Item;
        public int Activity;
        public int OriginalAmount;

        public override int GetCascadeLevel()
        {
            return CascadeLevel;
        }

        public virtual string GetRegisteredEventID()
        {
            return $"{nameof(UD_JostleObjectEvent)}";
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

            bool jostle = Item != null;

            if (jostle && Item.WantEvent(ID, E.GetCascadeLevel()))
            {
                E.Item = Item;
                E.Activity = Activity;
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

