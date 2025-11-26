using FMOD.Studio;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CCEnergy.Energies;

internal class ResidualEnergy : EnergyInfo
{
    public static readonly UK UK = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();
    
    public override UK GetUK() => UK;

    public override int GetTurnEnergy(State state, Combat c)
    {
        return ModEntry.Instance.Helper.ModData.GetModDataOrDefault(c, "PrevEnergy", 0);
    }

    public override Color GetColor()
    {
        return new Color("e6e164");
    }

    public override void DoPatches()
    {
        ModEntry.Instance.Harmony.Patch(
            original: AccessTools.DeclaredMethod(typeof(AAfterPlayerTurn), nameof(AAfterPlayerTurn.Begin)),
            prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AStartPlayerTurn_Begin_Prefix))
        );
    }

    public static void AStartPlayerTurn_Begin_Prefix(Combat c)
    {
        ModEntry.Instance.Helper.ModData.SetModData(c, "PrevEnergy", c.energy);
    }
}
