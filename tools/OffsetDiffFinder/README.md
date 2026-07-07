# MySEQ Offset Finder

Compares a known-good old `eqgame.exe` and `myseqserver.ini` against a newer
`eqgame.exe`, then writes a candidate offset ini and confidence report.

## Usage

```powershell
offset-finder.exe `
  --old-exe "D:\EQ\old\eqgame.exe" `
  --new-exe "D:\EQ\new\eqgame.exe" `
  --old-ini "D:\MySEQ-old\myseqserver.ini" `
  --out "D:\MySEQ-new\myseqserver.candidates.ini" `
  --report "D:\MySEQ-new\offset-finder-report.txt"
```

Use the candidate ini as a starting point. Offsets with confidence below `80`
should be checked with the debugger before replacing your live `myseqserver.ini`.

The GUI front-end is published as `offset-finder-gui.exe` and expects this
console tool to be in the same folder.

## How It Works

- Reads old offset values from `[Memory Offsets]` and `* Offsets` sections.
- Finds code/data references to each old value in the old executable.
- Builds context signatures around those references, with the old offset bytes
  wildcarded.
- Finds the same context in the new executable and extracts the new value.
- Reconstructs full `0x140...` primary addresses when the executable stores only
  the low 32 bits.
- For primary memory globals that are not directly referenced in code, translates
  the old address by matching the old/new PE section layout and preserving the
  same offset within the section.
- Returns a warning exit code when any offset is missing or below confidence `80`.
