using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalFileHosting.Core.Logging;
using LocalFileHosting.Core.Services;
using LocalFileHosting.Core.Storage;

namespace LocalFileHosting.UI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly FileHostingService _fileService;

    // Store our ViewModels here so they don't get destroyed!
    private readonly DashboardViewModel _dashboardViewModel;

    [ObservableProperty]
    private ViewModelBase _currentPageContent;

    [ObservableProperty]
    private string _pageTitle = "Dashboard";

    public MainWindowViewModel()
    {
        var logger = new ConsoleLogger();
        var storage = new LocalStorageProvider(long.MaxValue); // 1 TB limit
        _fileService = new FileHostingService(logger, storage);

        // Initialize the Dashboard exactly ONCE
        _dashboardViewModel = new DashboardViewModel(_fileService);

        CurrentPageContent = _dashboardViewModel;
    }

    [RelayCommand]
    private void Navigate(string pageName)
    {
        PageTitle = pageName;

        if (pageName == "Dashboard")
        {
            // Reuse the existing instance! State is preserved.
            CurrentPageContent = _dashboardViewModel;
        }
        else
        {
            CurrentPageContent = null!;
        }
    }
}