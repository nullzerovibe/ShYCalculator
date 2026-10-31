importScripts('nzv/sw-core.js');
const APP_VERSION = '0.9.3';
nzvWorker({
    id: 'shy',
    version: APP_VERSION,
    shell: ['app.js', 'app.css', 'data.js', 'syntax.js', 'engine.js', 'state.js', 'editor.js',
        'views/workbench.js', 'views/history.js', 'views/help.js', 'views/snippet.js',
        'favicon/favicon.ico', 'favicon/favicon-32x32.png', 'favicon/apple-touch-icon.png',
        'favicon/android-chrome-192x192.png', 'favicon/android-chrome-512x512.png'],
});
