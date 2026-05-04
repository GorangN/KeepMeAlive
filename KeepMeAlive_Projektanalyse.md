# Projektanalyse: KeepMeAlive

## Projektstruktur

```
KeepMeAlive/App.xaml
KeepMeAlive/App.xaml.cs
KeepMeAlive/Converters/BoolToVisibilityConverter.cs
KeepMeAlive/Helpers/Interop.cs
KeepMeAlive/Helpers/RelayCommand.cs
KeepMeAlive/MainWindow.xaml
KeepMeAlive/MainWindow.xaml.cs
KeepMeAlive/Resources/Colors.xaml
KeepMeAlive/Resources/Styles.xaml
KeepMeAlive/Services/IKeepAliveStrategy.cs
KeepMeAlive/Services/IdleMonitorService.cs
KeepMeAlive/Services/KeepAwakeService.cs
KeepMeAlive/Services/KeyboardStrategy_ScanCodeShift.cs
KeepMeAlive/Services/KeyboardStrategy_VirtualKey.cs
KeepMeAlive/Services/MouseStrategy.cs
KeepMeAlive/ViewModels/BaseViewModel.cs
KeepMeAlive/ViewModels/DashboardViewModel.cs
KeepMeAlive/Views/DashboardView.xaml
KeepMeAlive/Views/DashboardView.xaml.cs
KeepMeAlive/obj/Debug/net8.0-windows/.NETCoreApp,Version=v8.0.AssemblyAttributes.cs
KeepMeAlive/obj/Debug/net8.0-windows/App.g.cs
KeepMeAlive/obj/Debug/net8.0-windows/App.g.i.cs
KeepMeAlive/obj/Debug/net8.0-windows/GeneratedInternalTypeHelper.g.cs
KeepMeAlive/obj/Debug/net8.0-windows/GeneratedInternalTypeHelper.g.i.cs
KeepMeAlive/obj/Debug/net8.0-windows/MainWindow.g.cs
KeepMeAlive/obj/Debug/net8.0-windows/MainWindow.g.i.cs
KeepMeAlive/obj/Debug/net8.0-windows/KeepMeAlive.GlobalUsings.g.cs
KeepMeAlive/obj/Debug/net8.0-windows/KeepMeAlive_1lkxkkxn_wpftmp.GlobalUsings.g.cs
KeepMeAlive/obj/Debug/net8.0-windows/KeepMeAlive_50cifwzq_wpftmp.GlobalUsings.g.cs
KeepMeAlive/obj/Debug/net8.0-windows/KeepMeAlive_ap324vmc_wpftmp.GlobalUsings.g.cs
KeepMeAlive/obj/Debug/net8.0-windows/KeepMeAlive_byvbqbxr_wpftmp.GlobalUsings.g.cs
KeepMeAlive/obj/Debug/net8.0-windows/KeepMeAlive_frgflrdj_wpftmp.GlobalUsings.g.cs
KeepMeAlive/obj/Debug/net8.0-windows/KeepMeAlive_ivxgvhez_wpftmp.GlobalUsings.g.cs
KeepMeAlive/obj/Debug/net8.0-windows/KeepMeAlive_l14y4pdl_wpftmp.GlobalUsings.g.cs
KeepMeAlive/obj/Debug/net8.0-windows/KeepMeAlive_mujp43ro_wpftmp.GlobalUsings.g.cs
KeepMeAlive/obj/Debug/net8.0-windows/KeepMeAlive_pnqgtmge_wpftmp.GlobalUsings.g.cs
KeepMeAlive/obj/Debug/net8.0-windows/KeepMeAlive_rellmz12_wpftmp.GlobalUsings.g.cs
KeepMeAlive/obj/Debug/net8.0-windows/KeepMeAlive_ryjbolq3_wpftmp.GlobalUsings.g.cs
KeepMeAlive/obj/Debug/net8.0-windows/KeepMeAlive_tr1ydbjv_wpftmp.GlobalUsings.g.cs
KeepMeAlive/obj/Debug/net8.0-windows/KeepMeAlive_utmb5iws_wpftmp.GlobalUsings.g.cs
KeepMeAlive/obj/Debug/net8.0-windows/Views/DashboardView.g.cs
KeepMeAlive/obj/Debug/net8.0-windows/Views/DashboardView.g.i.cs
KeepMeAlive/obj/Release/net8.0-windows/.NETCoreApp,Version=v8.0.AssemblyAttributes.cs
KeepMeAlive/obj/Release/net8.0-windows/App.g.cs
KeepMeAlive/obj/Release/net8.0-windows/App.g.i.cs
KeepMeAlive/obj/Release/net8.0-windows/GeneratedInternalTypeHelper.g.cs
KeepMeAlive/obj/Release/net8.0-windows/GeneratedInternalTypeHelper.g.i.cs
KeepMeAlive/obj/Release/net8.0-windows/MainWindow.g.cs
KeepMeAlive/obj/Release/net8.0-windows/MainWindow.g.i.cs
KeepMeAlive/obj/Release/net8.0-windows/KeepMeAlive.GlobalUsings.g.cs
KeepMeAlive/obj/Release/net8.0-windows/KeepMeAlive_ek4rjqfe_wpftmp.GlobalUsings.g.cs
KeepMeAlive/obj/Release/net8.0-windows/KeepMeAlive_ke14rwqi_wpftmp.GlobalUsings.g.cs
KeepMeAlive/obj/Release/net8.0-windows/Views/DashboardView.g.cs
KeepMeAlive/obj/Release/net8.0-windows/Views/DashboardView.g.i.cs
```

