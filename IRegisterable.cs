using Nanoray.PluginManager;
using Nickel;

namespace CCEnergy;

internal interface IRegisterable
{
    static abstract void Register(IPluginPackage<IModManifest> package, IModHelper helper);
}