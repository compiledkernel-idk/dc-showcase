using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Threading.Tasks;
using CsvHelper;
using System.Globalization;
using System.Text.RegularExpressions;
using Spectre.Console; 

namespace DiscordVoyagerCLI
{
    public class Parser
    {
        public static async Task<VoyagerStats> Process(string inputPath, ProgressTask? progressTask = null)
        {
            var stats = new VoyagerStats();
            
            if (File.Exists(inputPath) && inputPath.EndsWith(".zip"))
            {
                await ProcessZip(inputPath, stats, progressTask);
            }
            else if (Directory.Exists(inputPath))
            {
                await ProcessDirectory(inputPath, stats, progressTask);
            }
            else
            {
                throw new FileNotFoundException("Invalid input path");
            }

            return stats;
        }

        private static async Task ProcessDirectory(string dir, VoyagerStats stats, ProgressTask? progressTask)
        {
            var indexPath = Path.Combine(dir, "messages", "index.json");
            if (File.Exists(indexPath))
            {
                try 
                {
                    var json = await File.ReadAllTextAsync(indexPath);
                    stats.Channels = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
                }
                catch {}
            }

            var messageFiles = Directory.GetFiles(dir, "messages.csv", SearchOption.AllDirectories)
                .Where(f => Regex.IsMatch(f, @"messages[\\/]c\d+[\\/]messages\.csv"))
                .ToList();

            if (!File.Exists(indexPath) && messageFiles.Count == 0)
            {
                throw new InvalidDataException("This does not appear to be a valid Discord Data Package. Could not find 'messages/index.json' or any message CSV files.");
            }

            if (progressTask != null) progressTask.MaxValue = messageFiles.Count;

            foreach (var file in messageFiles)
            {
                var match = Regex.Match(file, @"c(\d+)[\\/]");
                var channelId = match.Success ? match.Groups[1].Value : "unknown";
                
                using var reader = new StreamReader(file);
                await ParseCsv(reader, channelId, stats);
                
                progressTask?.Increment(1);
            }
        }

        private static async Task ProcessZip(string zipPath, VoyagerStats stats, ProgressTask? progressTask)
        {
            using var archive = ZipFile.OpenRead(zipPath);

            var indexEntry = archive.GetEntry("messages/index.json");
            if (indexEntry != null)
            {
                using var stream = indexEntry.Open();
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();
                try 
                {
                    stats.Channels = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
                }
                catch {}
            }

            var messageEntries = archive.Entries
                .Where(e => Regex.IsMatch(e.FullName, @"messages/c\d+/messages\.csv"))
                .ToList();

            if (indexEntry == null && messageEntries.Count == 0)
            {
                throw new InvalidDataException("This does not appear to be a valid Discord Data Package. Could not find 'messages/index.json' or any message CSV files.");
            }

            if (progressTask != null) progressTask.MaxValue = messageEntries.Count;

            foreach (var entry in messageEntries)
            {
                 var match = Regex.Match(entry.FullName, @"c(\d+)/");
                 var channelId = match.Success ? match.Groups[1].Value : "unknown";

                 using var stream = entry.Open();
                 using var reader = new StreamReader(stream);
                 await ParseCsv(reader, channelId, stats);

                 progressTask?.Increment(1);
            }
        }

        private static async Task ParseCsv(StreamReader textReader, string channelId, VoyagerStats stats)
        {
            using var csv = new CsvReader(textReader, CultureInfo.InvariantCulture);
            
            await csv.ReadAsync();
            csv.ReadHeader();
            
            while (await csv.ReadAsync())
            {
                stats.TotalMessages++;
                
                if (csv.TryGetField<DateTime>("Timestamp", out var date)) 
                {
                     if (!stats.MessagesByYear.ContainsKey(date.Year)) stats.MessagesByYear[date.Year] = 0;
                     stats.MessagesByYear[date.Year]++;
                     stats.MessagesByHour[date.Hour]++;
                }

                if (!stats.Servers.ContainsKey(channelId))
                {
                    var name = stats.Channels.ContainsKey(channelId) ? stats.Channels[channelId] : $"Unknown ({channelId})";
                    stats.Servers[channelId] = new ChannelStats { Id = channelId, Name = name, Count = 0 };
                }
                stats.Servers[channelId].Count++;

                if (csv.TryGetField<string>("Contents", out var content) && !string.IsNullOrEmpty(content))
                {
                    if (content.Contains("Started a call") || content == "Joined a call.")
                    {
                        stats.VoiceActivity++;
                    }
                }
            }
        }
    }
}
