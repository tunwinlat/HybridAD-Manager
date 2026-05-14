using HybridADManager.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace HybridADManager;

public partial class App : Application
{
    public new static App Current => (App)Application.Current;

    public IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        Services = serviceCollection.BuildServiceProvider();

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        base.OnStartup(e);
    }

    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        var dialogService = Services.GetService(typeof(IDialogService)) as IDialogService ?? new DialogService();
        dialogService.ShowError($"An unexpected error occurred: {e.Exception.Message}", "Application Error");
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FATAL: {ex}");
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IAuthenticationService, AuthenticationService>();
        services.AddSingleton<IGraphService, GraphService>();
        services.AddSingleton<IActiveDirectoryService, ActiveDirectoryService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IDialogService, DialogService>();
    }
}
