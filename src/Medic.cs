using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;
using System.Reflection;
using System.Text.Json;

namespace medic;

public class ConfigGen : BasePluginConfig
{
    [JsonPropertyName("PluginTag")] 
    public string PluginTag { get; init; } = " [red][Medic]";
    [JsonPropertyName("MinHealth")] 
    public int MinHealth { get; init; } = 40;
    [JsonPropertyName("HealHealth")] 
    public int HealHealth { get; init; } = 100;
    [JsonPropertyName("Cost")] 
    public int Cost { get; init; } = 2000;
    [JsonPropertyName("ShowCall")] 
    public bool ShowCall { get; init; } = true;
    [JsonPropertyName("MaxUse")] 
    public int MaxUse { get; init; } = 2;
    [JsonPropertyName("AccessFlag")] 
    public string AccessFlag { get; init; } = "@css/ban";
    [JsonPropertyName("HealSuccessSound")] 
    public string HealSuccessSound { get; init; } = "items/healthshot_success_01";
    [JsonPropertyName("HealFailureSound")] 
    public string HealFailureSound { get; init; } = "buttons/blip2";
    [JsonPropertyName("ConfigVersion")]
    public override int Version { get; set; } = 2;
}

[MinimumApiVersion(225)]
public class Medic : BasePlugin, IPluginConfig<ConfigGen>
{
    public override string ModuleName => "Medic";
    public override string ModuleVersion => "1.0";
    public override string ModuleAuthor => "Quake1011, GSM-RO & panda.";
    public ConfigGen Config { get; set; } = null!;
    private static readonly string AssemblyName = Assembly.GetExecutingAssembly().GetName().Name ?? "";
    private static readonly string CfgPath = Path.Combine(Server.GameDirectory, "csgo", "addons", "counterstrikesharp", "configs", "plugins", AssemblyName, $"{AssemblyName}.json");

    private void UpdateConfig<T>(T config) where T : BasePluginConfig, new()
    {
        var newCfgVersion = new T().Version;

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        if (config.Version == newCfgVersion)
            return;

        config.Version = newCfgVersion;

        var updatedJsonContent = JsonSerializer.Serialize(config, options);
        File.WriteAllText(CfgPath, updatedJsonContent);

        Console.WriteLine($"Configuration file updated for V{newCfgVersion}.");
    }
    private static readonly Dictionary<string, char> ColorMap = new Dictionary<string, char>
    {
        { "[default]", ChatColors.Default },
        { "[white]", ChatColors.White },
        { "[darkred]", ChatColors.DarkRed },
        { "[green]", ChatColors.Green },
        { "[lightyellow]", ChatColors.LightYellow },
        { "[lightblue]", ChatColors.LightBlue },
        { "[olive]", ChatColors.Olive },
        { "[lime]", ChatColors.Lime },
        { "[red]", ChatColors.Red },
        { "[lightpurple]", ChatColors.LightPurple },
        { "[purple]", ChatColors.Purple },
        { "[grey]", ChatColors.Grey },
        { "[yellow]", ChatColors.Yellow },
        { "[gold]", ChatColors.Gold },
        { "[silver]", ChatColors.Silver },
        { "[blue]", ChatColors.Blue },
        { "[darkblue]", ChatColors.DarkBlue },
        { "[bluegrey]", ChatColors.BlueGrey },
        { "[magenta]", ChatColors.Magenta },
        { "[lightred]", ChatColors.LightRed },
        { "[orange]", ChatColors.Orange }
    };

    private string ReplaceColorPlaceholders(string message)
    {
        foreach (var colorPlaceholder in ColorMap)
        {
            message = message.Replace(colorPlaceholder.Key, colorPlaceholder.Value.ToString());
        }
        return message;
    }
    public void OnConfigParsed(ConfigGen config)
    {
        Config = config;

        UpdateConfig(config);  
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
    
    [GameEventHandler(HookMode.Post)]
    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null) return HookResult.Continue;

