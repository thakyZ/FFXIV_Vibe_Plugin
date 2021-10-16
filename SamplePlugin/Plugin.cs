using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Buttplug;
using System.Threading.Tasks;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using System.Linq;

namespace SamplePlugin
{
    public sealed class Plugin : IDalamudPlugin
    {
        [PluginService]
        [RequiredVersion("1.0")]
        private ChatGui Chat { get; init; }
        private Buttplug.ButtplugClient buttplugClient;
        private class Trigger
        {
            public Trigger(int intensity, string text)
            {
                Intensity = intensity;
                ToMatch = text;
            }

            public int Intensity { get; }
            public string ToMatch { get; }

            public override string ToString()
            {
                return $"Trigger(intensity: {Intensity}, text: {ToMatch})";
            }
            public bool Equals(Trigger that)
            {
                return this.Intensity == that.Intensity && this.ToMatch.Equals(that.ToMatch);
            }
        }

        private readonly List<Trigger> Triggers = new();
     
        public string Name => "Buttplug Triggers";

        private const string commandName = "/buttplugtriggers";

        private DalamudPluginInterface PluginInterface { get; init; }
        private CommandManager CommandManager { get; init; }
        private Configuration Configuration { get; init; }
        private PluginUI PluginUi { get; init; }

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;

            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);

            // you might normally want to embed resources and load them from the manifest stream
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var imagePath = Path.Combine(Path.GetDirectoryName(assemblyLocation)!, "goat.png");
            var goatImage = this.PluginInterface.UiBuilder.LoadImage(imagePath);
            this.PluginUi = new PluginUI(this.Configuration, goatImage);

            this.CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "A simple text triggers to buttplug.io plugin."
            });

            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
            Chat.ChatMessage += CheckForTriggers; // XXX: o.o
        }

        private void CheckForTriggers(XivChatType type, uint senderId, ref SeString sender, ref SeString _message, ref bool isHandled)
        {
            string message = _message.ToString();
            var matchingintensities = this.Triggers.Where(t => message.Contains(t.ToMatch) );
            int intensity = matchingintensities.Select(t => t.Intensity).Max();
            buttplugClient.Devices[0].SendVibrateCmd(intensity / 100.0f);
        }

        public void Dispose()
        {
            this.PluginUi.Dispose();
            this.CommandManager.RemoveHandler(commandName);
            this.buttplugClient.DisconnectAsync();
        }

        private void Print(string message)
        {
            Chat.Print(message);
        }

        private void PrintError(string error)
        {
            Chat.PrintError(error);
        }

        private void PrintHelp(string command)
        {
            string helpMessage =
                $@"Usage: {command} list
       {command} list
       {command} add <intensity 0-100> <trigger text> # where <trigger text> is / \w .+ (?=$) /
       {command} remove <id>
       {command} connect [ip[:port]] # defaults to 'localhost:12345', the intiface default
";
            Chat.Print(helpMessage);
        }

        private void OnCommand(string command, string args)
        {
            if (args.Length == 0)
            {
                PrintHelp(command);
            }
            switch (args.Substring(0, 3))
            {
                case "lis":
                    ListTriggers();
                    break;
                case "add":
                    AddTrigger(args);
                    break;
                case "rem":
                    RemoveTrigger(args);
                    break;
                case "con":
                    ConnectButtplugs(args);
                    break;
                case "dis":
                    DisconnectButtplugs();
                    break;
                default:
                    Print($"Unknown subcommand: {args}");
                    break;
            }

        }

        private void DisconnectButtplugs()
        {
            Task task = this.buttplugClient.DisconnectAsync();
            task.Wait();
            Print("Disconnected!");
        }

        private void ConnectButtplugs(string args)
        {
            try
            {
                this.buttplugClient = new("buttplugtriggers-dalamud");
            }
            catch (Exception e)
            {
                PrintError($"Can't load buttplug.io: {e.Message}");
            }
            buttplugClient.DeviceAdded += ButtplugClient_DeviceAdded;
            string host = "localhost";
            string port = ":12345";
            string hostandport = host + port;
            if (args.Contains(" "))
            {
                hostandport = args.Split(" ", 2)[1];
                if(!hostandport.Contains(":"))
                {
                    hostandport += port;
                }
            }
            var uri = new Uri($"ws://{hostandport}/buttplug");
            var connector = new ButtplugWebsocketConnectorOptions(uri);
            Print($"Connecting to {hostandport}...");
            Task task = buttplugClient.ConnectAsync(connector);
            task.Wait();
            if(buttplugClient.Connected)
            {
                Print($"Connected!");
            }
            else
            {
                PrintError("Failed connecting (TODO: Why?");
            }
            Print("Scanning for devices...");
            buttplugClient.StartScanningAsync();
        }

        private void ButtplugClient_DeviceAdded(object? sender, DeviceAddedEventArgs e)
        {
            Print("Added device: " + e.Device.Name);
        }

        private void RemoveTrigger(string args)
        {
            int id = -1;
            try
            {
                id = int.Parse(args.Split(" ")[1]);
                if (id < 0)
                {
                    throw new FormatException(); // XXX: exceptionally exceptional control flow please somnenoee hehhehjel;;  ,.-
                }
            } 
            catch(FormatException)
            {
                PrintError("Malformed argument for [remove]");
                return; // XXX: exceptional control flow
            }
            Trigger removed = Triggers[id];
            Triggers.RemoveAt(id);
            Print($@"Removed Trigger: {removed}");
        }

        private void AddTrigger(string args)
        {
            string[] blafuckcsharp;
            int intensity;
            string text;
            try
            {
                blafuckcsharp = args.Split(" ", 3);
                intensity = int.Parse(blafuckcsharp[1]);
                text = blafuckcsharp[2];
            }
            catch (Exception e) when (e is FormatException or IndexOutOfRangeException)
            {
                PrintError($"Malformed arguments for [add].");
                return; // XXX: exceptional control flow
            }
            Trigger newTrigger = new(intensity, text);
            Triggers.Add(newTrigger);
            Print($"Added Trigger: {newTrigger}");
        }

        private void ListTriggers()
        {
            string message =
                @"Configured triggers:
ID   Intensity     Text Match
";
            for (int i = 0; i < Triggers.Count; ++i)
            {
                message += $"[{i}]\t{Triggers[i].Intensity}\t{Triggers[i].ToMatch}\n";
            }
            Chat.Print(message);
        }

        private void DrawUI()
        {
            this.PluginUi.Draw();
        }

        private void DrawConfigUI()
        {
            this.PluginUi.SettingsVisible = true;
        }
    }
}
