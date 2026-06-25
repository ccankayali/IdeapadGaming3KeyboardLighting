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
            var color = System.Drawing.ColorTranslator.FromHtml("#" + hex);
            var c = ((byte)color.R, (byte)color.G, (byte)color.B);

            switch (s.Effect)
            {
                case "static":   ctrl.SendStatic(new[] { c }, s.Brightness); break;
                case "breath":   ctrl.SendBreath(new[] { c }, brightness: s.Brightness); break;
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
