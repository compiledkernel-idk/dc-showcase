function generateHTML(stats) {
    // Transform Stats for Chart.js
    const years = Object.keys(stats.messagesByYear).sort();
    const yearCounts = years.map(y => stats.messagesByYear[y]);

    // Sort Servers
    const topServers = Object.values(stats.servers)
        .sort((a, b) => b.count - a.count)
        .slice(0, 10);

    const now = new Date().toLocaleString();

    return `<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Discord Voyager Report</title>
    <script src="https://cdn.tailwindcss.com"></script>
    <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
    <script>
        tailwind.config = {
            theme: {
                extend: {
                    colors: {
                        discord: {
                            bg: '#313338',
                            dark: '#2B2D31',
                            darker: '#1E1F22',
                            accent: '#5865F2'
                        }
                    }
                }
            }
        }
    </script>
    <style>
        body { background-color: #313338; color: #DBDEE1; font-family: sans-serif; }
        .card { background-color: #2B2D31; border-radius: 0.75rem; padding: 1.5rem; }
    </style>
</head>
<body class="p-8 max-w-7xl mx-auto">
    <header class="mb-8 flex justify-between items-center">
        <div>
            <h1 class="text-4xl font-bold text-white mb-2">Voyager Report</h1>
            <p class="text-gray-400">Generated on ${now}</p>
        </div>
        <div class="px-4 py-2 bg-discord-accent rounded-lg font-semibold text-white">
            ${stats.totalMessages.toLocaleString()} Messages
        </div>
    </header>

    <div class="grid grid-cols-1 md:grid-cols-4 gap-6 mb-8">
        <div class="card text-center">
            <h3 class="text-gray-400 text-sm uppercase font-bold mb-2">Total Messages</h3>
            <p class="text-3xl font-bold text-white">${stats.totalMessages.toLocaleString()}</p>
        </div>
        <div class="card text-center">
            <h3 class="text-gray-400 text-sm uppercase font-bold mb-2">Active Channels</h3>
            <p class="text-3xl font-bold text-white">${Object.keys(stats.servers).length.toLocaleString()}</p>
        </div>
        <div class="card text-center">
            <h3 class="text-gray-400 text-sm uppercase font-bold mb-2">Voice Interactions</h3>
            <p class="text-3xl font-bold text-white">${stats.voiceActivity.toLocaleString()}</p>
            <p class="text-xs text-gray-500 mt-1">Calls Started/Joined</p>
        </div>
        <div class="card text-center">
            <h3 class="text-gray-400 text-sm uppercase font-bold mb-2">Top Year</h3>
            <p class="text-3xl font-bold text-white">${years.reduce((a, b) => stats.messagesByYear[a] > stats.messagesByYear[b] ? a : b, years[0] || 'N/A')}</p>
        </div>
    </div>

    <div class="grid grid-cols-1 lg:grid-cols-2 gap-8 mb-8">
        <div class="card">
            <h2 class="text-xl font-bold text-white mb-4">Activity by Year</h2>
            <canvas id="yearChart"></canvas>
        </div>
        <div class="card">
            <h2 class="text-xl font-bold text-white mb-4">Activity by Hour</h2>
            <canvas id="hourChart"></canvas>
        </div>
    </div>

    <div class="card">
        <h2 class="text-xl font-bold text-white mb-4">Top 10 Channels</h2>
        <div class="space-y-2">
            ${topServers.map((s, i) => `
                <div class="flex items-center justify-between p-3 bg-[#1E1F22] rounded hover:bg-[#35373C] transition-colors">
                    <div class="flex items-center gap-4">
                        <span class="font-mono text-gray-500 w-6">#${i + 1}</span>
                        <div>
                            <div class="font-medium text-white">${s.name}</div>
                            <div class="text-xs text-gray-500 font-mono">${s.id}</div>
                        </div>
                    </div>
                    <div class="font-bold text-discord-accent">${s.count.toLocaleString()}</div>
                </div>
            `).join('')}
        </div>
    </div>

    <script>
        // Year Chart
        new Chart(document.getElementById('yearChart'), {
            type: 'bar',
            data: {
                labels: ${JSON.stringify(years)},
                datasets: [{
                    label: 'Messages',
                    data: ${JSON.stringify(yearCounts)},
                    backgroundColor: '#5865F2',
                    borderRadius: 4
                }]
            },
            options: {
                responsive: true,
                plugins: { legend: { display: false } },
                scales: {
                    y: { grid: { color: '#3f4147' }, ticks: { color: '#949BA4' } },
                    x: { grid: { display: false }, ticks: { color: '#949BA4' } }
                }
            }
        });

        // Hour Chart
        new Chart(document.getElementById('hourChart'), {
            type: 'bar',
            data: {
                labels: ${JSON.stringify(Array.from({ length: 24 }, (_, i) => i + ':00'))},
                datasets: [{
                    label: 'Messages',
                    data: ${JSON.stringify(stats.messagesByHour)},
                    backgroundColor: '#23A559',
                    borderRadius: 4
                }]
            },
            options: {
                responsive: true,
                plugins: { legend: { display: false } },
                scales: {
                    y: { grid: { color: '#3f4147' }, ticks: { color: '#949BA4' } },
                    x: { grid: { display: false }, ticks: { color: '#949BA4' } }
                }
            }
        });
    </script>
</body>
</html>`;
}

module.exports = { generateHTML };
