package org.shafak.queuedisplay

import android.content.Context

/** The one thing this app needs configured: where the server lives. */
class Prefs(context: Context) {

    private val store = context.getSharedPreferences(NAME, Context.MODE_PRIVATE)

    var serverUrl: String?
        get() = store.getString(KEY_URL, null)
        set(value) = store.edit().putString(KEY_URL, value?.trim()).apply()

    /**
     * Device key from the admin settings screen. With it the board opens
     * straight away; without it the board asks for the shared password on every
     * restart, which is unusable on a screen with no keyboard.
     */
    var deviceKey: String?
        get() = store.getString(KEY_DEVICE, null)
        set(value) = store.edit().putString(KEY_DEVICE, value?.trim()).apply()

    /**
     * Full address of the display board.
     *
     * Accepts whatever shape the address was pasted in — bare host:port, a full
     * URL, with or without the /display path, and with the key already attached
     * (the admin screen hands out a ready-made link, so that is the common case).
     */
    fun displayUrl(): String? {
        val raw = serverUrl?.trim()?.takeIf { it.isNotEmpty() } ?: return null

        var url = if (raw.startsWith("http://") || raw.startsWith("https://")) raw else "http://$raw"

        // A pasted link may already carry ?key=; keep it and don't append twice.
        val existingQuery = url.substringAfter('?', "")
        url = url.substringBefore('?').trimEnd('/')

        if (!url.endsWith("/display", ignoreCase = true)) {
            url = "$url/display"
        }

        val key = deviceKey?.trim().orEmpty()
        return when {
            existingQuery.isNotEmpty() -> "$url?$existingQuery"
            key.isNotEmpty() -> "$url?key=$key"
            else -> url
        }
    }

    private companion object {
        const val NAME = "queue_display_prefs"
        const val KEY_URL = "server_url"
        const val KEY_DEVICE = "device_key"
    }
}
