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
    readonly List<IModdedTurnEnergyOverrideHook> ModdedTurnEnergyOverrideHooks = [];
    public void SetModdedEnergyCostBase(Card card, IDictionary<Energy, int> energyBlock)
    {
        SetModdedEnergyCostBaseHook(card, new DefaultSetModdedEnergyCostBaseHook { BaseModdedEnergyCost = energyBlock });
    }

    public void SetModdedEnergyCostBaseHook(Card card, ISetModdedEnergyCostBaseHook hook)
    {
        ModdedEnergyBaseCostHooks[card] = hook;
    }

    public IDictionary<Energy, int> GetModdedEnergyBaseCost(Card card, State s)
    {
        return ModdedEnergyBaseCostHooks.TryGetValue(card, out ISetModdedEnergyCostBaseHook? hook) ? hook.GetModdedEnergyCostBase(s) : new Dictionary<Energy, int>();
    }

    public IDictionary<Energy, int> GetModdedEnergyCost(Card card, State s)
    {
        Dictionary<Energy, int> cost = [];
        if (ModdedEnergyBaseCostHooks.TryGetValue(card, out ISetModdedEnergyCostBaseHook? hook)) {
            cost = hook.GetModdedEnergyCostBase(s).ToDictionary();
        }
        Dictionary<Energy, int> discounts = GetCardModdedEnergyDiscounts(card).ToDictionary();
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
            cost = moddedEnergyCostOverrideHook.GetModdedEnergyCostOveridden(card, s, cost).ToDictionary();
        }
        return cost;
    }

    public void AddCardModdedEnergyDiscount(Card card, Energy energyToDiscount, int amountToDiscount)
    {
        Dictionary<Energy, int> currentDiscount = card.GetModdedEnergyDiscount();
        currentDiscount[energyToDiscount] = currentDiscount.TryGetValue(energyToDiscount, out int value) ? value + amountToDiscount : amountToDiscount;
        card.SetModdedEnergyDiscount(currentDiscount);
    }

    public void SetCardModdedEnergyDiscount(Card card, IDictionary<Energy, int> discountEnergyBlock)
    {
        card.SetModdedEnergyDiscount(discountEnergyBlock.ToDictionary());
    }

    public IDictionary<Energy, int> GetCardModdedEnergyDiscounts(Card card)
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

    public void SetCombatModdedEnergy(Combat c, IDictionary<Energy, int> energyBlock)
    {
        c.SetEnergy(energyBlock.ToDictionary());
    }

    public IDictionary<Energy, int> GetCombatModdedEnergy(Combat c)
    {
        return c.GetEnergy();
    }

    public List<Energy> GetInUseEnergies(Combat c, State s)
    {
        List<Energy> inUseEnergy = [];
        foreach(Card card in s.deck.Concat(c.hand).Concat(c.discard).Concat(c.exhausted))
        {
            Dictionary<Energy, int> cardCost = GetModdedEnergyCost(card, s).ToDictionary();
            foreach (Energy energy in cardCost.Keys)
            {
                if (cardCost[energy] > 0 && !inUseEnergy.Contains(energy)) inUseEnergy.Add(energy);
            }
        }
        inUseEnergy.Sort();
        return inUseEnergy;
    }

    public void RegisterModdedTurnEnergyOverrideHook(IModdedTurnEnergyOverrideHook hook)
    {
        ModdedTurnEnergyOverrideHooks.Add(hook);
    }

    public void UnregisterModdedTurnEnergyOverrideHook(IModdedTurnEnergyOverrideHook hook)
    {
        ModdedTurnEnergyOverrideHooks.Remove(hook);
    }

    public IDictionary<Energy, int> GetOverriddenTurnEnergy(Combat c, State s, IDictionary<Energy, int> energyBlock)
    {
        foreach (IModdedTurnEnergyOverrideHook hook in ModdedTurnEnergyOverrideHooks)
        {
            energyBlock = hook.GetModdedEnergyCostOveridden(c, s, energyBlock);
        }
        return energyBlock;
    }
}

internal sealed class DefaultSetModdedEnergyCostBaseHook : ISetModdedEnergyCostBaseHook
{
    public required IDictionary<Energy, int> BaseModdedEnergyCost;
    public IDictionary<Energy, int> GetModdedEnergyCostBase(State s)
        => BaseModdedEnergyCost;
}
