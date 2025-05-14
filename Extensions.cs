using System;
using System.Collections.Generic;
using System.Text;

using XRL.World;
using XRL.World.Parts;

namespace UD_Ductape_Mod
{
    public static class Extensions
    {
        public static bool UsesCharge(this GameObject Object)
        {
            if (Object == null || !Object.HasPartDescendedFrom<IActivePart>())
            {
                return false;
            }

            foreach (IActivePart part in Object.GetPartsDescendedFrom<IActivePart>())
            {
                if (part.ChargeUse > 0)
                {
                    return true;
                }
            }    

            return false;
        }
    }
}
