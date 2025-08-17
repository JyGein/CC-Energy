using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CCEnergy.IEnergyApi;

namespace CCEnergy;

internal static class ModDataExt
{
    static readonly string ModdedEnergy = "ModdedEnergy";
    public static Dictionary<Energy, int> GetEnergy(this Combat c)
        => ModEntry.Instance.Helper.ModData.GetModDataOrDefault<Dictionary<Energy, int>>(c, ModdedEnergy, new());
    public static void SetEnergy(this Combat c, Dictionary<Energy, int> energyBlock)
        => ModEntry.Instance.Helper.ModData.SetModData(c, ModdedEnergy, energyBlock);

    static readonly string ModdedEnergyDiscount = "ModdedEnergyCostDiscount";
    public static Dictionary<Energy, int> GetModdedEnergyDiscount(this Card card)
        => ModEntry.Instance.Helper.ModData.GetModDataOrDefault<Dictionary<Energy, int>>(card, ModdedEnergyDiscount, []);
    public static void SetModdedEnergyDiscount(this Card card, Dictionary<Energy, int> energyBlock)
        => ModEntry.Instance.Helper.ModData.SetModData(card, ModdedEnergyDiscount, energyBlock);
}
