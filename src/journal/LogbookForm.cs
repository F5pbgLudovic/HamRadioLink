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
                new ColDef("QSO date", "COL_TIME_ON", 90, true), new ColDef("Time on", "COL_TIME_ON", 80, true), new ColDef("Call", "COL_CALL", 110, true), new ColDef("Locator", "COL_GRIDSQUARE", 70, true),
                new ColDef("Name", "COL_NAME", 130, true), new ColDef("QTH", "COL_QTH", 90, true), new ColDef("Freq", "COL_FREQ", 90, true, true), new ColDef("Mode", "COL_MODE", 60, true),
                new ColDef("Distance", "COL_DISTANCE", 70, true, true), new ColDef("Sent", "COL_RST_SENT", 50, true), new ColDef("Rcvd", "COL_RST_RCVD", 50, true),
                new ColDef("eQSL sent", "COL_EQSL_QSL_SENT", 80, true), new ColDef("LOTW sent", "COL_LOTW_QSL_SENT", 80, true), new ColDef("Club Log sent", "COL_CLUBLOG_LOCAL", 90, true), new ColDef("QRZ.com sent", "COL_QRZ_LOCAL", 90, true), new ColDef("Country", "COL_COUNTRY", 150, true), new ColDef("Comment", "COL_COMMENT", 300, true),
                new ColDef("Band", "COL_BAND", 50, false), new ColDef("Submode", "COL_SUBMODE", 70, false), new ColDef("Time off", "COL_TIME_OFF", 80, false), new ColDef("State", "COL_STATE", 60, false),
                new ColDef("Continent", "COL_CONT", 40, false), new ColDef("DXCC", "COL_DXCC", 50, false), new ColDef("CQ", "COL_CQZ", 40, false, true), new ColDef("ITU", "COL_ITUZ", 40, false, true),
                new ColDef("IOTA", "COL_IOTA", 60, false), new ColDef("QSL sent", "COL_QSL_SENT", 70, false), new ColDef("QSL rcvd", "COL_QSL_RCVD", 70, false), new ColDef("LOTW rcvd", "COL_LOTW_QSL_RCVD", 80, false),
                new ColDef("eQSL rcvd", "COL_EQSL_QSL_RCVD", 80, false), new ColDef("QSL via", "COL_QSL_VIA", 80, false), new ColDef("Station", "COL_STATION_CALLSIGN", 90, false), new ColDef("Operator", "COL_OPERATOR", 90, false),
                new ColDef("Prefix", "COL_PFX", 60, false), new ColDef("Contest", "COL_CONTEST_ID", 90, false), new ColDef("Notes", "COL_NOTES", 200, false), new ColDef("E-mail", "COL_EMAIL", 150, false),
                new ColDef("Power", "COL_TX_PWR", 50, false, true), new ColDef("Satellite", "COL_SAT_NAME", 80, false), new ColDef("Rig", "COL_MY_RIG", 100, false) };
        }

        public LogbookForm()
        {
            Text = "HamRadioLink " + App.Version + " Logbook - [" + App.Cfg.Get("db.name", "F5PBG") + "]";
            Size = new Size(1300, 860); Font = Ui.Normal; StartPosition = FormStartPosition.CenterScreen; Icon = Ui.AppIcon; Ui.RememberWindow(this, "lb");
            cols = AllCols();
            string vis = App.Cfg.Get("lb.cols", "");
            if (vis.Length > 0) { List<string> v = new List<string>(vis.Split(',')); foreach (ColDef c in cols) c.Visible = v.Contains(c.Title); if (App.Cfg.Get("lb.clcol", "") != "1") { foreach (ColDef c in cols) if (c.Title == "Club Log sent") c.Visible = true; App.Cfg.Set("lb.clcol", "1"); }
            if (App.Cfg.Get("lb.qrzcol", "") != "1") { foreach (ColDef c in cols) if (c.Title == "QRZ.com sent") c.Visible = true; App.Cfg.Set("lb.qrzcol", "1"); } }

            MenuStrip ms = new ClickMenuStrip();
            ToolStripMenuItem file = M("File", M("Fenêtre de télécommande", (s, e) => { Action a = Hub.ShowMain; if (a != null) a(); }), new ToolStripSeparator(), M("Open database...", (s, e) => OpenDb()), M("Importer / partager une base...", (s, e) => OpenSetup()), M("Réglages des ports...", (s, e) => { using (PortsForm f = new PortsForm()) f.ShowDialog(this); }), M("Moteur MariaDB...",(s, e) => { using (EngineForm f = new EngineForm()) f.ShowDialog(this); }), M("New log...", (s, e) => NewLog("")), M("Import ADIF...", (s, e) => ImportAdif()), M("Export ADIF (selection or all)...", (s, e) => ExportAdif()), new ToolStripSeparator(), M("Exit", (s, e) => Close()));
            ToolStripMenuItem edit = M("Edit", M("Add\tCtrl+A", (s, e) => AddQso()), M("View / Edit\tEnter", (s, e) => EditSelected()), M("Delete\tDel", (s, e) => DeleteSelected()), new ToolStripSeparator(), M("Edit selections...", (s, e) => EditSelections()), M("Copy", (s, e) => CopySel()));
            ToolStripMenuItem view = M("View", M("Refresh\tF5", (s, e) => Reload()), M("Show all", (s, e) => { where = ""; search.Text = ""; Reload(); }), M("Columns (Layout)...", (s, e) => ChooseColumns()));
            ToolStripMenuItem logb = M("Logbook", M("Awards tracking...", (s, e) => ShowAwards(0)), M("Statistics...", (s, e) => Stats()), M("Find duplicates...", (s, e) => FindDupes()), M("QSL à envoyer (filtre)", (s, e) => SetFilter("COL_QSL_SENT='Y' AND COL_QSL_RCVD<>'Y'")), M("Backup (ADIF)...", (s, e) => ExportAdif(true)), M("Update countries from QRZ/HamQTH data", (s, e) => UpdateFromCountries()));
            ToolStripMenuItem tools = M("Tools", M("Vérifier les distances", (s, e) => CheckDistances()), M("Online services...", (s, e) => { using (OnlineForm f = new OnlineForm()) f.ShowDialog(this); }), M("Envoyer à tous les logs en ligne...", (s, e) => SendAllOnline()), M("Exporter pour LoTW (fichier à importer à la main)...", (s, e) => ExportLotw()),
                new ToolStripSeparator(), M("Upload selection to QRZ.com Logbook", (s, e) => SendAllOnline("qrz")),
                M("Upload selection to Club Log", (s, e) => SendAllOnline("clublog")), M("Upload selection to eQSL.cc", (s, e) => SendAllOnline("eqsl")), M("Upload selection to LoTW (TQSL)", (s, e) => SendAllOnline("lotw")),
                new ToolStripSeparator());
            ToolStripMenuItem conf = M("Configuration", M("Station...", (s, e) => { using (StationForm f = new StationForm()) f.ShowDialog(this); }), M("DX Cluster...", (s, e) => { using (ClusterForm f = new ClusterForm()) f.ShowDialog(this); }), M("Importer / partager...", (s, e) => OpenSetup()), M("Sauvegarde (ADIF)...", (s, e) => ExportAdif(true)), M("Base...", (s, e) => OpenDb()), M("Ports...", (s, e) => { using (PortsForm f = new PortsForm()) f.ShowDialog(this); }));
            ToolStripMenuItem themes = M("Thèmes"); BuildThemeMenu(themes);
            ToolStripMenuItem help = M("Help", M("About", (s, e) => MessageBox.Show(this, "HamRadioLink " + App.Version + "\r\nJournal MariaDB.", "About")));
            ms.Items.AddRange(new ToolStripItem[] { file, edit, view, logb, tools, conf, themes, help });
            MainMenuStrip = ms; menuStrip = ms;

            ToolStrip ts = new ClickToolStrip { GripStyle = ToolStripGripStyle.Hidden, ImageScalingSize = new Size(1, 1) };
            ts.Items.Add(B("Add", (s, e) => AddQso())); ts.Items.Add(B("Delete", (s, e) => DeleteSelected()));
            ts.Items.Add(new ToolStripSeparator());
            ts.Items.Add(B("QRZ.com", (s, e) => OpenQrz())); ts.Items.Add(B("Awards Tracking", (s, e) => ShowAwards(0)));
            ts.Items.Add(new ToolStripSeparator());
            search = new TextBox { Width = 130 }; ts.Items.Add(new ToolStripControlHost(search));
            search.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { SetFilter(PeerLog.QuickWhere(search.Text)); e.SuppressKeyPress = true; } };
            ts.Items.Add(B("Filter", (s, e) => AdvancedFilter())); ts.Items.Add(B("All", (s, e) => { search.Text = ""; SetFilter(""); }));
            ts.Items.Add(B("▲", (s, e) => { if (grid.RowCount > 0) { grid.FirstDisplayedScrollingRowIndex = 0; foreach (DataGridViewColumn c in grid.Columns) if (c.Visible) { grid.CurrentCell = grid[c.Index, 0]; break; } grid.ClearSelection(); grid.Rows[0].Selected = true; } }));   // remonter tout en haut

            grid = new DataGridView
            {
                Dock = DockStyle.Fill, VirtualMode = true, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToOrderColumns = true,
                RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = true,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None, AllowUserToResizeRows = false, RowTemplate = { Height = 20 },
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
            };
            grid.DefaultCellStyle.Font = Ui.Mono;
            BuildGridColumns();
            grid.CellValueNeeded += GridValue; grid.CellFormatting += GridFormat;
            grid.ColumnHeaderMouseClick += (s, e) => SortBy(e.ColumnIndex);
            grid.CellMouseDown += (s, e) =>
            {
                if (e.Button != MouseButtons.Right || e.RowIndex < 0) return;
                if (!grid.Rows[e.RowIndex].Selected) { grid.ClearSelection(); grid.Rows[e.RowIndex].Selected = true; grid.CurrentCell = grid[Math.Max(0, e.ColumnIndex), e.RowIndex]; }
                ContextMenuStrip m = new ContextMenuStrip(); m.Items.Add("Supprimer le contact", null, (a, b) => DeleteSelected()); m.Items.Add(new ToolStripSeparator());
                m.Items.Add("Exporter la sélection (ADIF)...", null, (a, b) => ExportAdif(false, true)); m.Items.Add("Exporter la sélection pour LoTW...", null, (a, b) => ExportLotw());
                ToolStripMenuItem snd = new ToolStripMenuItem("Envoyer la sélection à");
                snd.DropDownItems.Add("QRZ.com", null, (a, b) => SendAllOnline("qrz")); snd.DropDownItems.Add("Club Log", null, (a, b) => SendAllOnline("clublog"));
                snd.DropDownItems.Add("eQSL.cc", null, (a, b) => SendAllOnline("eqsl")); snd.DropDownItems.Add("LoTW (TQSL)", null, (a, b) => SendAllOnline("lotw"));
                snd.DropDownItems.Add(new ToolStripSeparator()); snd.DropDownItems.Add("Tous les logs en ligne...", null, (a, b) => SendAllOnline());
                m.Items.Add(snd);
                m.Items.Add(new ToolStripSeparator());
                string[] ks = { "qrz", "clublog", "eqsl", "lotw", "all" }, ns = { "QRZ.com", "Club Log", "eQSL.cc", "LoTW", "Tous les services" };
                ToolStripMenuItem yes = new ToolStripMenuItem("Marquer la sélection comme envoyée à"), no = new ToolStripMenuItem("Marquer la sélection comme NON envoyée à");
                for (int i = 0; i < ks.Length; i++) { string k = ks[i]; yes.DropDownItems.Add(ns[i], null, (a, b) => MarkSent(k, true)); no.DropDownItems.Add(ns[i], null, (a, b) => MarkSent(k, false)); }
                m.Items.Add(yes); m.Items.Add(no); m.Show(grid, grid.PointToClient(Cursor.Position));
            };
            grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) EditSelected(); };
            grid.ColumnWidthChanged += (s, e) => { ColDef c = e.Column.Tag as ColDef; if (c != null) c.Width = e.Column.Width; };
            grid.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { EditSelected(); e.Handled = true; }
                else if (e.KeyCode == Keys.Delete) { DeleteSelected(); e.Handled = true; }
                else if (e.KeyCode == Keys.F5) { Reload(); e.Handled = true; }
                else if (e.Control && e.KeyCode == Keys.A) { AddQso(); e.Handled = true; }
            };

            StatusStrip status = new StatusStrip();
            lblFilter = new ToolStripStatusLabel("All") { BorderSides = ToolStripStatusLabelBorderSides.All };
            lblDb = new ToolStripStatusLabel("MySQL") { BorderSides = ToolStripStatusLabelBorderSides.All };
            lblCount = new ToolStripStatusLabel("0") { BorderSides = ToolStripStatusLabelBorderSides.All, Spring = true, TextAlign = ContentAlignment.MiddleRight };
            status.Items.AddRange(new ToolStripItem[] { lblFilter, lblDb, lblCount });

            logTabs = new LogTabs(); logTabs.TabSelected += SwitchLog; logTabs.AddClicked += () => NewLog(""); logTabs.RenameRequested += RenameLog; logTabs.RemoveRequested += RemoveLog; logTabs.DeleteRequested += DeleteLog;
            strip = new ClusterStrip(); frames = new ClusterFrames();
            strip.Width = Math.Max(120, App.Cfg.GetInt("cl.w", 200)); frames.Height = Math.Max(50, App.Cfg.GetInt("cl.h", 120));
            splitS = new Splitter { Dock = DockStyle.Right, Width = 4, MinSize = 120, MinExtra = 300, BackColor = Color.Black }; splitF = new Splitter { Dock = DockStyle.Bottom, Height = 4, MinSize = 50, MinExtra = 100, BackColor = Color.Black };
            splitS.SplitterMoved += (s, e) => { App.Cfg.Set("cl.w", strip.Width.ToString()); App.Cfg.Save(); }; splitF.SplitterMoved += (s, e) => { App.Cfg.Set("cl.h", frames.Height.ToString()); App.Cfg.Save(); };
            Controls.Add(grid); Controls.Add(splitF); Controls.Add(frames); Controls.Add(splitS); Controls.Add(strip); Controls.Add(logTabs); Controls.Add(ms); Controls.Add(status);
            toolStrip = ts; statusBar = status; grid.ColumnDisplayIndexChanged += (s, e) => FixLast(); ApplyLanguage(); ApplyTheme(); Theme.Changed += ApplyTheme; FormClosed += (s, e) => Theme.Changed -= ApplyTheme;
            FormClosing += (s, e) => { SaveCols(); };
            DxCluster.SpotClicked += OnSpotClicked; FormClosed += (s, e) => DxCluster.SpotClicked -= OnSpotClicked;
            ApplyCluster(); Shown += (s, e) => { DxCluster.AutoStart(); if (App.Log == null) OpenDb(); else Reload(); };
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
            double la, lo; if (!Geo.MyPos(out la, out lo)) { MessageBox.Show(this, "Renseignez d'abord votre locator : Configuration > Station.", "Distances"); return; }
            Bg.Run(this, "Vérifier les distances", (log, db, say, cancelled) =>
            {
                int done = 0, last = 0;
                while (!cancelled())
                {
                    List<Qso> l = log.Fetch("(COL_DISTANCE IS NULL OR COL_DISTANCE=0) AND COL_GRIDSQUARE<>'' AND COL_PRIMARY_KEY>" + last, "COL_PRIMARY_KEY ASC", 0, 500);
                    if (l.Count == 0) break;
                    foreach (Qso q in l) { last = Math.Max(last, q.Id); if (Geo.FillDistance(q)) { Qso c = new Qso(); c["COL_DISTANCE"] = q.S("COL_DISTANCE"); log.Update(q.Id, c); done++; } }
                    say(done + " distance(s) ajoutée(s)…", -1); System.Threading.Thread.Sleep(10);
                }
                return done + " distance(s) ajoutée(s)" + (cancelled() ? " (annulé)" : "") + ".";
            }, () => ReloadKeep());
        }

        MenuStrip menuStrip; ToolStrip toolStrip;
        StatusStrip statusBar; LogTabs logTabs;

        // ---------- onglets : plusieurs journaux ----------
        void SwitchLog(LogRef r)
        {
            if (r == App.CurrentLog) return; LogRef before = App.CurrentLog;
            try { Cursor = Cursors.WaitCursor; App.ConnectTo(r); where = ""; if (search != null) search.Text = ""; Text = "HamRadioLink " + App.Version + " Logbook - [" + r.Name + "]"; Reload(); logTabs.Invalidate(); }
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
                        Busy.Run(this, Ui.French ? "Création du journal" : "Creating the log", say =>
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
            using (Form f = new Form { Text = Ui.French ? "Renommer le journal" : "Rename the log", ClientSize = new Size(340, 90), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, Font = Ui.Normal })
            {
                TextBox t = Ui.Txt(f, 12, 14, 316); t.Text = l.Name; Button ok = Ui.Btn(f, "OK", 160, 48, 80, 28, (s, e) => f.DialogResult = DialogResult.OK); Ui.Btn(f, Ui.French ? "Annuler" : "Cancel", 248, 48, 80, 28, (s, e) => f.DialogResult = DialogResult.Cancel); f.AcceptButton = ok;
                if (f.ShowDialog(this) != DialogResult.OK || t.Text.Trim().Length == 0) return;
                l.Name = t.Text.Trim(); LogRef.SaveAll(App.Logs); if (l == App.CurrentLog) { App.Cfg.Set("db.name", l.Name); App.Cfg.Set("log.current", l.Name); App.Cfg.Save(); Text = "HamRadioLink " + App.Version + " Logbook - [" + l.Name + "]"; } logTabs.Invalidate();
            }
        }

        void RemoveLog(LogRef l)
        {
            if (App.Logs.Count <= 1) { MessageBox.Show(this, Ui.French ? "Il faut garder au moins un journal." : "Keep at least one log.", "HamRadioLink"); return; }
            if (!Ui.Ask(this, (Ui.French ? "Retirer « " : "Remove \"") + l.Name + (Ui.French ? " » de la liste ? Les QSO restent dans la base (rien n'est supprimé)." : "\" from the list? The QSOs stay in the database (nothing is deleted)."))) return;
            bool cur = l == App.CurrentLog; App.Logs.Remove(l); LogRef.SaveAll(App.Logs); if (cur) { App.CurrentLog = null; SwitchLog(App.Logs[0]); } logTabs.Invalidate();
        }
        // Retire le journal de la liste ET supprime sa base. Encart rouge, nom a retaper : aucune suppression par megarde.
        static readonly string[] SysSchemas = { "mysql", "information_schema", "performance_schema", "sys" };
        void DeleteLog(LogRef l)
        {
            if (App.Logs.Count <= 1) { MessageBox.Show(this, Ui.French ? "Il faut garder au moins un journal." : "Keep at least one log.", "HamRadioLink"); return; }
            bool sys = Array.IndexOf(SysSchemas, l.Schema.ToLowerInvariant()) >= 0; bool shared = App.Logs.Exists(x => x != l && x.Schema == l.Schema && x.Host == l.Host && x.Port == l.Port);
            long n = 0; try { if (l == App.CurrentLog && App.Log != null) n = App.Log.Count(""); } catch { }
            string what = sys ? "la TABLE du journal « " + PeerLog.Table + " » dans la base système « " + l.Schema + " » (la base elle-même est conservée)" : "TOUTE la base « " + l.Schema + " »";
            if (shared) what = "RIEN dans le serveur (un autre onglet utilise la même base : seul cet onglet est retiré)";
            string msg = "ATTENTION - SUPPRESSION DÉFINITIVE\r\n\r\nJournal : « " + l.Name + " »" + (n > 0 ? "  (" + n + " QSO)" : "") + "\r\nVa être supprimé : " + what + ".\r\n\r\nTous les QSO concernés seront PERDUS. Cette action est IRRÉVERSIBLE : pensez à faire une sauvegarde ADIF avant.";
            using (Form f = new Form { Text = "SUPPRESSION DÉFINITIVE", ClientSize = new Size(560, 330), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, ShowInTaskbar = false, BackColor = Color.FromArgb(150, 0, 0), ForeColor = Color.White, Font = new Font("Segoe UI", 10f) })
            {
                Label t = new Label { Left = 16, Top = 14, Width = 528, Height = 170, Text = msg, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold) }; f.Controls.Add(t);
                Label k = new Label { Left = 16, Top = 192, Width = 528, Height = 22, Text = "Pour confirmer, retapez exactement le nom du journal :" }; f.Controls.Add(k);
                TextBox tb = new TextBox { Left = 16, Top = 218, Width = 528, Font = new Font("Segoe UI", 11f) }; f.Controls.Add(tb);
                Button del = new Button { Left = 16, Top = 270, Width = 300, Height = 38, Text = "SUPPRIMER DÉFINITIVEMENT", Enabled = false, BackColor = Color.White, ForeColor = Color.FromArgb(150, 0, 0), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10f, FontStyle.Bold), DialogResult = DialogResult.OK };
                Button no = new Button { Left = 336, Top = 270, Width = 208, Height = 38, Text = "Annuler (ne rien faire)", BackColor = Color.White, ForeColor = Color.Black, FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel }; f.Controls.Add(del); f.Controls.Add(no);
                tb.TextChanged += (s, e) => del.Enabled = tb.Text == l.Name; f.CancelButton = no;
                if (f.ShowDialog(this) != DialogResult.OK) return;
            }
            try
            {
                if (!shared)
                {
                    string host, user, pass; int port; App.ServerOf(l, out host, out port, out user, out pass);
                    using (MySqlClient c = new MySqlClient(host, port, user, pass, "mysql")) { c.Open(); if (sys) c.Exec("DROP TABLE IF EXISTS " + MySqlClient.Ident(l.Schema) + "." + MySqlClient.Ident(PeerLog.Table)); else c.Exec("DROP DATABASE IF EXISTS " + MySqlClient.Ident(l.Schema)); }
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

        // Couleurs du Logbook (View > Colors) : fond, liste, en-tetes, cluster, carte de bande.
        void ApplyTheme()
        {
            Color back = Theme.Get("lb.back"), tt = Theme.Get("lb.tool.text"); BackColor = back; Looks.Dark = Theme.IsDark(back);
            if (toolStrip != null) { toolStrip.BackColor = back; toolStrip.ForeColor = tt; foreach (ToolStripItem it in toolStrip.Items) if (!(it is ToolStripControlHost)) it.ForeColor = tt; }
            if (statusBar != null) { statusBar.BackColor = back; statusBar.ForeColor = tt; foreach (ToolStripItem it in statusBar.Items) it.ForeColor = tt; }
            grid.BackgroundColor = Theme.Get("lb.grid.back"); grid.GridColor = Theme.Get("lb.grid.line"); grid.EnableHeadersVisualStyles = false;
            DataGridViewCellStyle d = grid.DefaultCellStyle; d.BackColor = Theme.Get("lb.grid.back"); d.ForeColor = Theme.Get("lb.grid.text"); d.SelectionBackColor = Theme.Get("lb.grid.sel"); d.SelectionForeColor = Theme.Get("lb.grid.seltext");
            DataGridViewCellStyle h = grid.ColumnHeadersDefaultCellStyle; h.BackColor = Theme.Get("lb.head.back"); h.ForeColor = Theme.Get("lb.head.text"); h.SelectionBackColor = Theme.Get("lb.head.back"); h.SelectionForeColor = Theme.Get("lb.head.text");
            
            grid.Invalidate(); Invalidate(true);
        }
        // Menu Thèmes : un clic applique le thème à toutes les fenêtres du journal (et le mémorise)
        ColorsForm colorsWin;
        void BuildThemeMenu(ToolStripMenuItem m)
        {
            for (int i = 0; i < Theme.All.Count; i++) { int n = i; ToolStripMenuItem it = new ToolStripMenuItem(Theme.All[i].Name); it.Click += (s, e) => Theme.Set(n); m.DropDownItems.Add(it); }
            m.DropDownItems.Add(new ToolStripSeparator());
            m.DropDownItems.Add(new ToolStripMenuItem("Personnaliser les couleurs du thème actuel...", null, (s, e) => { if (colorsWin == null || colorsWin.IsDisposed) { colorsWin = new ColorsForm(); colorsWin.Show(this); } else colorsWin.Activate(); }));
            m.DropDownOpening += (s, e) => { for (int i = 0; i < Theme.All.Count; i++) ((ToolStripMenuItem)m.DropDownItems[i]).Checked = i == Theme.Index; };
        }
        public void ApplyLanguage() { Ui.Translate(menuStrip); Ui.Translate(toolStrip); if (logTabs != null) logTabs.SetItems(toolStrip, search); }
        static ToolStripMenuItem M(string t, params ToolStripItem[] items) { ToolStripMenuItem m = new ToolStripMenuItem(t); m.DropDownItems.AddRange(items); return m; }
        static ToolStripMenuItem M(string t, EventHandler h) { return new ToolStripMenuItem(t, null, h); }
        static ToolStripButton B(string t, EventHandler h) { ToolStripButton b = new ToolStripButton(t) { DisplayStyle = ToolStripItemDisplayStyle.Text }; b.Click += h; return b; }

        // ---------- colonnes ----------
        // La derniere colonne (dans l'ordre affiche) occupe toujours le reste de la largeur, jusqu'au bord, quoi qu'on deplace.
        void FixLast()
        {
            DataGridViewColumn last = null; int mx = -1;
            foreach (DataGridViewColumn c in grid.Columns) { c.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; if (c.DisplayIndex > mx) { mx = c.DisplayIndex; last = c; } }
            if (last != null) { last.MinimumWidth = 70; last.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; }
        }
        void BuildGridColumns() { BuildGridColumnsRaw(); FixLast(); }
        void BuildGridColumnsRaw()
        {
            grid.Columns.Clear();
            foreach (ColDef c in cols)
            {
                if (!c.Visible) continue;
                DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn { HeaderText = c.Title, Width = c.Width, SortMode = DataGridViewColumnSortMode.Programmatic, Tag = c };
                if (c.Right) col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                grid.Columns.Add(col);
            }
        }

        void SaveCols() { List<string> v = new List<string>(); foreach (ColDef c in cols) if (c.Visible) v.Add(c.Title); App.Cfg.Set("lb.cols", string.Join(",", v.ToArray())); App.Cfg.Save(); }

        void ChooseColumns()
        {
            using (Form f = new Form { Text = "Layout : colonnes affichées", Size = new Size(320, 560), StartPosition = FormStartPosition.CenterParent, Font = Ui.Normal })
            {
                CheckedListBox l = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true };
                foreach (ColDef c in cols) l.Items.Add(c.Title, c.Visible);
                Panel p = new Panel { Dock = DockStyle.Bottom, Height = 44 };
                Button ok = Ui.Btn(p, "OK", 130, 8, 80, 28, (s, e) => f.DialogResult = DialogResult.OK); Ui.Btn(p, "Annuler", 215, 8, 80, 28, (s, e) => f.DialogResult = DialogResult.Cancel);
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
                pages.Clear(); pageOrder.Clear();
                order = sortCol + (sortDesc ? " DESC" : " ASC") + ", COL_PRIMARY_KEY " + (sortDesc ? "DESC" : "ASC");
                total = App.Log.Count(where);
                grid.RowCount = 0; grid.RowCount = (int)Math.Min(total, int.MaxValue);
                lblCount.Text = total.ToString("N0", CultureInfo.CurrentCulture) + " QSO";
                lblFilter.Text = where.Length == 0 ? "All" : "Filtered"; lblDb.Text = "MySQL " + App.Db.ServerVersion;
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

        void SortBy(int col)
        {
            ColDef cd = grid.Columns[col].Tag as ColDef; if (cd == null || cd.Sql == "COL_CLUBLOG_LOCAL" || cd.Sql == "COL_QRZ_LOCAL") return;   // colonne locale : pas de tri
            if (cd.Sql == sortCol) sortDesc = !sortDesc; else { sortCol = cd.Sql; sortDesc = false; }
            Reload();
        }

        Qso RowQso(int row)
        {
            int p = row / PageSize; List<Qso> page;
            if (!pages.TryGetValue(p, out page))
            {
                try { page = App.Log.Fetch(where, order, p * PageSize, PageSize); } catch (Exception ex) { Ui.Err(this, ex); page = new List<Qso>(); }
                pages[p] = page; pageOrder.Add(p);
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

        void DeleteSelected()
        {
            List<Qso> s = Selected(); if (s.Count == 0) return;
            if (!Ui.Ask(this, "Supprimer " + s.Count + " QSO de la base ?\r\nCette opération est définitive.")) return;
            try { List<int> ids = new List<int>(); foreach (Qso q in s) ids.Add(q.Id); App.Log.Delete(ids); ReloadKeep(); } catch (Exception ex) { Ui.Err(this, ex); }
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
            using (Form f = new Form { Text = "Edit selections", Size = new Size(430, 250), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, Font = Ui.Normal })
            {
                Ui.Lbl(f, "Champ :", 12, 18); ComboBox c = Ui.Combo(f, 90, 16, 300, new[] { "COL_QSL_SENT", "COL_QSL_RCVD", "COL_LOTW_QSL_SENT", "COL_LOTW_QSL_RCVD", "COL_EQSL_QSL_SENT", "COL_EQSL_QSL_RCVD", "COL_COMMENT", "COL_NOTES", "COL_QSL_VIA", "COL_MY_RIG", "COL_STATION_CALLSIGN", "COL_OPERATOR", "COL_CONTEST_ID", "COL_SUBMODE" }, true); c.SelectedIndex = 0;
                Ui.Lbl(f, "Valeur :", 12, 50); TextBox v = Ui.Txt(f, 90, 48, 300); v.Text = "Y";
                RadioButton r1 = new RadioButton { Left = 90, Top = 84, Width = 300, Text = "Les " + sel.Count + " QSO sélectionnés", Checked = sel.Count > 0, Enabled = sel.Count > 0 }; f.Controls.Add(r1);
                RadioButton r2 = new RadioButton { Left = 90, Top = 108, Width = 320, Text = "Tous les " + total.ToString("N0") + " QSO du filtre actuel", Checked = sel.Count == 0 }; f.Controls.Add(r2);
                Button ok = Ui.Btn(f, "Appliquer", 220, 160, 90, 28, (s, e) => f.DialogResult = DialogResult.OK); Ui.Btn(f, "Annuler", 320, 160, 80, 28, (s, e) => f.DialogResult = DialogResult.Cancel); f.AcceptButton = ok;
                if (f.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    string field = c.Text.Trim(); if (!App.Log.Has(field)) { MessageBox.Show(this, "Champ inconnu : " + field); return; }
                    string w = where;
                    if (r1.Checked) { List<string> ids = new List<string>(); foreach (Qso q in sel) ids.Add(q.Id.ToString()); w = "COL_PRIMARY_KEY IN (" + string.Join(",", ids.ToArray()) + ")"; }
                    else if (!Ui.Ask(this, "Modifier " + total.ToString("N0") + " QSO ?")) return;
                    string upd = MySqlClient.Ident(field) + "=" + MySqlClient.Quote(v.Text);
                    if (field.EndsWith("_SENT") || field.EndsWith("_RCVD")) { string d = field.Replace("_RCVD", "RDATE").Replace("_SENT", "SDATE"); if (App.Log.Has(d) && v.Text == "Y") upd += "," + MySqlClient.Ident(d) + "=NOW()"; }
                    string wf = w, uf = upd;
                    Bg.Run(this, "Modification de la colonne " + field, (log, db, say, cancelled) => { say("Mise à jour en cours…", -1); long n = UpdateChunked(db, log, uf, wf, cancelled, say); return n + " QSO modifiés" + (cancelled() ? " (annulé)" : "") + "."; }, () => ReloadKeep());
                }
                catch (Exception ex) { Ui.Err(this, ex); }
            }
        }

        void AdvancedFilter()
        {
            using (Form f = new Form { Text = "Filter", Size = new Size(430, 330), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, Font = Ui.Normal })
            {
                Ui.Lbl(f, "Indicatif :", 12, 16); TextBox call = Ui.Txt(f, 100, 14, 140);
                Ui.Lbl(f, "Bande :", 12, 48); ComboBox band = Ui.Combo(f, 100, 46, 100, Ui.Bands, true);
                Ui.Lbl(f, "Mode :", 12, 80); ComboBox mode = Ui.Combo(f, 100, 78, 100, Ui.Modes, true);
                Ui.Lbl(f, "Pays :", 12, 112); TextBox ctry = Ui.Txt(f, 100, 110, 200);
                Ui.Lbl(f, "Du :", 12, 144); DateTimePicker d1 = new DateTimePicker { Left = 100, Top = 142, Width = 110, Format = DateTimePickerFormat.Short, ShowCheckBox = true, Checked = false }; f.Controls.Add(d1);
                Ui.Lbl(f, "Au :", 220, 144); DateTimePicker d2 = new DateTimePicker { Left = 255, Top = 142, Width = 110, Format = DateTimePickerFormat.Short, ShowCheckBox = true, Checked = false }; f.Controls.Add(d2);
                Ui.Lbl(f, "Texte libre :", 12, 178); TextBox sql = Ui.Txt(f, 100, 176, 300); new ToolTip().SetToolTip(sql, "Cherche ce texte dans l'indicatif, le nom, le QTH, le pays, le commentaire et le locator");
                Button ok = Ui.Btn(f, "Filtrer", 220, 230, 90, 28, (s, e) => f.DialogResult = DialogResult.OK); Ui.Btn(f, "Annuler", 320, 230, 80, 28, (s, e) => f.DialogResult = DialogResult.Cancel); f.AcceptButton = ok;
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

        void OpenQrz() { List<Qso> s = Selected(); string c = s.Count > 0 ? s[0].S("COL_CALL") : search.Text.Trim(); if (c.Length > 0) try { Process.Start("https://www.qrz.com/db/" + Uri.EscapeDataString(c)); } catch { } }

        // Importer / partager : l'assistant qui retrouve les journaux MariaDB d'un autre logiciel (copie ou partage direct), installe le MariaDB intégré si besoin, importe un fichier ADIF.
        void OpenSetup()
        {
            using (SetupForm f = new SetupForm(true)) f.ShowDialog(this);
            if (App.Log != null) { App.LoadLogs(); logTabs.Invalidate(); Text = "HamRadioLink " + App.Version + " Logbook - [" + App.Cfg.Get("db.name", "Journal") + "]"; Reload(); }
        }
        void OpenDb()
        {
            using (DbForm f = new DbForm()) if (f.ShowDialog(this) == DialogResult.OK) { Text = "HamRadioLink " + App.Version + " Logbook - [" + App.Cfg.Get("db.name", "F5PBG") + "]"; Reload(); }
        }

        void ImportAdif()
        {
            using (OpenFileDialog o = new OpenFileDialog { Filter = "ADIF (*.adi;*.adif)|*.adi;*.adif|All|*.*" })
            {
                if (o.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    List<Dictionary<string, string>> recs = Adif.Parse(File.ReadAllText(o.FileName, Encoding.UTF8));
                    if (!Ui.Ask(this, recs.Count + " QSO trouvés. Les ajouter au journal ?")) return;
                    string me = App.Cfg.Get("me.call", "");
                    Bg.Run(this, "Import ADIF", (log, db, say, cancelled) =>
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
            using (SaveFileDialog sd = new SaveFileDialog { Filter = "ADIF (*.adi)|*.adi", FileName = "HamRadioLink-" + DateTime.Now.ToString("yyyy-MM-dd") + ".adi" })
            {
                if (sd.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    Cursor = Cursors.WaitCursor; List<Qso> list = new List<Qso>(); List<Qso> sel = all ? new List<Qso>() : Selected();
                    if (sel.Count > 1 || (onlySel && sel.Count == 1)) list = sel; else for (int off = 0; ; off += 2000) { List<Qso> p = App.Log.Fetch(all ? "" : where, "COL_TIME_ON ASC", off, 2000); list.AddRange(p); if (p.Count < 2000) break; }
                    File.WriteAllText(sd.FileName, Adif.Export(list), new UTF8Encoding(false)); Cursor = Cursors.Default;
                    MessageBox.Show(this, list.Count + " QSO exportés.", "Export ADIF");
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
            string wl = (where.Length > 0 ? "(" + where + ") AND " : "") + "(COL_LOTW_QSL_SENT IS NULL OR COL_LOTW_QSL_SENT<>'Y')"; long cnt;
            try { cnt = useSel ? sel.Count : App.Log.Count(wl); } catch (Exception ex) { Ui.Err(this, ex); return; }
            if (cnt == 0) { MessageBox.Show(this, "Aucun QSO à exporter.", "LoTW"); return; }
            string tqsl = App.Cfg.Get("lotw.tqsl", "C:\\Program Files (x86)\\TrustedQSL\\tqsl.exe"), loc = App.Cfg.Get("lotw.location", "");
            if (!File.Exists(tqsl) || loc.Length == 0) { MessageBox.Show(this, "Pour produire le fichier .tq8, réglez TQSL et le nom de la « Station Location » dans Outils > Online services.", "Fichier LoTW"); return; }
            using (SaveFileDialog sd = new SaveFileDialog { Filter = "Fichier LoTW signé (*.tq8)|*.tq8", FileName = "LoTW-" + DateTime.Now.ToString("yyyy-MM-dd") + ".tq8", Title = "Fichier LoTW : " + cnt + " QSO (" + (useSel ? "la sélection" : "les QSO pas encore envoyés à LoTW") + ")" })
            {
                if (sd.ShowDialog(this) != DialogResult.OK) return;
                string adi = Path.Combine(Path.GetTempPath(), "hrl-lotw-" + Guid.NewGuid().ToString("N") + ".adi"), tq8 = sd.FileName, tmp8 = Path.ChangeExtension(adi, ".tq8");   // TQSL ne sait pas écrire dans un dossier à accents : il signe dans le dossier temporaire, puis on copie
                // lecture, signature par TQSL et marquage : tout sur un autre fil (la télécommande et le journal restent utilisables)
                Bg.Run(this, "Fichier LoTW (signature par TQSL)", (log, db, say, cancelled) =>
                {
                    try
                    {
                        List<Qso> list = sel;
                        if (!useSel) { list = new List<Qso>(); say("Lecture des QSO…", -1); long n = Math.Min(cnt, 100000); for (int off = 0; off < n; off += 2000) { List<Qso> p = log.Fetch(wl, "COL_TIME_ON ASC", off, 2000); list.AddRange(p); if (p.Count < 2000) break; } }
                        say("Signature par TQSL de " + list.Count + " QSO (peut durer plusieurs minutes)…", -1);
                        File.WriteAllText(adi, Adif.Export(list), new UTF8Encoding(false));
                        int code; string o = Online.RunTqsl(tqsl, "-d -q -a all -l \"" + loc + "\" -o \"" + tmp8 + "\" \"" + adi + "\"", 600000, out code);
                        bool signed = code == 0 && File.Exists(tmp8) && new FileInfo(tmp8).Length > 100; if (signed) File.Copy(tmp8, tq8, true);
                        if (signed)
                        {
                            List<int> ids = new List<int>(); foreach (Qso q in list) ids.Add(q.Id); MarkDb(db, log, new List<string> { "lotw" }, ids, true, cancelled, say);
                            return list.Count + " QSO signés et marqués « envoyés à LoTW ».\r\n\r\nFichier à déposer sur https://lotw.arrl.org (Upload File) :\r\n" + tq8;
                        }
                        if (code == -1) o = "délai dépassé (TQSL attend peut-être le mot de passe de votre certificat)";
                        string keep = Path.ChangeExtension(tq8, ".adi"); File.Copy(adi, keep, true);
                        return "TQSL n'a pas produit le fichier .tq8 (code " + code + (o.Trim().Length > 0 ? " : " + o.Trim() : "") + "). Les QSO ne sont PAS marqués. ADIF gardé : " + keep + " (TQSL > File > Sign an ADIF file).";
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
                    if (sent) db.Exec("UPDATE " + lg.TableRef() + " SET " + fld + "='Y'," + dt + "=NOW() WHERE COL_PRIMARY_KEY IN (" + inl + ")");
                    else { try { db.Exec("UPDATE " + lg.TableRef() + " SET " + fld + "='N'," + dt + "=NULL WHERE COL_PRIMARY_KEY IN (" + inl + ")"); } catch { db.Exec("UPDATE " + lg.TableRef() + " SET " + fld + "='N' WHERE COL_PRIMARY_KEY IN (" + inl + ")"); } }
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
                foreach (string k in svc == "all" ? new[] { "qrz", "clublog", "eqsl", "lotw" } : new[] { svc })
                {
                    if (k == "qrz") { if (sent) QrzState.Mark(ids); else SentState.Unmark("qrz", ids); }
                    else if (k == "clublog") { if (sent) ClubLogState.Mark(ids); else SentState.Unmark("clublog", ids); }
                    else dbk.Add(k);
                }
                if (dbk.Count == 0) { ReloadKeep(); return; }
                if (ids.Count <= 300) { MarkDb(App.Db, App.Log, dbk, ids, sent, () => false, null); ReloadKeep(); }
                else Bg.Run(this, "Marquage de " + ids.Count + " QSO", (log, db, say, cancelled) => { MarkDb(db, log, dbk, ids, sent, cancelled, say); return ids.Count + " QSO marqués" + (cancelled() ? " (annulé)" : "") + "."; }, () => ReloadKeep());
            }
            catch (Exception ex) { Ui.Err(this, ex); }
        }

        // QRZ.com refuse l'envoi automatique (souvent : pas d'abonnement) : on crée le fichier ADI des QSO restants, on explique, et on ouvre la page d'import du journal QRZ.com
        void QrzManual(SendSvc s, string err, List<Qso> rest)
        {
            try
            {
                string dir = Path.Combine(App.DataDir ?? ".", "exports"); Directory.CreateDirectory(dir);
                string file = Path.Combine(dir, "QRZ-" + DateTime.Now.ToString("yyyy-MM-dd_HHmm") + ".adi"); File.WriteAllText(file, Adif.Export(rest), new UTF8Encoding(false)); List<int> qid = new List<int>(); foreach (Qso q in rest) qid.Add(q.Id); QrzState.Mark(qid); ReloadKeep();   // fichier préparé pour l'import = marqués « envoyés à QRZ.com »
                MessageBox.Show(this, "QRZ.com n'a pas accepté l'envoi automatique :\r\n" + err + "\r\n\r\n(L'envoi automatique demande un abonnement QRZ avec accès à l'API Logbook.)\r\n\r\nUn fichier ADI de " + rest.Count + " QSO a été créé :\r\n" + file + "\r\n\r\nLa page de votre journal QRZ.com va s'ouvrir : utilisez la fonction d'import (Upload / Import ADIF) et choisissez ce fichier.", "QRZ.com : envoi manuel");
                try { Process.Start(new ProcessStartInfo("https://logbook.qrz.com/logbook") { UseShellExecute = true }); } catch { }
                try { Process.Start("explorer.exe", "/select,\"" + file + "\""); } catch { }
            }
            catch (Exception ex) { Ui.Err(this, ex); }
        }

        // Outils > Envoyer à tous les logs en ligne : QRZ.com, Club Log, eQSL, LoTW (ceux qui sont réglés), dans une fenêtre qui ne bloque pas le journal.
        // QSO envoyés : la sélection ; sans sélection, ceux pas encore marqués « envoyés » à eQSL / LoTW (ou, si aucun des deux n'est réglé, les 500 derniers de la liste).
        void SendAllOnline(string only = null)
        {
            if (App.Log == null) return;
            List<SendSvc> sv = new List<SendSvc>(); List<string> skipped = new List<string>();
            if (only != null && only != "qrz") { } else if (App.Cfg.Get("qrzlog.key", "").Length > 0) sv.Add(new SendSvc { Key = "qrz", Name = "QRZ.com Logbook", Fn = Online.UploadQrz, Batch = 5, Mark = "qrz", OnFail = QrzManual });
            else if (only == "qrz") sv.Add(new SendSvc { Key = "qrz", Name = "QRZ.com Logbook", Fn = delegate(List<Qso> q) { throw new Exception("clé API QRZ.com Logbook non renseignée (Outils > Online services)"); }, Batch = 5, Mark = "qrz", OnFail = QrzManual });
            else skipped.Add("QRZ.com");
            if (only != null && only != "clublog") { } else if (App.Cfg.Get("clublog.email", "").Length > 0 && App.Cfg.Get("clublog.api", "").Length > 0) sv.Add(new SendSvc { Key = "clublog", Name = "Club Log", Fn = Online.UploadClubLog, Batch = 5, Mark = "clublog" }); else skipped.Add("Club Log");
            if (only != null && only != "eqsl") { } else if (App.Cfg.Get("eqsl.user", "").Length > 0) sv.Add(new SendSvc { Key = "eqsl", Name = "eQSL.cc", Fn = Online.UploadEqsl, Batch = 1, Mark = "eqsl" }); else skipped.Add("eQSL");
            if (only != null && only != "lotw") { } else if (File.Exists(App.Cfg.Get("lotw.tqsl", "C:\\Program Files (x86)\\TrustedQSL\\tqsl.exe")) && App.Cfg.Get("lotw.location", "").Length > 0) sv.Add(new SendSvc { Key = "lotw", Name = "LoTW (TQSL)", Fn = Online.UploadLotw, Batch = 500, Mark = "lotw" }); else skipped.Add("LoTW");
            if (sv.Count == 0) { MessageBox.Show(this, (only != null ? "Ce service n'est pas réglé" : "Aucun service en ligne n'est réglé") + " : Outils > Online services.", "Envoi"); return; }
            List<Qso> list = Selected(); string what = "les " + list.Count + " QSO sélectionnés";
            try
            {
                Cursor = Cursors.WaitCursor;
                if (list.Count == 0)
                {
                    List<string> c = new List<string>(); foreach (SendSvc s in sv) if (s.Mark == "eqsl") c.Add("(COL_EQSL_QSL_SENT IS NULL OR COL_EQSL_QSL_SENT<>'Y')"); else if (s.Mark == "lotw") c.Add("(COL_LOTW_QSL_SENT IS NULL OR COL_LOTW_QSL_SENT<>'Y')");
                    if (c.Count == 0) { list = App.Log.Fetch(where, "COL_TIME_ON DESC", 0, 500); what = "les " + list.Count + " derniers QSO de la liste"; }
                    else
                    {
                        string w = (where.Length > 0 ? "(" + where + ") AND " : "") + "(" + string.Join(" OR ", c.ToArray()) + ")";
                        long n = App.Log.Count(w); if (n > 50000) n = 50000;
                        for (int off = 0; off < n; off += 2000) { List<Qso> p = App.Log.Fetch(w, "COL_TIME_ON ASC", off, 2000); list.AddRange(p); if (p.Count < 2000) break; }
                        what = "les " + list.Count + " QSO pas encore envoyés à eQSL / LoTW";
                    }
                }
            }
            catch (Exception ex) { Cursor = Cursors.Default; Ui.Err(this, ex); return; }
            finally { Cursor = Cursors.Default; }
            if (list.Count == 0) { MessageBox.Show(this, "Aucun QSO à envoyer.", "Envoi"); return; }
            List<string> names = new List<string>(); foreach (SendSvc s in sv) names.Add(s.Name);
            if (!Ui.Ask(this, "Envoyer " + what + " à : " + string.Join(", ", names.ToArray()) + " ?" + (skipped.Count > 0 ? "\r\n\r\n(non réglés, ignorés : " + string.Join(", ", skipped.ToArray()) + ")" : ""))) return;
            SendAllForm f = new SendAllForm(sv, list);
            // chaque lot envoyé est marqué tout de suite (eQSL / LoTW dans la base, Club Log dans son fichier) : une interruption laisse un état exact
            f.BatchDone = delegate(SendSvc s, List<Qso> part)
            {
                try
                {
                    if (s.Mark == null || part.Count == 0 || App.Log == null) return;
                    List<int> ids = new List<int>(); foreach (Qso q in part) ids.Add(q.Id);
                    if (s.Mark == "clublog") { ClubLogState.Mark(ids); return; }
                    if (s.Mark == "qrz") { QrzState.Mark(ids); return; }
                    string fld = s.Mark == "eqsl" ? "COL_EQSL_QSL_SENT" : "COL_LOTW_QSL_SENT", dt = s.Mark == "eqsl" ? "COL_EQSL_QSLSDATE" : "COL_LOTW_QSLSDATE"; List<string> sid = new List<string>(); foreach (int i in ids) sid.Add(i.ToString());
                    App.Db.Exec("UPDATE " + App.Log.TableRef() + " SET " + fld + "='Y'," + dt + "=NOW() WHERE COL_PRIMARY_KEY IN (" + string.Join(",", sid.ToArray()) + ")");
                }
                catch (Exception ex) { Ui.Err(this, ex); }
            };
            f.Finished = delegate(List<SendSvc> done) { try { ReloadKeep(); } catch { } };            f.Show(this); f.Start();
        }
        void FindDupes()
        {
            try
            {
                MySqlResult r = App.Db.Query("SELECT COL_CALL, COL_TIME_ON, COL_BAND, COUNT(*) c FROM " + App.Log.TableRef() + " GROUP BY COL_CALL, COL_TIME_ON, COL_BAND HAVING c>1 ORDER BY COL_TIME_ON DESC LIMIT 200");
                if (r.Rows.Count == 0) { MessageBox.Show(this, "Aucun doublon (même indicatif, même heure, même bande).", "Doublons"); return; }
                if (Ui.Ask(this, r.Rows.Count + " groupes de doublons. Les afficher dans la liste ?")) SetFilter("(COL_CALL, COL_TIME_ON, COL_BAND) IN (SELECT COL_CALL, COL_TIME_ON, COL_BAND FROM " + App.Log.TableRef() + " GROUP BY COL_CALL, COL_TIME_ON, COL_BAND HAVING COUNT(*)>1)");
            }
            catch (Exception ex) { Ui.Err(this, ex); }
        }

        // Complète pays / continent / DXCC / zones des QSO où ils manquent, d'après la table des pays
        void UpdateFromCountries()
        {
            if (!Countries.Loaded) { MessageBox.Show(this, "Table des pays absente (fichier Countries.txt dans le dossier data/journal).", "Pays"); return; }
            if (!Ui.Ask(this, "Compléter les QSO sans pays / DXCC d'après l'indicatif ?")) return;
            Bg.Run(this, "Compléter les pays", (log, db, say, cancelled) =>
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
                sb.AppendLine("QSO : " + App.Log.Count(""));
                sb.AppendLine("Pays (DXCC) : " + App.Db.Scalar("SELECT COUNT(DISTINCT COL_DXCC) FROM " + App.Log.TableRef() + " WHERE COL_DXCC<>'' AND COL_DXCC<>'0'"));
                sb.AppendLine("Indicatifs : " + App.Db.Scalar("SELECT COUNT(DISTINCT COL_CALL) FROM " + App.Log.TableRef()));
                sb.AppendLine().AppendLine("Par bande :");
                foreach (string[] r in App.Db.Query("SELECT COL_BAND, COUNT(*) FROM " + App.Log.TableRef() + " GROUP BY COL_BAND ORDER BY 2 DESC LIMIT 12").Rows) sb.AppendLine("  " + r[0] + " : " + r[1]);
                sb.AppendLine().AppendLine("Par mode :");
                foreach (string[] r in App.Db.Query("SELECT COL_MODE, COUNT(*) FROM " + App.Log.TableRef() + " GROUP BY COL_MODE ORDER BY 2 DESC LIMIT 12").Rows) sb.AppendLine("  " + r[0] + " : " + r[1]);
                sb.AppendLine().AppendLine("Par année :");
                foreach (string[] r in App.Db.Query("SELECT YEAR(COL_TIME_ON), COUNT(*) FROM " + App.Log.TableRef() + " GROUP BY 1 ORDER BY 1 DESC LIMIT 15").Rows) sb.AppendLine("  " + r[0] + " : " + r[1]);
                MessageBox.Show(this, sb.ToString(), "Statistiques");
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
            try { Dxcc(); Simple("WAZ (zones CQ)", "COL_CQZ", 40, "CQ zone"); Simple("WAC (continents)", "COL_CONT", 6, "Continent"); Was(); Simple("IOTA", "COL_IOTA", 0, "Référence IOTA"); }
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
                foreach (KeyValuePair<Color, string> k in new[] { new KeyValuePair<Color, string>(Green, "Confirmé"), new KeyValuePair<Color, string>(Amber, "Contacté"), new KeyValuePair<Color, string>(Color.FromArgb(70, 74, 100), "Pas encore") })
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
            foreach (string[] r in App.Db.Query("SELECT COL_DXCC, COL_BAND, MAX(" + Conf + ") c FROM " + App.Log.TableRef() + " WHERE COL_DXCC<>'' AND COL_DXCC<>'0' GROUP BY COL_DXCC, COL_BAND").Rows)
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
            top.Text = "  DXCC : " + worked + " contactés, " + conf + " confirmés (QSL / LoTW / eQSL)  —  entités actuelles : " + list.FindAll(x => !x.Deleted).Count;
        }

        void Simple(string title, string col, int goal, string head)
        {
            TabPage tp = new TabPage(title) { BackColor = Theme.Get("lb.back") }; tabs.TabPages.Add(tp); DataGridView g = NewGrid(); tp.Controls.Add(g);
            Label top = new Label { Dock = DockStyle.Top, Height = 24, Font = new Font(Ui.Normal, FontStyle.Bold) }; tp.Controls.Add(top);
            g.Columns.Add("v", head); g.Columns.Add("w", "QSO"); g.Columns.Add("c", "Confirmés"); int worked = 0, conf = 0;
            foreach (string[] r in App.Db.Query("SELECT " + col + ", COUNT(*), SUM(" + Conf + ") FROM " + App.Log.TableRef() + " WHERE " + col + " IS NOT NULL AND " + col + "<>'' AND " + col + "<>'0' GROUP BY " + col + " ORDER BY " + (col == "COL_CQZ" ? "CAST(" + col + " AS UNSIGNED)" : col)).Rows)
            { g.Rows.Add(r[0], r[1], r[2]); worked++; if (r[2] != "0" && r[2] != null) conf++; }
            top.Text = "  " + title + " : " + worked + (goal > 0 ? " / " + goal : "") + " contactés, " + conf + " confirmés";
        }

        void Was()
        {
            TabPage tp = new TabPage("WAS (états US)") { BackColor = Theme.Get("lb.back") }; tabs.TabPages.Add(tp); DataGridView g = NewGrid(); tp.Controls.Add(g);
            Label top = new Label { Dock = DockStyle.Top, Height = 24, Font = new Font(Ui.Normal, FontStyle.Bold) }; tp.Controls.Add(top);
            g.Columns.Add("v", "État"); g.Columns.Add("w", "QSO"); g.Columns.Add("c", "Confirmés"); int worked = 0, conf = 0;
            foreach (string[] r in App.Db.Query("SELECT COL_STATE, COUNT(*), SUM(" + Conf + ") FROM " + App.Log.TableRef() + " WHERE COL_DXCC IN ('291','6','110') AND COL_STATE<>'' GROUP BY COL_STATE ORDER BY 1").Rows)
            { g.Rows.Add(r[0], r[1], r[2]); worked++; if (r[2] != "0") conf++; }
            top.Text = "  WAS : " + worked + " / 50 contactés, " + conf + " confirmés";
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
