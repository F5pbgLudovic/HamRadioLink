using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace HamRadioLink.Journal
{
    // Fenêtre « Add / View » d'un QSO, même disposition que celle de l'autre logiciel Logbook.
    public class QsoForm : Form
    {
        readonly Qso orig;
        readonly bool isNew;
        public Qso Result; PeerLog log = App.Log; MySqlClient extraDb; ComboBox cLog; List<LogRef> logChoices = new List<LogRef>();   // le journal ouvert quand la fenetre a ete creee (un changement d onglet ne deroute pas l enregistrement)
        DateTimePicker dDate; DateTimePicker dStart, dEnd;
        TextBox tCall, tRstS, tRstR, tName, tComment, tIota, tLoc, tState, tQth, tWpx, tFreq, tCountry, tCont, tDxcc, tCq, tItu, tDist, tVia, tQslMsg, tContest, tSrx, tStx, tNotes, tSub;
        ComboBox cBand, cMode, cSub;
        ComboBox cQslS, cQslR, cLotwS, cLotwR, cEqslS, cEqslR;
        CheckBox kBand, kMode, kSwl, kEndNow;
        DataGridView prev;
        Button bAdd;
        CheckBox kDeep; bool deepReset; public Action ContactsChanged;
        void DeepOff() { deepReset = true; try { if (kDeep != null) kDeep.Checked = false; } finally { deepReset = false; } }
        // Clic droit sur un ancien contact : supprimer
        void PrevMenu(int row, Point p)
        {
            if (row < 0 || row >= prevQsos.Count) return; prev.ClearSelection(); prev.Rows[row].Selected = true;
            ContextMenuStrip m = new ContextMenuStrip(); Qso q = prevQsos[row];
            m.Items.Add("Supprimer le contact", null, delegate
            {
                if (MessageBox.Show(this, "Supprimer le contact " + q.S("COL_CALL") + " du " + q.S("COL_TIME_ON") + " ?", "Supprimer", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
                try { log.Delete(new[] { q.Id }); } catch (Exception ex) { Ui.Err(this, ex); return; }
                LoadPrev(); Action a = ContactsChanged; if (a != null) a();
            });
            m.Show(prev, p);
        }
        public void RefreshPrev() { try { if (!IsDisposed) LoadPrev(); } catch { } }
        public Action<Qso> OpenQso;   // double-clic sur un ancien contact : ouvre sa fiche (modifiable, bouton Mise à jour)

        public QsoForm(Qso q, string freqHz, string mode)
        {
            isNew = q == null;
            orig = q ?? new Qso();
            Text = (isNew ? "Add: " : "View: ") + App.Cfg.Get("db.name", "F5PBG");
            Size = new Size(1070, 780); StartPosition = FormStartPosition.CenterParent; Ui.AutoScale(this); Ui.RememberWindow(this, "qso", true); Shown += (s, e) => { StartTrack(); };
            Font = Ui.Normal; KeyPreview = true; BackColor = Color.FromArgb(176, 196, 240);
            Build();
            if (isNew)
            {
                DateTime now = DateTime.UtcNow;
                dDate.Value = now.Date; dStart.Value = now; dEnd.Value = now;
                if (!string.IsNullOrEmpty(freqHz)) SetFreq(freqHz);
                if (!string.IsNullOrEmpty(mode)) cMode.Text = mode;
                tRstS.Text = "59"; tRstR.Text = "59";
            }
            else Load(q);
            Shown += (s, e) => tCall.Focus();
            FormClosed += (s, e) => { if (extraDb != null) { try { extraDb.Dispose(); } catch { } } };
        }

        // Journal de destination du nouveau QSO (si plusieurs journaux) : connexion propre, le journal affiche dans le Logbook ne change pas
        void ChooseLog()
        {
            LogRef r = cLog.SelectedIndex >= 0 && cLog.SelectedIndex < logChoices.Count ? logChoices[cLog.SelectedIndex] : null; if (r == null) return;
            try
            {
                if (extraDb != null) { try { extraDb.Dispose(); } catch { } extraDb = null; }
                if (r == App.CurrentLog && App.Log != null) { log = App.Log; return; }
                string host, user, pass; int port; App.ServerOf(r, out host, out port, out user, out pass);
                extraDb = new MySqlClient(host, port, user, pass, r.Schema); extraDb.Open(); log = new PeerLog(extraDb, r.Schema);
            }
            catch (Exception ex) { Ui.Err(this, ex); log = App.Log; if (cLog.Items.Count > 0) cLog.SelectedIndex = Math.Max(0, logChoices.IndexOf(App.CurrentLog)); }
        }

        void Build()
        {
            Panel top = new Panel { Left = 0, Top = 0, Width = 1060, Height = 350 };
            Controls.Add(top);
            Ui.Lbl(top, "Date:", 8, 12); dDate = new DateTimePicker { Left = 68, Top = 10, Width = 140, Format = DateTimePickerFormat.Short }; top.Controls.Add(dDate);
            Ui.Lbl(top, "(F2) Start:", 8, 40); dStart = TimePick(top, 68, 38);
            Ui.Lbl(top, "(F3) End:", 8, 66); dEnd = TimePick(top, 68, 64);
            kEndNow = Ui.Chk(top, "now", 190, 66); kEndNow.Checked = true;
            Ui.Lbl(top, "(F5) Call:", 8, 100); tCall = Ui.Txt(top, 68, 98, 140); tCall.BackColor = Color.FromArgb(255, 255, 200); tCall.CharacterCasing = CharacterCasing.Upper;
            tCall.Leave += (s, e) => { callTimer.Stop(); OnCallLeft(); };
            // Dès qu'on modifie l'indicatif : on efface les infos de l'ancien ; les nouvelles s'affichent 0,5 s après la fin de la frappe.
            callTimer.Tick += delegate { callTimer.Stop(); OnCallLeft(); };
            tCall.TextChanged += (s, e) =>
            {
                if (!isNew) return;
                if (lastCall.Length > 0 && tCall.Text.Trim() != lastCall) { ClearCallInfo(); lastCall = ""; DeepOff(); }
                callTimer.Stop(); if (tCall.Text.Trim().Length > 0) callTimer.Start();
            };
            Ui.Btn(top, "qrz.com", 215, 96, 66, 24, (s, e) => OpenSite("https://www.qrz.com/db/"));
            Ui.Btn(top, "qrzcq", 287, 96, 56, 24, (s, e) => OpenSite("https://www.qrzcq.com/call/"));
            kDeep = Ui.Chk(top, "Recherche approfondie", 68, 122, 300); kDeep.CheckedChanged += (s, e) => { if (!deepReset && tCall.Text.Trim().Length > 0) LoadPrev(); };
            tLoc_TextChangedHook = true;
            Ui.Lbl(top, "RST Sent:", 8, 148); tRstS = Ui.Txt(top, 68, 146, 90);
            Ui.Lbl(top, "RST Rcvd:", 8, 174); tRstR = Ui.Txt(top, 68, 172, 90);
            Ui.Lbl(top, "Name:", 8, 202); tName = Ui.Txt(top, 68, 200, 320);
            Ui.Lbl(top, "Comment:", 8, 228); tComment = Ui.Txt(top, 68, 226, 320, 56);
            Ui.Lbl(top, "Country:", 8, 292); tCountry = Ui.Txt(top, 68, 290, 280);
            tCont = Ui.Txt(top, 352, 290, 36);
            Ui.Lbl(top, "DXCC:", 8, 318); tDxcc = Ui.Txt(top, 68, 316, 50);
            Ui.Lbl(top, "CQ:", 126, 318); tCq = Ui.Txt(top, 150, 316, 40);
            Ui.Lbl(top, "ITU:", 198, 318); tItu = Ui.Txt(top, 224, 316, 40);

            Ui.Lbl(top, "Freq (MHz):", 430, 12); tFreq = Ui.Txt(top, 510, 10, 110); tFreq.Font = Ui.Mono;
            tFreq.Leave += (s, e) => { long hz = ParseFreq(tFreq.Text); if (hz > 0) { SetFreq(hz.ToString()); } };
            Ui.Lbl(top, "Band:", 430, 40); cBand = Ui.Combo(top, 510, 38, 90, Ui.Bands, true); kBand = Ui.Chk(top, "track", 610, 40);
            Ui.Lbl(top, "Mode:", 430, 66); cMode = Ui.Combo(top, 510, 64, 90, Ui.Modes, true); kMode = Ui.Chk(top, "track", 610, 66); Ui.Lbl(top, "Radio :", 650, 12); cRadio = Ui.Combo(top, 700, 10, 190, null); FillRadios(); cRadio.DropDown += (s2, e2) => FillRadios();
            Ui.Lbl(top, "Journal :", 650, 40); cLog = new ComboBox { Left = 715, Top = 38, Width = 180, DropDownStyle = ComboBoxStyle.DropDownList, Font = Ui.Normal }; top.Controls.Add(cLog);
            if (isNew && App.Logs.Count > 1) { foreach (LogRef lr in App.Logs) { logChoices.Add(lr); cLog.Items.Add(lr.Name); } cLog.SelectedIndex = Math.Max(0, logChoices.IndexOf(App.CurrentLog)); cLog.SelectedIndexChanged += (s, e) => ChooseLog(); }
            else { cLog.Visible = false; top.Controls[top.Controls.Count - 2].Visible = false; }
            Ui.Lbl(top, "Submode:", 430, 92); cSub = Ui.Combo(top, 510, 90, 90, new[] { "", "USB", "LSB", "FT4", "PSK31", "PSK63", "JS8", "JT9" }, true);
            Ui.Lbl(top, "IOTA:", 430, 130); tIota = Ui.Txt(top, 510, 128, 90);
            Ui.Lbl(top, "Locator:", 430, 160); tLoc = Ui.Txt(top, 510, 158, 90); tLoc.CharacterCasing = CharacterCasing.Upper;
            Ui.Lbl(top, "State:", 430, 188); tState = Ui.Txt(top, 510, 186, 140);
            Ui.Lbl(top, "QTH:", 430, 216); tQth = Ui.Txt(top, 510, 214, 140);
            Ui.Lbl(top, "WPX:", 430, 244); tWpx = Ui.Txt(top, 510, 242, 60);
            Ui.Lbl(top, "Distance:", 430, 272); tDist = Ui.Txt(top, 510, 270, 70);
            kSwl = Ui.Chk(top, "SWL report", 520, 300);

            TabControl tabs = new TabControl { Left = 8, Top = 350, Width = 1040, Height = 340 };
            Controls.Add(tabs);
            TabPage tLog = new TabPage("Logbook"), tQsl = new TabPage("QSL"), tCon = new TabPage("Contest"), tNot = new TabPage("Notes"), tEq = new TabPage("eQSL.cc"), tLo = new TabPage("LOTW");
            tabs.TabPages.AddRange(new[] { tLog, tQsl, tCon, tEq, tLo, tNot });
            prev = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, BackgroundColor = Color.White, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells }; prev.CellClick += (s, e) => CopyFromPrevious(e.RowIndex); prev.CellMouseDown += (s, e) => { if (e.Button == MouseButtons.Right && e.RowIndex >= 0) PrevMenu(e.RowIndex, prev.PointToClient(Cursor.Position)); }; prev.CellDoubleClick +=(s, e) => { if (e.RowIndex >= 0 && e.RowIndex < prevQsos.Count && OpenQso != null) OpenQso(prevQsos[e.RowIndex]); }; prev.Cursor = Cursors.Hand; new ToolTip().SetToolTip(prev, "Clic : copie les infos de ce contact (nom, QTH, locator, pays...) dans le contact en cours");
            foreach (string h in new[] { "QSO date", "Time on", "Call", "Mode", "Sent", "Rcvd", "Band", "Freq", "Name", "Country", "Comment" }) prev.Columns.Add(h, h);
            tLog.Controls.Add(prev);
            string[] st = { "N", "Y", "R", "Q", "I" };
            Ui.Lbl(tQsl, "QSL sent:", 10, 14); cQslS = Ui.Combo(tQsl, 90, 12, 60, st); Ui.Lbl(tQsl, "QSL rcvd:", 170, 14); cQslR = Ui.Combo(tQsl, 250, 12, 60, st);
            Ui.Lbl(tQsl, "Via:", 10, 46); tVia = Ui.Txt(tQsl, 90, 44, 220);
            Ui.Lbl(tQsl, "Message:", 10, 78); tQslMsg = Ui.Txt(tQsl, 90, 76, 400);
            Ui.Lbl(tLo, "LOTW sent:", 10, 14); cLotwS = Ui.Combo(tLo, 90, 12, 60, st); Ui.Lbl(tLo, "LOTW rcvd:", 170, 14); cLotwR = Ui.Combo(tLo, 250, 12, 60, st);
            Ui.Lbl(tEq, "eQSL sent:", 10, 14); cEqslS = Ui.Combo(tEq, 90, 12, 60, st); Ui.Lbl(tEq, "eQSL rcvd:", 170, 14); cEqslR = Ui.Combo(tEq, 250, 12, 60, st);
            Ui.Lbl(tCon, "Contest:", 10, 14); tContest = Ui.Txt(tCon, 90, 12, 200);
            Ui.Lbl(tCon, "Sent nr:", 10, 46); tStx = Ui.Txt(tCon, 90, 44, 80); Ui.Lbl(tCon, "Rcvd nr:", 190, 46); tSrx = Ui.Txt(tCon, 260, 44, 80);
            tNotes = Ui.Txt(tNot, 10, 10, 800, 200); tNotes.Multiline = true;

            bAdd = Ui.Btn(this, isNew ? "Add (F7)" : "Mise à jour (F7)", 8, 700, isNew ? 90 : 130, 28, (s, e) => Commit());
            int bx = isNew ? 108 : 148;
            Ui.Btn(this, "Cancel", bx, 700, 90, 28, (s, e) => { DialogResult = DialogResult.Cancel; Close(); });
            Ui.Btn(this, "Reset (F4)", bx + 100, 700, 90, 28, (s, e) => { if (isNew) ResetFields(); });
            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.F7) { Commit(); e.Handled = true; }
                else if (e.KeyCode == Keys.F4) { if (isNew) ResetFields(); e.Handled = true; }
                else if (e.KeyCode == Keys.F2) { dStart.Value = DateTime.UtcNow; e.Handled = true; }
                else if (e.KeyCode == Keys.F3) { dEnd.Value = DateTime.UtcNow; e.Handled = true; }
                else if (e.KeyCode == Keys.F5) { tCall.Focus(); e.Handled = true; }
                else if (e.KeyCode == Keys.Escape) { DialogResult = DialogResult.Cancel; Close(); }
            };
        }

        public bool IsNewQso { get { return isNew; } }
        public void PrefillCall(string call) { tCall.Text = call; OnCallLeft(); }

        DateTimePicker TimePick(Control p, int x, int y)
        {
            DateTimePicker d = new DateTimePicker { Left = x, Top = y, Width = 110, Format = DateTimePickerFormat.Custom, CustomFormat = "HH:mm:ss", ShowUpDown = true };
            p.Controls.Add(d); return d;
        }

        static long ParseFreq(string t)
        {
            if (string.IsNullOrWhiteSpace(t)) return 0;
            t = t.Trim();
            double m;
            // "7.074.200" (format l'autre logiciel) ou "7.0742" (MHz)
            int dots = 0; foreach (char c in t) if (c == '.') dots++;
            if (dots >= 2) t = t.Replace(".", "");
            else t = t.Replace(',', '.');
            if (dots >= 2) { long hz; return long.TryParse(t, out hz) ? hz : 0; }
            if (!double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out m)) return 0;
            return m < 1000 ? (long)Math.Round(m * 1e6) : (long)m;
        }

        void SetFreq(string hz)
        {
            tFreq.Text = Ui.FreqText(hz); tFreq.Tag = hz;
            long f; if (long.TryParse(hz, out f)) { string b = Ui.BandOf(f); if (b.Length > 0) cBand.Text = b; }
        }

        void ResetFields()
        {
            foreach (TextBox t in new[] { tCall, tName, tComment, tCountry, tCont, tDxcc, tCq, tItu, tIota, tLoc, tState, tQth, tWpx, tDist, tVia, tQslMsg, tContest, tSrx, tStx, tNotes }) t.Text = "";
            tRstS.Text = "59"; tRstR.Text = "59"; prev.Rows.Clear(); DeepOff(); lastCall = ""; tCall.Focus();
        }

        bool tLoc_TextChangedHook;
        string lastCall = ""; readonly System.Windows.Forms.Timer callTimer = new System.Windows.Forms.Timer { Interval = 50 };
        void ClearCallInfo()
        {
            foreach (TextBox t in new[] { tName, tComment, tCountry, tCont, tDxcc, tCq, tItu, tIota, tLoc, tState, tQth, tWpx, tDist, tVia, tQslMsg }) t.Text = "";
            tRstS.Text = "59"; tRstR.Text = "59"; prev.Rows.Clear(); prevQsos = new List<Qso>();
        }
        void OnCallLeft()
        {
            string c = tCall.Text.Trim(); if (c.Length == 0 || c == lastCall) return;
            lastCall = c; LoadPrev(); FillFromPrevious(); FillFromCountry();
        }

        // Indicatif sans extension (/P /M /MM /QRP, préfixe de pays…) : la partie la plus longue qui contient un chiffre.
        public static string BaseCall(string call)
        {
            string best = (call ?? "").Trim().ToUpperInvariant(); string r = ""; foreach (string p in best.Split('/')) { bool dig = false; foreach (char ch in p) if (char.IsDigit(ch)) dig = true; if (dig && p.Length > r.Length) r = p; }
            return r.Length > 0 ? r : best;
        }

        void OpenSite(string url)
        {
            string c = BaseCall(tCall.Text); if (c.Length == 0) { MessageBox.Show(this, "Entrez d'abord un indicatif."); return; }
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url + Uri.EscapeDataString(c)) { UseShellExecute = true }); } catch (Exception ex) { Ui.Err(this, ex); }
        }

        // Pays, continent, DXCC, zones, préfixe WPX et distance d'après le préfixe (table des pays) et le locator
        void FillFromCountry()
        {
            if (!isNew) return;
            string call = tCall.Text.Trim(); Country c = Countries.Find(call);
            if (c != null)
            {
                if (tCountry.Text.Length == 0) tCountry.Text = c.Name;
                if (tCont.Text.Length == 0) tCont.Text = c.Cont;
                if (tDxcc.Text.Length == 0 || tDxcc.Text == "0") tDxcc.Text = c.Dxcc.ToString();
                if (tCq.Text.Length == 0 || tCq.Text == "0") tCq.Text = c.Cq.ToString();
                if (tItu.Text.Length == 0 || tItu.Text == "0") tItu.Text = c.Itu.ToString();
            }
            tWpx.Text = Countries.Wpx(call);
            UpdateDistance(c);
        }

        void UpdateDistance(Country c)
        {
            double la1, lo1, la2, lo2;
            if (!Geo.MyPos(out la1, out lo1)) return;
            if (Geo.GridToLatLon(tLoc.Text, out la2, out lo2) || (c != null && ((la2 = c.Lat) != 0 || (lo2 = c.Lon) != 0)))
                tDist.Text = ((int)Geo.Distance(la1, lo1, la2, lo2)).ToString();
        }

        void DoLookup()
        {
            string call = tCall.Text.Trim(); if (call.Length == 0) return;
            try
            {
                Cursor = Cursors.WaitCursor; LookupResult r = Online.Lookup(call); Cursor = Cursors.Default;
                if (r == null) { MessageBox.Show(this, "Indicatif introuvable.", "Lookup"); return; }
                if (r.Name.Length > 0) tName.Text = r.Name; if (r.Qth.Length > 0) tQth.Text = r.Qth; if (r.Grid.Length > 0) tLoc.Text = r.Grid.ToUpperInvariant();
                if (r.Country.Length > 0) tCountry.Text = r.Country; if (r.State.Length > 0) tState.Text = r.State; if (r.Iota.Length > 0) tIota.Text = r.Iota;
                if (r.Cq > 0) tCq.Text = r.Cq.ToString(); if (r.Itu > 0) tItu.Text = r.Itu.ToString(); if (r.Dxcc > 0) tDxcc.Text = r.Dxcc.ToString();
                if (r.QslVia.Length > 0) tVia.Text = r.QslVia;
                lookupExtra = r; UpdateDistance(Countries.Find(call));
            }
            catch (Exception ex) { Cursor = Cursors.Default; Ui.Err(this, ex); }
        }
        LookupResult lookupExtra;

        List<Qso> prevQsos = new List<Qso>();
        void LoadPrev()
        {
            prev.Rows.Clear();
            if (tCall.Text.Trim().Length == 0) return;
            try
            {
                string b = BaseCall(tCall.Text);   // l'indicatif seul + ses variantes /P /M /MM /QRP, préfixes de pays…
                prevQsos = log.Fetch("COL_CALL=" + MySqlClient.Quote(b) + " OR COL_CALL LIKE " + MySqlClient.Quote(b + "/%") + (kDeep != null && kDeep.Checked ? " OR COL_CALL LIKE " + MySqlClient.Quote("%/" + b) + " OR COL_CALL LIKE " + MySqlClient.Quote("%/" + b + "/%") : ""), "COL_TIME_ON DESC", 0, 100);
                foreach (Qso q in prevQsos)
                {
                    DateTime d; DateTime.TryParse(q.S("COL_TIME_ON"), CultureInfo.InvariantCulture, DateTimeStyles.None, out d);
                    prev.Rows.Add(d.ToString("dd/MM/yyyy"), d.ToString("HH:mm:ss"), q.S("COL_CALL"), q.S("COL_MODE"), q.S("COL_RST_SENT"), q.S("COL_RST_RCVD"), q.S("COL_BAND"), Ui.FreqText(q.S("COL_FREQ")), q.S("COL_NAME"), q.S("COL_COUNTRY"), q.S("COL_COMMENT"));
                }
            }
            catch (Exception ex) { Ui.Err(this, ex); }
        }

        // "track" : la 1re case suit la frequence et la bande de la radio, la 2e suit le mode, en temps reel (nouveau contact seulement).
        // Les cases ne sont jamais memorisees : elles repartent decochees a chaque fenetre et s'arretent avec elle.
        Timer trackTimer;
        static readonly string[] DigitalModes = { "FT8", "FT4", "PSK", "RTTY", "JT65", "JT9", "MFSK", "JS8", "OLIVIA", "SSTV", "WSPR", "PKT", "HELL", "THOR", "CONTESTIA", "DIGITALVOICE", "FAX" };
        // Choix de l'emetteur-recepteur suivi par CETTE fenetre Add (chaque fenetre est independante).
        ComboBox cRadio; readonly List<int> radioN = new List<int>(); public bool TrackOn = true;   // suivi active par defaut (pas pour un contact ouvert depuis un spot)
        void FillRadios()
        {
            int sel = cRadio.SelectedIndex >= 0 && cRadio.SelectedIndex < radioN.Count ? radioN[cRadio.SelectedIndex] : App.Instance;
            radioN.Clear(); cRadio.Items.Clear();
            foreach (InstInfo i in App.Instances()) { radioN.Add(i.N); cRadio.Items.Add((i.Name.Length > 0 ? i.Name : "Rig " + i.N) + (i.Online ? "" : " (hors ligne)")); }
            if (radioN.Count == 0) { radioN.Add(App.Instance); cRadio.Items.Add("Rig"); }
            int k = radioN.IndexOf(sel); cRadio.SelectedIndex = k >= 0 ? k : 0;
        }
        DateTime srcStamp = DateTime.MinValue; long srcHz; string srcMode = "";
        void ReadSource()
        {
            int n = cRadio.SelectedIndex >= 0 && cRadio.SelectedIndex < radioN.Count ? radioN[cRadio.SelectedIndex] : App.Instance;
            if (n == App.Instance) { srcHz = RigState.Hz; srcMode = RigState.Mode ?? ""; return; }
            if ((DateTime.UtcNow - srcStamp).TotalMilliseconds < 250) return; srcStamp = DateTime.UtcNow;
            foreach (InstInfo i in App.Instances()) if (i.N == n) { srcHz = i.Hz; srcMode = i.Mode ?? ""; return; }
            srcHz = 0;
        }

        void StartTrack()
        {
            if (!isNew) return;
            kBand.Checked = TrackOn; kMode.Checked = TrackOn;
            trackTimer = new Timer { Interval = 300 }; trackTimer.Tick += (s, e) => TrackTick(); trackTimer.Start(); TrackTick();
            FormClosed += (s, e) => { trackTimer.Stop(); trackTimer.Dispose(); };
        }
        void TrackTick()
        {
            if (!kBand.Checked && !kMode.Checked) return; ReadSource();
            if (kBand.Checked && srcHz > 0)
            {
                string f = srcHz.ToString(); if (Ui.FreqText(f) != tFreq.Text) SetFreq(f);
                string b = Ui.BandOf(srcHz); if (b.Length > 0 && cBand.Text != b) cBand.Text = b;
            }
            if (kMode.Checked && srcMode.Length > 0)
            {
                string m = srcMode.ToUpperInvariant(), t = null;
                if (m == "LSB" || m == "USB" || m == "CW" || m == "AM" || m == "FM") t = m; else if (m == "CWR") t = "CW"; else if (m.StartsWith("RTTY")) t = "RTTY";
                if (t != null && cMode.Text != t) cMode.Text = t;   // mode numerique (DATA...) : on garde celui choisi (FT8, PSK...)
            }
        }
        // Un clic sur un ancien contact (liste du bas) : copie ses infos (nom, QTH, locator, pays, zones, etat, IOTA, commentaire) dans le contact en cours.
        void CopyFromPrevious(int row)
        {
            if (row < 0 || row >= prevQsos.Count) return; Qso q = prevQsos[row];
            Action<TextBox, string> put = (t, v) => { if (!string.IsNullOrEmpty(v) && v != "0") t.Text = v; };
            put(tName, q.S("COL_NAME")); put(tQth, q.S("COL_QTH")); put(tLoc, q.S("COL_GRIDSQUARE")); put(tCountry, q.S("COL_COUNTRY")); put(tCont, q.S("COL_CONT"));
            put(tDxcc, q.S("COL_DXCC")); put(tCq, q.S("COL_CQZ")); put(tItu, q.S("COL_ITUZ")); put(tState, q.S("COL_STATE")); put(tIota, q.S("COL_IOTA")); put(tComment, q.S("COL_COMMENT"));
            Text = Text.Split(new[] { "   <" }, StringSplitOptions.None)[0] + "   < infos copiees du contact du " + q.S("COL_TIME_ON").Split(' ')[0] + " >";
        }

        // Reprend nom, QTH, locator, pays du dernier QSO avec cet indicatif (comme l'autre logiciel)
        void FillFromPrevious()
        {
            if (!isNew || prevQsos.Count == 0) return;
            Qso q = prevQsos[0];
            if (tName.Text.Length == 0) tName.Text = q.S("COL_NAME");
            if (tQth.Text.Length == 0) tQth.Text = q.S("COL_QTH");
            if (tLoc.Text.Length == 0) tLoc.Text = q.S("COL_GRIDSQUARE");
            if (tCountry.Text.Length == 0) tCountry.Text = q.S("COL_COUNTRY");
            if (tCont.Text.Length == 0) tCont.Text = q.S("COL_CONT");
            if (tDxcc.Text.Length == 0) tDxcc.Text = q.S("COL_DXCC");
            if (tCq.Text.Length == 0 || tCq.Text == "0") tCq.Text = q.S("COL_CQZ");
            if (tItu.Text.Length == 0 || tItu.Text == "0") tItu.Text = q.S("COL_ITUZ");
            if (tState.Text.Length == 0) tState.Text = q.S("COL_STATE");
            // commentaire : celui du dernier contact (le plus récent) qui en contient un
            if (tComment.Text.Length == 0)
            {
                string best = "", bt = "";
                foreach (Qso p in prevQsos) { string c = p.S("COL_COMMENT").Trim(), t = p.S("COL_TIME_ON"); if (c.Length > 0 && string.CompareOrdinal(t, bt) >= 0) { best = c; bt = t; } }
                if (best.Length > 0) tComment.Text = best;
            }
        }

        void Load(Qso q)
        {
            DateTime on, off;
            if (DateTime.TryParse(q.S("COL_TIME_ON"), CultureInfo.InvariantCulture, DateTimeStyles.None, out on)) { dDate.Value = on.Date; dStart.Value = on; }
            if (DateTime.TryParse(q.S("COL_TIME_OFF"), CultureInfo.InvariantCulture, DateTimeStyles.None, out off)) dEnd.Value = off; kEndNow.Checked = false;
            tCall.Text = q.S("COL_CALL"); tRstS.Text = q.S("COL_RST_SENT"); tRstR.Text = q.S("COL_RST_RCVD");
            tName.Text = q.S("COL_NAME"); tComment.Text = q.S("COL_COMMENT"); tCountry.Text = q.S("COL_COUNTRY"); tCont.Text = q.S("COL_CONT");
            tDxcc.Text = q.S("COL_DXCC"); tCq.Text = q.S("COL_CQZ"); tItu.Text = q.S("COL_ITUZ");
            SetFreq(q.S("COL_FREQ")); cBand.Text = q.S("COL_BAND"); cMode.Text = q.S("COL_MODE"); cSub.Text = q.S("COL_SUBMODE");
            tIota.Text = q.S("COL_IOTA"); tLoc.Text = q.S("COL_GRIDSQUARE"); tState.Text = q.S("COL_STATE"); tQth.Text = q.S("COL_QTH"); tWpx.Text = q.S("COL_PFX"); tDist.Text = q.S("COL_DISTANCE");
            kSwl.Checked = q.S("COL_SWL") == "1";
            tVia.Text = q.S("COL_QSL_VIA"); tQslMsg.Text = q.S("COL_QSLMSG"); tContest.Text = q.S("COL_CONTEST_ID"); tNotes.Text = q.S("COL_NOTES");
            tStx.Text = q.S("COL_STX_STRING"); tSrx.Text = q.S("COL_SRX_STRING");
            cQslS.Text = q.S("COL_QSL_SENT"); cQslR.Text = q.S("COL_QSL_RCVD"); cLotwS.Text = q.S("COL_LOTW_QSL_SENT"); cLotwR.Text = q.S("COL_LOTW_QSL_RCVD"); cEqslS.Text = q.S("COL_EQSL_QSL_SENT"); cEqslR.Text = q.S("COL_EQSL_QSL_RCVD");
            LoadPrev();
        }

        static string Nz(string s) { return string.IsNullOrEmpty(s) ? null : s; }

        void Commit()
        {
            if (tCall.Text.Trim().Length == 0) { MessageBox.Show(this, "Indicatif obligatoire.", "HamRadioLink"); tCall.Focus(); return; }
            Qso q = new Qso();
            DateTime on = dDate.Value.Date + dStart.Value.TimeOfDay, off = dDate.Value.Date + (kEndNow.Checked ? DateTime.UtcNow.TimeOfDay : dEnd.Value.TimeOfDay);
            if (off < on) off = off.AddDays(1);
            q["COL_TIME_ON"] = on.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            q["COL_TIME_OFF"] = off.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            q["COL_CALL"] = tCall.Text.Trim().ToUpperInvariant();
            q["COL_RST_SENT"] = tRstS.Text; q["COL_RST_RCVD"] = tRstR.Text;
            q["COL_NAME"] = Nz(tName.Text); q["COL_COMMENT"] = Nz(tComment.Text); q["COL_COUNTRY"] = Nz(tCountry.Text); q["COL_CONT"] = Nz(tCont.Text);
            q["COL_DXCC"] = Nz(tDxcc.Text); q["COL_CQZ"] = tCq.Text.Length == 0 ? "0" : tCq.Text; q["COL_ITUZ"] = tItu.Text.Length == 0 ? "0" : tItu.Text;
            long hz = ParseFreq(tFreq.Text); q["COL_FREQ"] = hz.ToString();
            q["COL_BAND"] = Nz(cBand.Text) ?? Ui.BandOf(hz); q["COL_MODE"] = Nz(cMode.Text); q["COL_SUBMODE"] = Nz(cSub.Text);
            q["COL_IOTA"] = Nz(tIota.Text); q["COL_GRIDSQUARE"] = Nz(tLoc.Text); q["COL_STATE"] = Nz(tState.Text); q["COL_QTH"] = Nz(tQth.Text); q["COL_PFX"] = Nz(tWpx.Text);
            double dist; if (double.TryParse(tDist.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out dist)) q["COL_DISTANCE"] = dist.ToString(CultureInfo.InvariantCulture);
            q["COL_SWL"] = kSwl.Checked ? "1" : "0";
            q["COL_QSL_VIA"] = Nz(tVia.Text); q["COL_QSLMSG"] = Nz(tQslMsg.Text); q["COL_CONTEST_ID"] = Nz(tContest.Text); q["COL_NOTES"] = Nz(tNotes.Text);
            if (lookupExtra != null) { q["COL_EMAIL"] = Nz(lookupExtra.Email); q["COL_WEB"] = Nz(lookupExtra.Web); q["COL_ADDRESS"] = Nz((lookupExtra.Address + " " + lookupExtra.Zip + " " + lookupExtra.Qth).Trim()); if (lookupExtra.HasLatLon) { q["COL_LAT"] = lookupExtra.Lat.ToString(CultureInfo.InvariantCulture); q["COL_LON"] = lookupExtra.Lon.ToString(CultureInfo.InvariantCulture); } }
            q["COL_STX_STRING"] = Nz(tStx.Text); q["COL_SRX_STRING"] = Nz(tSrx.Text);
            q["COL_QSL_SENT"] = Nz(cQslS.Text) ?? "N"; q["COL_QSL_RCVD"] = Nz(cQslR.Text) ?? "N";
            q["COL_LOTW_QSL_SENT"] = Nz(cLotwS.Text) ?? "N"; q["COL_LOTW_QSL_RCVD"] = Nz(cLotwR.Text) ?? "N";
            q["COL_EQSL_QSL_SENT"] = Nz(cEqslS.Text) ?? "N"; q["COL_EQSL_QSL_RCVD"] = Nz(cEqslR.Text) ?? "N";
            if (isNew)
            {   // ma station (profil) : comme l'autre logiciel qui recopie l'identité de la station
                q["COL_STATION_CALLSIGN"] = Nz(App.Cfg.Get("me.call", "")); q["COL_OPERATOR"] = Nz(App.Cfg.Get("me.call", "")); q["COL_OWNER_CALLSIGN"] = Nz(App.Cfg.Get("me.call", ""));
                q["COL_MY_GRIDSQUARE"] = Nz(App.Cfg.Get("me.grid", "")); q["COL_MY_CITY"] = Nz(App.Cfg.Get("me.city", "")); q["COL_MY_NAME"] = Nz(App.Cfg.Get("me.name", ""));
                q["COL_MY_RIG"] = Nz(App.Cfg.Get("me.rig", "")); q["COL_MY_POSTAL_CODE"] = Nz(App.Cfg.Get("me.zip", "")); q["COL_MY_COUNTRY"] = Nz(App.Cfg.Get("me.country", ""));
            }
            try
            {
                if (isNew) { q["COL_PRIMARY_KEY"] = log.Insert(q).ToString(); }
                else { log.Update(orig.Id, q); q["COL_PRIMARY_KEY"] = orig.Id.ToString(); }
                Result = q; DialogResult = DialogResult.OK; Close();
            }
            catch (Exception ex) { Ui.Err(this, ex); }
        }
    }
}



