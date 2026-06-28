using Avalonia;
using System;
using System.IO;
using System.Text.Json;

namespace KeyboardLight;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--headless")
        {
            ApplySavedSettings();
            return;
        }
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static void ApplySavedSettings()
    {
        try
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "keyboardlight.json");
            if (!File.Exists(path)) return;
            var s = JsonSerializer.Deserialize<Settings>(File.ReadAllText(path));
            if (s == null) return;

            using var ctrl = new KeyboardController();
            if (!ctrl.Connect()) return;

            var hex = s.Hex.Length == 6 ? s.Hex : "FFFFFF";
            byte r = Convert.ToByte(hex[0..2], 16);
            byte g = Convert.ToByte(hex[2..4], 16);
            byte b = Convert.ToByte(hex[4..6], 16);
            var c = (r, g, b);

            // 4 bölge renkleri
            static (byte, byte, byte) ParseHex(string h)
            {
                if (h.Length != 6) h = "FFFFFF";
                return (Convert.ToByte(h[0..2], 16),
                        Convert.ToByte(h[2..4], 16),
                        Convert.ToByte(h[4..6], 16));
            }

            var colors = s.ZoneMode
                ? new[] { ParseHex(s.Zone1), ParseHex(s.Zone2), ParseHex(s.Zone3), ParseHex(s.Zone4) }
                : new[] { c };

            switch (s.Effect)
            {
                case "static":   ctrl.SendStatic(colors, s.Brightness); break;
                case "breath":   ctrl.SendBreath(colors, brightness: s.Brightness); break;
                case "wave-rtl": ctrl.SendWave(rtl: true); break;
                case "wave-ltr": ctrl.SendWave(rtl: false); break;
                case "hue":      ctrl.SendHue(); break;
                case "off":      ctrl.SendOff(); break;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"KeyboardLight headless error: {ex.Message}");
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}