using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace LocalNode.UI.ViewModels;

public class AppConfig
{
    public string DisplayName { get; set; } = "Anonymous";
    public string DefaultHostFolder { get; set; } = string.Empty;
    public string DefaultPort { get; set; } = "5050";
    public string RoomPassword { get; set; } = string.Empty;
    public string LogDirectory { get; set; } = string.Empty; // <-- New!
}

public partial class SettingsViewModel : ViewModelBase
{
    public const string ConfigFilePath = "appsettings.json";

    [ObservableProperty] private string _displayName = "Anonymous";
    [ObservableProperty] private string _defaultHostFolder = string.Empty;
    [ObservableProperty] private string _defaultPort = "5050";
    [ObservableProperty] private string _roomPassword = string.Empty;
    [ObservableProperty] private string _logDirectory = string.Empty; // <-- New!

    [ObservableProperty] private string _statusMessage = string.Empty;

    public SettingsViewModel()
    {
        LoadSettings();
    }

    public void LoadSettings()
    {
        if (File.Exists(ConfigFilePath))
        {
            try
            {
                var json = File.ReadAllText(ConfigFilePath);
                var config = JsonSerializer.Deserialize<AppConfig>(json);
                if (config != null)
                {
                    DisplayName = config.DisplayName ?? "Anonymous";
                    DefaultHostFolder = config.DefaultHostFolder ?? string.Empty;
                    DefaultPort = config.DefaultPort ?? "5050";
                    RoomPassword = config.RoomPassword ?? string.Empty;
                    LogDirectory = config.LogDirectory ?? string.Empty; // <-- New!
                }
            }
            catch { /* Ignore errors */ }
        }
    }

    [RelayCommand]
    public void SaveSettings()
    {
        var config = new AppConfig
        {
            DisplayName = DisplayName,
            DefaultHostFolder = DefaultHostFolder,
            DefaultPort = DefaultPort,
            RoomPassword = RoomPassword,
            LogDirectory = LogDirectory
        };

        File.WriteAllText(ConfigFilePath, JsonSerializer.Serialize(config));

        StatusMessage = "Settings saved successfully!";
        Task.Delay(3000).ContinueWith(_ => StatusMessage = string.Empty);
    }
}