using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using LocalNode.UI.ViewModels;
using System.Linq;

namespace LocalNode.UI.Views;

public partial class HostedFilesView : UserControl
{
    public HostedFilesView()
    {
        InitializeComponent();
    }

    private async void OnAddFileClicked(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select a file to host",
            AllowMultiple = false
        });

        if (files.Count > 0)
        {
            var filePath = files.First().Path.LocalPath;
            if (DataContext is HostedFilesViewModel vm)
            {
                vm.AddPhysicalFile(filePath);
            }
        }
    }

    private void OnItemDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is HostedFileItem item)
        {
            if (item.IsFolder && DataContext is HostedFilesViewModel vm)
            {
                vm.NavigateIntoFolder(item.Name);
            }
        }
    }
}