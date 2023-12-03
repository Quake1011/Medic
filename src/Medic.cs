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
    [JsonPropertyName("MinHealth")] public int MinHealth { get; set; } = 40;
    [JsonPropertyName("HealHealth")] public int HealHealth { get; set; } = 100;
    [JsonPropertyName("Cost")] public int Cost { get; set; } = 2000;
    [JsonPropertyName("ShowCall")] public bool ShowCall { get; set; } = true;
    [JsonPropertyName("MaxUse")] public int MaxUse { get; set; } = 2;
    [JsonPropertyName("AccessFlag")] public string AccessFlag { get; set; } = "@css/ban";
}

[MinimumApiVersion(90)]
public class Medic : BasePlugin, IPluginConfig<ConfigGen>
{
    public override string ModuleName => "Medic";
    public override string ModuleVersion => "0.0.3";
    public override string ModuleAuthor => "Quake1011";
    public ConfigGen Config { get; set; } = null!;
    public void OnConfigParsed(ConfigGen config) { Config = config; }

    public static Dictionary<string, List<ItemDefinition>> SlotByWeapon = new()
    {
        { "1", new List<ItemDefinition> { ItemDefinition.AWP, ItemDefinition.AK_47, ItemDefinition.M4A4, ItemDefinition.M4A1_S, ItemDefinition.FAMAS, ItemDefinition.GALIL_AR, ItemDefinition.SSG_08, ItemDefinition.AUG, ItemDefinition.SG_553, ItemDefinition.NOVA, ItemDefinition.SAWED_OFF, ItemDefinition.XM1014, ItemDefinition.PP_BIZON, ItemDefinition.MAG_7, ItemDefinition.UMP_45, ItemDefinition.P90, ItemDefinition.MP5_SD } },
        { "2", new List<ItemDefinition> { ItemDefinition.DESERT_EAGLE, ItemDefinition.DUAL_BERETTAS, ItemDefinition.GLOCK_18, ItemDefinition.P2000, ItemDefinition.USP_S, ItemDefinition.P250, ItemDefinition.TEC_9, ItemDefinition.FIVE_SEVEN, ItemDefinition.CZ75_AUTO, ItemDefinition.R8_REVOLVER } },
        { "3", new List<ItemDefinition> { ItemDefinition.KNIFE_T, ItemDefinition.KNIFE_CT, ItemDefinition.KNIFE_GG, ItemDefinition.KNIFE_GHOST, ItemDefinition.BARE_HANDS, ItemDefinition.MEELE, ItemDefinition.AXE, ItemDefinition.HAMMER, ItemDefinition.WRENCH } },
        { "4", new List<ItemDefinition> { ItemDefinition.HIGH_EXPLOSIVE_GRENADE, ItemDefinition.FLASHBANG, ItemDefinition.SMOKE_GRENADE, ItemDefinition.MOLOTOV, ItemDefinition.INCENDIARY_GRENADE, ItemDefinition.DEFUSE_KIT, ItemDefinition.RESCUE_KIT, ItemDefinition.HEALTHSHOT, ItemDefinition.MUSIC_KIT, ItemDefinition.HIGH_EXPLOSIVE_GRENADE, ItemDefinition.FIRE_BOMB, ItemDefinition.DIVERSION_DEVICE, ItemDefinition.FRAG_GRENADE, ItemDefinition.BUMP_MINE } },
        { "5", new List<ItemDefinition> { ItemDefinition.C4_EXPLOSIVE, ItemDefinition.TABLET } }
    };

    private Dictionary<ulong, int> _tries = new();

    public override void Load(bool hotReload) { RegisterEventHandler<EventRoundStart>(OnRoundStart); }

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
        
       
        RefreshUi(activator, activator.PlayerPawn.Value!.WeaponServices!.ActiveWeapon.Value!.AttributeManager.Item.ItemDefinitionIndex);
    }

    private static void RefreshUi(CCSPlayerController client, ushort defIndex)
    {
        if (client.PlayerPawn.Value!.WeaponServices == null || client.PlayerPawn.Value!.ItemServices == null) return;
        
        client.GiveNamedItem(CsItem.BreachCharge);
        
        foreach (var weapon in client.PlayerPawn.Value!.WeaponServices.MyWeapons)
        {
            if (weapon.IsValid && weapon.Value != null && weapon.Value.IsValid &&
                string.IsNullOrWhiteSpace(weapon.Value.DesignerName) == false &&
                weapon.Value.DesignerName.Equals("weapon_breachcharge"))
            {
                weapon.Value.Remove();
                client.ExecuteClientCommand("slot" + SlotByWeapon.FirstOrDefault(kv => kv.Value.Contains((ItemDefinition)defIndex)).Key);
                break;
            }
        }
    }
}