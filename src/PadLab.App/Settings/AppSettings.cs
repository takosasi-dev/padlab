using System.IO;
using System.Text.Json;

namespace PadLab.App.Settings;

/// <summary>%LOCALAPPDATA%\PadLab\settings.json。exe 隣に portable.txt があれば exe 隣。</summary>
public sealed class AppSettings
{
    public bool ReduceMotion { get; set; }
    public bool LightTheme { get; set; }
    public bool ShowPollingRate { get; set; } = true;
    public bool ShowFakeDevice { get; set; } = true;
    /// <summary>デモ用仮想コントローラーが名乗る機種 (DeviceKind 名)。パドル画面など機種依存 UI を実機なしで確認するため。</summary>
    public string FakeKind { get; set; } = "Fake";
    public bool SoundEnabled { get; set; }
    public float DriftMinor { get; set; } = 0.03f;
    public float DriftNotable { get; set; } = 0.08f;
    public double ChatterGapMs { get; set; } = 15;
    public double IdleMinutes { get; set; } = 10;
    /// <summary>常に false 既定。ON にしても今バージョンでは何も通信しない。</summary>
    public bool AllowNetwork { get; set; }

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static string DataDirectory
    {
        get
        {
            var exeDir = AppContext.BaseDirectory;
            return File.Exists(Path.Combine(exeDir, "portable.txt"))
                ? exeDir
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PadLab");
        }
    }

    public static string FilePath => Path.Combine(DataDirectory, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath), Json) ?? new AppSettings();
        }
        catch (Exception) { /* 壊れた設定は既定値で続行 */ }
        return new AppSettings();
    }

    public void Save()
    {
        Directory.CreateDirectory(DataDirectory);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, Json));
    }
}
