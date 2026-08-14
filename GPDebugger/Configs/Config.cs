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

        [Description("How often the pointer Transform inspector refreshes, in seconds.")]
        public float PointerInspectorUpdateInterval { get; set; } = 0.25f;

        [Description("Maximum raycast distance used by the pointer Transform inspector.")]
        public float PointerInspectorMaxDistance { get; set; } = 200f;

        [Description("Horizontal HintServiceMeow coordinate used by the pointer Transform inspector.")]
        public float PointerInspectorXCoordinate { get; set; } = 0f;

        [Description("Vertical HintServiceMeow coordinate used by the pointer Transform inspector.")]
        public float PointerInspectorYCoordinate { get; set; } = 400f;

        [Description("Font size used by the pointer Transform inspector.")]
        public int PointerInspectorFontSize { get; set; } = 15;

        [Description("Maximum number of lines shown by the pointer Transform inspector. The final line is used for an omission notice when truncated.")]
        public int PointerInspectorMaxLines { get; set; } = 40;

        [Description("Maximum distance from the pointer ray used to select a Transform that has neither a Collider nor a Renderer.")]
        public float PointerInspectorTransformSelectionRadius { get; set; } = 0.35f;

        [Description("How long Renderer and Transform scene search results are cached, in seconds.")]
        public float PointerInspectorSceneCacheLifetime { get; set; } = 5f;

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
