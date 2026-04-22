# Klavier — Iteration 13+ Plan

## Context

Plans 01 and 02 took Klavier from a 2-line console POC to a playable desktop piano with hexagonal architecture, FluidSynth audio, keyboard input (3 layouts + custom-layout editor), settings persistence, a themable settings panel, a SoundFont picker with preset selection, and configurable key colors. The app today is fully playable and configurable, but all its output is driven by the user pressing keys, and the settings panel has accumulated 17 flat rows with several technical labels (e.g. "Topmost") that are obvious to developers but opaque to end users.

This plan tackles two axes:

- **UX polish first**: the settings panel gets section headers and friendlier wording so every later iteration that adds a row has a clear home for it.
- **Passive content second**: the app should be able to load a MIDI file and *show it* as falling note bars above the piano (Synthesia / "Piano from Above" style), with optional audio playback through FluidSynth. Once MIDI file playback is in, three smaller follow-ups finish the feature set promised in plan 02's backlog: recording the user's playing back to a .mid file, upgrading the sustain pedal from binary to continuous, and expanding the piano from 61 to 88 keys.

The order is:

1. **Iteration 13 — Settings Panel Refinement** (UX polish, high-level)
2. **Iteration 14 — MIDI File Playback + Falling-Notes Visualization** (the big one, detailed below)
3. **Iteration 15 — MIDI Recording** (high-level)
4. **Iteration 16 — Sustain Half-Pedal** (high-level)
5. **Iteration 17 — 88-Key Piano** (high-level)
6. **Backlog one-liners**: MIDI Input, SharpHook

Settings-panel polish goes first because iterations 14-17 will each want to add at least one new row (lookahead, audio-enabled persistence, recording defaults, sustain-max-value, possibly a piano-range selector), and those rows land much better once the panel has clear sections to slot them into. MIDI file playback is the natural entry point for the content axis because `.mid` is the de-facto interchange format for piano visualization (Synthesia, Piano from Above, Rousseau-style YouTube tutorials all consume MIDI files), and once `Melanchall.DryWetMidi` is in the solution the recording iteration comes cheaply on top of the same library.

---

## Iteration 13: Settings Panel Refinement

**Goal:** Turn the current 17-row flat settings panel into a grouped, better-documented surface. No new behavior — only layout, labels, and tooltips. This is pure UX polish that sets up every subsequent iteration to slot its new rows into a clear home.

### Design decisions (locked in)

- **Section grouping (5 sections):**
  1. **Sound & Playback** — Velocity, Transpose, Volume, Sustain Mode, SoundFont, Preset
  2. **Piano Display** — Show Key Labels, Show Note Labels, Note Name Style
  3. **Keyboard** — Keyboard Layout (with the existing Create / Edit buttons)
  4. **Window** — Topmost
  5. **Theme & Colors** — Theme, Accent, White Key, Black Key, Key Border
  - Reset Defaults button stays at the bottom of the panel, outside any section.
- **Section header style:** bold title (slightly larger than row labels) with a 1-pixel divider line below. Reuses `ThemePaletteProvider.Divider` for consistency with the divider already used between the piano and the panel. Static — **not** collapsible in this iteration.
- **Help text strategy (hybrid):**
  - **Hover tooltips** on rows whose labels aren't self-explanatory (label gets a `ToolTip.Tip` attached, or a small `?` glyph next to the label — pick cheaper at impl time).
  - **Persistent subtext** only on rows that require a restart. The "(restart)" suffix currently baked into 5 labels is removed; instead a small secondary-text-colored "(requires restart)" line renders under the label. Cleaner visually and more discoverable.
- **Label renames:** the plan commits to the *intent* of replacing technical labels with user-friendly wording. Specific new strings are decided row-by-row at implementation time, not pinned in the plan. Obvious candidates for rewording include `Topmost`, `Velocity`, `Sustain Mode`, `Accent`, and the stripped `(restart)` suffix. Self-explanatory labels (Volume, Transpose, Preset, the three color names, Keyboard Layout, the two toggles) may stay or be slightly polished as we go.
- **No em-dashes in UI strings** (per existing project convention): use " - " if a rename needs a dash.

### Step 1 — Section headers helper

- New helper in `src/Klavier.UI/Views/Settings/SettingsPanel.Helpers.cs`: `CreateSectionHeader(string title) -> StackPanel` producing `[TextBlock (bold, +2pt), Border (1px divider)]` vertically stacked. Reuses `ThemePaletteProvider.TextPrimary` / `ThemePaletteProvider.Divider`.

### Step 2 — Reorder + insert headers in `SettingsPanel.cs`

