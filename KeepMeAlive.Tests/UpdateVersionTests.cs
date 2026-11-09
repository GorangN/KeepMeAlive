using System.Reflection;
using KeepMeAlive.Services.Interfaces;
using KeepMeAlive.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace KeepMeAlive.Tests;

/// <summary>Checks that About displays the executable release version across the presentation boundary.</summary>
public sealed class UpdateVersionTests
{
    /// <summary>Checks that explicitly supplied release versions omit optional source revision metadata.</summary>
    /// <param name="version">The informational version supplied by the host.</param>
    /// <param name="expected">The release version displayed to the user.</param>
    [Theory]
    [InlineData("1.1.0", "1.1.0")]
    [InlineData("1.1.0+abc123", "1.1.0")]
    [InlineData("2.0.0-preview.1+abc123", "2.0.0-preview.1")]
    public void InjectedVersionPreservesReleaseAndStripsCommitMetadata(string version, string expected)
    {
        var dependencies = new UpdateDependencies();
        var model = new UpdateViewModel(dependencies, dependencies, version);
        Assert.Equal(expected, model.AppVersion);
    }

    /// <summary>Checks that application composition supplies the executable version rather than the view-model assembly version.</summary>
    [Fact]
    public void CompositionUsesExecutableInformationalVersion()
    {
        var services = new ServiceCollection();
        ApplicationHost.ConfigureServices(services);
        using var provider = services.BuildServiceProvider();
        var model = provider.GetRequiredService<UpdateViewModel>();
        var expected = typeof(App).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
            .InformationalVersion.Split('+')[0];
        Assert.Equal(expected, model.AppVersion);
    }

    private sealed class UpdateDependencies : IUpdateService, IBrowserLauncher
    {
        public string LatestVersion => string.Empty;
        public string ReleaseUrl => string.Empty;
        public Task<bool> CheckForUpdateAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);
        public void Open(Uri uri) { }
    }
}
