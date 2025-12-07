const AdmZip = require('adm-zip');
const Papa = require('papaparse');
const fs = require('fs');
const path = require('path');

// Helper to calculate stats from a list of files or zip structure
async function processData(inputPath) {
    const stats = {
        servers: {},
        dms: 0,
        totalMessages: 0,
        messagesByYear: {},
        messagesByHour: new Array(24).fill(0),
        topWords: {},
        channels: {},
        voiceActivity: 0,
        years: [], // To track active years for charts
    };

    console.log(`[Parser] Reading input: ${inputPath}`);

    let fileMap = {}; // Relative Path -> Content Wrapper (Async or Sync)
    let isZip = false;

    // Detect if Zip
    if (inputPath.toLowerCase().endsWith('.zip')) {
        isZip = true;
        const zip = new AdmZip(inputPath);
        const zipEntries = zip.getEntries();

        zipEntries.forEach(entry => {
            if (!entry.isDirectory) {
                fileMap[entry.entryName] = {
                    name: entry.entryName,
                    read: () => zip.readAsTextAsync(entry)
                };
            }
        });
    } else {
        // Assume Folder
        // Recursive walk helper
        function walk(dir, rootDir) {
            const list = fs.readdirSync(dir);
            list.forEach(file => {
                const fullPath = path.join(dir, file);
                const stat = fs.statSync(fullPath);
                if (stat && stat.isDirectory()) {
                    walk(fullPath, rootDir);
                } else {
                    const relativePath = path.relative(rootDir, fullPath).replace(/\\/g, '/');
                    fileMap[relativePath] = {
                        name: relativePath,
                        read: () => Promise.resolve(fs.readFileSync(fullPath, 'utf8'))
                    }
                }
            });
        }
        if (fs.existsSync(inputPath)) {
            walk(inputPath, inputPath);
        } else {
            throw new Error("Path does not exist");
        }
    }

    // 1. Parse Index
    // Look for messages/index.json
    const indexKey = Object.keys(fileMap).find(k => k.endsWith('messages/index.json'));
    if (indexKey) {
        try {
            const text = await fileMap[indexKey].read();
            stats.channels = JSON.parse(text);
        } catch (e) {
            console.warn("Failed to parse index.json");
        }
    }

    // 2. Parse Messages
    const messageKeys = Object.keys(fileMap).filter(k => k.match(/messages\/c\d+\/messages\.csv/));
    console.log(`[Parser] Found ${messageKeys.length} message files.`);

    let processed = 0;
    for (const key of messageKeys) {
        const text = await fileMap[key].read();

        // Extract Channel ID
        const match = key.match(/c(\d+)\//);
        const channelId = match ? match[1] : 'unknown';
        const channelName = stats.channels[channelId] || `Unknown Channel (${channelId})`;

        Papa.parse(text, {
            header: true,
            skipEmptyLines: true,
            step: (row) => {
                const msg = row.data;
                if (!msg.Timestamp) return;

                stats.totalMessages++;
                const date = new Date(msg.Timestamp);
                const year = date.getFullYear();
                const hour = date.getHours();

                stats.messagesByYear[year] = (stats.messagesByYear[year] || 0) + 1;
                stats.messagesByHour[hour]++;

                // Server/Channel Aggregation
                if (!stats.servers[channelId]) {
                    stats.servers[channelId] = { id: channelId, name: channelName, count: 0 };
                }
                stats.servers[channelId].count++;

                // Voice Heuristics
                if (msg.Contents && (msg.Contents.includes('Started a call') || msg.Contents === 'Joined a call.')) {
                    stats.voiceActivity++;
                }
            }
        });

        processed++;
        if (processed % 100 === 0) process.stdout.write('.');
    }

    console.log("\n[Parser] Done.");
    return stats;
}

module.exports = { processData };
