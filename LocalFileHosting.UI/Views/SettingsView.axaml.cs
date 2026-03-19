using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using LocalFileHosting.UI.ViewModels;
using System.Linq;

namespace LocalFileHosting.UI.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private async void OnBrowseFolderClicked(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Select Folder to Host", AllowMultiple = false });

        if (folders.Count > 0)
        {
            if (DataContext is SettingsViewModel vm)
            {
                vm.DefaultHostFolder = folders.First().Path.LocalPath;
                vm.SaveSettings();
            }
        }
    }

    // NEW METHOD FOR LOG FOLDER
    private async void OnBrowseLogFolderClicked(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Select Log Save Location", AllowMultiple = false });

        if (folders.Count > 0)
        {
            if (DataContext is SettingsViewModel vm)
            {
                vm.LogDirectory = folders.First().Path.LocalPath;
                vm.SaveSettings();
            }
        }
    }
}