# Lessons Learned

- Explain unfamiliar release terms before listing technical fields. SMTP is the email-delivery service for verification and reset emails; give a beginner a numbered setup path and distinguish source pushes, validation builds and public releases.

- After UI restyling work, re-check dashboard CTAs for responsive layout regressions. Avoid absolute margins or `Grid.ColumnSpan` hacks for primary action columns when the original grid alignment can express the layout.
- After the user explicitly reauthorizes Computer Use following an accidental Escape, reset the node_repl kernel before retrying; the stopped helper state can persist across requests. Re-select the application from fresh returned windows.
- After a browser interruption, use a fresh desktop authorization request when the signed OAuth context has expired. Reset Computer Use only after user reauthorization, and verify the app's Connected screen before claiming native sign-in success.
- For WPF template validation, enable the control before checking read-only and editing behavior. A disabled control can satisfy an offscreen assertion while the enabled native control still accepts text; verify the actual keyboard path as well.
- When the user selects an icon library, audit explicit glyphs, implicit control affordances, tray/executable icons and installer artwork. Pin the source and license, and use shared theme-aware vectors throughout; do not mix font symbols or stock pictograms into the application.
- Simulated app palettes do not verify vendor control resources. Actual Windows high contrast exposed white-on-white dropdown items and calendar dates; assert the rendered vendor popup, arrow and day brushes against their real backgrounds, and retest live system-theme changes.
- Check the rendered leaf TextBlock foreground as well as its ContentPresenter. An implicit TextBlock style can override inherited selection colors while a presenter-only assertion still passes.
- Before using a native screenshot for coordinates, raise the selected window through its exposed Raise action. Occluding windows can appear in a bounded capture; verify the screenshot actually belongs to the intended target.
