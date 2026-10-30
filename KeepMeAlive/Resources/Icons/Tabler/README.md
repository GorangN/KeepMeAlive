# Tabler icon sources

These outline SVGs and `LICENSE.txt` are copied unchanged from [Tabler Icons](https://github.com/tabler/tabler-icons) at commit [`ed177a085b652fe04ec4f37ced867c94621e7840`](https://github.com/tabler/tabler-icons/tree/ed177a085b652fe04ec4f37ced867c94621e7840). The upstream MIT license is retained alongside the sources.

`manifest.json` records the source filenames, WPF resource keys, and SHA-256 hashes. The native application uses the generated `../Tabler.xaml` geometry dictionary; no SVG parser, webfont, icon package, or network request is required at runtime. The geometry retains Tabler's 24 x 24 coordinate system, 2-unit strokes, and round caps/joins. Foreground colors inherit from the owning control and theme.

The application/tray artwork uses the same `activity` vector on a transparent background. The inactive variant is `#636366`; active is `#0066CC`. Both ICOs contain 16, 20, 24, 32, 48, 64, 128, and 256 pixel PNG frames. The installer BMPs place the same blue vector on the white background required by their opaque format.

`ComboBoxDownArrowGeometry` is a filled outline derived by widening Tabler's `chevron-down` path using the same 2-unit round-cap, round-join stroke. It supplies MahApps' existing dropdown glyph resource hook without replacing its input behavior or popup template.

Regenerate from the repository root, offline, using Windows PowerShell and the installed Windows WPF renderer:

```powershell
powershell.exe -NoProfile -STA -File scripts/Generate-TablerIcons.ps1
```

The generator verifies all vendored SVG/license hashes and rejects unsupported SVG primitives or styling rather than silently approximating them. `generated-checksums.json` records the generated dictionary, ICOs, and installer BMPs. Commit source, manifest, generated output, and checksum changes together when deliberately updating the upstream pin.
