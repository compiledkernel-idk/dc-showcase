using System;
using System.Linq;
using System.Text.Json;
using System.Text;
using System.Collections.Generic;

namespace DiscordVoyagerCLI
{
    public static class HtmlGenerator
    {
        public static string Generate(VoyagerStats stats)
        {
            var years = stats.MessagesByYear.Keys.OrderBy(k => k).ToArray();
            var yearCounts = years.Select(y => stats.MessagesByYear[y]).ToArray();
            
            var topServers = stats.Servers.Values
                .OrderByDescending(s => s.Count)
                .Take(10)
                .ToList();

            var generateTime = DateTime.Now.ToString("MMMM dd, yyyy HH:mm");

            var jsonYears = JsonSerializer.Serialize(years);
            var jsonYearCounts = JsonSerializer.Serialize(yearCounts);
            var jsonHours = JsonSerializer.Serialize(stats.MessagesByHour);
            var jsonDays = JsonSerializer.Serialize(stats.MessagesByDayOfWeek);
            
            // Normalize word sizes for tag cloud
            var topWords = stats.WordFrequency.OrderByDescending(x => x.Value).Take(60).ToList();
            var maxWordCount = topWords.Any() ? topWords.First().Value : 1;
            
            string GetWordSizeClass(int count)
            {
                var percent = (double)count / maxWordCount;
                if (percent > 0.8) return "text-4xl text-brand-accent font-bold";
                if (percent > 0.6) return "text-3xl text-white font-semibold";
                if (percent > 0.4) return "text-2xl text-slate-300 font-medium";
                if (percent > 0.2) return "text-xl text-slate-400";
                return "text-lg text-slate-500";
            }

            var topServersHtml = new StringBuilder();
            foreach (var (server, i) in topServers.Select((s, i) => (s, i)))
            {
                topServersHtml.Append($@"
                <div class=""flex items-center justify-between p-4 bg-slate-800/50 rounded-xl hover:bg-slate-800 transition-all border border-transparent hover:border-slate-600"">
                    <div class=""flex items-center gap-4"">
                        <span class=""flex items-center justify-center w-8 h-8 rounded-lg bg-slate-700 text-brand-muted font-mono font-bold"">#{i + 1}</span>
                        <div>
                            <div class=""font-medium text-white text-lg"">{server.Name}</div>
                            <div class=""text-xs text-brand-muted font-mono opacity-60"">{server.Id}</div>
                        </div>
                    </div>
                    <div class=""font-bold text-brand-accent bg-brand-accent/10 px-3 py-1 rounded-lg"">{server.Count:N0}</div>
                </div>");
            }

            var topWordsHtml = new StringBuilder();
            foreach (var word in topWords)
            {
                topWordsHtml.Append($"<span class='{GetWordSizeClass(word.Value)} hover:text-brand-accent transition-colors cursor-default' title='{word.Value:N0} times'>{word.Key}</span> ");
            }

            return HtmlTemplate.Template
                .Replace("{{generateTime}}", generateTime)
                .Replace("{{totalMessages}}", stats.TotalMessages.ToString("N0"))
                .Replace("{{serverCount}}", stats.Servers.Count.ToString("N0"))
                .Replace("{{voiceActivity}}", stats.VoiceActivity.ToString("N0"))
                .Replace("{{peakYear}}", (years.Length > 0 ? years.OrderByDescending(y => stats.MessagesByYear[y]).First().ToString() : "N/A"))
                .Replace("{{jsonYears}}", jsonYears)
                .Replace("{{jsonYearCounts}}", jsonYearCounts)
                .Replace("{{jsonHours}}", jsonHours)
                .Replace("{{jsonDays}}", jsonDays)
                .Replace("{{topWords}}", topWordsHtml.ToString())
                .Replace("{{topServers}}", topServersHtml.ToString());
        }
    }
}