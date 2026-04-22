# Klavier — Iteration 6+ Plan

## Context

Iterations 1–5.5 delivered: piano rendering, keyboard input, audio output (FluidSynth), transpose/velocity config, sustain pedal (keyboard + UI bar), panic button, toolbar, and UI polish (pressed-state styling, delimiter, theme colors). The app is playable with a PC keyboard. This plan covers the next wave of features to make Klavier configurable, themeable, and usable beyond the foreground window.

---

## Iteration 6: Settings Persistence

**Goal:** User-modified settings survive app restarts.

- **Mechanism:** On change, write overridden settings to a **user settings file** (e.g., `usersettings.json`) separate from the default `appsettings.json`. On startup, load `appsettings.json` first, then overlay `usersettings.json` if it exists.
- **Reset Defaults:** A "Reset defaults" button (added in iteration 7 with the UI) deletes/clears the user settings file and reverts all values to `appsettings.json` defaults.
- **Scope:** All settings that will be exposed in the settings bar (velocity, transpose, volume, sustain mode, and others evaluated at implementation time).

### Key files to modify/create
- `src/Klavier.UI/Options/UserSettingsService.cs` — new, read/write user settings file
- `src/Klavier.UI/Program.cs` or host builder — configure `usersettings.json` as an additional config source
- `src/Klavier.Core/Options/` — if config model changes are needed

---

## Iteration 7: Settings UI Panel

**Goal:** Expose runtime-configurable settings in a togglable bar below the toolbar.

- **Access:** A gear icon/button in the ToolbarView toggles the settings bar visibility.
- **Layout:** A new `SettingsBarView` docked below the toolbar (same pattern as ToolbarView — `DockPanel.SetDock(Dock.Bottom)`).
- **Controls:**
  | Setting | Control | Range |
  |---|---|---|
  | Velocity | Slider + numeric label | 0–127 |
  | Transpose | Slider + numeric label | -24 to +24 semitones |
  | Volume | Slider + numeric label | 0–120% |
  | Sustain Mode | Dropdown | Hold / InvertedHold / Toggle |
- **Other settings** (topmost, audio output device, etc.) will be evaluated at implementation time for inclusion.
- **Persistence:** Settings write back via `UserSettingsService` (iteration 6) and `IOptionsMonitor<PianoConfig>` / `IOptionsMonitor<UIConfig>` for hot-reload.
- **Reset Defaults:** A "Reset defaults" button wired to `UserSettingsService`.

### Key files to modify/create
- `src/Klavier.UI/Views/SettingsBarView.cs` — new
- `src/Klavier.UI/Views/ToolbarView.cs` — add gear toggle button
- `src/Klavier.UI/Views/MainWindow.cs` — dock settings bar, manage visibility
- `src/Klavier.UI/ServiceCollectionExtensions.cs` — register SettingsBarView

---

## Iteration 8: Dark/Light Theme Toggle

**Goal:** Add a light theme and a toggle in the settings bar to switch between dark and light.

- **Toggle location:** Inside the settings bar (not a separate toolbar icon).
- **Approach:** Refactor `KlavierTheme` and `PianoColors` to support swappable palettes (e.g., a `ThemePalette` class with Dark and Light static instances). All brushes derive from the active palette.
- **Light palette:** I propose colors based on the dark theme structure; user tweaks live during implementation.

### Key files to modify/create
- `src/Klavier.UI/Theme/ThemePalette.cs` — new, holds a full set of colors for one theme
- `src/Klavier.UI/Theme/KlavierTheme.cs` — refactor to use active ThemePalette
- `src/Klavier.UI/Theme/PianoColors.cs` — refactor to use active ThemePalette
- `src/Klavier.UI/Views/SettingsBarView.cs` — add theme toggle control
- All views using static brushes — update to react to theme changes

---

## Iteration 9: SoundFont Preset Selection

**Goal:** Let the user see the presets available in the loaded SoundFont and pick which one to play. Persisted across restarts; reacts when the SoundFont path changes.

### Step 1 — Config restructure
Move `Audio.SoundFontPath` into a nested `Audio:SoundFont:Path`, alongside a new `Audio:SoundFont:Preset:{Bank,Program}` section.

- `src/Klavier.Config/AudioConfig.cs` — drop `SoundFontPath`, add `SoundFont` nested property
- `src/Klavier.Config/SoundFontConfig.cs` — new (`Path`, `Preset`)
- `src/Klavier.Config/SoundFontPresetConfig.cs` — new (`Bank`, `Program`)
- `src/Klavier/appsettings.json` — restructure to nested
- `src/Klavier.Audio/FluidSynthAudioOutput.cs` — read `audio.SoundFont.Path` (no preset wiring yet)

