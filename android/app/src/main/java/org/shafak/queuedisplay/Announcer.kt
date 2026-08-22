package org.shafak.queuedisplay

import android.content.Context
import android.speech.tts.TextToSpeech
import android.util.Log
import android.webkit.JavascriptInterface
import java.util.Locale

/**
 * Speaks the board's call announcements through Android's text-to-speech.
 *
 * Android's WebView does not implement the Web Speech API — `speechSynthesis`
 * is a Chrome feature and is simply absent here — so the page's own
 * announcement would be silent inside this app. [INSTALL_SCRIPT] redirects it
 * to this bridge, leaving the page unchanged and still working normally in a
 * desktop browser.
 */
class Announcer(context: Context) {

    private var engine: TextToSpeech? = null
    private var ready = false

    init {
        engine = TextToSpeech(context.applicationContext) { status ->
            if (status != TextToSpeech.SUCCESS) {
                Log.w(TAG, "Text-to-speech unavailable; calls will be silent.")
                return@TextToSpeech
            }

            val result = engine?.setLanguage(Locale("ar"))
            if (result == TextToSpeech.LANG_MISSING_DATA || result == TextToSpeech.LANG_NOT_SUPPORTED) {
                // The device has TTS but no Arabic voice data installed. Announce
                // anyway with whatever voice exists rather than going silent.
                Log.w(TAG, "No Arabic voice installed — install one from the device's TTS settings.")
            }
            engine?.setSpeechRate(SPEECH_RATE)
            ready = true
        }
    }

    val bridge = Bridge()

    inner class Bridge {
        @JavascriptInterface
        fun speak(text: String) {
            if (!ready) return
            // Each call supersedes the previous one; a stale number must never
            // be read out over the number now on screen.
            engine?.speak(text, TextToSpeech.QUEUE_FLUSH, null, UTTERANCE_ID)
        }

        @JavascriptInterface
        fun isAvailable(): Boolean = ready
    }

    fun shutdown() {
        engine?.stop()
        engine?.shutdown()
        engine = null
    }

    companion object {
        private const val TAG = "Announcer"
        const val JS_NAME = "HQAndroid"
        private const val UTTERANCE_ID = "hq-call"
        private const val SPEECH_RATE = 0.9f

        /**
         * Injected after each page load. It swaps the page's `announce` for the
         * native bridge while leaving the rest of the display logic — the alert
         * tone, the wording, the number formatting — exactly as the page
         * defines it.
         */
        const val INSTALL_SCRIPT = """
            (function () {
                if (!window.hospitalQueueDisplay || !window.HQAndroid) return;
                if (window.hospitalQueueDisplay.__nativeTts) return;

                var board = window.hospitalQueueDisplay;
                var spokenNumber = board._spokenNumber
                    ? board._spokenNumber.bind(board)
                    : function (n) { return String(n || ''); };

                board.announce = function (ticketNumber, serviceName) {
                    try {
                        window.HQAndroid.speak(
                            'الرجاء من صاحب الرقم ' + spokenNumber(ticketNumber) +
                            ' التوجه الى عيادة ' + serviceName);
                    } catch (e) {
                        console.warn('native announce failed', e);
                    }
                };
                board.__nativeTts = true;
            })();
        """
    }
}
