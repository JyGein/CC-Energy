using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CCEnergy.Energies;

internal class ChargedEnergy : EnergyInfo
{
    public static readonly UK UK = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();

    public override UK GetUK() => UK;

    public override int GetTurnEnergy(State state, Combat c)
    {
        int max = ModEntry.EnergyApi.chargedEnergyApi.GetMaximumEnergy(state, c, 6);
        return c.turn > max ? max : (c.turn == 0 ? 1 : c.turn);
    }

    public override Color GetColor()
    {
        return new Color("00eeee");
    }
}
