using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Spectre.Console;

namespace DiscordVoyagerCLI
{
    class Program
    {
        private static VoyagerStats? _currentStats;
        private static string? _lastReportPath;

        static async Task Main(string[] args)
        {
            // Set console title
            Console.Title = "Discord Voyager";

            // If args provided, try to load immediately (legacy support)
            if (args.Length > 0)
            {
                await AnalyzeFlow(args[0]);
                AnsiConsole.MarkupLine("Press [green]Enter[/] to enter interactive mode...");
                Console.ReadLine();
            }

            while (true)
            {
                AnsiConsole.Clear();
                ShowHeader();

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[bold cyan]What would you like to do?[/]")
                        .PageSize(10)
                        .HighlightStyle(new Style(foreground: Color.Cyan1, decoration: Decoration.Bold))
                        .AddChoices(new[] {
                            "Analyze Data Package",
                            "View Current Statistics",
                            "Generate HTML Report",
                            "Help / About",
                            "Exit"
                        }));

                switch (choice)
                {
                    case "Analyze Data Package":
                        await AnalyzeFlow();
                        break;
                    case "View Current Statistics":
                        if (_currentStats == null)
                        {
                            AnsiConsole.MarkupLine("[red]No data loaded. Please analyze a package first.[/]");
                            WaitKey();
                        }
                        else
                        {
                            ViewStatsMenu();
                        }
                        break;
                    case "Generate HTML Report":
                         if (_currentStats == null)
                        {
                            AnsiConsole.MarkupLine("[red]No data loaded. Please analyze a package first.[/]");
                            WaitKey();
                        }
                        else 
                        {
                            GenerateReport();
                            WaitKey();
                        }
                        break;
                    case "Help / About":
                        ShowHelp();
                        break;
                    case "Exit":
                        AnsiConsole.MarkupLine("[grey]Closing application...[/]");
                        return;
                }
            }
        }

        static void ShowHeader()
        {
             AnsiConsole.Write(
                new FigletText("Voyager")
                    .Color(Color.Cyan1));
            AnsiConsole.MarkupLine("[bold cyan]Discord Data Explorer[/] v1.1");
            AnsiConsole.MarkupLine("[dim]License: GPL-v3[/]");
            AnsiConsole.MarkupLine("[dim]------------------------------------------------[/]");
            if (_currentStats != null)
            {
                AnsiConsole.MarkupLine($"[green]Loaded:[/] {_currentStats.TotalMessages:N0} msgs over {_currentStats.Servers.Count} servers.");
            }
            AnsiConsole.WriteLine();
        }

        static async Task AnalyzeFlow(string? path = null)
        {
            if (path == null)
            {
                path = AnsiConsole.Ask<string>("Enter path to [green].zip[/] or text [green]folder[/]:");
                path = path.Replace("\"", ""); // Handle quotes from drag-drop
            }

            if (!File.Exists(path) && !Directory.Exists(path))
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Path not found: {path}");
                WaitKey();
                return;
            }

