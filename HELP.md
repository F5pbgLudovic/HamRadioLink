# HamRadioLink — user guide (English)

*Version française : [AIDE.md](AIDE.md).*

## 1. First start
1. Extract the zip anywhere (no installer) and run `HamRadioLink.exe`.
2. **Poste** menu: pick your radio (the front panel and link defaults change).
3. Top right, **Liaison**: choose the interface, then **CONNECTER** (connect).

No radio? Choose **Poste virtuel (simulateur)** (virtual radio) then connect. It enforces the manuals' formats strictly.

## 2. Connecting
| Interface | When |
|---|---|
| **Port COM** | Normal case (CAT / USB cable). Set port, baud rate (same as the radio's menu), stop bits, RTS / DTR. |
| **CAT sur TCP** | Serial-over-network server / remote COM port. |
| **Poste virtuel** | Try without hardware. |

**By brand**
- **Yaesu**: baud = *CAT RATE* menu (4800 default on FT-2000 / FTdx5000, 38400 on FTdx10); enable *CAT RTS* on FT-2000 / FTdx5000; FTdx10: use the "Enhanced" port of the Silicon Labs CP210x pair.
- **Icom / Xiegu (CI-V)**: set *CI-V baud rate* and *CI-V address* in the radio; change HamRadioLink's **CI-V address** box only if you changed the factory address. The program builds the `FE FE … FD` frames and BCD frequencies itself. Turn *CI-V transceive* off if you see foreign replies.
- **Yaesu FT-817 / 818 / 857 / 897 / 847 / 650 (5-byte binary)**: *CAT RATE* menu, **2 stop bits** (automatic). Basic set only (frequency in 10 Hz steps, mode, PTT, lock, split, CLAR on/off, A/B, S-meter, power, squelch, high SWR). **No CAT manual was used: check on the radio.**
- **Kenwood / Elecraft**: baud per the radio's menu; TS-890 announces transmit state (`AI2;`).

"No answer from the radio": check COM port, baud rate, RTS, cable.

## 3. Normal view
- **Knobs**: drag vertically or mouse wheel; double-click = zero (Shift = fine).
- **Frequency**: click top / bottom of a digit, wheel, or double-click to type.
- **Tuning wheel**: drag in a circle or wheel; step below it.
- **Keyboard**: ← → step, ↑ ↓ 1 kHz, **Esc** stops transmit.
- **CAT monitor**: all traffic, plus a box to send a command by hand (`;` is added).

## 4. "Vue config" (layout editor)
**Vue** menu → *vue config*. Drag to move, bottom-right corner to resize. **Valider** saves, **Retour arrière** undoes (unlimited), **Réinit** restores everything (asks first). Leaving with unsaved changes asks Yes / No / Cancel.
Update: Settings ⚙ > *Mise à jour* (automatic at start or button; one click, safety copy, only the program is replaced).
ROS alert: Settings ⚙ > *Alerte ROS* (threshold, delay, beeps, power drop + test). Status message: right-click to ignore a warning.
A warning is shown when entering the config view (risk of spoiling the skin). **Right-click an element**: the *Nom affiché* field is a **drop-down** of every front-panel and menu command of the radio; picking one fills SET, READ, expected reply and values. An **S-meter** gets the S scale and a larger box if needed (an added element becomes a meter; a built-in non-meter element refuses and points to *Ajouter*).
- **Right-click** an element: see and edit its CAT / CI-V command (send, read, reply regex, choice values, slider range). *Tester la lecture* sends the read; *Essai* tests a pasted reply; *Défaut* restores the original (green dot = modified).
- Send templates: `{v}` value · `{v:3}` 3 digits · `{v:+4}` signed · `{p:RU:4:;}` / `{n:RD:4:;}` text + 4 digits by sign · `{wait:1000}` pause (ms).
- **Ajouter**: create a button, toggle, knob, choice row, meter or LED from the **selected radio's own command list**; right-click an added element to delete it.

## 4 bis. Meters, locks, settings, export / import
- **Meters**: right-click a meter = bar graph ↔ needle (remembered per meter).
- **Reset locks** (⚙ Settings → *Réinitialiser les verrous*): one click, then a confirmation, and **every** lock set by right-click (all radios, whole controls and row keys) is cleared. The Lock key at the top is not affected.
- **Command log** (⚙ Settings → *Journal des commandes*): one file **per day** in the `logs` folder (next to the program, or `%APPDATA%\HamRadioLink\logs`), deleted after 7 days (adjustable). It holds the orders sent to the radio, replies that change, errors (`?;`) and events: connection / disconnection, sharing, com0com pairs (state of each pair, creation, deletion), command-server orders. Identical periodic reads are not written (compact mode; detailed mode optional) and a 25 MB per-day cap prevents huge files. Send this file to understand a problem. **Active keys**: dark navy background (adjustable: "Touche active (fond)").
- **CONNECT / DISCONNECT button**: **green** background while the radio answers, **red** when the connection is absent or lost (not connected, cable unplugged, radio off…). Both colours are set in ⚙ Settings → Colours ("Bouton de connexion").
- **Lock veil level**: ⚙ Settings → *Verrous* → slider 10 to 100 % (67 % default); the veil colour is set in *Couleurs de l'interface*.
- **PTT key**: the whole key is green at rest, red while transmitting. **Right-click PTT = TX safety timeout** (off, 30 s, 1, 2, 3, 5, 10, 15, 20, 25 or 30 minutes; saved per radio): a transmission started by the key is **cut automatically** at the deadline, a "● TX m:ss" countdown replaces the key's name while transmitting and a message warns you. A transmission started elsewhere (radio mic, connected program) is not touched. To lock the PTT key: Ctrl + right-click (or the menu entry).
- **PC microphone to the rig** (right-click PTT, or Settings ⚙ > "Micro vers le poste" for older radios without a PTT key): continuously sends the chosen microphone to the radio's transmit sound card (PC output wired to the radio's data / mic input), through shared WASAPI: other programs keep the microphone and nothing is played back on the PC. Pick the microphone and the sound card, then tick the box. The radio only transmits this sound while transmitting, with its modulation source set to USB / rear. One setting for all radios, saved (`micro.*` in `settings.ini`).
- **TUNE button** (bottom right of every radio front panel, 190 × 62, movable in layout view): one click transmits a 1500 Hz tone on the radio's transmit sound card (the one of "PC microphone to the rig") with PTT by CAT, or the radio's VOX, for 5 to 60 s; the button shows "● TUNE" and the countdown, a second click stops it. Settings in **Settings ⚙ > Bouton TUNE**: level, duration, rig VOX tick box, colour at rest (green) and while tuning (red). If the radio stops answering, the CONNECT button turns red and one click reconnects.
- **Lock key** (right of "Serveur"): blocks the **whole program** except itself — click, wheel, keyboard, open windows (the program connected to the command server keeps control: WSJT-X, SDRLink…) — and turns **red** while active (colour adjustable: Settings → Colours, "Touche Lock verrouillée"). A second click unlocks. The lock is not remembered: the program starts unlocked. Closing the window while locked asks for confirmation. COM / TCP sharing keeps passing data (it does not go through the interface).
- **Several radios at once**: pick another radio in the *Poste* list while the first one is connected (each radio has its own link): the first stays connected and open. Right of *Vue*, in **every view** (normal and config view), one **tick per open radio** (☑ = active, ☐ = other; a click activates that radio; **green background when connected**) — as soon as there is a radio — and, with two radios, two **split icons** (side by side, or one above the other): both front panels show together, each scaled down with the **same proportions** as the whole display. Touching a control of a radio makes it active. A disconnected background radio closes. *Vue config* works on one radio only (the split closes). **Sharing is set radio by radio**: the *Partage* button sets the active radio (e.g. pair COM20 ↔ COM21 for the FT-2000, COM22 ↔ COM23 for the FTdx10); changing one radio's pair does not touch the other. A pair already assigned to another radio is refused (same for a TCP port).
- **Remembered radio buttons**: after a **successful connection** the radio's button stays at the top (even after closing the program); a click reopens it and recalls its link settings (type, COM port, baud rate, stop bits, RTS / DTR, network address, CI-V address), a **right-click removes it**. These settings are saved per radio at connection, and also when leaving the radio or the program.
- **Automatic connection**: **right-click CONNECTER** ("AUTO" appears): that radio connects by itself at start-up with its own link; several radios can have it.
- **Locks**: right-click **any control** (button, knob, choice, band / mode row, frequency display, tuning wheel) = locked: the control gets a **dark red veil** (colour adjustable: Settings → Colours, "Voile d'un organe verrouillé") and a red frame and no longer responds (click, wheel, and the arrow keys for a frequency); right-click again to release. For a list choice, **Ctrl + right-click** opens its list of values. Remembered per radio. Esc (stop transmit) always works.
- **⚙ Settings**: *Interface colours* (12 colours, instant), *Meters* (all needle / all bars), *Mouse wheel* (1–10 % of travel per notch, default 2 %, Shift = finer), *Locks* (unlock all), *Manual command* (raw command; CI-V: hex bytes).
- **⇅ Export / import**: *Export* writes one `.ini` with the program settings, every radio's layout and — if connected — the radio's settings (buttons, knobs, choices). *Import* restores it and restarts; radio settings are re-sent at the next connection (never transmit or power-off).
  **By parts**: export offers check boxes (program and layouts, the radio's settings, the radio's **internal menu** for the Yaesu FT-2000, FTdx5000, FT-450D, FT-950, FT-991A and FTdx3000 — read menu by menu, about a minute); import reads the file, offers the parts it holds, refuses another radio's settings and sends the settings to the connected radio without restarting (it restarts only for the program settings). The internal menu was only tried on the simulated radio.
