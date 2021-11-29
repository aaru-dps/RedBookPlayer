# RedBookPlayer

[Audio CD](https://en.wikipedia.org/wiki/Compact_Disc_Digital_Audio) player for [Aaru format](https://github.com/aaru-dps/Aaru).

* This project is fully sponsored by the [Game Preservation Society](https://www.gamepres.org/en/).

[OpenAL](https://www.openal.org/) is required to run this application. Please install it using the most recent instructions for your operating system of choice.

## Default Player Controls

| Key | Action |
| --- | ------ |
| **F1**  | Open Settings Window |
| **F2** | Load New Image |
| **S** | Save Track(s) |
| **Space** | Toggle Play / Pause |
| **Esc** | Stop Playback |
| **~** | Eject |
| **Page Up** | Next Disc |
| **Page Down** | Previous Disc |
| **&#8594;** | Next Track |
| **&#8592;** | Previous Track |
| **R** | Shuffle Tracks |
| **]** | Next Index |
| **[** | Previous Index |
| **.** | Fast Forward |
| **,** | Rewind |
| **Numpad +** | Volume Up |
| **Numpad -** | Volume Down |
| **M** | Mute |
| **E** | Toggle Emphasis |

For Save Track(s):
- Holding no modifying keys will prompt to save the current track
- Holding **Shift** will prompt to save all tracks (including hidden)

For Disc Switching:
- If you change the number of discs in the internal changer, you must restart the program for it to take effect

For Shuffling:
- Shuffling only works on the current set of playable tracks
- If you are in single disc mode and switch discs, it will not automatically shuffle the new tracks

For both Volume Up and Volume Down:
- Holding **Ctrl** will move in increments of 2
- Holding **Shift** will move in increments of 5
- Holding both will move in increments of 10