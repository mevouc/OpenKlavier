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

## Iteration 9: Key Color Customization

**Goal:** Let users customize the full color palette via hex code text fields.

- **Scope:** Full palette — keys, pressed states, background, toolbar, accent, text. Every color in `ThemePalette` is editable.
- **UI:** Hex code text fields with a color preview swatch next to each field. Lives in the settings bar or an expanded section.
- **Persistence:** Custom colors saved via the user settings file (iteration 6 infrastructure). Overrides the base theme.

### Key files to modify/create
- `src/Klavier.UI/Views/SettingsBarView.cs` — add color customization section
- `src/Klavier.UI/Theme/ThemePalette.cs` — support user overrides
- `src/Klavier.UI/Options/` — possible new config type for user colors

---

## Iteration 10: Custom Keybinds

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

## Iteration 11: SharpHook (Background Keyboard Capture)

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

## Iteration 12+ (Backlog)

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
