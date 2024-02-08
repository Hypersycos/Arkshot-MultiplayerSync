# Arkshot-MultiplayerSync
This mod provides an API for syncing config values between different modded clients. It also provides a way to require a mod to join a lobby.

## Example Usage
[Arkshot-StaminaPlus](https://github.com/Hypersycos/Arkshot-StaminaPlus) utilises this mod to sync changed stamina values. It provides good coverage of the entire API.

# Installation
## Prerequisites
Requires the 32-bit / x86 version of BepinEx. The plugin was developed with [5.4.21](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.21/), so if in doubt use that. Further instructions can be found [here](https://docs.bepinex.dev/articles/user_guide/installation/index.html). Once installed, run then close the game, and modify `BepInEx/config/BepInEx.cfg` so that [Preloader.Entrypoint], found at the bottom of the file, reads
```
[Preloader.Entrypoint]

## The local filename of the assembly to target.
# Setting type: String
# Default value: UnityEngine.dll
Assembly = Assembly-CSharp.dll

## The name of the type in the entrypoint assembly to search for the entrypoint method.
# Setting type: String
# Default value: Application
Type = MonoBehaviour

## The name of the method in the specified entrypoint assembly and type to hook and load Chainloader from.
# Setting type: String
# Default value: .cctor
Method = .cctor
```

## Plugin
Download the [latest dll](https://github.com/Hypersycos/Arkshot-MultiplayerSync/releases/latest/download/MultiplayerSync.dll) and put it in `Arkshot/BepInEx/plugins`.
