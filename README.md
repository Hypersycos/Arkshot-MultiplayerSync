# Arkshot-MultiplayerSync
This mod provides an API for syncing config values between different modded clients. It also provides a way to require a mod to join a lobby.

## Example Usage
[Arkshot-StaminaPlus](https://github.com/Hypersycos/Arkshot-StaminaPlus) utilises this mod to sync changed stamina values. It provides good coverage of the entire API.

# Installation
## Prerequisites
The only requirement is BepInEx. The plugin was developed with [5.4.21](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.21/), so if in doubt use that. Make sure to download the 32-bit / x86 edition. Further instructions can be found [here](https://docs.bepinex.dev/articles/user_guide/installation/index.html). Arkshot also requires the alternative endpoint, detailed [here](https://docs.bepinex.dev/articles/user_guide/troubleshooting.html#change-the-entry-point-1)

## Plugin
Download the [latest dll](https://github.com/Hypersycos/Arkshot-MultiplayerSync/releases/latest/download/MultiplayerSync.dll) and put it in Arkshot/BepInEx/plugins.
