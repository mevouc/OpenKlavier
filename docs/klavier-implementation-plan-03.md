# Klavier — Iteration 13+ Plan

## Context

Plans 01 and 02 took Klavier from a 2-line console POC to a playable desktop piano with hexagonal architecture, FluidSynth audio, keyboard input (3 layouts + custom-layout editor), settings persistence, a themable settings panel, a SoundFont picker with preset selection, and configurable key colors. The app today is fully playable and configurable, but all its output is driven by the user pressing keys, and the settings panel has accumulated 17 flat rows with several technical labels (e.g. "Topmost") that are obvious to developers but opaque to end users.

This plan tackles two axes:

- **UX polish first**: the settings panel gets section headers and friendlier wording so every later iteration that adds a row has a clear home for it.
- **Passive content second**: the app should be able to load a MIDI file and *show it* as falling note bars above the piano (Synthesia / "Piano from Above" style), with optional audio playback through FluidSynth. Once MIDI file playback is in, four smaller follow-ups finish the feature set promised in plan 02's backlog: reading from an external MIDI keyboard, recording the user's playing back to a .mid file, upgrading the sustain pedal from binary to continuous, and expanding the piano from 61 to 88 keys.

The order is:

1. **Iteration 13 — Settings Panel Refinement** (UX polish, high-level)
2. **Iteration 14 — MIDI File Playback + Falling-Notes Visualization** (the big one, detailed below)
3. **Iteration 15 — MIDI Input** (high-level)
4. **Iteration 16 — MIDI Recording** (high-level)
5. **Iteration 17 — Sustain Half-Pedal** (high-level)
6. **Iteration 18 — 88-Key Piano** (high-level)
7. **Backlog one-liners**: SharpHook

Settings-panel polish goes first because iterations 14-18 will each want to add at least one new row (lookahead, audio-enabled persistence, MIDI input device selector, recording defaults, sustain-max-value, possibly a piano-range selector), and those rows land much better once the panel has clear sections to slot them into. MIDI file playback is the natural entry point for the content axis because `.mid` is the de-facto interchange format for piano visualization (Synthesia, Piano from Above, Rousseau-style YouTube tutorials all consume MIDI files), and once `Melanchall.DryWetMidi` is in the solution the MIDI input and recording iterations both come cheaply on top of the same library.

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
| MIDI library | **Melanchall.DryWetMidi** (MIT, mature, standard). Central package version added once; reused by iterations 15 (MIDI input) and 16 (MIDI recording). |
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

**Deferred to Iteration 15 (MIDI Input):** `Klavier.Midi` already being in the solution makes the MIDI input work a drop-in.

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

Ghost-column rendering: any note whose `NotePitch` is outside `[C2, C7]` draws as a fixed 8 px-wide bar pinned to the left edge (if `pitch < C2`) or right edge (if `pitch > C7`) of `FallingNotesView`. Multiple out-of-range notes at the same time just stack in the same strip — visual clutter is acceptable because this is the "there's stuff outside the piano's range" indicator. Iteration 18 removes the need entirely.

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

## Iteration 15: MIDI Input

**Goal:** Let the user play Klavier through a connected MIDI keyboard / controller. Incoming Note On / Note Off / CC64 (sustain) events from one selected device flow through `IPianoEngine` and share the audio + visualization fan-out already used by PC-keyboard and file-playback sources. A dedicated audio-mute toggle lets users with a digital piano that has its own speakers silence Klavier's FluidSynth output for MIDI-device events while still highlighting the piano view and feeding the future recorder.

### Design decisions (locked in)

