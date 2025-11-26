using CCEnergy.Cards;
using CCEnergy.Energies;
using CCEnergy.External;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Graphics;
using Nanoray.PluginManager;
using Nickel;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using static CCEnergy.EnergyApi;
using static CCEnergy.IEnergyApi;
using MGColor = Microsoft.Xna.Framework.Color;

namespace CCEnergy;

internal class ModEntry : SimpleMod
{
    internal static ModEntry Instance { get; private set; } = null!;
    internal static EnergyApi EnergyApi { get; private set; } = null!;
    internal readonly static List<EnergyInfo> Energies = [new RevengeEnergy(), new ResidualEnergy(), new ThermalEnergy(), new KineticEnergy(), new CoreEnergy(), new SacrificeEnergy(), new ChargedEnergy()];
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
    internal ISpriteEntry CardModdedEnergyBG { get; }
    internal ISpriteEntry CardEnergyBGExtension { get; }
    internal ISpriteEntry CombatMiniEnergy { get; }
    internal ISpriteEntry GenericEnergyIcon { get; }
    internal List<ISpriteEntry> EnergyLights { get; }
    internal ISpriteEntry EnergyNumbers { get; }
    internal ISpriteEntry ModdedEnergyCostSatisfied { get; }
    internal ISpriteEntry ModdedEnergyCostUnsatisfied { get; }
    internal Dictionary<Energy, ISpriteEntry> EnergyIcons { get; }
    internal IStatusEntry MaxChargedEnergy { get; }

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

        CardModdedEnergyBG = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Card/cardEnergyBG.png"));
        CardEnergyBGExtension = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Card/cardEnergyBGExtension.png"));
        CombatMiniEnergy = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Combat/mini_energy.png"));
        GenericEnergyIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/icons/grayscale_energy.png"));
        EnergyLights = [];
        EnergyLights.Add(helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Combat/energy_light_1.png")));
        EnergyLights.Add(helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Combat/energy_light_2.png")));
        EnergyLights.Add(helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Combat/energy_light_3.png")));
        EnergyLights.Add(helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Combat/energy_light_4.png")));
        EnergyLights.Add(helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Combat/energy_light_5.png")));
        EnergyLights.Add(helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Combat/energy_light_6.png")));
        EnergyLights.Add(helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Combat/energy_light_7.png")));
        EnergyNumbers = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/numbers/energyNumebrs.png"));
        ModdedEnergyCostSatisfied = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/icons/EnergyCostSatisfied.png"));
        ModdedEnergyCostUnsatisfied = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/icons/EnergyCostUnsatisfied.png"));
        EnergyIcons = [];
        MaxChargedEnergy = helper.Content.Statuses.RegisterStatus("MaxChargedEnergy", new()
        {
            Definition = new()
            {
                icon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/icons/MaxChargedEnergy.png")).Sprite,
                color = new("00eeee"),
                isGood = false
            },
            Name = AnyLocalizations.Bind(["status", "Max Charged Energy", "name"]).Localize,
            Description = AnyLocalizations.Bind(["status", "Max Charged Energy", "description"]).Localize
        });
        foreach (Energy energy in Enum.GetValues(typeof(Energy)))
        {
            IModdedEnergyResource moddedEnergyResource = new ModdedEnergyResource { EnergyType = energy };
            EnergyInfo energyInfo = ModEntry.Energies[(int)moddedEnergyResource.EnergyType];
            Color color = energyInfo.GetColor();
            Spr SatifiedIcon = helper.Content.Sprites.RegisterSprite(() =>
            {
                Texture2D moddedEnergyCostSatisfiedTexture = SpriteLoader.Get(ModdedEnergyCostSatisfied.Sprite)!;
                MGColor[] data = new MGColor[moddedEnergyCostSatisfiedTexture.Width * moddedEnergyCostSatisfiedTexture.Height];
                moddedEnergyCostSatisfiedTexture.GetData(data);

                for (int i = 0; i < data.Length; i++)
                    data[i] = new MGColor(
                        (float)(data[i].R / 255.0 * color.r),
                        (float)(data[i].G / 255.0 * color.g),
                        (float)(data[i].B / 255.0 * color.b),
                        (float)(data[i].A / 255.0 * color.a)
                    );

                Texture2D texture = new Texture2D(MG.inst.GraphicsDevice, moddedEnergyCostSatisfiedTexture.Width, moddedEnergyCostSatisfiedTexture.Height);
                texture.SetData(data);
                return texture;
            }).Sprite;
            KokoroApi.ActionCosts.RegisterResourceCostIcon(moddedEnergyResource, SatifiedIcon, ModdedEnergyCostUnsatisfied.Sprite);
            EnergyIcons[energy] = helper.Content.Sprites.RegisterSprite(() =>
            {
                Texture2D moddedEnergyIcon = SpriteLoader.Get(GenericEnergyIcon.Sprite)!;
                MGColor[] data = new MGColor[moddedEnergyIcon.Width * moddedEnergyIcon.Height];
                moddedEnergyIcon.GetData(data);

                for (int i = 0; i < data.Length; i++)
                    data[i] = new MGColor(
                        (float)(data[i].R / 255.0 * color.r),
                        (float)(data[i].G / 255.0 * color.g),
                        (float)(data[i].B / 255.0 * color.b),
                        (float)(data[i].A / 255.0 * color.a)
                    );

                Texture2D texture = new Texture2D(MG.inst.GraphicsDevice, moddedEnergyIcon.Width, moddedEnergyIcon.Height);
                texture.SetData(data);
                return texture;
            });
        }

        ModdedEnergyAsStatusApi.ModdedEnergyAsStatusManager.Setup(Harmony);
        foreach (var type in AllRegisterableTypes)
            AccessTools.DeclaredMethod(type, nameof(IRegisterable.Register))?.Invoke(null, [package, helper]);
        foreach (EnergyInfo energy in Energies)
            energy.DoPatches();

        _ = new Patches(Harmony);
    }

    public override object GetApi(IModManifest requestingMod) => EnergyApi;
    
    public static ISpriteEntry RegisterSprite(IPluginPackage<IModManifest> package, string dir)
    {
        return Instance.Helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile(dir));
    }
}

