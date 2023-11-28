using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace medic;

public class ConfigGen : BasePluginConfig
{
    [JsonPropertyName("MinHealth")] public int MinHealth { get; set; } = 40;
    [JsonPropertyName("HealHealth")] public int HealHealth { get; set; } = 100;
    [JsonPropertyName("Cost")] public int Cost { get; set; } = 2000;
    [JsonPropertyName("ShowCall")] public bool ShowCall { get; set; } = true;
    [JsonPropertyName("MaxUse")] public int MaxUse { get; set; } = 1;
    [JsonPropertyName("AccessFlag")] public string AccessFlag { get; set; } = "@css/ban";
}

[MinimumApiVersion(78)]
public class Medic : BasePlugin, IPluginConfig<ConfigGen>
{
    public override string ModuleName => "Medic";
    public override string ModuleVersion => "0.0.3";
    public override string ModuleAuthor => "Quake1011";
    public ConfigGen Config { get; set; } = null!;
    public void OnConfigParsed(ConfigGen config) { Config = config; }

    private int[]? _tries;
    
    public override void Load(bool hotReload) { RegisterEventHandler<EventRoundStart>(OnRoundStart); }

    [GameEventHandler(mode: HookMode.Post)]
    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        _tries ??= null;
        _tries = Enumerable.Repeat(Config.MaxUse, Server.MaxPlayers).ToArray();
        return HookResult.Continue;
    }

    [ConsoleCommand("heal", "Heal player")]
    [ConsoleCommand("medic", "Heal player")]
    [ConsoleCommand("doctor", "Heal player")]
    [ConsoleCommand("hp", "Heal player")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnCommand(CCSPlayerController? activator, CommandInfo command)
    {
        if (activator == null || activator.PawnIsAlive) 
			return;

        if (_tries == null)
            return;

        if (Config.AccessFlag != "")
        {
            if (!AdminManager.PlayerHasPermissions(activator, Config.AccessFlag))
            {
                activator.PrintToChat($" {ChatColors.Red}[Medic] {ChatColors.Default}Have not access for to use this command.");
                return;
            }
        }
        
        if (_tries[activator.EntityIndex!.Value.Value - 1] <= 0)
        {
            activator.PrintToChat($" {ChatColors.Red}[Medic] {ChatColors.Default}The limit has been reached. Total: {ChatColors.Red}{Config.MaxUse}");
            return;
        }

        if (activator.InGameMoneyServices!.Account < Config.Cost)
        {
            activator.PrintToChat($" {ChatColors.Red}[Medic] {ChatColors.Default}Not enough money to use medic. Need: {ChatColors.Red}{Config.Cost}$");
            return;
        }

        if (activator.Health < Config.MinHealth)
        {
            activator.PrintToChat($" {ChatColors.Red}[Medic] {ChatColors.Default}Too little health for to use medic. Need: {ChatColors.Red}{Config.MinHealth}hp");
            return;
        }
        
        activator.InGameMoneyServices.Account -= Config.Cost;

        var total = (activator.MaxHealth >= activator.Health + Config.HealHealth)
            ? Config.HealHealth
            : activator.MaxHealth - activator.Health;

        activator.Health += total;
                    
        _tries[activator.EntityIndex!.Value.Value - 1]--;
		
        if (Config.ShowCall)
            Server.PrintToChatAll($" {ChatColors.Red}[Medic] {ChatColors.Default}Player {ChatColors.Green}{activator.PlayerName}{ChatColors.Default} used medic and restore {ChatColors.Red}{total}hp");
    }
}