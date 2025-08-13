using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nickel;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BaseMod.External;

namespace BaseMod;

internal class ModEntry : SimpleMod
{
    internal static ModEntry Instance { get; private set; } = null!;
    internal Harmony Harmony;
    internal IKokoroApi.IV2 KokoroApi;
    internal ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations { get; }
    internal ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations { get; }

    private static readonly List<Type> BaseCommonCardTypes = [
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

    public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
    {
        Instance = this;
        Harmony = new Harmony("YourNameHere.BaseMod");
        
        //You're probably gonna use kokoro
        KokoroApi = helper.ModRegistry.GetApi<IKokoroApi>("Shockah.Kokoro")!.V2;

        AnyLocalizations = new JsonLocalizationProvider(
            tokenExtractor: new SimpleLocalizationTokenExtractor(),
            localeStreamFunction: locale => package.PackageRoot.GetRelativeFile($"i18n/{locale}.json").OpenRead()
        );
        Localizations = new MissingPlaceholderLocalizationProvider<IReadOnlyList<string>>(
            new CurrentLocaleOrEnglishLocalizationProvider<IReadOnlyList<string>>(AnyLocalizations)
        );

        foreach (var type in AllRegisterableTypes)
            AccessTools.DeclaredMethod(type, nameof(IRegisterable.Register))?.Invoke(null, [package, helper]);
    }
    
    public static ISpriteEntry RegisterSprite(IPluginPackage<IModManifest> package, string dir)
    {
        return Instance.Helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile(dir));
    }
}

