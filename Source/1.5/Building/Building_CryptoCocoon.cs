using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaveOurShip2
{
    class Building_CryptoCocoon : Building_CryptosleepCasket
    {
        public override void EjectContents()
        {
            base.EjectContents();
            Kill();
        }
    }
}
