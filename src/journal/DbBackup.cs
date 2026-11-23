using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace HamRadioLink.Journal
{
    // Sauvegarde de la base MariaDB du journal avec l'outil de sauvegarde du serveur (mysqldump / mariadb-dump) : un fichier .sql daté par journal,
    // dans le dossier choisi, avec un nombre de copies conservé. Automatique (à la fermeture / chaque jour / semaine / mois) si la case est cochée.
    // Réglages settings.ini : bak.dir, bak.keep, bak.auto (0/1), bak.mode (0 fermeture, 1 jour, 2 semaine, 3 mois), bak.log ("" = tous), bak.dump (outil choisi à la main), bak.last.<journal>.
    public static class DbBackup
    {
        public static readonly string[] ModeNames = { "À la fermeture du programme", "Chaque jour", "Chaque semaine", "Chaque mois" };
        static volatile bool busy; static System.Windows.Forms.Timer tm;
        public static string Dir { get { string d = App.Cfg.Get("bak.dir", ""); return d.Length > 0 ? d : Path.Combine(App.DataDir, "sauvegardes"); } }
        public static int Keep { get { return Math.Max(1, App.Cfg.GetInt("bak.keep", 7)); } }

        // outil de sauvegarde : réglage manuel, moteur intégré, dossier du service Windows MariaDB/MySQL, Program Files
        public static string FindDump() { return FindTool(true); }
        public static string FindClient() { return FindTool(false); }
        static string FindTool(bool dumpTool)
        {
            string m = App.Cfg.Get("bak.dump", ""); if (m.Length > 0 && File.Exists(m)) { if (dumpTool) return m; string md = Path.GetDirectoryName(m); foreach (string x in new string[] { "mariadb.exe", "mysql.exe" }) if (File.Exists(Path.Combine(md, x))) return Path.Combine(md, x); }
            List<string> dirs = new List<string>();
            try { dirs.Add(Path.Combine(Embedded.EngineDir, "bin")); } catch { }
            try
            {
                using (Microsoft.Win32.RegistryKey svc = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services"))
                    if (svc != null)
                        foreach (string n in svc.GetSubKeyNames())
                        {
                            if (n.IndexOf("maria", StringComparison.OrdinalIgnoreCase) < 0 && n.IndexOf("mysql", StringComparison.OrdinalIgnoreCase) < 0) continue;
                            using (Microsoft.Win32.RegistryKey k = svc.OpenSubKey(n))
                            {
                                string img = (k == null ? null : k.GetValue("ImagePath") as string) ?? ""; img = img.Trim();
                                string exe = img.StartsWith("\"") ? (img.IndexOf('"', 1) > 0 ? img.Substring(1, img.IndexOf('"', 1) - 1) : img.Substring(1)) : (img.IndexOf(".exe", StringComparison.OrdinalIgnoreCase) > 0 ? img.Substring(0, img.IndexOf(".exe", StringComparison.OrdinalIgnoreCase) + 4) : img);
                                try { if (exe.Length > 0) dirs.Add(Path.GetDirectoryName(exe)); } catch { }
                            }
                        }
            }
            catch { }
            foreach (string pf in new string[] { Environment.GetEnvironmentVariable("ProgramFiles"), Environment.GetEnvironmentVariable("ProgramFiles(x86)") })
            {
                if (string.IsNullOrEmpty(pf)) continue;
                try { foreach (string d in Directory.GetDirectories(pf)) { string n = Path.GetFileName(d); if (n.StartsWith("MariaDB", StringComparison.OrdinalIgnoreCase) || n.StartsWith("MySQL", StringComparison.OrdinalIgnoreCase)) dirs.Add(Path.Combine(d, "bin")); } } catch { }
            }
            foreach (string d in dirs) foreach (string x in dumpTool ? new string[] { "mariadb-dump.exe", "mysqldump.exe" } : new string[] { "mariadb.exe", "mysql.exe" }) { try { string p = Path.Combine(d, x); if (File.Exists(p)) return p; } catch { } }
            return null;
        }

        static string Safe(string s) { foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_'); return s.Trim().Length == 0 ? "journal" : s.Trim(); }

        // sauvegarde un journal ; renvoie un message (null si erreur de format impossible). ok = réussite.
        public static string One(LogRef l, out bool ok, bool prune = true)
        {
            ok = false; string dump = FindDump();
            if (dump == null) return "Outil de sauvegarde (mysqldump) introuvable : indiquez son emplacement dans la fenêtre.";
            string host, user, pass; int port; App.ServerOf(l, out host, out port, out user, out pass);
            string dir = Dir; Directory.CreateDirectory(dir);
            string name = Safe(l.Name), file = Path.Combine(dir, name + "-" + DateTime.Now.ToString("yyyy-MM-dd_HHmm") + ".sql"), cnf = Path.Combine(Path.GetTempPath(), "hrl-" + Guid.NewGuid().ToString("N") + ".cnf");
            try
            {
                File.WriteAllText(cnf, "[client]\nuser=\"" + user.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"\n" + (pass.Length > 0 ? "password=\"" + pass.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"\n" : ""), new UTF8Encoding(false));
                ProcessStartInfo psi = new ProcessStartInfo(dump, "--defaults-extra-file=\"" + cnf + "\" --protocol=tcp --host=" + host + " --port=" + port + " --single-transaction --routines --default-character-set=utf8mb4 --result-file=\"" + file + "\" " + l.Schema);
                psi.UseShellExecute = false; psi.CreateNoWindow = true; psi.RedirectStandardError = true; psi.RedirectStandardOutput = true;
                using (Process p = Process.Start(psi))
                {
                    string err = p.StandardError.ReadToEnd(); p.StandardOutput.ReadToEnd();
                    if (!p.WaitForExit(600000)) { try { p.Kill(); } catch { } try { File.Delete(file); } catch { } return l.Name + " : délai dépassé."; }
                    if (p.ExitCode != 0 || !File.Exists(file)) { try { File.Delete(file); } catch { } return l.Name + " : échec (" + err.Trim() + ")."; }
                }
                App.Cfg.Set("bak.last." + l.Name, DateTime.Now.Ticks.ToString()); App.Cfg.Save();
                int del = prune ? Prune(dir, name) : 0; ok = true;
                return l.Name + " : " + Path.GetFileName(file) + " (" + (new FileInfo(file).Length / 1024) + " Ko)" + (del > 0 ? ", " + del + " ancienne(s) copie(s) supprimée(s)" : "") + ".";
            }
            catch (Exception ex) { return l.Name + " : " + ex.Message; }
            finally { try { File.Delete(cnf); } catch { } }
        }

        // ne garde que les N copies les plus récentes de ce journal
        static int Prune(string dir, string name)
        {
            int n = 0; try
            {
                List<string> fs = new List<string>(Directory.GetFiles(dir, name + "-????-??-??_????.sql")); fs.Sort(StringComparer.Ordinal); fs.Reverse();
                for (int i = Keep; i < fs.Count; i++) { try { File.Delete(fs[i]); n++; } catch { } }
            }
            catch { }
            return n;
        }

        public static List<LogRef> Targets() { List<LogRef> r = new List<LogRef>(); string w = App.Cfg.Get("bak.log", ""); if (App.Logs != null) foreach (LogRef l in App.Logs) if (w.Length == 0 || l.Name == w) r.Add(l); return r; }

        public static void All(Action<string> say)
        {
            foreach (LogRef l in Targets()) { bool ok; string m = One(l, out ok); if (say != null) say((ok ? "OK — " : "ERREUR — ") + m); }
        }

        // restaure un fichier .sql dans le journal l (les tables du fichier remplacent celles du journal) ; renvoie un message, ok = réussite
        public static string Restore(LogRef l, string sql, out bool ok)
        {
            ok = false; string cli = FindClient(); if (cli == null) return "Client MariaDB (mariadb.exe / mysql.exe) introuvable : indiquez l'emplacement de mysqldump dans la fenêtre de sauvegarde, le client doit être dans le même dossier.";
            string host, user, pass; int port; App.ServerOf(l, out host, out port, out user, out pass);
            string cnf = Path.Combine(Path.GetTempPath(), "hrl-" + Guid.NewGuid().ToString("N") + ".cnf");
            try
            {
                File.WriteAllText(cnf, "[client]\nuser=\"" + user.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"\n" + (pass.Length > 0 ? "password=\"" + pass.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"\n" : ""), new UTF8Encoding(false));
                ProcessStartInfo psi = new ProcessStartInfo(cli, "--defaults-extra-file=\"" + cnf + "\" --protocol=tcp --host=" + host + " --port=" + port + " --default-character-set=utf8mb4 " + l.Schema);
                psi.UseShellExecute = false; psi.CreateNoWindow = true; psi.RedirectStandardInput = true; psi.RedirectStandardError = true; psi.RedirectStandardOutput = true;
                using (Process p = Process.Start(psi))
                {
                    System.Text.StringBuilder err = new System.Text.StringBuilder();
                    p.ErrorDataReceived += (s, e) => { if (e.Data != null) err.AppendLine(e.Data); }; p.BeginErrorReadLine(); p.OutputDataReceived += (s, e) => { }; p.BeginOutputReadLine();
                    using (FileStream fs = File.OpenRead(sql)) { try { fs.CopyTo(p.StandardInput.BaseStream); p.StandardInput.Close(); } catch { } }
                    if (!p.WaitForExit(1800000)) { try { p.Kill(); } catch { } return l.Name + " : délai dépassé."; }
                    p.WaitForExit();
                    if (p.ExitCode != 0) return l.Name + " : échec (" + err.ToString().Trim() + ").";
                }
                ok = true; return l.Name + " : restauration de " + Path.GetFileName(sql) + " terminée.";
            }
            catch (Exception ex) { return l.Name + " : " + ex.Message; }
            finally { try { File.Delete(cnf); } catch { } }
        }

        // ---- sauvegarde automatique ----
        public static void Attach()
        {
            if (tm != null) return;
            tm = new System.Windows.Forms.Timer(); tm.Interval = 600000; tm.Tick += delegate { Check(); }; tm.Start();
            System.Windows.Forms.Timer first = new System.Windows.Forms.Timer { Interval = 60000 }; first.Tick += delegate { first.Stop(); first.Dispose(); Check(); }; first.Start();
        }
        static bool Due(LogRef l, int mode)
        {
            long t; DateTime last; if (!long.TryParse(App.Cfg.Get("bak.last." + l.Name, ""), out t)) return true; last = new DateTime(t);
            if (mode == 1) return last.Date < DateTime.Today; if (mode == 2) return (DateTime.Now - last).TotalDays >= 7; if (mode == 3) return last.AddMonths(1) <= DateTime.Now; return false;
        }
        static void Check()
        {
            try
            {
                if (busy || !App.Cfg.GetBool("bak.auto", false)) return; int mode = App.Cfg.GetInt("bak.mode", 1); if (mode < 1) return;
                List<LogRef> due = Targets().FindAll(l => Due(l, mode)); if (due.Count == 0) return;
                busy = true;
                new Thread(() => { try { foreach (LogRef l in due) { bool ok; string m = One(l, out ok); CatLog.Event("sauvegarde", (ok ? "OK — " : "ERREUR — ") + m); } } catch { } finally { busy = false; } }) { IsBackground = true }.Start();
            }
            catch { }
        }
        // arrêt du programme : sauvegarde « à la fermeture » (on attend la fin, avant l'arrêt de la base)
        public static void OnQuit()
        {
            try
            {
                if (!App.Cfg.GetBool("bak.auto", false) || App.Cfg.GetInt("bak.mode", 1) != 0) return;
                int w = 0; while (busy && w++ < 100) Thread.Sleep(100);
                busy = true; try { All(null); } finally { busy = false; }
            }
            catch { }
        }
    }

    // Fenêtre non bloquante : réglages + bouton « Sauvegarder maintenant ».
    public class DbBackupForm : Form
    {
        readonly ComboBox cbLog, cbMode; readonly TextBox tbDir, tbDump, tbKeep, log; readonly CheckBox auto; readonly Button go;
        public DbBackupForm(LogbookForm owner)
        {
            AutoScaleMode = AutoScaleMode.None; Text = Tr.T("Sauvegarder la base"); FormBorderStyle = FormBorderStyle.FixedDialog; StartPosition = FormStartPosition.CenterParent; MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false; Font = Ui.Normal; ClientSize = new Size(640, 470);
            int y = 14; Ui.Lbl(this, Tr.T("Journal à sauvegarder :"), 12, y + 3, 190);
            List<string> nm = new List<string>(); nm.Add(Tr.T("Tous les journaux")); if (App.Logs != null) foreach (LogRef l in App.Logs) nm.Add(l.Name);
            cbLog = Ui.Combo(this, 210, y, 300, nm); cbLog.DropDownStyle = ComboBoxStyle.DropDownList; { string w = App.Cfg.Get("bak.log", ""); int i = w.Length == 0 ? 0 : nm.IndexOf(w); cbLog.SelectedIndex = i < 0 ? 0 : i; }
            y += 36; Ui.Lbl(this, Tr.T("Dossier des sauvegardes :"), 12, y + 3, 190); tbDir = Ui.Txt(this, 210, y, 330); tbDir.Text = DbBackup.Dir;
            Ui.Btn(this, "...", 548, y - 1, 80, 26, (s, e) => { using (FolderBrowserDialog d = new FolderBrowserDialog { SelectedPath = tbDir.Text, Description = Tr.T("Dossier des sauvegardes") }) if (d.ShowDialog(this) == DialogResult.OK) tbDir.Text = d.SelectedPath; });
            y += 36; Ui.Lbl(this, Tr.T("Copies à conserver (par journal) :"), 12, y + 3, 190); tbKeep = Ui.Txt(this, 210, y, 60); tbKeep.Text = DbBackup.Keep.ToString();
            y += 36; Ui.Lbl(this, Tr.T("Outil mysqldump :"), 12, y + 3, 190); tbDump = Ui.Txt(this, 210, y, 330); string f = DbBackup.FindDump(); tbDump.Text = App.Cfg.Get("bak.dump", "").Length > 0 ? App.Cfg.Get("bak.dump", "") : (f ?? "");
            Ui.Btn(this, "...", 548, y - 1, 80, 26, (s, e) => { using (OpenFileDialog d = new OpenFileDialog { Filter = "mysqldump / mariadb-dump|mysqldump.exe;mariadb-dump.exe|*.exe|*.exe" }) if (d.ShowDialog(this) == DialogResult.OK) tbDump.Text = d.FileName; });
            y += 40; auto = Ui.Chk(this, Tr.T("Sauvegarde automatique :"), 12, y, 200); auto.Checked = App.Cfg.GetBool("bak.auto", false);
            List<string> md = new List<string>(); foreach (string s in DbBackup.ModeNames) md.Add(Tr.T(s)); cbMode = Ui.Combo(this, 210, y - 3, 300, md); cbMode.DropDownStyle = ComboBoxStyle.DropDownList; cbMode.SelectedIndex = Math.Max(0, Math.Min(3, App.Cfg.GetInt("bak.mode", 1))); cbMode.Enabled = auto.Checked;
            auto.CheckedChanged += (s, e) => cbMode.Enabled = auto.Checked;
            y += 40; go = Ui.Btn(this, Tr.T("Sauvegarder maintenant"), 12, y, 200, 30, (s, e) => Now()); Ui.Btn(this, Tr.T("Enregistrer les réglages"), 220, y, 200, 30, (s, e) => { Save(); log.AppendText(Tr.T("Réglages enregistrés.") + "\r\n"); }); Ui.Btn(this, Tr.T("Fermer"), 536, y, 92, 30, (s, e) => Close());
            y += 42; log = new TextBox { Left = 12, Top = y, Width = 616, Height = 470 - y - 12, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Font = Ui.Mono }; Controls.Add(log);
            log.Text = Tr.T("La sauvegarde est un fichier .sql daté (structure + QSO), restaurable avec le client MariaDB.") + "\r\n";
            FormClosing += (s, e) => { try { Save(); } catch { } };
        }
        void Save()
        {
            int k; if (!int.TryParse(tbKeep.Text.Trim(), out k) || k < 1) k = 7;
            App.Cfg.Set("bak.log", cbLog.SelectedIndex <= 0 ? "" : (string)cbLog.SelectedItem); App.Cfg.Set("bak.dir", tbDir.Text.Trim()); App.Cfg.Set("bak.keep", k.ToString());
            string found = DbBackup.FindDump(); App.Cfg.Set("bak.dump", tbDump.Text.Trim() == found ? App.Cfg.Get("bak.dump", "") : tbDump.Text.Trim());
            App.Cfg.Set("bak.auto", auto.Checked ? "1" : "0"); App.Cfg.Set("bak.mode", cbMode.SelectedIndex.ToString()); App.Cfg.Save();
        }
        void Now()
        {
            Save(); go.Enabled = false; log.AppendText(Tr.T("Sauvegarde en cours...") + "\r\n");
            new Thread(() =>
            {
                try { DbBackup.All(m => { try { BeginInvoke(new Action(() => log.AppendText(m + "\r\n"))); } catch { } }); }
                catch (Exception ex) { try { BeginInvoke(new Action(() => log.AppendText(ex.Message + "\r\n"))); } catch { } }
                try { BeginInvoke(new Action(() => { go.Enabled = true; log.AppendText(Tr.T("Terminé.") + "\r\n"); })); } catch { }
            }) { IsBackground = true }.Start();
        }
    }

    // Fenêtre non bloquante : restaure un fichier .sql dans un journal. Le dossier des sauvegardes est proposé en premier, un autre chemin peut être choisi.
    public class DbRestoreForm : Form
    {
        readonly LogbookForm owner; readonly ComboBox cbLog; readonly TextBox tbDir, log; readonly ListBox lst; readonly Button go; readonly List<string> files = new List<string>();
        public DbRestoreForm(LogbookForm o)
        {
            owner = o; AutoScaleMode = AutoScaleMode.None; Text = Tr.T("Restaurer la base"); FormBorderStyle = FormBorderStyle.FixedDialog; StartPosition = FormStartPosition.CenterParent; MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false; Font = Ui.Normal; ClientSize = new Size(660, 500);
            int y = 14; Ui.Lbl(this, Tr.T("Restaurer dans le journal :"), 12, y + 3, 200);
            List<string> nm = new List<string>(); if (App.Logs != null) foreach (LogRef l in App.Logs) nm.Add(l.Name);
            cbLog = Ui.Combo(this, 220, y, 300, nm); cbLog.DropDownStyle = ComboBoxStyle.DropDownList; cbLog.SelectedIndex = Math.Max(0, nm.IndexOf(App.CurrentLog == null ? "" : App.CurrentLog.Name));
            y += 36; Ui.Lbl(this, Tr.T("Dossier des sauvegardes :"), 12, y + 3, 200); tbDir = Ui.Txt(this, 220, y, 330); tbDir.Text = DbBackup.Dir;
            Ui.Btn(this, Tr.T("Parcourir..."), 558, y - 1, 90, 26, (s, e) => { using (FolderBrowserDialog d = new FolderBrowserDialog { SelectedPath = tbDir.Text, Description = Tr.T("Dossier contenant les sauvegardes") }) if (d.ShowDialog(this) == DialogResult.OK) { tbDir.Text = d.SelectedPath; Fill(); } });
            tbDir.Leave += (s, e) => Fill();
            y += 36; lst = new ListBox { Left = 12, Top = y, Width = 636, Height = 190, HorizontalScrollbar = true }; Controls.Add(lst);
            y += 198; Ui.Btn(this, Tr.T("Autre fichier .sql..."), 12, y, 170, 28, (s, e) => { using (OpenFileDialog d = new OpenFileDialog { Filter = "SQL (*.sql)|*.sql|*.*|*.*", InitialDirectory = Directory.Exists(tbDir.Text) ? tbDir.Text : null }) if (d.ShowDialog(this) == DialogResult.OK) { files.Insert(0, d.FileName); lst.Items.Insert(0, d.FileName); lst.SelectedIndex = 0; } });
            go = Ui.Btn(this, Tr.T("Restaurer maintenant"), 380, y, 170, 28, (s, e) => Go()); Ui.Btn(this, Tr.T("Fermer"), 558, y, 90, 28, (s, e) => Close());
            y += 38; log = new TextBox { Left = 12, Top = y, Width = 636, Height = 500 - y - 12, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Font = Ui.Mono }; Controls.Add(log);
            log.Text = Tr.T("Les tables du fichier remplacent celles du journal choisi. Une sauvegarde du journal est faite juste avant.") + "\r\n";
            Fill();
        }
        void Fill()
        {
            lst.Items.Clear(); files.Clear();
            try
            {
                if (Directory.Exists(tbDir.Text)) { List<string> fs = new List<string>(Directory.GetFiles(tbDir.Text, "*.sql")); fs.Sort((a, b) => File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a))); foreach (string f in fs) { files.Add(f); lst.Items.Add(Path.GetFileName(f) + "   (" + File.GetLastWriteTime(f).ToString("dd/MM/yyyy HH:mm") + ", " + (new FileInfo(f).Length / 1024) + " Ko)"); } }
            }
            catch { }
            if (lst.Items.Count > 0) lst.SelectedIndex = 0;
        }
        void Go()
        {
            if (lst.SelectedIndex < 0 || lst.SelectedIndex >= files.Count) { log.AppendText(Tr.T("Choisissez un fichier de sauvegarde.") + "\r\n"); return; }
            LogRef l = App.Logs.Find(x => x.Name == (string)cbLog.SelectedItem); if (l == null) return; string file = files[lst.SelectedIndex];
            if (MessageBox.Show(this, Tr.T("Restaurer « ") + Path.GetFileName(file) + Tr.T(" » dans le journal « ") + l.Name + Tr.T(" » ?\r\n\r\nLes QSO actuels de ce journal seront remplacés par ceux du fichier. Une sauvegarde du journal est faite avant."), Tr.T("Restaurer la base"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            go.Enabled = false; log.AppendText(Tr.T("Restauration en cours...") + "\r\n"); bool isCur = l == App.CurrentLog;
            new Thread(() =>
            {
                bool ok; string m = "";
                try
                {
                    string pre = DbBackup.One(l, out ok, false); say((ok ? "Sauvegarde préalable : " : "ERREUR sauvegarde préalable — ") + pre);
                    if (!ok) { say("Restauration annulée (la sauvegarde préalable a échoué)."); }
                    else { m = DbBackup.Restore(l, file, out ok); say((ok ? "OK — " : "ERREUR — ") + m); if (ok && isCur) try { BeginInvoke(new Action(() => { try { App.ConnectTo(l); owner.ReloadFromOutside(); } catch (Exception ex) { log.AppendText(ex.Message + "\r\n"); } })); } catch { } }
                }
                catch (Exception ex) { say(ex.Message); }
                try { BeginInvoke(new Action(() => go.Enabled = true)); } catch { }
            }) { IsBackground = true }.Start();
        }
        void say(string m) { try { BeginInvoke(new Action(() => log.AppendText(m + "\r\n"))); } catch { } }
    }
}
