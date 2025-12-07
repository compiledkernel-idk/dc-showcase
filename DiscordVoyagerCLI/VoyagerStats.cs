using System;
using System.Collections.Generic;

namespace DiscordVoyagerCLI
{
    public class ChannelStats
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public int Count { get; set; }
    }

    public class VoyagerStats
    {
        public long TotalMessages { get; set; }
        public int VoiceActivity { get; set; }
        public Dictionary<string, string> Channels { get; set; } = new();
        public Dictionary<string, ChannelStats> Servers { get; set; } = new();
        public Dictionary<int, int> MessagesByYear { get; set; } = new();
        public int[] MessagesByHour { get; set; } = new int[24];
        public int[] MessagesByDayOfWeek { get; set; } = new int[7];
        public Dictionary<string, int> WordFrequency { get; set; } = new();
    }
}
