using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CCEnergy.Energies;

internal class CoreEnergy : EnergyInfo
{
    public static readonly UK UK = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();

    public override UK GetUK() => UK;

    public override int GetTurnEnergy(State state, Combat c)
    {
        return ModEntry.Instance.Helper.ModData.GetModDataOrDefault(c, "PrevCoreEnergy", 10);
    }

    public override Color GetColor()
    {
        return new Color("333333");
    }

    public override void DoPatches()
    {
        ModEntry.Instance.Harmony.Patch(
            original: AccessTools.DeclaredMethod(typeof(AAfterPlayerTurn), nameof(AAfterPlayerTurn.Begin)),
            prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAfterPlayerTurn_Begin_Prefix))
        );
    }

    public static void AAfterPlayerTurn_Begin_Prefix(Combat c)
    {
        Dictionary<IEnergyApi.Energy, int> energy = c.GetEnergy();
        if (!energy.ContainsKey(IEnergyApi.Energy.Core)) return;
        ModEntry.Instance.Helper.ModData.SetModData(c, "PrevCoreEnergy", energy[IEnergyApi.Energy.Core]);
    }
}
