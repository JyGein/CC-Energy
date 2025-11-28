using Microsoft.Xna.Framework;
using Nanoray.PluginManager;
using Nickel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCEnergy.Energies;

internal abstract class EnergyInfo : IEnergyApi.IEnergyInfoApi
{
    public abstract UK GetUK();
    public abstract int GetTurnEnergy(State state, Combat c);
    public abstract Color GetColor();
    public virtual void DoPatches() { }
}
