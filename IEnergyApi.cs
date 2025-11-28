using CCEnergy.External;
using Nickel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CCEnergy.External.IKokoroApi.IV2;

namespace CCEnergy;

public partial interface IEnergyApi
{
    /// <summary>
    /// Energy!
    /// </summary>
    public enum Energy
    {
        Revenge,
        Residual,
        Thermal,
        Kinetic,
        Core,
        Sacrifice,
        Charged
    }
    /// <summary>
    /// Sets a cards base cost, ideally in the card's constructor.
    /// </summary>
    /// <param name="card">This card.</param>
    /// <param name="energyBlock">The base cost of the card.</param>
    void SetModdedEnergyCostBase(Card card, IDictionary<Energy, int> energyBlock);
    /// <summary>
    /// Sets a cards base cost with a hook to dynamically change the cost (ex: upgrades).
    /// </summary>
    /// <param name="card">This card.</param>
    /// <param name="hook">The hook.</param>
	void SetModdedEnergyCostBaseHook(Card card, ISetModdedEnergyCostBaseHook hook);
    /// <summary>
    /// Gets the base modded energy cost of a card.
    /// </summary>
    /// <param name="card">The card.</param>
    /// <returns>A dictionary of the card's base modded energy cost.</returns>
    IDictionary<Energy, int> GetModdedEnergyBaseCost(Card card, State s);
    /// <summary>
    /// Gets a card's modded energy cost.
    /// </summary>
    /// <param name="card">The card.</param>
    /// <param name="s">The game state.</param>
    /// <returns>A dictionary of the card's modded energy cost.</returns>
    IDictionary<Energy, int> GetModdedEnergyCost(Card card, State s);
    /// <summary>
    /// A hook related to a cards self-defined base cost.
    /// </summary>
    public interface ISetModdedEnergyCostBaseHook
    {
        /// <summary>
        /// Gets this card's base cost.
        /// </summary>
        /// <param name="s">The game state.</param>
        /// <returns>A dictionary of the card's base modded energy cost.</returns>
        IDictionary<Energy, int> GetModdedEnergyCostBase(State s);
    }
    /// <summary>
    /// Adds to the discount of an energy type on a card.
    /// </summary>
    /// <param name="card">The Card to discount.</param>
    /// <param name="energyToDiscount">The Energy type to discount.</param>
    /// <param name="amountToDiscount">The amount of that energy to discount.</param>
    void AddCardModdedEnergyDiscount(Card card, Energy energyToDiscount, int amountToDiscount);
    /// <summary>
    /// Set a card's modded energy discount.
    /// </summary>
    /// <param name="card">The card to discount.</param>
    /// <param name="discountEnergyBlock">The discount to be set.</param>
    void SetCardModdedEnergyDiscount(Card card, IDictionary<Energy, int> discountEnergyBlock);
    /// <summary>
    /// Gets a card's modded energy discounts.
    /// </summary>
    /// <param name="card"></param>
    /// <returns>A dictionary of each energy and the amount it's discounted.</returns>
    IDictionary<Energy, int> GetCardModdedEnergyDiscounts(Card card);
    /// <summary>
    /// Registers a hook to override card's modded energy cost. 
    /// </summary>
    /// <param name="hook">The hook.</param>
    void RegisterModdedEnergyCostOverrideHook(IModdedEnergyCostOverrideHook hook);
    /// <summary>
    /// Unregisters a hook that overrides a card's modded energy cost. 
    /// </summary>
    /// <param name="hook">The hook.</param>
    void UnregisterModdedEnergyCostOverrideHook(IModdedEnergyCostOverrideHook hook);
    /// <summary>
    /// A hook to override a card's modded energy cost.
    /// </summary>
    public interface IModdedEnergyCostOverrideHook
    {
        /// <summary>
        /// Potentially overrides a card's cost.
        /// </summary>
        /// <param name="card">The card.</param>
        /// <param name="s">The game state.</param>
        /// <param name="energyBlock">The card's cost with discounts.</param>
        /// <returns>A dictionary of the card's modded energy costs overridden.</returns>
        IDictionary<Energy, int> GetModdedEnergyCostOveridden(Card card, State s, IDictionary<Energy, int> energyBlock);
    }
    /// <summary>
    /// A new AModdedEnergy action.
    /// </summary>
    IAModdedEnergy AModdedEnergy { get; }
    /// <summary>
    /// The AModdedEnergy CardAction.
    /// </summary>
    interface IAModdedEnergy : IKokoroApi.IV2.ICardAction<CardAction>
	{
        /// <summary>
        /// The amount of energy to add or remove.
        /// </summary>
		public int changeAmount { get; set; }
        /// <summary>
        /// The energy type to change.
        /// </summary>
        public Energy energyToChange { get; set; }
    }
    /// <summary>
    /// Sets the current modded energy for this combat. Should rarely be used instead of a AModdedEnergy Action.
    /// </summary>
    /// <param name="c">The current combat.</param>
    /// <param name="energyBlock">The energy</param>
    void SetCombatModdedEnergy(Combat c, IDictionary<Energy, int> energyBlock);
    /// <summary>
    /// Gets the current modded energy from this combat.
    /// </summary>
    /// <param name="c">The current combat.</param>
    /// <returns>A dictionary of the combat's current modded energy.</returns>
    IDictionary<Energy, int> GetCombatModdedEnergy(Combat c);
    /// <summary>
    /// Gets the energy types that are currently in use between the deck, hand, discard, and exhaust.
    /// </summary>
    /// <param name="c">The current combat.</param>
    /// <param name="s">The game state.</param>
    /// <returns>A List of energy types that are currently in use between the deck, hand, discard, and exhaust.</returns>
    IList<Energy> GetInUseEnergiesInCombat(State s, Combat c);
    /// <summary>
    /// Gets the energy types that are currently in use between the deck, hand, discard, and exhaust.
    /// </summary>
    /// <param name="c">The current combat.</param>
    /// <param name="s">The game state.</param>
    /// <returns>A List of energy types that are currently in use between the deck, hand, discard, and exhaust.</returns>
    IList<Energy> GetInUseEnergiesOutOfCombat(State s);
    /// <summary>
    /// Registers a hook to override the modded turn energy. 
    /// </summary>
    /// <param name="hook">The hook.</param>
    void RegisterModdedTurnEnergyOverrideHook(IModdedTurnEnergyOverrideHook hook);
    /// <summary>
    /// Unregisters a hook that overrides the modded turn energy. 
    /// </summary>
    /// <param name="hook">The hook.</param>
    void UnregisterModdedTurnEnergyOverrideHook(IModdedTurnEnergyOverrideHook hook);
    /// <summary>
    /// A hook to override the modded turn energy.
    /// </summary>
    public interface IModdedTurnEnergyOverrideHook
    {
        /// <summary>
        /// Potentially overrides the modded turn energy.
        /// </summary>
        /// <param name="c">The current combat.</param>
        /// <param name="s">The game state.</param>
        /// <param name="energyBlock">The current energy for this turn.</param>
        /// <returns>A dictionary of this turn's modded energy overridden.</returns>
        IDictionary<Energy, int> GetModdedEnergyCostOveridden(Combat c, State s, IDictionary<Energy, int> energyBlock);
    }
    /// <summary>
    /// Gets the Charged Energy Api
    /// </summary>
    /// <returns>The Charged Energy Api</returns>
    public IChargedEnergyApi ChargedEnergyApi { get; }
    /// <summary>
    /// Holds hooks specific to Charged Energy
    /// </summary>
    public interface IChargedEnergyApi
    {
        /// <summary>
        /// Registers a hook to override the maximum amount of Charged Energy gained per turn.
        /// </summary>
        /// <param name="hook"></param>
        void RegisterChargedEnergyMaximumOverrideHook(IChargedEnergyMaximumOverrideHook hook);
        /// <summary>
        /// Unregisters a hook to override the maximum amount of Charged Energy gained per turn.
        /// </summary>
        /// <param name="hook"></param>
        void UnRegisterChargedEnergyMaximumOverrideHook(IChargedEnergyMaximumOverrideHook hook);
        /// <summary>
        /// A hook to override the maximum amount of Charged Energy gained per turn.
        /// </summary>
        public interface IChargedEnergyMaximumOverrideHook
        {
            /// <summary>
            /// Potentially overrides the maximum amount of Charged Energy gained per turn.
            /// </summary>
            /// <param name="c"></param>
            /// <param name="s"></param>
            /// <param name="energyBlock"></param>
            /// <returns></returns>
            int GetChargedEnergyMaximumOverridden(Combat c, State s, int currentMaximum);
        }
        /// <summary>
        /// A status that increases your max Charged energy per turn, 1 for each stack.
        /// </summary>
        public IStatusEntry MaxChargedEnergy { get; }
    }
    /// <inheritdoc cref="IModdedEnergyAsStatusApi"/>
    IModdedEnergyAsStatusApi ModdedEnergyAsStatus { get; }

