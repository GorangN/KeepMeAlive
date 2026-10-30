using KeepMeAlive.ViewModels;
using KeepMeAlive.Models;

namespace KeepMeAlive.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public void ApplicationHasNoPresentationOrInfrastructureDependencies()
    {
        var references = typeof(MainViewModel).Assembly.GetReferencedAssemblies().Select(reference => reference.Name).ToArray();
        Assert.DoesNotContain("PresentationFramework", references);
        Assert.DoesNotContain("WindowsBase", references);
        Assert.DoesNotContain("KeepMeAlive.Infrastructure", references);
        Assert.Contains("KeepMeAlive.Core", references);
    }

    [Fact]
    public void CoreHasNoApplicationOrVendorDependencies()
    {
        var references = typeof(AppSettings).Assembly.GetReferencedAssemblies().Select(reference => reference.Name).ToArray();
        Assert.DoesNotContain(references, name => name?.StartsWith("KeepMeAlive.", StringComparison.Ordinal) == true);
        Assert.DoesNotContain("CommunityToolkit.Mvvm", references);
        Assert.DoesNotContain("PresentationFramework", references);
    }
}