## App

### `KeepMeAlive/App.xaml`

- Ressourcen- oder UI-Definition fÃ¼r die OberflÃ¤che.

### `KeepMeAlive/App.xaml.cs`

- EnthÃ¤lt eine Klasse, die eine bestimmte Logik oder Ansicht abbildet.
- Einstiegspunkt der Anwendung.

## Converters

### `KeepMeAlive/Converters/BoolToVisibilityConverter.cs`

- EnthÃ¤lt eine Klasse, die eine bestimmte Logik oder Ansicht abbildet.
- Implementiert einen ValueConverter (z.â€¯B. fÃ¼r Bindings in WPF).

## Helpers

### `KeepMeAlive/Helpers/Interop.cs`

- EnthÃ¤lt eine Klasse, die eine bestimmte Logik oder Ansicht abbildet.

### `KeepMeAlive/Helpers/RelayCommand.cs`

- EnthÃ¤lt eine Klasse, die eine bestimmte Logik oder Ansicht abbildet.
- EnthÃ¤lt Command-Logik (RelayCommand oder DelegateCommand).
- Methode: `public bool CanExecute(object? parameter) => _can?.Invoke(parameter) ?? true;`
- Methode: `public void Execute(object? parameter) => _exec(parameter);`

## Resources

### `KeepMeAlive/Resources/Colors.xaml`

- Ressourcen- oder UI-Definition fÃ¼r die OberflÃ¤che.

### `KeepMeAlive/Resources/Styles.xaml`

- Ressourcen- oder UI-Definition fÃ¼r die OberflÃ¤che.

## Services

### `KeepMeAlive/Services/IKeepAliveStrategy.cs`

- EnthÃ¤lt ein Interface zur Abstraktion einer Strategie oder Logik.
- Methode: `public interface IKeepAliveStrategy { void Pulse(); }`

### `KeepMeAlive/Services/IdleMonitorService.cs`

- EnthÃ¤lt eine Klasse, die eine bestimmte Logik oder Ansicht abbildet.
- MVVM ViewModel mit Property-Changed-Mechanismus.
- Methode: `public void Start() => _timer.Start();`
- Methode: `public void Stop() => _timer.Stop();`
- Methode: `public void Dispose() => _timer.Stop();`

### `KeepMeAlive/Services/KeepAwakeService.cs`

- EnthÃ¤lt eine Klasse, die eine bestimmte Logik oder Ansicht abbildet.
- Methode: `public void Enable() => SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_SYSTEM_REQUIRED | EXECUTION_STATE.ES_DISPLAY_REQUIRED);`
- Methode: `public void Refresh() => Enable();`
- Methode: `public void Disable() => SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);`

### `KeepMeAlive/Services/KeyboardStrategy_ScanCodeShift.cs`

- EnthÃ¤lt eine Klasse, die eine bestimmte Logik oder Ansicht abbildet.
- Methode: `public void Pulse()`

### `KeepMeAlive/Services/KeyboardStrategy_VirtualKey.cs`

- EnthÃ¤lt eine Klasse, die eine bestimmte Logik oder Ansicht abbildet.
- Methode: `public void Pulse()`

### `KeepMeAlive/Services/MouseStrategy.cs`

- EnthÃ¤lt eine Klasse, die eine bestimmte Logik oder Ansicht abbildet.
- Methode: `public void Pulse()`

## ViewModels

### `KeepMeAlive/ViewModels/BaseViewModel.cs`