| Topic | Decision |
|---|---|
| MIDI library | Reuse `Melanchall.DryWetMidi` already in `Klavier.Midi` from Iteration 14. Input device support lives in the `Melanchall.DryWetMidi.Multimedia` sub-namespace which uses WinMM on Windows out of the box. Linux/macOS need an extra native dependency — accepted as a follow-up if/when Klavier is built for those platforms. Windows-first for v1. |
| Project placement | All new code lives in `Klavier.Midi` (no new project). New `Klavier.Midi/Input/` subfolder mirrors the existing `Loading/`, `Parsing/`, `Playback/` layout. |
| Audio-mute routing | **Source-tagged + audio-side filter.** `InputSource` enum gains a `MidiInput` value; `NoteOnEvent` carries a `Source` property; `INoteEventHandler.OnSustainChanged` takes a `source` parameter. `FluidSynthAudioOutput` reads `MidiInputConfig.AudioEnabled` via `IOptionsMonitor` and skips events whose source is `MidiInput` when muted. Engine fan-out stays unchanged — handlers that don't care about source (PianoViewModel) keep working as-is. Chosen over coordinator-bypass routing because handlers are not individually addressable from `Klavier.Midi` and bypassing the engine would fork the existing fan-out model. |
| Hot-plug | `MidiInputCoordinator` runs a background `Timer` (~2 s interval) calling DryWetMidi's `InputDevice.GetAll()`, raises `DevicesChanged` when the set changes, and falls back to "(none)" if the open device disappears or `Open` throws. Disconnect is **surfaced** via a brief inline notification next to the device combobox (see Step 7) so the user notices the cable / driver issue. Chosen over refresh-on-demand: matches the plan's default and avoids surfacing a refresh button. Replugged devices are **not** auto-reopened — selection becomes "(none)" and the user must reselect. |
| Audio-mute scope | App-wide single `MidiInputConfig.AudioEnabled` bool. No per-device storage. |
| Thread model | DryWetMidi fires `EventReceived` on a non-UI thread. `Klavier.Midi` stays UI-free: the coordinator forwards events directly into `IPianoEngine` on whatever thread they arrive. Existing handlers already marshal to the UI thread themselves (`PianoViewModel` uses `Dispatcher.UIThread.Post`, `FluidSynthAudioOutput` is thread-agnostic). No `Avalonia.Threading` dependency creeps into `Klavier.Midi`. |
| MIDI scope | Note On / Note Off / CC64 (sustain) **plus CC120 (All Sound Off) and CC123 (All Notes Off)** — the latter two are treated identically: `engine.AllNotesOff(InputSource.MidiInput) + engine.SustainOff(InputSource.MidiInput)`. Real-world hardware often sends CC123 on reset, mode-change, or power-up; honoring both avoids surprising silent drops. All 16 MIDI channels accepted (no channel filtering). No pitch bend, modulation wheel, aftertouch, program change, or MIDI Thru. |
| Velocity | Device's per-note velocity is preserved end-to-end. `Piano:Velocity` continues to apply only when no explicit velocity is provided (i.e. only PC-keyboard input). |
| Transpose | The engine applies `Piano:Transpose` to every source. MIDI-device pitches are transposed identically to PC-keyboard input. |
| Sustain when muted | When `MidiInput:AudioEnabled = false`, the **coordinator does not forward sustain events into `IPianoEngine` at all** (notes still flow through normally so the piano view highlights). The engine's aggregate sustain therefore never includes the muted device's sustain — no hung-note edge case can occur. **Two v1 limitations** flow from this: (a) the recorder (Iter 16) will NOT capture sustain from the device while muted; (b) "mute-toggle ghost-off" — if the user mutes while physically holding the pedal, then unmutes without first releasing, the next pedal-up does nothing because the engine never knew the pedal was down. Workaround: after toggling Play MIDI input back on, release and repress the pedal to re-engage. Both limitations documented in the in-app tooltip if space allows. Note events stay routed through the engine even when muted so the piano view + future recorder still see them; only sustain has the asymmetry. |
| Duplicate device names | Accepted limitation. If two same-model controllers are connected, the dropdown shows two entries with identical text and the coordinator opens whichever DryWetMidi's `InputDevice.GetAll()` enumerates first matching the persisted name. Behavior with duplicates is documented as undefined for v1 — rare enough not to engineer for. |
| Open failures | When `TryOpen` throws (device locked by another app, driver error, device disappears between poll and open), the coordinator logs the failure, leaves `CurrentOpenDevice` null, and raises a `DeviceOpenFailed(string deviceName, string reason)` event. The settings UI displays a brief warning-colored notification in the same inline TextBlock slot used for disconnects (`"Cannot open {name}"`), auto-cleared after ~4 s. **No auto-retry** — user must reselect to try again. Keeps the failure surface visible without an infinite-loop risk. |
| Held-state drain on transitions | Because the device can vanish mid-press or the mute toggle can intercept sustain events upstream of the engine, the coordinator must proactively drain the engine of any held MidiInput state at well-defined transitions. Rules: (a) **on disconnect** AND (b) **when the user selects "(none)"** — drain BOTH notes (`engine.AllNotesOff(InputSource.MidiInput)`) AND sustain (`engine.SustainOff(InputSource.MidiInput)`), because no further events will come from the device. (c) **When `AudioEnabled` toggles true→false** — drain SUSTAIN ONLY (`engine.SustainOff(InputSource.MidiInput)`), not notes: NoteOff events still flow through the coordinator unconditionally (mute only filters at the audio side), so held notes will release naturally when the user releases them. (d) **`AudioEnabled` false→true** — no drain needed; the engine's MidiInput sustain state was never tracked during the muted period. |
| Engine concurrency | Iter 15 adds a third writer thread to `PianoEngine`'s active-notes dicts (PC keyboard on UI thread, MidiPlayer on Timer threadpool, DryWetMidi on its callback thread). The pre-existing two-writer race was latent; three writers makes it likelier to hit dict corruption under chord-stress. Iter 15 adds a single `Lock` field in `PianoEngine` and wraps the mutating methods (`NoteOn`, `NoteOff`, `AllNotesOff`, `SustainOn`, `SustainOff`, `ToggleSustain`, `Panic`, `OnPianoConfigChanged`). Performance cost is negligible — piano events are sparse. |
| DI disposal on exit (partial fix via C1) | The `using IHost host` + `host.StopAsync()` added by C1 incidentally disposes the `IServiceProvider`, which disposes all `IDisposable` singletons. Our new `MidiInputCoordinator` and `DryWetMidiInputDevice` implement `IDisposable` and benefit from this. Pre-existing singletons that DON'T implement `IDisposable` (e.g. `PianoEngine`, `MidiPlaybackCoordinator`) still leak — a broader audit is owed but deferred from this iteration. |
| Testing approach | Manual end-to-end testing with real MIDI hardware. No unit tests for the coordinator and no virtual-port harness in v1. Rationale: this iteration is a thin UI/coordination layer over a native lib (DryWetMidi Multimedia / WinMM) — most failure modes only surface against real hardware, and unit tests of the coordinator alone wouldn't catch them. The Step 10 verification checklist is the contract. |
| No em-dash in UI strings | Tooltip / label text uses " - " not " — ". |

### Step 0 — POC: validate the DryWetMidi pipeline end-to-end

**Why this step exists.** Before investing in the proper port/adapter split (Step 4), coordinator (Step 5), config-driven device selection (Steps 2 + 7), audio mute (Steps 3 + 8), hot-plug, drain, and UI, validate that DryWetMidi can actually open a connected device on this machine, receive Note On / Note Off / CC64 events with acceptable latency, and route them into the engine. Risk-mitigation slice — gives an early "go / no-go" on the native lib.

**Scope.** Hardcoded single-device path: at app startup, auto-pick the first device returned by `InputDevice.GetAll()`, open it, route events. No config, no UI, no error recovery beyond "log and bail if no device found". Every artifact created in Step 0 is deleted by the end of Step 6 — see the **POC removal manifest** below.

**POC removal manifest.** Every line of POC-specific code introduced in Step 0 is removed by the time iteration 15 is complete. Tracked precisely so we can verify zero leftovers:

