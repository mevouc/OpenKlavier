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

**Goal:** Let users customize the full color palette via hex code text fields.

- **Scope:** Full palette — keys, pressed states, background, toolbar, accent, text. Every color in `ThemePalette` is editable.
- **UI:** Hex code text fields with a color preview swatch next to each field. Lives in the settings bar or an expanded section.
- **Persistence:** Custom colors saved via the user settings file (iteration 6 infrastructure). Overrides the base theme.

### Key files to modify/create
- `src/Klavier.UI/Views/SettingsBarView.cs` — add color customization section
- `src/Klavier.UI/Theme/ThemePalette.cs` — support user overrides
- `src/Klavier.UI/Options/` — possible new config type for user colors

---

## Iteration 12: Custom Keybinds

**Goal:** Let users remap PC keyboard keys to piano notes, with preset layouts.

- **UI:** Opens a **separate window** for key mapping configuration (design details deferred to implementation time).
- **Presets:** Ship QWERTY and AZERTY preset layouts. Users can also create fully custom layouts.
- **Integration:** Builds on the existing `KeyboardMappingProvider` and `PhysicalKey → NotePitch` mapping system.

### Key files to modify/create
- `src/Klavier.UI/Views/KeybindWindow.cs` — new, separate window for mapping
- `src/Klavier.UI/Input/KeyboardMappingProvider.cs` — support multiple presets + custom
- `src/Klavier.UI/Options/` — keybind config persistence
- `src/Klavier.UI/Views/SettingsBarView.cs` — button to open keybind window

---

## Iteration 13: SharpHook (Background Keyboard Capture)

**Goal:** Capture keyboard input even when Klavier is not the focused window.

- **Mode:** Togglable — off by default. Setting in the settings bar or toolbar.
- **Library:** SharpHook (chosen from earlier research). A second research/validation phase at implementation time to confirm compatibility and behavior on Windows.
- **Architecture:** SharpHook hooks run on a background thread; events are marshaled to the existing `KeyboardInputHandler` pipeline.

### Key files to modify/create
- `src/Klavier.UI/Input/GlobalKeyboardHook.cs` — new, SharpHook integration
- `src/Klavier.UI/Input/KeyboardInputHandler.cs` — accept events from both Avalonia and SharpHook
- `src/Klavier.UI/Views/SettingsBarView.cs` — toggle for background capture
- `src/Klavier.UI/ServiceCollectionExtensions.cs` — register hook service
- NuGet: add SharpHook package

---

## Iteration 14+ (Backlog)

These items are deferred — no detailed design yet.

- **MIDI Input** — Connect external MIDI keyboards/controllers
- **MIDI Recording (Output)** — Record played notes to a MIDI file
- **Sustain Half-Pedal** — Continuous CC64 value range instead of on/off
- **Result\<T\> pattern for KeyboardMappingProvider** — Error handling improvement

---

## Verification

Each iteration should be verified by:
1. **Build:** `dotnet build` passes with no errors/warnings
2. **Run:** Launch the app and test the new feature end-to-end
3. **Regression:** Existing features (play notes, sustain, panic, toolbar) still work correctly
4. **Config hot-reload:** Changed settings take effect without app restart (where applicable)
