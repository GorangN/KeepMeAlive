# KeepMeAlive design direction

KeepMeAlive is a quiet desktop utility. Its first screen answers: is keep-alive running, and when will it act next? The next-pulse countdown, method and Start/Stop control give the application its identity. Values come from the engine; there are no invented usage metrics.

The user selected Apple's iOS/iPadOS 26 visual direction. The Figma community file could not be inspected, so this is a native WPF interpretation of Apple's materials and hierarchy, not a replica. Preserve Windows window controls and desktop navigation.

Use Inter: 14 DIP body, 12 caption, 17 section, 28 page title. Spacing: 4, 8, 12, 16, 24, 32 DIP. Group radius 18, input radius 10, capsule primary action. Sidebar 220 DIP. Primary actions remain visible at 860×580.

| Token | Light | Dark |
|---|---|---|
| Canvas | #F2F2F7 | #111113 |
| Surface | #FFFFFF | #1C1C1E |
| Text | #1C1C1E | #F5F5F7 |
| Secondary text | #636366 | #AEAEB2 |
| Accent | #0066CC | #66B0FF |
| Running | #207A3D | #63DA85 |
| Error | #B42318 | #FF8A80 |

Translucent navigation and restrained highlights communicate floating controls. Forms and session content use opaque surfaces for legibility. High contrast uses Windows semantic colors. Press feedback is immediate, transitions brief, and system animation preferences take priority. Avoid indefinite decorative animation.

Inspect native screenshots at minimum/default sizes, light/dark/high contrast, 100/150/200% DPI. Verify keyboard focus, automation labels, readable validation, and Start/Stop alignment. Review the local anti-slop UI/human skills and Taste app-screen rules; browser-only inspectors do not validate WPF.

Tabler outline is the only icon source. Vendor official SVGs at a pinned upstream commit and keep the MIT notice with all distributions. Use the shared native `TablerIconStyle`: a 24-unit source grid with round 2-unit strokes, rendered at 18 DIP for navigation and section labels and 16 DIP for window controls. Icons inherit their parent foreground so selected, disabled and high-contrast states remain legible. Keep text labels and existing text-only actions. Use Tabler `activity` for the utility's pulse identity and active/inactive tray states; derive the executable and installer assets from that same vector. Native confirmation dialogs omit stock pictograms and retain their accessible text and buttons. Layout geometry, focus indicators and status data marks remain ordinary WPF primitives.
