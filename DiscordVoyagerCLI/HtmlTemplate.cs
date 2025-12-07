namespace DiscordVoyagerCLI
{
    public static class HtmlTemplate
    {
        public const string Template = @"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Voyager Analysis Report</title>
    <script src=""https://cdn.tailwindcss.com""></script>
    <script src=""https://cdn.jsdelivr.net/npm/chart.js""></script>
    <link href=""https://fonts.googleapis.com/css2?family=Outfit:wght@300;400;500;700&display=swap"" rel=""stylesheet"">
    <script>
        tailwind.config = {
            theme: {
                extend: {
                    fontFamily: {
                        sans: ['Outfit', 'sans-serif'],
                    },
                    colors: {
                        brand: {
                            bg: '#0F172A',
                            card: '#1E293B',
                            accent: '#6366F1',
                            success: '#10B981',
                            text: '#F8FAFC',
                            muted: '#94A3B8'
                        }
                    }
                }
            }
        }
    </script>
    <style>
        body { background-color: #0F172A; color: #F8FAFC; }
        .glass-card { 
            background: rgba(30, 41, 59, 0.7); 
            backdrop-filter: blur(10px); 
            border: 1px solid rgba(255, 255, 255, 0.1);
            border-radius: 1rem; 
            padding: 1.5rem; 
        }
    </style>
</head>
<body class=""min-h-screen p-6 md:p-12 max-w-7xl mx-auto selection:bg-brand-accent selection:text-white"">
    <header class=""mb-12 flex flex-col md:flex-row justify-between items-start md:items-center gap-4 animate-fade-in"">
        <div>
            <h1 class=""text-5xl font-bold bg-gradient-to-r from-indigo-400 to-cyan-400 bg-clip-text text-transparent mb-2"">Voyager Report</h1>
            <p class=""text-brand-muted font-light"">Analysis generated on {{generateTime}}</p>
        </div>
        <div class=""px-6 py-3 bg-brand-accent/20 border border-brand-accent/50 rounded-full font-medium text-brand-accent shadow-[0_0_15px_rgba(99,102,241,0.3)]"">
            {{totalMessages}} Total Messages
        </div>
    </header>

    <div class=""grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-12"">
        <div class=""glass-card hover:border-brand-accent/50 transition-colors duration-300"">
            <h3 class=""text-brand-muted text-xs uppercase tracking-widest font-semibold mb-3"">Messages</h3>
            <p class=""text-4xl font-bold text-white mb-1"">{{totalMessages}}</p>
            <div class=""h-1 w-full bg-gradient-to-r from-brand-accent to-transparent rounded mt-2""></div>
        </div>
        <div class=""glass-card hover:border-brand-accent/50 transition-colors duration-300"">
            <h3 class=""text-brand-muted text-xs uppercase tracking-widest font-semibold mb-3"">Active Servers</h3>
            <p class=""text-4xl font-bold text-white mb-1"">{{serverCount}}</p>
            <div class=""h-1 w-full bg-gradient-to-r from-purple-500 to-transparent rounded mt-2""></div>
        </div>
        <div class=""glass-card hover:border-brand-accent/50 transition-colors duration-300"">
            <h3 class=""text-brand-muted text-xs uppercase tracking-widest font-semibold mb-3"">Voice Count</h3>
            <p class=""text-4xl font-bold text-white mb-1"">{{voiceActivity}}</p>
            <p class=""text-xs text-brand-muted mt-2"">Calls / Joins</p>
        </div>
        <div class=""glass-card hover:border-brand-accent/50 transition-colors duration-300"">
            <h3 class=""text-brand-muted text-xs uppercase tracking-widest font-semibold mb-3"">Peak Year</h3>
            <p class=""text-4xl font-bold text-white mb-1"">{{peakYear}}</p>
            <div class=""h-1 w-full bg-gradient-to-r from-brand-success to-transparent rounded mt-2""></div>
        </div>
    </div>

    <div class=""grid grid-cols-1 lg:grid-cols-2 gap-8 mb-12"">
        <div class=""glass-card"">
            <h2 class=""text-xl font-bold text-white mb-6 flex items-center gap-2"">
                <span class=""w-2 h-8 bg-brand-accent rounded-full""></span>
                Activity Timeline
            </h2>
            <div class=""relative h-64"">
                <canvas id=""yearChart""></canvas>
            </div>
        </div>
        <div class=""glass-card"">
            <h2 class=""text-xl font-bold text-white mb-6 flex items-center gap-2"">
                <span class=""w-2 h-8 bg-brand-success rounded-full""></span>
                24-Hour Activity
            </h2>
            <div class=""relative h-64"">
                <canvas id=""hourChart""></canvas>
            </div>
        </div>
    </div>
    
    <div class=""grid grid-cols-1 lg:grid-cols-2 gap-8 mb-12"">
        <div class=""glass-card"">
            <h2 class=""text-xl font-bold text-white mb-6 flex items-center gap-2"">
                <span class=""w-2 h-8 bg-purple-500 rounded-full""></span>
                Weekly Habits
            </h2>
            <div class=""relative h-64"">
                <canvas id=""dayChart""></canvas>
            </div>
        </div>
        <div class=""glass-card"">
            <h2 class=""text-xl font-bold text-white mb-6 flex items-center gap-2"">
                <span class=""w-2 h-8 bg-pink-500 rounded-full""></span>
                Most Used Words
            </h2>
            <div class=""flex flex-wrap gap-x-4 gap-y-2 justify-center items-center h-64 overflow-y-auto pr-2 custom-scrollbar"">
                {{topWords}}
            </div>
        </div>
    </div>

    <div class=""glass-card"">
        <h2 class=""text-xl font-bold text-white mb-6 flex items-center gap-2"">
            <span class=""w-2 h-8 bg-purple-500 rounded-full""></span>
            Top Communities
        </h2>
        <div class=""grid grid-cols-1 md:grid-cols-2 gap-4"">
            {{topServers}}
        </div>
    </div>

    <footer class=""mt-12 text-center text-brand-muted text-sm opacity-50"">
        Generated by Discord Voyager CLI
    </footer>

    <script>
        Chart.defaults.color = '#94A3B8';
        Chart.defaults.borderColor = 'rgba(148, 163, 184, 0.1)';
        Chart.defaults.font.family = 'Outfit';

        new Chart(document.getElementById('yearChart'), {
            type: 'bar',
            data: {
                labels: {{jsonYears}},
                datasets: [{
                    label: 'Messages',
                    data: {{jsonYearCounts}},
                    backgroundColor: '#6366F1',
                    borderRadius: 6,
                    hoverBackgroundColor: '#818CF8'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: {
                    y: { grid: { display: true }, border: { display: false } },
                    x: { grid: { display: false }, border: { display: false } }
                }
            }
        });

        new Chart(document.getElementById('hourChart'), {
            type: 'line',
            data: {
                labels: Array.from({length:24}, (_,i) => i + ':00'),
                datasets: [{
                    label: 'Messages',
                    data: {{jsonHours}},
                    borderColor: '#10B981',
                    backgroundColor: 'rgba(16, 185, 129, 0.1)',
                    borderWidth: 3,
                    pointBackgroundColor: '#10B981',
                    fill: true,
                    tension: 0.4
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: {
                    y: { grid: { display: true }, border: { display: false } },
                    x: { grid: { display: false }, border: { display: false }, ticks: { maxTicksLimit: 8 } }
                }
            }
        });
        
        new Chart(document.getElementById('dayChart'), {
            type: 'bar',
            data: {
                labels: ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'],
                datasets: [{
                    label: 'Messages',
                    data: {{jsonDays}},
                    backgroundColor: '#A855F7',
                    borderRadius: 6,
                    hoverBackgroundColor: '#C084FC'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: {
                    y: { grid: { display: true }, border: { display: false } },
                    x: { grid: { display: false }, border: { display: false } }
                }
            }
        });
    </script>
</body>
</html>
";
    }
}
