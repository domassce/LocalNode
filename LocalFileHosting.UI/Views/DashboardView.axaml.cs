using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using LocalFileHosting.UI.ViewModels;
using System.Linq;

namespace LocalFileHosting.UI.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
    }

    private async void OnBrowseFolderClicked(object? sender, RoutedEventArgs e)
    {
        // Get the top-level window to access the StorageProvider (native dialogs)
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        // Open the folder picker dialog
        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Folder to Host",
            AllowMultiple = false
        });

        // If the user selected a folder, update the ViewModel
        if (folders.Count > 0)
        {
            var folderPath = folders.First().Path.LocalPath;
            if (DataContext is DashboardViewModel vm)
            {
                vm.SelectedFolderPath = folderPath;
            }
        }
    }
}