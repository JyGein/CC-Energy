using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCEnergy.Energies;

internal abstract class EnergyInfo
{
    public abstract UK GetUK();
    public abstract int GetTurnEnergy(State state, Combat c);
    public abstract Color GetColor();
}
