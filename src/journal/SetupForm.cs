using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace HamRadioLink.Journal
{
    // Assistant de première utilisation : station, base de données (MariaDB intégré si aucun MariaDB n'est installé),
    // récupération des réglages de l'autre logiciel, importation de son journal après sauvegarde ADI.
    public class SetupForm : Form
    {
        class Choice { public string Text, Kind; public PeerDb Db; }

        readonly PeerProfile peer; readonly string sysServer; readonly int peerPort;
        readonly Panel[] pages = new Panel[5]; int page;
        readonly Button bBack, bNext, bCancel; readonly Label lblTitle;
        // page 1
        TextBox tCall, tName, tCity, tGrid, tCountry, tZip; Label lblPeerNote;
        // page 2
        readonly List<Choice> choices = new List<Choice>(); readonly List<RadioButton> radios = new List<RadioButton>(); Label lblWhere; TextBox tHost, tPort, tUser, tPass; Panel pChoices, pServer;
        // page 3
        CheckBox kBackup; Label lblBackup; Button bBackup; bool backupBusy;
        // page 4
        TextBox log; ProgressBar bar; volatile bool working, done;
        public bool Completed;

        public SetupForm(bool rerun = false)
        {
            Text = "HamRadioLink — Première utilisation"; Font = Ui.Normal; Icon = Ui.AppIcon; ClientSize = new Size(640, 566); StartPosition = FormStartPosition.CenterScreen; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
            peer = PeerProfile.Read(); sysServer = Embedded.SystemServer(); peerPort = App.Cfg.GetInt("test.peerport", 0);
            if (peerPort > 0) foreach (PeerDb d in peer.Dbs) if (d.IsMySql) { d.Port = peerPort; d.Database = App.Cfg.Get("test.peerdb", d.Database); }   // essais sur une copie
            if (peer.Found) peer.Probe(0);

            lblTitle = new Label { Left = 20, Top = 12, Width = 600, Height = 28, Font = new Font("Segoe UI", 13f, FontStyle.Bold) }; Controls.Add(lblTitle);
            for (int i = 0; i < pages.Length; i++) { pages[i] = new Panel { Left = 20, Top = 50, Width = 600, Height = 456, Visible = false }; Controls.Add(pages[i]); }
            bBack = Ui.Btn(this, "< Précédent", 330, 524, 90, 28, (s, e) => Go(-1)); bNext = Ui.Btn(this, "Suivant >", 426, 524, 90, 28, (s, e) => Go(1)); bCancel = Ui.Btn(this, "Annuler", 530, 524, 90, 28, (s, e) => { if (!working) Close(); });
            BuildWelcome(); BuildStation(); BuildJournal(); BuildBackup(); BuildInstall();
            Show(0);
            FormClosing += (s, e) => { if (working) e.Cancel = true; };
        }

        // Essai automatique (--setup-test=shared|copy|empty) : parcourt les pages, les photographie dans data\smoke et lance l'installation.
        public bool AutoRun(string kind)
        {
            string dir = Path.Combine(App.DataDir, "smoke"); Directory.CreateDirectory(dir); Show(); 
            Action<string> snap = n => { using (Bitmap b = new Bitmap(Width, Height)) { DrawToBitmap(b, new Rectangle(0, 0, Width, Height)); b.Save(Path.Combine(dir, "assistant-" + n + ".png")); } };
            Action pump = () => { for (int i = 0; i < 6; i++) { Application.DoEvents(); Thread.Sleep(40); } };
            Show(0); pump(); snap("1-bienvenue"); Show(1); pump(); if (tCall.Text.Length == 0) { tCall.Text = "14AB1234"; tGrid.Text = "JN18"; } snap("2-station");
            Show(2); pump(); for (int i = 0; i < radios.Count; i++) radios[i].Checked = choices[i].Kind == (kind == "copy" ? "import" : kind); pump(); snap("3-journal");
            if (NeedsBackup()) { Show(3); pump(); kBackup.Checked = true; snap("4-sauvegarde"); }
            string ta = App.Cfg.Get("test.adif", ""); if (ta.Length > 0) adifJobs.Add(new AdifJob { File = ta, Name = "Import test" }); string tb = App.Cfg.Get("test.adif2", ""); if (tb.Length > 0) adifJobs.Add(new AdifJob { File = tb, Name = "Second ADI" });
            Show(4); DateTime end = DateTime.UtcNow.AddMinutes(8); while ((working || !log.Text.Contains("Installation terminée")) && !log.Text.Contains("ERREUR") && DateTime.UtcNow < end) { Application.DoEvents(); Thread.Sleep(100); }
            pump(); snap("5-installation"); File.WriteAllText(Path.Combine(dir, "assistant-journal.txt"), log.Text); return log.Text.Contains("Installation terminée");
        }

        // ---------------- pages ----------------
        void BuildWelcome()
        {
            Panel p = pages[0]; StringBuilder sb = new StringBuilder();
            sb.AppendLine("Bienvenue dans HamRadioLink.");
            sb.AppendLine(); sb.AppendLine("Cet assistant prépare le journal de trafic (base de données), vos informations de station et, si l'autre logiciel est présent, reprend ses réglages.");
            sb.AppendLine(); sb.AppendLine("Ce qui a été détecté sur cet ordinateur :");
            sb.AppendLine("  •  Autre logiciel de journal (réglages et base MariaDB trouvés) : " + (peer.Found ? "oui" + (!peer.Settings.ContainsKey("me.call") || string.IsNullOrEmpty(peer.Settings["me.call"]) ? "" : " (indicatif " + peer.Settings["me.call"] + ")") : "non"));
            sb.AppendLine("  •  MariaDB / MySQL déjà installé : " + (sysServer ?? "non"));
            sb.AppendLine("  •  Base MariaDB intégrée à HamRadioLink : " + (Embedded.EnginePresent ? "présente" + (Embedded.DataReady ? " (journal déjà créé)" : "") : "absente"));
            sb.AppendLine();
            if (sysServer != null) sb.AppendLine("MariaDB étant déjà installé, HamRadioLink l'utilisera : rien de plus n'est installé.");
            else sb.AppendLine("Aucun MariaDB n'est installé : HamRadioLink utilisera son propre MariaDB intégré, installé automatiquement dans son dossier (aucun service Windows, aucune action de votre part).");
            Label l = new Label { Left = 0, Top = 0, Width = 600, Height = 340, Text = sb.ToString() }; p.Controls.Add(l);
        }

        void BuildStation()
        {
            Panel p = pages[1]; int y = 6;
            lblPeerNote = new Label { Left = 0, Top = y, Width = 600, Height = 34, ForeColor = Color.DarkGreen, Text = peer.Found && peer.Settings.ContainsKey("me.call") ? "Ces informations ont été reprises de l'autre logiciel : vérifiez-les." : "Renseignez votre station. Elle sert à tous les QSO enregistrés (indicatif, locator)." }; p.Controls.Add(lblPeerNote); y += 44;
            Func<string, string> g = k => { string v; return peer.Settings.TryGetValue(k, out v) ? v : App.Cfg.Get(k, ""); };
            Ui.Lbl(p, "Indicatif *", 0, y); tCall = Ui.Txt(p, 150, y - 2, 200); tCall.Text = g("me.call"); Ui.Lbl(p, "radioamateur (F5PBG, DL/F5PBG/P) ou autre (14AB1234)", 360, y); y += 34;
            Ui.Lbl(p, "Locator (QRA) *", 0, y); tGrid = Ui.Txt(p, 150, y - 2, 120); tGrid.Text = g("me.grid"); Ui.Lbl(p, "ex. IN78TI", 280, y); y += 34;
            Ui.Lbl(p, "Nom / opérateur", 0, y); tName = Ui.Txt(p, 150, y - 2, 300); tName.Text = g("me.name"); y += 34;
            Ui.Lbl(p, "Ville (QTH)", 0, y); tCity = Ui.Txt(p, 150, y - 2, 300); tCity.Text = g("me.city"); y += 34;
            Ui.Lbl(p, "Pays", 0, y); tCountry = Ui.Txt(p, 150, y - 2, 300); tCountry.Text = g("me.country"); y += 34;
            Ui.Lbl(p, "Code postal", 0, y); tZip = Ui.Txt(p, 150, y - 2, 120); tZip.Text = g("me.zip");
        }

        void BuildJournal()
        {
            Panel p = pages[2];
            lblWhere = new Label { Left = 0, Top = 0, Width = 600, Height = 50 }; p.Controls.Add(lblWhere);
            pChoices = new Panel { Left = 0, Top = 56, Width = 600, Height = 200, AutoScroll = true }; p.Controls.Add(pChoices);
            pServer = new Panel { Left = 0, Top = 262, Width = 600, Height = 96 }; p.Controls.Add(pServer);
            // Import manuel de fichiers ADIF (.adi) : chacun devient un NOUVEAU journal (onglet) dont on choisit le nom
            pAdif = new Panel { Left = 0, Top = 352, Width = 600, Height = 96 }; p.Controls.Add(pAdif);
            Ui.Btn(pAdif, "Ajouter un fichier ADIF (.adi)...", 0, 0, 230, 28, (s, e) => PickAdif()); Ui.Btn(pAdif, "Retirer le fichier s\u00e9lectionn\u00e9", 240, 0, 220, 28, (s, e) => RemoveAdif());
            lbAdif = new ListBox { Left = 0, Top = 32, Width = 600, Height = 60, Font = Ui.Normal }; pAdif.Controls.Add(lbAdif);
            lblAdif = new Label { Left = 470, Top = 4, Width = 130, Height = 24, ForeColor = Color.DarkGreen, Text = "(facultatif)" }; pAdif.Controls.Add(lblAdif);
            Ui.Lbl(pServer, "Serveur MariaDB existant (pour un nouveau journal) :", 0, 0, 500);
            Ui.Lbl(pServer, "Serveur", 0, 28); tHost = Ui.Txt(pServer, 70, 26, 140); Ui.Lbl(pServer, "Port", 225, 28); tPort = Ui.Txt(pServer, 260, 26, 60);
            Ui.Lbl(pServer, "Utilisateur", 0, 60); tUser = Ui.Txt(pServer, 70, 58, 140); Ui.Lbl(pServer, "Mot de passe", 225, 60); tPass = Ui.Txt(pServer, 310, 58, 160); tPass.UseSystemPasswordChar = true;
        }

        void BuildBackup()
        {
            Panel p = pages[3];
            p.Controls.Add(new Label { Left = 0, Top = 0, Width = 600, Height = 120, Text = "Avant de reprendre le journal de l'autre logiciel, faites une SAUVEGARDE ADI par sécurité.\r\n\r\nLe journal de l'autre logiciel n'est que lu (jamais modifié) pendant l'importation, mais une sauvegarde reste indispensable : c'est un fichier .adi que n'importe quel logiciel de trafic peut relire." });
            bBackup = Ui.Btn(p, "Faire la sauvegarde ADI maintenant...", 0, 130, 280, 30, (s, e) => DoBackup());
            lblBackup = new Label { Left = 0, Top = 168, Width = 600, Height = 50, ForeColor = Color.DarkGreen }; p.Controls.Add(lblBackup);
            kBackup = Ui.Chk(p, "J'ai fait une sauvegarde ADI de mon journal (obligatoire pour continuer)", 0, 232); kBackup.CheckedChanged += (s, e) => bNext.Enabled = kBackup.Checked;
        }

        void BuildInstall()
        {
            Panel p = pages[4]; bar = new ProgressBar { Left = 0, Top = 0, Width = 600, Height = 18, Style = ProgressBarStyle.Marquee, Visible = false }; p.Controls.Add(bar);
            log = new TextBox { Left = 0, Top = 26, Width = 600, Height = 320, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Font = Ui.Mono }; p.Controls.Add(log);
        }

        // ---------------- navigation ----------------
        string[] titles = { "Bienvenue", "Votre station", "Votre journal de trafic", "Sauvegarde de votre journal", "Installation" };
        bool NeedsBackup() { Choice c = Selected(); return c != null && (c.Kind == "import" || c.Kind == "shared"); }
        Choice Selected() { for (int i = 0; i < radios.Count; i++) if (radios[i].Checked) return choices[i]; return null; }

        void Show(int n)
        {
            page = n; for (int i = 0; i < pages.Length; i++) pages[i].Visible = i == n; lblTitle.Text = titles[n];
            bBack.Enabled = n > 0 && n < 4; bNext.Text = n == 3 ? "Installer >" : n == 2 && !NeedsBackup() ? "Installer >" : "Suivant >"; bNext.Enabled = true; bCancel.Text = "Annuler";
            if (n == 2) FillJournal();
            if (n == 3) { bNext.Enabled = kBackup.Checked; }
            if (n == 4) { bBack.Enabled = false; bNext.Text = "Terminer"; bNext.Enabled = false; bCancel.Enabled = false; Install(); }
        }

        void Go(int d)
        {
            if (d > 0)
            {
                if (page == 1)
                {
                    if (!Input.Callsign(tCall.Text)) { MessageBox.Show(this, "Indicatif non valide. Exemples : F5PBG, DL/F5PBG/P, 14AB1234 (lettres, chiffres, au moins un chiffre).", "HamRadioLink"); return; }
                    if (!Input.Locator(tGrid.Text)) { MessageBox.Show(this, "Locator non valide. Format : 2 lettres, 2 chiffres, puis 2 lettres (ex. IN78 ou IN78TI).", "HamRadioLink"); return; }
                }
                if (page == 2)
                {
                    Choice c = Selected(); if (c == null) return;
                    if (c.Kind == "empty" && sysServer != null && !(c.Db != null)) { int pt; if (tHost.Text.Trim().Length == 0 || !int.TryParse(tPort.Text, out pt)) { MessageBox.Show(this, "Indiquez le serveur et le port de MariaDB.", "HamRadioLink"); return; } }
                    Show(NeedsBackup() ? 3 : 4); return;
                }
                if (page == 3) { Show(4); return; }
                if (page == 4) { Completed = true; DialogResult = DialogResult.OK; Close(); return; }
                Show(page + 1);
            }
            else Show(page == 3 ? 2 : page - 1);
        }

        // Journaux de l'autre logiciel trouves (MariaDB) : on peut les reprendre tous, chacun devient un onglet du Logbook.
        CheckedListBox clb; readonly List<PeerDb> found = new List<PeerDb>();

        void FillJournal()
        {
            choices.Clear(); radios.Clear(); pChoices.Controls.Clear(); found.Clear(); int y = 0;
            clb = new CheckedListBox { Left = 24, Top = 84, Width = 560, Height = 84, CheckOnClick = true, Font = Ui.Normal }; clb.ItemCheck += (s, e) => BeginInvoke(new Action(Show2));
            foreach (PeerDb d in peer.Dbs) if (d.Count >= 0 && d.IsMySql) { found.Add(d); clb.Items.Add(d.ToString(), true); }
            Action<Choice, bool> add = (c, chk) => { RadioButton r = new RadioButton { Left = 0, Top = y, Width = 580, Height = 28, Text = c.Text, Checked = chk, Font = Ui.Normal }; r.CheckedChanged += (s, e) => Show2(); pChoices.Controls.Add(r); choices.Add(c); radios.Add(r); y += 28; };
            bool any = found.Count > 0;
            if (any)
            {
                add(new Choice { Kind = "import", Text = "IMPORTER une copie de chaque journal coché (un onglet par journal ; l'original reste intact)" }, true);
                add(new Choice { Kind = "shared", Text = "Utiliser DIRECTEMENT le journal MariaDB existant (partagé avec l'autre logiciel)" }, false);
                pChoices.Controls.Add(new Label { Left = 24, Top = 60, Width = 560, Height = 20, Text = "Cochez les journaux à reprendre (décochez ceux que vous ne voulez pas importer) :" }); pChoices.Controls.Add(clb); y = 172;
            }
            add(new Choice { Kind = "empty", Text = "Créer un NOUVEAU journal vide." }, !any);
            string unreadable = ""; foreach (PeerDb d in peer.Dbs) if (d.Count < 0 && d.IsMySql && d.Error.Length > 0) unreadable += "  • " + d.Title + " : " + d.Error + "\r\n";
            lblWhere.Text = (sysServer != null ? "MariaDB est déjà installé sur cet ordinateur (" + sysServer + ") : HamRadioLink l'utilise." : "Aucun MariaDB installé : HamRadioLink installera et utilisera son MariaDB intégré (automatique).")
                + (peer.Dbs.Count > 0 ? "\r\nJournaux trouvés dans l'autre logiciel : " + peer.Dbs.Count + (found.Count < peer.Dbs.Count ? " (non lisibles : " + (peer.Dbs.Count - found.Count) + ")" : "") + "." : "");
            if (unreadable.Length > 0) lblWhere.Text += "\r\n" + unreadable.TrimEnd();
            if (peer.Dbs.Count > 0 && tHost.Text.Length == 0) { PeerDb d0 = peer.Dbs.Find(x => x.IsMySql); if (d0 != null) { tHost.Text = d0.Host; tPort.Text = d0.Port.ToString(); tUser.Text = d0.User; tPass.Text = d0.Password; } }
            if (tHost.Text.Length == 0) { tHost.Text = "127.0.0.1"; tPort.Text = "3306"; tUser.Text = "root"; }
            Show2();
        }
        List<PeerDb> Chosen() { List<PeerDb> l = new List<PeerDb>(); if (clb != null) for (int i = 0; i < clb.Items.Count; i++) if (clb.GetItemChecked(i)) l.Add(found[i]); return l; }
        void Show2()
        {
            Choice c = Selected(); bool importing = c != null && c.Kind != "empty";
            if (clb != null) clb.Enabled = importing; pServer.Visible = sysServer != null && (c == null || c.Kind == "empty");
            bNext.Text = NeedsBackup() ? "Suivant >" : "Installer >";
        }

        // ---------------- import ADIF manuel : un onglet par fichier ----------------
        class AdifJob { public string File, Name; }
        Panel pAdif; Label lblAdif; ListBox lbAdif; readonly List<AdifJob> adifJobs = new List<AdifJob>();
        void FillAdifList() { lbAdif.Items.Clear(); foreach (AdifJob j in adifJobs) lbAdif.Items.Add("Onglet \u00ab " + j.Name + " \u00bb  \u2190  " + Path.GetFileName(j.File)); lblAdif.Text = adifJobs.Count == 0 ? "(facultatif)" : adifJobs.Count + " fichier(s)"; }
        void PickAdif()
        {
            using (OpenFileDialog d = new OpenFileDialog { Filter = "ADIF (*.adi;*.adif)|*.adi;*.adif|Tous|*.*", Multiselect = true, Title = "Fichiers ADIF \u00e0 importer" })
            {
                if (d.ShowDialog(this) != DialogResult.OK) return;
                foreach (string f in d.FileNames)
                {
                    if (adifJobs.Exists(x => x.File == f)) continue;
                    string def = Path.GetFileNameWithoutExtension(f), name = Ui.Prompt(this, "Nom du nouvel onglet de journal pour \u00ab " + Path.GetFileName(f) + " \u00bb :", def);
                    if (name == null || name.Trim().Length == 0) continue;   // annule : ce fichier n'est pas retenu
                    string nm0 = name.Trim(); if (nm0.Length > 22) nm0 = nm0.Substring(0, 22).TrimEnd();   // nom d onglet trop long : raccourci
                    string n = nm0; int k = 2; while (adifJobs.Exists(x => string.Equals(x.Name, n, StringComparison.OrdinalIgnoreCase))) n = nm0 + " " + (k++);
                    adifJobs.Add(new AdifJob { File = f, Name = n });
                }
                FillAdifList();
            }
        }
        void RemoveAdif() { int i = lbAdif.SelectedIndex; if (i < 0 || i >= adifJobs.Count) return; adifJobs.RemoveAt(i); FillAdifList(); }
        // ---------------- sauvegarde ADI ----------------
        void DoBackup()
        {
            List<PeerDb> logs = Chosen(); if (logs.Count == 0 || backupBusy) return;
            using (FolderBrowserDialog d = new FolderBrowserDialog { Description = "Dossier où enregistrer les sauvegardes ADI (un fichier par journal)" })
            {
                if (d.ShowDialog(this) != DialogResult.OK) return;
                string dir = d.SelectedPath; backupBusy = true; bBackup.Enabled = false; lblBackup.ForeColor = Color.Black; lblBackup.Text = "Sauvegarde en cours...";
                new Thread(() =>
                {
                    string msg; bool ok = false;
                    try
                    {
                        List<string> files = new List<string>(); long total = 0;
                        foreach (PeerDb db in logs)
                        {
                            string file = Path.Combine(dir, "sauvegarde-journal-" + System.Text.RegularExpressions.Regex.Replace(db.Title, "[^A-Za-z0-9_-]+", "_") + "-" + DateTime.Now.ToString("yyyy-MM-dd") + ".adi"); long n = 0; bool firstBatch = true;
                            using (StreamWriter w = new StreamWriter(file, false, new UTF8Encoding(false)))
                            {
                                Action<List<Qso>> write = b => { string s = Adif.Export(b); if (!firstBatch) { int h = s.IndexOf("<EOH>"); s = s.Substring(h + 5).TrimStart(); } firstBatch = false; w.Write(s); n += b.Count; string t = db.Title; BeginInvoke(new Action(() => lblBackup.Text = "Sauvegarde de « " + t + " » : " + n + " QSO...")); };
                                using (MySqlClient cn = new MySqlClient(db.Host, peerPort > 0 ? peerPort : db.Port, db.User, db.Password, db.Database))
                                    {
                                        cn.Open(); PeerLog l = new PeerLog(cn, db.Database);
                                        for (int off = 0; ; off += 2000) { List<Qso> b = l.Fetch("", "COL_PRIMARY_KEY ASC", off, 2000); if (b.Count == 0) break; write(b); if (b.Count < 2000) break; }
                                    }
                            }
                            files.Add(Path.GetFileName(file) + " (" + n + " QSO)"); total += n;
                        }
                        msg = "Sauvegarde terminée : " + total + " QSO dans " + files.Count + " fichier(s) de " + dir + " :  " + string.Join(" ; ", files.ToArray()); ok = true;
                    }
                    catch (Exception ex) { msg = "Échec de la sauvegarde : " + ex.Message; }
                    BeginInvoke(new Action(() => { backupBusy = false; bBackup.Enabled = true; lblBackup.ForeColor = ok ? Color.DarkGreen : Color.Firebrick; lblBackup.Text = msg; if (ok) kBackup.Checked = true; }));
                }) { IsBackground = true }.Start();
            }
        }

        // ---------------- installation ----------------
        void Log(string s) { BeginInvoke(new Action(() => { log.AppendText(s + "\r\n"); })); }

        void Install()
        {
            working = true; bar.Visible = true; Choice c = Selected(); List<PeerDb> chosen = Chosen(); string call = tCall.Text.Trim().ToUpperInvariant(), grid = tGrid.Text.Trim().ToUpperInvariant();
            string name = tName.Text.Trim(), city = tCity.Text.Trim(), country = tCountry.Text.Trim(), zip = tZip.Text.Trim();
            string host = tHost.Text.Trim(), user = tUser.Text.Trim(), pass = tPass.Text; int port; int.TryParse(tPort.Text, out port); List<AdifJob> adif = new List<AdifJob>(adifJobs);
            new Thread(() =>
            {
                bool ok = false;
                try
                {
                    Ini cfg = App.Cfg; if (c.Kind == "empty") chosen = new List<PeerDb>();
                    PeerDb firstSql = chosen.Find(x => x.IsMySql);
                    // 1) quel serveur ?
                    string dbHost, dbUser, dbPass, mode; int dbPort;
                    if (sysServer != null || firstSql != null)
                    {
                        mode = "external";
                        if (firstSql != null) { dbHost = firstSql.Host; dbPort = peerPort > 0 ? peerPort : firstSql.Port; dbUser = firstSql.User; dbPass = firstSql.Password; }
                        else { dbHost = host; dbPort = port > 0 ? port : 3306; dbUser = user; dbPass = pass; }
                        Log("MariaDB déjà installé : utilisation du serveur " + dbHost + ":" + dbPort + ".");
                    }
                    else
                    {
                        mode = "embedded"; dbHost = "127.0.0.1"; dbUser = "root";
                        if (!Embedded.EnginePresent)
                        {
                            Log("Moteur MariaDB absent du dossier : téléchargement de la version officielle...");
                            Embedded.Release r = Embedded.LatestInBranch("12.3.0"); if (r == null) throw new Exception("Moteur MariaDB introuvable (pas de connexion Internet ?).");
                            InstallEngineFrom(r);
                        }
                        dbPass = cfg.Get("emb.pass", ""); if (dbPass.Length == 0) dbPass = Embedded.RandomPassword();
                        dbPort = cfg.GetInt("emb.port", 0); if (dbPort == 0) dbPort = Embedded.FreePort(3307);
                        if (!Embedded.DataReady) { Log("Création des données MariaDB intégrées..."); Embedded.InitData(dbPass); }
                        cfg.Set("emb.pass", dbPass); cfg.Set("emb.port", dbPort);
                        Log("Démarrage de MariaDB intégré (port " + dbPort + ")...");
                        Embedded.Start(dbPort, dbPass);
                    }
                    // 2) journaux : un onglet par journal
                    List<LogRef> logs = new List<LogRef>();
                    using (MySqlClient dst = new MySqlClient(dbHost, dbPort, dbUser, dbPass, "mysql"))
                    {
                        dst.Open();
                        if (chosen.Count == 0 && adif.Count == 0) { LogRef r = new LogRef { Name = call.Length > 0 ? call : "Journal", Schema = "hamradiolink" }; LogSetup.EnsureTable(dst, r.Schema, Log); logs.Add(r); }
                        bool firstDone = false;
                        foreach (PeerDb d in chosen)
                        {
                            if (c.Kind == "shared" && d == firstSql) { logs.Insert(0, new LogRef { Name = d.Title, Schema = d.Database }); Log("Journal existant « " + d.Title + " » : utilisé directement (structure inchangée)."); continue; }
                            string slug = System.Text.RegularExpressions.Regex.Replace(d.Title.ToLowerInvariant(), "[^a-z0-9]+", "_").Trim('_'); if (slug.Length == 0) slug = "log";
                            LogRef r = new LogRef { Name = d.Title, Schema = "hl_" + slug }; firstDone = true;
                            LogSetup.EnsureTable(dst, r.Schema, Log);
                            if (dst.Scalar("SELECT COUNT(*) FROM " + MySqlClient.Ident(r.Schema) + "." + MySqlClient.Ident(PeerLog.Table)) != "0") { Log("« " + d.Title + " » : le journal contient déjà des QSO, importation ignorée pour ne rien dupliquer."); logs.Add(r); continue; }
                            Log("Importation de « " + d.Title + " » (lecture seule de la source)...");
                            long n; string t = d.Title;
                            using (MySqlClient src = new MySqlClient(d.Host, peerPort > 0 ? peerPort : d.Port, d.User, d.Password, d.Database))
                                {
                                    src.Open(); n = LogSetup.Copy(src, d.Database, dst, r.Schema, (x, tot) => { if (x % 5000 < 250) Log("  " + t + " : " + x + " / " + tot + " QSO copiés"); });
                                }
                            Log("« " + d.Title + " » : " + n + " QSO importés."); logs.Add(r);
                        }
                    }
                    // 2b) fichiers ADIF : chacun dans son propre nouveau journal (onglet), jamais dans le journal partage de l'autre logiciel
                    if (adif.Count > 0)
                    {
                        using (MySqlClient dst2 = new MySqlClient(dbHost, dbPort, dbUser, dbPass, "mysql"))
                        {
                            dst2.Open();
                            foreach (AdifJob j in adif)
                            {
                                string slug = System.Text.RegularExpressions.Regex.Replace(j.Name.ToLowerInvariant(), "[^a-z0-9]+", "_").Trim('_'); if (slug.Length == 0) slug = "adi";
                                LogRef r = new LogRef { Name = j.Name, Schema = "hl_" + slug }; int k = 2; while (logs.Exists(x => x.Schema == r.Schema)) r.Schema = "hl_" + slug + "_" + (k++);
                                LogSetup.EnsureTable(dst2, r.Schema, Log); PeerLog tl = new PeerLog(dst2, r.Schema);
                                Log("Import ADIF \u00ab " + Path.GetFileName(j.File) + " \u00bb dans le nouvel onglet \u00ab " + j.Name + " \u00bb...");
                                int a, du; AdifImport.Run(tl, j.File, Log, out a, out du); Log("  " + a + " QSO import\u00e9s, " + du + " doublons ignor\u00e9s."); logs.Add(r);
                            }
                        }
                    }                    // 3) reglages
                    cfg.Set("db.mode", mode); cfg.Set("db.host", dbHost); cfg.Set("db.port", dbPort); cfg.Set("db.user", dbUser); cfg.Set("db.pass", dbPass);
                    // le journal principal (premier onglet) : le MariaDB de l'autre logiciel en priorite
                    LogRef first = logs.Find(x => firstSql != null && x.Name == firstSql.Title) ?? logs[0]; logs.Remove(first); logs.Insert(0, first);
                    App.Logs = logs; LogRef.SaveAll(logs); App.CurrentLog = first; cfg.Set("log.current", first.Name); cfg.Set("db.schema", first.Schema); cfg.Set("db.name", first.Name);
                    if (peer.Found) { peer.Apply(cfg, false); Log("Réglages repris de l'autre logiciel : " + peer.Settings.Count + " (station, recherche d'indicatifs, cluster)."); foreach (string n in peer.Notes) Log("  " + n); }
                    cfg.Set("me.call", call); cfg.Set("me.grid", grid); cfg.Set("me.name", name); cfg.Set("me.city", city); cfg.Set("me.country", country); cfg.Set("me.zip", zip);
                    cfg.Set("setup.done", true); cfg.Save();
                    App.ConnectDb(); Log("Connexion au journal « " + first.Name + " » : " + App.Log.Count("") + " QSO." + (logs.Count > 1 ? " Les " + (logs.Count - 1) + " autre(s) journal(aux) sont des onglets en haut du Logbook." : ""));
                    Log(""); Log("Installation terminée. Cliquez sur Terminer.");
                    ok = true;
                }
                catch (Exception ex) { Log(""); Log("ERREUR : " + ex.Message); Log("Vous pouvez fermer cette fenêtre et relancer l'assistant (Help > Setup assistant)."); }
                BeginInvoke(new Action(() => { working = false; bar.Visible = false; bCancel.Enabled = true; bCancel.Text = "Fermer"; bNext.Enabled = ok; }));
            }) { IsBackground = true }.Start();
        }

        void InstallEngineFrom(Embedded.Release r)
        {
            // Premier téléchargement du moteur : même procédure que la mise à jour, sans serveur à arrêter.
            string tmp = Path.Combine(Path.GetTempPath(), "hrl-mariadb-" + r.Version); Directory.CreateDirectory(tmp); string zip = Path.Combine(tmp, r.FileName);
            System.Net.ServicePointManager.SecurityProtocol = (System.Net.SecurityProtocolType)3072 | (System.Net.SecurityProtocolType)768 | System.Net.SecurityProtocolType.Tls;
            using (System.Net.WebClient w = new System.Net.WebClient()) w.DownloadFile(r.Url, zip);
            string sha; using (FileStream fs = File.OpenRead(zip)) using (System.Security.Cryptography.SHA256 h = System.Security.Cryptography.SHA256.Create()) sha = BitConverter.ToString(h.ComputeHash(fs)).Replace("-", "").ToLowerInvariant();
            if (sha != r.Sha256) throw new Exception("Somme de contrôle incorrecte : téléchargement rejeté.");
            using (System.IO.Compression.ZipArchive za = System.IO.Compression.ZipFile.OpenRead(zip))
                foreach (System.IO.Compression.ZipArchiveEntry e in za.Entries)
                {
                    int i = e.FullName.IndexOf('/'); if (i < 0 || e.Length == 0) continue; string rel = e.FullName.Substring(i + 1);
                    bool keep = rel.StartsWith("share/") || rel == "COPYING" || (rel.StartsWith("bin/") && Array.IndexOf(Embedded.EngineFiles, rel.Substring(4)) >= 0);
                    if (!keep) continue; string dst = Path.Combine(Embedded.EngineDir, rel.Replace('/', '\\')); Directory.CreateDirectory(Path.GetDirectoryName(dst)); System.IO.Compression.ZipFileExtensions.ExtractToFile(e, dst, true);
                }
            if (!File.Exists(Embedded.Bin("mysqld.exe"))) File.Copy(Embedded.Bin("mariadbd.exe"), Embedded.Bin("mysqld.exe"));
            Log("Moteur MariaDB " + r.Version + " installé.");
        }
    }
}
