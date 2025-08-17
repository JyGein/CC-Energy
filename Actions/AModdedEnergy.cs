// Decompiled with JetBrains decompiler
// Type: AEnergy
// Assembly: CobaltCore, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B6C8BC24-5604-4FA9-930A-6C74A92FF0EB
// Assembly location: CobaltCore.dll inside C:\Program Files (x86)\Steam\steamapps\common\Cobalt Core\CobaltCore.exe)

using CCEnergy;
using CCEnergy.External;
using FMOD;
using System;
using System.Collections.Generic;
using static CCEnergy.IEnergyApi;

namespace CCEnergy.Actions;

#nullable enable
public class AModdedEnergy : CardAction, IAModdedEnergy
{
    public int changeAmount { get; set; }
    public Energy energyToChange { get; set; }
    public CardAction AsCardAction => this;
    
    public override void Begin(G g, State s, Combat c)
    {
        Dictionary<Energy, int> energyBlock = ModEntry.EnergyApi.GetCombatModdedEnergy(c);
        energyBlock[energyToChange] += changeAmount;
        ModEntry.EnergyApi.SetCombatModdedEnergy(c, energyBlock);
        Audio.Play(new GUID?(this.changeAmount > 0 ? FSPRO.Event.Status_PowerUp : FSPRO.Event.Status_PowerDown));
        if (this.changeAmount < 0)
        {
            c.pulseEnergyBad = 0.5;
        }
        else
        {
            if (this.changeAmount <= 0)
            return;
            c.pulseEnergyGood = 0.5;
        }
    }
    
    public override List<Tooltip> GetTooltips(State s)
    {
        List<Tooltip> tooltips = new List<Tooltip>();
        tooltips.Add((Tooltip) new TTGlossary(this.changeAmount > 0 ? "action.gainEnergy" : "action.loseEnergy", new object[1]//NEW TOOLTIP NEEDED
        {
            (object) this.changeAmount
        }));
        return tooltips;
    }
    
    public override Icon? GetIcon(State s)
    {
        return new Icon?(new Icon(StableSpr.icons_energy, new int?(this.changeAmount), Colors.textMain));//SHOULD CHANGE TO REPRESENT THE SPECIFIC ENERGY THAT'S BEING CHANGED
    }
}
