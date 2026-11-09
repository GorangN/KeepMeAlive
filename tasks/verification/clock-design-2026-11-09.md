# Microsoft Clock design verification

Reference: three user-supplied Microsoft Clock screenshots. Implementation: WPF, WPF UI 4.3.0, Segoe UI Variable/Segoe UI, official pinned Tabler outline vectors. Dial: ENERGY 1 / RHYTHM 1 / MOTION 1.

## Automated checks

- PASS: Release solution compilation with zero warnings and zero errors.
- PASS: 144 desktop tests, zero failures and zero skipped tests, after the native acceptance fixes.
- PASS: five mounted pages rendered at 860×580, 1020×680 and 1200×940 in light/dark/simulated high contrast, at 100/150/200% render scale. This produces 135 page images under `artifacts/ui-preview`.
- PASS: no binding errors in page/dialog rendering; interactive control names are asserted.
- PASS: true engine-deadline countdown presentation: rounding, jittered cycles, elapsed deadlines, stopped/error reset, paused state and resumed deadlines.
- PASS: Settings navigation shares the original draft and preserves save/discard/cancel, validation and persistence-failure behavior.
- PASS: ComboBox selected foreground including leaf text, calendar day selection, native month/year/decade transitions, calendar-only date entry and Tabler control affordances.
- PASS: tray dictionary composition and menu-item count. A native startup failure caused by the initially missing tray style was corrected and covered by this check.
- PASS: Save/Discard/Cancel and Yes/No dialogs render in each theme; safe defaults and command result mapping are asserted. Six dialog images are recorded.
- PASS: portable publication with WPF UI and Tabler notices. MahApps and ControlzEx are absent from the active normal/publish lockfiles.
- PASS: NuGet vulnerability scan of the presentation project including transitive packages found no known vulnerable dependencies in the configured sources.
- PASS: static independent review found no blocking defect in resource ordering, theme replacement, presentation dependencies, dialog choices or navigation draft protection.

## Visual review

- PASS: inspected rendered light dashboard/settings and compact dark scheduling against the references. Corrected background rendering, selected foreground inheritance, compact/reference gutters, idle timer color, blank validation space and expander width.
- PASS: hierarchy uses 280-DIP sidebar, 32-DIP title bar, fine outlined 4/8-DIP surfaces, green actions/selection, a real 210-DIP timer ring and compact settings rows.
- PASS: icons, including new play/stop/settings/palette/bell/power/window affordances, derive from the pinned official MIT sources and generation manifest. No decorative metric, identity, profile image or Clock-only feature was invented.
- PASS: no indefinite decorative animation; only the real remaining-cycle value changes the ring. Light/dark/system preferences and semantic high-contrast brushes remain supported.

## Native acceptance resumed

The user reauthorized Computer Use. The portable build ran with isolated storage under `artifacts/native-acceptance`; real user settings and credentials were not used.

- PASS: native startup after the tray fix, all five mounted pages and their rendered English labels.
- PASS: Keyboard/F15 session starts; real countdown and remaining ring decrease; method and interval disable while running; Stop returns to Ready and enables inputs.
- PASS: live keep-awake toggle and Mouse session at one-pixel delta. The engine continues while navigating to another page; no input/power error appeared.
- PASS: malformed interval remains visible, gives a useful error and disables Start; a corrected interval enables Start. Invalid mouse distance is rejected.
- PASS: save/discard/cancel modal presentation, explicit Cancel returning to the original page, invalid Save refusing navigation, and valid Save committing input settings before navigation.
- PASS: enabled date field refuses typed text; Alt+Down opens the calendar. Native header clicks traverse month/year/decade; selecting year, month and day updates the date.
- PASS: invalid `25:99` refuses schedule save. A test action for 1 January 2027 was saved, then disabled and saved again. Both native status and persisted `ScheduledActionEnabled=false` confirmed disarming; no shutdown, sleep or restart was executed.
- PASS: Settings light/dark preview, Discard restoring Light, network group collapse, and isolated settings persistence. Update check displays its in-progress feedback and disables its button.
- PASS: signed-out Account page explains unavailable sign-in; Sign in is disabled because the public release config has no OAuth client identifier.
- OBSERVED before the user's exclusion: maximize respects the 1920×1032 work area, restore returns to 1020×680, and minimum 860×580 remains usable. No additional screen-size measurements were made after the exclusion.

## Defects fixed during native acceptance

- About used the Application assembly's 1.0.0 version. Composition now supplies the executable's 1.1.0 informational version; four regression cases verify version sourcing and metadata handling.
- Focused vendor TextBox underline inherited the purple Windows accent. WPF UI's accent resources now use the selected app palette; the rendering suite verifies vendor and app accents match in every theme.
- Cancel sign-in appeared during unrelated Refresh/Save/Sign out operations. Visibility and command availability now follow actual sign-in progress; seven account cases cover success, failure, cancellation and other pending operations.
- Refresh removed the explanation for disabled sign-in in an unconfigured release. The configuration explanation now survives signed-out refresh/cache updates.
- Reentrant delivery while an unsaved-changes modal was open could open a second modal. Navigation now guards the entire transaction and releases the guard in finally; seven cases cover duplicate delivery, save failures, cancellation and exceptions.

## Exact remaining limits

- EXCLUDED at the user's request: actual Windows contrast and display-size/DPI measurements. No contrast or DPI setting was committed.
- Pending: final native retest of the corrected build, tray Start/Stop/reopen, restart persistence, final update result and graceful UI Quit. Computer Use again reported the physical Escape stop and remained stopped when selecting the app after session reset.
- Unavailable in the public configuration: authenticated account/profile/subscription acceptance. No credentials or production OAuth configuration were invented or changed.
- The isolated running test process was terminated through the development shell after UI control stopped, releasing its owned power requests. A subsequent process check found no KeepMeAlive process; this is cleanup, not evidence that native Quit passed.
- `powercfg /requests` requires administrator access here, so independent OS power-request inspection was unavailable.

The corrected code passes 144 tests and portable publication with zero build warnings/errors. The final package is under `artifacts/fluent-portable`. Native acceptance is partial; production readiness is not asserted.
