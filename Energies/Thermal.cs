using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCEnergy.Energies;

internal class ThermalEnergy : EnergyInfo
{
    public static readonly UK UK = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();

    public override UK GetUK() => UK;

    public override int GetTurnEnergy(State state, Combat c)
    {
        return state.ship.Get(Status.heat);
    }

    public override Color GetColor()
    {
        return new Color("f5a449");
    }
}
