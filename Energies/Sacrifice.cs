using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CCEnergy.Energies;

internal class SacrificeEnergy : EnergyInfo
{
    public static readonly UK UK = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();

    public override UK GetUK() => UK;

    public override int GetTurnEnergy(State state, Combat c)
    {
        return ModEntry.Instance.Helper.ModData.GetModDataOrDefault(c, "PrevCardsInHand", 0);
    }

    public override Color GetColor()
    {
        return new Color("ee1111");
    }

    public override void DoPatches()
    {
        ModEntry.Instance.Harmony.Patch(
            original: AccessTools.DeclaredMethod(typeof(AEndTurn), nameof(AEndTurn.Begin)),
            prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AEndTurn_Begin_Prefix))
        );
    }

    public static void AEndTurn_Begin_Prefix(Combat c)
    {
        ModEntry.Instance.Helper.ModData.SetModData(c, "PrevCardsInHand", c.hand.Count);
    }
}
