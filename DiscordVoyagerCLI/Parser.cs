using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CsvHelper;
using Spectre.Console;

namespace DiscordVoyagerCLI
{
    public class Parser
    {
        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "the", "be", "to", "of", "and", "a", "in", "that", "have", "i", "it", "for", "not", "on", "with", "he", "as", "you", "do", "at",
            "this", "but", "his", "by", "from", "they", "we", "say", "her", "she", "or", "an", "will", "my", "one", "all", "would", "there",
            "their", "what", "so", "up", "out", "if", "about", "who", "get", "which", "go", "me", "when", "make", "can", "like", "time", "no",
            "just", "him", "know", "take", "people", "into", "year", "your", "good", "some", "could", "them", "see", "other", "than", "then",
            "now", "look", "only", "come", "its", "over", "think", "also", "back", "after", "use", "two", "how", "our", "work", "first", "well",
            "way", "even", "new", "want", "because", "any", "these", "give", "day", "most", "us", "is", "are", "was", "were", "been", "has", "had",
            "https", "http", "com", "www", "tenor", "gif", "url"
        };

        public static async Task<VoyagerStats> Process(string inputPath, ProgressTask? progressTask = null)
        {
            var stats = new VoyagerStats();

            if (File.Exists(inputPath) && inputPath.EndsWith(".zip"))
            {
                await ProcessZip(inputPath, stats, progressTask);
            }
            else if (Directory.Exists(inputPath))
            {
                ProcessDirectory(inputPath, stats, progressTask);
            }
            else
            {
                throw new FileNotFoundException("Invalid input path");
            }

            // Post-process server names
            foreach (var server in stats.Servers.Values)
            {
                if (stats.Channels.TryGetValue(server.Id, out var name))
                {
                    server.Name = name;
                }
                else
                {
                    server.Name = $"Unknown ({server.Id})";
                }
            }

            // Post-process top words to keep only top 100
            stats.WordFrequency = stats.WordFrequency
                .OrderByDescending(x => x.Value)
                .Take(100)
                .ToDictionary(x => x.Key, x => x.Value);

            return stats;
        }