- EnthÃ¤lt eine Klasse, die eine bestimmte Logik oder Ansicht abbildet.
- MVVM ViewModel mit Property-Changed-Mechanismus.

### `KeepMeAlive/ViewModels/DashboardViewModel.cs`

- EnthÃ¤lt eine Klasse, die eine bestimmte Logik oder Ansicht abbildet.
- Implementiert einen ValueConverter (z.â€¯B. fÃ¼r Bindings in WPF).
- Methode: `public bool KeepAwakeEnabled { get => _keepAwakeEnabled; set => Set(ref _keepAwakeEnabled, value); }`
- Methode: `public bool IsRunning { get => _isRunning; private set { if (Set(ref _isRunning, value)) { Raise(nameof(CanStart)); Raise(nameof(StartButtonText)); } } }`
- Methode: `public bool CanStart => !IsRunning && IntervalSeconds >= 20;`
- Methode: `public bool IsKeyboard => SelectedMethod == Method.Keyboard;`
- Methode: `public bool IsMouse => SelectedMethod == Method.Mouse;`

## Views

### `KeepMeAlive/Views/DashboardView.xaml`

- Ressourcen- oder UI-Definition fÃ¼r die OberflÃ¤che.

### `KeepMeAlive/Views/DashboardView.xaml.cs`

- EnthÃ¤lt eine Klasse, die eine bestimmte Logik oder Ansicht abbildet.

## Main

### `KeepMeAlive/MainWindow.xaml`

- Ressourcen- oder UI-Definition fÃ¼r die OberflÃ¤che.
- Hauptfenster der Anwendung.

### `KeepMeAlive/MainWindow.xaml.cs`

- EnthÃ¤lt eine Klasse, die eine bestimmte Logik oder Ansicht abbildet.
- Hauptfenster der Anwendung.

### `KeepMeAlive/obj/Debug/net8.0-windows/.NETCoreApp,Version=v8.0.AssemblyAttributes.cs`

### `KeepMeAlive/obj/Debug/net8.0-windows/App.g.cs`

- EnthÃ¤lt eine Klasse, die eine bestimmte Logik oder Ansicht abbildet.
- Einstiegspunkt der Anwendung.
- Methode: `public void InitializeComponent() {`
- Methode: `public static void Main() {`

### `KeepMeAlive/obj/Debug/net8.0-windows/App.g.i.cs`

- EnthÃ¤lt eine Klasse, die eine bestimmte Logik oder Ansicht abbildet.
- Einstiegspunkt der Anwendung.
- Methode: `public void InitializeComponent() {`
- Methode: `public static void Main() {`

### `KeepMeAlive/obj/Debug/net8.0-windows/GeneratedInternalTypeHelper.g.cs`

- EnthÃ¤lt eine Klasse, die eine bestimmte Logik oder Ansicht abbildet.

### `KeepMeAlive/obj/Debug/net8.0-windows/GeneratedInternalTypeHelper.g.i.cs`

- EnthÃ¤lt eine Klasse, die eine bestimmte Logik oder Ansicht abbildet.

### `KeepMeAlive/obj/Debug/net8.0-windows/MainWindow.g.cs`

- EnthÃ¤lt eine Klasse, die eine bestimmte Logik oder Ansicht abbildet.
- Hauptfenster der Anwendung.
- Methode: `public void InitializeComponent() {`

### `KeepMeAlive/obj/Debug/net8.0-windows/MainWindow.g.i.cs`

- EnthÃ¤lt eine Klasse, die eine bestimmte Logik oder Ansicht abbildet.
- Hauptfenster der Anwendung.
- Methode: `public void InitializeComponent() {`

### `KeepMeAlive/obj/Debug/net8.0-windows/KeepMeAlive.GlobalUsings.g.cs`

### `KeepMeAlive/obj/Debug/net8.0-windows/KeepMeAlive_1lkxkkxn_wpftmp.GlobalUsings.g.cs`

### `KeepMeAlive/obj/Debug/net8.0-windows/KeepMeAlive_50cifwzq_wpftmp.GlobalUsings.g.cs`

### `KeepMeAlive/obj/Debug/net8.0-windows/KeepMeAlive_ap324vmc_wpftmp.GlobalUsings.g.cs`

### `KeepMeAlive/obj/Debug/net8.0-windows/KeepMeAlive_byvbqbxr_wpftmp.GlobalUsings.g.cs`

### `KeepMeAlive/obj/Debug/net8.0-windows/KeepMeAlive_frgflrdj_wpftmp.GlobalUsings.g.cs`