| Artifact (introduced in Step 0) | Removed in |
|---|---|
| `src/Klavier.Midi/Input/MidiInputPoc.cs` (the whole file) | **Step 4** (replaced by `IMidiInputDevice` + `DryWetMidiInputDevice`) |
| `services.AddSingleton<MidiInputPoc>();` in `Klavier.Midi/ServiceCollectionExtensions.cs` | **Step 4** (replaced by the new port + adapter registration) |
| `InitializeMidiInputPoc` extension method in `Klavier/Extensions/HostExtensions.cs` | **Step 6** (the coordinator is started via `IHostedService` instead, so no eager-resolve helper needed) |
| `.InitializeMidiInputPoc()` call in `Klavier/Program.cs`'s init chain | **Step 6** (same reason) |

The minimal Core slice that Step 0 adds to `PianoEngine.cs` (third dict, third sustain field, switch arms, `IsSustainOn` aggregate, `PanicAllNotesOff` guard extension) is NOT removed — it is **kept and built upon** by Step 1 (which refactors it further: dict type change, `Enum.GetValues<InputSource>()` iteration, concurrency lock). The `MidiInput` enum value added to `InputSource.cs` is likewise kept. Step 6's verification confirms only the POC-specific artifacts are gone, not the Core extensions.

**`src/Klavier.Core/Primitives/InputSource.cs`** — Add `MidiInput` as the third enum value. (Step 1 builds further on this; for now we just need the enum value to exist.)

**`src/Klavier.Core/Engine/PianoEngine.cs`** — Minimum needed to make `MidiInput` a usable source so the POC doesn't crash on the first event. Step 1 will refactor / extend these:

- Add a third active-notes field: `private readonly Dictionary<NotePitch, int> _midiInputActiveNotes = [];` (same type as the existing two dicts — Step 1 changes the type later).
- Add a `MidiInput => _midiInputActiveNotes` arm to the `GetActiveNotes(source)` switch.
- Add `private bool _midiInputSustainOn;` and extend the `SustainOn` / `SustainOff` switches with `case InputSource.MidiInput: _midiInputSustainOn = true/false; break;`.
- Extend `IsSustainOn` to: `IsSustainOn => _userSustainOn || _playerSustainOn || _midiInputSustainOn;`.
- Extend the `PanicAllNotesOff` early-return guard and the clear loop to include `_midiInputActiveNotes`. (Step 1 will replace these hardcoded lists with iteration over `Enum.GetValues<InputSource>()` — for now, the manual addition is fine.)

**`src/Klavier.Midi/Input/MidiInputPoc.cs`** (new, **temporary** — removed in Step 4). Singleton with the minimum needed to wire DryWetMidi to the engine.

- Constructor: `(IPianoEngine engine, ILogger<MidiInputPoc> logger)`.
- On construction:
    1. Call `Melanchall.DryWetMidi.Multimedia.InputDevice.GetAll()`.
    2. If empty, log a warning and bail (no exception — the app still runs normally without MIDI).
    3. Pick the first device, instantiate via `InputDevice.GetByName(...)`, subscribe to `EventReceived`, call `StartEventsListening()`.
    4. Log: `"POC opened MIDI input device {Name}"`.
- `EventReceived` handler pattern-matches on `Melanchall.DryWetMidi.Core.*`:
    - `NoteOnEvent` → `_engine.NoteOn(new NotePitch(noteNumber), new NoteVelocity(velocity), InputSource.MidiInput)`.
    - `NoteOffEvent` → `_engine.NoteOff(new NotePitch(noteNumber), InputSource.MidiInput)`.
    - `ControlChangeEvent` with `ControlNumber == 64` → `_engine.SustainOn(InputSource.MidiInput)` when `value >= 64`, otherwise `_engine.SustainOff(InputSource.MidiInput)`.
    - All other events: ignored.
- Implements `IDisposable`: `StopEventsListening`, unsubscribe, dispose the device.

**`src/Klavier.Midi/ServiceCollectionExtensions.cs`** — Register the POC as a singleton: `services.AddSingleton<MidiInputPoc>();`. (Removed in Step 4 when the class goes away.)

**`src/Klavier/Extensions/HostExtensions.cs`** — Add a new extension method mirroring `InitializeMidiPlaybackCoordinator`:

```csharp
public static IHost InitializeMidiInputPoc(this IHost host)
{
    host.Services.GetRequiredService<MidiInputPoc>();
    return host;
}
```

**`src/Klavier/Program.cs`** — Add `.InitializeMidiInputPoc()` to the existing chain after `InitializeMidiPlaybackCoordinator()`.

**Verification.**

1. Build: `dotnet build` succeeds.
2. Launch with no MIDI device connected. Console logs `"No MIDI input devices found"` (or equivalent). App runs normally; PC keyboard still works.
3. Plug a device in, then restart. Console logs `"POC opened MIDI input device {Name}"` at startup. Press a key on the device — piano view highlights, FluidSynth plays the note at the device's velocity. Release — piano de-highlights, note decays.
4. Press the device's sustain pedal — held notes ring through. Release — notes decay.
5. PC keyboard input still works. Spacebar sustain still works. Panic still works.

**What this step deliberately does NOT do** (and which later steps cover):
- Hot-plug detection — Step 5.
- Config-driven device selection / persistence across launches — Steps 2 + 7.
- Audio mute toggle — Steps 3 + 8.
- Open-failure / disconnect notifications — Steps 5 + 7.
- Held-state drain — Step 5.
- Settings UI (combobox, status dot, notifications) — Steps 7 + 8.
- Concurrency lock in `PianoEngine`, dict type change, full source propagation on `NoteOnEvent` / `OnSustainChanged` — Step 1.

**Go / no-go signal.** If Step 0 fails (DryWetMidi can't open the device, latency is unacceptable, EventReceived doesn't fire), iter 15's whole architecture is in question and we revisit the library choice before continuing.

### Step 1 — Core: extend events with `InputSource`

Step 0 already added the `MidiInput` enum value and the minimal per-source fields in `PianoEngine` so the POC routes correctly. Step 1 completes the source-propagation story, refactors the panic guard, fixes the `AllNotesOff` stuck-highlight bug, and adds the concurrency lock. Build stays green, no behavior change beyond what Step 0 already enables.

**`src/Klavier.Core/Primitives/InputSource.cs`** — Add `MidiInput` as the third enum value.

