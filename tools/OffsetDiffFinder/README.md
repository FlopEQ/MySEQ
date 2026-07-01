# MySEQ Offset Diff Finder

Compares a known-good old `eqgame.exe` and `myseqserver.ini` against a newer
`eqgame.exe`, then writes a candidate offset ini and confidence report.

## Usage

```powershell
offset-diff-finder.exe `
  --old-exe "D:\EQ\old\eqgame.exe" `
  --new-exe "D:\EQ\new\eqgame.exe" `
  --old-ini "D:\MySEQ-old\myseqserver.ini" `
  --out "D:\MySEQ-new\myseqserver.candidates.ini" `
  --report "D:\MySEQ-new\offset-diff-report.txt"
```

Use the candidate ini as a starting point. Offsets with confidence below `80`
should be checked with the debugger before replacing your live `myseqserver.ini`.

The GUI front-end is published as `offset-diff-finder-gui.exe` and expects this
console tool to be in the same folder.

## How It Works

- Reads old offset values from `[Memory Offsets]` and `* Offsets` sections.
- Finds code/data references to each old value in the old executable.
- Builds context signatures around those references, with the old offset bytes
  wildcarded.
- Finds the same context in the new executable and extracts the new value.
- Reconstructs full `0x140...` primary addresses when the executable stores only
  the low 32 bits.
