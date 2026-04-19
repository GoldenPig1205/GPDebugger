using Exiled.API.Interfaces;
using System.Collections.Generic;
using System.ComponentModel;

namespace GPDebugger.Configs
{
    public class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; } = false;

        [Description("The maximum length of a message to show in the console.")]
        public int ConsoleMessageLengthLimit { get; set; } = 100;
        [Description("The color of the console messages.")]
        public string ConsoleMessageColor { get; set; } = "white";

        [Description("List of handlers to allow. If this list has at least one value, only these handlers will be logged. (ex. Player, Server)")]
        public List<string> HandlerWhitelist { get; set; } = new();

        [Description("List of handlers to ignore. These handlers will be hidden from handler logging.")]
        public List<string> IgnoredHandlers { get; set; } = new();

        [Description("List of event args names to ignore from being printed. (ex. Player.MakingNoiseEventArgs)")]
        public List<string> IgnoredEvents { get; set; } = new()
        {
            "Player.MakingNoiseEventArgs",
            "Player.TriggeringTeslaEventArgs",
            "Item.UsingRadioPickupBatteryEventArgs",
            "Item.UsingRadioBatteryEventArgs"
        };

        [Description("Ignored network method names for logging. (ex. TargetReplyEncrypted)")]
        public List<string> IgnoredNetworkMethods { get; set; } = new()
        {
            "TargetReplyEncrypted",
            "TargetSyncGameplayData",
            "CmdSendEncryptedQuery"
        };

        [Description("Ignored network message names for logging. (ex. SpawnMessage)")]
        public List<string> IgnoredNetworkMessages { get; set; } = new()
        {
            "SpawnMessage",
            "ObjectDestroyMessage",
            "NetworkPingMessage",
            "NetworkPongMessage",
            "FpcFromClientMessage",
            "SubroutineMessage",
            "StatMessage",
            "VoiceMessage",
            "TransmitterPositionMessage",
            "ElevatorSyncMsg",
            "FpcOverrideMessage",
            "TimeSnapshotMessage",
            "EntityStateMessage",
            "FpcPositionMessage",
            "EncryptedMessageOutside"
        };
    }
}
