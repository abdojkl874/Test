package org.shafak.queuedisplay

import android.os.Bundle
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import org.shafak.queuedisplay.databinding.ActivitySettingsBinding

/** Where the address of the server is entered — reachable by long-pressing the board. */
class SettingsActivity : AppCompatActivity() {

    private lateinit var binding: ActivitySettingsBinding

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivitySettingsBinding.inflate(layoutInflater)
        setContentView(binding.root)

        val prefs = Prefs(this)
        binding.urlInput.setText(prefs.serverUrl ?: "")
        binding.keyInput.setText(prefs.deviceKey ?: "")

        binding.saveButton.setOnClickListener {
            val entered = binding.urlInput.text?.toString()?.trim().orEmpty()
            if (entered.isEmpty()) {
                Toast.makeText(this, R.string.settings_url_required, Toast.LENGTH_LONG).show()
                return@setOnClickListener
            }

            prefs.serverUrl = entered
            prefs.deviceKey = binding.keyInput.text?.toString()?.trim()
            Toast.makeText(this, getString(R.string.settings_saved, prefs.displayUrl()), Toast.LENGTH_LONG).show()
            finish()
        }
    }
}
