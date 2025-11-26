using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCEnergy.Energies;

internal class KineticEnergy : EnergyInfo
{
    public static readonly UK UK = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();

    public override UK GetUK() => UK;

    public override int GetTurnEnergy(State state, Combat c)
    {
        int output = Math.Abs(ModEntry.Instance.Helper.ModData.GetModDataOrDefault(c, "PrevPosition", state.ship.x) - state.ship.x);
        ModEntry.Instance.Helper.ModData.SetModData(c, "PrevPosition", state.ship.x);
        return output;
    }   

    public override Color GetColor()
    {
        return new Color("ffffff");
    }
}
