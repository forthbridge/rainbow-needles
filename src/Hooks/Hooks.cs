using System;
using System.Linq;

namespace RainbowNeedles;

public static partial class Hooks
{
    public static void ApplyInit() => On.RainWorld.OnModsInit += RainWorld_OnModsInit;

    public static bool IsInit { get; private set; } = false;

    private static void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        try
        {
            ModOptions.RegisterOI();

            if (IsInit) return;
            IsInit = true;

            var mod = ModManager.ActiveMods.FirstOrDefault(mod => mod.id == Plugin.MOD_ID);

            Plugin.MOD_NAME = mod.name;
            Plugin.VERSION = mod.version;
            Plugin.AUTHORS = mod.authors;

            ApplyHooks();
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError("OnModsInit Exception:\n" + e);
        }
        finally
        {
            orig(self);
        }
    }

    public static void ApplyHooks()
    {
        ApplyFunctionHooks();
    }
}
