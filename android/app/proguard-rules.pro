# The page reaches the TTS bridge by name through addJavascriptInterface, so
# these members must survive shrinking.
-keepclassmembers class org.shafak.queuedisplay.Announcer$Bridge {
    public *;
}
