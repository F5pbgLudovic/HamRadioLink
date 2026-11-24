using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace HamRadioLink.Journal
{
    class ColDef
    {
        public string Title, Sql; public int Width; public bool Visible, Right;
        public ColDef(string t, string s, int w, bool v, bool r = false) { Title = t; Sql = s; Width = w; Visible = v; Right = r; }
    }

    // Fenêtre Logbook : liste virtuelle (288 000 QSO sans lenteur), barre d'outils et menus classiques.
    public class LogbookForm : Form
    {
        OnlineForm onlineFrm; ColumnDragger colDrag;
        const int PageSize = 200;
        readonly DataGridView grid;
        readonly ToolStripStatusLabel lblCount, lblDb, lblFilter;
        readonly Dictionary<int, List<Qso>> pages = new Dictionary<int, List<Qso>>();
        readonly List<int> pageOrder = new List<int>();
        string where = "", order = "COL_TIME_ON DESC, COL_PRIMARY_KEY DESC";
        long total;
        TextBox search;
        readonly List<ColDef> cols = new List<ColDef>();
        string sortCol = "COL_TIME_ON"; bool sortDesc = true;

        static List<ColDef> AllCols()
        {
            return new List<ColDef> {
                new ColDef(Tr.T("QSO date"), "COL_TIME_ON", 90, true), new ColDef(Tr.T("Time on"), "COL_TIME_ON", 80, true), new ColDef("Call", "COL_CALL", 110, true), new ColDef("Locator", "COL_GRIDSQUARE", 70, true),
                new ColDef("Name", "COL_NAME", 130, true), new ColDef("QTH", "COL_QTH", 90, true), new ColDef("Freq", "COL_FREQ", 90, true, true), new ColDef("Mode", "COL_MODE", 60, true),
                new ColDef("Distance", "COL_DISTANCE", 70, true, true), new ColDef("Sent", "COL_RST_SENT", 50, true), new ColDef("Rcvd", "COL_RST_RCVD", 50, true),
                new ColDef(Tr.T("eQSL sent"), "COL_EQSL_QSL_SENT", 80, true), new ColDef(Tr.T("LOTW sent"), "COL_LOTW_QSL_SENT", 80, true), new ColDef(Tr.T("Club Log sent"), "COL_CLUBLOG_LOCAL", 90, true), new ColDef(Tr.T("QRZ.com sent"), "COL_QRZ_LOCAL", 90, true), new ColDef(Tr.T("HRDLog sent"), "COL_HRDLOG_LOCAL", 90, true), new ColDef(Tr.T("QRZCQ sent"), "COL_QRZCQ_LOCAL", 90, true), new ColDef(Tr.T("HamQTH sent"), "COL_HAMQTH_LOCAL", 90, true), new ColDef(Tr.T("WRL sent"), "COL_WRL_LOCAL", 90, true), new ColDef("Country", "COL_COUNTRY", 150, true), new ColDef("Comment", "COL_COMMENT", 300, true),
                new ColDef("Band", "COL_BAND", 50, false), new ColDef("Submode", "COL_SUBMODE", 70, false), new ColDef(Tr.T("Time off"), "COL_TIME_OFF", 80, false), new ColDef("State", "COL_STATE", 60, false),
                new ColDef("Continent", "COL_CONT", 40, false), new ColDef("DXCC", "COL_DXCC", 50, false), new ColDef("CQ", "COL_CQZ", 40, false, true), new ColDef("ITU", "COL_ITUZ", 40, false, true),
                new ColDef("IOTA", "COL_IOTA", 60, false), new ColDef(Tr.T("QSL sent"), "COL_QSL_SENT", 70, false), new ColDef(Tr.T("QSL rcvd"), "COL_QSL_RCVD", 70, false), new ColDef(Tr.T("LOTW rcvd"), "COL_LOTW_QSL_RCVD", 80, false),
                new ColDef(Tr.T("eQSL rcvd"), "COL_EQSL_QSL_RCVD", 80, false), new ColDef(Tr.T("QSL via"), "COL_QSL_VIA", 80, false), new ColDef("Station", "COL_STATION_CALLSIGN", 90, false), new ColDef("Operator", "COL_OPERATOR", 90, false),
                new ColDef("Prefix", "COL_PFX", 60, false), new ColDef("Contest", "COL_CONTEST_ID", 90, false), new ColDef("Notes", "COL_NOTES", 200, false), new ColDef("E-mail", "COL_EMAIL", 150, false),
                new ColDef("Power", "COL_TX_PWR", 50, false, true), new ColDef("Satellite", "COL_SAT_NAME", 80, false), new ColDef("Rig", "COL_MY_RIG", 100, false) };
        }

        public LogbookForm()
        {
            Text = Tr.T("HamRadioLink ") + App.Version + " Logbook - [" + App.Cfg.Get("db.name", "F5PBG") + "]";
            Size = new Size(1300, 860); Font = Ui.Normal; StartPosition = FormStartPosition.CenterScreen; Icon = Ui.AppIcon; Ui.RememberWindow(this, "lb");
            cols = AllCols();
            string vis = App.Cfg.Get("lb.cols", "");
            if (vis.Length > 0) { List<string> v = new List<string>(vis.Split(',')); foreach (ColDef c in cols) c.Visible = v.Contains(c.Title); if (App.Cfg.Get("lb.clcol", "") != "1") { foreach (ColDef c in cols) if (c.Title == "Club Log sent") c.Visible = true; App.Cfg.Set("lb.clcol", "1"); }
            if (App.Cfg.Get("lb.qrzcol", "") != "1") { foreach (ColDef c in cols) if (c.Title == "QRZ.com sent") c.Visible = true; App.Cfg.Set("lb.qrzcol", "1"); }
                if (App.Cfg.Get("lb.svc17", "") != "1") { foreach (ColDef c in cols) if (c.Title == "HRDLog sent" || c.Title == "QRZCQ sent" || c.Title == "HamQTH sent") c.Visible = true; App.Cfg.Set("lb.svc17", "1"); }
                if (App.Cfg.Get("lb.wrlcol", "") != "1") { foreach (ColDef c in cols) if (c.Title == "WRL sent") c.Visible = true; App.Cfg.Set("lb.wrlcol", "1"); } }   // 1.7 : nouvelles colonnes affichées une fois, même si une liste de colonnes était déjà mémorisée
            if (vis.Length > 0)   // ordre des colonnes enregistré (glisser-déposer des en-têtes) : les colonnes affichées reprennent leur place, les autres suivent
            {
                List<string> ord = new List<string>(vis.Split(',')); List<ColDef> re = new List<ColDef>();
                foreach (string o in ord) { ColDef f = cols.Find(c => c.Title == o && !re.Contains(c)); if (f != null) re.Add(f); }
                foreach (ColDef c in cols) if (!re.Contains(c)) re.Add(c);
                cols = re;
            }
            foreach (string kv in App.Cfg.Get("lb.colw", "").Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)) { int q = kv.LastIndexOf('='), wv; if (q > 0 && int.TryParse(kv.Substring(q + 1), out wv)) foreach (ColDef c in cols) if (c.Title == kv.Substring(0, q)) c.Width = Math.Max(20, wv); }   // largeurs mémorisées (à 100 % de zoom)

            MenuStrip ms = new ClickMenuStrip();
            ToolStripMenuItem file = M("File", M(Tr.T("Fenêtre de télécommande"), (s, e) => { Action a = Hub.ShowMain; if (a != null) a(); }), new ToolStripSeparator(), M("Import ADIF...", (s, e) => ImportAdif()), M("Export ADIF (selection or all)...", (s, e) => ExportAdif()), M("New log...", (s, e) => NewLog("")), new ToolStripSeparator(), M(Tr.T("Réglages des ports..."), (s, e) => Ui.ShowFree(new PortsForm(), this)), new ToolStripSeparator(), M("Open database...", (s, e) => OpenDb()), M(Tr.T("Importer / partager une base..."), (s, e) => OpenSetup()), M(Tr.T("Sauvegarder la base maintenant..."), (s, e) => Ui.ShowFree(new DbBackupForm(this), this)), M(Tr.T("Restaurer la base maintenant..."), (s, e) => Ui.ShowFree(new DbRestoreForm(this), this)), M(Tr.T("Moteur MariaDB..."),(s, e) => Ui.ShowFree(new EngineForm(), this)), new ToolStripSeparator(), M("Exit", (s, e) => Close()));
            file.DropDownOpening += delegate { bool emb = App.Cfg.Get("db.mode", "") == "embedded"; ToolStripItem eng = null; foreach (ToolStripItem it in file.DropDownItems) if (it.Text.StartsWith("Moteur MariaDB") || it.Text.StartsWith("MariaDB engine")) eng = it; if (eng != null) eng.Visible = emb; };   // le moteur n'a de réglages que pour le MariaDB intégré
            ToolStripMenuItem edit = M("Edit", M("Add\tCtrl+N", (s, e) => AddQso()), M("View / Edit\tEnter", (s, e) => EditSelected()), M("Delete\tDel", (s, e) => DeleteSelected()), new ToolStripSeparator(), M("Edit selections...", (s, e) => EditSelections()), M("Copy	Ctrl+C", (s, e) => CopyRows()), M("Cut	Ctrl+X", (s, e) => CutRows()), M("Paste	Ctrl+V", (s, e) => PasteRows()));
            ToolStripMenuItem view = M("View", M("Refresh\tF5", (s, e) => Reload()), M("Show all", (s, e) => { where = ""; search.Text = ""; Reload(); }), M("Columns (Layout)...", (s, e) => ChooseColumns()));
            ToolStripMenuItem logb = M("Logbook", M("Awards tracking...", (s, e) => ShowAwards(0)), M("Statistics...", (s, e) => Stats()), M("Find duplicates...", (s, e) => FindDupes()), M(Tr.T("QSL à envoyer (filtre)"), (s, e) => SetFilter("COL_QSL_SENT='Y' AND COL_QSL_RCVD<>'Y'")), M("Backup (ADIF)...", (s, e) => ExportAdif(true)), M("Update countries from QRZ/HamQTH data", (s, e) => UpdateFromCountries()));
            ToolStripMenuItem tools = M("Tools", M(Tr.T("Vérifier les distances"), (s, e) => CheckDistances()), M("Online services...", (s, e) => { if (onlineFrm != null && !onlineFrm.IsDisposed) { onlineFrm.Activate(); return; } onlineFrm = new OnlineForm(); onlineFrm.Show(this); }), M(Tr.T("Envoi automatique..."), (s, e) => AutoSend.ShowDialog(this)), M(Tr.T("Envoyer à tous les logs en ligne..."), (s, e) => SendAllOnline()), M(Tr.T("Exporter pour LoTW (fichier à importer à la main)..."), (s, e) => ExportLotw()),
                new ToolStripSeparator(), M("Upload selection to QRZ.com Logbook", (s, e) => SendAllOnline("qrz")),
                M("Upload selection to Club Log", (s, e) => SendAllOnline("clublog")), M("Upload selection to eQSL.cc", (s, e) => SendAllOnline("eqsl")), M("Upload selection to LoTW (TQSL)", (s, e) => SendAllOnline("lotw")), M("Upload selection to HRDLog.net", (s, e) => SendAllOnline("hrdlog")), M("Upload selection to QRZCQ.com", (s, e) => SendAllOnline("qrzcq")), M("Upload selection to HamQTH", (s, e) => SendAllOnline("hamqth")), M("Upload selection to World Radio League", (s, e) => SendAllOnline("wrl")),
                new ToolStripSeparator(), M(Tr.T("Vérifier les doublons"), (s, e) => CheckDuplicates()));
            ToolStripMenuItem conf = M(Tr.T("Configuration"), M("Langue / Language...", (s, e) => Ui.ShowFree(new LangForm(), this)), M(Tr.T("Station..."), (s, e) => Ui.ShowFree(new StationForm(), this)), M(Tr.T("DX Cluster..."), (s, e) => Ui.ShowFree(new ClusterForm(), this)), M(Tr.T("Importer / partager..."), (s, e) => OpenSetup()), M(Tr.T("Sauvegarde (ADIF)..."), (s, e) => ExportAdif(true)), M(Tr.T("Base..."), (s, e) => OpenDb()), M(Tr.T("Ports..."), (s, e) => { using (PortsForm f = new PortsForm()) f.ShowDialog(this); }));
            ToolStripMenuItem themes = M(Tr.T("Thèmes")); BuildThemeMenu(themes);
            ToolStripMenuItem rotor = M(Tr.T("Rotor")); rotor.Click += (s, e) => { if (rotorWin == null || rotorWin.IsDisposed) { rotorWin = new RotorForm(() => { List<Qso> sl = Selected(); return sl.Count > 0 ? sl[0] : null; }); rotorWin.Show(this); } else { rotorWin.RefreshSel(); rotorWin.Activate(); } };
            ToolStripMenuItem help = M("Help", M("About", (s, e) => MessageBox.Show(this, Tr.T("HamRadioLink ") + App.Version + "\r\nJournal MariaDB.", "About")));
            ms.Items.AddRange(new ToolStripItem[] { file, edit, view, logb, tools, conf, themes, rotor, help });
            MainMenuStrip = ms; menuStrip = ms;

            ToolStrip ts = new ClickToolStrip { GripStyle = ToolStripGripStyle.Hidden, ImageScalingSize = new Size(1, 1) };
            ts.Items.Add(B("Add", (s, e) => AddQso())); ts.Items.Add(B("Delete", (s, e) => DeleteSelected()));
            ts.Items.Add(new ToolStripSeparator());
            ts.Items.Add(B("QRZ.com", (s, e) => OpenQrz())); ts.Items.Add(B("QRZCQ", (s, e) => OpenQrzCq())); ts.Items.Add(B("Awards Tracking", (s, e) => ShowAwards(0)));
            ts.Items.Add(new ToolStripSeparator());
            search = new TextBox { Width = 130 }; ts.Items.Add(new ToolStripControlHost(search));
            search.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { SetFilter(PeerLog.QuickWhere(search.Text)); e.SuppressKeyPress = true; } };
            ts.Items.Add(B("Filter", (s, e) => AdvancedFilter())); ts.Items.Add(B("All", (s, e) => { search.Text = ""; SetFilter(""); }));
            ts.Items.Add(B("▲", (s, e) => { if (grid.RowCount > 0) { grid.FirstDisplayedScrollingRowIndex = 0; foreach (DataGridViewColumn c in grid.Columns) if (c.Visible) { grid.CurrentCell = grid[c.Index, 0]; break; } grid.ClearSelection(); grid.Rows[0].Selected = true; } }));   // remonter tout en haut
            ts.Items.Add(B("↔", (s, e) => FitColumns()));   // ajuste la largeur de chaque colonne à son contenu
            ts.Items.Add(B("↶", (s, e) => DoUndo(true))); ToolStripButton redo = B("↷", (s, e) => DoUndo(false)); ts.Items.Add(redo); LoupeButton loupe = new LoupeButton { Like = redo }; loupe.Click += (s, e) => Ui.ShowFree(new SearchForm(this), this); ts.Items.Add(loupe); { int q = 26; foreach (ToolStripItem it in ts.Items) if (it is LoupeButton || it.Text == "▲" || it.Text == "↔" || it.Text == "↶" || it.Text == "↷") { it.AutoSize = false; it.Size = new Size(q, q); it.Margin = new Padding(1, 1, 1, 2); } }   // ▲ ↔ ↶ ↷ et loupe : même petit carré   // loupe : recherche Suivant / Précédent (fenêtre non bloquante)   // annuler / refaire (Ctrl+Z / Ctrl+Y)

            grid = new LogGrid
            {
                Dock = DockStyle.Fill, VirtualMode = true, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToOrderColumns = true,
                RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = true,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None, AllowUserToResizeRows = false, RowTemplate = { Height = 20 },
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
            };
            grid.DefaultCellStyle.Font = Ui.Mono;
            BuildGridColumns();
            grid.CellValueNeeded += GridValue; grid.CellFormatting += GridFormat;
            colDrag = new ColumnDragger(grid);
            grid.ColumnHeaderMouseClick += (s, e) => { if (colDrag.Dragged || colDrag.Active) return; SortBy(e.ColumnIndex); };
            grid.CellMouseDown += (s, e) =>
            {
                if (e.Button != MouseButtons.Right || e.RowIndex < 0) return;
                if (!grid.Rows[e.RowIndex].Selected) { grid.ClearSelection(); grid.Rows[e.RowIndex].Selected = true; grid.CurrentCell = grid[Math.Max(0, e.ColumnIndex), e.RowIndex]; }
                ContextMenuStrip m = new ContextMenuStrip(); m.Items.Add(Tr.T("Supprimer le contact"), null, (a, b) => DeleteSelected()); m.Items.Add(new ToolStripSeparator());
                m.Items.Add(Tr.T("Exporter la sélection (ADIF)..."), null, (a, b) => ExportAdif(false, true)); m.Items.Add(Tr.T("Exporter la sélection pour LoTW..."), null, (a, b) => ExportLotw());
                ToolStripMenuItem snd = new ToolStripMenuItem(Tr.T("Envoyer la sélection à"));
                snd.DropDownItems.Add("QRZ.com", null, (a, b) => SendAllOnline("qrz")); snd.DropDownItems.Add(Tr.T("Club Log"), null, (a, b) => SendAllOnline("clublog"));
                snd.DropDownItems.Add("eQSL.cc", null, (a, b) => SendAllOnline("eqsl")); snd.DropDownItems.Add(Tr.T("LoTW (TQSL)"), null, (a, b) => SendAllOnline("lotw")); snd.DropDownItems.Add("HRDLog.net", null, (a, b) => SendAllOnline("hrdlog")); snd.DropDownItems.Add("QRZCQ.com", null, (a, b) => SendAllOnline("qrzcq")); snd.DropDownItems.Add("HamQTH", null, (a, b) => SendAllOnline("hamqth")); snd.DropDownItems.Add("World Radio League", null, (a, b) => SendAllOnline("wrl"));
                snd.DropDownItems.Add(new ToolStripSeparator()); snd.DropDownItems.Add(Tr.T("Tous les logs en ligne..."), null, (a, b) => SendAllOnline());
                m.Items.Add(snd);
                m.Items.Add(new ToolStripSeparator());
                string[] ks = { "qrz", "clublog", "eqsl", "lotw", "hrdlog", "qrzcq", "hamqth", "wrl", "all" }, ns = { "QRZ.com", Tr.T("Club Log"), "eQSL.cc", "LoTW", "HRDLog.net", "QRZCQ.com", "HamQTH", "World Radio League", Tr.T("Tous les services") };
                ToolStripMenuItem yes = new ToolStripMenuItem(Tr.T("Marquer la sélection comme envoyée à")), no = new ToolStripMenuItem(Tr.T("Marquer la sélection comme NON envoyée à"));
                for (int i = 0; i < ks.Length; i++) { string k = ks[i]; yes.DropDownItems.Add(ns[i], null, (a, b) => MarkSent(k, true)); no.DropDownItems.Add(ns[i], null, (a, b) => MarkSent(k, false)); }
                m.Items.Add(yes); m.Items.Add(no);
                ToolStripMenuItem cb = new ToolStripMenuItem(Tr.T("Changer la bande / fréquence de la sélection")); foreach (string bn in BandSet) { string b2 = bn; cb.DropDownItems.Add(bn, null, (a, b) => ChangeBand(b2)); }
                m.Items.Add(new ToolStripSeparator()); m.Items.Add(cb); m.Show(grid, grid.PointToClient(Cursor.Position));
            };
            grid.KeyDown += (s, e) => { if (e.Control && e.KeyCode == Keys.Z) { DoUndo(true); e.Handled = true; } else if (e.Control && e.KeyCode == Keys.Y) { DoUndo(false); e.Handled = true; } };
            grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) EditSelected(); };
            grid.ColumnWidthChanged += (s, e) => { ColDef c = e.Column.Tag as ColDef; if (c != null) { c.Width = e.Column.Width; if (c.Sql == "COL_COMMENT") RefreshHeights(); } };
            grid.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { EditSelected(); e.Handled = true; }
                else if (e.KeyCode == Keys.Delete) { DeleteSelected(); e.Handled = true; }
                else if (e.KeyCode == Keys.F5) { Reload(); e.Handled = true; }
                else if (e.Control && e.KeyCode == Keys.N) { AddQso(); e.Handled = true; }
                else if (e.Control && e.KeyCode == Keys.A) { grid.SelectAll(); e.Handled = true; }
                else if (e.Control && e.KeyCode == Keys.C) { CopyRows(); e.Handled = true; e.SuppressKeyPress = true; }
                else if (e.Control && e.KeyCode == Keys.X) { CutRows(); e.Handled = true; e.SuppressKeyPress = true; }
                else if (e.Control && e.KeyCode == Keys.V) { PasteRows(); e.Handled = true; }
            };

            StatusStrip status = new StatusStrip();
            lblFilter = new ToolStripStatusLabel("All") { BorderSides = ToolStripStatusLabelBorderSides.All };
            lblDb = new ToolStripStatusLabel("MySQL") { BorderSides = ToolStripStatusLabelBorderSides.All };
            lblCount = new ToolStripStatusLabel("0") { BorderSides = ToolStripStatusLabelBorderSides.All, Spring = true, TextAlign = ContentAlignment.MiddleRight };
            status.Items.AddRange(new ToolStripItem[] { lblFilter, lblDb, lblCount });

            logTabs = new LogTabs(); logTabs.ZoomStep += d => { LogZoom.Step("main", d); ApplyZoomMain(); }; logTabs.ZoomReset += () => { LogZoom.ResetAll(); ApplyZoomMain(); strip.ApplyZoom(); frames.ApplyZoom(); }; logTabs.TabSelected += SwitchLog; logTabs.AddClicked += () => NewLog(""); logTabs.RenameRequested += RenameLog; logTabs.RemoveRequested += RemoveLog; logTabs.DeleteRequested += DeleteLog;
            strip = new ClusterStrip(); frames = new ClusterFrames();
            strip.Width = Math.Max(120, App.Cfg.GetInt("cl.w", 200)); frames.Height = Math.Max(50, App.Cfg.GetInt("cl.h", 120));
            splitS = new Splitter { Dock = DockStyle.Right, Width = 4, MinSize = 120, MinExtra = 300, BackColor = Color.Black }; splitF = new Splitter { Dock = DockStyle.Bottom, Height = 4, MinSize = 50, MinExtra = 100, BackColor = Color.Black };
            splitS.SplitterMoved += (s, e) => { App.Cfg.Set("cl.w", strip.Width.ToString()); App.Cfg.Save(); }; splitF.SplitterMoved += (s, e) => { App.Cfg.Set("cl.h", frames.Height.ToString()); App.Cfg.Save(); };
            Controls.Add(grid); Controls.Add(splitF); Controls.Add(frames); Controls.Add(splitS); Controls.Add(strip); Controls.Add(logTabs); Controls.Add(ms); Controls.Add(status);
            toolStrip = ts; statusBar = status; grid.ColumnDisplayIndexChanged += (s, e) => { SyncColOrder(); FixLast(); }; grid.ColumnWidthChanged += (s, e) => { if (applyingZoom || stretching) return; ColDef cd = e.Column.Tag as ColDef; if (cd != null) cd.Width = Math.Max(20, (int)Math.Round(e.Column.Width / LogZoom.Factor("main"))); StretchLast(); }; grid.SizeChanged += (s, e) => StretchLast(); grid.RowsAdded += (s, e) => StretchLast(); grid.RowsRemoved += (s, e) => StretchLast(); ApplyLanguage(); ApplyTheme(); Theme.Changed += ApplyTheme; FormClosed += (s, e) => Theme.Changed -= ApplyTheme;
            FormClosing += (s, e) => { SaveCols(); };
            DxCluster.SpotClicked += OnSpotClicked; FormClosed += (s, e) => DxCluster.SpotClicked -= OnSpotClicked;
            ApplyZoomMain(); ApplyCluster(); Shown += (s, e) => { DxCluster.AutoStart(); if (App.Log == null) OpenDb(); else Reload(); };
        }

        // Clic sur un indicatif de la réglette : la fenêtre « Ajouter » la plus récente reprend l'indicatif, comme si on l'avait tapé
        void OnSpotClicked(Spot sp)
        {
            for (int i = qsoWins.Count - 1; i >= 0; i--) { QsoForm q = qsoWins[i]; if (!q.IsDisposed && q.IsNewQso) { q.PrefillCall(sp.Call); return; } }
        }
        ClusterStrip strip; ClusterFrames frames; Splitter splitS, splitF;
        internal void ApplyCluster() { strip.Visible = splitS.Visible = DxCluster.ShowStrip; frames.Visible = splitF.Visible = DxCluster.ShowFrames; }

        // Outils > Vérifier les distances : ajoute la distance manquante (locator du contact présent) ; une distance déjà présente est laissée.
        void CheckDistances()
        {
            if (App.Log == null) return;
            double la, lo; if (!Geo.MyPos(out la, out lo)) { MessageBox.Show(this, Tr.T("Renseignez d'abord votre locator : Configuration > Station."), "Distances"); return; }
            Bg.Run(this, Tr.T("Vérifier les distances"), (log, db, say, cancelled) =>
            {
                int done = 0, last = 0;
                while (!cancelled())
                {
                    List<Qso> l = log.Fetch(Tr.T("(COL_DISTANCE IS NULL OR COL_DISTANCE=0) AND COL_GRIDSQUARE<>'' AND COL_PRIMARY_KEY>") + last, "COL_PRIMARY_KEY ASC", 0, 500);
                    if (l.Count == 0) break;
                    foreach (Qso q in l) { last = Math.Max(last, q.Id); if (Geo.FillDistance(q)) { Qso c = new Qso(); c["COL_DISTANCE"] = q.S("COL_DISTANCE"); log.Update(q.Id, c); done++; } }
                    say(done + " distance(s) ajoutée(s)…", -1); System.Threading.Thread.Sleep(10);
                }
                return done + " distance(s) ajoutée(s)" + (cancelled() ? " (annulé)" : "") + ".";
            }, () => ReloadKeep());
        }

        // Outils > Vérifier les doublons : même indicatif, même bande, heures à ± 2 minutes. On garde le contact le plus renseigné,
        // on y reporte les champs qui lui manquent, on supprime l'autre.
        static bool Filled(string v) { return !string.IsNullOrEmpty(v) && v != "0" && !v.StartsWith("0000-00-00") && v != "0.0"; }
        static int InfoCount(Qso q) { int n = 0; foreach (KeyValuePair<string, string> kv in q) if (kv.Key != "COL_PRIMARY_KEY" && Filled(kv.Value)) n++; return n; }
        static string Desc(Qso q, Qso mark)
        {
            List<string> l = new List<string>();
            foreach (KeyValuePair<string, string> kv in q) if (kv.Key != "COL_PRIMARY_KEY" && Filled(kv.Value)) l.Add(kv.Key.Replace("COL_", "") + "=" + kv.Value + ((mark != null && mark.ContainsKey(kv.Key)) ? " (repris)" : ""));
            return string.Join("; ", l.ToArray());
        }
        class DupAct { public int Win, Lo; public Qso Fill; }
        List<DupAct> dupPlan = new List<DupAct>(); string dupReport = "";
        // Étape 1 : analyse seule (rien n'est modifié) + rapport ; étape 2 : suppression après confirmation.
        void CheckDuplicates()
        {
            if (App.Log == null) return;
            dupPlan = new List<DupAct>(); dupReport = "";
            List<DupAct> plan = dupPlan; System.Text.StringBuilder rep = new System.Text.StringBuilder();
            Bg.Run(this, Tr.T("Vérifier les doublons"), (log, db, say, cancelled) =>
            {
                List<Qso> all = new List<Qso>(); int off = 0;
                while (!cancelled())
                {
                    List<Qso> l = log.Fetch("", "COL_CALL ASC, COL_TIME_ON ASC, COL_PRIMARY_KEY ASC", off, 2000);
                    if (l.Count == 0) break; all.AddRange(l); off += l.Count; say(all.Count + " contact(s) lus…", -1);
                }
                if (cancelled()) { plan.Clear(); return "Annulé."; }
                int merged = 0; List<Qso> kept = new List<Qso>(); string curCall = null; List<DateTime> keptT = new List<DateTime>();
                foreach (Qso q in all)
                {
                    string call = q.S("COL_CALL").Trim().ToUpperInvariant(); DateTime t;
                    if (call.Length == 0 || !DateTime.TryParse(q.S("COL_TIME_ON"), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out t)) continue;
                    if (call != curCall) { kept.Clear(); keptT.Clear(); curCall = call; }
                    int hit = -1;
                    for (int k = 0; k < kept.Count; k++)
                    {
                        if (Math.Abs((t - keptT[k]).TotalSeconds) > 120) continue;
                        string b1 = kept[k].S("COL_BAND").Trim().ToLowerInvariant(), b2 = q.S("COL_BAND").Trim().ToLowerInvariant();
                        if (b1.Length > 0 && b2.Length > 0 && b1 != b2) continue;
                        hit = k; break;
                    }
                    if (hit < 0) { kept.Add(q); keptT.Add(t); continue; }
                    Qso a = kept[hit], w = a, lo = q;
                    if (InfoCount(q) > InfoCount(a)) { w = q; lo = a; }
                    Qso fill = new Qso();
                    foreach (KeyValuePair<string, string> kv in lo)
                        if (kv.Key != "COL_PRIMARY_KEY" && Filled(kv.Value) && !Filled(w.S(kv.Key))) fill[kv.Key] = kv.Value;
                    foreach (KeyValuePair<string, string> kv in fill) w[kv.Key] = kv.Value;
                    if (fill.Count > 0) merged++;
                    plan.Add(new DupAct { Win = w.Id, Lo = lo.Id, Fill = fill });
                    List<string> fk = new List<string>(); foreach (string k2 in fill.Keys) fk.Add(k2.Replace("COL_", ""));
                    rep.AppendLine(plan.Count + "  " + call + "  " + q.S("COL_TIME_ON") + "  " + q.S("COL_BAND") + "  | gardé id " + w.Id + " (" + InfoCount(w) + " champs) | supprimé id " + lo.Id + " (" + InfoCount(lo) + ")" + (fk.Count > 0 ? " | reporté : " + string.Join(",", fk.ToArray()) : ""));
                    kept[hit] = w; keptT[hit] = t;
                }
                dupReport = "Rapport « Vérifier les doublons » du " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " — AUCUNE modification faite à ce stade.\r\n" + plan.Count + " doublon(s) trouvé(s), " + merged + " contact(s) à compléter.\r\nCritère : même indicatif, même bande, heures à ± 2 minutes.\r\n\r\n" + rep.ToString();
                return plan.Count + " doublon(s) trouvé(s) sur " + all.Count + " contacts. Rien n'est encore supprimé.";
            }, () => ConfirmDuplicates());
        }
        Form dupWin;
        void ConfirmDuplicates()
        {
            if (dupPlan.Count == 0) { MessageBox.Show(this, Tr.T("Aucun doublon trouvé."), Tr.T("Vérifier les doublons")); return; }
            string path = System.IO.Path.Combine(App.DataDir, "doublons-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt");
            try { System.IO.File.WriteAllText(path, dupReport, new System.Text.UTF8Encoding(true)); } catch { }
            if (dupWin != null && !dupWin.IsDisposed) dupWin.Close();
            List<DupAct> plan = dupPlan; string[] lines = dupReport.Replace("\r", "").Split('\n');
            Form f = new Form(); dupWin = f;
            f.Text = Tr.T("Vérifier les doublons") + " — " + plan.Count; f.Font = Ui.Normal; f.Icon = Ui.AppIcon; f.StartPosition = FormStartPosition.Manual; f.Size = new Size(1000, 620); f.MinimizeBox = false;
            f.Location = new Point(Math.Max(0, Left + Width - 1000), Math.Max(0, Top + Height - 620));
            TextBox tb = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, WordWrap = false, Font = new Font("Consolas", 9f), Dock = DockStyle.Fill, Text = dupReport, BackColor = SystemColors.Window };
            Panel bar = new Panel { Dock = DockStyle.Top, Height = 78 };
            Label info = new Label { Left = 12, Top = 44, Width = 960, Height = 30, Text = Tr.T("Cliquez sur une ligne : le journal se place sur le contact gardé (clic à droite de « supprimé » : sur le contact supprimé). Rien n'est supprimé tant que vous n'avez pas cliqué sur le bouton rouge. Rapport :") + " " + path };
            Button ok = Ui.Btn(bar, Tr.T("Supprimer les doublons"), 12, 8, 240, 32, (s, e) =>
            {
                if (MessageBox.Show(f, plan.Count + Tr.T(" doublon(s) seront supprimés. Continuer ?"), Tr.T("Vérifier les doublons"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
                f.Close(); ApplyDuplicates(plan, path);
            });
            ok.BackColor = Color.Firebrick; ok.ForeColor = Color.White; ok.FlatStyle = FlatStyle.Flat;
            Button no = Ui.Btn(bar, Tr.T("Fermer sans supprimer"), 264, 8, 190, 32, (s, e) => f.Close());
            bar.Controls.Add(info);
            f.Controls.Add(tb); f.Controls.Add(bar); tb.Select(0, 0); f.CancelButton = no;
            tb.MouseUp += (s, e) =>
            {
                try
                {
                    int ci = tb.SelectionStart, ln = tb.GetLineFromCharIndex(ci); if (ln < 0 || ln >= lines.Length) return;
                    string L = lines[ln]; System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(L, @"^(\d+)\s");
                    if (!m.Success) return; int n = int.Parse(m.Groups[1].Value) - 1; if (n < 0 || n >= plan.Count) return;
                    int col = ci - tb.GetFirstCharIndexFromLine(ln), sp = L.IndexOf("| supprimé");
                    int id = (sp >= 0 && col >= sp) ? plan[n].Lo : plan[n].Win;
                    tb.Select(tb.GetFirstCharIndexFromLine(ln), L.Length);
                    GotoId(id, "");
                }
                catch { }
            };
            f.Show(this);
        }
        void ApplyDuplicates(List<DupAct> plan, string path)
        {
            Bg.Run(this, Tr.T("Supprimer les doublons"), (log, db, say, cancelled) =>
            {
                int n = 0;
                foreach (DupAct d in plan)
                {
                    if (cancelled()) return n + " doublon(s) supprimé(s) (annulé). Rapport : " + path;
                    if (d.Fill.Count > 0) log.Update(d.Win, d.Fill);
                    log.Delete(new int[] { d.Lo }); n++;
                    if (n % 50 == 0) say(n + " / " + plan.Count + " supprimé(s)…", n * 100 / plan.Count);
                }
                return n + " doublon(s) supprimé(s). Rapport : " + path;
            }, () => ReloadKeep());
        }

        MenuStrip menuStrip; ToolStrip toolStrip;
        StatusStrip statusBar; LogTabs logTabs;

        // ---------- onglets : plusieurs journaux ----------
        void SwitchLog(LogRef r)
        {
            if (r == App.CurrentLog) return; LogRef before = App.CurrentLog;
            try { Cursor = Cursors.WaitCursor; App.ConnectTo(r); where = ""; if (search != null) search.Text = ""; Text = Tr.T("HamRadioLink ") + App.Version + " Logbook - [" + r.Name + "]"; Reload(); logTabs.Invalidate(); }
            catch (Exception ex) { Ui.Err(this, ex); try { if (before != null) App.ConnectTo(before); } catch { } logTabs.Invalidate(); }
            finally { Cursor = Cursors.Default; }
        }

        void NewLog(string preset)
        {
            using (NewLogForm f = new NewLogForm(preset))
            {
                if (f.ShowDialog(this) != DialogResult.OK) return; LogRef r = f.Result;
                try
                {
                    if (r.Host.Length == 0)
                    {
                        string host, user, pass; int port; App.ServerOf(new LogRef(), out host, out port, out user, out pass);
                        Busy.Run(this, Ui.French ? Tr.T("Création du journal") : Tr.T("Creating the log"), say =>
                        {
                            using (MySqlClient c = new MySqlClient(host, port, user, pass, "mysql"))
                            {
                                c.Open(); LogSetup.EnsureTable(c, r.Schema, say);
                            }
                        });
                    }
                    App.Logs.Add(r); LogRef.SaveAll(App.Logs); SwitchLog(r);
                    if (App.CurrentLog != r) { App.Logs.Remove(r); LogRef.SaveAll(App.Logs); return; }
                    if (f.ThenImportAdif) ImportAdif();
                }
                catch (Exception ex) { Ui.Err(this, ex); }
            }
        }

        void RenameLog(LogRef l)
        {
            using (Form f = new Form { Text = Ui.French ? Tr.T("Renommer le journal") : Tr.T("Rename the log"), ClientSize = new Size(340, 90), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, Font = Ui.Normal })
            {
                TextBox t = Ui.Txt(f, 12, 14, 316); t.Text = l.Name; Button ok = Ui.Btn(f, "OK", 160, 48, 80, 28, (s, e) => f.DialogResult = DialogResult.OK); Ui.Btn(f, Ui.French ? "Annuler" : "Cancel", 248, 48, 80, 28, (s, e) => f.DialogResult = DialogResult.Cancel); f.AcceptButton = ok;
                if (f.ShowDialog(this) != DialogResult.OK || t.Text.Trim().Length == 0) return;
                l.Name = t.Text.Trim(); LogRef.SaveAll(App.Logs); if (l == App.CurrentLog) { App.Cfg.Set("db.name", l.Name); App.Cfg.Set("log.current", l.Name); App.Cfg.Save(); Text = Tr.T("HamRadioLink ") + App.Version + " Logbook - [" + l.Name + "]"; } logTabs.Invalidate();
            }
        }

        void RemoveLog(LogRef l)
        {
            if (App.Logs.Count <= 1) { MessageBox.Show(this, Ui.French ? Tr.T("Il faut garder au moins un journal.") : Tr.T("Keep at least one log."), "HamRadioLink"); return; }
            if (!Ui.Ask(this, (Ui.French ? Tr.T("Retirer « ") : Tr.T("Remove \"")) + l.Name + (Ui.French ? " » de la liste ? Les QSO restent dans la base (rien n'est supprimé)." : Tr.T("\" from the list? The QSOs stay in the database (nothing is deleted).")))) return;
            bool cur = l == App.CurrentLog; App.Logs.Remove(l); LogRef.SaveAll(App.Logs); if (cur) { App.CurrentLog = null; SwitchLog(App.Logs[0]); } logTabs.Invalidate();
        }
        // Retire le journal de la liste ET supprime sa base. Encart rouge, nom a retaper : aucune suppression par megarde.
        static readonly string[] SysSchemas = { "mysql", "information_schema", "performance_schema", "sys" };
        void DeleteLog(LogRef l)
        {
            if (App.Logs.Count <= 1) { MessageBox.Show(this, Ui.French ? Tr.T("Il faut garder au moins un journal.") : Tr.T("Keep at least one log."), "HamRadioLink"); return; }
            bool sys = Array.IndexOf(SysSchemas, l.Schema.ToLowerInvariant()) >= 0; bool shared = App.Logs.Exists(x => x != l && x.Schema == l.Schema && x.Host == l.Host && x.Port == l.Port);
            long n = 0; try { if (l == App.CurrentLog && App.Log != null) n = App.Log.Count(""); } catch { }
            string what = sys ? Tr.T("la TABLE du journal « ") + PeerLog.Table + " » dans la base système « " + l.Schema + " » (la base elle-même est conservée)" : Tr.T("TOUTE la base « ") + l.Schema + " »";
            if (shared) what = Tr.T("RIEN dans le serveur (un autre onglet utilise la même base : seul cet onglet est retiré)");
            string msg = Tr.T("ATTENTION - SUPPRESSION DÉFINITIVE\r\n\r\nJournal : « ") + l.Name + " »" + (n > 0 ? "  (" + n + " QSO)" : "") + Tr.T("\r\nVa être supprimé : ") + what + Tr.T(".\r\n\r\nTous les QSO concernés seront PERDUS. Cette action est IRRÉVERSIBLE : pensez à faire une sauvegarde ADIF avant.");
            using (Form f = new Form { Text = Tr.T("SUPPRESSION DÉFINITIVE"), ClientSize = new Size(560, 330), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, ShowInTaskbar = false, BackColor = Color.FromArgb(150, 0, 0), ForeColor = Color.White, Font = new Font(Tr.T("Segoe UI"), 10f) })
            {
                Label t = new Label { Left = 16, Top = 14, Width = 528, Height = 170, Text = msg, Font = new Font(Tr.T("Segoe UI"), 10.5f, FontStyle.Bold) }; f.Controls.Add(t);
                Label k = new Label { Left = 16, Top = 192, Width = 528, Height = 22, Text = Tr.T("Pour confirmer, retapez exactement le nom du journal :") }; f.Controls.Add(k);
                TextBox tb = new TextBox { Left = 16, Top = 218, Width = 528, Font = new Font(Tr.T("Segoe UI"), 11f) }; f.Controls.Add(tb);
                Button del = new Button { Left = 16, Top = 270, Width = 300, Height = 38, Text = Tr.T("SUPPRIMER DÉFINITIVEMENT"), Enabled = false, BackColor = Color.White, ForeColor = Color.FromArgb(150, 0, 0), FlatStyle = FlatStyle.Flat, Font = new Font(Tr.T("Segoe UI"), 10f, FontStyle.Bold), DialogResult = DialogResult.OK };
                Button no = new Button { Left = 336, Top = 270, Width = 208, Height = 38, Text = Tr.T("Annuler (ne rien faire)"), BackColor = Color.White, ForeColor = Color.Black, FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel }; f.Controls.Add(del); f.Controls.Add(no);
                tb.TextChanged += (s, e) => del.Enabled = tb.Text == l.Name; f.CancelButton = no;
                if (f.ShowDialog(this) != DialogResult.OK) return;
            }
            try
            {
                if (!shared)
                {
                    string host, user, pass; int port; App.ServerOf(l, out host, out port, out user, out pass);
                    using (MySqlClient c = new MySqlClient(host, port, user, pass, "mysql")) { c.Open(); if (sys) c.Exec(Tr.T("DROP TABLE IF EXISTS ") + MySqlClient.Ident(l.Schema) + "." + MySqlClient.Ident(PeerLog.Table)); else c.Exec(Tr.T("DROP DATABASE IF EXISTS ") + MySqlClient.Ident(l.Schema)); }
                }
            }
            catch (Exception ex) { Ui.Err(this, ex); return; }
            bool cur = l == App.CurrentLog; App.Logs.Remove(l); LogRef.SaveAll(App.Logs); if (cur) { App.CurrentLog = null; SwitchLog(App.Logs[0]); } logTabs.Invalidate();
        }
        // Fenetres Add / View : autant qu'on veut (un QSO a plusieurs correspondants), en cascade, SANS bloquer le journal.
        readonly List<QsoForm> qsoWins = new List<QsoForm>();
        void ShowQso(QsoForm f, Action onSaved)
        {
            f.ContactsChanged = () => Reload();
            f.OpenQso = delegate(Qso old) { ShowQso(new QsoForm(old, null, null), () => { Reload(); f.RefreshPrev(); }); };
            int n = qsoWins.Count; Rectangle wa = Screen.FromControl(this).WorkingArea;
            int x = Left + 70 + 30 * (n % 12), y = Top + 90 + 30 * (n % 12);
            x = Math.Max(wa.Left, Math.Min(x, wa.Right - f.Width)); y = Math.Max(wa.Top, Math.Min(y, wa.Bottom - f.Height));
            f.StartPosition = FormStartPosition.Manual; f.Location = new Point(x, y); if (f.Text.StartsWith("Add")) f.Text += "  (" + (n + 1) + ")";
            qsoWins.Add(f);
            f.FormClosed += (s, e) => { qsoWins.Remove(f); bool ok = f.DialogResult == DialogResult.OK; f.Dispose(); if (ok && !IsDisposed) { if (onSaved != null) onSaved(); else Reload(); } };
            f.Show(this);
        }

        // Zoom du texte de la liste des contacts (boutons ronds − / + tout à droite de la ligne des boutons) : taille de la police, hauteur des lignes et de l'en-tête
        float gridBase = 0, cellBase = 0; int zoomApplied = -1; bool applyingZoom, stretching;
        void ApplyZoomMain()
        {
            if (gridBase == 0) { gridBase = grid.Font.Size; cellBase = grid.DefaultCellStyle.Font != null ? grid.DefaultCellStyle.Font.Size : Ui.Mono.Size; }
            int zp = LogZoom.Get("main"); float z = zp / 100f;
            Font gf = grid.Font; grid.Font = new Font(gf.FontFamily, gridBase * z); if (gf != null && !object.ReferenceEquals(gf, Font)) { /* police héritée : rien à libérer */ }
            FontFamily fam = grid.DefaultCellStyle.Font != null ? grid.DefaultCellStyle.Font.FontFamily : Ui.Mono.FontFamily; grid.DefaultCellStyle.Font = new Font(fam, cellBase * z);
            applyingZoom = true; foreach (DataGridViewColumn c in grid.Columns) { ColDef cd = c.Tag as ColDef; if (cd != null) c.Width = Math.Max(24, (int)Math.Round(cd.Width * z)); } applyingZoom = false;   // les colonnes s'écartent comme le texte : on voit le même contenu
            int h = (int)Math.Ceiling(grid.DefaultCellStyle.Font.GetHeight()) + 6; grid.RowTemplate.Height = h; grid.ColumnHeadersHeight = Math.Max(22, (int)Math.Ceiling(grid.Font.GetHeight()) + 8);
            if (zoomApplied != zp && grid.RowCount > 0)
            {
                int rc = grid.RowCount, first = Math.Max(0, grid.FirstDisplayedScrollingRowIndex), sel = grid.CurrentCell != null ? grid.CurrentCell.RowIndex : -1, col = grid.CurrentCell != null ? grid.CurrentCell.ColumnIndex : -1;
                grid.RowCount = 0; grid.RowCount = rc;
                try { if (first < rc) grid.FirstDisplayedScrollingRowIndex = first; if (sel >= 0 && sel < rc && col >= 0) grid.CurrentCell = grid[col, sel]; } catch { }
            }
            zoomApplied = zp; StretchLast(); RefreshHeights(); grid.Invalidate();
        }
        // Couleurs du Logbook (View > Colors) : fond, liste, en-tetes, cluster, carte de bande.
        void ApplyTheme()
        {
            Color back = Theme.Get("lb.back"), tt = Theme.Get("lb.tool.text"); BackColor = back; Looks.Dark = Theme.IsDark(back);
            if (toolStrip != null) { toolStrip.BackColor = back; toolStrip.ForeColor = tt; foreach (ToolStripItem it in toolStrip.Items) if (!(it is ToolStripControlHost)) { it.ForeColor = tt; LoupeButton lpb = it as LoupeButton; if (lpb != null) lpb.SetColor(tt); } }
            if (statusBar != null) { statusBar.BackColor = back; statusBar.ForeColor = tt; foreach (ToolStripItem it in statusBar.Items) it.ForeColor = tt; }
            grid.BackgroundColor = Theme.Get("lb.grid.back"); grid.GridColor = Theme.Get("lb.grid.line"); grid.EnableHeadersVisualStyles = false;
            DataGridViewCellStyle d = grid.DefaultCellStyle; d.BackColor = Theme.Get("lb.grid.back"); d.ForeColor = Theme.Get("lb.grid.text"); d.SelectionBackColor = Theme.Get("lb.grid.sel"); d.SelectionForeColor = Theme.Get("lb.grid.seltext");
            DataGridViewCellStyle h = grid.ColumnHeadersDefaultCellStyle; h.BackColor = Theme.Get("lb.head.back"); h.ForeColor = Theme.Get("lb.head.text"); h.SelectionBackColor = Theme.Get("lb.head.back"); h.SelectionForeColor = Theme.Get("lb.head.text");
            
            grid.Invalidate(); Invalidate(true); if (gridBase != 0 && IsHandleCreated) BeginInvoke(new Action(ApplyZoomMain));
        }
        // Menu Thèmes : un clic applique le thème à toutes les fenêtres du journal (et le mémorise)
        ColorsForm colorsWin; RotorForm rotorWin;
        void BuildThemeMenu(ToolStripMenuItem m)
        {
            // « Thèmes » ouvre une petite fenêtre : liste défilante (ascenseur + molette), un clic applique le thème sans la refermer, ligne « Personnaliser » toujours visible en bas ;
            // un clic ailleurs la ferme.
            m.Click += (s, e) =>
            {
                Point p = m.Owner.PointToScreen(new Point(m.Bounds.Left, m.Bounds.Bottom));
                ThemePicker f = new ThemePicker(() => { if (colorsWin == null || colorsWin.IsDisposed) { colorsWin = new ColorsForm(); colorsWin.Show(this); } else colorsWin.Activate(); }); f.Location = p; f.Show(this);
            };
        }
        public void ApplyLanguage() { Ui.Translate(menuStrip); Ui.Translate(toolStrip); if (logTabs != null) logTabs.SetItems(toolStrip, search); }
        static ToolStripMenuItem M(string t, params ToolStripItem[] items) { ToolStripMenuItem m = new ToolStripMenuItem(t); m.DropDownItems.AddRange(items); return m; }
        static ToolStripMenuItem M(string t, EventHandler h) { int k = t.IndexOf('\t'); ToolStripMenuItem m = new ToolStripMenuItem(k >= 0 ? t.Substring(0, k) : t, null, h); if (k >= 0) m.ShortcutKeyDisplayString = t.Substring(k + 1); return m; }
        static ToolStripButton B(string t, EventHandler h) { ToolStripButton b = new ToolStripButton(t) { DisplayStyle = ToolStripItemDisplayStyle.Text }; b.Click += h; return b; }

        // ---------- colonnes ----------
        // La derniere colonne (dans l'ordre affiche) occupe toujours le reste de la largeur, jusqu'au bord, quoi qu'on deplace.
        // Toutes les colonnes sont redimensionnables à la souris (y compris la dernière) ; plus de colonne « étirée » automatiquement.
        void FixLast()
        {
            foreach (DataGridViewColumn c in grid.Columns) { c.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; c.MinimumWidth = 24; c.Resizable = DataGridViewTriState.True; }
            grid.AllowUserToResizeColumns = true; StretchLast();
        }
        // La dernière colonne (dans l'ordre affiché, par ex. « Commentaire ») va toujours jusqu'au bord droit de la liste : elle prend au moins la largeur restante,
        // et garde sa propre largeur (réglable à la souris) si elle est plus grande.
        void StretchLast()
        {
            if (stretching || grid == null || grid.Columns.Count == 0 || grid.ClientSize.Width < 50) return;
            DataGridViewColumn last = null; int sum = 0;
            foreach (DataGridViewColumn c in grid.Columns) { if (!c.Visible) continue; if (last == null || c.DisplayIndex > last.DisplayIndex) last = c; }
            if (last == null) return;
            foreach (DataGridViewColumn c in grid.Columns) if (c.Visible && c != last) sum += c.Width;
            int vs = 0; foreach (Control k in grid.Controls) { VScrollBar v = k as VScrollBar; if (v != null && v.Visible) vs = v.Width; }
            ColDef cd = last.Tag as ColDef; int own = cd != null ? (int)Math.Round(cd.Width * LogZoom.Factor("main")) : 24;
            int rest = grid.ClientSize.Width - vs - sum - 1; if (cd != null && cd.Sql == "COL_COMMENT") rest = Math.Min(rest, CommentCap());
            int want = Math.Max(Math.Max(24, own), rest);
            if (last.Width != want) { stretching = true; try { last.Width = want; } finally { stretching = false; } }
        }
        // ---------- colonne Commentaire : retour à la ligne (jamais au milieu d'un mot), la ligne du contact s'agrandit ----------
        const int CommentChars = 70;
        readonly HashSet<int> tallRows = new HashSet<int>(); readonly List<int> heightPages = new List<int>(); Timer hTimer;
        DataGridViewColumn CommentCol() { foreach (DataGridViewColumn c in grid.Columns) { ColDef cd = c.Tag as ColDef; if (cd != null && cd.Sql == "COL_COMMENT") return c; } return null; }
        int CommentCap() { Font f = grid.DefaultCellStyle.Font ?? grid.Font; return TextRenderer.MeasureText(new string('0', CommentChars), f, new Size(int.MaxValue, 100), TextFormatFlags.NoPadding).Width + 14; }
        void QueueHeights(int page) { if (!heightPages.Contains(page)) heightPages.Add(page); StartHeightTimer(); }
        void StartHeightTimer() { if (hTimer == null) { hTimer = new Timer { Interval = 80 }; hTimer.Tick += (s, e) => { hTimer.Stop(); ApplyHeights(); }; } hTimer.Stop(); hTimer.Start(); }
        void RefreshHeights() { heightPages.Clear(); foreach (int p in pageOrder) heightPages.Add(p); StartHeightTimer(); }
        void ApplyHeights()
        {
            DataGridViewColumn cc = CommentCol(); if (cc == null || grid.RowCount == 0) { heightPages.Clear(); return; }
            Font f = grid.DefaultCellStyle.Font ?? grid.Font; int tpl = grid.RowTemplate.Height, w = Math.Max(20, cc.Width - 10);
            List<int> todo = new List<int>(heightPages); heightPages.Clear();
            foreach (int p in todo)
            {
                List<Qso> page; if (!pages.TryGetValue(p, out page)) continue;
                for (int i = 0; i < page.Count; i++)
                {
                    int idx = p * PageSize + i; if (idx >= grid.RowCount) break;
                    string t = page[i].S("COL_COMMENT"); int need = tpl;
                    if (t.Length > 0) { int h = TextRenderer.MeasureText(t, f, new Size(w, int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.NoPadding).Height + 6; if (h > need) need = h; }
                    if (need > tpl) { grid.Rows[idx].Height = need; tallRows.Add(idx); }
                    else if (tallRows.Remove(idx)) grid.Rows[idx].Height = tpl;
                }
            }
            grid.Invalidate();
        }

        // Bouton ↔ : chaque colonne prend la largeur de son contenu affiché et de son titre, pour que tout soit lisible
        void FitColumns()
        {
            grid.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            foreach (DataGridViewColumn c in grid.Columns) c.Width += 12;
            DataGridViewColumn cm = CommentCol(); if (cm != null) cm.Width = Math.Min(cm.Width, CommentCap());
            StretchLast(); RefreshHeights();
        }
        bool buildingCols;
        // Les colonnes se déplacent à la souris (maintenir le clic sur l'en-tête puis glisser) : le nouvel ordre est gardé dans la liste des colonnes, donc enregistré et retrouvé au prochain démarrage. Aucune donnée n'est modifiée.
        void SyncColOrder()
        {
            if (buildingCols) return;
            List<DataGridViewColumn> vis = new List<DataGridViewColumn>(); foreach (DataGridViewColumn c in grid.Columns) if (c.Visible && c.Tag is ColDef) vis.Add(c);
            vis.Sort((a, b) => a.DisplayIndex.CompareTo(b.DisplayIndex)); int k = 0;
            for (int i = 0; i < cols.Count; i++) if (cols[i].Visible && k < vis.Count) cols[i] = (ColDef)vis[k++].Tag;
        }
        void BuildGridColumns() { BuildGridColumnsRaw(); FixLast(); }
        void BuildGridColumnsRaw()
        {
            buildingCols = true; try { grid.Columns.Clear();
            foreach (ColDef c in cols)
            {
                if (!c.Visible) continue;
                DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn { HeaderText = c.Title, Width = (int)Math.Round(c.Width * LogZoom.Factor("main")), MinimumWidth = 24, Resizable = DataGridViewTriState.True, SortMode = DataGridViewColumnSortMode.Programmatic, Tag = c };
                if (c.Right) col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                if (c.Sql == "COL_COMMENT") col.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                grid.Columns.Add(col);
            }
            }
            finally { buildingCols = false; }
        }

        void SaveCols() { List<string> v = new List<string>(), w = new List<string>(); foreach (ColDef c in cols) { if (c.Visible) v.Add(c.Title); w.Add(c.Title + "=" + c.Width); } App.Cfg.Set("lb.cols", string.Join(",", v.ToArray())); App.Cfg.Set("lb.colw", string.Join(";", w.ToArray())); App.Cfg.Save(); }

        void ChooseColumns()
        {
            using (Form f = new Form { Text = Tr.T("Layout : colonnes affichées"), Size = new Size(320, 560), StartPosition = FormStartPosition.CenterParent, Font = Ui.Normal })
            {
                CheckedListBox l = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true };
                foreach (ColDef c in cols) l.Items.Add(c.Title, c.Visible);
                Panel p = new Panel { Dock = DockStyle.Bottom, Height = 44 };
                Button ok = Ui.Btn(p, "OK", 130, 8, 80, 28, (s, e) => f.DialogResult = DialogResult.OK); Ui.Btn(p, Tr.T("Annuler"), 215, 8, 80, 28, (s, e) => f.DialogResult = DialogResult.Cancel);
                f.Controls.Add(l); f.Controls.Add(p); f.AcceptButton = ok;
                if (f.ShowDialog(this) != DialogResult.OK) return;
                for (int i = 0; i < cols.Count; i++) cols[i].Visible = l.GetItemChecked(i);
                BuildGridColumns(); grid.RowCount = 0; grid.RowCount = (int)Math.Min(total, int.MaxValue); SaveCols();
            }
        }

        // ---------- données ----------
        internal void Reload()
        {
            if (App.Log == null) return;
            try
            {
                pages.Clear(); pageOrder.Clear(); tallRows.Clear(); heightPages.Clear();
                order = sortCol + (sortDesc ? " DESC" : " ASC") + Tr.T(", COL_PRIMARY_KEY ") + (sortDesc ? "DESC" : "ASC");
                total = App.Log.Count(where);
                grid.RowCount = 0; grid.RowCount = (int)Math.Min(total, int.MaxValue);
                lblCount.Text = total.ToString("N0", CultureInfo.CurrentCulture) + " QSO";
                lblFilter.Text = where.Length == 0 ? "All" : "Filtered"; lblDb.Text = Tr.T("MySQL ") + App.Db.ServerVersion;
            }
            catch (Exception ex) { Ui.Err(this, ex); }
        }

        // Recharge la liste SANS la ramener en haut : même position de défilement, mêmes lignes sélectionnées (après un envoi, une suppression…)
        void ReloadKeep()
        {
            int first = 0, cur = -1, col = 0; List<int> sel = new List<int>();
            try { if (grid.RowCount > 0 && grid.FirstDisplayedScrollingRowIndex >= 0) first = grid.FirstDisplayedScrollingRowIndex; if (grid.CurrentCell != null) { cur = grid.CurrentCell.RowIndex; col = grid.CurrentCell.ColumnIndex; } foreach (DataGridViewRow r in grid.SelectedRows) sel.Add(r.Index); } catch { }
            Reload();
            try
            {
                if (grid.RowCount == 0) return;
                grid.ClearSelection(); if (cur >= 0 && cur < grid.RowCount && col >= 0 && col < grid.ColumnCount) grid.CurrentCell = grid[col, cur];
                foreach (int r in sel) if (r < grid.RowCount) grid.Rows[r].Selected = true;
                grid.FirstDisplayedScrollingRowIndex = Math.Min(first, grid.RowCount - 1);
            }
            catch { }
        }
        public void ReloadFromOutside() { Reload(); }

        void SetFilter(string w) { where = w ?? ""; Reload(); }

        // ---------- recherche (loupe) : Suivant / Précédent dans l'ordre du tableau, sur tout le journal affiché (filtre compris) ----------
        string RevOrder() { return sortCol + (sortDesc ? " ASC" : " DESC") + ", COL_PRIMARY_KEY " + (sortDesc ? "ASC" : "DESC"); }
        // QSO affichés APRÈS (after) ou AVANT le QSO « id » dans l'ordre courant du tableau (valeurs vides comprises)
        string CondRel(int id, bool after)
        {
            string c = MySqlClient.Ident(sortCol), pk = "COL_PRIMARY_KEY", rc = "(SELECT " + c + " FROM " + App.Log.TableRef() + " WHERE " + pk + "=" + id + ")";
            if (after != sortDesc) return "(" + c + ">" + rc + " OR (" + rc + " IS NULL AND " + c + " IS NOT NULL) OR (" + c + "<=>" + rc + " AND " + pk + ">" + id + "))";
            return "(" + c + "<" + rc + " OR (" + c + " IS NULL AND " + rc + " IS NOT NULL) OR (" + c + "<=>" + rc + " AND " + pk + "<" + id + "))";
        }
        static string MatchSql(int field, string text)
        {
            if (field == 0)
            {
                DateTime d; System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(text, @"^(\d{4})(?:-(\d{1,2}))?$");
                if (m.Success) return "COL_TIME_ON LIKE " + MySqlClient.Quote(m.Groups[1].Value + (m.Groups[2].Success ? "-" + m.Groups[2].Value.PadLeft(2, '0') : "") + "%");
                if (DateTime.TryParse(text, new CultureInfo("fr-FR"), DateTimeStyles.None, out d) || DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out d)) return "DATE(COL_TIME_ON)=" + MySqlClient.Quote(d.ToString("yyyy-MM-dd"));
                throw new Exception(Tr.T("Date non reconnue (exemple : 22/11/2026 ou 2026-11-22)."));
            }
            if (field == 1) return "COL_CALL LIKE " + MySqlClient.Quote("%" + text + "%");
            return PeerLog.QuickWhere(text);
        }
        // Date saisie (jour précis, ou début du mois / de l'année si seul « 2026-11 » ou « 2026 » est donné)
        static bool TryDate(string text, out DateTime d)
        {
            System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(text.Trim(), @"^(\d{4})(?:-(\d{1,2}))?$");
            if (m.Success) { int mo = m.Groups[2].Success ? int.Parse(m.Groups[2].Value) : 1; d = new DateTime(int.Parse(m.Groups[1].Value), Math.Max(1, Math.Min(12, mo)), 1); return true; }
            return DateTime.TryParse(text, new CultureInfo("fr-FR"), DateTimeStyles.None, out d) || DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out d);
        }
        // Se place sur la ligne du QSO « idv » (sélection + défilement) et indique sa position
        string GotoId(int idv, string note)
        {
            long row = App.Log.Count((where.Length > 0 ? "(" + where + ") AND " : "") + CondRel(idv, false));
            int r = (int)Math.Min(row, grid.RowCount - 1); int ci = -1; foreach (DataGridViewColumn c in grid.Columns) if (c.Visible && (ci < 0 || c.DisplayIndex < grid.Columns[ci].DisplayIndex)) ci = c.Index;
            if (ci < 0) return "";
            int vis = Math.Max(1, grid.DisplayedRowCount(false)), first = grid.FirstDisplayedScrollingRowIndex;
            if (r < first || r >= first + vis) grid.FirstDisplayedScrollingRowIndex = Math.Max(0, r - vis / 2);
            grid.CurrentCell = grid[ci, r]; grid.ClearSelection(); grid.Rows[r].Selected = true;
            return note + Tr.T("Trouvé : ligne ") + (r + 1).ToString("N0") + " / " + grid.RowCount.ToString("N0") + ".";
        }
        // Remonte tout en haut du tableau (première ligne sélectionnée)
        internal string GoTop()
        {
            if (grid.RowCount == 0) return Tr.T("Le journal est vide.");
            int ci = -1; foreach (DataGridViewColumn c in grid.Columns) if (c.Visible && (ci < 0 || c.DisplayIndex < grid.Columns[ci].DisplayIndex)) ci = c.Index;
            if (ci < 0) return "";
            grid.FirstDisplayedScrollingRowIndex = 0; grid.CurrentCell = grid[ci, 0]; grid.ClearSelection(); grid.Rows[0].Selected = true;
            return Tr.T("Début du tableau.");
        }
        internal string FindNext(int field, string text, bool fwd)
        {
            if (App.Log == null || grid.RowCount == 0) return Tr.T("Le journal est vide.");
            text = text.Trim(); if (text.Length == 0) return Tr.T("Saisissez ce qu'il faut chercher.");
            string m = MatchSql(field, text), T = App.Log.TableRef(), wh = where.Length > 0 ? "(" + where + ") AND " : "", ord = fwd ? order : RevOrder();
            int cur = grid.CurrentCell != null ? grid.CurrentCell.RowIndex : -1; Qso cq = cur >= 0 ? RowQso(cur) : null; string note = "";
            string id = cq != null ? App.Db.Scalar("SELECT COL_PRIMARY_KEY FROM " + T + " WHERE " + wh + "(" + m + ") AND " + CondRel(cq.Id, fwd) + " ORDER BY " + ord + " LIMIT 1") : null;
            if (string.IsNullOrEmpty(id))
            {
                id = App.Db.Scalar("SELECT COL_PRIMARY_KEY FROM " + T + " WHERE " + wh + "(" + m + ") ORDER BY " + ord + " LIMIT 1");
                if (string.IsNullOrEmpty(id))
                {
                    DateTime want;
                    if (field == 0 && TryDate(text, out want))   // la date n'existe pas dans le journal : on va au QSO dont la date en est la plus proche
                    {
                        id = App.Db.Scalar("SELECT COL_PRIMARY_KEY FROM " + T + " WHERE " + wh + "COL_TIME_ON IS NOT NULL ORDER BY ABS(DATEDIFF(DATE(COL_TIME_ON), " + MySqlClient.Quote(want.ToString("yyyy-MM-dd")) + ")) ASC, COL_PRIMARY_KEY DESC LIMIT 1");
                        if (string.IsNullOrEmpty(id)) return Tr.T("Aucun QSO trouvé.");
                        string dn = App.Db.Scalar("SELECT DATE_FORMAT(COL_TIME_ON, '%d/%m/%Y') FROM " + T + " WHERE COL_PRIMARY_KEY=" + int.Parse(id));
                        return GotoId(int.Parse(id), Tr.T("Aucun QSO à cette date : date la plus proche, le ") + dn + ". ");
                    }
                    return Tr.T("Aucun QSO trouvé.");
                }
                if (cq != null) note = fwd ? Tr.T("Fin du tableau atteinte : retour au début. ") : Tr.T("Début du tableau atteint : retour à la fin. ");
            }
            return GotoId(int.Parse(id), note);
        }

        void SortBy(int col)
        {
            ColDef cd = grid.Columns[col].Tag as ColDef; if (cd == null || cd.Sql.EndsWith("_LOCAL")) return;   // colonne locale : pas de tri
            if (cd.Sql == sortCol) sortDesc = !sortDesc; else { sortCol = cd.Sql; sortDesc = false; }
            Reload();
        }

        Qso RowQso(int row)
        {
            int p = row / PageSize; List<Qso> page;
            if (!pages.TryGetValue(p, out page))
            {
                try { page = App.Log.Fetch(where, order, p * PageSize, PageSize); } catch (Exception ex) { Ui.Err(this, ex); page = new List<Qso>(); }
                pages[p] = page; pageOrder.Add(p); QueueHeights(p);
                if (pageOrder.Count > 40) { pages.Remove(pageOrder[0]); pageOrder.RemoveAt(0); }
            }
            int i = row - p * PageSize; return i < page.Count ? page[i] : null;
        }

        static string YesNo(string v) { switch (v) { case "Y": return "Yes"; case "N": case "": case null: return "No"; case "R": return "Requested"; case "Q": return "Queued"; case "I": return "Invalid"; default: return v; } }

        void GridValue(object s, DataGridViewCellValueEventArgs e)
        {
            Qso q = RowQso(e.RowIndex); if (q == null) { e.Value = ""; return; }
            ColDef c = grid.Columns[e.ColumnIndex].Tag as ColDef; if (c == null) return; DateTime d;
            switch (c.Title)
            {
                case "QSO date": e.Value = DateTime.TryParse(q.S("COL_TIME_ON"), CultureInfo.InvariantCulture, DateTimeStyles.None, out d) ? d.ToString("dd/MM/yyyy") : ""; break;
                case "Time on": e.Value = DateTime.TryParse(q.S("COL_TIME_ON"), CultureInfo.InvariantCulture, DateTimeStyles.None, out d) ? d.ToString("HH:mm:ss") : ""; break;
                case "Time off": e.Value = DateTime.TryParse(q.S("COL_TIME_OFF"), CultureInfo.InvariantCulture, DateTimeStyles.None, out d) ? d.ToString("HH:mm:ss") : ""; break;
                case "Freq": e.Value = Ui.FreqText(q.S("COL_FREQ")); break;
                case "Distance": { double v; e.Value = double.TryParse(q.S("COL_DISTANCE"), NumberStyles.Float, CultureInfo.InvariantCulture, out v) && v > 0 ? ((int)v).ToString() : ""; break; }
                case "eQSL sent": case "LOTW sent": case "eQSL rcvd": case "LOTW rcvd": case "QSL sent": case "QSL rcvd": e.Value = YesNo(q.S(c.Sql)); break;
                case "Club Log sent": e.Value = ClubLogState.IsSent(q.Id) ? "Yes" : "No"; break;
                case "QRZ.com sent": e.Value = QrzState.IsSent(q.Id) ? "Yes" : "No"; break;
                case "HRDLog sent": e.Value = SentState.IsSent("hrdlog", q.Id) ? "Yes" : "No"; break;
                case "QRZCQ sent": e.Value = SentState.IsSent("qrzcq", q.Id) ? "Yes" : "No"; break;
                case "HamQTH sent": e.Value = SentState.IsSent("hamqth", q.Id) ? "Yes" : "No"; break;
                case "WRL sent": e.Value = SentState.IsSent("wrl", q.Id) ? "Yes" : "No"; break;
                case "Comment": e.Value = q.S("COL_COMMENT"); break;
                case "CQ": case "ITU": case "Power": e.Value = q.S(c.Sql) == "0" ? "" : q.S(c.Sql); break;
                default: e.Value = q.S(c.Sql); break;
            }
        }

        void GridFormat(object s, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || grid.Rows[e.RowIndex].Selected) return;
            e.CellStyle.BackColor = (e.RowIndex % 2 == 1) ? Theme.Get("lb.grid.alt") : Theme.Get("lb.grid.back");
        }

        List<Qso> Selected()
        {
            List<Qso> l = new List<Qso>(); List<int> rows = new List<int>();
            foreach (DataGridViewRow r in grid.SelectedRows) rows.Add(r.Index);
            rows.Sort(); foreach (int i in rows) { Qso q = RowQso(i); if (q != null) l.Add(q); }
            return l;
        }

        // ---------- actions ----------
        void AddQso()
        {
            if (App.Log == null) return;
            ShowQso(new QsoForm(null, App.Cfg.Get("rig.freq", ""), App.Cfg.Get("rig.mode", "")), null);
        }

        void EditSelected()
        {
            List<Qso> s = Selected(); if (s.Count == 0) return;
                        ShowQso(new QsoForm(s[0], null, null), () => { int r = grid.CurrentRow != null ? grid.CurrentRow.Index : 0; Reload(); if (r < grid.RowCount && grid.ColumnCount > 0) grid.CurrentCell = grid[0, r]; });
        }

        // Changement de bande par lot : la fréquence prend la valeur habituelle du mode (FT8 / FT4), sinon le début de la bande
        static readonly string[] BandSet = { "160m", "80m", "60m", "40m", "30m", "20m", "17m", "15m", "12m", "10m", "6m", "2m", "70cm" };
        static readonly long[] BandStart = { 1800000, 3500000, 5351500, 7000000, 10100000, 14000000, 18068000, 21000000, 24890000, 28000000, 50000000, 144000000, 430000000 };
        static readonly long[] BandFt8 = { 1840000, 3573000, 5357000, 7074000, 10136000, 14074000, 18100000, 21074000, 24915000, 28074000, 50313000, 144174000, 432174000 };
        static readonly long[] BandFt4 = { 1840000, 3575000, 5357000, 7047500, 10140000, 14080000, 18104000, 21140000, 24919000, 28180000, 50318000, 144170000, 432174000 };
        void ChangeBand(string band)
        {
            List<Qso> sel = Selected(); int bi = Array.IndexOf(BandSet, band); if (sel.Count == 0 || bi < 0 || App.Log == null) return;
            try
            {
                Dictionary<long, List<int>> g = new Dictionary<long, List<int>>();
                foreach (Qso q in sel)
                {
                    string md = (q.S("COL_MODE") + " " + q.S("COL_SUBMODE")).ToUpperInvariant(); long f = BandStart[bi];
                    if (md.Contains("FT8")) f = BandFt8[bi]; else if (md.Contains("FT4")) f = BandFt4[bi];
                    List<int> l; if (!g.TryGetValue(f, out l)) { l = new List<int>(); g[f] = l; } l.Add(q.Id);
                }
                List<int> uid = new List<int>(); foreach (Qso q in sel) uid.Add(q.Id); Dictionary<int, Qso> bef = LogUndo.Snap(App.Log, uid);
                long n = 0; bool hasBand = App.Log.Has("COL_BAND");
                foreach (KeyValuePair<long, List<int>> kv in g)
                    for (int i = 0; i < kv.Value.Count; i += 500)
                    {
                        List<string> ids = new List<string>(); for (int j = i; j < Math.Min(i + 500, kv.Value.Count); j++) ids.Add(kv.Value[j].ToString());
                        n += App.Db.Exec("UPDATE " + App.Log.TableRef() + " SET COL_FREQ=" + MySqlClient.Quote(kv.Key.ToString()) + (hasBand ? Tr.T(", COL_BAND=") + MySqlClient.Quote(band) : "") + " WHERE COL_PRIMARY_KEY IN (" + string.Join(",", ids.ToArray()) + ")");
                    }
                LogUndo.Record(Tr.T("Changement de bande"), App.Log, bef, uid); ReloadKeep();
            }
            catch (Exception ex) { Ui.Err(this, ex); }
        }

        // Annuler / Refaire : ↶ et ↷ (ou Ctrl+Z / Ctrl+Y dans la liste)
        void DoUndo(bool back)
        {
            try
            {
                string n = back ? LogUndo.Undo() : LogUndo.Redo();
                if (n == null) { MessageBox.Show(this, back ? Tr.T("Rien à annuler.") : Tr.T("Rien à refaire."), back ? "Annuler" : "Refaire"); return; }
                ReloadKeep();
            }
            catch (Exception ex) { Ui.Err(this, ex); }
        }

        // Copier / Coller de contacts (Ctrl+C / Ctrl+V) : le collage crée de nouveaux contacts (nouveau numéro), dans le journal affiché ; annulable
        static List<Qso> copied = new List<Qso>();
        void CopyRows()
        {
            List<Qso> s = Selected(); if (s.Count == 0) return;
            copied = new List<Qso>(); foreach (Qso q in s) { Qso c = new Qso(); foreach (KeyValuePair<string, string> kv in q) c[kv.Key] = kv.Value; copied.Add(c); }
            CopySel();
        }
        // Couper (Ctrl+X) : copie puis supprime les contacts sélectionnés (annulable avec Ctrl+Z)
        void CutRows()
        {
            List<Qso> s = Selected(); if (s.Count == 0 || App.Log == null) return;
            CopyRows();
            try { List<int> ids = new List<int>(); foreach (Qso q in s) ids.Add(q.Id); Dictionary<int, Qso> bef = LogUndo.Snap(App.Log, ids); App.Log.Delete(ids); LogUndo.Record(Tr.T("Couper ") + ids.Count + " contact(s)", App.Log, bef, ids); ReloadKeep(); }
            catch (Exception ex) { Ui.Err(this, ex); }
        }

        void PasteRows()
        {
            if (copied.Count == 0 || App.Log == null) return;
            try
            {
                List<int> ids = new List<int>();
                foreach (Qso q in copied) { Qso c = new Qso(); foreach (KeyValuePair<string, string> kv in q) if (kv.Key != "COL_PRIMARY_KEY") c[kv.Key] = kv.Value; ids.Add(App.Log.Insert(c)); }
                Dictionary<int, Qso> bef = new Dictionary<int, Qso>(); foreach (int i in ids) bef[i] = null;
                LogUndo.Record(Tr.T("Coller ") + ids.Count + " contact(s)", App.Log, bef, ids); ReloadKeep();
            }
            catch (Exception ex) { Ui.Err(this, ex); }
        }

        void DeleteSelected()
        {
            List<Qso> s = Selected(); if (s.Count == 0) return;
            if (!Ui.Ask(this, Tr.T("Supprimer ") + s.Count + " QSO de la base ?\r\nCette opération est définitive.")) return;
            try { List<int> ids = new List<int>(); foreach (Qso q in s) ids.Add(q.Id); Dictionary<int, Qso> bef = LogUndo.Snap(App.Log, ids); App.Log.Delete(ids); LogUndo.Record("Suppression", App.Log, bef, ids); ReloadKeep(); } catch (Exception ex) { Ui.Err(this, ex); }
        }

        void CopySel()
        {
            StringBuilder sb = new StringBuilder();
            foreach (Qso q in Selected()) sb.AppendLine(q.S("COL_TIME_ON") + "\t" + q.S("COL_CALL") + "\t" + Ui.FreqText(q.S("COL_FREQ")) + "\t" + q.S("COL_MODE") + "\t" + q.S("COL_RST_SENT") + "\t" + q.S("COL_RST_RCVD") + "\t" + q.S("COL_NAME"));
            if (sb.Length > 0) Clipboard.SetText(sb.ToString());
        }

        // Modification en bloc d'un champ sur la sélection (ou sur tout le filtre)
        void EditSelections()
        {
            List<Qso> sel = Selected();
            using (Form f = new Form { Text = Tr.T("Edit selections"), Size = new Size(430, 250), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, Font = Ui.Normal })
            {
                Ui.Lbl(f, Tr.T("Champ :"), 12, 18); ComboBox c = Ui.Combo(f, 90, 16, 300, new[] { "COL_QSL_SENT", "COL_QSL_RCVD", "COL_LOTW_QSL_SENT", "COL_LOTW_QSL_RCVD", "COL_EQSL_QSL_SENT", "COL_EQSL_QSL_RCVD", "COL_COMMENT", "COL_NOTES", "COL_QSL_VIA", "COL_MY_RIG", "COL_STATION_CALLSIGN", "COL_OPERATOR", "COL_CONTEST_ID", "COL_SUBMODE" }, true); c.SelectedIndex = 0;
                Ui.Lbl(f, Tr.T("Valeur :"), 12, 50); TextBox v = Ui.Txt(f, 90, 48, 300); v.Text = "Y";
                RadioButton r1 = new RadioButton { Left = 90, Top = 84, Width = 300, Text = Tr.T("Les ") + sel.Count + " QSO sélectionnés", Checked = sel.Count > 0, Enabled = sel.Count > 0 }; f.Controls.Add(r1);
                RadioButton r2 = new RadioButton { Left = 90, Top = 108, Width = 320, Text = Tr.T("Tous les ") + total.ToString("N0") + " QSO du filtre actuel", Checked = sel.Count == 0 }; f.Controls.Add(r2);
                Button ok = Ui.Btn(f, Tr.T("Appliquer"), 220, 160, 90, 28, (s, e) => f.DialogResult = DialogResult.OK); Ui.Btn(f, Tr.T("Annuler"), 320, 160, 80, 28, (s, e) => f.DialogResult = DialogResult.Cancel); f.AcceptButton = ok;
                if (f.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    string field = c.Text.Trim(); if (!App.Log.Has(field)) { MessageBox.Show(this, Tr.T("Champ inconnu : ") + field); return; }
                    string w = where;
                    if (r1.Checked) { List<string> ids = new List<string>(); foreach (Qso q in sel) ids.Add(q.Id.ToString()); w = "COL_PRIMARY_KEY IN (" + string.Join(",", ids.ToArray()) + ")"; }
                    else if (!Ui.Ask(this, Tr.T("Modifier ") + total.ToString("N0") + " QSO ?")) return;
                    string upd = MySqlClient.Ident(field) + "=" + MySqlClient.Quote(v.Text);
                    if (field.EndsWith("_SENT") || field.EndsWith("_RCVD")) { string d = field.Replace("_RCVD", "RDATE").Replace("_SENT", "SDATE"); if (App.Log.Has(d) && v.Text == "Y") upd += "," + MySqlClient.Ident(d) + Tr.T("=NOW()"); }
                    string wf = w, uf = upd; List<int> uid = new List<int>(); Dictionary<int, Qso> bef = null; if (r1.Checked) { foreach (Qso q in sel) uid.Add(q.Id); bef = LogUndo.Snap(App.Log, uid); }
                    Bg.Run(this, Tr.T("Modification de la colonne ") + field, (log, db, say, cancelled) => { say(Tr.T("Mise à jour en cours…"), -1); long n = UpdateChunked(db, log, uf, wf, cancelled, say); return n + " QSO modifiés" + (cancelled() ? " (annulé)" : "") + "."; }, () => { if (bef != null) LogUndo.Record(Tr.T("Modification en bloc"), App.Log, bef, uid); ReloadKeep(); });
                }
                catch (Exception ex) { Ui.Err(this, ex); }
            }
        }

        void AdvancedFilter()
        {
            using (Form f = new Form { Text = "Filter", Size = new Size(430, 330), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, Font = Ui.Normal })
            {
                Ui.Lbl(f, Tr.T("Indicatif :"), 12, 16); TextBox call = Ui.Txt(f, 100, 14, 140);
                Ui.Lbl(f, Tr.T("Bande :"), 12, 48); ComboBox band = Ui.Combo(f, 100, 46, 100, Ui.Bands, true);
                Ui.Lbl(f, Tr.T("Mode :"), 12, 80); ComboBox mode = Ui.Combo(f, 100, 78, 100, Ui.Modes, true);
                Ui.Lbl(f, Tr.T("Pays :"), 12, 112); TextBox ctry = Ui.Txt(f, 100, 110, 200);
                Ui.Lbl(f, "Du :", 12, 144); DateTimePicker d1 = new DateTimePicker { Left = 100, Top = 142, Width = 110, Format = DateTimePickerFormat.Short, ShowCheckBox = true, Checked = false }; f.Controls.Add(d1);
                Ui.Lbl(f, "Au :", 220, 144); DateTimePicker d2 = new DateTimePicker { Left = 255, Top = 142, Width = 110, Format = DateTimePickerFormat.Short, ShowCheckBox = true, Checked = false }; f.Controls.Add(d2);
                Ui.Lbl(f, Tr.T("Texte libre :"), 12, 178); TextBox sql = Ui.Txt(f, 100, 176, 300); new ToolTip().SetToolTip(sql, Tr.T("Cherche ce texte dans l'indicatif, le nom, le QTH, le pays, le commentaire et le locator"));
                Button ok = Ui.Btn(f, Tr.T("Filtrer"), 220, 230, 90, 28, (s, e) => f.DialogResult = DialogResult.OK); Ui.Btn(f, Tr.T("Annuler"), 320, 230, 80, 28, (s, e) => f.DialogResult = DialogResult.Cancel); f.AcceptButton = ok;
                if (f.ShowDialog(this) != DialogResult.OK) return;
                List<string> w = new List<string>();
                if (call.Text.Trim().Length > 0) w.Add("COL_CALL LIKE " + MySqlClient.Quote(call.Text.Trim().Replace("*", "%") + (call.Text.Contains("*") ? "" : "%")));
                if (band.Text.Length > 0) w.Add("COL_BAND=" + MySqlClient.Quote(band.Text)); if (mode.Text.Length > 0) w.Add("COL_MODE=" + MySqlClient.Quote(mode.Text));
                if (ctry.Text.Trim().Length > 0) w.Add("COL_COUNTRY LIKE " + MySqlClient.Quote("%" + ctry.Text.Trim() + "%"));
                if (d1.Checked) w.Add("COL_TIME_ON>=" + MySqlClient.Quote(d1.Value.ToString("yyyy-MM-dd"))); if (d2.Checked) w.Add("COL_TIME_ON<" + MySqlClient.Quote(d2.Value.AddDays(1).ToString("yyyy-MM-dd")));
                if (sql.Text.Trim().Length > 0) w.Add(PeerLog.QuickWhere(sql.Text.Trim()));
                SetFilter(string.Join(" AND ", w.ToArray()));
            }
        }

        void OpenQrzCq() { List<Qso> s = Selected(); string c = s.Count > 0 ? s[0].S("COL_CALL") : search.Text.Trim(); if (c.Length > 0) try { Process.Start("https://www.qrzcq.com/call/" + Uri.EscapeDataString(c)); } catch { } }
        void OpenQrz() { List<Qso> s = Selected(); string c = s.Count > 0 ? s[0].S("COL_CALL") : search.Text.Trim(); if (c.Length > 0) try { Process.Start("https://www.qrz.com/db/" + Uri.EscapeDataString(c)); } catch { } }

        // Importer / partager : l'assistant qui retrouve les journaux MariaDB d'un autre logiciel (copie ou partage direct), installe le MariaDB intégré si besoin, importe un fichier ADIF.
        void OpenSetup()
        {
            Ui.ShowFree(new SetupForm(true), this);
            if (App.Log != null) { App.LoadLogs(); logTabs.Invalidate(); Text = Tr.T("HamRadioLink ") + App.Version + " Logbook - [" + App.Cfg.Get("db.name", "Journal") + "]"; Reload(); }
        }
        void OpenDb()
        {
            DbForm f = new DbForm(); f.FormClosed += (s, e) => { if (f.DialogResult == DialogResult.OK) { Text = Tr.T("HamRadioLink ") + App.Version + " Logbook - [" + App.Cfg.Get("db.name", "F5PBG") + "]"; Reload(); } }; Ui.ShowFree(f, this);
        }

        // Plusieurs journaux : demande dans lequel importer (le journal choisi devient le journal affiché). false = annulé.
        bool PickImportLog()
        {
            if (App.Logs == null || App.Logs.Count < 2) return true;
            using (Form f = new Form { Text = Tr.T("Importer un ADIF"), FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent, ClientSize = new Size(420, 110), MaximizeBox = false, MinimizeBox = false, ShowInTaskbar = false, Font = Ui.Normal })
            {
                Ui.Lbl(f, Tr.T("Importer dans quel journal ?"), 12, 14, 396); List<string> nm = new List<string>(); foreach (LogRef l in App.Logs) nm.Add(l.Name);
                ComboBox cb = Ui.Combo(f, 12, 40, 396, nm); cb.DropDownStyle = ComboBoxStyle.DropDownList; cb.SelectedIndex = Math.Max(0, App.Logs.IndexOf(App.CurrentLog));
                Button ok = Ui.Btn(f, "OK", 230, 72, 85, 28, (s, e) => { f.DialogResult = DialogResult.OK; }); Ui.Btn(f, Tr.T("Annuler"), 323, 72, 85, 28, (s, e) => { f.DialogResult = DialogResult.Cancel; }); f.AcceptButton = ok;
                if (f.ShowDialog(this) != DialogResult.OK) return false;
                SwitchLog(App.Logs[cb.SelectedIndex]); return App.CurrentLog == App.Logs[cb.SelectedIndex];
            }
        }
        void ImportAdif()
        {
            if (!PickImportLog()) return;
            using (OpenFileDialog o = new OpenFileDialog { Filter = Tr.T("ADIF (*.adi;*.adif)|*.adi;*.adif|All|*.*") })
            {
                if (o.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    List<Dictionary<string, string>> recs = Adif.Parse(File.ReadAllText(o.FileName, Encoding.UTF8));
                    if (!Ui.Ask(this, recs.Count + " QSO trouvés. Les ajouter au journal ?")) return;
                    string me = App.Cfg.Get("me.call", "");
                    Bg.Run(this, Tr.T("Import ADIF"), (log, db, say, cancelled) =>
                    {
                        int n = 0, dup = 0, k = 0;
                        foreach (Dictionary<string, string> r in recs)
                        {
                            if (cancelled()) break;
                            Qso q = Adif.ToQso(r, log); if (!q.ContainsKey("COL_CALL")) continue;
                            string dupSql = "COL_CALL=" + MySqlClient.Quote(q.S("COL_CALL")) + " AND COL_TIME_ON=" + MySqlClient.Quote(q.S("COL_TIME_ON")) + (q.ContainsKey("COL_BAND") ? " AND COL_BAND=" + MySqlClient.Quote(q.S("COL_BAND")) : "");
                            if (log.Count(dupSql) > 0) { dup++; continue; }
                            if (!q.ContainsKey("COL_STATION_CALLSIGN") && me.Length > 0) q["COL_STATION_CALLSIGN"] = me;
                            Country c = Countries.Find(q.S("COL_CALL")); if (c != null) { if (!q.ContainsKey("COL_COUNTRY")) q["COL_COUNTRY"] = c.Name; if (!q.ContainsKey("COL_DXCC")) q["COL_DXCC"] = c.Dxcc.ToString(); if (!q.ContainsKey("COL_CONT")) q["COL_CONT"] = c.Cont; }
                            log.Insert(q); n++;
                            if (++k % 50 == 0) { say(k + " / " + recs.Count + " QSO traités (" + n + " ajoutés, " + dup + " doublons)", k * 100 / Math.Max(1, recs.Count)); System.Threading.Thread.Sleep(10); }
                        }
                        return n + " QSO importés, " + dup + " doublons ignorés" + (cancelled() ? " (annulé)" : "") + ".";
                    }, () => ReloadKeep());
                }
                catch (Exception ex) { Ui.Err(this, ex); }
            }
        }

        void ExportAdif(bool all = false, bool onlySel = false)
        {
            using (SaveFileDialog sd = new SaveFileDialog { Filter = Tr.T("ADIF (*.adi)|*.adi"), FileName = "HamRadioLink-" + DateTime.Now.ToString("yyyy-MM-dd") + ".adi" })
            {
                if (sd.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    Cursor = Cursors.WaitCursor; List<Qso> list = new List<Qso>(); List<Qso> sel = all ? new List<Qso>() : Selected();
                    if (sel.Count > 1 || (onlySel && sel.Count == 1)) list = sel; else for (int off = 0; ; off += 2000) { List<Qso> p = App.Log.Fetch(all ? "" : where, "COL_TIME_ON ASC", off, 2000); list.AddRange(p); if (p.Count < 2000) break; }
                    File.WriteAllText(sd.FileName, Adif.Export(list), new UTF8Encoding(false)); Cursor = Cursors.Default;
                    MessageBox.Show(this, (list.Count - Adif.Skipped) + " QSO exportés." + (Adif.Skipped > 0 ? "\r\n" + Adif.Skipped + Tr.T(" contact(s) défectueux ignoré(s).") : ""), Tr.T("Export ADIF"));
                }
                catch (Exception ex) { Cursor = Cursors.Default; Ui.Err(this, ex); }
            }
        }

        // Fichier .tq8 (signé par TQSL) pour LoTW, à déposer à la main : la sélection, sinon les QSO pas encore envoyés à LoTW ; une fois le fichier signé produit, ces QSO sont marqués « envoyés à LoTW ». LoTW n'accepte que des fichiers SIGNÉS par TQSL (.tq8) :
        // si TQSL et la « Station Location » sont réglés, le fichier est signé ici ; sinon on garde l'ADIF, à signer dans TQSL (File > Sign an ADIF file).
        void ExportLotw()
        {
            if (App.Log == null) return;
            List<Qso> sel = Selected(); bool useSel = sel.Count > 1;
            string wl = (where.Length > 0 ? "(" + where + Tr.T(") AND ") : "") + Tr.T("(COL_LOTW_QSL_SENT IS NULL OR COL_LOTW_QSL_SENT<>'Y')"); long cnt;
            try { cnt = useSel ? sel.Count : App.Log.Count(wl); } catch (Exception ex) { Ui.Err(this, ex); return; }
            if (cnt == 0) { MessageBox.Show(this, Tr.T("Aucun QSO à exporter."), "LoTW"); return; }
            string tqsl = App.Cfg.Get("lotw.tqsl", Tr.T("C:\\Program Files (x86)\\TrustedQSL\\tqsl.exe")), loc = App.Cfg.Get("lotw.location", "");
            if (!File.Exists(tqsl) || loc.Length == 0) { MessageBox.Show(this, Tr.T("Pour produire le fichier .tq8, réglez TQSL et le nom de la « Station Location » dans Outils > Online services."), Tr.T("Fichier LoTW")); return; }
            using (SaveFileDialog sd = new SaveFileDialog { Filter = Tr.T("Fichier LoTW signé (*.tq8)|*.tq8"), FileName = "LoTW-" + DateTime.Now.ToString("yyyy-MM-dd") + ".tq8", Title = Tr.T("Fichier LoTW : ") + cnt + " QSO (" + (useSel ? Tr.T("la sélection") : Tr.T("les QSO pas encore envoyés à LoTW")) + ")" })
            {
                if (sd.ShowDialog(this) != DialogResult.OK) return;
                string adi = Path.Combine(Path.GetTempPath(), "hrl-lotw-" + Guid.NewGuid().ToString("N") + ".adi"), tq8 = sd.FileName, tmp8 = Path.ChangeExtension(adi, ".tq8");   // TQSL ne sait pas écrire dans un dossier à accents : il signe dans le dossier temporaire, puis on copie
                // lecture, signature par TQSL et marquage : tout sur un autre fil (la télécommande et le journal restent utilisables)
                Bg.Run(this, Tr.T("Fichier LoTW (signature par TQSL)"), (log, db, say, cancelled) =>
                {
                    try
                    {
                        List<Qso> list = sel;
                        if (!useSel) { list = new List<Qso>(); say(Tr.T("Lecture des QSO…"), -1); long n = Math.Min(cnt, 100000); for (int off = 0; off < n; off += 2000) { List<Qso> p = log.Fetch(wl, "COL_TIME_ON ASC", off, 2000); list.AddRange(p); if (p.Count < 2000) break; } }
                        say(Tr.T("Signature par TQSL de ") + list.Count + " QSO (peut durer plusieurs minutes)…", -1);
                        File.WriteAllText(adi, Online.ExportOnline(list), new UTF8Encoding(false));
                        int code; string o = Online.RunTqsl(tqsl, Tr.T("-d -q -a all -l \"") + loc + "\" -o \"" + tmp8 + "\" \"" + adi + "\"", 600000, out code);
                        bool signed = code == 0 && File.Exists(tmp8) && new FileInfo(tmp8).Length > 100; if (signed) File.Copy(tmp8, tq8, true);
                        if (signed)
                        {
                            List<int> ids = new List<int>(); foreach (Qso q in list) ids.Add(q.Id); MarkDb(db, log, new List<string> { "lotw" }, ids, true, cancelled, say);
                            return list.Count + Tr.T(" QSO signés et marqués « envoyés à LoTW ».\r\n\r\nFichier à déposer sur https://lotw.arrl.org (Upload File) :\r\n") + tq8;
                        }
                        if (code == -1) o = Tr.T("délai dépassé (TQSL attend peut-être le mot de passe de votre certificat)");
                        string keep = Path.ChangeExtension(tq8, ".adi"); File.Copy(adi, keep, true);
                        return Tr.T("TQSL n'a pas produit le fichier .tq8 (code ") + code + (o.Trim().Length > 0 ? " : " + o.Trim() : "") + Tr.T("). Les QSO ne sont PAS marqués. ADIF gardé : ") + keep + " (TQSL > File > Sign an ADIF file).";
                    }
                    finally { try { File.Delete(adi); } catch { } try { File.Delete(tmp8); } catch { } }
                }, () => ReloadKeep());
            }
        }

        // Clic droit > Marquer la sélection comme envoyée / non envoyée à un service (corriger l'état à la main)
        // Mise à jour des champs « envoyé » (eQSL / LoTW) par petits paquets, avec une courte pause entre deux : le journal reste utilisable et les verrous sont brefs
        static void MarkDb(MySqlClient db, PeerLog lg, List<string> keys, List<int> ids, bool sent, Func<bool> cancelled, Action<string, int> say)
        {
            foreach (string k in keys)
            {
                string fld = k == "eqsl" ? "COL_EQSL_QSL_SENT" : "COL_LOTW_QSL_SENT", dt = k == "eqsl" ? "COL_EQSL_QSLSDATE" : "COL_LOTW_QSLSDATE";
                for (int i = 0; i < ids.Count && !cancelled(); i += 500)
                {
                    List<string> sid = new List<string>(); for (int j = i; j < Math.Min(i + 500, ids.Count); j++) sid.Add(ids[j].ToString()); string inl = string.Join(",", sid.ToArray());
                    if (sent) db.Exec("UPDATE " + lg.TableRef() + " SET " + fld + "='Y'," + dt + Tr.T("=NOW() WHERE COL_PRIMARY_KEY IN (") + inl + ")");
                    else { try { db.Exec("UPDATE " + lg.TableRef() + " SET " + fld + "='N'," + dt + Tr.T("=NULL WHERE COL_PRIMARY_KEY IN (") + inl + ")"); } catch { db.Exec("UPDATE " + lg.TableRef() + " SET " + fld + Tr.T("='N' WHERE COL_PRIMARY_KEY IN (") + inl + ")"); } }
                    if (say != null) say(Math.Min(i + 500, ids.Count) + " / " + ids.Count + " QSO (" + k + ")", Math.Min(i + 500, ids.Count) * 100 / Math.Max(1, ids.Count));
                    System.Threading.Thread.Sleep(15);
                }
            }
        }

        // UPDATE d'une colonne sur tous les QSO d'un filtre, par paquets de 500
        static long UpdateChunked(MySqlClient db, PeerLog lg, string setClause, string where, Func<bool> cancelled, Action<string, int> say)
        {
            MySqlResult r = db.Query("SELECT COL_PRIMARY_KEY FROM " + lg.TableRef() + (where.Length > 0 ? " WHERE " + where : "")); long n = 0; int tot = r.Rows.Count;
            for (int i = 0; i < tot && !cancelled(); i += 500)
            {
                List<string> sid = new List<string>(); for (int j = i; j < Math.Min(i + 500, tot); j++) sid.Add(r.Rows[j][0]);
                n += db.Exec("UPDATE " + lg.TableRef() + " SET " + setClause + " WHERE COL_PRIMARY_KEY IN (" + string.Join(",", sid.ToArray()) + ")");
                say(Math.Min(i + 500, tot) + " / " + tot + " QSO", Math.Min(i + 500, tot) * 100 / Math.Max(1, tot)); System.Threading.Thread.Sleep(15);
            }
            return n;
        }

        // Clic droit > Marquer la sélection comme envoyée / non envoyée à un service (corriger l'état à la main)
        void MarkSent(string svc, bool sent)
        {
            List<Qso> sel = Selected(); if (sel.Count == 0 || App.Log == null) return;
            try
            {
                List<int> ids = new List<int>(); foreach (Qso q in sel) ids.Add(q.Id); List<string> dbk = new List<string>();
                foreach (string k in svc == "all" ? new[] { "qrz", "clublog", "eqsl", "lotw", "hrdlog", "qrzcq", "hamqth", "wrl" } : new[] { svc })
                {
                    if (k == "qrz") { if (sent) QrzState.Mark(ids); else SentState.Unmark("qrz", ids); }
                    else if (k == "clublog") { if (sent) ClubLogState.Mark(ids); else SentState.Unmark("clublog", ids); }
                    else if (k == "hrdlog" || k == "qrzcq" || k == "hamqth" || k == "wrl") { if (sent) SentState.Mark(k, ids); else SentState.Unmark(k, ids); }
                    else dbk.Add(k);
                }
                if (dbk.Count == 0) { ReloadKeep(); return; }
                if (ids.Count <= 300) { MarkDb(App.Db, App.Log, dbk, ids, sent, () => false, null); ReloadKeep(); }
                else Bg.Run(this, Tr.T("Marquage de ") + ids.Count + " QSO", (log, db, say, cancelled) => { MarkDb(db, log, dbk, ids, sent, cancelled, say); return ids.Count + " QSO marqués" + (cancelled() ? " (annulé)" : "") + "."; }, () => ReloadKeep());
            }
            catch (Exception ex) { Ui.Err(this, ex); }
        }

        // QRZ.com refuse l'envoi automatique (souvent : pas d'abonnement) : on crée le fichier ADI des QSO restants, on explique, et on ouvre la page d'import du journal QRZ.com
        void QrzManual(SendSvc s, string err, List<Qso> rest)
        {
            try
            {
                string dir = Path.Combine(App.DataDir ?? ".", "exports"); Directory.CreateDirectory(dir);
                string file = Path.Combine(dir, "QRZ-" + DateTime.Now.ToString("yyyy-MM-dd_HHmm") + ".adi"); File.WriteAllText(file, Online.ExportOnline(rest), new UTF8Encoding(false)); List<int> qid = new List<int>(); foreach (Qso q in rest) qid.Add(q.Id); QrzState.Mark(qid); ReloadKeep();   // fichier préparé pour l'import = marqués « envoyés à QRZ.com »
                try { Clipboard.SetText(file); } catch { }   // chemin du fichier prêt à coller (Ctrl+V) dans la fenêtre « Ouvrir » du navigateur
                try { Process.Start(new ProcessStartInfo("https://logbook.qrz.com/logbook") { UseShellExecute = true }); } catch { }
                Form h = new Form(); h.Text = Tr.T("QRZ.com : envoi manuel"); h.FormBorderStyle = FormBorderStyle.FixedToolWindow; h.StartPosition = FormStartPosition.CenterParent; h.Font = Ui.Normal; h.ShowInTaskbar = false; h.TopMost = true;
                Label l = new Label(); l.AutoSize = false; l.SetBounds(12, 12, 500, 250);
                l.Text = Tr.T("QRZ.com n'a pas accepté l'envoi automatique :\r\n") + err + Tr.T("\r\n\r\nUn fichier ADI de ") + rest.Count + Tr.T(" QSO a été créé :\r\n") + file + Tr.T("\r\n\r\nLa page de votre journal QRZ.com s'est ouverte dans votre navigateur :\r\n1. Si vous n'êtes pas connecté à QRZ.com, connectez-vous.\r\n2. Cliquez sur « Import » (Upload / Import ADIF), puis sur le choix du fichier.\r\n3. Dans la fenêtre de choix du fichier, faites Ctrl+V (le chemin du fichier est déjà copié) puis Entrée.\r\n4. Validez l'import sur la page QRZ.com.\r\n\r\nCette fenêtre reste ouverte sans bloquer le programme.");
                h.Controls.Add(l); Button hb = new Button(); hb.Text = "OK"; hb.SetBounds(432, 272, 80, 28); hb.Click += (s2, e2) => h.Close(); h.Controls.Add(hb);
                Button ex2 = new Button(); ex2.Text = Tr.T("Voir le fichier"); ex2.SetBounds(300, 272, 120, 28); ex2.Click += (s2, e2) => { try { Process.Start("explorer.exe", "/select,\"" + file + "\""); } catch { } }; h.Controls.Add(ex2);
                h.ClientSize = new Size(524, 312); h.Show(this);
            }
            catch (Exception ex) { Ui.Err(this, ex); }
        }

        // Outils > Envoyer à tous les logs en ligne : QRZ.com, Club Log, eQSL, LoTW (ceux qui sont réglés), dans une fenêtre qui ne bloque pas le journal.
        // QSO envoyés : la sélection ; sans sélection, ceux pas encore marqués « envoyés » à eQSL / LoTW (ou, si aucun des deux n'est réglé, les 500 derniers de la liste).
        // Services en ligne réglés (clé / jeton / identifiants présents). auto = envoi automatique : pas de fenêtre d import manuel QRZ.
        internal List<SendSvc> BuildSvcs(string only, List<string> skipped, bool auto)
        {
            List<SendSvc> sv = new List<SendSvc>();
            if (only != null && only != "qrz") { } else if (App.Cfg.Get("qrzlog.key", "").Length > 0) sv.Add(new SendSvc { Key = "qrz", Name = Tr.T("QRZ.com Logbook"), Fn = Online.UploadQrz, Batch = 5, Mark = "qrz", OnFail = QrzManual });
            else if (only == "qrz") sv.Add(new SendSvc { Key = "qrz", Name = Tr.T("QRZ.com Logbook"), Fn = delegate(List<Qso> q) { throw new Exception(Tr.T("clé API QRZ.com Logbook non renseignée (Outils > Online services)")); }, Batch = 5, Mark = "qrz", OnFail = QrzManual });
            else skipped.Add("QRZ.com");
            if (only != null && only != "clublog") { } else if (App.Cfg.Get("clublog.token", "").Length > 0) sv.Add(new SendSvc { Key = "clublog", Name = Tr.T("Club Log"), Fn = Online.UploadClubLog, Batch = 100, Mark = "clublog" }); else skipped.Add(Tr.T("Club Log"));
            if (only != null && only != "hrdlog") { } else if (App.Cfg.Get("hrdlog.code", "").Length > 0) sv.Add(new SendSvc { Key = "hrdlog", Name = "HRDLog.net", Fn = Online.UploadHrdlog, Batch = 100, Mark = "hrdlog" }); else skipped.Add("HRDLog.net");
            if (only != null && only != "qrzcq") { } else if (App.Cfg.Get("qrzcq.key", "").Length > 0) sv.Add(new SendSvc { Key = "qrzcq", Name = "QRZCQ.com", Fn = Online.UploadQrzcq, Batch = 100, PauseMs = 61000, Mark = "qrzcq" }); else skipped.Add("QRZCQ.com");
            if (only != null && only != "hamqth") { } else if (App.Cfg.Get("hamqth.user", "").Length > 0 || App.Cfg.Get("lookup.user", "").Length > 0) sv.Add(new SendSvc { Key = "hamqth", Name = "HamQTH", Fn = Online.UploadHamqth, Batch = 20, Mark = "hamqth" }); else skipped.Add("HamQTH");
            if (only != null && only != "wrl") { } else if (App.Cfg.Get("wrl.key", "").Length > 0) sv.Add(new SendSvc { Key = "wrl", Name = "World Radio League", Fn = Online.UploadWrl, Batch = 100, Mark = "wrl" }); else skipped.Add("World Radio League");
            if (only != null && only != "eqsl") { } else if (App.Cfg.Get("eqsl.user", "").Length > 0) sv.Add(new SendSvc { Key = "eqsl", Name = "eQSL.cc", Fn = Online.UploadEqsl, Batch = 1, Mark = "eqsl" }); else skipped.Add("eQSL");
            if (only != null && only != "lotw") { } else if (File.Exists(App.Cfg.Get("lotw.tqsl", Tr.T("C:\\Program Files (x86)\\TrustedQSL\\tqsl.exe"))) && App.Cfg.Get("lotw.location", "").Length > 0) sv.Add(new SendSvc { Key = "lotw", Name = Tr.T("LoTW (TQSL)"), Fn = Online.UploadLotw, Batch = 100, Mark = "lotw" }); else skipped.Add("LoTW");
            if (auto) foreach (SendSvc x in sv) if (x.Key == "qrz") x.OnFail = null;
            return sv;
        }
        // Marque un lot comme envoyé à un service (eQSL / LoTW dans la base, les autres dans leur fichier d état).
        internal void MarkSentTo(SendSvc s, List<Qso> part)
        {
            if (s.Mark == null || part.Count == 0 || App.Log == null) return;
            List<int> ids = new List<int>(), no = new List<int>(); foreach (Qso q in part) { if (Online.Refused.Contains(q.Id)) no.Add(q.Id); else ids.Add(q.Id); }
            Online.Refused.Clear();
            if (no.Count > 0)   // 1.13 : envois refusés par le service = colonne « envoyé » à No
            {
                if (s.Mark == "eqsl" || s.Mark == "lotw") { List<string> nid = new List<string>(); foreach (int i in no) nid.Add(i.ToString()); App.Db.Exec("UPDATE " + App.Log.TableRef() + " SET " + (s.Mark == "eqsl" ? "COL_EQSL_QSL_SENT" : "COL_LOTW_QSL_SENT") + "='N' WHERE COL_PRIMARY_KEY IN (" + string.Join(",", nid.ToArray()) + ")"); }
                else SentState.Unmark(s.Mark, no);
            }
            if (ids.Count == 0) return;
            if (s.Mark == "clublog") { ClubLogState.Mark(ids); return; }
            if (s.Mark == "qrz") { QrzState.Mark(ids); return; }
            if (s.Mark == "hrdlog" || s.Mark == "qrzcq" || s.Mark == "hamqth" || s.Mark == "wrl") { SentState.Mark(s.Mark, ids); return; }
            string fld = s.Mark == "eqsl" ? "COL_EQSL_QSL_SENT" : "COL_LOTW_QSL_SENT", dt = s.Mark == "eqsl" ? "COL_EQSL_QSLSDATE" : "COL_LOTW_QSLSDATE"; List<string> sid = new List<string>(); foreach (int i in ids) sid.Add(i.ToString());
            App.Db.Exec("UPDATE " + App.Log.TableRef() + " SET " + fld + "='Y'," + dt + Tr.T("=NOW() WHERE COL_PRIMARY_KEY IN (") + string.Join(",", sid.ToArray()) + ")");
        }
        internal static bool IsSentTo(string key, Qso q)
        {
            if (key == "eqsl") return q.S("COL_EQSL_QSL_SENT") == "Y";
            if (key == "lotw") return q.S("COL_LOTW_QSL_SENT") == "Y";
            return SentState.IsSent(key, q.Id);
        }

        internal void AfterAutoSend() { try { ReloadKeep(); } catch { } }

        // « Envoyer à tous les logs en ligne » : fenêtre avec les services (coches) et la date de départ ; les QSO de cette date à aujourd'hui sont envoyés.
        void SendAllDialog()
        {
            if (App.Log == null) return;
            List<string> sk = new List<string>(); List<SendSvc> avail = BuildSvcs(null, sk, false);
            if (avail.Count == 0) { MessageBox.Show(this, Tr.T("Aucun service en ligne n'est réglé") + " : Outils > Online services.", "Envoi"); return; }
            SendAllDlg dlg = new SendAllDlg(avail, sk, Selected().Count);   // fenêtre non bloquante : le journal reste utilisable pendant le choix
            dlg.FormClosed += (s0, e0) => { if (dlg.DialogResult == DialogResult.OK) SendAllGo(dlg, avail); };
            Ui.ShowFree(dlg, this);
        }

        void SendAllGo(SendAllDlg d, List<SendSvc> avail)
        {
            List<Qso> list = new List<Qso>(); List<SendSvc> sv; bool skipSent;
            {
                sv = avail.FindAll(x => d.Keys.Contains(x.Key)); skipSent = d.SkipSent;
                try
                {
                    Cursor = Cursors.WaitCursor;
                    if (d.OnlySel) list = Selected();
                    else
                    {
                        string w = "COL_TIME_ON >= '" + d.From.ToString("yyyy-MM-dd") + " 00:00:00'";
                        long n = App.Log.Count(w); if (n > 50000) n = 50000;
                        for (int off = 0; off < n; off += 2000) { List<Qso> p = App.Log.Fetch(w, "COL_TIME_ON ASC", off, 2000); list.AddRange(p); if (p.Count < 2000) break; }
                    }
                }
                catch (Exception ex) { Cursor = Cursors.Default; Ui.Err(this, ex); return; }
                finally { Cursor = Cursors.Default; }
            }
            if (skipSent) foreach (SendSvc x in sv) { string k = x.Key; x.Skip = q => IsSentTo(k, q); }
            if (list.Count == 0) { MessageBox.Show(this, Tr.T("Aucun QSO à envoyer."), "Envoi"); return; }
            SendAllForm f = new SendAllForm(sv, list);
            f.BatchDone = delegate(SendSvc s, List<Qso> part) { try { MarkSentTo(s, part); } catch (Exception ex) { Ui.Err(this, ex); } };
            f.Finished = delegate(List<SendSvc> done) { try { ReloadKeep(); } catch { } };
            f.Show(this); f.Start();
        }

        void SendAllOnline(string only = null)
        {
            if (only == null) { SendAllDialog(); return; }
            if (App.Log == null) return;
            List<string> skipped = new List<string>(); List<SendSvc> sv = BuildSvcs(only, skipped, false);
            if (sv.Count == 0) { MessageBox.Show(this, (only != null ? Tr.T("Ce service n'est pas réglé") : Tr.T("Aucun service en ligne n'est réglé")) + " : Outils > Online services.", "Envoi"); return; }
            List<Qso> list = Selected(); string what = Tr.T("les ") + list.Count + " QSO sélectionnés";
            try
            {
                Cursor = Cursors.WaitCursor;
                if (list.Count == 0)
                {
                    List<string> c = new List<string>(); foreach (SendSvc s in sv) if (s.Mark == "eqsl") c.Add(Tr.T("(COL_EQSL_QSL_SENT IS NULL OR COL_EQSL_QSL_SENT<>'Y')")); else if (s.Mark == "lotw") c.Add(Tr.T("(COL_LOTW_QSL_SENT IS NULL OR COL_LOTW_QSL_SENT<>'Y')"));
                    if (c.Count == 0) { list = App.Log.Fetch(where, "COL_TIME_ON DESC", 0, 500); what = Tr.T("les ") + list.Count + " derniers QSO de la liste"; }
                    else
                    {
                        string w = (where.Length > 0 ? "(" + where + Tr.T(") AND ") : "") + "(" + string.Join(" OR ", c.ToArray()) + ")";
                        long n = App.Log.Count(w); if (n > 50000) n = 50000;
                        for (int off = 0; off < n; off += 2000) { List<Qso> p = App.Log.Fetch(w, "COL_TIME_ON ASC", off, 2000); list.AddRange(p); if (p.Count < 2000) break; }
                        what = Tr.T("les ") + list.Count + Tr.T(" QSO pas encore envoyés à eQSL / LoTW");
                    }
                }
            }
            catch (Exception ex) { Cursor = Cursors.Default; Ui.Err(this, ex); return; }
            finally { Cursor = Cursors.Default; }
            if (list.Count == 0) { MessageBox.Show(this, Tr.T("Aucun QSO à envoyer."), "Envoi"); return; }
            List<string> names = new List<string>(); foreach (SendSvc s in sv) names.Add(s.Name);
            if (!Ui.Ask(this, Tr.T("Envoyer ") + what + " à : " + string.Join(", ", names.ToArray()) + " ?" + (skipped.Count > 0 ? "\r\n\r\n(non réglés, ignorés : " + string.Join(", ", skipped.ToArray()) + ")" : ""))) return;
            SendAllForm f = new SendAllForm(sv, list);
            // chaque lot envoyé est marqué tout de suite (eQSL / LoTW dans la base, Club Log dans son fichier) : une interruption laisse un état exact
            f.BatchDone = delegate(SendSvc s, List<Qso> part)
            {
                try { MarkSentTo(s, part); } catch (Exception ex) { Ui.Err(this, ex); }
            };
            f.Finished = delegate(List<SendSvc> done) { try { ReloadKeep(); } catch { } };            f.Show(this); f.Start();
        }
        void FindDupes()
        {
            try
            {
                MySqlResult r = App.Db.Query("SELECT COL_CALL, COL_TIME_ON, COL_BAND, COUNT(*) c FROM " + App.Log.TableRef() + " GROUP BY COL_CALL, COL_TIME_ON, COL_BAND HAVING c>1 ORDER BY COL_TIME_ON DESC LIMIT 200");
                if (r.Rows.Count == 0) { MessageBox.Show(this, Tr.T("Aucun doublon (même indicatif, même heure, même bande)."), "Doublons"); return; }
                if (Ui.Ask(this, r.Rows.Count + " groupes de doublons. Les afficher dans la liste ?")) SetFilter(Tr.T("(COL_CALL, COL_TIME_ON, COL_BAND) IN (SELECT COL_CALL, COL_TIME_ON, COL_BAND FROM ") + App.Log.TableRef() + " GROUP BY COL_CALL, COL_TIME_ON, COL_BAND HAVING COUNT(*)>1)");
            }
            catch (Exception ex) { Ui.Err(this, ex); }
        }

        // Complète pays / continent / DXCC / zones des QSO où ils manquent, d'après la table des pays
        void UpdateFromCountries()
        {
            if (!Countries.Loaded) { MessageBox.Show(this, Tr.T("Table des pays absente (fichier Countries.txt dans le dossier data/journal)."), "Pays"); return; }
            if (!Ui.Ask(this, Tr.T("Compléter les QSO sans pays / DXCC d'après l'indicatif ?"))) return;
            Bg.Run(this, Tr.T("Compléter les pays"), (log, db, say, cancelled) =>
            {
                int n = 0, k = 0; MySqlResult r = db.Query("SELECT COL_PRIMARY_KEY, COL_CALL FROM " + log.TableRef() + " WHERE (COL_COUNTRY IS NULL OR COL_COUNTRY='') ");
                foreach (string[] row in r.Rows)
                {
                    if (cancelled()) break; k++;
                    Country c = Countries.Find(row[1]); if (c == null) continue;
                    db.Exec("UPDATE " + log.TableRef() + " SET COL_COUNTRY=" + MySqlClient.Quote(c.Name) + ",COL_DXCC=" + MySqlClient.Quote(c.Dxcc.ToString()) + ",COL_CONT=" + MySqlClient.Quote(c.Cont) + ",COL_CQZ=" + c.Cq + ",COL_ITUZ=" + c.Itu + " WHERE COL_PRIMARY_KEY=" + row[0]); n++;
                    if (k % 100 == 0) say(k + " / " + r.Rows.Count + " QSO examinés (" + n + " complétés)", k * 100 / Math.Max(1, r.Rows.Count));
                }
                return n + " QSO complétés" + (cancelled() ? " (annulé)" : "") + ".";
            }, () => ReloadKeep());
        }

        void ShowAwards(int tab) { AwardsForm f = new AwardsForm(); f.Show(this); }

        void Stats()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(Tr.T("QSO : ") + App.Log.Count(""));
                sb.AppendLine(Tr.T("Pays (DXCC) : ") + App.Db.Scalar("SELECT COUNT(DISTINCT COL_DXCC) FROM " + App.Log.TableRef() + " WHERE COL_DXCC<>'' AND COL_DXCC<>'0'"));
                sb.AppendLine(Tr.T("Indicatifs : ") + App.Db.Scalar("SELECT COUNT(DISTINCT COL_CALL) FROM " + App.Log.TableRef()));
                sb.AppendLine().AppendLine(Tr.T("Par bande :"));
                foreach (string[] r in App.Db.Query("SELECT COL_BAND, COUNT(*) FROM " + App.Log.TableRef() + " GROUP BY COL_BAND ORDER BY 2 DESC LIMIT 12").Rows) sb.AppendLine("  " + r[0] + " : " + r[1]);
                sb.AppendLine().AppendLine(Tr.T("Par mode :"));
                foreach (string[] r in App.Db.Query("SELECT COL_MODE, COUNT(*) FROM " + App.Log.TableRef() + " GROUP BY COL_MODE ORDER BY 2 DESC LIMIT 12").Rows) sb.AppendLine("  " + r[0] + " : " + r[1]);
                sb.AppendLine().AppendLine(Tr.T("Par année :"));
                foreach (string[] r in App.Db.Query("SELECT YEAR(COL_TIME_ON), COUNT(*) FROM " + App.Log.TableRef() + " GROUP BY 1 ORDER BY 1 DESC LIMIT 15").Rows) sb.AppendLine("  " + r[0] + " : " + r[1]);
                MessageBox.Show(this, sb.ToString(), Tr.T("Statistiques"));
            }
            catch (Exception ex) { Ui.Err(this, ex); }
        }
    }

    // Suivi des diplômes : DXCC par bande, WAZ (zones CQ), WAS (états US), WAC (continents), IOTA
    public class AwardsForm : Form
    {
        readonly TabControl tabs = new TabControl { Dock = DockStyle.Fill };
        public AwardsForm()
        {
            Text = "Awards Tracking"; Size = new Size(1100, 720); StartPosition = FormStartPosition.CenterParent; Font = Ui.Normal; BackColor = Theme.Get("lb.back");
            Controls.Add(tabs);
            try { Dxcc(); Simple(Tr.T("WAZ (zones CQ)"), "COL_CQZ", 40, Tr.T("CQ zone")); Simple(Tr.T("WAC (continents)"), "COL_CONT", 6, "Continent"); Was(); Simple("IOTA", "COL_IOTA", 0, Tr.T("Référence IOTA")); }
            catch (Exception ex) { Ui.Err(this, ex); }
        }

        static readonly Color Green = Color.FromArgb(46, 170, 96), Amber = Color.FromArgb(235, 150, 40);
        // Légende en couleurs : confirmé / contacté / pas encore
        static Control Legend()
        {
            Panel p = new Panel { Dock = DockStyle.Top, Height = 26 };
            p.Paint += (s, e) =>
            {
                Graphics g = e.Graphics; g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; int x = 8; Color tt = Theme.Get("lb.tool.text");
                foreach (KeyValuePair<Color, string> k in new[] { new KeyValuePair<Color, string>(Green, "Confirmé"), new KeyValuePair<Color, string>(Amber, "Contacté"), new KeyValuePair<Color, string>(Color.FromArgb(70, 74, 100), Tr.T("Pas encore")) })
                {
                    using (SolidBrush b = new SolidBrush(k.Key)) g.FillEllipse(b, x, 6, 14, 14); TextRenderer.DrawText(g, k.Value, p.Font, new Point(x + 20, 4), tt); x += 30 + TextRenderer.MeasureText(k.Value, p.Font).Width + 14;
                }
            };
            return p;
        }
        const string Conf = "(COL_QSL_RCVD='Y' OR COL_LOTW_QSL_RCVD='Y' OR COL_EQSL_QSL_RCVD='Y')";
        static readonly string[] Bands = { "160m", "80m", "60m", "40m", "30m", "20m", "17m", "15m", "12m", "10m", "6m" };

        void Dxcc()
        {
            TabPage tp = new TabPage("DXCC") { BackColor = Theme.Get("lb.back") }; tabs.TabPages.Add(tp);
            DataGridView g = NewGrid(); tp.Controls.Add(g);
            Label top = new Label { Dock = DockStyle.Top, Height = 24, Font = new Font(Ui.Normal, FontStyle.Bold) }; tp.Controls.Add(top); tp.Controls.Add(Legend());
            g.Columns.Add("dxcc", "DXCC"); g.Columns.Add("name", "Pays"); foreach (string b in Bands) g.Columns.Add(b, b); g.Columns.Add("tot", "Total");
            g.Columns[1].Width = 220; g.Columns[0].Width = 50;
            Dictionary<string, Dictionary<string, string>> res = new Dictionary<string, Dictionary<string, string>>();
            foreach (string[] r in App.Db.Query("SELECT COL_DXCC, COL_BAND, MAX(" + Conf + Tr.T(") c FROM ") + App.Log.TableRef() + " WHERE COL_DXCC<>'' AND COL_DXCC<>'0' GROUP BY COL_DXCC, COL_BAND").Rows)
            { if (r[0] == null) continue; string bnd = r[1] ?? "?"; if (!res.ContainsKey(r[0])) res[r[0]] = new Dictionary<string, string>(); string prev; bool isC = r[2] == "1"; if (!res[r[0]].TryGetValue(bnd, out prev) || isC) res[r[0]][bnd] = isC ? "C" : "W"; }
            int worked = 0, conf = 0;
            List<Country> list = new List<Country>(Countries.All); list.Sort((a, b) => { int wa = res.ContainsKey(a.Dxcc.ToString()) || res.ContainsKey(a.Dxcc.ToString("000")) ? 0 : 1, wb = res.ContainsKey(b.Dxcc.ToString()) || res.ContainsKey(b.Dxcc.ToString("000")) ? 0 : 1; return wa != wb ? wa.CompareTo(wb) : a.Dxcc.CompareTo(b.Dxcc); });   // pays contactés en premier
            foreach (Country c in list)
            {
                if (c.Deleted) continue; Dictionary<string, string> d; string key = c.Dxcc.ToString("000"); string k2 = c.Dxcc.ToString();
                if (!res.TryGetValue(k2, out d) && !res.TryGetValue(key, out d)) { g.Rows.Add(k2, c.Name); g.Rows[g.Rows.Count - 1].DefaultCellStyle.ForeColor = Color.Gray; continue; }
                object[] row = new object[Bands.Length + 3]; row[0] = k2; row[1] = c.Name; bool anyC = false;
                for (int i = 0; i < Bands.Length; i++) { string v; row[i + 2] = d.TryGetValue(Bands[i], out v) ? v : ""; if (v == "C") anyC = true; }
                row[Bands.Length + 2] = d.Count; g.Rows.Add(row); worked++; if (anyC) conf++;
                DataGridViewRow rw = g.Rows[g.Rows.Count - 1]; rw.Cells[1].Style.Font = new Font(g.Font, FontStyle.Bold); rw.Cells[1].Style.ForeColor = anyC ? Green : Amber;
                for (int i = 0; i < Bands.Length; i++) { DataGridViewCell cell = rw.Cells[i + 2]; string v = cell.Value as string; if (v != "C" && v != "W") continue; Color fill = v == "C" ? Green : Amber; cell.Style.BackColor = fill; cell.Style.ForeColor = fill; cell.Style.SelectionBackColor = fill; cell.Style.SelectionForeColor = fill; cell.ToolTipText = v == "C" ? "Confirmé" : "Contacté"; }
            }
            top.Text = Tr.T("  DXCC : ") + worked + " contactés, " + conf + Tr.T(" confirmés (QSL / LoTW / eQSL)  —  entités actuelles : ") + list.FindAll(x => !x.Deleted).Count;
        }

        void Simple(string title, string col, int goal, string head)
        {
            TabPage tp = new TabPage(title) { BackColor = Theme.Get("lb.back") }; tabs.TabPages.Add(tp); DataGridView g = NewGrid(); tp.Controls.Add(g);
            Label top = new Label { Dock = DockStyle.Top, Height = 24, Font = new Font(Ui.Normal, FontStyle.Bold) }; tp.Controls.Add(top);
            g.Columns.Add("v", head); g.Columns.Add("w", "QSO"); g.Columns.Add("c", "Confirmés"); int worked = 0, conf = 0;
            foreach (string[] r in App.Db.Query("SELECT " + col + Tr.T(", COUNT(*), SUM(") + Conf + Tr.T(") FROM ") + App.Log.TableRef() + " WHERE " + col + " IS NOT NULL AND " + col + "<>'' AND " + col + "<>'0' GROUP BY " + col + " ORDER BY " + (col == "COL_CQZ" ? Tr.T("CAST(") + col + " AS UNSIGNED)" : col)).Rows)
            { g.Rows.Add(r[0], r[1], r[2]); worked++; if (r[2] != "0" && r[2] != null) conf++; }
            top.Text = "  " + title + " : " + worked + (goal > 0 ? " / " + goal : "") + " contactés, " + conf + " confirmés";
        }

        void Was()
        {
            TabPage tp = new TabPage(Tr.T("WAS (états US)")) { BackColor = Theme.Get("lb.back") }; tabs.TabPages.Add(tp); DataGridView g = NewGrid(); tp.Controls.Add(g);
            Label top = new Label { Dock = DockStyle.Top, Height = 24, Font = new Font(Ui.Normal, FontStyle.Bold) }; tp.Controls.Add(top);
            g.Columns.Add("v", "État"); g.Columns.Add("w", "QSO"); g.Columns.Add("c", "Confirmés"); int worked = 0, conf = 0;
            foreach (string[] r in App.Db.Query("SELECT COL_STATE, COUNT(*), SUM(" + Conf + Tr.T(") FROM ") + App.Log.TableRef() + " WHERE COL_DXCC IN ('291','6','110') AND COL_STATE<>'' GROUP BY COL_STATE ORDER BY 1").Rows)
            { g.Rows.Add(r[0], r[1], r[2]); worked++; if (r[2] != "0") conf++; }
            top.Text = Tr.T("  WAS : ") + worked + " / 50 contactés, " + conf + " confirmés";
        }

        static DataGridView NewGrid()
        {
            DataGridView g = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false, BackgroundColor = Theme.Get("lb.grid.back"), GridColor = Theme.Get("lb.grid.line"), BorderStyle = BorderStyle.None, EnableHeadersVisualStyles = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells, Font = Ui.Mono };
            DataGridViewCellStyle d = g.DefaultCellStyle; d.BackColor = Theme.Get("lb.grid.back"); d.ForeColor = Theme.Get("lb.grid.text"); d.SelectionBackColor = Theme.Get("lb.grid.sel"); d.SelectionForeColor = Theme.Get("lb.grid.seltext");
            DataGridViewCellStyle h = g.ColumnHeadersDefaultCellStyle; h.BackColor = Theme.Get("lb.head.back"); h.ForeColor = Theme.Get("lb.head.text"); h.SelectionBackColor = h.BackColor; h.SelectionForeColor = h.ForeColor;
            return g;
        }
    }

}
