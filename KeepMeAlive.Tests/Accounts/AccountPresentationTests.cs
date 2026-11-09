using System.Net.Http;
using KeepMeAlive.Models;
using KeepMeAlive.ViewModels;

namespace KeepMeAlive.Tests.Accounts;

public sealed class AccountPresentationTests
{
    [Fact]
    public async Task BrowserSignInOffersCancellationOnlyWhileItsOperationIsRunning()
    {
        await using var model = new AccountViewModel(new TestAccountService(), new TestAuthenticationService());
        var signingInChanges = new List<bool>();
        model.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(AccountViewModel.IsSigningIn))
            {
                signingInChanges.Add(model.IsSigningIn);
            }
        };
        Assert.False(model.IsSigningIn);
        Assert.False(model.CancelSignInCommand.CanExecute(null));

        var signIn = model.SignInCommand.ExecuteAsync(null);

        Assert.True(model.IsSigningIn);
        Assert.True(model.IsRefreshing);
        Assert.True(model.CancelSignInCommand.CanExecute(null));
        model.CancelSignInCommand.Execute(null);
        await signIn.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.False(model.IsSigningIn);
        Assert.False(model.IsRefreshing);
        Assert.False(model.CancelSignInCommand.CanExecute(null));
        Assert.Equal([true, false], signingInChanges);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SignInCompletionAndFailureBothRemoveCancellation(bool fail)
    {
        var accounts = new PresentationAccountService { Current = Profile() };
        var authentication = new PresentationAuthenticationService
        {
            SignInTask = fail ? Task.FromException(new HttpRequestException("Unavailable")) : Task.CompletedTask,
        };
        await using var model = new AccountViewModel(accounts, authentication);

        await model.SignInCommand.ExecuteAsync(null);

        Assert.False(model.IsSigningIn);
        Assert.False(model.IsRefreshing);
        Assert.False(model.CancelSignInCommand.CanExecute(null));
        Assert.Equal(!fail, model.IsSignedIn);
    }

    [Theory]
    [InlineData("Refresh")]
    [InlineData("SaveProfile")]
    [InlineData("SignOut")]
    public async Task OtherAccountOperationsDoNotOfferBrowserSignInCancellation(string operation)
    {
        var profile = Profile();
        var accounts = new PresentationAccountService { Current = profile };
        var authentication = new PresentationAuthenticationService();
        await using var model = new AccountViewModel(accounts, authentication);
        model.RefreshFromCache();
        var refreshCompletion = new TaskCompletionSource<UserAccountProfile?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var saveCompletion = new TaskCompletionSource<UserAccountProfile>(TaskCreationOptions.RunContinuationsAsynchronously);
        var signOutCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        accounts.RefreshTask = refreshCompletion.Task;
        accounts.SaveTask = saveCompletion.Task;
        authentication.SignOutTask = signOutCompletion.Task;

        var execution = operation switch
        {
            "Refresh" => model.RefreshCommand.ExecuteAsync(null),
            "SaveProfile" => model.SaveProfileCommand.ExecuteAsync(null),
            "SignOut" => model.SignOutCommand.ExecuteAsync(null),
            _ => throw new ArgumentException("Unknown account operation.", nameof(operation)),
        };

        Assert.True(model.IsRefreshing);
        Assert.False(model.IsSigningIn);
        Assert.False(model.CancelSignInCommand.CanExecute(null));
        refreshCompletion.SetResult(profile);
        saveCompletion.SetResult(profile);
        signOutCompletion.SetResult();
        await execution.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.False(model.IsSigningIn);
        Assert.False(model.IsRefreshing);
    }

    [Fact]
    public async Task UnconfiguredRefreshAndCacheRefreshKeepTheSignInExplanation()
    {
        await using var model = new AccountViewModel(new TestAccountService(), new PresentationAuthenticationService(false));
        var explanation = model.StatusMessage;
        Assert.Contains("being prepared for this release", explanation);

        await model.RefreshCommand.ExecuteAsync(null);
        Assert.Equal(explanation, model.StatusMessage);
        model.RefreshFromCache();

        Assert.Equal(explanation, model.StatusMessage);
        Assert.False(model.CanSignIn);
        Assert.False(model.IsSigningIn);
        Assert.False(model.IsSignedIn);
    }

    private static UserAccountProfile Profile() => new()
    {
        UserId = "presentation-user",
        DisplayName = "Test account",
        Email = "account@example.test",
        LastSyncedAtUtc = new DateTime(2026, 11, 9, 12, 0, 0, DateTimeKind.Utc),
    };
}
