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
        // Main Menu Choices
        private const string AnalyzePackageChoice = "Analyze Data Package";
        private const string ViewStatsChoice = "View Current Statistics";
        private const string GenerateReportChoice = "Generate HTML Report";
        private const string HelpChoice = "Help / About";
        private const string ExitChoice = "Exit";

        // Stats Menu Choices
        private const string GeneralOverviewChoice = "General Overview";
        private const string TopCommunitiesChoice = "Top Communities";
        private const string ActivityByYearChoice = "Activity by Year";
        private const string WeeklyActivityChoice = "Weekly Activity";
        private const string DailyActivityChoice = "Daily Activity (Hourly)";
        private const string TopWordsChoice = "Top Words";
        private const string BackToMainMenuChoice = "Back to Main Menu";

        static async Task Main(string[] args)
        {
            VoyagerStats? currentStats = null;
            string? lastReportPath = null;
            
            Console.Title = "Discord Voyager";

            if (args.Length > 0)
            {
                currentStats = await AnalyzeFlow(args[0]);
                AnsiConsole.MarkupLine("Press [green]Enter[/] to enter interactive mode...");
                Console.ReadLine();
            }

            while (true)
            {
                AnsiConsole.Clear();
                ShowHeader(currentStats);

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[bold cyan]What would you like to do?[/]")
                        .PageSize(10)
                        .HighlightStyle(new Style(foreground: Color.Cyan1, decoration: Decoration.Bold))
                        .AddChoices(new[] {
                            AnalyzePackageChoice, ViewStatsChoice, GenerateReportChoice, HelpChoice, ExitChoice
                        }));

                switch (choice)
                {
                    case AnalyzePackageChoice:
                        currentStats = await AnalyzeFlow();
                        break;
                    case ViewStatsChoice:
                        if (currentStats == null)
                        {
                            AnsiConsole.MarkupLine("[red]No data loaded. Please analyze a package first.[/]");
                            WaitKey();
                        }
                        else
                        {
                            ViewStatsMenu(currentStats);
                        }
                        break;
                    case GenerateReportChoice:
                         if (currentStats == null)
                        {
                            AnsiConsole.MarkupLine("[red]No data loaded. Please analyze a package first.[/]");
                            WaitKey();
                        }
                        else 
                        {
                            lastReportPath = GenerateReport(currentStats);
                            AnsiConsole.MarkupLine($"[green]Report saved to:[/] [link]{lastReportPath}[/]");
                            WaitKey();
                        }
                        break;
                    case HelpChoice:
                        ShowHelp();
                        break;
                    case ExitChoice:
                        AnsiConsole.MarkupLine("[bold cyan]Thanks for using Voyager! Fly safe![/]");
                        return;
                }
            }
        }

        static void ShowHeader(VoyagerStats? stats)
        {
             AnsiConsole.Write(
                new FigletText("Voyager")
                    .Color(Color.Cyan1));
            
            var rule = new Rule("[bold cyan]Discord Data Explorer[/] v1.2");
            rule.Style = Style.Parse("cyan dim");
            AnsiConsole.Write(rule);

            if (stats != null)
            {
                var statusGrid = new Grid();
                statusGrid.AddColumn(new GridColumn().NoWrap().PadRight(2));
                statusGrid.AddColumn(new GridColumn().NoWrap());
                
                statusGrid.AddRow("[dim]Loaded Data:[/]", $"[green]{stats.TotalMessages:N0} messages[/]");
                statusGrid.AddRow("[dim]Servers:[/]", $"[green]{stats.Servers.Count}[/]");
                
                AnsiConsole.Write(statusGrid);
                var bottomRule = new Rule();
                bottomRule.Style = Style.Parse("dim");
                AnsiConsole.Write(bottomRule);
            }
            AnsiConsole.WriteLine();
        }

        static async Task<VoyagerStats?> AnalyzeFlow(string? path = null)
        {
            if (path == null)
            {
                var currentDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (string.IsNullOrEmpty(currentDir)) currentDir = Directory.GetCurrentDirectory();

                while (true)
                {
                    AnsiConsole.Clear();
                    AnsiConsole.MarkupLine($"[bold cyan]Browsing:[/] {currentDir}");
                    
                    var choices = new List<ItemChoice>();
                    
                    // Option to go up
                    var parent = Directory.GetParent(currentDir);
                    if (parent != null)
                    {
                        choices.Add(new ItemChoice { Description = "[yellow]..  (Go Up)[/]", Path = parent.FullName, IsAction = true });
                    }

                    // Option to select current folder
                    choices.Add(new ItemChoice { Description = $"[green]>>  Select Current Folder[/]", Path = currentDir, IsSelect = true });

                    // Manual entry option
                    choices.Add(new ItemChoice { Description = "[grey]??  Enter path manually...[/]", IsManual = true });

                    try
                    {
                        var info = new DirectoryInfo(currentDir);
                        
                        // Directories
                        foreach (var dir in info.GetDirectories().OrderBy(d => d.Name))
                        {
                            if (!dir.Attributes.HasFlag(FileAttributes.Hidden))
                            {
                                choices.Add(new ItemChoice { Description = $"[blue]{dir.Name}/[/]", Path = dir.FullName, IsDir = true });
                            }
                        }

                        // Zip Files
                        foreach (var file in info.GetFiles("*.zip").OrderBy(f => f.Name))
                        {
                             choices.Add(new ItemChoice { Description = $"[cyan]{file.Name}[/]", Path = file.FullName, IsFile = true });
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        AnsiConsole.MarkupLine("[red]Access Denied to this folder.[/]");
                    }
                    catch (Exception ex)
                    {
                         AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                    }

                    // Render prompt
                    var selection = AnsiConsole.Prompt(
                        new SelectionPrompt<ItemChoice>()
                            .Title("Navigate to your Data Package:")
                            .PageSize(15)
                            .MoreChoicesText("[grey](Move up and down for more)[/]")
                            .UseConverter(x => x.Description)
                            .AddChoices(choices));

                    if (selection.IsManual)
                    {
                        path = AnsiConsole.Ask<string>("Enter path to [green].zip[/] or text [green]folder[/]:");
                        path = path.Replace("\"", "").Trim();
                        break;
                    }
                    else if (selection.IsSelect || selection.IsFile)
                    {
                        path = selection.Path;
                        break;
                    }
                    else if (selection.IsAction || selection.IsDir)
                    {
                        currentDir = selection.Path;
                        // Loop again to refresh view
                    }
                }
            }

            if (!File.Exists(path) && !Directory.Exists(path))
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Path not found: {path}");
                WaitKey();
                return null;
            }

            VoyagerStats? stats = null;
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
                        stats = await Parser.Process(path, task);
                    });

                sw.Stop();
                if (stats != null)
                {
                    AnsiConsole.MarkupLine($"[bold green]Success![/] Analyzed {stats.TotalMessages:N0} messages in {sw.Elapsed.TotalSeconds:F2}s.");
                }
                WaitKey();
                return stats;
            }
            catch (Exception ex)
            {
                 AnsiConsole.MarkupLine($"[red bold]Analysis Failed:[/]");
                 AnsiConsole.MarkupLine($"[red]{ex.Message}[/]");
                 WaitKey();
                 return null;
            }
        }

        static void ViewStatsMenu(VoyagerStats stats)
        {
            while (true)
            {
                AnsiConsole.Clear();
                ShowHeader(stats);
                
                var viewChoice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[bold]Select a category to view:[/]")
                        .AddChoices(new[] {
                            GeneralOverviewChoice, TopCommunitiesChoice, ActivityByYearChoice, 
                            WeeklyActivityChoice, DailyActivityChoice, TopWordsChoice, BackToMainMenuChoice
                        }));

                switch (viewChoice)
                {
                    case GeneralOverviewChoice:
                        ShowGeneralOverview(stats);
                        break;
                    case TopCommunitiesChoice:
                        ShowTopCommunities(stats);
                        break;
                    case ActivityByYearChoice:
                        ShowActivityByYear(stats);
                        break;
                    case WeeklyActivityChoice:
                        ShowWeeklyActivity(stats);
                        break;
                    case DailyActivityChoice:
                        ShowDailyActivity(stats);
                        break;
                    case TopWordsChoice:
                        ShowTopWords(stats);
                        break;
                    case BackToMainMenuChoice:
                        return;
                }
                WaitKey();
            }
        }

        private static void ShowGeneralOverview(VoyagerStats stats)
        {
            var grid = new Grid();
            grid.AddColumn();
            grid.AddColumn();
            grid.AddColumn();

            grid.AddRow(
                new Panel(new Align(new Markup($"[bold cyan]{stats.TotalMessages:N0}[/]"), HorizontalAlignment.Center, VerticalAlignment.Middle))
                    .Header("Total Messages")
                    .BorderColor(Color.Cyan1)
                    .Expand(),
                new Panel(new Align(new Markup($"[bold green]{stats.Servers.Count:N0}[/]"), HorizontalAlignment.Center, VerticalAlignment.Middle))
                    .Header("Active Servers")
                    .BorderColor(Color.Green)
                    .Expand(),
                new Panel(new Align(new Markup($"[bold yellow]{stats.VoiceActivity:N0}[/]"), HorizontalAlignment.Center, VerticalAlignment.Middle))
                    .Header("Voice Interactions")
                    .BorderColor(Color.Yellow)
                    .Expand());

            AnsiConsole.Write(grid);
        }

        private static void ShowTopCommunities(VoyagerStats stats)
        {
            var table = new Table().Border(TableBorder.Rounded);
            table.AddColumn("Rank");
            table.AddColumn("Server Name");
            table.AddColumn("Messages");

            var top = stats.Servers.Values.OrderByDescending(s => s.Count).Take(10).ToList();
            for(int i=0; i<top.Count; i++)
            {
                table.AddRow($"#{i+1}", top[i].Name, $"[cyan]{top[i].Count:N0}[/]");
            }
            AnsiConsole.Write(table);
        }

        private static void ShowActivityByYear(VoyagerStats stats)
        {
            var chart = new BarChart()
                .Width(60)
                .Label("[green]Messages per Year[/]")
                .CenterLabel();

            foreach(var kvp in stats.MessagesByYear.OrderBy(x => x.Key))
            {
                    chart.AddItem(kvp.Key.ToString(), kvp.Value, Color.Cyan1);
            }
            AnsiConsole.Write(chart);
        }

        private static void ShowWeeklyActivity(VoyagerStats stats)
        {
            var chart = new BarChart()
                .Width(60)
                .Label("[green]Messages per Day[/]")
                .CenterLabel();

            var days = new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
            for(int i=0; i<7; i++)
            {
                chart.AddItem(days[i], stats.MessagesByDayOfWeek[i], Color.Purple);
            }
            AnsiConsole.Write(chart);
        }

        private static void ShowDailyActivity(VoyagerStats stats)
        {
            var chart = new BarChart()
                .Width(60)
                .Label("[green]Activity by Hour of Day[/]")
                .CenterLabel();

            for(int i=0; i<24; i++)
            {
                // Use 12-hour format for labels if needed, or just 0-23
                chart.AddItem($"{i:00}:00", stats.MessagesByHour[i], Color.Blue);
            }
            AnsiConsole.Write(chart);
        }

        private static void ShowTopWords(VoyagerStats stats)
        {
            var table = new Table().Border(TableBorder.Rounded);
            table.AddColumn("Rank");
            table.AddColumn("Word");
            table.AddColumn("Frequency");

            var top = stats.WordFrequency.OrderByDescending(x => x.Value).Take(20).ToList();
            for(int i=0; i<top.Count; i++)
            {
                table.AddRow($"#{i+1}", top[i].Key, $"[cyan]{top[i].Value:N0}[/]");
            }
            AnsiConsole.Write(table);
        }

        static string GenerateReport(VoyagerStats stats)
        {
             try 
             {
                var html = HtmlGenerator.Generate(stats);
                var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "voyager_report.html");
                File.WriteAllText(outputPath, html);
                return outputPath;
             } 
             catch (Exception ex)
             {
                 AnsiConsole.MarkupLine($"[red]Failed to generate report:[/] {ex.Message}");
                 throw;
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

        private class ItemChoice
        {
            public required string Description { get; set; }
            public string? Path { get; set; }
            public bool IsDir { get; set; }
            public bool IsFile { get; set; }
            public bool IsAction { get; set; }
            public bool IsSelect { get; set; }
            public bool IsManual { get; set; }
        }
    }
}
