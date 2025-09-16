import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';

/**
 * Recursively copies a directory from source to destination
 */
function copyDir(src, dest) {
    if (!fs.existsSync(dest)) {
        fs.mkdirSync(dest, { recursive: true });
    }

    fs.readdirSync(src).forEach(item => {
        const srcPath = path.join(src, item);
        const destPath = path.join(dest, item);

        if (fs.statSync(srcPath).isDirectory()) {
            copyDir(srcPath, destPath);
        } else {
            fs.copyFileSync(srcPath, destPath);
        }
    });
}

const sourceDir = 'dist';
const allDestinations = [
    '../../ui-webview-windows/bin/Debug/net9.0-windows/src/dist',
    '../../ui-photino-linux/bin/Debug/net9.0/wwwroot',
    '../../ui-photino-linux/wwwroot'
];

const destinations = os.platform() === 'win32'
    ? allDestinations
    : allDestinations.filter(dest => !dest.includes('net9.0-windows'));

console.log('Starting file copy process...');

destinations.forEach(dest => {
    try {
        console.log(`Copying ${sourceDir} to ${dest}...`);
        copyDir(sourceDir, dest);
        console.log(`✓ Successfully copied to ${dest}`);
    } catch (error) {
        console.error(`✗ Failed to copy to ${dest}:`, error.message);
        process.exit(1);
    }
});

console.log('All files copied successfully!');