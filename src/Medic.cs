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
    [JsonPropertyName("HealSuccessSound")] public string HealSuccessSound { get; init; } = "items/healthshot_success_01";
    [JsonPropertyName("HealFailureSound")] public string HealFailureSound { get; init; } = "buttons/blip2";
}

[MinimumApiVersion(130)]
public class Medic : BasePlugin, IPluginConfig<ConfigGen>
{
    public override string ModuleName => "Medic";
    public override string ModuleVersion => "0.0.9";
    public override string ModuleAuthor => "Quake1011 and GSM-RO";
    public ConfigGen Config { get; set; } = null!;

    public void OnConfigParsed(ConfigGen config)
    {
        Config = config;
    }

    private Dictionary<ulong, int> _tries = new();

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        RegisterListener<Listeners.OnMapStart>(_ =>
        {
            Server.PrecacheModel("weapons/w_eq_charge");
        });
    }
    
    [GameEventHandler(mode: HookMode.Post)]
private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if(!player.IsValid || player.IsBot)
            return HookResult.Continue;
        
        if (!_tries.ContainsKey(player.SteamID))
            _tries.Add(player.SteamID, Config.MaxUse);

        return HookResult.Continue;
    }

    [GameEventHandler(mode: HookMode.Post)]
    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        _tries.Clear();
        
        foreach (var player in Utilities.GetPlayers()) _tries.TryAdd(player.SteamID, Config.MaxUse);

        return HookResult.Continue;
    }

    [ConsoleCommand("medkit", "Heal player")]
    [ConsoleCommand("medic", "Heal player")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnCommand(CCSPlayerController? activator, CommandInfo command)
    {
        if (activator == null) 
			return;

        if (!activator.PawnIsAlive)
        {
            activator.PrintToChat($" {ChatColors.Red}[Medic] {ChatColors.Default}You should be alive to use this command.");
            if(Config.HealFailureSound != "")
                activator.ExecuteClientCommand($"play {Config.HealFailureSound}");
            return;
        }
        
        if (Config.AccessFlag != "")
        {
            if (!AdminManager.PlayerHasPermissions(activator, Config.AccessFlag))
            {
                activator.PrintToChat($" {ChatColors.Red}[Medic] {ChatColors.Default}Have not access for to use this command.");
                if(Config.HealFailureSound != "")
                    activator.ExecuteClientCommand($"play {Config.HealFailureSound}");
                return;
            }
        }

        if (_tries[activator.SteamID] <= 0)
        {
            activator.PrintToChat($" {ChatColors.Red}[Medic] {ChatColors.Default}The limit has been reached. Total: {ChatColors.Red}{Config.MaxUse}");
            if(Config.HealFailureSound != "")
                activator.ExecuteClientCommand($"play {Config.HealFailureSound}");
            return;
        }

        if (activator.InGameMoneyServices!.Account < Config.Cost)
        {
            activator.PrintToChat($" {ChatColors.Red}[Medic] {ChatColors.Default}Not enough money to use medic. Need: {ChatColors.Red}{Config.Cost}$");
            if(Config.HealFailureSound != "")
                activator.ExecuteClientCommand($"play {Config.HealFailureSound}");
            return;
        }

        if (activator.PlayerPawn.Value != null && activator.PlayerPawn.Value.Health > Config.MinHealth)
        {
            activator.PrintToChat($" {ChatColors.Red}[Medic] {ChatColors.Default}Too much health for to use medic. Need: {ChatColors.Red}{Config.MinHealth}hp or less");
            if(Config.HealFailureSound != "")
                activator.ExecuteClientCommand($"play {Config.HealFailureSound}");
            return;
        }
        
        if (activator.PlayerPawn.Value != null && activator.PlayerPawn.Value.Health == activator.PlayerPawn.Value.MaxHealth)
        {
            activator.PrintToChat($" {ChatColors.Red}[Medic] {ChatColors.Default}You are have full hp already");
            if(Config.HealFailureSound != "")
                activator.ExecuteClientCommand($"play {Config.HealFailureSound}");
            return;
        }

        activator.InGameMoneyServices.Account -= Config.Cost;

        var total = activator.PlayerPawn.Value != null && (activator.PlayerPawn.Value.MaxHealth >= activator.PlayerPawn.Value.Health + Config.HealHealth)
            ? Config.HealHealth
            : activator.PlayerPawn.Value!.MaxHealth - activator.PlayerPawn.Value.Health;

        activator.PlayerPawn.Value.Health += total;
        Utilities.SetStateChanged(activator.PlayerPawn.Value, "CBaseEntity", "m_iHealth");
        
        _tries[activator.SteamID]--;
		
        if (Config.ShowCall)
            Server.PrintToChatAll($" {ChatColors.Red}[Medic] {ChatColors.Default}Player {ChatColors.Green}{activator.PlayerName}{ChatColors.Default} used medic and restore {ChatColors.Red}{total}hp");
        
        if(Config.HealSuccessSound != "")
            activator.ExecuteClientCommand($"play {Config.HealSuccessSound}");

                

    }
}