### Step 2 — `IUserSettingsService` colon-path API
Replace 2-level `(section, key, value)` with single MS-Configuration-style colon path so nested keys (`Audio:SoundFont:Preset:Bank`) and whole-object writes (`Audio:SoundFont:Preset` ← `{Bank, Program}`) work.

- `src/Klavier.UI/Ports/IUserSettingsService.cs` — `void UpdateSetting(string keyPath, object value)`
- `src/Klavier/Services/UserSettingsService.cs` — walk colon-separated path inside `JsonNode`, creating intermediates
- `src/Klavier.UI/Views/Settings/SettingsPanel.cs` + `SettingsPanel.Helpers.cs` — wire helpers take a single `keyPath` instead of `(section, key)`; call sites updated to `$"{section}:{key}"`

### Step 3 — `ISoundFontPresetProvider` + `ProgramSelect` on init
Enumerate presets from the loaded SF; apply chosen preset on startup; reload + reselect when path changes.

- `src/Klavier.Core/Ports/ISoundFontPresetProvider.cs` — new (`GetPresets()`, `event Action PresetsChanged`)
- `SoundFontPreset` record `(int Bank, int Program, string Name)` — placement (`Klavier.Core.Music` vs `Klavier.Config`) decided at impl time
- `src/Klavier.Audio/FluidSynthAudioOutput.cs` — implements both `IAudioOutput` and `ISoundFontPresetProvider`; on `Initialize` enumerate presets and `_synth.ProgramSelect(_MidiChannel, sfontId, bank, program)`; **throw if configured preset doesn't exist** (mirrors missing-SF-file behavior)
- `src/Klavier.Audio/ServiceCollectionExtensions.cs` — register single instance against both interfaces
- `OnAudioConfigChanged`: detect `SoundFont.Path` change → reload SF + refresh cache + `ProgramSelect` + fire `PresetsChanged`. Detect preset change → `ProgramSelect` only.

### Step 4 — Preset dropdown in `SettingsPanel`
- New "Preset" row with `ComboBox` showing `"bank:program — Name"` (override `ToString()` on the record).
- `SelectedItem` resolved by matching current `audio.SoundFont.Preset.Bank/Program` against the list.
- New `WirePresetComboBox` helper writes the whole preset object at `Audio:SoundFont:Preset` in a single call (avoids two disk writes / two `OnChange` fires).
- Subscribe to `ISoundFontPresetProvider.PresetsChanged` → repopulate items + reselect (UI-thread dispatch).
- `audioConfig.OnChange` also resyncs `SelectedItem` (handles reset + external changes).

### Verification
1. App launches with default preset (0/0); dropdown lists all presets in current SF.
2. Selecting a different preset switches instrument live and persists across restart.
3. Edit `appsettings.json` SF path → restart → dropdown shows that SF's presets.
4. Configure a non-existent preset → app fails to start with a clear error.
5. Existing settings (velocity, transpose, volume, theme, layout, toggles) still persist after the `IUserSettingsService` API change.
6. Reset Defaults restores default preset and the dropdown reflects it.

---

## Iteration 10: SoundFont Path Selector

**Goal:** Let the user change the loaded SoundFont from inside the settings panel, via a file picker. The current file is shown by its "pretty" SoundFont name (from the SF2 `INAM` metadata) with the filename as fallback and a tooltip. Picking a new file auto-recovers if the current preset doesn't exist in the new SF.

### Design
- **Control:** A `Border` (shared rounded outline) containing a `DockPanel` with:
  - Main content: read-only `TextBox` (`IsReadOnly = true`, `Focusable = false`) showing the pretty name.
  - Docked right: small `Border` acting as a clickable button, with a folder glyph rendered via Avalonia `PathIcon` + SVG `Geometry` (themed from `ThemePaletteProvider`). Hover highlight via the same `ActivableControl` pattern as `ToolbarButton`. If the custom vector proves fiddly at impl time, fall back to `…` ellipsis text.
- **Tooltip:** when the textbox shows `INAM`, tooltip shows the filename. When the textbox already shows the filename (INAM missing/empty), no tooltip.
- **Picker:** `TopLevel.GetTopLevel(this).StorageProvider.OpenFilePickerAsync` with a single file type `SoundFont` filtering `*.sf2` / `*.sf3`. Default start location = folder of current `SoundFont.Path`. Multi-select off.
- **Row placement:** After the `Volume` row, before the `Preset` row (so the flow is: pick SF → pick preset inside it).

### Step 1 — `SoundFontParser`: INAM extraction
Add a second entry point to the parser that returns the SoundFont's display name from the `INFO` LIST → `INAM` sub-chunk. Independent from preset parsing (single-responsibility).