- **Icom IC-7760**: opens the IC7760Link window (waterfall, spectrum, meters, bands / memories, CW / voice) with its own ⚙ and ⇅; settings in `data\ic7760.ini`.

## 4 ter. Sharing the radio
Hamlib, OmniRig and FLRig are not offered: they only carry a reduced command set. HamRadioLink keeps the links that carry **every** command of the radio. To let another program (OmniRig, a logger, WSJT-X) drive the same radio, use the **Partage** button:
- a **virtual COM port**: install a com0com / VSPE pair, give HamRadioLink one end (e.g. COM20) and the other program the other end (COM21), same baud rate on both sides;
- and / or a **TCP server** (this computer only by default; tick "local network" to open it).
In the list **no pair is hidden**: each shows its state — free, "used by HamRadioLink (radio …)" (held open by the sharing of a connected radio; released when that radio disconnects), or "busy with another program" (ExpertSDR3, a logger…). A pair busy with another program is not deleted: it is only refused as a sharing pair. Tick *Activer le partage*: if com0com is missing, HamRadioLink explains why it needs it and offers to install it itself (signed installer embedded, silent install: Windows only asks for administrator permission, and sometimes to confirm the driver). Under *Port COM virtuel* the list only shows **free pairs** (COMn ↔ COMm, never the radio's port); if there is none, "Créer une paire…" suggests two free numbers you can change; "Supprimer la paire…" removes the selected pair (asks first; only listed, i.e. free, pairs can be removed). The window then tells you which port HamRadioLink uses and **which one to pick in OmniRig or any other program** (same baud rate). OK, then CONNECT (applied at connection). Commands pass unfiltered; each reply goes back to whoever asked (the client does not receive HamRadioLink's own polling). Available for Yaesu / Kenwood / Elecraft (text), Icom / Xiegu (CI-V) and Yaesu 5-byte; not for the old Yaesu block-status models. On the virtual radio, only for text-command radios. A client that turns the radio's AI mode on receives all replies.

