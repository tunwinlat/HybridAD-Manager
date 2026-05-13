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

        base.OnStartup(e);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IAuthenticationService, AuthenticationService>();
        services.AddSingleton<IGraphService, GraphService>();
        services.AddSingleton<IActiveDirectoryService, ActiveDirectoryService>();
    }
}