            try
            {
                var sw = Stopwatch.StartNew();
                
                await AnsiConsole.Progress()
                    .AutoRefresh(true) 
                    .Columns(new ProgressColumn[] 
                    {
                        new TaskDescriptionColumn(),    
                        new ProgressBarColumn(),        
                        new PercentageColumn(),         
                        new SpinnerColumn(),            
                    })
                    .StartAsync(async ctx => 
                    {
                        var task = ctx.AddTask($"[green]Parsing {Path.GetFileName(path)}...[/]");
                        _currentStats = await Parser.Process(path, task);
                    });

                sw.Stop();
                if (_currentStats != null)
                {
                    AnsiConsole.MarkupLine($"[bold green]Success![/] Analyzed {_currentStats.TotalMessages:N0} messages in {sw.Elapsed.TotalSeconds:F2}s.");
                }
                WaitKey();
            }
            catch (Exception ex)
            {
                 AnsiConsole.MarkupLine($"[red bold]Analysis Failed:[/]");
                 AnsiConsole.MarkupLine($"[red]{ex.Message}[/]");
                 WaitKey();
            }
        }

        static void ViewStatsMenu()
        {
            while (true)
            {
                AnsiConsole.Clear();
                ShowHeader();
                
                var viewChoice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[bold]Select a category to view:[/]")
                        .AddChoices(new[] {
                            "General Overview",
                            "Top Communities",
                            "Activity by Year",
                            "Weekly Activity",
                            "Top Words",
                            "Back to Main Menu"
                        }));
                
                if (viewChoice == "Back to Main Menu") return;

                if (viewChoice == "General Overview")
                {
                    var table = new Table().Border(TableBorder.Rounded);
                    table.AddColumn("Metric");
                    table.AddColumn("Value");
                    table.AddRow("Total Messages", $"[cyan]{_currentStats!.TotalMessages:N0}[/]");
                    table.AddRow("Active Servers", $"[green]{_currentStats.Servers.Count:N0}[/]");
                    table.AddRow("Voice Interactions", $"[yellow]{_currentStats.VoiceActivity:N0}[/]");
                    AnsiConsole.Write(table);
                }
                else if (viewChoice == "Top Communities")
                {
                    var table = new Table().Border(TableBorder.Rounded);
                    table.AddColumn("Rank");
                    table.AddColumn("Server Name");
                    table.AddColumn("Messages");

                    var top = _currentStats!.Servers.Values.OrderByDescending(s => s.Count).Take(10).ToList();
                    for(int i=0; i<top.Count; i++)
                    {
                        table.AddRow($"#{i+1}", top[i].Name, $"[cyan]{top[i].Count:N0}[/]");
                    }
                    AnsiConsole.Write(table);
                }
                else if (viewChoice == "Activity by Year")
                {
                    var chart = new BarChart()
                        .Width(60)
                        .Label("[green]Messages per Year[/]")
                        .CenterLabel();

                    foreach(var kvp in _currentStats!.MessagesByYear.OrderBy(x => x.Key))
                    {
                         chart.AddItem(kvp.Key.ToString(), kvp.Value, Color.Cyan1);
                    }
                    AnsiConsole.Write(chart);
                }
                else if (viewChoice == "Weekly Activity")
                {
                    var chart = new BarChart()
                        .Width(60)
                        .Label("[green]Messages per Day[/]")
                        .CenterLabel();

                    var days = new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
                    for(int i=0; i<7; i++)
                    {
                        chart.AddItem(days[i], _currentStats!.MessagesByDayOfWeek[i], Color.Purple);
                    }
                    AnsiConsole.Write(chart);
                }
                else if (viewChoice == "Top Words")
                {
                    var table = new Table().Border(TableBorder.Rounded);
                    table.AddColumn("Rank");
                    table.AddColumn("Word");
                    table.AddColumn("Frequency");

                    var top = _currentStats!.WordFrequency.OrderByDescending(x => x.Value).Take(20).ToList();
                    for(int i=0; i<top.Count; i++)
                    {
                        table.AddRow($"#{i+1}", top[i].Key, $"[cyan]{top[i].Value:N0}[/]");
                    }
                    AnsiConsole.Write(table);
                }

                WaitKey();
            }
        }

        static void GenerateReport()
        {
             try 
             {
                var html = HtmlGenerator.Generate(_currentStats!);
                var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "voyager_report.html");
                File.WriteAllText(outputPath, html);
                _lastReportPath = outputPath;
                AnsiConsole.MarkupLine($"[green]Report saved to:[/] [link]{outputPath}[/]");
             } 
             catch (Exception ex)
             {
                 AnsiConsole.MarkupLine($"[red]Failed to generate report:[/] {ex.Message}");
             }
        }

        static void ShowHelp()
        {
            var panel = new Panel(
                "1. Select [bold]Analyze Data Package[/]\n" + 
                "2. Drag and drop your Discord Package (.zip) or Folder\n" + 
                "3. Wait for processing\n" + 
                "4. Browse stats or generate the HTML report!")
                .Header("Quick Start")
                .BorderColor(Color.Cyan1);
            
            AnsiConsole.Write(panel);
            WaitKey();
        }

        static void WaitKey()
        {
            AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
            Console.ReadKey(true);
        }
    }
}