## 4 quinquies. Command server ("Serveur" button)
HamRadioLink opens an **IP command server** (port 7809) that **WSJT-X, JTDX, JS8Call, SDRLink** and other ham radio programs know how to use: they then drive the radio selected in HamRadioLink, **whatever its model**: VFO A / B frequency, mode, PTT, split, volume. *Serveur* button → tick *Activer le serveur de commande* → OK; in the other program choose the radio type that connects to an IP server (its exact name depends on the program), address 127.0.0.1, port 7809. This computer only by default (tick "local network" to open it). If another program already uses port 7809, close it or change the port. The **server's radio** is chosen in the window ("Poste") and stays the same even if you change the radio on screen: the client program does not see the radio change. A **locked** control (right-click; for a row, the locked key) does not block the connected program (locks protect the interface only); every connection and disconnection is written to the log (Settings → Journal). **Several radios**: this first server stays on the **first open radio**; in the same window a **second server** (another port, 7810 by default) can drive a **second radio** picked from the list. Both protocol forms are accepted (plain text, and messages with a 16-byte header); only tried against simulated clients (not a real WSJT-X).

## 4 quater. Radio controlled by TCI (ExpertSDR3, Thetis…)
Pick **Poste commandé en TCI** in the *Poste* list: one entry for every SDR program that offers a **TCI** server (ExpertSDR3 / SunSDR, Thetis for ANAN and Apache…). In *Liaison* choose *TCI*, the address (127.0.0.1 on the same PC) and the port (40001 by default; Thetis: the "TCI Server" port), then CONNECT. The panel covers what TCI offers: VFO A and B, mode, PTT, TUNE, S-meter, DRIVE, volume, MUTE, SPLIT, RIT; changes made in the SDR program are followed live. **Flex** has no native TCI: use SmartCAT (Kenwood TS-2000 emulation) through a virtual COM port with the *Kenwood TS-2000* radio. TCI was only tried against a simulated TCI server.