**`src/Klavier.Core/Events/NoteOnEvent.cs`** — Add `InputSource Source` as the last positional property of the record struct.

**`src/Klavier.Core/Ports/INoteEventHandler.cs`** — Change `OnSustainChanged(bool isOn)` to `OnSustainChanged(bool isOn, InputSource source)`.

**`src/Klavier.Core/Engine/PianoEngine.cs`** — Four related changes:

1. **Event-shape source propagation.** Step 0 already added the `_midiInputActiveNotes` dict, the `_midiInputSustainOn` field, the `GetActiveNotes(MidiInput)` arm, the sustain switch cases, and the `IsSustainOn` aggregate. Step 1 now propagates the source through the event types themselves: `NoteOn` includes `source` in the constructed `NoteOnEvent` (which gains a `Source` property — see `NoteOnEvent.cs` above), and the existing `NotifyHandlers(handler => handler.OnSustainChanged(...))` calls pass `source` as the new second parameter (the signature was updated in `INoteEventHandler.cs` above).

2. **Active-notes dict type change** (fixes the `AllNotesOff` stuck-highlight bug when `Transpose ≠ 0`). Change `Dictionary<NotePitch, int>` to `Dictionary<NotePitch, (NotePitch KeyPitch, int Count)>` for all three source dicts (including the one Step 0 just added). The dict key stays sounding pitch (so `IsNoteActive` semantics are unchanged); the value carries the original key pitch. `NoteOn` stores `(keyPitch, 1)` and increments on re-press. `NoteOff` reads the count, decrements or removes. `AllNotesOff` iterates `sourceDict` and emits `NoteOffEvent(entry.Value.KeyPitch, entry.Key)`.

3. **`PanicAllNotesOff` refactor.** Replace the manual guard and clear loop (which Step 0 extended to include `_midiInputActiveNotes`) with iteration over `Enum.GetValues<InputSource>()` calling `GetActiveNotes(source)` — both for the guard and the clear loop. The same refactor pattern applies to the sustain aggregation: consider a `_sustainBySource` dict keyed by `InputSource`, with `IsSustainOn => _sustainBySource.Values.Any(v => v)` — decide at impl time. After this refactor, future `InputSource` additions need no changes here.

4. **Concurrency lock** (fixes the pre-existing race between PC keyboard, file playback, and now MIDI input threads). Add a private `Lock _lock = new();` field and wrap all mutating public methods (`NoteOn`, `NoteOff`, `AllNotesOff`, `SustainOn`, `SustainOff`, `ToggleSustain`, `Panic`) and `OnPianoConfigChanged` in `lock (_lock) { ... }`. `RegisterHandler` is called only at startup before any events flow and does not need the lock. Reads of single fields by handlers (post-notify) do not need it either.

**`src/Klavier.UI/ViewModels/PianoViewModel.cs`** — Update `OnSustainChanged` to accept the new `InputSource source` parameter; ignore it (highlight semantics do not depend on source).

**`src/Klavier.Audio/FluidSynthAudioOutput.cs`** — Same signature update; ignore the parameter for now. Step 3 adds the filtering logic.

**`src/Klavier.Midi/Playback/MidiPlaybackCoordinator.cs`** — No change required. It already passes `InputSource.Playback` to `_engine.SustainOn/Off`, and its subscription to `_player.SustainChanged` is at the player API surface where there is no source concept.

**Why `NoteOffEvent` does not gain a `Source`.** The engine fires an aggregate `OnNoteOff` only when *every* source has released the note, so attributing it to a single source would be misleading. The audio side does not need to filter `NoteOff` either — when audio was muted on the corresponding `NoteOn`, FluidSynth never started the note, so the matching `NoteOff` is a no-op.

### Step 2 — Config: `MidiInputConfig`

Pure config plumbing. Build stays green, still no behavior change.

**`src/Klavier.Config/Schema/MidiInputConfig.cs`** (new) — Mirror `PlayerConfig`'s shape:

```csharp
public class MidiInputConfig
{
    public const string SectionName = "MidiInput";

    public string SelectedDevice { get; init; } = "";
    public bool AudioEnabled { get; init; } = true;

    public static class Keys
    {
        public static readonly string SelectedDevice = ConfigKey.Of(SectionName, nameof(MidiInputConfig.SelectedDevice));
        public static readonly string AudioEnabled = ConfigKey.Of(SectionName, nameof(MidiInputConfig.AudioEnabled));
    }
}
```

**`src/Klavier/appsettings.json`** — Add a new top-level section: `"MidiInput": { "SelectedDevice": "", "AudioEnabled": true }`.

**`src/Klavier.Midi/ServiceCollectionExtensions.cs`** — In `AddMidi`, bind the config: `services.Configure<MidiInputConfig>(configuration.GetSection(MidiInputConfig.SectionName));`.

### Step 3 — Audio: filter `InputSource.MidiInput` Note On in FluidSynth when muted

**`src/Klavier.Audio/FluidSynthAudioOutput.cs`** — Inject `IOptionsMonitor<MidiInputConfig> midiInputConfig` via the constructor.

In `OnNoteOn(NoteOnEvent ev)`, early-return when the event comes from a muted MIDI input source:

```csharp
if (ev.Source == InputSource.MidiInput && !midiInputConfig.CurrentValue.AudioEnabled)
{
    return;
}
```

