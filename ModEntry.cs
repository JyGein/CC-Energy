using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nickel;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CCEnergy.External;
using CCEnergy.Energies;
using CCEnergy.Cards;

namespace CCEnergy;

internal class ModEntry : SimpleMod
{
    internal static ModEntry Instance { get; private set; } = null!;
    internal static EnergyApi EnergyApi { get; private set; } = null!;
    internal readonly static List<EnergyInfo> Energies = [new RevengeEnergy(), new CalmEnergy(), new ThermalEnergy()];
    internal Harmony Harmony;
    internal IKokoroApi.IV2 KokoroApi;
    internal ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations { get; }
    internal ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations { get; }

    private static readonly List<Type> BaseCommonCardTypes = [
        typeof(TestCard)
    ];
    private static readonly List<Type> BaseUncommonCardTypes = [
    ];
    private static readonly List<Type> BaseRareCardTypes = [
    ];
    private static readonly List<Type> BaseSpecialCardTypes = [
    ];
    private static readonly IEnumerable<Type> BaseCardTypes =
        BaseCommonCardTypes
            .Concat(BaseUncommonCardTypes)
            .Concat(BaseRareCardTypes)
            .Concat(BaseSpecialCardTypes);

    private static readonly List<Type> BaseCommonArtifacts = [
    ];
    private static readonly List<Type> BaseBossArtifacts = [
    ];
    private static readonly IEnumerable<Type> BaseArtifactTypes =
        BaseCommonArtifacts
            .Concat(BaseBossArtifacts);

    private static readonly IEnumerable<Type> AllRegisterableTypes =
        BaseCardTypes
            .Concat(BaseArtifactTypes);
    internal ISpriteEntry CardEnergyBGExtension { get; }
    internal ISpriteEntry CombatMiniEnergy { get; }
    internal ISpriteEntry GenericEnergyIcon { get; }
    internal List<ISpriteEntry> EnergyLights { get; }
    internal ISpriteEntry EnergyNumbers { get; }

    public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
    {
        Instance = this;EnergyApi = new();
        Harmony = new Harmony("JyGein.Energy");
        
        //You're probably gonna use kokoro
        KokoroApi = helper.ModRegistry.GetApi<IKokoroApi>("Shockah.Kokoro")!.V2;

        AnyLocalizations = new JsonLocalizationProvider(
            tokenExtractor: new SimpleLocalizationTokenExtractor(),
            localeStreamFunction: locale => package.PackageRoot.GetRelativeFile($"i18n/{locale}.json").OpenRead()
        );
        Localizations = new MissingPlaceholderLocalizationProvider<IReadOnlyList<string>>(
            new CurrentLocaleOrEnglishLocalizationProvider<IReadOnlyList<string>>(AnyLocalizations)
        );

        CardEnergyBGExtension = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Card/cardEnergyBG.png"));
        CombatMiniEnergy = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Combat/mini_energy.png"));
        GenericEnergyIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/icons/grayscale_energy.png"));
        EnergyLights = [];
        EnergyLights.Add(helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Combat/energy_light_1.png")));
        EnergyLights.Add(helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Combat/energy_light_2.png")));
        EnergyLights.Add(helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Combat/energy_light_3.png")));
        EnergyNumbers = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/numbers/energyNumebrs.png"));

        foreach (var type in AllRegisterableTypes)
            AccessTools.DeclaredMethod(type, nameof(IRegisterable.Register))?.Invoke(null, [package, helper]);

        _ = new Patches(Harmony);
    }

    public override object GetApi(IModManifest requestingMod) => EnergyApi;
    
    public static ISpriteEntry RegisterSprite(IPluginPackage<IModManifest> package, string dir)
    {
        return Instance.Helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile(dir));
    }
}

