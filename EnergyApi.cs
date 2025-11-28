using CCEnergy.Actions;
using CCEnergy.Energies;
using CCEnergy.External;
using HarmonyLib;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Nickel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static CCEnergy.External.IKokoroApi.IV2.IActionCostsApi;
using static CCEnergy.IEnergyApi;
using static CCEnergy.IEnergyApi.IChargedEnergyApi;

namespace CCEnergy;

public class EnergyApi : IEnergyApi
{
    readonly Dictionary<Card, ISetModdedEnergyCostBaseHook> ModdedEnergyBaseCostHooks = [];
    readonly List<IModdedEnergyCostOverrideHook> ModdedEnergyCostOverrideHooks = [];
    readonly List<IModdedTurnEnergyOverrideHook> ModdedTurnEnergyOverrideHooks = [];
    readonly List<IInUseEnergyHook> InUseEnergyHooks = [];
    public readonly ChargedEnergyApi chargedEnergyApi = new();
    public readonly ModdedEnergyAsStatusApi moddedEnergyAsStatusApi = new();

    public sealed class ModdedEnergyResource : IResource
    {
        public Energy EnergyType { get; init; }

        public string ResourceKey
        {
            get
            {
                ResourceKeyStorage ??= $"actioncost.resource.energy.{EnergyType}";
                return ResourceKeyStorage;
            }
        }

        private string? ResourceKeyStorage;

        public int GetCurrentResourceAmount(State state, Combat combat)
        {
            return combat.GetEnergy().GetValueOrDefault(EnergyType, 0);
        }

        public IReadOnlyList<Tooltip> GetTooltips(State state, Combat combat, int amount)
        {
            if (amount <= 0)
                return [];
            EnergyInfo energyInfo = ModEntry.Energies[(int)EnergyType];
            string nameFormat = ModEntry.Instance.Localizations.Localize(["resourceCost", "energy", "name"]);
            string descriptionFormat = ModEntry.Instance.Localizations.Localize(["resourceCost", "energy", "description"]);
            Spr icon = ModEntry.Instance.KokoroApi.ActionCosts.GetResourceCostIcons(this, amount)[0].CostSatisfiedIcon;
            string name = string.Format(nameFormat, EnergyType.ToString().ToUpper());
            string description = string.Format(descriptionFormat, amount, energyInfo.GetColor(), EnergyType.ToString().ToUpper()); 

            return [
                new GlossaryTooltip(ResourceKey)
                    {
                        Icon = icon,
                        TitleColor = energyInfo.GetColor(),
                        Title = name,
                        Description = description,
                    }
            ];
        }

        public void Pay(State state, Combat combat, int amount)
        {
            Dictionary<Energy, int> energyBlock = combat.GetEnergy();
            energyBlock[EnergyType] = energyBlock.GetValueOrDefault(EnergyType, 0) - amount;
            combat.SetEnergy(energyBlock);
        }
    }

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

    public IList<Energy> GetInUseEnergiesInCombat(State s, Combat c)
    {
        List<Energy> inUseEnergy = [];
        foreach(Card card in s.deck.Concat(c.hand).Concat(c.discard).Concat(c.exhausted))
        {
            List<CardAction> actions = card.GetActions(s, c);
            Dictionary<Energy, int> cardCost = GetModdedEnergyCost(card, s).ToDictionary();
            foreach (Energy energy in cardCost.Keys)
            {
                if (cardCost[energy] > 0 && !inUseEnergy.Contains(energy)) inUseEnergy.Add(energy);
            }
            foreach (CardAction action in actions)
            {
                if (action is ModdedEnergyAsStatusApi.AModdedEnergyVariableHint moddedEnergyVariableHintAction && !inUseEnergy.Contains(moddedEnergyVariableHintAction.EnergyType)) inUseEnergy.Add(moddedEnergyVariableHintAction.EnergyType);
                ICostAction? costAction = ModEntry.Instance.KokoroApi.ActionCosts.AsCostAction(action);
                if (costAction != null)
                {
                    IResourceCost? resourceCost = ModEntry.Instance.KokoroApi.ActionCosts.AsResourceCost(costAction.Cost);
                    if (resourceCost != null) foreach (IResource resource in resourceCost.PotentialResources) if (resource is ModdedEnergyResource moddedEnergyResource && !inUseEnergy.Contains(moddedEnergyResource.EnergyType)) inUseEnergy.Add(moddedEnergyResource.EnergyType);
                }
            }
        }
        foreach (IInUseEnergyHook hook in InUseEnergyHooks)
        {
            inUseEnergy = [.. inUseEnergy, .. hook.MoreEnergiesInUse(s, c)];
        }
        inUseEnergy = [.. inUseEnergy.Distinct()];
        inUseEnergy.Sort();
        return inUseEnergy;
    }