`OnNoteOff` does not filter (see Step 1's "Why `NoteOffEvent` does not gain a `Source`"). `OnSustainChanged` does not filter at the audio side either — sustain mute is enforced *upstream* at the coordinator (see Step 5), so FluidSynth simply never sees a muted MidiInput sustain event. The `source` parameter on `OnSustainChanged` exists for engine fan-out symmetry, not for audio filtering.

Build still green. The mute toggle takes effect silently once Steps 4-6 wire a real device.

### Step 4 — Domain port + DryWetMidi adapter

This step **replaces** Step 0's `MidiInputPoc.cs`: same DryWetMidi event-handling code, now split into a port + adapter and extended with the CC120/CC123 handling that the POC didn't cover. **Two POC artifacts are deleted in this step** — see the manifest in Step 0:

- Delete `src/Klavier.Midi/Input/MidiInputPoc.cs` entirely.
- Remove `services.AddSingleton<MidiInputPoc>();` from `src/Klavier.Midi/ServiceCollectionExtensions.cs` (replaced by `AddSingleton<IMidiInputDevice, DryWetMidiInputDevice>();` below).

The host-extension method and its call site in `Program.cs` are left in place for now — they reference `MidiInputPoc`, so the build won't actually be green at the end of Step 4 alone. Step 6 deletes them. (Acceptable to keep the build red between Step 4 and Step 6 since the coordinator wiring is split across the two; alternatively, comment out the call temporarily in Step 4 and uncomment-then-replace in Step 6 if a green build per-step is preferred.)

**`src/Klavier.Midi/Input/IMidiInputDevice.cs`** (new) — UI-free port for a single MIDI input device:

```csharp
public interface IMidiInputDevice
{
    bool IsOpen { get; }
    string? OpenDeviceName { get; }

    bool TryOpen(string deviceName);
    void Close();

    event Action<NotePitch, NoteVelocity>? NoteOnReceived;
    event Action<NotePitch>? NoteOffReceived;
    event Action<bool>? SustainReceived;
    event Action? AllNotesOffReceived;  // fired for CC120 or CC123
}
```

**`src/Klavier.Midi/Input/DryWetMidiInputDevice.cs`** (new) — Implementation wrapping `Melanchall.DryWetMidi.Multimedia.InputDevice`.

- `TryOpen(deviceName)` enumerates `InputDevice.GetAll()`, finds a matching device, instantiates via `InputDevice.GetByName(...)`, subscribes to `EventReceived`, and calls `StartEventsListening()`. Returns `false` on any exception (device locked by another app, driver error).
- `Close` calls `StopEventsListening`, unsubscribes, and disposes the underlying `InputDevice`.
- The `EventReceived` handler pattern-matches on `Melanchall.DryWetMidi.Core.NoteOnEvent` / `NoteOffEvent` / `ControlChangeEvent`, translates note numbers and velocities to `Klavier.Core.Primitives.NotePitch` / `NoteVelocity`, and raises the appropriate forwarding event.
- Control-change routing: `ControlNumber == 64` → `SustainReceived(value >= 64)`; `ControlNumber == 120` or `ControlNumber == 123` → `AllNotesOffReceived()`; all other CC numbers are ignored.
- Velocity-0 Note On is forwarded as `NoteOnReceived` unchanged — `PianoEngine.NoteOn` already converts velocity-0 to a NoteOff internally.

**Note on the Multimedia package.** DryWetMidi exposes the `Multimedia` namespace from the same `Melanchall.DryWetMidi` NuGet on Windows (uses WinMM, no extra dependency). Linux and macOS would need additional native libraries — out of scope for v1.

No DI wiring yet, so still not user-visible.

### Step 5 — Coordinator (`MidiInputCoordinator`)

**`src/Klavier.Midi/Input/IMidiInputCoordinator.cs`** (new) — UI-facing port:

```csharp
public interface IMidiInputCoordinator
{
    IReadOnlyList<string> GetAvailableDevices();
    string? CurrentOpenDevice { get; }

    event Action? DevicesChanged;
    event Action<string?>? CurrentOpenDeviceChanged;
    event Action<string>? DeviceDisconnected;
    event Action<string, string>? DeviceOpenFailed;  // (deviceName, reason)
}
```

**`src/Klavier.Midi/Input/MidiInputCoordinator.cs`** (new) — Singleton, also implements `IHostedService` (see Step 6). Constructor: `(IMidiInputDevice device, IPianoEngine engine, IOptionsMonitor<MidiInputConfig> config, ILogger<MidiInputCoordinator>)`.

The coordinator owns three pieces of state and one background timer:

- A `Timer` polling `Melanchall.DryWetMidi.Multimedia.InputDevice.GetAll()` every ~2 s, comparing to the previous list and raising `DevicesChanged` on any diff.
- A cached `IReadOnlyList<string>` of the last-known available devices.
- A subscription to `config.OnChange`, reacting to `SelectedDevice` and `AudioEnabled` updates from the UI.

The initial device scan and the auto-open of `config.CurrentValue.SelectedDevice` happen in `StartAsync` (see Step 6 for why).

**Event forwarding into `IPianoEngine`.**

- `NoteOnReceived` → `_engine.NoteOn(pitch, velocity, InputSource.MidiInput)` — **unconditionally**. Mute does not drop notes; FluidSynth filters them downstream (Step 3).
- `NoteOffReceived` → `_engine.NoteOff(pitch, InputSource.MidiInput)` — **unconditionally**.
- `SustainReceived(isOn)` → `_engine.SustainOn/Off(InputSource.MidiInput)` — **only when `config.CurrentValue.AudioEnabled` is true**. When muted, the sustain event is dropped at the coordinator so the engine's aggregate sustain never includes it.
- `AllNotesOffReceived` → `_engine.AllNotesOff(InputSource.MidiInput) + _engine.SustainOff(InputSource.MidiInput)` — **unconditionally**. Mute does not suppress drains (same as our local drain triggers).

**Config-OnChange dedup.** `IOptionsMonitor.OnChange` fires on *any* field change in `MidiInputConfig`. The coordinator caches `_lastSelectedDevice` and `_lastAudioEnabled` separately and handles each independently:

- If `SelectedDevice` changed → close the current device and open the new one (with drain on close, per the rules below).
- If `AudioEnabled` changed → drain sustain on `true → false` only.
- Both can change in the same notification — handle both arms.

Pattern reference: `FluidSynthAudioOutput.OnAudioConfigChanged` does field-by-field comparison the same way (compares `VolumeInPercent`, `SoundFont.Path`, and preset bank/program independently).

**Held-state drain at transitions** (see the design row of the same name).

- On disconnect AND on the user selecting "(none)" → `_engine.AllNotesOff(InputSource.MidiInput)` + `_engine.SustainOff(InputSource.MidiInput)` before closing the adapter.
- On `AudioEnabled` `true → false` → `_engine.SustainOff(InputSource.MidiInput)` only (notes keep flowing through the engine).

**Disconnect handling.** If the open device disappears between polls, the coordinator drains held state (as above), closes the adapter, clears `CurrentOpenDevice`, emits `CurrentOpenDeviceChanged(null)`, and raises `DeviceDisconnected(deviceName)`.

**Open-failure handling.** When `TryOpen` returns false, the coordinator logs the failure, leaves `CurrentOpenDevice` null, and raises `DeviceOpenFailed(deviceName, reason)`. **No auto-retry** — the user must reselect to try again.

**Persistence.** `MidiInputConfig:SelectedDevice` is *not* cleared on either disconnect or open-failure. It stays in `usersettings.json` so a same-named device returning later still appears in the dropdown — but the coordinator does **not** auto-reopen on replug.

**Threading.** Both the polling timer and DryWetMidi's `EventReceived` callback invoke the coordinator from non-UI threads. The coordinator uses an internal `Lock` for state transitions (open / close, device list mutation). Forwarding into `IPianoEngine` happens from the DryWetMidi callback thread — the engine's new `Lock` (Step 1) and the handlers' UI-thread marshalling (PianoViewModel) tolerate this.

### Step 6 — DI registration via `IHostedService`

The coordinator must run its initial scan and auto-open the persisted device at app startup, *before* the user can interact with the UI. Implementing `IHostedService` on the coordinator is the Microsoft-blessed pattern for startup-bound singletons and keeps us out of ad-hoc eager-resolve calls in `Program.cs`.

**`Directory.Packages.props`** — Add a centralized version pin for `Microsoft.Extensions.Hosting.Abstractions` if not already present. (`Microsoft.Extensions.Hosting`, which `Klavier` already references, brings the abstractions transitively — but `Klavier.Midi` should reference `.Abstractions` directly rather than pulling in the full `Hosting` package.)

**`src/Klavier.Midi/Klavier.Midi.csproj`** — Add `<PackageReference Include="Microsoft.Extensions.Hosting.Abstractions" />`.

**`src/Klavier.Midi/Input/MidiInputCoordinator.cs`** — Implement `IHostedService` alongside `IMidiInputCoordinator`.

- `StartAsync(CancellationToken)` — start the polling timer, do the initial `InputDevice.GetAll()` scan, and call `TryOpen` for `config.CurrentValue.SelectedDevice` if non-empty. Log a startup banner: `"MidiInputCoordinator started, {DeviceCount} device(s) available"`.
- `StopAsync(CancellationToken)` — dispose the timer, close the open device.

**`src/Klavier.Midi/ServiceCollectionExtensions.cs`** — Register `IMidiInputDevice` plus a single-instance dual registration for the coordinator (so the same object is both auto-started as an `IHostedService` and resolvable as `IMidiInputCoordinator` from the UI):

```csharp
services.AddSingleton<IMidiInputDevice, DryWetMidiInputDevice>();

services.AddSingleton<MidiInputCoordinator>();
services.AddSingleton<IMidiInputCoordinator>(sp => sp.GetRequiredService<MidiInputCoordinator>());
services.AddHostedService(sp => sp.GetRequiredService<MidiInputCoordinator>());
```

**`src/Klavier/Extensions/HostExtensions.cs`** — **Remove the `InitializeMidiInputPoc` extension method** added in Step 0. The coordinator is now started by the host's `IHostedService` pipeline; no eager-resolve helper needed.

**`src/Klavier/Program.cs`** — Two changes:

1. **Remove the `.InitializeMidiInputPoc()` call** added in Step 0 to the init chain. (After this removal, no code anywhere references `MidiInputPoc` — confirmed by the verification below.)
2. The `IHost` is currently never `.RunAsync`'d — `RunAvaloniaApp` calls `StartWithClassicDesktopLifetime` directly, which means `IHostedService.StartAsync` would never fire without this change. Three sub-modifications:
    1. Wrap `host` in a `using` declaration.
    2. Call `host.StartAsync().GetAwaiter().GetResult()` after the existing `EnsureValidUserSettings()…ApplyColorTheme()` chain, before `RunAvaloniaApp(args)`.
    3. Call `host.StopAsync().GetAwaiter().GetResult()` in a `finally` block after the Avalonia loop returns.

This makes `IHostedService` work, and incidentally disposes DI singletons on app exit (partial fix for the "no-disposal-on-exit" pre-existing issue — see the matching design row). The other existing eager-resolve init methods (`InitializeMidiPlaybackCoordinator`, etc.) continue to work — they run before `StartAsync` and remain the precedent for non-`IHostedService` coordinators.

After this step, plugging in a device and manually editing `usersettings.json` to set `MidiInput:SelectedDevice` would route notes into the engine on next launch. There is still no UI for the user to do this themselves.

**POC removal verification (run at end of Step 6, before moving to Step 7).** Confirm every artifact in Step 0's POC removal manifest is gone:

1. `git grep -n MidiInputPoc` returns **zero hits** anywhere in the repo (no file, no using, no DI registration, no host-extension method, no Program.cs call site).
2. `Get-ChildItem src/Klavier.Midi/Input/MidiInputPoc.cs` reports "file does not exist".
3. `git grep -n InitializeMidiInputPoc` returns **zero hits**.
4. The build is green: `dotnet build` succeeds with no errors (compiler would catch any dangling reference to `MidiInputPoc`).
5. At runtime: launch the app with a MIDI device connected. The startup banner is now `"MidiInputCoordinator started, {N} device(s) available"` (from the hosted service in Step 6), NOT `"POC opened MIDI input device {Name}"` (the Step 0 banner). The POC log line should never appear again.

If any of the above fails, stop and clean up before proceeding to Step 7. The "kept" Core extensions from Step 0 (third dict, sustain field, switch arms in `PianoEngine`, `MidiInput` enum value) are intentional and should remain — only POC-specific artifacts are checked.

### Step 7 — Settings UI: device-selector row

In `src/Klavier.UI/Views/Settings/SettingsView.cs`, add a new `BuildMidiInputDeviceRow()` method placed in the **Sound & Playback** section (after the Preset row, before the section ends). Pattern mirrors the existing keyboard-layout / soundfont rows:

- Inject `IMidiInputCoordinator` and `IOptionsMonitor<MidiInputConfig>` via the constructor.
- Build a `StyledComboBox` whose items are `["(none)", .. coordinator.GetAvailableDevices()]`. Selected item resolves from `MidiInputConfig.CurrentValue.SelectedDevice` — empty string maps to "(none)".
- A small `Ellipse` status dot (8 px) sits next to the combobox: `ThemePaletteProvider.Accent` brush when `coordinator.CurrentOpenDevice is not null`, neutral / muted color otherwise.
- Wire `SelectionChanged` to `_settingsService.UpdateSetting(MidiInputConfig.Keys.SelectedDevice, selectedNameOrEmpty)`. The coordinator's `config.OnChange` subscription does the actual open/close.
- Subscribe to `coordinator.DevicesChanged` via `UIThread.Post(...)` to refresh the combobox items, preserving the current selection.
- Subscribe to `coordinator.CurrentOpenDeviceChanged` via `UIThread.Post(...)` to update the status dot.
- Subscribe to `coordinator.DeviceDisconnected` and `coordinator.DeviceOpenFailed` via `UIThread.Post(...)`. Both feed into the **same inline `TextBlock`** next to the status dot in a warning-colored foreground — disconnect shows `"{deviceName} disconnected"`, open-failure shows `"Cannot open {deviceName}"`. A shared `DispatcherTimer` (single-shot, ~4 s) clears the text and returns the dot to its neutral color. No new toast infrastructure needed — the notification is local to the settings row.
- Tooltip on the row: `"Connected MIDI keyboard or controller (USB or 5-pin MIDI port)"`.

New label / tooltip constants at the top of `SettingsView.cs`:

```csharp
private const string _MidiInputDeviceLabel = "MIDI input device";
private const string _MidiInputDeviceTooltip = "External MIDI keyboard or controller. (none) disables external input.";
```

### Step 8 — Settings UI: audio-mute toggle row

Second row in the same section, immediately under the device-selector row. Pattern mirrors `BuildShowKeyLabelsRow()`:

- `ToggleSwitch` initialized from `MidiInputConfig.CurrentValue.AudioEnabled`.
- `WireToggle(toggle, MidiInputConfig.Keys.AudioEnabled)`.
- `_midiInputConfig.OnChangeOnUIThread(c => toggle.IsChecked = c.AudioEnabled)`.

Label and tooltip:

```csharp
private const string _MidiInputAudioEnabledLabel = "Play MIDI input";
private const string _MidiInputAudioEnabledTooltip = "Off when your MIDI keyboard has its own speakers and you don't want Klavier doubling the sound. Piano view still highlights pressed keys.";
```

### Step 9 — Hosted-service startup checkpoint

Behavior-level checkpoint, not a code step — Step 6 already wires the hosted service. Confirm via the running app that:

1. The coordinator's `StartAsync` runs before the main window appears (log message visible in console: e.g. `"MidiInputCoordinator started, {N} device(s) available"`).
2. If `usersettings.json` already has a `MidiInput:SelectedDevice` set to a currently-connected device, that device is auto-opened during `StartAsync` (log message: `"Opened MIDI input device {Device}"`).
3. If the persisted device is missing, the coordinator stays in "(none)" state and the settings UI shows "(none)" selected with a neutral status dot — no error, no notification (notifications are reserved for live disconnects).
4. App shutdown calls `StopAsync` which closes the open device cleanly (visible in log).

### Step 10 — Verification

1. Build: `dotnet build` succeeds with no errors / no new warnings.
2. Launch with no MIDI device connected. Settings panel → Sound & Playback → "MIDI input device" combobox shows only "(none)"; status dot is neutral; "Play MIDI input" toggle is on (default).
3. Plug a MIDI device in. Within ~2 s the combobox shows the device name as an option. Select it. Status dot turns accent-colored.
4. Press a key on the device. Piano view highlights the corresponding key. FluidSynth plays the note at the device's velocity (not `Piano:Velocity`).
5. Hold a key, release. Piano view de-highlights. FluidSynth releases.
6. Hold the sustain pedal on the device (CC64). UI sustain bar shows sustain on; held notes ring through; release pedal → sustain off; notes decay.
7. Set `Piano:Transpose` to +2 via the settings panel. Press the device's C key — the piano view highlights C, FluidSynth plays D. Same behavior as PC-keyboard input.
8. Toggle "Play MIDI input" off. Press a device key — piano view still highlights, but FluidSynth stays silent. Press a PC key — FluidSynth still plays (PC-keyboard is unaffected). Spacebar sustain still works for PC keys. **Press the device's sustain pedal — the UI sustain bar does NOT light up** (coordinator drops the event upstream of the engine). Toggle back on, press the pedal — sustain bar lights up immediately.
9. **Disconnect.** While the device is selected, unplug it. Within ~2 s the status dot returns to neutral; the combobox selection becomes "(none)"; a brief `"{deviceName} disconnected"` notification appears next to the dot in a warning color and clears after ~4 s; `usersettings.json` still has the previous name persisted.
10. **Open failure.** With the device connected, lock it in another MIDI app (e.g. open the device in a DAW with exclusive access), then in Klavier's settings panel select the device from the dropdown. The status dot stays neutral; a brief `"Cannot open {deviceName}"` notification appears in the same inline slot and clears after ~4 s. `CurrentOpenDevice` remains null; pressing keys on the device does nothing in Klavier. Releasing the lock in the other app + reselecting in Klavier succeeds.
11. **Held-state drain on disconnect.** Press AND hold several keys + the sustain pedal on the device. Without releasing, unplug the device. The piano view de-highlights all keys; the UI sustain bar turns off; FluidSynth releases the notes. No hung audio.
12. **Held-state drain on '(none)' selection.** Press AND hold several keys + the sustain pedal. Without releasing, select '(none)' in the device dropdown. Same observable behavior as #11.
13. **Held-state drain on mute toggle (sustain only).** Press AND hold the sustain pedal on the device. While holding, toggle "Play MIDI input" off. The UI sustain bar turns off immediately. Release the pedal physically (no observable effect). Press a key on the device — it lights up but FluidSynth stays silent. Toggle "Play MIDI input" back on, repress the pedal — sustain re-engages. Verify that toggling mute does NOT close + reopen the device (status dot stays accent-colored throughout).
14. **CC123 / CC120 drain.** If your device has an explicit "Panic" or "All Notes Off" button (many controllers do), press AND hold several keys + the sustain pedal, then press that button. All highlights clear, sustain bar turns off, FluidSynth releases. Same observable behavior as #11. (If the device sends CC123 automatically on power-up, this can also be observed by toggling the device on while Klavier has notes held from PC keyboard — only the MidiInput-sourced state drains, the PC-sourced state stays.)
15. **Replug.** Re-plug the same device. The combobox repopulates the option but selection stays "(none)" (no auto-reopen on hot-plug — only at startup). Select it manually → reopens.
16. **Restart with persisted device.** Restart the app with the device connected. Console logs show the coordinator auto-opening it. Pressing a key works without selecting it in the UI.
17. **Panic.** Click **Panic** while holding a device key. All notes cut; piano view clears; sustain clears. Playing on the device immediately afterward still works.
18. **Layered playback + input.** Load a MIDI file (Iteration 14) and click Play while playing along on the device. Both sources highlight the piano simultaneously; both make audio (assuming both audio toggles are on). Mute "Play MIDI input" — file audio continues, device-only events stay visual.
19. **Regression.** PC-keyboard input, all existing settings, file playback, panic, toolbar, settings panel, soundfont picker, theme switching, keybinds editor — all unaffected.

### Key files to create

- `src/Klavier.Config/Schema/MidiInputConfig.cs`
- `src/Klavier.Midi/Input/IMidiInputDevice.cs`
- `src/Klavier.Midi/Input/DryWetMidiInputDevice.cs`
- `src/Klavier.Midi/Input/IMidiInputCoordinator.cs`
- `src/Klavier.Midi/Input/MidiInputCoordinator.cs`

### Key files to modify

- `src/Klavier.Core/Primitives/InputSource.cs` — add `MidiInput`
- `src/Klavier.Core/Events/NoteOnEvent.cs` — add `Source` property
- `src/Klavier.Core/Ports/INoteEventHandler.cs` — `OnSustainChanged` gains `InputSource source`
- `src/Klavier.Core/Engine/PianoEngine.cs` — source propagation on events, active-notes dict type change (`(KeyPitch, Count)` value), `PanicAllNotesOff` refactor over `Enum.GetValues<InputSource>()`, and a `Lock` around mutating methods. Full details in Step 1.
- `src/Klavier.UI/ViewModels/PianoViewModel.cs` — update `OnSustainChanged` signature
- `src/Klavier.Audio/FluidSynthAudioOutput.cs` — inject `IOptionsMonitor<MidiInputConfig>`, filter `MidiInput` events when muted
- `src/Klavier.Midi/ServiceCollectionExtensions.cs` — register `MidiInputConfig` + new services + hosted-service dual registration
- `src/Klavier.Midi/Klavier.Midi.csproj` — add `Microsoft.Extensions.Hosting.Abstractions` package reference
- `Directory.Packages.props` — add centralized version for `Microsoft.Extensions.Hosting.Abstractions` if not already present
- `src/Klavier/appsettings.json` — add `MidiInput` section
- `src/Klavier/Program.cs` — wrap host in `using`, call `host.StartAsync()` after the init pipeline, `host.StopAsync()` after the Avalonia loop returns (required for `IHostedService.StartAsync` to fire)
- `src/Klavier.UI/Views/Settings/SettingsView.cs` — device-selector row + audio-mute toggle row + disconnect notification in Sound & Playback section
- `src/Klavier.UI/Views/Settings/SettingsView.Helpers.cs` — optionally a small helper for the status-dot `Ellipse`
- `src/Klavier.Midi/Playback/MidiPlaybackCoordinator.cs` — no change expected (it consumes `MidiPlayer` events, not `INoteEventHandler`)

### Deferred opportunities (intentionally out of scope, parked for later)

- **Test sound button next to the device selector.** Fires a brief C4 as if from the device. Diagnostic value once `MidiInput` source is in the engine. Deferred because the status dot + "press a key" already confirms wiring, and a synthetic event would pollute a future recorder (Iter 16).
- **Auto-select first device on first run.** When there's exactly one device and `SelectedDevice` is empty, auto-select. Deferred because cleanly distinguishing "never set" from "user explicitly chose (none)" requires a sentinel value or a "has-been-set" flag — scope creep for the value delivered.
- **Device-count hint in dropdown placeholder** (`(none) - 2 device(s) available`). Low value (one click reveals the list anyway) for low cost. Skip in v1.

---

## Iteration 16: MIDI Recording (Output)

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

## Iteration 17: Sustain Half-Pedal

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

## Iteration 18: 88-Key Piano

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

## Iteration 19+ (Backlog)

One-liners. No detailed design yet.

- **SharpHook (Global Keyboard Capture)** — New `Klavier.GlobalInput` project using SharpHook to capture PC keyboard input even when Klavier isn't the focused window.

---

## Verification (global)

Each iteration should be verified by:

1. **Build:** `dotnet build` passes with no errors / no new warnings.
2. **Run:** Launch the app and exercise the new feature end-to-end using the per-iteration verification steps above.
3. **Regression:** Every feature shipped up to and including iteration 12 (plan 02) still works identically — piano rendering, keyboard input (3 layouts + custom layouts from the editor), sustain (keyboard + UI bar + 3 modes), panic, toolbar, settings panel, persistence, dark/light theme, SoundFont path picker + preset picker, key-color customization.
4. **Config hot-reload:** Changed settings take effect without app restart wherever it was already the case in plan 02 (piano config, audio config). Color and theme changes remain restart-required.
