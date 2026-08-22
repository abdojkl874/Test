package org.shafak.queuedisplay

import android.content.Context

/** The one thing this app needs configured: where the server lives. */
class Prefs(context: Context) {

    private val store = context.getSharedPreferences(NAME, Context.MODE_PRIVATE)

    var serverUrl: String?
        get() = store.getString(KEY_URL, null)
        set(value) = store.edit().putString(KEY_URL, value?.trim()).apply()

    /**
     * Full address of the display board, accepting whatever shape the address
     * was typed in: with or without a scheme, with or without the /display path,
     * with or without a trailing slash.
     */
    fun displayUrl(): String? {
        val raw = serverUrl?.trim()?.takeIf { it.isNotEmpty() } ?: return null

        var url = if (raw.startsWith("http://") || raw.startsWith("https://")) raw else "http://$raw"
        url = url.trimEnd('/')

        return if (url.endsWith("/display", ignoreCase = true)) url else "$url/display"
    }

    private companion object {
        const val NAME = "queue_display_prefs"
        const val KEY_URL = "server_url"
    }
}
