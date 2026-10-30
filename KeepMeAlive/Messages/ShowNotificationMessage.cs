namespace KeepMeAlive.Messages;
/// <summary>A request for a presentation notification.</summary>
/// <param name="Title">The notification title.</param>
/// <param name="Body">The notification message.</param>
public sealed record ShowNotificationMessage(string Title, string Body);
