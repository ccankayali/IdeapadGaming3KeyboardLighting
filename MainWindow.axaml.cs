using System;
using System.IO;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace KeyboardLight;

public record Settings(string Hex, int Brightness, string Effect,
    string Zone1 = "FF0000", string Zone2 = "00FF00",
    string Zone3 = "0000FF", string Zone4 = "FF00FF",
    bool ZoneMode = false);

public partial class MainWindow : Window
{
    private static readonly string SettingsPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "keyboardlight.json");

    private readonly KeyboardController _controller = new();
    private TextBox?     _hexInput;
    private Border?      _colorPreview;
    private TextBlock?   _statusText;
    private RadioButton? _brightness2;
    private RadioButton? _modeZone;
    private Border?      _singlePanel;
    private Border?      _zonePanel;
    private TextBox?     _zone1Input, _zone2Input, _zone3Input, _zone4Input;
    private Border?      _zone1Preview, _zone2Preview, _zone3Preview, _zone4Preview;

    public MainWindow()
    {
        InitializeComponent();

        _hexInput     = this.FindControl<TextBox>("HexInput");
        _colorPreview = this.FindControl<Border>("ColorPreview");
        _statusText   = this.FindControl<TextBlock>("StatusText");
        _brightness2  = this.FindControl<RadioButton>("Brightness2");
        _modeZone     = this.FindControl<RadioButton>("ModeZone");
        _singlePanel  = this.FindControl<Border>("SinglePanel");
        _zonePanel    = this.FindControl<Border>("ZonePanel");
        _zone1Input   = this.FindControl<TextBox>("Zone1Input");
        _zone2Input   = this.FindControl<TextBox>("Zone2Input");
        _zone3Input   = this.FindControl<TextBox>("Zone3Input");
        _zone4Input   = this.FindControl<TextBox>("Zone4Input");
        _zone1Preview = this.FindControl<Border>("Zone1Preview");
        _zone2Preview = this.FindControl<Border>("Zone2Preview");
        _zone3Preview = this.FindControl<Border>("Zone3Preview");
        _zone4Preview = this.FindControl<Border>("Zone4Preview");

        if (_hexInput != null) _hexInput.TextChanged += OnHexChanged;
        if (_zone1Input != null) _zone1Input.TextChanged += (s, e) => UpdateZonePreview(_zone1Input, _zone1Preview);
        if (_zone2Input != null) _zone2Input.TextChanged += (s, e) => UpdateZonePreview(_zone2Input, _zone2Preview);
        if (_zone3Input != null) _zone3Input.TextChanged += (s, e) => UpdateZonePreview(_zone3Input, _zone3Preview);
        if (_zone4Input != null) _zone4Input.TextChanged += (s, e) => UpdateZonePreview(_zone4Input, _zone4Preview);

        LoadSettings();

        var connected = _controller.Connect();
        if (_statusText != null)
        {
            _statusText.Text = connected ? "Cihaz bağlı ✓" : "Cihaz bulunamadı!";
            _statusText.Foreground = connected
                ? new SolidColorBrush(Color.Parse("#a6e3a1"))
                : new SolidColorBrush(Color.Parse("#f38ba8"));
        }

        if (connected)
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    var s = JsonSerializer.Deserialize<Settings>(json);
                    if (s != null)
                    {
                        var colors = s.ZoneMode ? GetZoneColors() : GetColor();
                        var brightness = GetBrightness();
                        switch (s.Effect)
                        {
                            case "static":   _controller.SendStatic(colors, brightness); break;
                            case "breath":   _controller.SendBreath(colors, brightness: brightness); break;
                            case "wave-rtl": _controller.SendWave(rtl: true); break;
                            case "wave-ltr": _controller.SendWave(rtl: false); break;
                            case "hue":      _controller.SendHue(); break;
                        }
                        if (_statusText != null)
                        {
                            _statusText.Text = "Başlangıç efekti uygulandı ✓";
                            _statusText.Foreground = new SolidColorBrush(Color.Parse("#a6e3a1"));
                        }
                    }
                }
            }
            catch { }
        }
    }

    private void LoadSettings()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return;
            var json = File.ReadAllText(SettingsPath);
            var s = JsonSerializer.Deserialize<Settings>(json);
            if (s == null) return;

            if (_hexInput != null) _hexInput.Text = s.Hex;
            if (_brightness2 != null) _brightness2.IsChecked = s.Brightness == 2;
            if (_zone1Input != null) _zone1Input.Text = s.Zone1;
            if (_zone2Input != null) _zone2Input.Text = s.Zone2;
            if (_zone3Input != null) _zone3Input.Text = s.Zone3;
            if (_zone4Input != null) _zone4Input.Text = s.Zone4;

            if (s.ZoneMode && _modeZone != null)
            {
                _modeZone.IsChecked = true;
                if (_singlePanel != null) _singlePanel.IsVisible = false;
                if (_zonePanel != null) _zonePanel.IsVisible = true;
            }
        }
        catch { }
    }

    private void SaveSettings(string effect)
    {
        try
        {
            var s = new Settings(
                Hex: _hexInput?.Text?.Trim() ?? "FF0000",
                Brightness: GetBrightness(),
                Effect: effect,
                Zone1: _zone1Input?.Text?.Trim() ?? "FF0000",
                Zone2: _zone2Input?.Text?.Trim() ?? "00FF00",
                Zone3: _zone3Input?.Text?.Trim() ?? "0000FF",
                Zone4: _zone4Input?.Text?.Trim() ?? "FF00FF",
                ZoneMode: _modeZone?.IsChecked == true
            );
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(s));
        }
        catch { }
    }

    private void OnModeChanged(object? s, RoutedEventArgs e)
    {
        var zoneMode = _modeZone?.IsChecked == true;
        if (_singlePanel != null) _singlePanel.IsVisible = !zoneMode;
        if (_zonePanel != null) _zonePanel.IsVisible = zoneMode;
    }

    private void OnHexChanged(object? s, TextChangedEventArgs e)
    {
        var hex = _hexInput?.Text?.Trim() ?? "";
        if (hex.Length == 6 && _colorPreview != null)
        {
            try { _colorPreview.Background = new SolidColorBrush(Color.Parse("#" + hex)); }
            catch { }
        }
    }

    private void UpdateZonePreview(TextBox? input, Border? preview)
    {
        var hex = input?.Text?.Trim() ?? "";
        if (hex.Length == 6 && preview != null)
        {
            try { preview.Background = new SolidColorBrush(Color.Parse("#" + hex)); }
            catch { }
        }
    }

    private void OnPresetColor(object? s, RoutedEventArgs e)
    {
        if (s is Button btn && btn.Tag is string hex && _hexInput != null)
        {
            _hexInput.Text = hex;
            if (_colorPreview != null)
                _colorPreview.Background = new SolidColorBrush(Color.Parse("#" + hex));
        }
    }

    private void OnZonePreset(object? s, RoutedEventArgs e)
    {
        if (s is not Button btn || btn.Tag is not string tag) return;
        var parts = tag.Split(':');
        if (parts.Length != 2) return;
        var zone = parts[0];
        var hex  = parts[1];
        var color = Color.Parse("#" + hex);

        switch (zone)
        {
            case "Z1":
                if (_zone1Input != null) _zone1Input.Text = hex;
                if (_zone1Preview != null) _zone1Preview.Background = new SolidColorBrush(color);
                break;
            case "Z2":
                if (_zone2Input != null) _zone2Input.Text = hex;
                if (_zone2Preview != null) _zone2Preview.Background = new SolidColorBrush(color);
                break;
            case "Z3":
                if (_zone3Input != null) _zone3Input.Text = hex;
                if (_zone3Preview != null) _zone3Preview.Background = new SolidColorBrush(color);
                break;
            case "Z4":
                if (_zone4Input != null) _zone4Input.Text = hex;
                if (_zone4Preview != null) _zone4Preview.Background = new SolidColorBrush(color);
                break;
        }
    }

    private (byte R, byte G, byte B)[] GetColor()
    {
        var hex = _hexInput?.Text?.Trim() ?? "FFFFFF";
        if (hex.Length != 6) hex = "FFFFFF";
        var c = Color.Parse("#" + hex);
        return new[] { (c.R, c.G, c.B) };
    }

    private (byte R, byte G, byte B)[] GetZoneColors()
    {
        static (byte R, byte G, byte B) Parse(string? hex)
        {
            if (hex == null || hex.Length != 6) hex = "000000";
            var c = Color.Parse("#" + hex);
            return (c.R, c.G, c.B);
        }
        return new[]
        {
            Parse(_zone1Input?.Text?.Trim()),
            Parse(_zone2Input?.Text?.Trim()),
            Parse(_zone3Input?.Text?.Trim()),
            Parse(_zone4Input?.Text?.Trim()),
        };
    }

    private (byte R, byte G, byte B)[] GetActiveColors()
        => _modeZone?.IsChecked == true ? GetZoneColors() : GetColor();

    private int GetBrightness() => _brightness2?.IsChecked == true ? 2 : 1;

    private void OnStatic(object? s, RoutedEventArgs e)  => Try("static",  () => _controller.SendStatic(GetActiveColors(), GetBrightness()));
    private void OnBreath(object? s, RoutedEventArgs e)  => Try("breath",  () => _controller.SendBreath(GetActiveColors(), brightness: GetBrightness()));
    private void OnWaveRtl(object? s, RoutedEventArgs e) => Try("wave-rtl",() => _controller.SendWave(rtl: true));
    private void OnWaveLtr(object? s, RoutedEventArgs e) => Try("wave-ltr",() => _controller.SendWave(rtl: false));
    private void OnHue(object? s, RoutedEventArgs e)     => Try("hue",     () => _controller.SendHue());
    private void OnOff(object? s, RoutedEventArgs e)     => Try("off",     () => _controller.SendOff());

    private void Try(string effect, Action action)
    {
        try
        {
            action();
            SaveSettings(effect);
            if (_statusText != null)
            {
                _statusText.Text = "Komut gönderildi ✓";
                _statusText.Foreground = new SolidColorBrush(Color.Parse("#a6e3a1"));
            }
        }
        catch (Exception ex)
        {
            if (_statusText != null)
            {
                _statusText.Text = $"Hata: {ex.Message}";
                _statusText.Foreground = new SolidColorBrush(Color.Parse("#f38ba8"));
            }
        }
    }
}