    /// <summary>
    /// Allows treating <see cref="Energy"/> as if it was a status for <see cref="AStatus"/> and <see cref="AVariableHint"/>.
    /// </summary>
    public interface IModdedEnergyAsStatusApi
    {
        /// <summary>
        /// Casts the action to <see cref="IVariableHint"/>, if it is one.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <returns>The <see cref="IVariableHint"/>, if the given action is one, or <c>null</c> otherwise.</returns>
        IVariableHint? AsVariableHint(AVariableHint action);

        /// <summary>
        /// Creates a new variable hint action for the current amount of energy.
        /// </summary>
        /// <param name="tooltipOverride">An override value for the current amount of energy, used in tooltips. See <see cref="IVariableHint.TooltipOverride"/>.</param>
        /// <returns>The new variable hint action.</returns>
        IVariableHint MakeVariableHint(Energy energy, int? tooltipOverride = null);

        /// <summary>
        /// Casts the action to <see cref="IStatusAction"/>, if it is one.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <returns>The <see cref="IStatusAction"/>, if the given action is one, or <c>null</c> otherwise.</returns>
        IStatusAction? AsStatusAction(AStatus action);

        /// <summary>
        /// Creates a new <see cref="AStatus"/> action that changes the current amount of energy.
        /// </summary>
        /// <param name="amount">A modifier amount. See <see cref="AStatus.statusAmount"/>.</param>
        /// <param name="mode">A modifier mode. See <see cref="AStatus.mode"/>.</param>
        /// <returns></returns>
        IStatusAction MakeStatusAction(Energy energy, int amount, AStatusMode mode = AStatusMode.Add);