### `KeepMeAlive/obj/Debug/net8.0-windows/KeepMeAlive_ivxgvhez_wpftmp.GlobalUsings.g.cs`

### `KeepMeAlive/obj/Debug/net8.0-windows/KeepMeAlive_l14y4pdl_wpftmp.GlobalUsings.g.cs`

### `KeepMeAlive/obj/Debug/net8.0-windows/KeepMeAlive_mujp43ro_wpftmp.GlobalUsings.g.cs`

### `KeepMeAlive/obj/Debug/net8.0-windows/KeepMeAlive_pnqgtmge_wpftmp.GlobalUsings.g.cs`

### `KeepMeAlive/obj/Debug/net8.0-windows/KeepMeAlive_rellmz12_wpftmp.GlobalUsings.g.cs`

### `KeepMeAlive/obj/Debug/net8.0-windows/KeepMeAlive_ryjbolq3_wpftmp.GlobalUsings.g.cs`

### `KeepMeAlive/obj/Debug/net8.0-windows/KeepMeAlive_tr1ydbjv_wpftmp.GlobalUsings.g.cs`

### `KeepMeAlive/obj/Debug/net8.0-windows/KeepMeAlive_utmb5iws_wpftmp.GlobalUsings.g.cs`

### `KeepMeAlive/obj/Debug/net8.0-windows/Views/DashboardView.g.cs`

- EnthÃ¤lt eine Klasse, die eine bestimmte Logik oder Ansicht abbildet.
- Methode: `public void InitializeComponent() {`

### `KeepMeAlive/obj/Debug/net8.0-windows/Views/DashboardView.g.i.cs`

- EnthÃ¤lt eine Klasse, die eine bestimmte Logik oder Ansicht abbildet.
- Methode: `public void InitializeComponent() {`

### `KeepMeAlive/obj/Release/net8.0-windows/.NETCoreApp,Version=v8.0.AssemblyAttributes.cs`

### `KeepMeAlive/obj/Release/net8.0-windows/App.g.cs`

- EnthÃ¤lt eine Klasse, die eine bestimmte Logik oder Ansicht abbildet.
- Einstiegspunkt der Anwendung.
- Methode: `public void InitializeComponent() {`
- Methode: `public static void Main() {`

### `KeepMeAlive/obj/Release/net8.0-windows/App.g.i.cs`

- EnthÃ¤lt eine Klasse, die eine bestimmte Logik oder Ansicht abbildet.
- Einstiegspunkt der Anwendung.
- Methode: `public void InitializeComponent() {`
- Methode: `public static void Main() {`

### `KeepMeAlive/obj/Release/net8.0-windows/GeneratedInternalTypeHelper.g.cs`

- EnthÃ¤lt eine Klasse, die eine bestimmte Logik oder Ansicht abbildet.

### `KeepMeAlive/obj/Release/net8.0-windows/GeneratedInternalTypeHelper.g.i.cs`

- EnthÃ¤lt eine Klasse, die eine bestimmte Logik oder Ansicht abbildet.

### `KeepMeAlive/obj/Release/net8.0-windows/MainWindow.g.cs`

- EnthÃ¤lt eine Klasse, die eine bestimmte Logik oder Ansicht abbildet.
- Hauptfenster der Anwendung.
- Methode: `public void InitializeComponent() {`

### `KeepMeAlive/obj/Release/net8.0-windows/MainWindow.g.i.cs`

- EnthÃ¤lt eine Klasse, die eine bestimmte Logik oder Ansicht abbildet.
- Hauptfenster der Anwendung.
- Methode: `public void InitializeComponent() {`

### `KeepMeAlive/obj/Release/net8.0-windows/KeepMeAlive.GlobalUsings.g.cs`

### `KeepMeAlive/obj/Release/net8.0-windows/KeepMeAlive_ek4rjqfe_wpftmp.GlobalUsings.g.cs`

### `KeepMeAlive/obj/Release/net8.0-windows/KeepMeAlive_ke14rwqi_wpftmp.GlobalUsings.g.cs`

### `KeepMeAlive/obj/Release/net8.0-windows/Views/DashboardView.g.cs`

- EnthÃ¤lt eine Klasse, die eine bestimmte Logik oder Ansicht abbildet.
- Methode: `public void InitializeComponent() {`

### `KeepMeAlive/obj/Release/net8.0-windows/Views/DashboardView.g.i.cs`

- EnthÃ¤lt eine Klasse, die eine bestimmte Logik oder Ansicht abbildet.
- Methode: `public void InitializeComponent() {`

