using CCEnergy.External;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCEnergy;

public partial interface IEnergyApi
{
    /// <summary>
    /// Energy!
    /// </summary>
    public enum Energy
    {
        Revenge,
        Calm,
        Thermal
    }
    /// <summary>
    /// Sets a cards base cost, ideally in the card's constructor.
    /// </summary>
    /// <param name="card">This card.</param>
    /// <param name="energyBlock">The base cost of the card.</param>
    void SetModdedEnergyCostBase(Card card, Dictionary<Energy, int> energyBlock);
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
    Dictionary<Energy, int> GetModdedEnergyBaseCost(Card card, State s);
    /// <summary>
    /// Gets a card's modded energy cost.
    /// </summary>
    /// <param name="card">The card.</param>
    /// <param name="s">The game state.</param>
    /// <returns>A dictionary of the card's modded energy cost.</returns>
    Dictionary<Energy, int> GetModdedEnergyCost(Card card, State s);
    /// <summary>
    /// A hook related to a cards self-defined base cost.
    /// </summary>
    public interface ISetModdedEnergyCostBaseHook : IKokoroApi.IV2.IKokoroV2ApiHook
    {
        /// <summary>
        /// Gets this card's base cost.
        /// </summary>
        /// <param name="s">The game state.</param>
        /// <returns>A dictionary of the card's base modded energy cost.</returns>
        Dictionary<Energy, int> GetModdedEnergyCostBase(State s);
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
    void SetCardModdedEnergyDiscount(Card card, Dictionary<Energy, int> discountEnergyBlock);
    /// <summary>
    /// Gets a card's modded energy discounts.
    /// </summary>
    /// <param name="card"></param>
    /// <returns>A dictionary of each energy and the amount it's discounted.</returns>
    Dictionary<Energy, int> GetCardModdedEnergyDiscounts(Card card);
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
    public interface IModdedEnergyCostOverrideHook : IKokoroApi.IV2.IKokoroV2ApiHook
    {
        /// <summary>
        /// Potentially overrides a card's cost.
        /// </summary>
        /// <param name="card">The card.</param>
        /// <param name="s">The game state.</param>
        /// <param name="energyBlock">The card's cost with discounts.</param>
        /// <returns>A dictionary of the card's modded energy costs overridden.</returns>
        Dictionary<Energy, int> GetModdedEnergyCostOveridden(Card card, State s, Dictionary<Energy, int> energyBlock);
    }
    /// <summary>
    /// The AModdedEnergy CardAction
    /// </summary>
    IAModdedEnergy AModdedEnergy { get; }
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
    void SetCombatModdedEnergy(Combat c, Dictionary<Energy, int> energyBlock);
    /// <summary>
    /// Gets the current modded energy from this combat.
    /// </summary>
    /// <param name="c">The current combat.</param>
    /// <returns>A dictionary of the combat's current modded energy.</returns>
    Dictionary<Energy, int> GetCombatModdedEnergy(Combat c);
}
