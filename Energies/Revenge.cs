﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCEnergy.Energies;

internal class RevengeEnergy : EnergyInfo
{
    public static readonly UK UK = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();

    public override UK GetUK() => UK;

    public override int GetTurnEnergy(State state, Combat c)
    {
        float healthPercent = (float)state.ship.hull / (float)state.ship.hullMax;
        if (healthPercent > 0.75) return 0;
        if (healthPercent > 0.5) return 1;
        if (healthPercent > 0.25) return 2;
        return 3;
    }

    public override Color GetColor()
    {
        return new Color("e61ec8");
    }
}
