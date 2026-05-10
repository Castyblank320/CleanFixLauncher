# Universal Mod Manager (UMM)

> **⚠️ Disclaimer: This project was written entirely by AI (GitHub Copilot / ChatGPT).**  
> It is **not** a professional or serious tool. It was created for personal use and for a small group of friends who wanted a quick way to toggle **online-fixes** on Steam games without messing with the original installation.

## Why this exists

We buy games on Steam, but sometimes we need to apply a “fix” (a crack, a mod, or an online‑fix) to play with friends.  
No existing mod manager allowed us to:

- Keep the original game folder untouched.
- Switch between “vanilla” and “fixed” version with one click.
- Add any game, any fix, any folder structure.

So this tool does exactly that: it uses **symbolic links** (symlinks) to overlay the fix folder on top of the game folder.  
When you launch, it temporarily moves original files to `%TEMP%`, creates links to the fix files, and restores everything when the game exits (or thanks to the crash recovery button).

## Screenshot

![Main window](screenshot.png)

## Requirements

- Windows (tested on 10/11)
- **Run as Administrator** (needed to create symbolic links)

## How to use

1. **Add a game** – fill name and game folder (where the `.exe` is). Optionally select the executable.
2. **Add a fix** – name and folder (must have the **exact same relative structure** as the game folder).
3. **Select a fix** from the list and click `▶ Apply Virtual Layer & Launch`.
4. The game starts with the fix active. When you close the game, original files are restored automatically.
5. If your PC crashes while the fix was active, use `♻ Restore Orphaned Backups` to recover the original files.

All data is saved in `%AppData%\UniversalModManager\config.json`.

## Limitations

- Requires administrator rights (symlinks need it).
- The fix folder must **mirror the exact folder structure** of the game (same relative paths).
- While the fix is active, original files are physically moved to a temporary folder, not copied – this is fast but necessary because Windows cannot overwrite a file with a symlink.

## No warranty

This tool deletes symlinks and moves files. It worked for us, but use at your own risk.  
Always keep a backup of important game files if you are worried.

## License

Do whatever you want. It's just code.

---

*Made for fun, by a human + AI, for a very specific use case.*
