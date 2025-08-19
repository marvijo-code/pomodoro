const fs = require('fs');
const https = require('https');
const path = require('path');

const assetsDir = path.join(__dirname, 'assets');

// Create assets directory if it doesn't exist
if (!fs.existsSync(assetsDir)) {
  fs.mkdirSync(assetsDir);
}

// Download function
const download = (url, filename) => {
  https.get(url, (response) => {
    response.pipe(fs.createWriteStream(path.join(assetsDir, filename)));
  });
};

// Download assets
const assets = {
  'favicon.png': 'https://raw.githubusercontent.com/expo/expo/master/templates/expo-template-blank/assets/favicon.png',
  'icon.png': 'https://raw.githubusercontent.com/expo/expo/master/templates/expo-template-blank/assets/icon.png',
  'splash.png': 'https://raw.githubusercontent.com/expo/expo/master/templates/expo-template-blank/assets/splash.png',
  'adaptive-icon.png': 'https://raw.githubusercontent.com/expo/expo/master/templates/expo-template-blank/assets/adaptive-icon.png',
};

// Download each asset
Object.entries(assets).forEach(([filename, url]) => {
  download(url, filename);
});

// Create a simple notification sound file
const createNotificationSound = () => {
  // Create a minimal MP3 file (1 second of silence)
  const silentMp3 = Buffer.from([
    0xFF, 0xFB, 0x90, 0x44, 0x00, 0x00, 0x00, 0x00,
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
  ]);
  
  fs.writeFileSync(path.join(assetsDir, 'notification.mp3'), silentMp3);
};

createNotificationSound();

console.log('Assets created successfully!'); 