- `src/Klavier.SoundFont/SoundFontParser.cs`:
  - New `public static string? ParseSoundFontName(string filePath)` — walks to `INFO` LIST, finds `INAM` sub-chunk, reads null-terminated ASCII. Returns `null` if missing or empty/whitespace.
  - Reuse existing `ReadFourCC` / `SkipToListChunk` / `SkipToSubChunk` / `ReadNullTerminated` helpers — generalize them if needed (they're already generic over the four-CC argument).

### Step 2 — SoundFont row UI + picker wiring
All changes in `src/Klavier.UI/Views/Settings/`.

- `SettingsPanel.Helpers.cs`:
  - New `CreateSoundFontPickerRow(string label, TextBox pathDisplay, Border pickerButton) → DockPanel` helper that assembles the label + inline-icon control row.
  - New `CreateSoundFontPathDisplay(string initialText) → TextBox` — read-only, focusable=false, themed background matching the shared border.
  - New `CreatePickerIconButton() → Border` — small bordered button, hover highlight, `PathIcon` child with folder geometry (document the SVG path data inline — a simple folder shape).
  - New `WireSoundFontPicker(Border pickerButton, TextBox pathDisplay, SoundFontConfig currentConfig)` — handles `PointerPressed`:
    1. `TopLevel.GetTopLevel(pickerButton)?.StorageProvider.OpenFilePickerAsync(...)` with `.sf2`/`.sf3` filter and suggested start folder = directory of current path.
    2. If user cancels → return.
    3. Parse new presets via `SoundFontParser.ParsePresets(newPath)`.
    4. Decide preset: if current `(Bank, Program)` exists in new presets → keep it; else pick first available (prefer `(0, 0)`, else lowest by `(Bank, Program)` ordering).
    5. Write the whole nested SoundFont object in a single call: `_settingsService.UpdateSetting(ConfigKey.Of(AudioConfig.SectionName, nameof(AudioConfig.SoundFont)), new { Path = newPath, Preset = new { Bank, Program } })`. One disk write → one `OnChange` fire → `FluidSynthAudioOutput` reloads + `PresetsChanged` → `SettingsPanel` preset-combo resync already wired in iteration 9.
  - New `GetSoundFontDisplayName(string path) → (string display, string? tooltip)` — returns `INAM` + filename tooltip, or filename + no tooltip. Silent fallback on parse errors (return filename, log nothing — parser errors during playback are logged elsewhere).
- `SettingsPanel.cs`:
  - New `_SoundFontLabel` const (`"SoundFont"`).
  - Create the textbox + button + row; place before the preset row in the `StackPanel.Children` list.
  - In the existing `audioConfig.OnChange` handler, also resync the textbox text + tooltip on `SoundFont.Path` change (covers Reset Defaults and external edits).

### Verification
1. App launches; SoundFont row displays the loaded SF's pretty name (or filename if SF has no INAM); tooltip behavior per spec.
2. Click the folder icon → native file dialog opens, filtered to `.sf2`/`.sf3`, starting in the current SF's folder.
3. Pick a new SF whose preset list *includes* the current `(Bank, Program)` → path + preset persist; audio continues with same instrument; pretty name updates; preset dropdown refreshes and keeps current selection.
4. Pick a new SF that does *not* contain the current preset → preset auto-falls back to first available; `usersettings.json` contains a single consistent write of `Audio:SoundFont` = `{Path, Preset:{Bank,Program}}`; dropdown reflects the new selection.
5. Cancel the picker → no change to disk or UI.
6. Edit `appsettings.json` path manually → textbox and tooltip update via `audioConfig.OnChange`.
7. Reset Defaults → textbox reverts to default SF's pretty name/tooltip; preset reverts; dropdown reflects it.
8. Pick a corrupt/non-SF `.sf2` file (manually renamed) → config is still written (matches current behavior), runtime errors are logged by `FluidSynthAudioOutput`; app stays alive. Intentionally out of scope for this iteration.

### Key files to modify
- `src/Klavier.SoundFont/SoundFontParser.cs` — add `ParseSoundFontName`
- `src/Klavier.UI/Views/Settings/SettingsPanel.cs` — new row + `OnChange` textbox resync
- `src/Klavier.UI/Views/Settings/SettingsPanel.Helpers.cs` — picker row/button/textbox helpers + wiring

---

## Iteration 11: Key Color Customization

### Context

Today the piano uses five hardcoded colors in `src/Klavier.UI/Theme/PianoColors.cs` (WhiteKey, WhiteKeyPressed, BlackKey, BlackKeyPressed, KeyBorder), consumed only from `src/Klavier.UI/Views/Piano/PianoKeyControl.cs:14-18`. The theme's `Accent` is already used to tint the pressed-key borders/text in the same file (`PianoKeyControl.cs:19-20`). The `KeyPressed` variants look like the base key mixed toward the accent; if a computed blend reproduces the current feel, the statically-stored pressed colors become redundant, the palette shrinks, and the pressed states automatically follow any future accent change.

**Goal:** Let users customize the piano key colors (and possibly the theme accent) via hex text fields in the settings panel. Changes are restart-required (consistent with the existing `Theme (restart)` row).

### Scope (narrowed from original)

- **Not** in scope: `AppBackground`, `NeutralSurface`, `ContrastedSurface`, `TextPrimary`, `Divider` (theme chrome stays theme-controlled).
- **Definitely** in scope: `WhiteKey`, `BlackKey`, `KeyBorder` (3 base piano colors).
- **Conditionally** in scope: `Accent` - decision deferred to after Step 1.
- **Removed from the palette (if Step 1 succeeds):** `WhiteKeyPressed`, `BlackKeyPressed` become computed blends of their base color and `Accent`, no longer stored.
- **Overrides are shared across themes** (one set, layered over whichever theme is active).
- **Hex TextBox is the swatch**: the TextBox background itself is the current color; no separate rectangle.
- **Validation**: revert-on-blur when the typed value is not a valid `#RRGGBB`.
- **Live feedback**: background updates only when a complete valid hex is typed.

### Step 1 - Replace pressed variants with computed blends from Accent

Refactor `PianoColors` to compute `WhiteKeyPressed` and `BlackKeyPressed` via the existing `ThemePalette.Mix` helper (`src/Klavier.UI/Theme/ThemePalette.cs:40-46`), reading `Accent` from `ThemePaletteProvider`.

- `src/Klavier.UI/Theme/PianoColors.cs`:
  - Keep `WhiteKey`, `BlackKey`, `KeyBorder` as `static readonly Color`.
  - Replace the two pressed-state fields with `static Color` properties returning a mix of base + `ThemePaletteProvider.Accent`.
  - The `Mix` helper in `ThemePalette.cs` is currently `private static`; promote it to `public static` (or extract to a small `ColorMath` static class) so `PianoColors` can call it. Either is fine; chosen at implementation time.
- `src/Klavier.UI/Views/Piano/PianoKeyControl.cs:15,17`:
  - The two pressed brushes are `static readonly SolidColorBrush new(PianoColors.WhiteKeyPressed)`. Since `PianoColors.WhiteKeyPressed` is now a property, the brushes still capture the value once at static init (fine - restart-required already).
- Find mix ratios visually matching the current values:
  - Current: `WhiteKeyPressed = #D4DEF4` - plausible match: `Mix(WhiteKey #FAFAFA, Accent #3A60BF, 0.78)` ≈ `#D0D7EC`. Fine-tune at impl time.
  - Current: `BlackKeyPressed = #0E1530` - current is more saturated than a simple linear mix of `#1C1C1C` and `#3A60BF`; may need a slightly higher accent weight (e.g., `Mix(BlackKey, Accent, 0.35)` or a darken-then-mix). Verify by eye.

**Verification for Step 1:** run the app, press keys, confirm pressed colors look close to the current hardcoded values. If Accent is changed at design time, pressed states move with it.

### Step 2 - Decide Accent inclusion; prototype the hex-TextBox control

With pressed-state derivation working, decide whether `Accent` joins the editable set. If included, the editable list is **4 colors**: `WhiteKey`, `BlackKey`, `KeyBorder`, `Accent`. Otherwise **3 colors**.

Prototype the hex-TextBox control on **one row only** so we agree on visuals before replicating:

- `src/Klavier.UI/Views/Settings/SettingsPanel.Helpers.cs`:
  - New `CreateHexColorTextBox(Color initialColor) -> TextBox`:
    - `Text = $"#{color:X6}"`, max length 7, monospace-ish feel fine using the existing font.
    - `Background = new SolidColorBrush(color)`.
    - `Foreground = ContrastingTextColor(color)` - a small helper (luminance-based: white if (`0.299*R + 0.587*G + 0.114*B`) < 128, else black). Place alongside other helpers in this file.
    - `BorderBrush` and `BorderThickness` kept minimal so the background reads as a swatch.
  - Validation wiring (also in this file):
    - On `TextChanged`: try `Color.Parse` on the current text; if valid and matches `^#[0-9A-Fa-f]{6}$`, update background + foreground **and** call `_settingsService.UpdateSetting(keyPath, hex)`. If invalid, do nothing visible.
    - On `LostFocus`: if the current text is not a valid `#RRGGBB`, revert the TextBox's `Text` to the last-valid hex.
- Single row inserted into `SettingsPanel.cs` for the prototype (e.g., `WhiteKey`), placed just above the Reset row. No config read yet - just demonstrates the control.

**Verification for Step 2:** app runs, the prototype row shows the current WhiteKey as TextBox background, typing a valid hex updates the swatch, typing garbage and tabbing out reverts.

### Step 3 - Config + startup override wiring + full row set

Once Step 2's control is agreed:

- `src/Klavier.Config/UIColorsConfig.cs` - new record/class with three (or four, if Accent) `string?` hex properties: `WhiteKey`, `BlackKey`, `KeyBorder`, (`Accent`). Nullable = "no override, use default".
- `src/Klavier.Config/UIConfig.cs` - add nested property `UIColorsConfig Colors { get; init; } = new();`.
- `src/Klavier/appsettings.json` - no entry needed (all nullable); user overrides live in `usersettings.json` under `UI:Colors:{Key}`.
- Startup override injection - single read path, **before any UI is instantiated**:
  - `src/Klavier.UI/Theme/PianoColors.cs` - expose an `Initialize(UIColorsConfig colors)` method that replaces the static backing values for `WhiteKey`, `BlackKey`, `KeyBorder` (refactor the existing `static readonly` fields into static properties backed by private fields so they can be assigned once at startup).
  - `src/Klavier.UI/Theme/ThemePaletteProvider.cs` - if Accent is in scope, either clone `Active` with the override or add a parallel `OverrideAccent(Color)` method. Details at impl time.
  - Caller wiring: in `src/Klavier.UI/ServiceCollectionExtensions.cs` or in `Program.cs` (wherever the host starts the UI), resolve `IOptions<UIConfig>` and call `PianoColors.Initialize(ui.Colors)` once, before any view is constructed.
- Full row set in `SettingsPanel.cs`:
  - Labels `"White Key (restart)"`, `"Black Key (restart)"`, `"Key Border (restart)"`, (`"Accent (restart)"`).
  - Each row: label + `CreateHexColorTextBox` wired to `ConfigKey.Of(UIConfig.SectionName, nameof(UIConfig.Colors), nameof(UIColorsConfig.WhiteKey))` etc.
  - Inserted after `Keyboard Layout` row, before `Reset Defaults`.
  - Reset button already clears user settings (Iteration 6); after Reset, on restart the overrides are gone and defaults apply - no additional wiring needed.

**Verification for Step 3:**
1. Launch: color rows show current effective colors (defaults on first run, overrides on subsequent).
2. Type a valid hex in a color row -> swatch updates, `usersettings.json` gets `UI:Colors:WhiteKey = "#XXXXXX"`. No live change to piano keys (restart-required).
3. Restart -> piano keys use overridden colors. Pressed states (from Step 1) follow the override automatically (since they're computed blends).
4. Type a garbage value -> swatch stays on last-valid, tabbing away reverts text.
5. Reset Defaults -> on next restart, piano keys revert to built-in defaults; color rows reflect the same on reload.
6. Existing settings (velocity, transpose, volume, soundfont, preset, toggles) still persist.

### Key files to modify/create
- `src/Klavier.UI/Theme/PianoColors.cs` - refactor to computed pressed states + `Initialize` method
- `src/Klavier.UI/Theme/ThemePalette.cs` - promote `Mix` to public (if chosen path)
- `src/Klavier.Config/UIColorsConfig.cs` - new
- `src/Klavier.Config/UIConfig.cs` - nested `Colors` property
- `src/Klavier.UI/Views/Settings/SettingsPanel.cs` - new color rows
- `src/Klavier.UI/Views/Settings/SettingsPanel.Helpers.cs` - `CreateHexColorTextBox`, validation, contrast helper
- `src/Klavier.UI/ServiceCollectionExtensions.cs` or `src/Klavier/Program.cs` - call `PianoColors.Initialize` at startup

---

## Iteration 12: Custom Keybinds

### Context

Users currently pick between three shipped layouts (`qwerty`, `azerty`, `dvorak-fr`) from the `Keyboard Layout` dropdown in the settings panel. Each layout is a JSON file at `src/Klavier/mappings/*.json` with the same schema: `blackKeyModifier` + `whiteKeys` / `blackKeys` dictionaries keyed by `PhysicalKey` enum names, each entry carrying `pitch` (MIDI 0-127) and `label` (display string).

**Goal:** Let users create and edit their own layouts *in the same JSON format*, so `KeyboardMappingProvider` and `KeyboardInputHandler` need only minimal changes. The author UI is a separate window with a **piano-driven wizard**: the editor highlights each piano key in chromatic order (C2→C7), and the user presses a PC key to capture `(PhysicalKey, KeySymbol)` for that note. `KeySymbol` becomes the label, avoiding any scancode-to-character conversion (see `project_keyboard_layout_strategy.md` memory).

### Design decisions (locked in)

- **Editor flow**: piano-driven wizard. Sequentially highlights each piano key C2→C7; user presses a PC key to bind. Skip / Back / Save buttons for navigation. No schema-clicking.
- **Starting point**: clone the currently selected layout. Each target piano key is pre-filled from the clone; pressing a PC key remaps it, `[Skip]` keeps the existing binding.
- **Black-key modifier**: per-layout, picked upfront at the top of the editor window (dropdown: `Shift` / `Ctrl` / `Alt`, defaulting to the clone source's modifier). Same modifier for every black piano key.
- **Reserved keys during wizard**: `Escape` cancels the wizard (confirm-discard if dirty). `Space` is rejected with an inline warning ("Space is reserved for sustain").
- **PC key reuse within the session**: warn + auto-unbind the previous piano key that used that PC key. Result: at most one piano key per PC key in the final JSON.
- **Piano range**: C2-C7 (MIDI 36-96), 61 piano keys total (36 white + 25 black), matching built-ins.
- **Storage**: `%LocalAppData%/Klavier/mappings/*.json`. `KeyboardMappingProvider` reads from *two* roots (built-ins at `AppContext.BaseDirectory/mappings/` + customs at user dir), merged **with user dir winning on name collision**. Saving a layout with a built-in name (e.g. `qwerty`) is allowed and effectively creates a user-side override of the built-in; the original built-in file is untouched and can be restored by deleting the user override from disk.
- **CRUD**: Create + Edit. No Delete in this iteration (user removes the user-dir file manually to revert an override or drop a custom).
- **Naming**: prompted at save time via a small dialog. Empty name and invalid filename chars are rejected. Built-in stems are **allowed** (they create overrides). Collision with an existing user-dir file (including an existing override) prompts confirm-overwrite.

### Step 1 - Two-location mapping discovery

- `src/Klavier.UI/Input/Mapping/KeyboardMappingProvider.cs`:
  - `UserMappingsDirectory`: static property returning `Path.Combine(Environment.GetFolderPath(SpecialFolder.LocalApplicationData), "Klavier", "mappings")`; ensure the directory exists on first use.
  - `GetAvailableLayouts()`: scan both app's `mappings/` and user dir; merge stems with **user dir winning** on collision. Return sorted alphabetically.
  - `Load(string layoutName)`: check user dir first, fall back to app dir; throw if not found in either. (Already correct behavior — user override shadows built-in.)
  - New `Save(string name, KeyboardMappingDto dto)`: validate `name` (via `LayoutNameValidator`), serialize with `JsonSerializerOptions { WriteIndented = true }` to `{UserMappingsDirectory}/{name}.json`.
  - New `LayoutsChanged` event raised after `Save`, so `SettingsPanel` can refresh `ItemsSource`.
- `src/Klavier.UI/Input/Mapping/LayoutNameValidator.cs` (new):
  - `TryValidate(string? name, out string? reason)`: rejects null/whitespace and path separators / OS-reserved chars (`Path.GetInvalidFileNameChars`). Built-in stems are permitted (they become user overrides).
  - Used by both `KeyboardMappingProvider.Save` and the naming dialog (for live error feedback).

### Step 2 - Editor window

New directory: `src/Klavier.UI/Views/KeybindsEditor/`.

- `KeybindsEditorWindow.cs`:
  - Constructor params: `KeyboardMapping cloneSource`, `string? existingLayoutName` (null = Create, non-null = Edit), `KeyboardMappingProvider provider`.
  - Sized ~800×500. Theming via `ThemePaletteProvider`.
  - **Header**: `Modifier for black keys:` label + ComboBox (`Shift` / `Ctrl` / `Alt`), defaulting to `cloneSource.BlackKeyModifier`.
  - **Piano view**: reuse/adapt `PianoView` to render C2-C7. Note-label display forced on (user needs to know which piano key is the target regardless of their normal display setting). The note-label rendering respects `UIConfig.NoteNameStyle` (Anglo-Saxon vs. Solfege) just like the main piano — the editor must never hardcode `C2`-style names regardless of user locale. Non-interactive for input; highlights the current target key using the existing `UserPalette.WhiteKeyPressed` / `UserPalette.BlackKeyPressed` brushes (visually identical to a real press, keeps the editor consistent with the main piano).
  - **Status strip below piano**:
    - Large text: `"Bind <NoteName>"` where `<NoteName>` is rendered via the same note-name formatter the main piano uses, honoring `UIConfig.NoteNameStyle` (so a French user with Solfege sees `"Bind Do2"`, an Anglo user sees `"Bind C2"`).
    - Sub-line: `"Pressed: <label>"` when a binding was just captured, otherwise `"Press a PC key"`.
    - Inline warning area (red text) for reserved-key and wrong-modifier warnings.
  - **PC keyboard schema (read-only, below the status strip)**: a visual representation of a standard PC keyboard showing only the keys that are valid binding targets (letters A-Z, digits 0-9, standard punctuation, plus the modifier key highlighted as the layout's `blackKeyModifier`). Each key displays the note name of its current binding, formatted through the same style-aware note-name formatter used by the piano view. When a `PhysicalKey` has both a white and a black binding, show them stacked (white on top, black below — separator deferred to impl, `/` is a placeholder). Unbound keys are rendered empty. The schema updates live as the user makes changes during the wizard and is never interactive — it's pure feedback so the user can see at a glance which PC keys are free and which are taken. The currently-used modifier label is visually emphasized (e.g., a subtle accent border on the `Shift` block when modifier = Shift). Layout of the rendered keyboard keys uses Avalonia `PhysicalKey` geometry (US ANSI main block) regardless of the OS keyboard layout, since bindings are indexed by `PhysicalKey` — the on-key labels reflect what the user actually pressed (their `KeySymbol`), so AZERTY users see "A" on the physical-Q key.
  - **Shared note-name formatter**: the same helper the main `PianoView` uses to convert `NotePitch` → displayed string must be reused here. If it's currently inlined in `PianoView`, extract it to a static helper in `src/Klavier.UI/Views/Piano/` (or `src/Klavier.Core/` if non-UI) so both views call the same code path.
  - **Buttons row**: `[Back]` (disabled at index 0) / `[Skip]` (advances, keeps existing clone binding for this piano key) / `[Save]`.
  - **Key capture** (`OnKeyDown`):
    - `Escape` → close; if dirty, small confirm dialog "Discard changes?".
    - `Space` → show warning; do not bind.
    - Other presses: resolve based on current target:
      - **White piano key target**: require no modifier held. If modifier present → warn "White keys must be pressed plain". Else, write entry `(PhysicalKey, label)` to in-memory `whiteKeys`. Label derivation: `KeySymbol` if non-empty, else `PhysicalKey.ToString()` (handles keys like F1 with no KeySymbol). If the resulting label is a single latin letter (a-z or A-Z), normalize to upper-case so stored labels match existing built-ins ("Q", "A", ...). Non-letter labels (digits, punctuation, non-latin characters) are stored as captured.
      - **Black piano key target**: require exactly the configured `blackKeyModifier` held (no extras). Wrong / missing / extra → warn. Else, write entry to in-memory `blackKeys` using the same label rules. Note: Shift+letter already yields an upper-case `KeySymbol` on most layouts, but the upper-case normalization is applied uniformly regardless.
    - On valid press: if the `PhysicalKey` was already bound elsewhere in this session, remove that prior entry and flash a warning ("T was bound to C2; moved to D2"). Advance to next piano key.
  - On modifier-dropdown change: if user already bound some black keys, show a confirm dialog "Changing modifier will clear N bindings." Clear `blackKeys` on confirm, revert dropdown on cancel.

- `NameLayoutDialog.cs` (new, same dir): small modal child window with a single TextBox + `[Save]` / `[Cancel]`. Live-validates via `LayoutNameValidator`; disables `[Save]` while invalid. On Save-to-existing-custom, shows confirm-overwrite inline.

### Step 3 - Settings panel layout row

- `src/Klavier.UI/Views/Settings/SettingsPanel.cs`:
  - Today (line 200): `CreateRow(_KeyboardLayoutLabel, keyboardLayoutCombo)`.
  - Wrap the ComboBox + a `[+]` button (create) + a `[Edit]` button into a horizontal `DockPanel`. Use existing iconic button style (similar to `soundFontPickerButton` in `SoundFont` row). Both buttons are always visible — any layout (built-in or custom) can be edited, because editing writes to the user dir and shadows the built-in.
  - `[+]` button click: open `KeybindsEditorWindow` with `cloneSource = current loaded mapping`, `existingLayoutName = null`. Save dialog starts with an empty name field.
  - `[Edit]` button click: open `KeybindsEditorWindow` with `cloneSource = current loaded mapping`, `existingLayoutName = current`. Save dialog pre-fills the name field with the current layout's name (keeping it as-is will overwrite or create the user override; changing it will create a new layout).
- `src/Klavier.UI/Views/Settings/SettingsPanel.Helpers.cs`:
  - New `CreateLayoutRow(string label, ComboBox combo, Border createBtn, Border editBtn) -> DockPanel` helper.
  - New `CreateIconButton(Geometry icon, string tooltip) -> Border` if one doesn't already exist (see `CreateSoundFontPickerButton` - may already fit).

### Step 4 - Save & hot-reload

- Editor `[Save]` button:
  1. Open `NameLayoutDialog` (prefilled with `existingLayoutName` if editing).
  2. On dialog confirm: `provider.Save(name, dto)` writes file; raises `LayoutsChanged`.
  3. Set `UIConfig.KeyboardLayout = name` via `_settingsService.UpdateSetting(ConfigKey.Of(UIConfig.SectionName, nameof(UIConfig.KeyboardLayout)), name)` - triggers existing `UIConfig.OnChange` pipeline; `KeyboardInputHandler` reloads the mapping automatically.
  4. Close editor.
- `SettingsPanel` subscribes to `KeyboardMappingProvider.LayoutsChanged` and refreshes `keyboardLayoutCombo.ItemsSource` on the UI thread.
- Edit flow (saving the currently active layout): hot-reload picks up the new bindings automatically via the same `UIConfig.OnChange` chain (the `KeyboardLayout` value hasn't changed but the file content did - may need explicit reload. Handled by also raising a `LayoutsChanged` → `SettingsPanel` forces `KeyboardInputHandler` reload via a deliberate re-write of the same `UIConfig.KeyboardLayout` value, OR by exposing a reload method on `KeyboardInputHandler`. Choice deferred to impl time; simpler of the two wins.).

### Step 5 - Edge cases & polish

- **No KeySymbol**: for physical keys without a character (F1, arrows, etc.), fallback label is `PhysicalKey.ToString()`. Acceptable for v1; users can see what they pressed.
- **Rename via Save As**: if editing and the user changes the name in the save dialog, a new file is written; old file stays (no Delete this iteration). Matches the "No Delete" decision.
- **Missing active layout at startup**: if `UIConfig.KeyboardLayout` references a file that no longer exists, `KeyboardMappingProvider.Load` currently throws. Deferred: graceful fallback to first available layout + log a warning. Not in scope for this iteration unless trivial.

### Verification

1. Click `[+]` next to the layout dropdown. Editor opens; first piano key (C2) is highlighted; current clone's binding for C2 shows in the status strip.
2. Change modifier dropdown from Shift to Ctrl on an unsaved new layout → OK, no confirm (no black bindings yet).
3. Press a plain PC key (e.g., Q) while C2 highlighted → binding captured; label = KeySymbol; advances to C#2.
4. On C#2 (black), press Ctrl+W → binding captured in `blackKeys`.
5. On C#2 (black), press Shift+W instead → warning "Black keys require Ctrl"; no binding.
6. Later, on D2, press Q again → warning "Q was bound to C2; moved to D2"; C2 in-memory binding cleared.
7. Press Space on any target → warning "Space is reserved for sustain"; no binding; no advance.
8. Press Escape → editor closes (confirm prompt since dirty).
9. Click Save → naming dialog opens. Type a name with invalid chars (e.g., `foo/bar`) → inline error. Type "mine" → Save enabled. Confirm. `%LocalAppData%/Klavier/mappings/mine.json` exists with matching DTO. Dropdown refreshes. `UI:KeyboardLayout = "mine"` in `usersettings.json`. Piano plays with new bindings without restart.
10. Select "qwerty" in the dropdown, click `[Edit]`, tweak a key, click Save → dialog pre-filled with "qwerty"; confirm as-is → user override `%LocalAppData%/Klavier/mappings/qwerty.json` written. `GetAvailableLayouts()` still returns "qwerty" only once. Playback uses the new bindings immediately.
11. Delete `%LocalAppData%/Klavier/mappings/qwerty.json` from disk → on next launch, built-in qwerty is active again (no ghost override).
12. Click `[Edit]` while "mine" is selected → editor opens pre-filled; header modifier matches saved; tweak one key; Save → overwrites user-dir file in place; live playback updates.
13. Remove `mine.json` from disk manually, relaunch app → dropdown no longer lists "mine". If it was the active layout, app logs a warning and falls back to the first available (deferred - graceful handling flagged above).
14. Regression: both `[+]` and `[Edit]` buttons are visible for every layout; QWERTY / AZERTY / Dvorak-FR still work identically when no user override exists.

### Key files to modify/create

- `src/Klavier.UI/Input/Mapping/KeyboardMappingProvider.cs` - two-location scan with user-dir precedence, `Save`, `LayoutsChanged` event
- `src/Klavier.UI/Input/Mapping/LayoutNameValidator.cs` - new
- `src/Klavier.UI/Views/KeybindsEditor/KeybindsEditorWindow.cs` - new wizard window
- `src/Klavier.UI/Views/KeybindsEditor/NameLayoutDialog.cs` - new modal save dialog
- `src/Klavier.UI/Views/Settings/SettingsPanel.cs` - wrap layout row with Create/Edit buttons; subscribe to `LayoutsChanged`
- `src/Klavier.UI/Views/Settings/SettingsPanel.Helpers.cs` - `CreateLayoutRow` helper; possibly a reusable icon-button helper

---

## Iteration 13+ (Backlog)

These items are deferred — no detailed design yet.

- **MIDI Input** — Connect external MIDI keyboards/controllers
- **MIDI Recording (Output)** — Record played notes to a MIDI file
- **Sustain Half-Pedal** — Continuous CC64 value range instead of on/off
- **88 keys piano** — Support a wider piano variant
- **Sharphook** — Capture keyboard input even when Klavier is not the focused window.

---

## Verification

Each iteration should be verified by:
1. **Build:** `dotnet build` passes with no errors/warnings
2. **Run:** Launch the app and test the new feature end-to-end
3. **Regression:** Existing features (play notes, sustain, panic, toolbar) still work correctly
4. **Config hot-reload:** Changed settings take effect without app restart (where applicable)
