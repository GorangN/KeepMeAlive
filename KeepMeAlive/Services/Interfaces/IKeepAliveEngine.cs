using KeepMeAlive.Models;

namespace KeepMeAlive.Services.Interfaces;

/// <summary>Owns the cancellable input loop and the lifetime of its power request.</summary>
public interface IKeepAliveEngine
{
    /// <summary>Occurs when actual engine state changes or its countdown advances.</summary>
    event EventHandler<KeepAliveStatus>? StatusChanged;

    /// <summary>Runs one session until cancellation or a native operation fails.</summary>
    /// <param name="options">The validated preferences for the session.</param>
    /// <param name="cancellationToken">Cancels the running session.</param>
    /// <returns>A task completed after power resources are released.</returns>
    Task RunAsync(KeepAliveOptions options, CancellationToken cancellationToken);

    /// <summary>Updates whether the current session requests system/display power.</summary>
    /// <param name="enabled">Whether power should be requested.</param>
    void SetKeepAwake(bool enabled);
}
