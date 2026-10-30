namespace KeepMeAlive;
/// <summary>The Windows STA composition entry point.</summary>
public static class Program
{
    /// <summary>Runs the desktop application with deterministic host cleanup.</summary>
    /// <returns>The application exit code.</returns>
    [STAThread]
    public static int Main()
    {
        var app = new App();
        using var host = new ApplicationHost(app);
        return app.Run();
    }
}
