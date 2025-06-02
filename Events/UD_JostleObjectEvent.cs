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
        public int Amount;
        public int OriginalAmount;
        public MinEvent Source;

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
            Amount = 0;
            Source = null;
        }

        public static void Send(GameObject Item, int Amount, MinEvent Source = null)
        {
            Debug.Entry(4,
                $"{nameof(UD_JostleObjectEvent)}." +
                $"{nameof(Send)}(int Amount: {Amount}, GameObject Actor: {Item?.DebugName ?? NULL})",
                Indent: 0, Toggle: doDebug);

            UD_JostleObjectEvent E = FromPool();

            bool flag = true;
            if (flag && Item.WantEvent(ID, E.GetCascadeLevel()))
            {
                E.Item = Item;
                E.Amount = Amount;
                E.OriginalAmount = Amount;
                E.Source = Source;
                flag = Item.HandleEvent(E);
            }
            if (flag && Item.HasRegisteredEvent(E.GetRegisteredEventID()))
            {
                Event @event = Event.New(E.GetRegisteredEventID());
                @event.SetParameter(nameof(Item), Item);
                @event.SetParameter(nameof(Amount), Amount);
                Item.FireEvent(@event);
            }
        }
    }
}

