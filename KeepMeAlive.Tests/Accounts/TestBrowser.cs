using KeepMeAlive.Services.Interfaces;

namespace KeepMeAlive.Tests.Accounts;

internal sealed class TestBrowser : IBrowserLauncher
{
    public Uri? LastUri { get; private set; }
    public void Open(Uri uri) => LastUri = uri;
}
