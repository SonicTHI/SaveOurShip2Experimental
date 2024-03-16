using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld
{
    class Designator_Ship : Designator_Place
    {
        public override BuildableDef PlacingDef
        {
            get { return null; }
        }

        public override ThingStyleDef ThingStyleDefForPreview => null;

        public override ThingDef StuffDef => null;

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            throw new NotImplementedException();
        }
    }
}
