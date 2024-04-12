# Medic
Allows you to refill your health. A rewritten version of the [plugin](https://forums.alliedmods.net/showthread.php?p=886430) for the CS2 game on C#

## Requirements
- [Metamod](https://www.sourcemm.net/downloads.php/?branch=master)
- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp/releases/tag/v129) >= v211(tested here and 100% working)

## Install
- Move the contents of folder `build/` to `addons/counterstrikesharp/plugins/medic`
- Configuration file will be generated after the plugin is launched in the folder `addons/counterstrikesharp/configs/plugins/medic/medic.json`
	
### medic.json
 - **MinHealth** - minimum HP, for the use of a medic [default: 40]
 - **Healthhealth** - HP that will be restored after use [default: 100]
 - **Cost** - the price of using a medic [default: 2000]
 - **ShowCall** - show everyone in the chat that the player used a medic [default: true]
 - **MaxUse** - number of heal uses per round [default: 2]
 - **AccessFlag** - access flag for to use commands. Set empty to allow for all [default: @css/ban]
 - **HealSuccessSound** - The path to the file being played after heal HP. Set empty for turn off this function [default: items/healthshot_success_01]
 - **HealFailureSound** - The path to the file being played after failure heal HP. Set empty for turn off this function [default: buttons/blip2]

