// Fullscreen helper for the kiosk screen. Browsers only grant
// requestFullscreen() from inside a user gesture, so this is always called
// from a click handler.
window.hospitalQueueKiosk = {
    enter: function () {
        const el = document.documentElement;
        if (el.requestFullscreen) {
            el.requestFullscreen().catch(() => { });
        }
    },
};
