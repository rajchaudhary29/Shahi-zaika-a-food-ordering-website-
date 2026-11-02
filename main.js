// Forwarding entrypoint to modern full-stack application client
import('./app.js').catch(() => {
    // If not running as module, app.js is loaded via script tag
});
