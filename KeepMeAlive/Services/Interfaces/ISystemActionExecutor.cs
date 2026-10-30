using KeepMeAlive.Models;

namespace KeepMeAlive.Services.Interfaces;

/// <summary>Executes explicitly scheduled operating-system actions.</summary>
public interface ISystemActionExecutor
{
    /// <summary>Starts the selected system action and reports native failures.</summary>
    /// <param name="action">The action requested by the user.</param>
    /// <exception cref="System.ComponentModel.Win32Exception">Windows refused the action.</exception>
    /// <exception cref="InvalidOperationException">The action could not be started.</exception>
    void Execute(ScheduledAction action);
}