Reorder the row creation and the `StackPanel.Children` collection in the constructor to match the five-section grouping above. Intersperse section headers between groups. No row logic changes — existing wiring (`WireSlider`, `WireComboBox`, `WireToggle`, `WireHexColorTextBox`, etc.) stays as-is.

### Step 3 — Restart subtext + "(restart)" suffix removal

- New helper `CreateRowWithRestartSubtext(string label, params Control[] controls) -> StackPanel` that stacks the normal row on top of a small "(requires restart)" `TextBlock` in secondary text color.
- Strip `(restart)` from the five current label constants (`_ThemeLabel`, `_AccentLabel`, `_WhiteKeyLabel`, `_BlackKeyLabel`, `_KeyBorderLabel`) and route those rows through the new helper.

### Step 4 — Tooltip wiring

For each non-self-explanatory row, attach `ToolTip.SetTip(label, "one-sentence explanation")`. Specific wording decided row-by-row at impl time. No new infrastructure — Avalonia's built-in `ToolTip` suffices.

### Step 5 — Rename constants

Rename the `_XxxLabel` const strings whose current wording is technical. Do not change the identifiers of any config keys — renames are UI-string only, so `ConfigKey.Of(...)` calls remain untouched and existing `usersettings.json` files stay valid.

### Verification

1. Panel opens with five visually distinct sections, each with a bold header and a thin divider below.
2. Rows land in the same sections as specified above.
3. Restart-required rows show a small "(requires restart)" line under their label instead of a suffix in the label itself.
4. Hovering a renamed / non-self-explanatory row shows a tooltip explaining the setting.
5. All existing settings continue to read / write to `usersettings.json` with no key changes. Reset Defaults still works.
6. No regressions in: soundfont picker, preset combo, keybinds editor invocation, hex color textboxes, theme toggle, all sliders and toggles.

### Key files to modify
- `src/Klavier.UI/Views/Settings/SettingsPanel.cs` — reorder rows, insert section headers, route restart rows through the new helper, apply renames
- `src/Klavier.UI/Views/Settings/SettingsPanel.Helpers.cs` — new `CreateSectionHeader` and `CreateRowWithRestartSubtext` helpers; tooltip helper if we choose a `?` glyph pattern

---

## Iteration 14: MIDI File Playback + Falling-Notes Visualization

**Goal:** The user loads a `.mid` file via a toolbar button or drag-and-drop. A new panel above the existing piano shows the upcoming notes as falling rounded rectangles (height proportional to duration, single accent color, columns aligned with the piano keys). Bars pass through the piano-key area and fade away as the note's duration elapses. An in-panel player bar exposes Play / Pause / Stop + a tempo slider (0.25x - 2x) and a thin non-interactive progress line. Audio is user-toggleable: either the file plays through FluidSynth (keys animate as they do for user input) or it stays silent (pure visual score). Out-of-range notes (the piano is 61 keys until Iteration 17) show in narrow "ghost" strips at the panel edges. Existing app behavior (Panic, sustain, transpose, volume, settings) continues to work, and Panic also stops active playback.

### Design decisions (locked in)

