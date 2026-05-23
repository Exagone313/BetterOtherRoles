namespace BetterOtherRoles.Modules;

public static class CustomRegions
{
    public static readonly IRegionInfo[] DefaultRegions = {
        new StaticHttpRegionInfo("<color=#C7B816FF>BOR [EU]</color>",
            StringNames.NoTranslation,
            "https://amongus-eu.eno.pm",
            new Il2CppReferenceArray<ServerInfo>(new ServerInfo[1]
            {
                new("Http-1", "https://amongus-eu.eno.pm", 443, false)
            })).CastFast<IRegionInfo>(),
        new StaticHttpRegionInfo("<color=#00C800>Exa [EU]</color>",
            StringNames.NoTranslation,
            "https://amongus-eu.eno.pm",
            new Il2CppReferenceArray<ServerInfo>(new ServerInfo[1]
            {
                new("Http-1", "https://impostor.ewd.app", 443, false)
            })).CastFast<IRegionInfo>(),
    };
}
