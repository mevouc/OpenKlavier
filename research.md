# Ideas

## Layouts & mapping

- 61 or 88 keys piano
- Layout to note-ids mapping -> for inputs
    - QWERTY or AZERTY layout (basically, where is M key?), mapped to 61 keys - Shift + key for #/black
    - for additional 88 keys, Ctrl + some keys, (keep Space reserved for Sustain pedal)
    - Physical keys / Scan-code based: IMPORTANT
- Scan-codes to Displayed key mappings
    - swap between physical mapping display (what's written on the keys, digits are diplayed as 1-2-3, not &-é-" for example)
    - get from OS (can display Dvorak-fr values)
        - with default AZERTY, will display special characters for digits

## Components (Hexagonal Architecture / Ports & Adapters)

- Options

- Input Adapters (Publishers)
    - receive user interactions (or other things, file I/O, whatever)
    - outputs a note / an action to the Core
    - list of adapters:
        - keyboard input (AvaloniaUI natively, or SharpHook for headless/background mode)
        - mouse clicks on UI
        - MIDI input, like OpenPiano (use DryWetMidi)

- Core / Domain (Event Bus) / Instrument
    - receives inputs signals from any input adapter
    - entirely decoupled from OS, Audio, and UI libraries
    - outputs note events to subscribed output adapters

- UI, use AvaloniaUI package
    - acts as an input adapter (mouse control, active window keyboard focus)
    - acts as a visual output adapter
    - keyboard with white and black keys
    - display note when pressed
    - labels on keys
    - "Stay on Top" (Topmost) window property option

- Output Adapters (Subscribers)
    - receive notes to play/process from the Core
    - list of adapters:
        - UI visualizer (shows key pressed)
        - Audio output (runs on dedicated high-priority thread)
            - SoundFont, use MeltySynth package
            - pipe synth to audio output, use MiniAudioExNET package (cross-platform alternative to NAudio)
        - MIDI output
            - recording, use DryWetMidi package
            - virtual instrument (via loopMIDI or OS native loopback)