        private static void ProcessDirectory(string dir, VoyagerStats stats, ProgressTask? progressTask)
        {
            var indexPath = Path.Combine(dir, "messages", "index.json");
            if (File.Exists(indexPath))
            {
                try
                {
                    var json = File.ReadAllText(indexPath);
                    stats.Channels = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
                }
                catch {{ }}
            }

            var messageFiles = Directory.GetFiles(dir, "messages.csv", SearchOption.AllDirectories)
                .Where(f => Regex.IsMatch(f, @"messages[\\/]c\d+[\\/]messages\.csv"))
                .ToList();

            if (!File.Exists(indexPath) && messageFiles.Count == 0)
            {
                throw new InvalidDataException("This does not appear to be a valid Discord Data Package. Could not find 'messages/index.json' or any message CSV files.");
            }

            if (progressTask != null) progressTask.MaxValue = messageFiles.Count;

            var lockObj = new object();

            Parallel.ForEach(messageFiles, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, () => new VoyagerStats(), (file, loop, localStats) =>
            {
                var match = Regex.Match(file, @"c(\d+)[\\/]");
                var channelId = match.Success ? match.Groups[1].Value : "unknown";

                using var reader = new StreamReader(file);
                // CsvHelper is synchronous by default, usually faster for simple file IO in Parallel loop than async/await overhead
                ParseCsvSync(reader, channelId, localStats);
                
                lock (lockObj)
                {
                    progressTask?.Increment(1);
                }

                return localStats;
            },
            (localStats) =>
            {
                lock (lockObj)
                {
                    MergeStats(stats, localStats);
                }
            });
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
                catch {{ }}
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
                // Keep async for Zip stream
                await ParseCsvAsync(reader, channelId, stats);

                progressTask?.Increment(1);
            }
        }

        private static void ParseCsvSync(StreamReader textReader, string channelId, VoyagerStats stats)
        {
            using var csv = new CsvReader(textReader, CultureInfo.InvariantCulture);
            csv.Read();
            csv.ReadHeader();

            while (csv.Read())
            {
                ProcessRow(csv, channelId, stats);
            }
        }

        private static async Task ParseCsvAsync(StreamReader textReader, string channelId, VoyagerStats stats)
        {
            using var csv = new CsvReader(textReader, CultureInfo.InvariantCulture);
            
            await csv.ReadAsync();
            csv.ReadHeader();
            
            while (await csv.ReadAsync())
            {
                ProcessRow(csv, channelId, stats);
            }
        }

        private static void ProcessRow(CsvReader csv, string channelId, VoyagerStats stats)
        {
            stats.TotalMessages++;

            if (csv.TryGetField<DateTime>("Timestamp", out var date))
            {
                if (!stats.MessagesByYear.ContainsKey(date.Year)) stats.MessagesByYear[date.Year] = 0;
                stats.MessagesByYear[date.Year]++;
                stats.MessagesByHour[date.Hour]++;
                stats.MessagesByDayOfWeek[(int)date.DayOfWeek]++;
            }

            if (!stats.Servers.ContainsKey(channelId))
            {
                // Note: Channel names might not be available in local stats if we haven't passed the master channel list.
                // We'll fix names in post-processing or just store ID for now.
                stats.Servers[channelId] = new ChannelStats { Id = channelId, Name = channelId, Count = 0 };
            }
            stats.Servers[channelId].Count++;

            if (csv.TryGetField<string>("Contents", out var content) && !string.IsNullOrEmpty(content))
            {
                if (content.Contains("Started a call") || content == "Joined a call.")
                {
                    stats.VoiceActivity++;
                }

                // Simple word tokenizer
                var words = content.Split(new[] { ' ', '\t', '\n', '\r', '.', ',', '!', '?', '"', '(', ')', '[', ']' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in words)
                {
                    if (word.Length > 2)
                    {
                        var cleanWord = word.ToLowerInvariant();
                        if (!StopWords.Contains(cleanWord) && !Regex.IsMatch(cleanWord, @"^\d+$") && !cleanWord.StartsWith("<") && !cleanWord.EndsWith(">"))
                        {
                             if (!stats.WordFrequency.ContainsKey(cleanWord)) stats.WordFrequency[cleanWord] = 0;
                             stats.WordFrequency[cleanWord]++;
                        }
                    }
                }
            }
        }

        private static void MergeStats(VoyagerStats target, VoyagerStats source)
        {
            target.TotalMessages += source.TotalMessages;
            target.VoiceActivity += source.VoiceActivity;

            foreach (var kvp in source.MessagesByYear)
            {
                if (!target.MessagesByYear.ContainsKey(kvp.Key)) target.MessagesByYear[kvp.Key] = 0;
                target.MessagesByYear[kvp.Key] += kvp.Value;
            }

            for (int i = 0; i < 24; i++) target.MessagesByHour[i] += source.MessagesByHour[i];
            for (int i = 0; i < 7; i++) target.MessagesByDayOfWeek[i] += source.MessagesByDayOfWeek[i];

            foreach (var kvp in source.Servers)
            {
                if (!target.Servers.ContainsKey(kvp.Key))
                {
                    target.Servers[kvp.Key] = new ChannelStats { Id = kvp.Key, Name = kvp.Value.Name, Count = 0 };
                }
                target.Servers[kvp.Key].Count += kvp.Value.Count;
            }

            foreach (var kvp in source.WordFrequency)
            {
                 if (!target.WordFrequency.ContainsKey(kvp.Key)) target.WordFrequency[kvp.Key] = 0;
                 target.WordFrequency[kvp.Key] += kvp.Value;
            }
        }
    }
}