// Small enhancements for the public display board: an audible beep plus a
// best-effort Arabic voice announcement whenever a ticket is called.
// Everything here degrades silently if the browser/OS has no Arabic voice.
window.hospitalQueueDisplay = {
    beep: function () {
        try {
            const ctx = new (window.AudioContext || window.webkitAudioContext)();
            const osc = ctx.createOscillator();
            const gain = ctx.createGain();
            osc.type = "sine";
            osc.frequency.value = 880;
            osc.connect(gain);
            gain.connect(ctx.destination);
            gain.gain.setValueAtTime(0.2, ctx.currentTime);
            osc.start();
            osc.stop(ctx.currentTime + 0.35);
        } catch (e) {
            console.warn("Beep failed:", e);
        }
    },

    announce: function (ticketNumber, serviceName) {
        try {
            if (!window.speechSynthesis) return;
            const utterance = new SpeechSynthesisUtterance(
                `الرجاء من صاحب الرقم ${ticketNumber} التوجه الى عيادة ${serviceName}`);
            utterance.lang = "ar-SA";
            window.speechSynthesis.speak(utterance);
        } catch (e) {
            console.warn("Speech synthesis failed:", e);
        }
    },

    ticketCalled: function (ticketNumber, serviceName) {
        this.beep();
        this.announce(ticketNumber, serviceName);
    }
};
