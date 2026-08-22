package org.shafak.queuedisplay

import android.annotation.SuppressLint
import android.content.Intent
import android.graphics.Bitmap
import android.os.Bundle
import android.view.KeyEvent
import android.view.View
import android.view.WindowManager
import android.webkit.WebChromeClient
import android.webkit.WebResourceError
import android.webkit.WebResourceRequest
import android.webkit.WebSettings
import android.webkit.WebView
import android.webkit.WebViewClient
import androidx.appcompat.app.AppCompatActivity
import org.shafak.queuedisplay.databinding.ActivityMainBinding

/**
 * Full-screen kiosk shell around the queue display board.
 *
 * The board itself stays a server-rendered page — this only supplies what a
 * browser tab can't on a wall-mounted screen: no chrome, screen kept awake,
 * sound without a tap, a native Arabic voice, and recovery when the network or
 * the server goes away.
 */
class MainActivity : AppCompatActivity() {

    private lateinit var binding: ActivityMainBinding
    private lateinit var prefs: Prefs
    private lateinit var announcer: Announcer

    /** Set while the error screen is up, so a reconnect attempt reloads the board. */
    private var showingError = false

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityMainBinding.inflate(layoutInflater)
        setContentView(binding.root)

        prefs = Prefs(this)
        announcer = Announcer(this)

        // A queue board is useless if the screen sleeps.
        window.addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON)

        configureWebView()

        binding.retryButton.setOnClickListener { loadBoard() }
        binding.settingsButton.setOnClickListener { openSettings() }
        // Long-pressing anywhere is the way back to settings once the board is
        // running full screen with no visible controls.
        binding.webView.setOnLongClickListener { openSettings(); true }

        if (prefs.serverUrl.isNullOrBlank()) {
            openSettings()
        } else {
            loadBoard()
        }
    }

    @SuppressLint("SetJavaScriptEnabled")
    private fun configureWebView() = with(binding.webView.settings) {
        javaScriptEnabled = true
        domStorageEnabled = true
        databaseEnabled = true
        loadsImagesAutomatically = true
        cacheMode = WebSettings.LOAD_DEFAULT

        // The alert tone and the announcement have to play on their own; without
        // this the WebView silently blocks both until somebody taps the screen,
        // which nobody ever does on a wall display.
        mediaPlaybackRequiresUserGesture = false

        // The board is laid out for the screen it runs on; leave it alone.
        useWideViewPort = false
        loadWithOverviewMode = false
        setSupportZoom(false)
        builtInZoomControls = false
        displayZoomControls = false

        mixedContentMode = WebSettings.MIXED_CONTENT_COMPATIBILITY_MODE
    }.also {
        binding.webView.webChromeClient = WebChromeClient()
        binding.webView.webViewClient = BoardWebViewClient()
        binding.webView.setBackgroundColor(0xFF0A1F33.toInt())

        // Android WebView has no Web Speech API, so the page's announcement is
        // routed to the platform's text-to-speech engine instead.
        binding.webView.addJavascriptInterface(announcer.bridge, Announcer.JS_NAME)
    }

    private fun loadBoard() {
        val url = prefs.displayUrl()
        if (url == null) {
            openSettings()
            return
        }

        showingError = false
        binding.errorPanel.visibility = View.GONE
        binding.webView.visibility = View.VISIBLE
        binding.webView.loadUrl(url)
    }

    private fun openSettings() {
        startActivity(Intent(this, SettingsActivity::class.java))
    }

    private fun showError(message: String) {
        showingError = true
        binding.errorText.text = message
        binding.errorPanel.visibility = View.VISIBLE
        binding.webView.visibility = View.GONE

        // Keep trying on its own — a wall display has nobody to press retry.
        binding.root.postDelayed({ if (showingError) loadBoard() }, RETRY_DELAY_MS)
    }

    private inner class BoardWebViewClient : WebViewClient() {
        override fun onPageStarted(view: WebView?, url: String?, favicon: Bitmap?) {
            super.onPageStarted(view, url, favicon)
            showingError = false
        }

        override fun onPageFinished(view: WebView?, url: String?) {
            super.onPageFinished(view, url)
            if (!showingError) {
                binding.errorPanel.visibility = View.GONE
                binding.webView.visibility = View.VISIBLE
            }
            view?.evaluateJavascript(Announcer.INSTALL_SCRIPT, null)
        }

        override fun onReceivedError(
            view: WebView?,
            request: WebResourceRequest?,
            error: WebResourceError?,
        ) {
            super.onReceivedError(view, request, error)
            // Sub-resource failures (a missing icon, say) must not blank the board.
            if (request?.isForMainFrame != true) return
            showError(getString(R.string.error_connect, prefs.serverUrl ?: ""))
        }
    }

    /** Immersive full screen, re-applied because Android drops it after dialogs. */
    private fun enterImmersiveMode() {
        @Suppress("DEPRECATION")
        window.decorView.systemUiVisibility = (
            View.SYSTEM_UI_FLAG_LAYOUT_STABLE
                or View.SYSTEM_UI_FLAG_LAYOUT_HIDE_NAVIGATION
                or View.SYSTEM_UI_FLAG_LAYOUT_FULLSCREEN
                or View.SYSTEM_UI_FLAG_HIDE_NAVIGATION
                or View.SYSTEM_UI_FLAG_FULLSCREEN
                or View.SYSTEM_UI_FLAG_IMMERSIVE_STICKY
            )
    }

    override fun onResume() {
        super.onResume()
        enterImmersiveMode()

        // Coming back from settings with a different address should switch boards.
        val url = prefs.displayUrl()
        if (url != null && url != binding.webView.url && !showingError) {
            loadBoard()
        }
    }

    override fun onWindowFocusChanged(hasFocus: Boolean) {
        super.onWindowFocusChanged(hasFocus)
        if (hasFocus) enterImmersiveMode()
    }

    /** Back would leave the board on screen-with-no-keyboard hardware; ignore it. */
    override fun onKeyDown(keyCode: Int, event: KeyEvent?): Boolean {
        if (keyCode == KeyEvent.KEYCODE_BACK) return true
        return super.onKeyDown(keyCode, event)
    }

    override fun onDestroy() {
        announcer.shutdown()
        binding.webView.destroy()
        super.onDestroy()
    }

    private companion object {
        const val RETRY_DELAY_MS = 8_000L
    }
}
