# KeepMeAlive design direction

KeepMeAlive is a quiet desktop utility. Its first screen answers: is keep-alive running, and when will it act next? The next-pulse countdown, method and Start/Stop control give the application its identity. Values come from the engine; there are no invented usage metrics.

The approved visual reference is Microsoft Clock on Windows 11. Reproduce its Fluent composition across the whole application: a quiet 280 DIP navigation pane, 32 DIP title bar, green selection marker, near-white cards, fine outlines, and compact setting rows. Preserve Windows window controls and desktop navigation.

Use Segoe UI Variable with Segoe UI fallback: 14 DIP body, 12 caption, 14 semibold section, 36 page title, 38 countdown. Spacing: 4, 8, 12, 16, 24, 32, 56 DIP. Card radius 8, setting-row and input radius 4. Circular controls are reserved for timer actions. Sidebar 280 DIP, content gutters 56 DIP at reference size and 24 DIP at compact sizes. Primary actions remain visible at 860×580. ENERGY 1 / RHYTHM 1 / MOTION 1.

| Token | Light | Dark |
|---|---|---|
| Canvas | #F4FAF8 | #202422 |
| Surface | #FCFEFD | #2B302D |
| Text | #1B1D1C | #F5F7F6 |
| Secondary text | #606663 | #BCC5BF |
| Accent | #007632 | #63D58B |
| Running | #007632 | #63D58B |
| Error | #B42318 | #FF8A80 |

The fixed reference palette reproduces the light mint cast of the screenshots without depending on wallpaper. Mica is used behind supported window chrome, with solid fallback when unavailable, transparency is disabled, or high contrast is active. Forms and session content remain opaque for legibility. WPF UI 4.3.0 is the sole Fluent control foundation; its templates use the existing Tabler vectors instead of bundled symbol icons. High contrast uses Windows semantic colors. Press feedback is immediate; system animation preferences take priority. Avoid indefinite decorative animation. The timer ring represents the real remaining pulse cycle; it is never a decorative spinner.

Inspect native screenshots at minimum/default sizes, light/dark/high contrast, 100/150/200% DPI. Verify keyboard focus, automation labels, readable validation, and Start/Stop alignment. Review the local anti-slop UI/human skills and Taste app-screen rules; browser-only inspectors do not validate WPF.

Tabler outline is the only icon source. Vendor official SVGs at a pinned upstream commit and keep the MIT notice with all distributions. Use the shared native `TablerIconStyle`: a 24-unit source grid with round 2-unit strokes, rendered at 18 DIP for navigation and section labels and 16 DIP for window controls. Icons inherit their parent foreground so selected, disabled and high-contrast states remain legible. Keep text labels and existing text-only actions. Use Tabler `activity` for the utility's pulse identity and active/inactive tray states; derive the executable and installer assets from that same vector. Native confirmation dialogs omit stock pictograms and retain their accessible text and buttons. Layout geometry, focus indicators and status data marks remain ordinary WPF primitives.
