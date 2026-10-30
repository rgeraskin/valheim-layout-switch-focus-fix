# CLAUDE.md

LayoutSwitchFocusFix: a client-side BepInEx plugin (net462) that keeps keyboard focus in the text field being typed in when the player switches the keyboard layout. User docs: README.md.

## The bug and the fix

A layout switch makes the Input System report `InputDeviceChange.ConfigurationChanged` for the keyboard. `ZInput.OnDeviceChanged` (assembly_utils) forwards every Added/Reconnected/ConfigurationChanged as `ZInput.OnInputLayoutChanged` (meant for keyboard ↔ gamepad). Of its listeners only `BuildUi.OnLayoutChanged` touches the selection: `EventSystem.current.SetSelectedGameObject(null)` plus closing the favorites dropdown, so any focused field anywhere loses focus. `src/KeepFocus.cs` is a prefix that skips the deselect (still closes the dropdown) while a `TMP_InputField` is focused and the gamepad isn't active. Same stack on Windows and macOS (verified).

`src/Diagnostics.cs` (config `Debug.Log`): logs keyboard device changes and stack traces of `TMP_InputField.OnDeselect`/`DeactivateInputField` on a focused field. That's how the cause was found; keep it for bug reports.

## Commands

Tools come from mise (`mise.toml`: dotnet 10). `VALHEIM_DIR` is `.deps`, a symlink to `../valheim-dev-harness/.deps`.

- `mise run build`, `mise run build-harness` (companion; needs valheim-dev-harness built next to this repo), `mise run package` (dist/ zip)
- Images: `uv run tools/make_images.py .harness/shots .harness/fonts` (`mise run images`) makes package/icon.png, logo.png and nexus-header.jpg (1300x372, Nexus' size) from one Mac screenshot, w1 (`mac-game.sh`, then the harness's `camshot w1`). No gallery: a still frame can't show focus surviving a layout switch. Fonts (OFL, google/fonts): Cinzel for titles, Forum for Cyrillic.

## CI and releases

Copied from ClientPortals: `.github/workflows/ci.yml` builds against the free dedicated server's assemblies (`tools/ci/fetch-deps.sh`, steamcmd retried) and packages; a tag `v<version>` runs `release.yml`: GitHub release, then Thunderstore, Hexium, Nexus via `tools/release/publish.cs` (settings `tools/release/sites.json`; Nexus mod 4716, file 8272593; the first file went up by hand, in the `BepInEx/plugins/LayoutSwitchFocusFix/` layout publish.cs uses; the Nexus page text is `package/nexus-description.bbcode`, synced on every release). Secrets: THUNDERSTORE_TOKEN, HEXIUM_TOKEN, NEXUS_API_KEY. Run release.yml by hand for a dry run. Push over SSH (https serves the work account's token).

The same fix is offered to Valheim-Community-Patch as `Patches/Correctness/LayoutSwitchFocusPatch.cs` ("Fix Layout Switch Focus"); both can be installed together (each prefix only skips the deselect).

## Testing

- Windows (valheim-dev-harness on a Windows test PC): `scripts/deploy.sh --profile <profile> <this dll> <companion dll>` from the harness repo. The companion's `oskey 29` presses the real ` key through SendInput; set it as the Windows layout hotkey (Grave Accent) and the switch goes through Windows exactly as a player's; `fg grab` first brings the game to the front (SendInput goes to the foreground window). `pinname`, `activate <field>`, `focus`, `closeui` set up and read the scene. Agree on turns with other sessions sharing the test PC.
- macOS: `tools/harness/mac-game.sh` starts the Steam build here with the harness in `.harness/profile`; `tools/harness/mac-cmd.sh` sends commands; `.harness/mac-switch-layout` (built from `tools/harness/mac-switch-layout.swift`) selects the next keyboard input source. Put the input source back afterwards.
- Check both ways: `cfg rgeraskin.LayoutSwitchFocusFix General.Enabled false` must reproduce the loss (`focus` → `selected=none`).

## Conventions

- Commit messages: no Claude/AI attribution footer. Default branch `master`.
- Markdown: no hard line wraps.
