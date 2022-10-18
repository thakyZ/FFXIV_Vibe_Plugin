# FFXIV Vibe Plugin

<p align="center">
  <img width="200" height="200" src="./Data/logo.png">
</p>

A plugin for FFXIV that will let you vibe your controller or toys.

## Prerequisites
- [FFXIV QuickLauncher](https://github.com/goatcorp/FFXIVQuickLauncher).
- [7-zip](https://www.7-zip.org/).
- Visual Studio 2019 if you want to compile.
- [Intiface Desktop](https://intiface.com/desktop/)


## Installation

1. Download a [release here](https://github.com/kacie8989/FFXIV-Vibe-Plugin/releases)
(or clone this repository and build the project using Visual Studio 2019)
2. Go to FFXIV QuickLauncher folder (<kbd>WIN</kbd>+<kbd>R</kbd>, then `%appdata%\XIVLauncher\devPlugins`)
3. Extract the compressed file (eg: `FFXIV_Vibe_Plugin_v0.0.10.zip`) into FFXIV QuickLauncher `devPlugins` folder. You should have the following structure:

    - `C:\Users\<Username>\AppData\Roaming\XIVLauncher\devPlugins\FFXIV_Vibe_Plugin`.
    - The folder should contain some DLL files.

4. Start *FFXIV* using the **FFXIV QuickLauncher**
5. In game, type: `/xlplugins`. A red window should open. It's *Dalamud*, the plugin manage for FFXIV.
6. In Dalamud go to: `Developer tools` > `Plugins in developement` > `FFXIV Vibe Plugin`
7. Make sure the plugin is enabled
8. Type: `/fvp`. You should see the configuration panel.
9. Start `Intiface` and click on `Start Server`. Power on your toy, scan toys and make sure it is connected to Intiface.
10. Back in to the game, start the connect command: `/fvp connect`

Well done ! You have now FFXIV connected to Intiface and your toys. 

![ingame](./Docs/screenshot.png)

## Quick guide to make it work

| Command                | Description  |
|------------------------|--------------|
| /fvp | List all the available commands |
| /fvp connect [ip[:port]] | Connect to Intiface |
| /fvp disconnect | Disconnect from Intiface |
| /fvp scan | Force re-scanning |
| /fvp toys_list | List all the toys that are connected |
| /fvp save [file path] | Save the current configuration |
| /fvp load [file path] | Load a configuration |
| /fvp chat_list_triggers | List all the added words and intensity. |
| /fvp chat_add <intensity 0-100> <The words to match> | Add a word that will trigger update the intensity of the toy. |
| /fvp chat_remove <id> | Remove a word from the list. |
| /fvp chat_user <username> | Triggered only by the define username. |
| /fvp hp_toggle | Will vibe when some HP are missing. |
| /fvp send <0-100> | Sends some vibes to the toys |
| /fvp threshold <0-100> | Will pet to feel it to strongly. |
| /fvp stop | Stop the toys (basically sending zero intensity). |

### Examples

```
/fvp connect
/fvp add 20 hello world
/fvp add 0 stop
/fvp add 100 lol
/fvp user Alice           <==== Only names matching "...Alice..." will be able to control you. 
/fvp hp_toggle
/fvp threshold 50
```

Now open the FFXIV chat texbox, select the /freecompany channel and write: `hello world`.

Enjoy :)

## Tested controllers
- Microsoft XBox Controller

## Tested toys
- Lovense: Hush, Domi, Ferri, Diamo, Edge 2, Gush


# Discord
- [This plugin discord](https://discord.gg/CPbuGv6y) 
- [Ms. Tress #discussion](https://discord.gg/fx5pABsE)
