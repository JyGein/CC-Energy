using CCEnergy.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CCEnergy.IEnergyApi;

namespace CCEnergy;

public class EnergyApi : IEnergyApi
{
    readonly Dictionary<Card, ISetModdedEnergyCostBaseHook> ModdedEnergyBaseCostHooks = [];
    readonly List<IModdedEnergyCostOverrideHook> ModdedEnergyCostOverrideHooks = [];
    public void SetModdedEnergyCostBase(Card card, Dictionary<Energy, int> energyBlock)
    {
        SetModdedEnergyCostBaseHook(card, new DefaultSetModdedEnergyCostBaseHook { BaseModdedEnergyCost = energyBlock });
    }

    public void SetModdedEnergyCostBaseHook(Card card, ISetModdedEnergyCostBaseHook hook)
    {
        ModdedEnergyBaseCostHooks[card] = hook;
    }

    public Dictionary<Energy, int> GetModdedEnergyBaseCost(Card card, State s)
    {
        return ModdedEnergyBaseCostHooks.TryGetValue(card, out ISetModdedEnergyCostBaseHook? hook) ? hook.GetModdedEnergyCostBase(s) : [];
    }

    public Dictionary<Energy, int> GetModdedEnergyCost(Card card, State s)
    {
        Dictionary<Energy, int> cost = [];
        if (ModdedEnergyBaseCostHooks.TryGetValue(card, out ISetModdedEnergyCostBaseHook? hook)) {
            cost = hook.GetModdedEnergyCostBase(s);
        }
        Dictionary<Energy, int> discounts = GetCardModdedEnergyDiscounts(card);
        foreach (Energy energy in discounts.Keys)
        {
            if (cost.ContainsKey(energy))
            {
                cost[energy] -= discounts[energy];
                if (cost[energy] < 0) cost[energy] = 0;
            }
        }
        foreach (IModdedEnergyCostOverrideHook moddedEnergyCostOverrideHook in ModdedEnergyCostOverrideHooks)
        {
            cost = moddedEnergyCostOverrideHook.GetModdedEnergyCostOveridden(card, s, cost);
        }
        return cost;
    }

    public void AddCardModdedEnergyDiscount(Card card, Energy energyToDiscount, int amountToDiscount)
    {
        Dictionary<Energy, int> currentDiscount = card.GetModdedEnergyDiscount();
        currentDiscount[energyToDiscount] = currentDiscount.TryGetValue(energyToDiscount, out int value) ? value + amountToDiscount : amountToDiscount;
        card.SetModdedEnergyDiscount(currentDiscount);
    }

    public void SetCardModdedEnergyDiscount(Card card, Dictionary<Energy, int> discountEnergyBlock)
    {
        card.SetModdedEnergyDiscount(discountEnergyBlock);
    }

    public Dictionary<Energy, int> GetCardModdedEnergyDiscounts(Card card)
        => card.GetModdedEnergyDiscount();

    public void RegisterModdedEnergyCostOverrideHook(IModdedEnergyCostOverrideHook hook)
    {
        ModdedEnergyCostOverrideHooks.Add(hook);
    }

    public void UnregisterModdedEnergyCostOverrideHook(IModdedEnergyCostOverrideHook hook)
    {
        ModdedEnergyCostOverrideHooks.Remove(hook);
    }

    public IAModdedEnergy AModdedEnergy => new AModdedEnergy();

    public void SetCombatModdedEnergy(Combat c, Dictionary<Energy, int> energyBlock)
    {
        c.SetEnergy(energyBlock);
    }

    public Dictionary<Energy, int> GetCombatModdedEnergy(Combat c)
    {
        return c.GetEnergy();
    }

    public List<Energy> GetInUseEnergies(Combat c, State s)
    {
        List<Energy> inUseEnergy = [];
        foreach(Card card in s.deck.Concat(c.hand).Concat(c.discard))
        {
            Dictionary<Energy, int> cardCost = GetModdedEnergyCost(card, s);
            foreach (Energy energy in cardCost.Keys)
            {
                if (cardCost[energy] > 0 && !inUseEnergy.Contains(energy)) inUseEnergy.Add(energy);
            }
        }
        inUseEnergy.Sort();
        return inUseEnergy;
    }
}

internal sealed class DefaultSetModdedEnergyCostBaseHook : ISetModdedEnergyCostBaseHook
{
    public required Dictionary<Energy, int> BaseModdedEnergyCost;
    public Dictionary<Energy, int> GetModdedEnergyCostBase(State s)
        => BaseModdedEnergyCost;
}