## 4 sexies. The traffic log ("LOG" button — keep an ADIF backup)
- **LOG** (under "Serveur") opens the log: a list of all your QSOs (quick search, Filter, Layout for the columns), tabs for several logs, **Add** (Ctrl+N) or double-click to view / edit a QSO. The current radio's frequency and mode are offered.
- **Base…** : pick the MariaDB / MySQL database (server, port, user, password, schema). "Tester" checks everything: MariaDB installed on the computer?, embedded engine present?, port listening?, connection, database, table, QSO count. With no MariaDB installed, HamRadioLink uses its own embedded MariaDB (inside its folder, no Windows service; File → MariaDB engine… to update it).
- **Importer / partager…** : finds the MariaDB logs of another logging program (copy: the original is only read; or direct sharing of the same database), takes over your station, imports ADIF files (one tab per file). Make an ADIF backup first: the assistant offers it.
- Menus: File (ADIF import / export…), Edit, View, Logbook (awards, statistics, duplicates, ADIF backup), Tools (**check distances**, online services, **send to all online logs**, upload the selection to QRZ.com / Club Log / eQSL / LoTW), **Configuration**, **Themes**, Help.
- **Top row**: the log tab, "+", then Add, Delete, QRZ.com, Awards tracking (colour DXCC table: green = confirmed, orange = worked), Filter (callsign, band, mode, country, dates, **free text**), All and ▲ (back to the top of the list). Right-click a contact: Delete.
- **Add window**: the callsign's details appear 0.05 s after you stop typing (name, QTH, locator, comment of the last contact…); the list below shows the contacts of the callsign and its /P /M /MM /QRP variants (**deep search** box for xx/callsign/xxx); double-click an old contact to open it for editing (**Mise à jour** button); **qrz.com** and **qrzcq** buttons open the callsign's page.
- **Configuration**: **Station** (callsign, first name, locator: the distance of each contact is computed when it is received; Tools > check distances fills in the old ones), **DX Cluster** (saved clusters, automatic connect / reconnect, strip, frames, VFO A / B cursors), Import / share…, ADIF backup, Database…, Ports….
- **DX Cluster**: a strip on the right shows the band's callsigns (Auto = radio's band, or a chosen band) with VFO A (red) and VFO B (blue) cursors; **mouse wheel = zoom**, drag = move, **click a callsign = the radio tunes** (frequency and mode), right-click = filters (digital, CW, phone, FreeDV). Raw frames appear at the bottom with the **Cluster** (active clusters) and **Alerte** (several text lines joined by "or" / "and", a beep sounds on a matching live spot — note: the beep goes through the Windows sound card) buttons.
- **Themes**: 53 themes (from Windows 3.11 to the most modern, sea, nature, retro) applied to every log window; **Themes > Personnaliser les couleurs**: 64-colour palette + RGB, saved for each theme.
- **Closing the remote window**: if the log is open it stays open (radios disconnected, no link with the radio); clicking HamRadioLink again brings the remote back (only one copy of the program at a time).
- **QSOs from WSJT-X arrive automatically**: every logged contact is added to the current log (duplicates ignored). HamRadioLink listens on UDP **2333** (WSJT-X "N1MM Logger+" broadcast: Settings > Reporting, tick the box and set port 2333), **2237** (WSJT-X UDP Server, same page), **7825** (raw ADIF), plus 2 free ports. Only one program can listen on a given port. Settings: **Settings ⚙ > Réglages des ports** (or the log's "Ports…" button), 0 = unused.
- **Online uploads** (Tools): QRZ.com (the log's API key, QRZ subscription), Club Log (your callsign and a personal **token** "cl_…" created on clublog.org > Settings > API; no password or software key any more), eQSL, LoTW (TQSL installed and the "Station Location" name) are set in **Tools > Online services**. **Send to all online logs** uploads the selection (or whatever is not yet marked "sent") in a progress window that does not block the log. Columns "eQSL sent", "LOTW sent", "Club Log sent", "QRZ.com sent". Right-click on contacts: Export the selection (ADIF) or **for LoTW** (.tq8 file signed by TQSL, to upload by hand on the LoTW site), Send the selection to…, Mark sent / not sent. If QRZ.com refuses the upload, an ADI file is created and the QRZ.com import page opens.
- Data: folder `data\journal`; settings `jn.*` in `settings.ini`.

## 5. Your settings
Everything is in the `data` folder next to the exe: `<radio>.layout.ini`, `<radio>.cat.ini`, `<radio>.custom.ini`, `settings.ini`. Delete a file to reset that part. Copy the folder to move to another PC.

## 6. Updating
Replace `HamRadioLink.exe` (keep `data`). `build.ps1` renames the previous exe to `HamRadioLink-<version>.exe`.

## 7. Troubleshooting
- **No answer**: port, baud, cable, RTS; Icom: CI-V address and speed.
- **"?;" in the monitor**: the radio refuses the command (a read refused 3 times is dropped). Fix it by right-click.
- **A button does nothing**: expected for models not checked against a manual; compare the command with your radio's CAT / CI-V manual.
- **Report**: [GitHub issue](https://github.com/F5pbgLudovic/HamRadioLink/issues) with model, link and a monitor excerpt.

## 8. Command line
`--radio=<key>` (e.g. `ftdx10`, `ic7610`, `ts890`) · `--virtual` · `--data=<folder>` · `--selftest=report.txt` · `--dump=file.txt` · `--shot=image.png,ms,actions`.

## 9. Caution
This software sends commands to your radio, **including transmit**. Try the virtual radio first, then the real radio into a **dummy load at low power**. Commands of unchecked models may be wrong.

73 de F5PBG

## 4 septies. What's new in 1.2 (scales, calibration, radio menu)
- **Settings ⚙ > Réglage S-mètre puissance / Réglage autres S-mètres / Réglage Width / CONT / RF PWR**: maximum power, wattmeter calibration (10 W, 20 W… up to the maximum power), SWR / ALC / COMP / Id / Vdd meters, and the Width, CONT and RF PWR knobs (shown in Hz / W). A "full-scale Po" below the maximum power also limits the RF PWR knob.
- **Vue config**: the LIAISON block is hidden; **Aligner** gives the same size to look-alike buttons and knobs of a section (Retour arrière undoes it).
- **MENU button** (Yaesu FT-2000, FTdx5000, FT-450, FT-950, FT-991A, FTdx3000): the radio's menu table (EX command). Use "Télécharger les valeurs du poste" first; click a value to change it (read-back check); right-click to rename a menu or define its possible values. **Warning: every changed value is sent to the radio immediately.**
- **LOG**: round − / + / ↺ text zoom buttons (list, cluster ruler, bottom frames), ↔ column fit button, QRZCQ button, 12 new themes.

## 4 octies. What's new in 1.3 (FT-991A)
- **FT-991A**: **144** and **430 MHz** band buttons; **CLAR** knob (10 Hz steps); **DT GAIN** knob (menu 073, DATA-mode level); **MIC EQ** button (microphone equalizer); **CTCSS / DCS** (mode, CTCSS frequency, DCS code); **C4FM TX (AMS)** (menu 090).
- The rest of C4FM (GM, dedicated screen) has no CAT command: not reproduced. None of this has been tried on a real FT-991A yet: if something differs (e.g. MIC EQ reversed), right-click in config view to fix it.
- The CLAR knob through RC / RU / RD is also restored on other Yaesu radios whose catalog lacks the CF command.

## 4 nonies. What's new in 1.4 (FTdx10, FT-991A DF5DP, TUNE)
- **FTdx10**: **MIC EQ** button; **PROC** gets its level back when switched on again; **FT8/FT4...** button (mode grid, after DATA-FM-N): USB, with PROC and MIC EQ switched off; when leaving, the previous mode, PROC (with its level) and MIC EQ come back. Choosing another mode also leaves FT8/FT4. Commands are spaced 350 ms apart. The same button appears on Yaesu radios whose mode grid has a free cell.
- **"Yaesu FT-991A DF5DP" radio**: a copy of the FT-991A with its own layout (`data\ft991a-df5dp.*`). Right-click on CTCSS ENC / FREQ CTCSS / CODE DCS: list of all values; Ctrl + right-click: lock.
- **9-digit frequencies** (radios above 100 MHz); **long lists** (over 20 values) with scroll bar and mouse wheel.
- **TUNE**: "Change the power during TUNE" box (Settings > TUNE): the set value is sent before transmitting, the previous power is restored afterwards.
- **com0com pair**: if creation fails, the message shows com0com's answer (kept in the log too).
- Nothing new has been tried on a real radio yet.

## 4 decies. What's new in 1.5 (log: languages, undo, copy, themes)
- **Log language**: **Configuration > Langue / Language...** (23 languages, French by default), applied when the log is reopened. Only the log is translated.
- **Cluster**: **5 to 30 min** list next to "Auto" = how long spots stay displayed. **Themes**: 53 (13 modern ones added); the list stays open to compare, click elsewhere or Esc to leave; "Personnaliser" always visible.
- **Right-click on selected QSOs > Change band / frequency**: FT8 → the band's FT8 frequency, other modes → band start.
- **↶ ↷** (Ctrl+Z / Ctrl+Y): undo / redo add, edit, delete (50 operations). **Ctrl+C / X / V** copy, cut, paste rows; **Ctrl+A** select all; **Ctrl+N** add; **Enter** validates the add window.
- **Command server**: radio not connected → "set" commands are refused, the radio keeps its frequency.
- Not covered by automatic tests: copy / paste, batch band change, theme window. Translations not reviewed by native speakers.

## 4 undecies. What's new in 1.6 (built-in layouts)
- **Default layouts inside the exe**: FT-991A DF5DP, FTdx10, FTdx5000 and FT-2000 no longer need files in `data`; the layout is the one built into the exe.
- **FT-991A / DF5DP swapped**: at first start, old `data\ft991a*.layout.ini` and `.cat.ini` files are moved once to `data\ancien-ft991a` (recoverable). Your later key moves are still saved (**Validate**).

## 4 duodecies. What's new in 1.7

Club Log no longer requires a software key: **Tools > Online services > Club Log** = your callsign + a personal **token** "cl_…" (clublog.org > Settings > API > Create token; copy it at once, it is shown only once). Treat it like a password. Club Log deletes a token unused for 6 months.

Online uploads are sent in **sessions of 100 QSOs** (3 s pause between sessions) for every service. In Online services, "Demander un jeton" opens the Club Log page and "Aide" explains the steps; these windows do not block the rest of the program.

**HRDLog.net, QRZCQ.com, HamQTH** (1.7): set up in Online services (HRDLog: callsign + upload code received by e-mail; QRZCQ: callsign + API key; HamQTH: HamQTH username + password), available in Send selection to / Mark, with their "sent" columns.

**World Radio League**: set up in Online services (API key created on WRL: Integrations > Developer API > Generate API key; "Demander une clé" and "Aide" buttons), "WRL sent" column, Send / Mark menus.

**Send to all online logs**: window with the services (checkboxes; unset ones greyed) and a start date (up to today). **Tools > Envoi automatique** (automatic sending): new contacts (typed or received from WSJT-X), selected services, delay: instant / 30 min / 1 h / 2 h / when the log is closed. The last processed contact and the time of the last send are remembered (settings.ini): after an interruption, sending resumes without restarting from the beginning of the log. When enabled, only later contacts are sent.

**Alerts button** (danger sign, after the "?"): lit when there are new messages (link lost, SWR alarm with the SWR value, sound card unavailable...), off otherwise. A click opens the time-stamped list; OK closes, Effacer (clear) empties it. Messages are kept for 7 days, then deleted automatically. Works for every radio.

**Magnifier** (toolbar, after "Refaire"): opens a search window that does not block the log. Pick the field (QSO date, call sign, free text: name, QTH, comment...), type the value, then "Suivant" (next) / "Précédent" (previous) to move down and up the table (Enter = next, Shift+Enter = previous). Date: 22/11/2026, 2026-11-22, 2026-11 or 2026.

**Receiving the radio on the PC** (1.7): right-click the AF volume knob of a radio: "Listen to the radio on the PC" (on/off), "sound card of the radio" (per radio), and "mute the radio speaker (sound stays on the PC)", which sets the radio AF volume to 0 over CAT while listening is on (restored afterwards and when the program exits). Ctrl + right-click: lock the knob. Log magnifier: "Début" button (top of the table); a date that does not exist jumps to the nearest date.

When "mute the radio speaker" is on, the **AF volume knob sets the volume of the sound played on the PC** (the radio stays at 0; its original volume is restored when turned off and when the program exits).

## 4 terdecies. What's new in 1.8

**FTdx10 MENU key** (between METER and TUNE): a window that follows the radio's own menu. Tabs QUICK MENU (home settings), RADIO / CW / OPERATION / DISPLAY SETTING and their sub-pages; each line shows the value read from the radio and changes it; changes made on the radio show up (re-read every 5 s). A red banner reports anything the radio refuses. The window takes the full screen height.

**Right-click on FTdx10 keys**: MIC EQ = microphone equalizer (live curve; the radio refuses gains below -10 dB over CAT, a known firmware limit: set them on the radio), NB = level / width / rejection, DNR = level, VOX = mic or data source, MOX = safety timeout like PTT, NOTCH = wide / narrow width (shown on the key), STEP = previous value. Ctrl + right-click locks the key. Left-click on the VFO B mode: mode list. 70 key: start of the 70 MHz band. CLAR RX and CLAR TX are two independent settings (the program sends the one you turn, with only its clarifier on); CLEAR CLAR resets both to 0; turning the wheel fast = 100 Hz steps.

**Big needle S-meter** (FTdx10, FT-2000; other radios: "S-mètre" button of the config view): right-click a left-hand meter = replace the bars with the S-meter (and back); left-click = next function (PO, SWR, ALC, COMP, ID, VDD, the radio's METER command). The needle follows the signal on receive and the selected measurement on transmit. In the config view you see either the S-meter or the bars.

**At startup** no command is sent to the radio until all its parameters have been read; the last session's positions are shown at once. **FT8**: the previous state (PROC, PROC level, MIC EQ, mode) is saved on disk and restored when leaving FT8, even after the program was stopped.

**Gear > SWR alert** (at the bottom): SWR calibration for every radio (several measurements or "Passer", extrapolation); WIDTH is calibrated every 100 Hz.

**Log**: Add window (RST proposed by mode, its own theme); reorganized File menu with "Sauvegarder la base maintenant…" (dated .sql file, folder and number of copies of your choice, automatic backup at close / daily / weekly / monthly) and "Restaurer la base maintenant…" (backup folder or another path, with a safety backup first); destination log for received QSOs (Ports settings); Online services: "Ne pas transmettre les commentaires" check box.

## 4 quaterdecies. What's new in 1.9

**10-band audio equalizer on the PC**: one window (right-click PTT > "10-band equalizer + compressor / limiter (transmit)", or right-click the AF volume knob > "10-band equalizer (receive)") with 10 sliders from 50 Hz to 10 kHz (±12 dB, up = gain), a live response curve, presets and "À plat" (flat); right-click a slider to reset it.

- **Transmit**: equalizer then **compressor / limiter** (threshold, ratio, output gain, ceiling, attack, release; gain reduction is shown). It acts on the microphone sent to the radio, so "PC microphone to the radio" must be ticked.
- **Receive**: acts on the radio's sound played on the PC ("Listen to the radio on the PC" ticked).
- Nothing changes for a microphone plugged straight into the radio or for the radio's own speaker. Settings are saved and shared by all radios.

## 4 quindecies. What's new in 1.10

**Transmit noise gate**: in the audio equalizer window (Transmit tab), a "Noise gate" panel below the compressor cuts the noise between words (threshold, attenuation, attack, hold, release). Chain order: noise gate, 10-band equalizer, compressor / limiter. As for the rest, it acts on the microphone sent to the radio ("PC microphone to the radio" ticked).

## 4 sexdecies. What's new in 1.11

**Audio equalizer**: the 10 sliders now cover **50 Hz to 10 kHz** (50, 100, 200, 400, 800 Hz, 1.6, 3.15, 5, 7, 10 kHz) and the direction is fixed: **slider up = gain, down = attenuation**. Presets redone; old band settings are not kept (the equalizer starts flat).

## 4 septdecies. What's new in 1.12

**ADIF import**: files whose frequency is in kHz or Hz (OPSLOG, for example) now import without the "out of range value for column col_freq" error. The scale (MHz, kHz, Hz) is chosen from the QSO's band; a frequency that does not fit the band, or above 2.147 GHz, is not stored and its original value is appended to the comment ("[FREQ … ?]").

## 4 octodecies. What's new in 1.13

**Tolerant exports**: if a QSO causes a problem during an ADIF export (file or backup), it is skipped and the export carries on; a message tells how many QSOs were skipped. Same for online uploads to QRZ.com, HRDLog.net, HamQTH and eQSL: a refused QSO no longer stops the upload. Refused QSOs are marked **No** in that service's "sent" column (and will be retried on the next upload).


## 4 novodecies. What's new in 1.14

**Themes**: 115 themes sorted from brightest (no. 1, for daytime) to darkest (night). In the colours window: "Create a new theme…", "Export this theme…" (`.hrltheme` file), "Delete this theme". In the themes window: "Import a theme…". Your themes are in `data\themes`.

**Add window**: the **azimuth** is shown right of the distance (contact's locator, else its country centre). The end time follows the PC clock while "now" is ticked (default for a new QSO); a reopened QSO has it unticked, ticking it again restarts the clock. The **Rotor** button only appears if a rotor is really present and points it at the correspondent.

**Rotor menu** (right of Themes): model choice (Yaesu, WiMo… over 40; GS-232, DCU / Hy-Gain, SPID, Easycomm, network server protocols), port or address, go to an azimuth, long path, STOP, automatic follow of the QSO. The red **Calibration** button opens a separate window (confirmation with "Cancel": risk of upsetting the antenna alignment): type the real value directly, or adjust guided by + / −. Nothing is blocking. Hy-Gain, SPID and Easycomm are untested on hardware.

**Update**: only the previous version is kept (`HamRadioLink-x.y.exe`); older ones are deleted at startup.

**FT-2000**: the menu window re-reads the visible menus on opening, on scrolling and every 5 s ("Reading…"). New **EXT-GND PA** button under KEYER (menu 146).

## 4 vicies. What's new in 1.15

**Logbook > Tools > Check duplicates** (bottom of the menu).
1. The program looks for contacts with the same callsign and band whose start times differ by at most 120 seconds.
2. A window shows the report (one line per duplicate, scrollable). **Click a line**: the logbook jumps to the kept contact; click to the right of the word "supprimé" to see the deleted one. If a filter is active, remove it.
3. Nothing is deleted until you click the red **Supprimer les doublons** button (then confirm). **Fermer sans supprimer** cancels.
4. The most complete contact is kept, missing fields from the other are copied onto it, then the other is deleted. The report is also saved in `data\doublons-….txt`.

Deletion is permanent: make an ADIF backup first (Configuration > Backup).