    public IList<Energy> GetInUseEnergiesOutOfCombat(State s)
    {
        List<Energy> inUseEnergy = [];
        foreach (Card card in s.deck)
        {
            List<CardAction> actions = card.GetActions(s, DB.fakeCombat);
            Dictionary<Energy, int> cardCost = GetModdedEnergyCost(card, s).ToDictionary();
            foreach (Energy energy in cardCost.Keys)
            {
                if (cardCost[energy] > 0 && !inUseEnergy.Contains(energy)) inUseEnergy.Add(energy);
            }
            foreach (CardAction action in actions)
            {
                if (action is ModdedEnergyAsStatusApi.AModdedEnergyVariableHint moddedEnergyVariableHintAction && !inUseEnergy.Contains(moddedEnergyVariableHintAction.EnergyType)) inUseEnergy.Add(moddedEnergyVariableHintAction.EnergyType);
                ICostAction? costAction = ModEntry.Instance.KokoroApi.ActionCosts.AsCostAction(action);
                if (costAction != null)
                {
                    IResourceCost? resourceCost = ModEntry.Instance.KokoroApi.ActionCosts.AsResourceCost(costAction.Cost);
                    if (resourceCost != null) foreach (IResource resource in resourceCost.PotentialResources) if (resource is ModdedEnergyResource moddedEnergyResource && !inUseEnergy.Contains(moddedEnergyResource.EnergyType)) inUseEnergy.Add(moddedEnergyResource.EnergyType);
                }
            }
        }
        foreach (IInUseEnergyHook hook in InUseEnergyHooks)
        {
            inUseEnergy = [.. inUseEnergy, .. hook.MoreEnergiesInUseOutOfCombat(s)];
        }
        inUseEnergy = [.. inUseEnergy.Distinct()];
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

    IChargedEnergyApi IEnergyApi.ChargedEnergyApi
        => chargedEnergyApi;


    public sealed class ChargedEnergyApi : IChargedEnergyApi
    {
        public readonly List<IChargedEnergyMaximumOverrideHook> ModdedTurnEnergyOverrideHooks = [];

        public void RegisterChargedEnergyMaximumOverrideHook(IChargedEnergyMaximumOverrideHook hook)
        {
            ModdedTurnEnergyOverrideHooks.Add(hook);
        }

        public void UnRegisterChargedEnergyMaximumOverrideHook(IChargedEnergyMaximumOverrideHook hook)
        {
            ModdedTurnEnergyOverrideHooks.Remove(hook);
        }

        public int GetMaximumEnergy(State state, Combat c, int baseMax)
        {
            baseMax += state.ship.Get(ModEntry.Instance.MaxChargedEnergy.Status);
            foreach (IChargedEnergyMaximumOverrideHook chargedEnergyMaximumOverrideHook in ModdedTurnEnergyOverrideHooks)
            {
                baseMax = chargedEnergyMaximumOverrideHook.GetChargedEnergyMaximumOverridden(c, state, baseMax);
            }
            return baseMax;
        }

        public IStatusEntry MaxChargedEnergy => ModEntry.Instance.MaxChargedEnergy;
    }

    public IModdedEnergyAsStatusApi ModdedEnergyAsStatus
        => moddedEnergyAsStatusApi;

    public sealed class ModdedEnergyAsStatusApi : IModdedEnergyAsStatusApi
    {
        public IModdedEnergyAsStatusApi.IVariableHint? AsVariableHint(AVariableHint action)
            => action as IModdedEnergyAsStatusApi.IVariableHint;

        public IModdedEnergyAsStatusApi.IVariableHint MakeVariableHint(Energy energy, int? tooltipOverride = null)
            => new AModdedEnergyVariableHint { EnergyType = energy, TooltipOverride = tooltipOverride };

        public IModdedEnergyAsStatusApi.IStatusAction? AsStatusAction(AStatus action)
        {
            if (action is IModdedEnergyAsStatusApi.IStatusAction statusAction)
                return statusAction;
            if (ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(action, "moddedEnergy"))
                return new StatusWrapper { Wrapped = action };
            return null;
        }

        public IModdedEnergyAsStatusApi.IStatusAction MakeStatusAction(Energy energy, int amount, AStatusMode mode = AStatusMode.Add)
        {
            var wrapped = new AStatus
            {
                targetPlayer = true,
                statusAmount = amount,
                mode = mode,
            };
            ModEntry.Instance.Helper.ModData.SetModData(wrapped, "moddedEnergy", energy);
            return new StatusWrapper { Wrapped = wrapped };
        }

        private sealed class StatusWrapper : IModdedEnergyAsStatusApi.IStatusAction
        {
            public required AStatus Wrapped { get; init; }

            [JsonIgnore]
            public AStatus AsCardAction
                => Wrapped;
        }

        public sealed class AModdedEnergyVariableHint : AVariableHint, IModdedEnergyAsStatusApi.IVariableHint
        {
            public Energy EnergyType { get; set; }
            public int? TooltipOverride { get; set; }

            [JsonIgnore]
            public AVariableHint AsCardAction
                => this;

            public AModdedEnergyVariableHint()
            {
                this.hand = true;
            }

            public override Icon? GetIcon(State s)
                => new(
                    path: ModEntry.Instance.EnergyIcons[EnergyType].Sprite,
                    number: null,
                    color: Colors.action
                );

            public override List<Tooltip> GetTooltips(State s)
                => [
                    new GlossaryTooltip($"action.xHintModdedEnergy.{EnergyType}.desc")
                        {
                            Description = s.route is Combat combat
                                ? ModEntry.Instance.Localizations.Localize(["moddedEnergyVariableHint", "stateful"], new { Color = ModEntry.Energies[(int)EnergyType].GetColor(), Type = EnergyType.ToString().ToUpper(), Energy = ModEntry.Instance.Helper.ModData.GetOptionalModData<int>(this, "energyTooltipOverride") ?? ( combat.GetEnergy().GetValueOrDefault(EnergyType, 0)) })
                                : ModEntry.Instance.Localizations.Localize(["moddedEnergyVariableHint", "stateless"], new { Color = ModEntry.Energies[(int)EnergyType].GetColor(), Type = EnergyType.ToString().ToUpper() }),
                        }
                ];

            public IModdedEnergyAsStatusApi.IVariableHint SetTooltipOverride(int? value)
            {
                TooltipOverride = value;
                return this;
            }
        }

        internal sealed class ModdedEnergyAsStatusManager
        {
            internal static void Setup(Harmony harmony)
            {
                harmony.Patch(
                    original: AccessTools.DeclaredMethod(typeof(AStatus), nameof(AStatus.GetTooltips)),
                    postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AStatus_GetTooltips_Postfix))
                );
                harmony.Patch(
                    original: AccessTools.DeclaredMethod(typeof(AStatus), nameof(AStatus.GetIcon)),
                    postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AStatus_GetIcon_Postfix))
                );
                harmony.Patch(
                    original: AccessTools.DeclaredMethod(typeof(AStatus), nameof(AStatus.Begin)),
                    prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AStatus_Begin_Prefix))
                );
            }

            private static void AStatus_GetTooltips_Postfix(AStatus __instance, ref List<Tooltip> __result)
            {
                if (!ModEntry.Instance.Helper.ModData.TryGetModData(__instance, "moddedEnergy", out Energy energyType))
                    return;

                __result.Clear();
                __result.Add(new GlossaryTooltip($"AStatus.ModdedEnergy.{energyType}")
                {
                    Icon = ModEntry.Instance.GenericEnergyIcon.Sprite,
                    IconColor = ModEntry.Energies[(int)energyType].GetColor(),
                    TitleColor = ModEntry.Energies[(int)energyType].GetColor(),
                    Title = ModEntry.Instance.Localizations.Localize(["energy", energyType.ToString(), "name"]),
                    Description = ModEntry.Instance.Localizations.Localize(["energy", energyType.ToString(), "description"]),
                });
            }

            private static void AStatus_GetIcon_Postfix(AStatus __instance, ref Icon? __result)
            {
                if (!ModEntry.Instance.Helper.ModData.TryGetModData(__instance, "moddedEnergy", out Energy energyType))
                    return;

                __result = new(
                    path: ModEntry.Instance.GenericEnergyIcon.Sprite,
                    number: __instance.mode == AStatusMode.Set ? null : __instance.statusAmount,
                    color: ModEntry.Energies[(int)energyType].GetColor()
                );
            }

            private static bool AStatus_Begin_Prefix(AStatus __instance, Combat c)
            {
                if (!ModEntry.Instance.Helper.ModData.TryGetModData(__instance, "moddedEnergy", out Energy energyType))
                    return true;

                int currentAmount = c.GetEnergy().GetValueOrDefault(energyType, 0);
                int newAmount = __instance.mode switch
                {
                    AStatusMode.Set => __instance.statusAmount,
                    AStatusMode.Add => currentAmount + __instance.statusAmount,
                    AStatusMode.Mult => currentAmount * __instance.statusAmount,
                    _ => currentAmount
                };

                __instance.timer = 0;
                c.QueueImmediate(new AModdedEnergy { changeAmount = newAmount - currentAmount, energyToChange = energyType });
                return false;
            }
        }
    }

    public IEnergyInfoApi GetEnergyInfo(Energy energy)
    {
        return ModEntry.Energies[(int)energy];
    }

    public IModdedEnergyTooltipInfo ModdedEnergyTooltipInfo => new ModdedEnergyTooltipInfoApi();

    public sealed class ModdedEnergyTooltipInfoApi : IModdedEnergyTooltipInfo
    {
        public string ResourceCostDescription(Energy energy, int amount)
        {
            EnergyInfo energyInfo = ModEntry.Energies[(int)energy];
            string descriptionFormat = ModEntry.Instance.Localizations.Localize(["resourceCost", "energy", "description"]);
            string description = string.Format(descriptionFormat, amount, energyInfo.GetColor(), energy.ToString().ToUpper());
            return description;
        }

        public string ResourceCostName(Energy energy)
        {
            EnergyInfo energyInfo = ModEntry.Energies[(int)energy];
            string nameFormat = ModEntry.Instance.Localizations.Localize(["resourceCost", "energy", "name"]);
            string name = string.Format(nameFormat, energy.ToString().ToUpper());
            return name;
        }

        public Spr ResourceCostSatisfiedIcon(Energy energy, int amount)
        {
            return ModEntry.Instance.KokoroApi.ActionCosts.GetResourceCostIcons(new ModdedEnergyResource { EnergyType = energy }, amount)[0].CostSatisfiedIcon;
        }

        public Spr ResourceCostUnsatisfiedIcon(Energy energy, int amount)
        {
            return ModEntry.Instance.KokoroApi.ActionCosts.GetResourceCostIcons(new ModdedEnergyResource { EnergyType = energy }, amount)[0].CostUnsatisfiedIcon;
        }
    }

    public void RegisterInUseEnergyHook(IInUseEnergyHook hook)
    {
        InUseEnergyHooks.Add(hook);
    }

    public void UnregisterInUseEnergyHook(IInUseEnergyHook hook)
    {
        InUseEnergyHooks.Remove(hook);
    }
}

internal sealed class DefaultSetModdedEnergyCostBaseHook : ISetModdedEnergyCostBaseHook
{
    public required IDictionary<Energy, int> BaseModdedEnergyCost;
    public IDictionary<Energy, int> GetModdedEnergyCostBase(State s)
        => BaseModdedEnergyCost;
}
