using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;

namespace medic;

public class ConfigGen : BasePluginConfig
{
    [JsonPropertyName("MinHealth")] public int MinHealth { get; init; } = 40;
    [JsonPropertyName("HealHealth")] public int HealHealth { get; init; } = 100;
    [JsonPropertyName("Cost")] public int Cost { get; init; } = 2000;
    [JsonPropertyName("ShowCall")] public bool ShowCall { get; init; } = true;
    [JsonPropertyName("MaxUse")] public int MaxUse { get; init; } = 2;
    [JsonPropertyName("AccessFlag")] public string AccessFlag { get; init; } = "@css/ban";
}

[MinimumApiVersion(130)]
public class Medic : BasePlugin, IPluginConfig<ConfigGen>
{
    public override string ModuleName => "Medic";
    public override string ModuleVersion => "0.0.5";
    public override string ModuleAuthor => "Quake1011";
    public ConfigGen Config { get; set; } = null!;

    public void OnConfigParsed(ConfigGen config)
    {
        Config = config;
    }

    private Dictionary<ulong, int> _tries = new();

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventRoundStart>(OnRoundStart);

        RegisterListener<Listeners.OnMapStart>(_ =>
        {
            Server.PrecacheModel("weapons/w_eq_charge");
        });
    }

    [GameEventHandler(mode: HookMode.Post)]
    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        _tries.Clear();
        
        foreach (var player in Utilities.GetPlayers()) _tries.TryAdd(player.SteamID, Config.MaxUse);

        return HookResult.Continue;
    }

    [ConsoleCommand("heal", "Heal player")]
    [ConsoleCommand("medic", "Heal player")]
    [ConsoleCommand("doctor", "Heal player")]
    [ConsoleCommand("hp", "Heal player")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnCommand(CCSPlayerController? activator, CommandInfo command)
    {
        if (activator == null || !activator.PawnIsAlive) 
			return;
        
        if (Config.AccessFlag != "")
        {
            if (!AdminManager.PlayerHasPermissions(activator, Config.AccessFlag))
            {
                activator.PrintToChat($" {ChatColors.Red}[Medic] {ChatColors.Default}Have not access for to use this command.");
                return;
            }
        }

        if (_tries[activator.SteamID] <= 0)
        {
            activator.PrintToChat($" {ChatColors.Red}[Medic] {ChatColors.Default}The limit has been reached. Total: {ChatColors.Red}{Config.MaxUse}");
            return;
        }

        if (activator.InGameMoneyServices!.Account < Config.Cost)
        {
            activator.PrintToChat($" {ChatColors.Red}[Medic] {ChatColors.Default}Not enough money to use medic. Need: {ChatColors.Red}{Config.Cost}$");
            return;
        }

        if (activator.PlayerPawn.Value != null && activator.PlayerPawn.Value.Health > Config.MinHealth)
        {
            activator.PrintToChat($" {ChatColors.Red}[Medic] {ChatColors.Default}Too much health for to use medic. Need: {ChatColors.Red}{Config.MinHealth}hp or less");
            return;
        }
        
        if (activator.PlayerPawn.Value != null && activator.PlayerPawn.Value.Health == activator.PlayerPawn.Value.MaxHealth)
        {
            activator.PrintToChat($" {ChatColors.Red}[Medic] {ChatColors.Default}You are have full hp already");
            return;
        }

        activator.InGameMoneyServices.Account -= Config.Cost;

        var total = activator.PlayerPawn.Value != null && (activator.PlayerPawn.Value.MaxHealth >= activator.PlayerPawn.Value.Health + Config.HealHealth)
            ? Config.HealHealth
            : activator.PlayerPawn.Value!.MaxHealth - activator.PlayerPawn.Value.Health;

        activator.PlayerPawn.Value.Health += total;
        
        _tries[activator.SteamID]--;
		
        if (Config.ShowCall)
            Server.PrintToChatAll($" {ChatColors.Red}[Medic] {ChatColors.Default}Player {ChatColors.Green}{activator.PlayerName}{ChatColors.Default} used medic and restore {ChatColors.Red}{total}hp");
        
        activator.GiveNamedItem(CsItem.BreachCharge);
        activator.RemoveItemByDesignerName("weapon_breachcharge");
    }
}