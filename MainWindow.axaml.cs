using System;
using System.IO;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace KeyboardLight;

public record Settings(string Hex, int Brightness, string Effect);

public partial class MainWindow : Window
{
    private static readonly string SettingsPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "keyboardlight.json");

    private readonly KeyboardController _controller = new();
    private TextBox?      _hexInput;
    private Border?       _colorPreview;
    private TextBlock?    _statusText;
    private RadioButton?  _brightness2;

    public MainWindow()
    {
        InitializeComponent();

        _hexInput     = this.FindControl<TextBox>("HexInput");
        _colorPreview = this.FindControl<Border>("ColorPreview");
        _statusText   = this.FindControl<TextBlock>("StatusText");
        _brightness2  = this.FindControl<RadioButton>("Brightness2");

        if (_hexInput != null)
            _hexInput.TextChanged += OnHexChanged;

        // Kayıtlı ayarı yükle
        LoadSettings();

        var connected = _controller.Connect();
        if (_statusText != null)
        {
            _statusText.Text = connected ? "Cihaz bağlı ✓" : "Cihaz bulunamadı!";
            _statusText.Foreground = connected
                ? new SolidColorBrush(Color.Parse("#a6e3a1"))
                : new SolidColorBrush(Color.Parse("#f38ba8"));
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
                Effect: effect
            );
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(s));
        }
        catch { }
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

    private void OnPresetColor(object? s, RoutedEventArgs e)
    {
        if (s is Button btn && btn.Tag is string hex && _hexInput != null)
        {
            _hexInput.Text = hex;
            if (_colorPreview != null)
                _colorPreview.Background = new SolidColorBrush(Color.Parse("#" + hex));
        }
    }

    private (byte R, byte G, byte B)[] GetColor()
    {
        var hex = _hexInput?.Text?.Trim() ?? "FFFFFF";
        if (hex.Length != 6) hex = "FFFFFF";
        var c = Color.Parse("#" + hex);
        return new[] { (c.R, c.G, c.B) };
    }

    private int GetBrightness() => _brightness2?.IsChecked == true ? 2 : 1;

    private void OnStatic(object? s, RoutedEventArgs e)  => Try("static",  () => _controller.SendStatic(GetColor(), GetBrightness()));
    private void OnBreath(object? s, RoutedEventArgs e)  => Try("breath",  () => _controller.SendBreath(GetColor(), brightness: GetBrightness()));
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
