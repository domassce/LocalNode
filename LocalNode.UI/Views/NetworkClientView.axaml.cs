using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using LocalNode.UI.ViewModels;

namespace LocalNode.UI.Views;

public partial class NetworkClientView : UserControl
{
    public NetworkClientView()
    {
        InitializeComponent();
    }

    private async void OnDownloadClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string itemName)
        {
            if (DataContext is NetworkClientViewModel vm)
            {
                // Instantly downloads to the OS Downloads folder
                await vm.DownloadItemAsync(itemName);
            }
        }
    }

    private async void OnItemDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is RemoteFileItem item)
        {
            if (item.IsFolder && DataContext is NetworkClientViewModel vm)
            {
                await vm.NavigateIntoFolder(item.Name);
            }
        }
    }
}