| Topic | Decision |
|---|---|
| Note source | `.mid` / `.midi` files only (no live input overlay in this iteration). |
| MIDI library | **Melanchall.DryWetMidi** (MIT, mature, standard). Central package version added once; reused by iteration 15 and any later MIDI-Input work. |
| Interaction | Passive visualization. A player-bar toggle chooses between **silent** (bars fall, no file audio — only the user's own keys make sound) and **passive-play** (bars fall AND FluidSynth plays the file; user can optionally play along, not required). |
| Layout | Falling-notes surface docked **above the existing piano, same window**. Toggleable via a new toolbar button. |
| Bar style | Rounded rectangles, height = note duration, single color = current theme `Accent`. No text labels on bars. |
| Hit behavior | Bar keeps rendering as it crosses into the piano area, drawn over the key (semi-transparent), fading until the note's duration fully elapses. |
| Playback controls | Play / Pause / Stop + tempo slider (0.25x - 2.0x). **No** seek bar, **no** A/B loop in this iteration. |
| Progress indicator | Thin non-interactive line under the player bar, fills left-to-right as playback advances. |
| Load UX | (a) Toolbar button opens Avalonia file picker filtered to `*.mid`/`*.midi`; (b) drag-and-drop a `.mid` file anywhere on `MainWindow`. No recent-files list, no default-folder setting. |
| On-load state | Player parses the file; the first few seconds of notes are pre-positioned above the piano but not falling yet. User must click **Play** to start. Fall direction is top -> piano. |
| End-of-song | Stop and reset to start. Pressing Play replays from the beginning. No looping in this iteration. |
| Lookahead | Configurable via a new `Player:LookaheadSeconds` setting (1-10, default 3). The panel always shows that many seconds of upcoming score regardless of panel height; fall speed scales accordingly. |
| Out-of-range notes | Two narrow "ghost" strips at the far left / far right of the falling-notes panel (above where the piano would extend, but within the panel's current width). All out-of-range notes stack into those strips. Audio still plays them through FluidSynth (which is not range-limited). Becomes mostly moot after Iteration 17 but is removable then without drama. |
| Existing settings interaction | `Transpose` shifts the file's pitches (bars + audio alike). `VolumeInPercent` scales file audio via the existing FluidSynth gain path. File velocities are used as-is (`Piano:Velocity` still only applies to user-pressed keys). File CC64 events are honored through `FluidSynthAudioOutput.OnSustainChanged`; user sustain (spacebar) layers on top. |
| Panic during playback | Panic stops ALL active notes (file + user) AND stops MIDI playback. UI returns to the pre-Play state. |
| Input during playback | User keyboard / mouse input is unaffected. Notes played by the user layer on top of the file's audio (passive-play mode) or over silence (silent mode). |
| No em-dash in UI strings | Button / label text uses " - " not " — ". Plan prose may use em-dashes freely. |

### Step 1 — Packages + new project

- `Directory.Packages.props`: add `<PackageVersion Include="Melanchall.DryWetMidi" Version="<latest stable>" />`. Version pin decided at implementation time against the .NET 10 SDK.
- New project **`src/Klavier.Midi/Klavier.Midi.csproj`**:
  - TargetFramework matches the rest (`net10.0`).
  - References `Klavier.Core` and `Klavier.Config` only (no Avalonia).
  - PackageReferences: `Melanchall.DryWetMidi`, `Microsoft.Extensions.Logging.Abstractions`, `Microsoft.Extensions.Options`.
- `Klavier.slnx`: register `src/Klavier.Midi/Klavier.Midi.csproj` under `/src/`.
- `src/Klavier.UI/Klavier.UI.csproj`: add `<ProjectReference Include="..\Klavier.Midi\Klavier.Midi.csproj" />`.
- `src/Klavier/Klavier.csproj`: already transitively references via `Klavier.UI`, but add an explicit reference for DI registration.

**Deferred to a future iteration (MIDI Input):** `Klavier.Midi` already being in the solution makes the later MIDI Input work a drop-in.

### Step 2 — Domain + loader (`Klavier.Midi`)

All types here are pure data / pure service code — no UI dependencies.

| File | Purpose |
|---|---|
| `src/Klavier.Midi/Domain/MidiNote.cs` | `readonly record struct MidiNote(NotePitch Pitch, TimeSpan Start, TimeSpan Duration, NoteVelocity Velocity)`. Start is the note's absolute time from the start of the score. |
| `src/Klavier.Midi/Domain/MidiSustainEvent.cs` | `readonly record struct MidiSustainEvent(TimeSpan At, bool IsOn)`. Parsed from CC64 events. |
| `src/Klavier.Midi/Domain/MidiScore.cs` | `readonly record class MidiScore(string FilePath, string? DisplayName, TimeSpan TotalDuration, IReadOnlyList<MidiNote> Notes, IReadOnlyList<MidiSustainEvent> SustainEvents)`. DisplayName pulled from the SMF metadata if present, falling back to the filename. |
| `src/Klavier.Midi/Ports/IMidiScoreLoader.cs` | `Task<MidiScore> LoadAsync(string filePath, CancellationToken ct = default)`. |
| `src/Klavier.Midi/DryWetMidi/DryWetMidiScoreLoader.cs` | Implementation. Uses `MidiFile.Read(...)`, iterates `GetNotes()` flattening across tracks (track separation is not used this iteration — single accent color), converts DryWetMidi's metric time (via `TempoMap`) to `TimeSpan`, extracts CC64 events, and returns a `MidiScore`. Throws `InvalidDataException` on malformed files (logged by the caller). |

### Step 3 — Player (`Klavier.Midi`)

| File | Purpose |
|---|---|
| `src/Klavier.Midi/Ports/IMidiPlayer.cs` | The runtime-facing port. See below. |
| `src/Klavier.Midi/MidiPlayer.cs` | Implementation. |
| `src/Klavier.Midi/MidiPlayerState.cs` | Enum: `Idle`, `Loaded`, `Playing`, `Paused`. |

`IMidiPlayer` surface (draft):

```csharp
MidiPlayerState State { get; }
MidiScore? CurrentScore { get; }
TimeSpan Position { get; }      // current playhead, scaled by tempo
double TempoMultiplier { get; set; }  // 0.25 .. 2.0
bool AudioEnabled { get; set; }       // passive-play (true) vs silent (false)

void Load(MidiScore score);
void Play();
void Pause();
void Stop();

event Action<MidiScore>? Loaded;
event Action? Started;
event Action? Paused;
event Action? Stopped;      // user-initiated
event Action? Finished;     // reached end of score -> auto-stops & resets to start
event Action<TimeSpan>? Tick;   // ~60 Hz, for visualization re-draw
event Action<NoteOnEvent>? NoteOn;   // fired when a file note begins
event Action<NoteOffEvent>? NoteOff; // fired when a file note ends
event Action<bool>? SustainChanged;  // fired for file CC64 events
```

Implementation notes:

- One `System.Threading.Timer` (or `DispatcherTimer` on UI thread) ticking at ~16 ms. Each tick advances `Position` by `elapsed * TempoMultiplier`, fires `Tick`, and walks a sorted note-start / note-end index forward, emitting `NoteOn` / `NoteOff` / `SustainChanged` for every event the playhead crossed since the last tick.
- `Stop()` re-emits `NoteOff` for every currently-sounding note and a final `SustainChanged(false)` before resetting `Position = 0`. Same for `Finished`, with the additional state transition to `Loaded`.
- `TempoMultiplier` changes take effect on the next tick (no mid-note retiming beyond that).
- No tempo-map-aware scheduling inside the player — the tempo map is already baked into each `MidiNote.Start/Duration` by the loader. `TempoMultiplier` is a *playback* rate multiplier on top.
- `AudioEnabled` is a **pure routing flag inside the player**: when `false`, the player still emits `NoteOn` / `NoteOff` / `SustainChanged` **but the audio-side subscriber is not wired** (see Step 5). This way the visualization always receives events regardless.

### Step 4 — Config

| File | Purpose |
|---|---|
| `src/Klavier.Config/PlayerConfig.cs` | `public const string SectionName = "Player";` + `int LookaheadSeconds { get; init; } = 3;` + `double TempoMultiplier { get; init; } = 1.0;` + `bool AudioEnabled { get; init; } = true;`. |
| `src/Klavier/appsettings.json` | Add a `Player` section with the three defaults above. |

`TempoMultiplier` and `AudioEnabled` are persisted via `IUserSettingsService.UpdateSetting(...)` the same way settings-panel controls persist today (Iteration 9 colon-path API). `LookaheadSeconds` is exposed in the settings panel (inside the new **Sound & Playback** section from Iteration 13 — it's a playback-timing knob); `TempoMultiplier` + `AudioEnabled` are exposed in the player bar and are persisted on change so a restart remembers them.

### Step 5 — Wiring (`Klavier.UI` + `Klavier`)

| File | Purpose |
|---|---|
| `src/Klavier.Midi/ServiceCollectionExtensions.cs` | `AddKlavierMidi(IServiceCollection, IConfiguration)`: binds `PlayerConfig`, registers `IMidiScoreLoader` and `IMidiPlayer` as singletons. |
| `src/Klavier/Program.cs` | Call `AddKlavierMidi(...)` alongside existing `AddFluidSynthAudio` / `AddKlavierUI`. |
| `src/Klavier.UI/ServiceCollectionExtensions.cs` | Register the new `PlayerView`, `PlayerBarView`, `FallingNotesView`, and their view-model (if any). |
| `src/Klavier.UI/Services/MidiPlaybackCoordinator.cs` | New small service, UI-layer. Subscribes to `IMidiPlayer.NoteOn/NoteOff/SustainChanged`. When `player.AudioEnabled` is `true`, forwards those events to `IPianoEngine.NoteOn/NoteOff/SustainOn/SustainOff`. When `false`, does nothing (visualization still draws because the `FallingNotesView` reads `MidiScore` + `Tick` directly, not `NoteOn`). Also subscribes to `IPianoEngine.Panic` via a new `Panic` event on `IPianoEngine` (or wires through `ToolbarView`'s panic button pointer handler) to call `_player.Stop()`. |

`IPianoEngine.Panic` today calls `AllNotesOff` + `SustainOff`. It does not currently expose an event, so the coordinator subscribes instead to the **toolbar's** panic button pointer-pressed handler. Alternative: add an `event Action? PanicRaised` to `IPianoEngine`. Either is fine; choose the cheaper one at impl time — leaning toward the event, since it keeps the UI concern (player stop) out of `ToolbarView`.

### Step 6 — Player view (`Klavier.UI/Views/Player/`)

All new files.

| File | Purpose |
|---|---|
| `PlayerView.cs` | Composite `DockPanel`. Docks `PlayerBarView` at top, a 2 px progress `Border` below it, and hosts `FallingNotesView` as fill. The whole control has `IsVisible = false` until a score is loaded or the toolbar Player toggle is on. |
| `PlayerBarView.cs` | Horizontal `DockPanel`: left = filename label (updates on `IMidiPlayer.Loaded`), middle = `mm:ss / mm:ss` label (updates on `Tick`), right = Play/Pause/Stop buttons + tempo slider (0.25 - 2.0) + an "Audio" toggle button (speaker / muted speaker, reuses `KlavierButton` pattern). Play/Pause/Stop buttons use `KlavierButton`, same styling as the existing Panic/Settings buttons. |
| `FallingNotesView.cs` | Custom `Control` with `OnRender(DrawingContext context)`. Reads `IMidiPlayer.Position` + `CurrentScore.Notes` on every `Tick`, computes which notes overlap `[Position - fadeWindow, Position + LookaheadSeconds]`, and draws each as a rounded rectangle. Invalidates itself on every `Tick` via `InvalidateVisual()`. |
| `FallingNotesGeometry.cs` | Pure static helpers: converts `(NotePitch, panelWidth)` -> column x/width using **the same formula `PianoView` uses** (`whiteKeyWidth = panelWidth / whiteKeyCount`, black-key overlay at boundaries). Extracted as a static helper so `PianoView` and `FallingNotesView` share one source of truth on key geometry (see Step 6b). |

**Step 6a — Column geometry math.**

`PianoView.ArrangeOverride` (`src/Klavier.UI/Views/Piano/PianoView.cs:71`) currently inlines: `whiteKeyWidth = finalSize.Width / whiteKeyCount`, black key sits at `x = (i + 1) * whiteKeyWidth - blackKeyWidth / 2` where the preceding white key has a sharp. `FallingNotesView` must produce the same x coordinates for the same `NotePitch`, so this logic should be extracted. Proposed target: a `PianoKeyGeometry` static class in `src/Klavier.UI/Views/Piano/` exposing:

```csharp
public static double GetColumnCenterX(NotePitch pitch, double panelWidth, IReadOnlyList<NotePitch> whiteKeyPitches);
public static double GetColumnWidth(NotePitch pitch, double panelWidth, int whiteKeyCount);
```

Both `PianoView` and `FallingNotesView` call into it. The list of white-key pitches already lives in `PianoKeysBuilder` (`src/Klavier.UI/ViewModels/PianoKeysBuilder.cs`), so `FallingNotesView` can get the same sequence and compute columns identically.

**Step 6b — Falling bar rendering.**

Per-tick, for each in-window note:

1. `y_top = ((note.Start - Position) / LookaheadSeconds) * panelHeight` (clamped to `[0, panelHeight]`).
2. `y_bottom = ((note.Start + note.Duration - Position) / LookaheadSeconds) * panelHeight` (clamped).
3. Width = `GetColumnWidth(note.Pitch, panelWidth, whiteKeyCount)`; center x = `GetColumnCenterX(...)`.
4. Color = `ThemePaletteProvider.Accent` (follows theme).
5. Opacity: fully opaque while the bar is wholly above the piano line (`y_bottom <= panelHeight`). Once `y_bottom >= panelHeight`, the bar's bottom is clamped at `panelHeight` and an opacity gradient (1.0 at the top, 0.0 at the bottom) is applied — produces the "passes through the piano key and fades" effect the user chose. When the full note duration has elapsed (`Position > note.Start + note.Duration`), the bar disappears.
6. Corner radius ~6 px. `DrawRectangle` with a rounded geometry.

Ghost-column rendering: any note whose `NotePitch` is outside `[C2, C7]` draws as a fixed 8 px-wide bar pinned to the left edge (if `pitch < C2`) or right edge (if `pitch > C7`) of `FallingNotesView`. Multiple out-of-range notes at the same time just stack in the same strip — visual clutter is acceptable because this is the "there's stuff outside the piano's range" indicator. Iteration 17 removes the need entirely.

### Step 7 — MainWindow integration

Change in `src/Klavier.UI/Views/MainWindow.cs`:

- Constructor: accept `PlayerView playerView` and `IMidiPlayer player` and `IMidiScoreLoader loader` (via DI).
- `topSection` DockPanel children become `[toolbarView (Dock=Bottom), separator (Dock=Bottom), playerView (Dock=Top), pianoView (fill)]`. Avalonia's DockPanel stacks top-docked items above the fill child, so this puts the player above the piano.
- `playerView.IsVisible = false` by default. Made visible by the toolbar Player toggle (Step 8) AND automatically whenever a `MidiScore` is loaded.
- Add `DragDrop.AllowDrop = true;` on the window + `AddHandler(DragDrop.DropEvent, OnDrop);`. `OnDrop` reads `e.Data.GetFiles()`, takes the first `.mid` / `.midi` file, calls `await _loader.LoadAsync(path)` + `_player.Load(score)` and then raises the Player toggle if it's off.
- The existing `PointerPressedEvent` tunnel handler (MainWindow.cs:84) stays untouched — clicks outside TextBoxes still blur focus.

### Step 8 — Toolbar additions

Change in `src/Klavier.UI/Views/ToolbarView.cs` — follows the same pattern as the existing Panic / Settings buttons:

- New `KlavierButton loadMidiButton = new("Load MIDI");` (or a file-folder `PathIconButton` — either is fine; leaning toward a text button for consistency with Panic/Settings in this iteration). PointerPressed: opens `TopLevel.GetTopLevel(this).StorageProvider.OpenFilePickerAsync` with filter `*.mid;*.midi`, calls loader + player. Default start folder: last used, persisted via `IUserSettingsService`.
- New `KlavierButton playerToggleButton = new("Player", momentaryActiveOnPress: false);` — mirrors the Settings button's behavior, raises `PlayerToggled?.Invoke(bool)` for `MainWindow` to show/hide `PlayerView`.
- Panic button (existing, line 24): **after** `pianoEngine.Panic()`, also call `midiPlayer.Stop()`. Requires adding `IMidiPlayer` to the `ToolbarView` constructor. If that feels intrusive, alternative: wire via `IPianoEngine.PanicRaised` event inside `MidiPlaybackCoordinator` (Step 5) — cleaner. Pick one at impl time; leaning event-based.

### Step 9 — Settings panel additions

Change in `src/Klavier.UI/Views/Settings/SettingsPanel.cs` + `SettingsPanel.Helpers.cs`:

- New "Lookahead (seconds)" row, slider 1-10 with a live numeric label. Wired to `ConfigKey.Of(PlayerConfig.SectionName, nameof(PlayerConfig.LookaheadSeconds))`. Placed in the **Sound & Playback** section (from Iteration 13).
- `TempoMultiplier` and `AudioEnabled` are deliberately **not** in the settings panel — they live in the player bar where they're most useful, but still persist via `UpdateSetting` so a restart remembers them.

### Step 10 — PianoKeyViewModel unchanged

The user's own keypresses continue to flow through `KeyboardInputHandler` -> `IPianoEngine.NoteOn` -> handlers (audio + piano-view highlight). File notes when `AudioEnabled = true` flow through `MidiPlaybackCoordinator` -> `IPianoEngine.NoteOn` -> handlers. Both paths share the engine, so piano-key highlight always works correctly in passive-play mode. When `AudioEnabled = false`, file notes bypass the engine, so no key highlight fires — only the falling bar is visible as it passes through. This was the intent of the "silent = dynamic pretty score" mode.

### Verification

1. Build: `dotnet build` succeeds with no errors / no new warnings.
2. Launch, click **Load MIDI**, select a known `.mid` (e.g. a public-domain Bach invention). Player bar appears; first few seconds of notes are positioned above the piano; filename and total duration are shown.
3. Click **Play**. Bars start falling at a rate that puts every note at the piano exactly when it sounds. In passive-play mode (default), FluidSynth plays the file; piano keys animate; bars cross into the piano area and fade.
4. Toggle audio off. Restart playback. Bars fall; piano stays silent unless the user plays keys themselves; piano keys only highlight for user input, not file notes.
5. Drag a `.mid` file onto the window. File loads; Player view opens automatically; same playback behavior.
6. Slide tempo to 0.5x mid-playback. Fall slows to half speed; file audio plays at half speed; everything stays in sync.
7. Click **Pause**, then **Play**. Resumes at the paused position with all previously-sounding notes correctly held. Click **Stop**: playhead resets to 0, bars re-pre-position to the first few seconds.
8. Let a short file play to the end. Playback auto-stops, bars clear, UI returns to the pre-Play state.
9. Set `Player:LookaheadSeconds = 6` via the settings panel (in the Sound & Playback section). Reload a file: twice as much upcoming score is visible at once.
10. Change `Piano:Transpose` to +2. Bars shift up 2 semitones visually; file audio plays 2 semitones higher. Change `Audio:VolumeInPercent` to 30: file audio gets quieter. Both take effect without restart.
11. Press the sustain key (spacebar) during playback. User sustain layers with the file's CC64 events — both contribute to notes being held.
12. Click **Panic** during playback. All sounding notes cut immediately; playback stops; UI returns to pre-Play state. Pressing **Play** replays from the start.
13. Load a file containing notes outside C2 - C7 (e.g. a classical piano piece). Out-of-range bars appear in the narrow ghost strips at the left / right edges; audio still plays them correctly through FluidSynth.
14. Regression: user keyboard input, sustain, panic, toolbar, settings panel, soundfont picker, keybinds editor, all existing features continue to work when no file is loaded and when a file is playing.

### Key files to create

- `src/Klavier.Midi/Klavier.Midi.csproj`
- `src/Klavier.Midi/Domain/{MidiNote,MidiSustainEvent,MidiScore}.cs`
- `src/Klavier.Midi/Ports/{IMidiScoreLoader,IMidiPlayer}.cs`
- `src/Klavier.Midi/DryWetMidi/DryWetMidiScoreLoader.cs`
- `src/Klavier.Midi/{MidiPlayer,MidiPlayerState,ServiceCollectionExtensions}.cs`
- `src/Klavier.Config/PlayerConfig.cs`
- `src/Klavier.UI/Services/MidiPlaybackCoordinator.cs`
- `src/Klavier.UI/Views/Piano/PianoKeyGeometry.cs` (extracted from `PianoView`)
- `src/Klavier.UI/Views/Player/{PlayerView,PlayerBarView,FallingNotesView,FallingNotesGeometry}.cs`

### Key files to modify

- `Directory.Packages.props` — add `Melanchall.DryWetMidi`
- `Klavier.slnx` — register `Klavier.Midi`
- `src/Klavier/appsettings.json` — add `Player` section
- `src/Klavier/Program.cs` — call `AddKlavierMidi`
- `src/Klavier/Klavier.csproj` — reference `Klavier.Midi`
- `src/Klavier.UI/Klavier.UI.csproj` — reference `Klavier.Midi`
- `src/Klavier.UI/ServiceCollectionExtensions.cs` — register new views
- `src/Klavier.UI/Views/MainWindow.cs` — dock `PlayerView`, wire drag-drop
- `src/Klavier.UI/Views/ToolbarView.cs` — Load MIDI + Player toggle buttons
- `src/Klavier.UI/Views/Piano/PianoView.cs` — delegate geometry to `PianoKeyGeometry`
- `src/Klavier.UI/Views/Settings/SettingsPanel.cs` + `SettingsPanel.Helpers.cs` — lookahead row
- `src/Klavier.Core/Engine/IPianoEngine.cs` + `PianoEngine.cs` — (optional) `event Action? PanicRaised`

---

## Iteration 15: MIDI Recording (Output)

**Goal:** Let the user record everything they play into a `.mid` file, from pressing Record to pressing Stop.

- **Mechanism:** New `MidiRecorderOutput` in `Klavier.Midi/` implements `INoteEventHandler` — it captures `OnNoteOn` / `OnNoteOff` / `OnSustainChanged` with timestamps starting at Record-press, builds a DryWetMidi `MidiFile` (Type 0, single track), and writes to a user-selected path on Stop.
- **UI:** Two new toolbar buttons (or one toggle): **Record** (red dot when active, ticking elapsed-time label next to it) and **Stop Recording** (which opens a file-save dialog). Defaults save folder persisted via `IUserSettingsService`.
- **Scope:** Captures user-played notes only. Whether to also record file playback (Iteration 14) is the user's choice — cleanest default is "record everything that reaches `PianoEngine`", so file playback in passive-play mode is captured, silent-mode file playback is not (matches the audio-on-toggle semantics).

### Key files to create / modify
- `src/Klavier.Midi/MidiRecorderOutput.cs` — new
- `src/Klavier.Midi/ServiceCollectionExtensions.cs` — register recorder
- `src/Klavier.UI/Views/ToolbarView.cs` — Record / Stop Recording buttons + elapsed-time label
- `src/Klavier.UI/Services/MidiRecorderCoordinator.cs` — open/close file, drive the recorder lifecycle
- `src/Klavier/Klavier.csproj` — no change (already references `Klavier.Midi` after Iteration 14)

---

## Iteration 16: Sustain Half-Pedal

**Goal:** Upgrade sustain from binary on/off to continuous 0-127, exposing partial-pedal nuance that FluidSynth already supports.

- **Mechanism:** Change `INoteEventHandler.OnSustainChanged(bool isOn)` to `OnSustainChanged(byte value)` (0-127). `FluidSynthAudioOutput` already calls `_synth.CC(_MidiChannel, _SustainController, value)` — just passes the new byte straight through instead of mapping bool to 0/127.
- **UI:** Replace the binary sustain bar visual (`SustainBarControl`) with a slider / vertical bar that maps to 0-127. Keyboard sustain (spacebar) still sets 127 on press / 0 on release by default, preserving the current feel.
- **Optional:** a "Sustain Intensity" setting (0-127) that caps the value sent when the user presses the sustain key, letting them dial in partial-pedal behavior. Or a touch-sensitive sustain bar where drag depth = value.
- **Scope:** Keep the existing three sustain *modes* (`Hold`, `InvertedHold`, `Toggle`) intact; they now toggle between 0 and a configurable "max value" instead of 0/127.

### Key files to modify
- `src/Klavier.Core/Ports/INoteEventHandler.cs` — signature change
- `src/Klavier.Core/Engine/PianoEngine.cs` — `SustainOn` / `SustainOff` / `ToggleSustain` now operate on / notify a value
- `src/Klavier.Audio/FluidSynthAudioOutput.cs` — pass value directly to CC64
- `src/Klavier.UI/Views/Piano/SustainBarControl.cs` — visual upgrade
- `src/Klavier.UI/Input/KeyboardInputHandler.cs` — value-aware sustain key handling
- `src/Klavier.Midi/MidiPlayer.cs` + `MidiRecorderOutput.cs` — emit / capture values not bools
- `src/Klavier.Config/PianoConfig.cs` — (optional) `SustainMaxValue`

---

## Iteration 17: 88-Key Piano

**Goal:** Expand the piano from 61 keys (C2 - C7) to 88 keys (A0 - C8). Matches a real acoustic piano and removes the need for the ghost-column fallback introduced in Iteration 14.

- **Mechanism:** `PianoKeysBuilder` already enumerates a `NotePitch` range — widen the range and everything downstream (PianoView layout, FallingNotesView columns via `PianoKeyGeometry`) scales automatically.
- **Keyboard mappings:** The current QWERTY / AZERTY / Dvorak-FR layouts only cover 61 piano keys; we need to decide whether to expand them to 88 (many piano keys won't have sensible PC-key bindings) or leave PC-key bindings as-is and rely on the user to use mouse / MIDI input for the extra range. Leaning toward **leave existing mappings alone** and just widen the piano: unmapped piano keys are still clickable / MIDI-controllable, and the keyboard editor (Iteration 12) can bind them if the user wants.
- **Ghost columns cleanup:** `FallingNotesView` ghost-column branch becomes dead code — remove (or keep as a fallback for future instruments with even wider ranges like Bösendorfer Imperial's 97 keys).
- **Window minimum width:** 88 keys at the current key width requires a wider window. Either shrink per-key width or bump `_MinWidth` on `MainWindow`.

### Key files to modify
- `src/Klavier.UI/ViewModels/PianoKeysBuilder.cs` — range widens
- `src/Klavier.UI/Views/Piano/PianoView.cs` — nothing structural, but double-check edge-case arrangement at larger counts
- `src/Klavier.UI/Views/Player/FallingNotesView.cs` — remove ghost-column branch
- `src/Klavier.UI/Input/Mapping/*.json` built-in layouts — optional, if we decide to extend mappings
- `src/Klavier.UI/Views/MainWindow.cs` — possibly bump `_MinWidth`

---

## Iteration 18+ (Backlog)

One-liners. No detailed design yet.

- **MIDI Input** — Read note-on/off from an external MIDI keyboard via DryWetMidi; surface device selection in the settings panel; preserve device velocity on its way through `PianoEngine`.
- **SharpHook (Global Keyboard Capture)** — New `Klavier.GlobalInput` project using SharpHook to capture PC keyboard input even when Klavier isn't the focused window.

---

## Verification (global)

Each iteration should be verified by:

1. **Build:** `dotnet build` passes with no errors / no new warnings.
2. **Run:** Launch the app and exercise the new feature end-to-end using the per-iteration verification steps above.
3. **Regression:** Every feature shipped up to and including iteration 12 (plan 02) still works identically — piano rendering, keyboard input (3 layouts + custom layouts from the editor), sustain (keyboard + UI bar + 3 modes), panic, toolbar, settings panel, persistence, dark/light theme, SoundFont path picker + preset picker, key-color customization.
4. **Config hot-reload:** Changed settings take effect without app restart wherever it was already the case in plan 02 (piano config, audio config). Color and theme changes remain restart-required.
