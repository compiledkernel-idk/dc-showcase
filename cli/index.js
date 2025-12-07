const fs = require('fs');
const path = require('path');
const { processData } = require('./parser');
const { generateHTML } = require('./template');

async function main() {
    // Basic argument parsing
    const args = process.argv.slice(2);
    if (args.length === 0) {
        console.log("Usage: node cli/index.js <path-to-zip-or-folder>");
        process.exit(1);
    }

    const inputPath = args[0];
    const absPath = path.resolve(inputPath);

    if (!fs.existsSync(absPath)) {
        console.error("Error: Path does not exist -> " + absPath);
        process.exit(1);
    }

    try {
        console.log("Voyager CLI - Discord Data Analyzer");
        console.log("-----------------------------------");

        const startTime = Date.now();
        const stats = await processData(absPath);
        const duration = ((Date.now() - startTime) / 1000).toFixed(2);

        console.log(`[Success] Analyzed ${stats.totalMessages} messages in ${duration}s.`);

        const html = generateHTML(stats);
        const outputPath = path.resolve(process.cwd(), 'report.html');

        fs.writeFileSync(outputPath, html);
        console.log(`[Output] Report generated at: ${outputPath}`);
        console.log("Open this file in your browser to view the beautiful report!");

    } catch (e) {
        console.error("Fatal Error:", e);
        process.exit(1);
    }
}

main();
