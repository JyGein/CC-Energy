using Nanoray.PluginManager;
using Nickel;

namespace BaseMod;

internal interface IRegisterable
{
    static abstract void Register(IPluginPackage<IModManifest> package, IModHelper helper);
}