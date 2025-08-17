using CCEnergy.Actions;
using CCEnergy.Energies;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Input;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using static CCEnergy.IEnergyApi;

namespace CCEnergy;

internal sealed class Patches
{
    public Patches(Harmony harmony)
    {
        ModEntry.Instance.Harmony.Patch(
            original: AccessTools.DeclaredMethod(typeof(AAfterPlayerTurn), nameof(AAfterPlayerTurn.Begin)),
            postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAfterPlayerTurn_Begin_Postfix))
        );
        ModEntry.Instance.Harmony.Patch(
            original: AccessTools.DeclaredMethod(typeof(AStartPlayerTurn), nameof(AStartPlayerTurn.Begin)),
            postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AStartPlayerTurn_Begin_Postfix))
        );
        ModEntry.Instance.Harmony.Patch(
            original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.Render)),
            transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_Render_Transpiler))
        );
        ModEntry.Instance.Harmony.Patch(
            original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.Make)),
            postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_Make_Postfix))
        );
        ModEntry.Instance.Harmony.Patch(
            original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.TryPlayCard)),
            transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryPlayCard_Transpiler))
        );
        ModEntry.Instance.Harmony.Patch(
            original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.RenderCards)),
            transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderCards_Transpiler))
        );
        ModEntry.Instance.Harmony.Patch(
            original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.RenderEndTurn)),
            transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderEndTurn_Transpiler))
        );
        ModEntry.Instance.Harmony.Patch(
            original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.RenderEnergy)),
            //prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderEnergy_Prefix)),
            //postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderEnergy_Postfix)),
            transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderEnergy_Transpiler))
        );
        ModEntry.Instance.Harmony.Patch(
            original: AccessTools.DeclaredMethod(typeof(EditorShortcuts), nameof(EditorShortcuts.DebugKeys)),
            postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(EditorShortcuts_DebugKeys_Postfix)),
            transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(EditorShortcuts_DebugKeys_Transpiler))
        );
    }

    private static void AAfterPlayerTurn_Begin_Postfix(Combat c)
    {
        Dictionary<Energy, int> energy = c.GetEnergy();
        foreach (Energy e in energy.Keys) energy[e] = 0;
        c.SetEnergy(energy);
    }

    private static void AStartPlayerTurn_Begin_Postfix(G g, Combat c)
    {
        if (c.turn == 1) return;
        Dictionary<Energy, int> energy = c.GetEnergy();
        foreach (Energy e in energy.Keys) energy[e] += ModEntry.Energies[(int)e].GetTurnEnergy(g.state, c);
        energy = ModEntry.EnergyApi.GetOverriddenTurnEnergy(c, g.state, energy).ToDictionary();
        c.SetEnergy(energy);
    }

    private static IEnumerable<CodeInstruction> Card_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
    {
        try
        {
            return new SequenceBlockMatcher<CodeInstruction>(instructions)
                .Find(
                    ILMatches.AnyLdarg,
                    ILMatches.AnyCall,
                    ILMatches.Ldfld<Deck>(),
                    ILMatches.LdcI4((int)Deck.sasha),
                    ILMatches.Beq
                )
                .Find(
                    ILMatches.Call(nameof(Draw.Sprite))
                )
                .Insert(
                    SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Ldloc_S, 11),
                    new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(DrawEnergyBGs)))
                )
                .Find(
                    ILMatches.Call(nameof(BigNumbers.Render))
                )
                .Insert(
                    SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Ldloc_S, 11),
                    new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(DrawEnergyCosts)))
                )
                .AllElements();
        }
        catch (Exception ex)
        {
            ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.GetDisplayName(@long: false), ex);
            return instructions;
        }
    }

    private static void DrawEnergyBGs(Card card, G g, Vec v)
    {
        Dictionary<Energy, int> moddedEnergyCardCost = ModEntry.EnergyApi.GetModdedEnergyCost(card, g.state).ToDictionary();
        int count = 0;
        foreach (Energy energy in Enum.GetValues(typeof(Energy)))
        {
            if (moddedEnergyCardCost.TryGetValue(energy, out int cost) && cost > 0)
            {
                count++;
                Draw.Sprite(ModEntry.Instance.CardEnergyBGExtension.Sprite, v.x + 0.0 + (11.0 * count), v.y + 16.0, color: ModEntry.Energies[(int)energy].GetColor());
            }
        }
    }

    private static void DrawEnergyCosts(State state, Card card, G g, Vec v)
    {
        Dictionary<Energy, int> moddedEnergyCardCost = ModEntry.EnergyApi.GetModdedEnergyCost(card, g.state).ToDictionary();
        int count = 0;
        foreach(Energy energy in Enum.GetValues(typeof(Energy)))
        {
            if (moddedEnergyCardCost.TryGetValue(energy, out int cost) && cost > 0)
            {
                count++;
                bool flag1 = !(state.route is Combat c) || c.energy >= cost;
                BigNumbers.Render(cost, v.x + 3.0 + (11.0 * count), v.y + 18.0, flag1 ? Color.Lerp(ModEntry.Energies[(int)energy].GetColor(), Colors.white, 0.6) : Color.Lerp(Colors.textMain.fadeAlpha(0.55), Colors.redd, card.shakeNoAnim));
                //ADD UNPLAYABLE ICON OVER THE NUMBER IF UNPLAYABLE
            }
        }
    }

    private static void Combat_Make_Postfix(ref Combat __result, State s)
    {
        Dictionary<Energy, int> energy = new();
        foreach (Energy e in Enum.GetValues(typeof(Energy))) energy[e] = ModEntry.Energies[(int)e].GetTurnEnergy(s, __result);
        energy = ModEntry.EnergyApi.GetOverriddenTurnEnergy(__result, s, energy).ToDictionary();
        __result.SetEnergy(energy);
    }

    private static IEnumerable<CodeInstruction> Combat_TryPlayCard_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
    {
        try
        {
            return new SequenceBlockMatcher<CodeInstruction>(instructions)
                .Find(
                    ILMatches.AnyLdloc,
                    ILMatches.Ldarg(0),
                    ILMatches.Ldfld<int>(),
                    ILMatches.Bgt.GetBranchTarget(out StructRef<Label> label)
                )
                .Insert(
                    SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Ldarg_2),
                    new CodeInstruction(OpCodes.Ldarg_3),
                    new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CanPayCostState))),
                    new CodeInstruction(OpCodes.Brfalse, label.Value)
                )
                .Find(
                    ILMatches.Instruction(OpCodes.Sub),
                    ILMatches.Stfld<int>()
                )
                .Insert(
                    SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Ldarg_2),
                    new CodeInstruction(OpCodes.Ldarg_3),
                    new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(PayCost)))
                )
                .AllElements();
        }
        catch (Exception ex)
        {
            ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.GetDisplayName(@long: false), ex);
            return instructions;
        }
    }

    private static bool CanPayCostState(Combat c, State s, Card card, bool playNoMatterWhatForFree)
    {
        if (playNoMatterWhatForFree) return true;
        Dictionary<Energy, int> cardCost = ModEntry.EnergyApi.GetModdedEnergyCost(card, s).ToDictionary();
        Dictionary<Energy, int> currentEnergy = c.GetEnergy();
        foreach(Energy energy in cardCost.Keys)
        {
            if (currentEnergy[energy] < cardCost[energy]) return false;
        }
        return true;
    }

    private static void PayCost(Combat c, State s, Card card, bool playNoMatterWhatForFree)
    {
        ModEntry.EnergyApi.SetCardModdedEnergyDiscount(card, new Dictionary<Energy, int>());
        if (playNoMatterWhatForFree) return;
        Dictionary<Energy, int> cardCost = ModEntry.EnergyApi.GetModdedEnergyCost(card, s).ToDictionary();
        Dictionary<Energy, int> currentEnergy = c.GetEnergy();
        foreach (Energy energy in cardCost.Keys)
        {
            currentEnergy[energy] -= cardCost[energy];
        }
        c.SetEnergy(currentEnergy);
        return;
    }

    private static IEnumerable<CodeInstruction> Combat_RenderCards_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
    {
        try
        {
            return new SequenceBlockMatcher<CodeInstruction>(instructions)
                .Find(
                    ILMatches.AnyLdloc,
                    ILMatches.AnyLdarg,
                    ILMatches.Ldfld<State>(),
                    ILMatches.AnyCall,
                    ILMatches.AnyLdarg,
                    ILMatches.Ldfld<int>(),
                    ILMatches.AnyBgt.GetBranchTarget(out StructRef<Label> label)
                )
                .Insert(
                    SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Ldloc_S, 7),
                    new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CanPayCostG))),
                    new CodeInstruction(OpCodes.Brfalse, label.Value)
                )
                .AllElements();
        }
        catch (Exception ex)
        {
            ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.GetDisplayName(@long: false), ex);
            return instructions;
        }
    }

    private static bool CanPayCostG(Combat c, G g, Card card)
    {
        Dictionary<Energy, int> cardCost = ModEntry.EnergyApi.GetModdedEnergyCost(card, g.state).ToDictionary();
        Dictionary<Energy, int> currentEnergy = c.GetEnergy();
        foreach (Energy energy in cardCost.Keys)
        {
            if (currentEnergy[energy] < cardCost[energy]) return false;
        }
        return true;
    }

    private static IEnumerable<CodeInstruction> Combat_RenderEndTurn_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod, ILGenerator il)
    {
        try
        {
            /*new SequenceBlockMatcher<CodeInstruction>(instructions)
                .Find(
                    ILMatches.Ldarg(0),
                    ILMatches.Ldfld<int>(),
                    ILMatches.LdcI4(0),
                    ILMatches.AnyBgt
                )
                .Find(
                    ILMatches.Ldarg(0)
                )
                .Find(
                    ILMatches.Ldarg(0).CreateLabel(il, out Label SkipLabel)
                );*/
            return new SequenceBlockMatcher<CodeInstruction>(instructions)
                .Find(
                    ILMatches.Ldarg(0),
                    ILMatches.Ldfld<int>(),
                    ILMatches.LdcI4(0),
                    ILMatches.AnyBgt.GetBranchTarget(out StructRef<Label> EndLabel)
                )
                .Insert(
                    SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CanPayAnyCard))),
                    new CodeInstruction(OpCodes.Brfalse, EndLabel.Value)/*,
                    new CodeInstruction(OpCodes.Br, SkipLabel)*/
                )
                .Find(
                    ILMatches.Ldarg(0),
                    ILMatches.Ldfld<List<Card>>(),
                    ILMatches.AnyLdloc,
                    ILMatches.Instruction(OpCodes.Ldftn),
                    ILMatches.Instruction(OpCodes.Newobj),
                    ILMatches.Call(nameof(Enumerable.Any)),
                    ILMatches.Brtrue
                )
                .Remove()
                .AllElements();
        }
        catch (Exception ex)
        {
            ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.GetDisplayName(@long: false), ex);
            return instructions;
        }
    }

    private static bool CanPayAnyCard(Combat c, G g) //RETURN TRUE IF NONE OF THE CARDS CAN BE PLAYED
    {
        foreach(Card card in c.hand)
        {
            if (card.GetCurrentCost(g.state) > 0) continue;
            Dictionary<Energy, int> cardCost = ModEntry.EnergyApi.GetModdedEnergyCost(card, g.state).ToDictionary();
            Dictionary<Energy, int> currentEnergy = c.GetEnergy();
            bool cardUnplayable = false;
            foreach (Energy energy in cardCost.Keys)
            {
                if (currentEnergy[energy] < cardCost[energy]) cardUnplayable = true;
            }
            if (cardUnplayable) continue;
            return false;
        }
        return true;
    }
    private static bool Combat_RenderEnergy_Prefix(Combat __instance, G g)
    {
        return true;
        int count = 0;
        Dictionary<Energy, int> energyBlock = __instance.GetEnergy();
        List<Energy> inUseEnergy = ModEntry.EnergyApi.GetInUseEnergies(__instance, g.state);
        if (inUseEnergy.Count < 3) return true;
        Box box = g.Push(StableUK.combat_energy, new Rect(Mutil.AnimHelper(__instance.introTimer, -90.0, 0.0, 360.0, 0.35), w: 19.0, h: 19.0) + Combat.energyPos - new Vec(4, 12) + new Vec(21 * (count % 2), 21 * (count / 2)), gamepadUntargetable: true);
        Vec xy = box.rect.xy;
        if (box.IsHover())
            g.tooltips.AddGlossary(xy + new Vec(21.0), "combat.energyCounter");
        Color energyColor = Colors.textMain;
        Draw.Sprite(ModEntry.Instance.CombatMiniEnergy.Sprite, xy.x - 1.0, xy.y - 1.0, color: energyColor);
        Color textColor = __instance.energy > 0 ? energyColor : Colors.redd;
        double pulse = Mutil.GetPulse(g.state.time, __instance.pulseEnergyBad, 0.5) / 2;
        if (__instance.pulseEnergyBad > 0.0)
            textColor = Color.Lerp(textColor, Colors.pulseEnergyBadText, __instance.pulseEnergyBad / 0.5);
        if (__instance.pulseEnergyGood > 0.0)
            textColor = Color.Lerp(textColor, Colors.pulseEnergyGoodText, __instance.pulseEnergyGood / 0.5);
        Color glowColor = __instance.pulseEnergyGood > __instance.pulseEnergyBad ? Colors.pulseEnergyGoodGlow : Colors.pulseEnergyBadGlow;
        Glow.Draw(xy + new Vec(10.0, 10.0), 50.0, glowColor.gain(Math.Max(__instance.pulseEnergyGood, __instance.pulseEnergyBad) / 0.5));
        string str = DB.IntStringCache(__instance.energy);
        double x = xy.x + 9.0 + pulse;
        double y = xy.y + 5.0;
        Draw.Text(str, x, y, DB.thicket, textColor, align: daisyowl.text.TAlign.Center, dontSubstituteLocFont: true);
        g.Pop();
        count++;
        foreach (Energy energy in inUseEnergy)
        {
            EnergyInfo energyInfo = ModEntry.Energies[(int)energy];
            Box box1 = g.Push((UIKey)energyInfo.GetUK(), new Rect(Mutil.AnimHelper(__instance.introTimer, -90.0, 0.0, 360.0, 0.35), w: 19.0, h: 19.0) + Combat.energyPos - new Vec(4, 12) + new Vec(21 * (count % 2), 21 * (count / 2)), gamepadUntargetable: true);
            Vec xy1 = box1.rect.xy;
            if (box1.IsHover()) g.tooltips.Add(xy1 + new Vec(21.0),
                new GlossaryTooltip($"{energy}ENERGY")
                {
                    Icon = ModEntry.Instance.GenericEnergyIcon.Sprite,
                    IconColor = energyInfo.GetColor(),
                    Title = ModEntry.Instance.Localizations.Localize(["energy", energy.ToString(), "name"]),
                    TitleColor = energyInfo.GetColor(),
                    Description = ModEntry.Instance.Localizations.Localize(["energy", energy.ToString(), "description"])
                });
            Color energyColor1 = energyInfo.GetColor();
            Draw.Sprite(ModEntry.Instance.CombatMiniEnergy.Sprite, xy1.x - 1.0, xy1.y - 1.0, color: energyColor1);
            Color textColor1 = energyBlock[energy] > 0 ? energyColor1 : Colors.redd;
            double pulse1 = Mutil.GetPulse(g.state.time, __instance.pulseEnergyBad, 0.5) / 2;
            if (__instance.pulseEnergyBad > 0.0)
                textColor1 = Color.Lerp(textColor1, Colors.pulseEnergyBadText, __instance.pulseEnergyBad / 0.5);
            if (__instance.pulseEnergyGood > 0.0)
                textColor1 = Color.Lerp(textColor1, Colors.pulseEnergyGoodText, __instance.pulseEnergyGood / 0.5);
            Color glowColor1 = __instance.pulseEnergyGood > __instance.pulseEnergyBad ? Colors.pulseEnergyGoodGlow : Colors.pulseEnergyBadGlow;
            Glow.Draw(xy1 + new Vec(10.0, 10.0), 50.0, glowColor1.gain(Math.Max(__instance.pulseEnergyGood, __instance.pulseEnergyBad) / 0.5));
            string str1 = DB.IntStringCache(energyBlock[energy]);
            double x1 = xy1.x + 9.0 + pulse1;
            double y1 = xy1.y + 5.0;
            Draw.Text(str1, x1, y1, DB.thicket, textColor1, align: daisyowl.text.TAlign.Center, dontSubstituteLocFont: true);
            g.Pop();
            count++;
        }
        return false;
    }

    static readonly List<(Vec, double)> NumberPositions = [
        (new (0, -5), -7),
        (new (13, -8), -3),
        (new (26, -5), 0),
        ];
    private static void Combat_RenderEnergy_Postfix(Combat __instance, G g)
    {
        int count = 0;
        Dictionary<Energy, int> energyBlock = __instance.GetEnergy();
        List<Energy> inUseEnergy = ModEntry.EnergyApi.GetInUseEnergies(__instance, g.state);
        // if (inUseEnergy.Count > 2) return;
        foreach (Energy energy in inUseEnergy)
        {
            EnergyInfo energyInfo = ModEntry.Energies[(int)energy];
            Box box = g.Push((UIKey)energyInfo.GetUK(), new Rect?(new Rect(Mutil.AnimHelper(__instance.introTimer, -90.0, 0.0, 360.0, 0.35), w: 30.0, h: 29.0) + Combat.energyPos - new Vec(0, 2)), gamepadUntargetable: true);
            Vec xy = box.rect.xy;
            if (box.IsHover()) g.tooltips.Add(xy + new Vec(31),
                new GlossaryTooltip($"{energy}ENERGY")
                {
                    Icon = ModEntry.Instance.GenericEnergyIcon.Sprite,
                    IconColor = energyInfo.GetColor(),
                    Title = ModEntry.Instance.Localizations.Localize(["energy", energy.ToString(), "name"]),
                    TitleColor = energyInfo.GetColor(),
                    Description = ModEntry.Instance.Localizations.Localize(["energy", energy.ToString(), "description"])
            });
            Color energyColor = energyInfo.GetColor();
            Draw.Sprite(ModEntry.Instance.EnergyLights[count].Sprite, xy.x - 1.0, xy.y - 1.0, color: energyColor);
            Color textColor = energyBlock[energy] > 0 ? Color.Lerp(energyColor, Colors.textMain, 0.5) : Colors.redd;
            double pulse = Mutil.GetPulse(g.state.time, __instance.pulseEnergyBad, 0.5) / 2;
            if (__instance.pulseEnergyBad > 0.0)
                textColor = Color.Lerp(textColor, Colors.pulseEnergyBadText, __instance.pulseEnergyBad / 0.5);
            if (__instance.pulseEnergyGood > 0.0)
                textColor = Color.Lerp(textColor, Colors.pulseEnergyGoodText, __instance.pulseEnergyGood / 0.5);
            Color glowColor = __instance.pulseEnergyGood > __instance.pulseEnergyBad ? Colors.pulseEnergyGoodGlow : Colors.pulseEnergyBadGlow;
            //Glow.Draw(xy + new Vec(10.0, 10.0), 50.0, glowColor.gain(Math.Max(__instance.pulseEnergyGood, __instance.pulseEnergyBad) / 0.5));
            string str = DB.IntStringCache(energyBlock[energy]);
            double x = xy.x + pulse + NumberPositions[count].Item1.x + ((str.Length - 1) * NumberPositions[count].Item2);
            double y = xy.y + NumberPositions[count].Item1.y;
            EnergyNumbers.Render(energyBlock[energy], x, y, textColor);
            //Draw.Text(str, x, y, DB.thicket, textColor, align: daisyowl.text.TAlign.Center, dontSubstituteLocFont: true);
            g.Pop();
            count++;
        }
    }

    private static IEnumerable<CodeInstruction> Combat_RenderEnergy_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
    {
        try
        {
            return new SequenceBlockMatcher<CodeInstruction>(instructions)
                .Find(
                    ILMatches.Call(nameof(G.Push))
                )
                .Insert(
                    SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
                    new CodeInstruction(OpCodes.Dup),
                    new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(SaveEnergyBox)))
                )
                .Find(
                    ILMatches.LdcR8(31.0)
                )
                .Insert(
                    SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
                    new CodeInstruction(OpCodes.Ldc_R8, 2.0),
                    new CodeInstruction(OpCodes.Add)
                )
                .Find(
                    ILMatches.Call(nameof(Draw.Sprite))
                )
                .Insert(
                    SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(DrawEnergyLights)))
                )
                .Find(
                    ILMatches.Call(nameof(Draw.Text)),
                    ILMatches.Instruction(OpCodes.Pop)
                )
                .Insert(
                    SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(DrawEnergyNumbers)))
                )
                .AllElements();
        }
        catch (Exception ex)
        {
            ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.GetDisplayName(@long: false), ex);
            return instructions;
        }
    }

    private static void SaveEnergyBox(Box box)
    {
        EnergyBox = box;
    }
    static Box EnergyBox = new();
    private static void DrawEnergyLights(Combat c, G g)
    {
        int count = 0;
        Dictionary<Energy, int> energyBlock = c.GetEnergy();
        List<Energy> inUseEnergy = ModEntry.EnergyApi.GetInUseEnergies(c, g.state);
        // if (inUseEnergy.Count > 2) return;
        Box box = EnergyBox;
        foreach (Energy energy in inUseEnergy)
        {
            EnergyInfo energyInfo = ModEntry.Energies[(int)energy];
            Vec xy = box.rect.xy;
            if (box.IsHover()) g.tooltips.Add(xy + new Vec(33), [
                new TTDivider(),
                new GlossaryTooltip($"{energy}ENERGY")
                {
                    Icon = ModEntry.Instance.GenericEnergyIcon.Sprite,
                    IconColor = energyInfo.GetColor(),
                    Title = ModEntry.Instance.Localizations.Localize(["energy", energy.ToString(), "name"]),
                    TitleColor = energyInfo.GetColor(),
                    Description = ModEntry.Instance.Localizations.Localize(["energy", energy.ToString(), "description"])
                }
            ]);
            Color energyColor = energyInfo.GetColor();
            Draw.Sprite(ModEntry.Instance.EnergyLights[count].Sprite, xy.x - 1.0, xy.y - 3.0, color: energyColor);
            count++;
        }
    }
    private static void DrawEnergyNumbers(Combat c, G g)
    {
        int count = 0;
        Dictionary<Energy, int> energyBlock = c.GetEnergy();
        List<Energy> inUseEnergy = ModEntry.EnergyApi.GetInUseEnergies(c, g.state);
        // if (inUseEnergy.Count > 2) return;
        Box box = EnergyBox;
        foreach (Energy energy in inUseEnergy)
        {
            EnergyInfo energyInfo = ModEntry.Energies[(int)energy];
            Vec xy = box.rect.xy;
            Color energyColor = energyInfo.GetColor();
            Color textColor = energyBlock[energy] > 0 ? Color.Lerp(energyColor, Colors.textMain, 0.5) : Colors.redd;
            double pulse = Mutil.GetPulse(g.state.time, c.pulseEnergyBad, 0.5) / 2;
            if (c.pulseEnergyBad > 0.0)
                textColor = Color.Lerp(textColor, Colors.pulseEnergyBadText, c.pulseEnergyBad / 0.5);
            if (c.pulseEnergyGood > 0.0)
                textColor = Color.Lerp(textColor, Colors.pulseEnergyGoodText, c.pulseEnergyGood / 0.5);
            Color glowColor = c.pulseEnergyGood > c.pulseEnergyBad ? Colors.pulseEnergyGoodGlow : Colors.pulseEnergyBadGlow;
            //Glow.Draw(xy + new Vec(10.0, 10.0), 50.0, glowColor.gain(Math.Max(__instance.pulseEnergyGood, __instance.pulseEnergyBad) / 0.5));
            string str = DB.IntStringCache(energyBlock[energy]);
            double x = xy.x + pulse + NumberPositions[count].Item1.x + ((str.Length - 1) * NumberPositions[count].Item2);
            double y = xy.y + NumberPositions[count].Item1.y - 2.0;
            EnergyNumbers.Render(energyBlock[energy], x, y, textColor);
            //Draw.Text(str, x, y, DB.thicket, textColor, align: daisyowl.text.TAlign.Center, dontSubstituteLocFont: true);
            count++;
        }
    }

    private static void EditorShortcuts_DebugKeys_Postfix(G g)
    {
        if (Input.GetKeyHeld(Keys.N) && Input.shift && g.state?.route is Combat c)
        { 
            foreach (Energy energy in Enum.GetValues<Energy>())
            {
                if (Input.GetKeyDown((Keys)((int)Keys.D1 + (int)energy)))
                {
                    c.QueueImmediate(new AModdedEnergy()
                    {
                        changeAmount = 1,
                        energyToChange = energy
                    });
                }
                if (Input.GetKeyDown((Keys)((int)Keys.NumPad1+(int)energy)))
                {
                    c.QueueImmediate(new AModdedEnergy()
                    {
                        changeAmount = 1,
                        energyToChange = energy
                    });
                }
            }
        }
    }

    private static IEnumerable<CodeInstruction> EditorShortcuts_DebugKeys_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
    {
        try
        {
            return new SequenceBlockMatcher<CodeInstruction>(instructions)
                .Find(
                    ILMatches.LdcI4(Keys.N),
                    ILMatches.LdcI4(1),
                    ILMatches.Call(nameof(Input.GetKeyDown)),
                    ILMatches.Brfalse.GetBranchTarget(out StructRef<Label> label)
                )
                .Insert(
                    SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
                    new CodeInstruction(OpCodes.Ldsfld, AccessTools.DeclaredField(typeof(Input), nameof(Input.shift))),
                    new CodeInstruction(OpCodes.Brtrue, label.Value)
                )
                .AllElements();
        }
        catch (Exception ex)
        {
            ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.GetDisplayName(@long: false), ex);
            return instructions;
        }
    }
}
