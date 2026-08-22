package org.shafak.queuedisplay

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent

/**
 * Brings the board back up after a power cut without anyone touching the screen
 * — the usual way a wall display is "restarted".
 */
class BootReceiver : BroadcastReceiver() {

    override fun onReceive(context: Context, intent: Intent) {
        if (intent.action != Intent.ACTION_BOOT_COMPLETED &&
            intent.action != "android.intent.action.QUICKBOOT_POWERON"
        ) {
            return
        }

        // Nothing to show until an address has been set.
        if (Prefs(context).serverUrl.isNullOrBlank()) return

        val launch = Intent(context, MainActivity::class.java).apply {
            addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
        }
        context.startActivity(launch)
    }
}
