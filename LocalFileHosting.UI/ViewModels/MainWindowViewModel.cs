using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalFileHosting.Core.Logging;
using LocalFileHosting.Core.Services;
using LocalFileHosting.Core.Storage;
using LocalFileHosting.UI.Services;

namespace LocalFileHosting.UI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly FileHostingService _fileService;

    private readonly DashboardViewModel _dashboardViewModel;
    private readonly HostedFilesViewModel _hostedFilesViewModel;
    private readonly NetworkClientViewModel _networkClientViewModel;
    private readonly SettingsViewModel _settingsViewModel;

    [ObservableProperty] private ViewModelBase _currentPageContent;
    [ObservableProperty] private string _pageTitle = "Dashboard";
    [ObservableProperty] private string _userName = "Anonymous";

    public MainWindowViewModel()
    {
        _settingsViewModel = new SettingsViewModel();
        UserName = _settingsViewModel.DisplayName;

        var logger = new FileLogger(_settingsViewModel);
        var storage = new LocalStorageProvider(long.MaxValue);
        _fileService = new FileHostingService(logger, storage);

        _dashboardViewModel = new DashboardViewModel(_fileService, _settingsViewModel, logger);

        // THIS IS THE CRITICAL FIX! 
        _hostedFilesViewModel = new HostedFilesViewModel(_fileService)
        {
            Settings = _settingsViewModel // <-- We must give it the settings!
        };

        _networkClientViewModel = new NetworkClientViewModel(_settingsViewModel);
        _hostedFilesViewModel.Dashboard = _dashboardViewModel;
        CurrentPageContent = _dashboardViewModel;
    }

    [RelayCommand]
    private void Navigate(string pageName)
    {
        PageTitle = pageName;

        if (pageName == "Dashboard")
        {
            _dashboardViewModel.RefreshStats();
            CurrentPageContent = _dashboardViewModel;
        }
        else if (pageName == "Hosted Files")
        {
            _hostedFilesViewModel.RefreshFiles();
            CurrentPageContent = _hostedFilesViewModel;
        }
        else if (pageName == "Network Client")
        {
            CurrentPageContent = _networkClientViewModel;
        }
        else if (pageName == "Settings")
        {
            CurrentPageContent = _settingsViewModel;
        }
        else
        {
            CurrentPageContent = null!;
        }
    }
}