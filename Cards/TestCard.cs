using Nanoray.PluginManager;
using Nickel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using static CCEnergy.IEnergyApi;
using CCEnergy.External;

namespace CCEnergy.Cards;

internal sealed class TestCard : Card, IRegisterable, ISetModdedEnergyCostBaseHook
{
    public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
    {
        helper.Content.Cards.RegisterCard("TestCard", new()
        {
            CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
            Meta = new()
            {
                deck = Deck.test,
                rarity = Rarity.common,
                upgradesTo = [Upgrade.A, Upgrade.B]
            },
            Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "TestCard", "name"]).Localize,
            Art = StableSpr.cards_Spacer
        });
    }

    public TestCard()
    {
        ModEntry.EnergyApi.SetModdedEnergyCostBaseHook(this, this);
    }

    public IDictionary<Energy, int> GetModdedEnergyCostBase(State s)
    {
        return new Dictionary<Energy, int>()
        {
            { Energy.Revenge, upgrade == Upgrade.None ? 1 : 0 },
            { Energy.Calm, upgrade == Upgrade.A ? 1 : 0 },
            { Energy.Thermal, upgrade == Upgrade.B ? 1 : 0 }
        };
    }

    public override CardData GetData(State state)
    {
        CardData data = new CardData()
        {
            cost = 1,
            singleUse = true
        };
        return data;
    }

    public override List<CardAction> GetActions(State s, Combat c)
    {
        List<CardAction> actions =
        [
            new ADrawCard()
            {
                count = 1
            },
        ];
        return actions;
    }
}
