package com.aimclassic.mobile;

import android.annotation.SuppressLint;
import android.app.Activity;
import android.content.Intent;
import android.content.res.AssetManager;
import android.graphics.Color;
import android.net.Uri;
import android.os.Build;
import android.os.Bundle;
import android.view.ViewGroup;
import android.view.Window;
import android.webkit.WebChromeClient;
import android.webkit.WebResourceRequest;
import android.webkit.WebSettings;
import android.webkit.WebView;
import android.webkit.WebViewClient;

import java.io.BufferedReader;
import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.io.OutputStream;
import java.net.InetAddress;
import java.net.ServerSocket;
import java.net.Socket;
import java.nio.charset.StandardCharsets;
import java.util.Locale;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

public class MainActivity extends Activity {
    private WebView _webView;
    private ServerSocket _server;
    private Thread _serverThread;
    private final ExecutorService _pool = Executors.newFixedThreadPool(4);
    private volatile boolean _running = true;
    private int _port = 15199;

    @SuppressLint("SetJavaScriptEnabled")
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        Window w = getWindow();
        if (Build.VERSION.SDK_INT >= 21) {
            w.setStatusBarColor(Color.parseColor("#003DB8"));
        }

        startLocalAssetServer();

        _webView = new WebView(this);
        _webView.setLayoutParams(new ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT,
                ViewGroup.LayoutParams.MATCH_PARENT));

        WebSettings s = _webView.getSettings();
        s.setJavaScriptEnabled(true);
        s.setDomStorageEnabled(true);
        s.setDatabaseEnabled(true);
        s.setMediaPlaybackRequiresUserGesture(false);
        if (Build.VERSION.SDK_INT >= 21) {
            s.setMixedContentMode(WebSettings.MIXED_CONTENT_ALWAYS_ALLOW);
        }

        _webView.setWebChromeClient(new WebChromeClient());
        _webView.setWebViewClient(new WebViewClient() {
            @Override
            public boolean shouldOverrideUrlLoading(WebView view, WebResourceRequest request) {
                Uri uri = request.getUrl();
                if (uri != null && "127.0.0.1".equals(uri.getHost())) {
                    return false;
                }
                try {
                    startActivity(new Intent(Intent.ACTION_VIEW, uri));
                    return true;
                } catch (Exception ex) {
                    return false;
                }
            }
        });

        setContentView(_webView);
        _webView.loadUrl("http://127.0.0.1:" + _port + "/index.html");
    }

    private void startLocalAssetServer() {
        ServerSocket ss = null;
        for (int candidate = 15199; candidate <= 15215; candidate++) {
            try {
                ss = new ServerSocket(candidate, 16, InetAddress.getByName("127.0.0.1"));
                _port = candidate;
                break;
            } catch (IOException ignored) { }
        }
        if (ss == null) {
            try {
                ss = new ServerSocket(0, 16, InetAddress.getByName("127.0.0.1"));
                _port = ss.getLocalPort();
            } catch (IOException ignored) { }
        }
        _server = ss;
        if (_server == null) return;

        _serverThread = new Thread(new Runnable() {
            @Override
            public void run() {
                while (_running && _server != null && !_server.isClosed()) {
                    try {
                        final Socket cli = _server.accept();
                        _pool.execute(new Runnable() {
                            @Override
                            public void run() {
                                handleClient(cli);
                            }
                        });
                    } catch (IOException e) {
                        if (!_running) break;
                    }
                }
            }
        });
        _serverThread.setDaemon(true);
        _serverThread.start();
    }

    private void handleClient(Socket cli) {
        try {
            cli.setSoTimeout(4000);
            BufferedReader br = new BufferedReader(new InputStreamReader(cli.getInputStream(), "US-ASCII"));
            String reqLine = br.readLine();
            if (reqLine == null || reqLine.trim().isEmpty()) {
                cli.close();
                return;
            }
            String hdr;
            while ((hdr = br.readLine()) != null && !hdr.isEmpty()) { }

            String[] parts = reqLine.split(" ");
            String path = parts.length >= 2 ? parts[1] : "/";
            int q = path.indexOf('?');
            if (q >= 0) path = path.substring(0, q);
            if ("/".equals(path) || path.isEmpty()) path = "/index.html";

            OutputStream os = cli.getOutputStream();
            if ("/api/bootstrap".equalsIgnoreCase(path)) {
                String lang = Locale.getDefault().getLanguage().startsWith("ru") ? "ru" : "en";
                String json = "{\"room\":\"AIM-Global-2006\",\"lang\":\"" + lang + "\",\"pcNick\":\"\"}";
                writeHttp(os, "200 OK", "application/json; charset=utf-8", json.getBytes(StandardCharsets.UTF_8));
                cli.close();
                return;
            }

            if (path.contains("..")) {
                writeHttp(os, "403 Forbidden", "text/plain", "Forbidden".getBytes(StandardCharsets.UTF_8));
                cli.close();
                return;
            }

            String assetRel = "pwa" + path;
            byte[] data = readAsset(assetRel);
            if (data == null) {
                writeHttp(os, "404 Not Found", "text/plain", "Not Found".getBytes(StandardCharsets.UTF_8));
                cli.close();
                return;
            }
            writeHttp(os, "200 OK", guessMime(path), data);
            cli.close();
        } catch (Exception ignored) {
            try { cli.close(); } catch (IOException ignored2) { }
        }
    }

    private byte[] readAsset(String assetPath) {
        AssetManager am = getAssets();
        try (InputStream is = am.open(assetPath)) {
            ByteArrayOutputStream bos = new ByteArrayOutputStream();
            byte[] buf = new byte[8192];
            int n;
            while ((n = is.read(buf)) > 0) {
                bos.write(buf, 0, n);
            }
            return bos.toByteArray();
        } catch (IOException e) {
            return null;
        }
    }

    private static String guessMime(String path) {
        String low = path.toLowerCase(Locale.US);
        if (low.endsWith(".html")) return "text/html; charset=utf-8";
        if (low.endsWith(".css")) return "text/css; charset=utf-8";
        if (low.endsWith(".js")) return "application/javascript; charset=utf-8";
        if (low.endsWith(".webmanifest") || low.endsWith(".json")) return "application/manifest+json; charset=utf-8";
        if (low.endsWith(".png")) return "image/png";
        if (low.endsWith(".gif")) return "image/gif";
        if (low.endsWith(".jpg") || low.endsWith(".jpeg")) return "image/jpeg";
        if (low.endsWith(".svg")) return "image/svg+xml";
        if (low.endsWith(".wav")) return "audio/wav";
        if (low.endsWith(".mp3")) return "audio/mpeg";
        return "application/octet-stream";
    }

    private static void writeHttp(OutputStream os, String status, String mime, byte[] body) throws IOException {
        String hdr = "HTTP/1.1 " + status + "\r\n" +
                "Content-Type: " + mime + "\r\n" +
                "Content-Length: " + body.length + "\r\n" +
                "Cache-Control: no-cache\r\n" +
                "Connection: close\r\n\r\n";
        os.write(hdr.getBytes("US-ASCII"));
        os.write(body);
        os.flush();
    }

    @Override
    public void onBackPressed() {
        if (_webView != null && _webView.canGoBack()) {
            _webView.goBack();
        } else {
            moveTaskToBack(true);
        }
    }

    @Override
    protected void onDestroy() {
        _running = false;
        try { if (_server != null) _server.close(); } catch (IOException ignored) { }
        _pool.shutdownNow();
        if (_webView != null) _webView.destroy();
        super.onDestroy();
    }
}
