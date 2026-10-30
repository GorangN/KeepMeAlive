namespace KeepMeAlive.Models;

/// <summary>Describes the engine's actual state at a point in time.</summary>
/// <param name="IsRunning">Whether a session owns the engine.</param>
/// <param name="IsPaused">Whether input is paused because the session is unavailable.</param>
/// <param name="NextPulseAt">The next eligible pulse time, or null when stopped or paused.</param>
/// <param name="Error">A user-facing failure, or null after normal operation.</param>
public sealed record KeepAliveStatus(bool IsRunning, bool IsPaused, DateTimeOffset? NextPulseAt, string? Error);