        /// <summary>
        /// A variable hint action for the current amount of energy.
        /// </summary>
        public interface IVariableHint : ICardAction<AVariableHint>
        {
            /// <summary>
            /// The type of energy.
            /// </summary>
            Energy EnergyType { get; set; }
            /// <summary>
            /// An override value for the current amount of energy, used in tooltips.
            /// </summary>
            /// <remarks>
            /// This can be used to correct the amount of energy if a card does not cost 0, or otherwise changes the amount of energy between its actions.
            /// </remarks>
            int? TooltipOverride { get; set; }

            /// <summary>
            /// Sets <see cref="TooltipOverride"/>.
            /// </summary>
            /// <param name="value">The new value.</param>
            /// <returns>This object after the change.</returns>
            IVariableHint SetTooltipOverride(int? value);
        }

        /// <summary>
        /// An <see cref="AStatus"/> wrapper action that changes the current amount of energy.
        /// </summary>
        public interface IStatusAction : ICardAction<AStatus>;
    }

    /// <inheritdoc cref="IEnergyInfoApi"/>
    IEnergyInfoApi GetEnergyInfo(Energy energy);

    /// <summary>
    /// An <see cref="Energy"/>'s information.
    /// </summary>
    public interface IEnergyInfoApi
    {
        UK GetUK();
        int GetTurnEnergy(State state, Combat c);
        Color GetColor();
        void DoPatches();
    }

    /// <inheritdoc cref="IModdedEnergyTooltipInfo"/>
    IModdedEnergyTooltipInfo ModdedEnergyTooltipInfo { get; }

    /// <summary>
    /// Useful localizations for tooltips.
    /// </summary>
    public interface IModdedEnergyTooltipInfo
    {
        string ResourceCostName(Energy energy);
        string ResourceCostDescription(Energy energy, int amount);
        Spr ResourceCostSatisfiedIcon(Energy energy, int amount);
        Spr ResourceCostUnsatisfiedIcon(Energy energy, int amount);
    }

    void RegisterInUseEnergyHook(IInUseEnergyHook hook);
    void UnregisterInUseEnergyHook(IInUseEnergyHook hook);
    public interface IInUseEnergyHook
    {
        IReadOnlyList<Energy> MoreEnergiesInUse(State s, Combat c);
        IReadOnlyList<Energy> MoreEnergiesInUseOutOfCombat(State s);
    }
}
