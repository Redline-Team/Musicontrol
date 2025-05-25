# Musicontrol

Musicontrol is a Unity Editor plugin that allows you to control your music players directly from within the Unity Editor. It supports Windows, macOS, and Linux platforms and works with popular music players.

## Features

- Control popular music players without leaving the Unity Editor
- Play, pause, next, and previous track controls
- Volume control
- Support for multiple platforms:
  - **Windows**: Spotify, iTunes/Apple Music, Windows Media Player
  - **macOS**: Apple Music, Spotify
  - **Linux**: Spotify, Rhythmbox, VLC
- Special support for Linux/Wayland environments
  - Manual connection option for when automatic detection fails
  - Launch music players directly from Unity

## Requirements

- Unity 2022.3 or later
- Music players installed on your system

## Installation

### Using Package Manager

1. Add the package to your Unity project through the Package Manager:
   - Open the Package Manager (Window > Package Manager)
   - Click the "+" button
   - Select "Add package from git URL..."
   - Enter the repository URL: `https://github.com/Redline-Team/Musicontrol.git`

   Alternatively, you can add it directly to your `manifest.json`:
   ```json
   "dependencies": {
     "dev.redline-team.musicontrol": "https://github.com/Redline-Team/Musicontrol.git"
   }
   ```

### Using RPM/VCC/ALCOM

You can also install this plugin using RPM, VCC, or ALCOM from the following repository:

```
https://rlist.arch-linux.pro/index.json
```

Add this repository to your package manager and then install the Musicontrol package.

## Usage

1. Open the Musicontrol window by selecting **Redline > Modules > Musicontrol > Music Player Control** from the Unity menu.
2. Select your music player from the dropdown menu.
3. Use the controls to play, pause, skip tracks, and adjust volume.

### Linux/Wayland Users

If you're using Linux with Wayland and experiencing detection issues:

1. Expand the "Manual Connection Options" section.
2. Select your music player type from the dropdown.
3. Click "Launch Player" to start the music player from Unity (recommended for better detection).
4. Click "Connect Manually" to establish a connection with the running player.

## Platform-Specific Notes

### Windows
- The plugin uses Windows API calls and keyboard shortcuts to control music players.
- Make sure your music player is running before using the plugin.

### macOS
- The plugin uses AppleScript to control music players on macOS.
- You may need to grant permission for Unity to control your music players the first time you use it.

### Linux
- The plugin uses D-Bus to communicate with music players on Linux.
- Make sure your music player supports the MPRIS D-Bus interface.
- For Wayland users, the manual connection option provides a more reliable experience.

## Troubleshooting

- If a music player is not showing up in the dropdown, make sure it's running and supported on your platform.
- Try refreshing the available players list by clicking the "Refresh Available Players" button.
- For Linux/Wayland users, try the manual connection option if automatic detection fails.
- If you're still having issues, check the Unity Console for any error messages.

## Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for details on how to contribute to this project.

## License

This project is licensed under the GPL-3.0 License - see the [LICENSE](LICENSE) file for details.

## Credits

Developed by The Redline Team.