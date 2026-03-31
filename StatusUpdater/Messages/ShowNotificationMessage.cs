using Hardcodet.Wpf.TaskbarNotification;

namespace StatusUpdater.Messages;

public record ShowNotificationMessage(
    string Title,
    string Body,
    BalloonIcon Icon = BalloonIcon.Info);