        if(!player.IsValid || player.IsBot)
            return HookResult.Continue;
        
        if (!_tries.ContainsKey(player.SteamID))
            _tries.Add(player.SteamID, Config.MaxUse);

        return HookResult.Continue;
    }

    [GameEventHandler(HookMode.Post)]
    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        _tries.Clear();
        
        foreach (var player in Utilities.GetPlayers()) _tries.TryAdd(player.SteamID, Config.MaxUse);

        return HookResult.Continue;
    }

    [ConsoleCommand("css_medkit", "Heal player")]
    [ConsoleCommand("css_medic", "Heal player")]
    [ConsoleCommand("css_doctor", "Heal player")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) 
			return;

        if (!player.PawnIsAlive)
        {
            player.PrintToChat(ReplaceColorPlaceholders(string.Format(Localizer["medic.AliveMsg"], Config.PluginTag)));
            if(Config.HealFailureSound != "")
                player.ExecuteClientCommand($"play {Config.HealFailureSound}");
            return;
        }
        
        if (Config.AccessFlag != "")
        {
            if (!AdminManager.PlayerHasPermissions(player, Config.AccessFlag))
            {
                player.PrintToChat(ReplaceColorPlaceholders(string.Format(Localizer["medic.NoAccess"], Config.PluginTag)));
                if(Config.HealFailureSound != "")
                    player.ExecuteClientCommand($"play {Config.HealFailureSound}");
                return;
            }
        }

        if (_tries[player.SteamID] <= 0)
        {
            player.PrintToChat(ReplaceColorPlaceholders(string.Format(Localizer["medic.LimitReached"], Config.PluginTag, Config.MaxUse)));
            if(Config.HealFailureSound != "")
                player.ExecuteClientCommand($"play {Config.HealFailureSound}");
            return;
        }

        if (player.InGameMoneyServices!.Account < Config.Cost)
        {
            player.PrintToChat(ReplaceColorPlaceholders(string.Format(Localizer["medic.NoMoney"], Config.PluginTag, Config.Cost)));
            if(Config.HealFailureSound != "")
                player.ExecuteClientCommand($"play {Config.HealFailureSound}");
            return;
        }

        if (player.PlayerPawn.Value != null && player.PlayerPawn.Value.Health > Config.MinHealth)
        {
            player.PrintToChat(ReplaceColorPlaceholders(string.Format(Localizer["medic.MinHealth"], Config.PluginTag, Config.MinHealth)));
            if(Config.HealFailureSound != "")
                player.ExecuteClientCommand($"play {Config.HealFailureSound}");
            return;
        }
        
        if (player.PlayerPawn.Value != null && player.PlayerPawn.Value.Health == player.PlayerPawn.Value.MaxHealth)
        {
            player.PrintToChat(ReplaceColorPlaceholders(string.Format(Localizer["medic.FullHP"], Config.PluginTag)));
            if(Config.HealFailureSound != "")
                player.ExecuteClientCommand($"play {Config.HealFailureSound}");
            return;
        }

        player.InGameMoneyServices.Account -= Config.Cost;

        var total = player.PlayerPawn.Value != null && (player.PlayerPawn.Value.MaxHealth >= player.PlayerPawn.Value.Health + Config.HealHealth)
            ? Config.HealHealth
            : player.PlayerPawn.Value!.MaxHealth - player.PlayerPawn.Value.Health;

        player.PlayerPawn.Value.Health += total;
        Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseEntity", "m_iHealth");
        
        _tries[player.SteamID]--;
		
        if (Config.ShowCall)
            Server.PrintToChatAll(ReplaceColorPlaceholders(string.Format(Localizer["medic.HealMsg"], Config.PluginTag, player.PlayerName, total)));
            
        if(Config.HealSuccessSound != "")
            player.ExecuteClientCommand($"play {Config.HealSuccessSound}");
    }
}