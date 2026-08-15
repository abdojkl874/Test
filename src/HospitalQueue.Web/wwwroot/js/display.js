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

    // Browsers populate the voice list asynchronously, so the very first
    // announcement after a page load often finds getVoices() still empty.
    // Wait (briefly) for the voiceschanged event rather than speaking with
    // whatever default voice is active, which is usually a non-Arabic one.
    _withVoices: function (callback) {
        const voices = window.speechSynthesis.getVoices();
        if (voices && voices.length > 0) {
            callback(voices);
            return;
        }

        let done = false;
        const run = () => {
            if (done) return;
            done = true;
            callback(window.speechSynthesis.getVoices() || []);
        };

        window.speechSynthesis.addEventListener("voiceschanged", run, { once: true });
        setTimeout(run, 1000);
    },

    // Ticket numbers are stored as "B-001", which a speech engine reads out
    // literally: the dash becomes an audible "dash"/pause and the padding
    // becomes "zero zero one". Spoken form keeps the service code and the
    // significant digits only, so "B-001" is announced as "B 1".
    _spokenNumber: function (ticketNumber) {
        return String(ticketNumber || "")
            .split("-")
            .map(part => /^\d+$/.test(part) ? String(parseInt(part, 10)) : part)
            .filter(part => part.length > 0)
            .join(" ");
    },

    announce: function (ticketNumber, serviceName) {
        try {
            if (!window.speechSynthesis) return;

            const spoken = this._spokenNumber(ticketNumber);
            const text = `الرجاء من صاحب الرقم ${spoken} التوجه الى عيادة ${serviceName}`;

            this._withVoices(function (voices) {
                try {
                    // A call already being announced is stale the moment a new
                    // number is called, so drop it instead of queueing behind it.
                    window.speechSynthesis.cancel();

                    const utterance = new SpeechSynthesisUtterance(text);
                    utterance.lang = "ar-SA";
                    utterance.rate = 0.9;

                    const arabic = voices.find(v => v.lang && v.lang.toLowerCase().startsWith("ar"));
                    if (arabic) {
                        utterance.voice = arabic;
                    } else {
                        console.warn("No Arabic voice installed — announcing with the default voice.");
                    }

                    window.speechSynthesis.speak(utterance);
                } catch (e) {
                    console.warn("Speech synthesis failed:", e);
                }
            });
        } catch (e) {
            console.warn("Speech synthesis failed:", e);
        }
    },

    ticketCalled: function (ticketNumber, serviceName) {
        this.beep();
        this.announce(ticketNumber, serviceName);
    }
};
