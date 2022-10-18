# FFXIV Buttplug

A buttplug plugin for FFXIV that will change the intensity of your toys when a word from the /freecompany is matched.

Credits: I have not done this plugin. It was initialy done by 
[Ms. Tress](https://discord.com/channels/793663567424520194/793663567865970701/793673706411917363) and updated by [Coranila](https://github.com/crnilaaaa/SamplePlugin).
I have only tried to document the installation process which took me multiple attempts and hours to make it work.

## Prerequisites
- [FFXIV QuickLauncher](https://github.com/goatcorp/FFXIVQuickLauncher).
- [7-zip](https://www.7-zip.org/).
- Visual Studio 2019 if you want to compile.
- [Intiface Desktop](https://intiface.com/desktop/)


## Installation

1. Download a [release here](https://github.com/kacie8989/FFXIV_BP/releases)
(or clone this repository and build the project using Visual Studio 2019)
2. Go to FFXIV QuickLauncher folder (<kbd>WIN</kbd>+<kbd>R</kbd>, then `%appdata%\XIVLauncher\devPlugins`)
3. Extract the compressed file (eg: `ffxiv-bp-v0.0.3.7z`) into FFXIV QuickLauncher `devPlugins` folder. You should have the following structure:

    - `C:\Users\<Username>\AppData\Roaming\XIVLauncher\devPlugins\FFXIV_BP`
    - The folder should contain ~30 dlls

4. Start *FFXIV* using the **FFXIV QuickLauncher**
5. In game, type: `/xlplugins`. A red window should open. It's *Dalamud*, the plugin manage for FFXIV.
6. In Dalamud go to: `Developer tools` > `Plugins in developement` > `Buttplug Triggers`
7. Make sure the plugin is enabled
8. Type: `/buttplugtriggers`. You should see a list of commands.
9. Start `Intiface` and click on `Start Server`. Power on your toy, scan toys and make sure it is connected to Intiface.
10. Back in to the game, start the connect command: `/buttplugtriggers connect`

Well done ! You have now FFXIV connected to Intiface and your toys. 

![ingame](https://cdn.discordapp.com/attachments/794224288826654780/899748631119282176/PXL_20211018_195852122.MP2.jpg)

## Quick guide to make it work

| Command                | Description  |
|------------------------|--------------|
| /buttplugtriggers      | List all the available commands |
| /buttplugtriggers list | List all the added words and intensity |
| /buttplugtriggers add <intensity 0-100> <The words to match> | Add a word that will trigger update the intensity of the toy. |
| /buttplugtriggers user <UserName> | Triggered only by the define username. |

### Examples

```
/buttplugtriggers connect
/buttplugtriggers add 20 hello world
/buttplugtriggers add 0 stop
/buttplugtriggers add 100 lol
/buttplugtriggers user Alice           <==== Only names matching "...Alice..." will be able to control you. 
```

Now open the FFXIV chat texbox, select the /freecompany channel and write: `hello world`.

Enjoy :)

# Discord

[Ms. Tress #discussion](https://discord.gg/fx5pABsE)
