using KeepMeAlive.Services;

namespace KeepMeAlive.Tests.Persistence;

/// <summary>Checks that portable startup changes cannot claim another installation's registration.</summary>
public sealed class StartupOwnershipTests
{
    /// <summary>Accepts only this apphost's supported quoted startup commands.</summary>
    [Fact]
    public void OnlyExactOwnedExecutableCommandsMatch()
    {
        const string executable = @"C:\Portable KeepMeAlive\KeepMeAlive.exe";
        Assert.True(StartupService.IsOwnedCommand(StartupService.BuildCommand(executable, false), executable));
        Assert.True(StartupService.IsOwnedCommand(StartupService.BuildCommand(executable, true), executable));
        Assert.True(StartupService.IsOwnedCommand("  \"c:\\portable keepmealive\\keepmealive.exe\" --silent  ", executable));
        foreach (var command in new[] { null, "", @"C:\Portable KeepMeAlive\KeepMeAlive.exe", "\"C:\\Installed KeepMeAlive\\KeepMeAlive.exe\" --silent", "\"C:\\Portable KeepMeAlive\\KeepMeAlive.exe\" --other", "\"C:\\Portable KeepMeAlive\\KeepMeAlive.exe\".other", "\"C:\\Portable KeepMeAlive\\KeepMeAlive.exe.other\"" })
        {
            Assert.False(StartupService.IsOwnedCommand(command, executable));
        }
    }
}
