using Hardcodet.Wpf.TaskbarNotification;

namespace KeepMeAlive.Messages;

public record ShowNotificationMessage(
    string Title,
    string Body,
    BalloonIcon Icon = BalloonIcon.Info);
