using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace HamRadioLink
{
    // Mise à jour depuis la dernière version publiée sur GitHub : seul le programme (l'exe) est remplacé ; le dossier data (réglages, dispositions, commandes, journal)
    // reste en place. L'ancien exe est conservé sous le nom HamRadioLink-<ancienne version>.exe. Réglage : Paramètres > Mise à jour.
    static class Updater
    {
        public static bool Disabled;                 // essais automatiques (autotest, copie d'écran) : aucune vérification
        public static string ApiOverride;            // pour les essais : adresse (file:// ou http) à la place de GitHub
        static bool aa; public static bool AutoAccept { get { return aa || Environment.GetEnvironmentVariable("HRL_UPDATE_AUTO") == "1"; } set { aa = value; } }   // pour les essais : accepte sans fenêtre
        public const string Repo = "F5pbgLudovic/HamRadioLink";
        public static bool AutoOn { get { return Cfg.GetBool("maj.auto", false); } }
        public class Info { public string Tag = "", Version = "", Url = "", Notes = ""; public long Size; }

        static string Api
        {
            get
            {
                string o = ApiOverride ?? Environment.GetEnvironmentVariable("HRL_UPDATE_API");
                return !string.IsNullOrEmpty(o) ? o : "https://api.github.com/repos/" + Repo + "/releases/latest";
            }
        }
        static WebClient Web()
        {
            try { ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | (SecurityProtocolType)768 | ServicePointManager.SecurityProtocol; } catch { }
            WebClient w = new WebClient(); w.Encoding = Encoding.UTF8; w.Headers[HttpRequestHeader.UserAgent] = "HamRadioLink-updater"; w.Headers[HttpRequestHeader.Accept] = "application/vnd.github+json"; return w;
        }

        // « v1.10 » > « 1.9 » : comparaison numérique par morceaux
        public static int Compare(string a, string b)
        {
            int[] x = Parts(a), y = Parts(b);
            for (int i = 0; i < Math.Max(x.Length, y.Length); i++) { int p = i < x.Length ? x[i] : 0, q = i < y.Length ? y[i] : 0; if (p != q) return p < q ? -1 : 1; }
            return 0;
        }
        static int[] Parts(string s)
        {
            List<int> l = new List<int>(); foreach (string p in (s ?? "").TrimStart('v', 'V', ' ').Split('.')) { Match m = Regex.Match(p, @"^\d+"); int n; l.Add(m.Success && int.TryParse(m.Value, out n) ? n : 0); } return l.ToArray();
        }

        // Lit la dernière version publiée ; null + message si impossible
        public static Info Check(out string err)
        {
            err = null;
            try
            {
                string json; using (WebClient w = Web()) json = w.DownloadString(Api);
                return Parse(json, out err);
            }
            catch (Exception ex) { err = "Vérification impossible : " + ex.Message; return null; }
        }
        public static Info Parse(string json, out string err)
        {
            err = null; Info i = new Info();
            Match t = Regex.Match(json, "\"tag_name\"\\s*:\\s*\"([^\"]+)\""); if (!t.Success) { err = "Réponse inattendue (pas de numéro de version)."; return null; }
            i.Tag = t.Groups[1].Value; i.Version = i.Tag.TrimStart('v', 'V');
            foreach (Match m in Regex.Matches(json, "\"browser_download_url\"\\s*:\\s*\"([^\"]+)\"")) { string u = m.Groups[1].Value.Replace("\\/", "/"); if (u.EndsWith("/HamRadioLink.exe", StringComparison.OrdinalIgnoreCase)) { i.Url = u; break; } }
            Match s = Regex.Match(json, "\"size\"\\s*:\\s*(\\d+)[^{}]*\"browser_download_url\"\\s*:\\s*\"[^\"]*HamRadioLink\\.exe\""); if (s.Success) long.TryParse(s.Groups[1].Value, out i.Size);
            Match b = Regex.Match(json, "\"body\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"");
            if (b.Success) { string n = b.Groups[1].Value.Replace("\\r", "").Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\/", "/").Replace("\\\\", "\\"); i.Notes = n.Length > 900 ? n.Substring(0, 900) + "…" : n; }
            if (i.Url.Length == 0) { err = "La version " + i.Version + " ne contient pas HamRadioLink.exe."; return null; }
            return i;
        }
        public static bool IsNewer(Info i) { return i != null && Compare(i.Version, MainForm.Version) > 0; }

        // Télécharge le nouvel exe à côté de l'ancien (HamRadioLink.maj) ; vérifie qu'il ressemble à un programme Windows
        public static string Download(Info i, string dir, Action<int> progress, out string err)
        {
            err = null; string dst = Path.Combine(dir, "HamRadioLink.maj");
            try
            {
                using (WebClient w = Web())
                {
                    ManualResetEvent done = new ManualResetEvent(false); Exception fail = null;
                    if (progress != null) w.DownloadProgressChanged += delegate(object s, DownloadProgressChangedEventArgs e) { progress(e.ProgressPercentage); };
                    w.DownloadFileCompleted += delegate(object s, System.ComponentModel.AsyncCompletedEventArgs e) { fail = e.Error; done.Set(); };
                    w.DownloadFileAsync(new Uri(i.Url), dst); while (!done.WaitOne(40)) Application.DoEvents();
                    if (fail != null) throw fail;
                }
                FileInfo f = new FileInfo(dst); byte[] head = new byte[2]; using (FileStream fs = File.OpenRead(dst)) fs.Read(head, 0, 2);
                if (f.Length < 500000 || head[0] != 'M' || head[1] != 'Z') { try { File.Delete(dst); } catch { } err = "Le fichier téléchargé n'est pas un programme valide."; return null; }
                if (i.Size > 0 && f.Length != i.Size) { try { File.Delete(dst); } catch { } err = "Téléchargement incomplet (" + f.Length + " / " + i.Size + " octets)."; return null; }
                return dst;
            }
            catch (Exception ex) { try { if (File.Exists(dst)) File.Delete(dst); } catch { } err = "Téléchargement impossible : " + ex.Message; return null; }
        }

        // Fichiers de personnalisation : réglages du logiciel / dispositions et commandes des postes
        public static List<string> SettingsFiles(string dir) { List<string> l = new List<string>(); string f = Path.Combine(dir, "settings.ini"); if (File.Exists(f)) l.Add(f); return l; }
        public static List<string> PostFiles(string dir)
        {
            List<string> l = new List<string>(); if (!Directory.Exists(dir)) return l;
            foreach (string p in new[] { "*.layout.ini", "*.cat.ini", "*.custom.ini" }) l.AddRange(Directory.GetFiles(dir, p)); return l;
        }
        // Copie de sécurité de tout ce qui est modifiable (dossier data\sauvegarde-avant-MAJ-<date>) ; retourne le dossier
        public static string BackupData(string dir)
        {
            string b = Path.Combine(dir, "sauvegarde-avant-MAJ-" + DateTime.Now.ToString("yyyy-MM-dd_HHmmss")); Directory.CreateDirectory(b);
            foreach (string f in Directory.GetFiles(dir, "*.ini")) File.Copy(f, Path.Combine(b, Path.GetFileName(f)), true);
            return b;
        }

        // Au démarrage : ne garde que la version précédente parmi les anciens programmes laissés par les mises à jour (HamRadioLink-1.12.exe, HamRadioLink-1.12-153012.exe...) ; jamais le programme en cours
        public static void CleanOld()
        {
            try
            {
                string dir = AppDomain.CurrentDomain.BaseDirectory, me = Path.GetFileName(Application.ExecutablePath);
                List<KeyValuePair<Version, string>> old = new List<KeyValuePair<Version, string>>();
                foreach (string f in Directory.GetFiles(dir, "HamRadioLink-*.exe"))
                {
                    string n = Path.GetFileName(f); if (string.Equals(n, me, StringComparison.OrdinalIgnoreCase)) continue;
                    System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(n, @"^HamRadioLink-(\d+(?:\.\d+)+)(?:-(\d{6}))?\.exe$", System.Text.RegularExpressions.RegexOptions.IgnoreCase); Version v;
                    if (m.Success && Version.TryParse(m.Groups[1].Value, out v)) old.Add(new KeyValuePair<Version, string>(v, f));
                }
                // on garde seulement la version précédente (la plus récente de celles qui restent, copie la plus récente si plusieurs) comme second exemplaire
                old.Sort((a, b) => { int c = b.Key.CompareTo(a.Key); return c != 0 ? c : File.GetLastWriteTimeUtc(b.Value).CompareTo(File.GetLastWriteTimeUtc(a.Value)); });
                for (int i = 1; i < old.Count; i++) try { File.Delete(old[i].Value); } catch { }
            }
            catch { }
        }

        public static string BuildScript(int pid, string exe, List<string> toDelete)
        {
            StringBuilder sb = new StringBuilder(); sb.AppendLine("@echo off"); sb.AppendLine("chcp 65001 >nul");
            sb.AppendLine(":w"); sb.AppendLine("tasklist /FI \"PID eq " + pid + "\" 2>nul | find \"" + pid + "\" >nul && (ping -n 2 127.0.0.1 >nul & goto w)");
            foreach (string f in toDelete) sb.AppendLine("del /f /q \"" + f + "\" 2>nul");
            sb.AppendLine("start \"\" \"" + exe + "\""); sb.AppendLine("(goto) 2>nul & del \"%~f0\"");
            return sb.ToString();
        }

        // Remplace l'exe : l'ancien est renommé HamRadioLink-<version>.exe (permis même ouvert), le nouveau prend sa place ; un script attend la fin du programme puis le relance.
        public static bool Apply(string exe, string newFile, string oldVersion, List<string> toDelete, bool launch, out string backup, out string err)
        {
            err = null; backup = null; string dir = Path.GetDirectoryName(exe);
            try
            {
                string bk = Path.Combine(dir, "HamRadioLink-" + oldVersion + ".exe"); if (File.Exists(bk)) bk = Path.Combine(dir, "HamRadioLink-" + oldVersion + "-" + DateTime.Now.ToString("HHmmss") + ".exe");
                File.Move(exe, bk);
                try { File.Move(newFile, exe); } catch (Exception ex) { try { File.Move(bk, exe); } catch { } err = "Remplacement impossible : " + ex.Message; return false; }
                backup = bk;
                if (launch)
                {
                    string cmd = Path.Combine(Path.GetTempPath(), "hamradiolink-maj.cmd"); File.WriteAllText(cmd, BuildScript(Process.GetCurrentProcess().Id, exe, toDelete), new UTF8Encoding(false));
                    ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", "/c \"" + cmd + "\""); psi.CreateNoWindow = true; psi.UseShellExecute = false; psi.WindowStyle = ProcessWindowStyle.Hidden; Process.Start(psi);
                }
                return true;
            }
            catch (Exception ex) { err = "Mise à jour impossible : " + ex.Message + "\n(le dossier du programme est-il protégé en écriture ?)"; return false; }
        }

        // Au démarrage (case cochée) : vérifie en arrière-plan, ne dit rien si à jour ou en cas d'échec, propose sinon
        public static void CheckAtStart(MainForm f)
        {
            if (Disabled || !AutoOn) return;
            Thread t = new Thread(delegate()
            {
                string err; Info i = Check(out err);
                if (i == null || !IsNewer(i) || Cfg.Get("maj.ignore", "") == i.Version) return;
                try { f.BeginInvoke((Action)delegate { Propose(f, i, true); }); } catch { }
            }); t.IsBackground = true; t.Start();
        }

        // Fenêtre de proposition ; un clic sur « Mettre à jour » fait tout (sauvegarde, téléchargement, remplacement, redémarrage)
        public static void Propose(MainForm owner, Info i, bool auto)
        {
            bool backup = true, keepSet = true, keepPost = true; bool ok;
            if (AutoAccept) ok = true;
            else using (UpdateDlg d = new UpdateDlg(i, auto)) { DialogResult r = d.ShowDialog(owner); ok = r == DialogResult.OK; if (r == DialogResult.Ignore) { Cfg.Set("maj.ignore", i.Version); Cfg.Save(); } backup = d.Backup; keepSet = d.KeepSettings; keepPost = d.KeepPosts; }
            if (!ok) return;
            if (!owner.CanQuitForUpdate()) return;
            string dir = Cfg.Dir ?? "."; List<string> del = new List<string>();
            try
            {
                string bk = BackupData(dir); if (!backup && Directory.Exists(bk)) { /* la copie est toujours faite : elle protège aussi en cas de remise à zéro */ }
                if (!keepSet) del.AddRange(SettingsFiles(dir)); if (!keepPost) del.AddRange(PostFiles(dir));
            }
            catch (Exception ex) { MessageBox.Show(owner, "Sauvegarde impossible : " + ex.Message + "\nMise à jour annulée.", "Mise à jour"); return; }
            string err; string exe = Application.ExecutablePath; string tmp = null;
            using (ProgressForm pf = new ProgressForm("Téléchargement de la version " + i.Version + "…"))
            {
                pf.Show(owner); Application.DoEvents();
                tmp = Download(i, Path.GetDirectoryName(exe), delegate(int p) { pf.Set(p); Application.DoEvents(); }, out err);
            }
            if (tmp == null) { MessageBox.Show(owner, err, "Mise à jour"); return; }
            string bkExe; if (!Apply(exe, tmp, MainForm.Version, del, true, out bkExe, out err)) { MessageBox.Show(owner, err, "Mise à jour"); try { File.Delete(tmp); } catch { } return; }
            CatLog.Event("maj", "mise à jour " + MainForm.Version + " -> " + i.Version + " (ancien programme : " + bkExe + ")");
            owner.QuitForUpdate();
        }
    }

    class ProgressForm : Form
    {
        readonly ProgressBar pb = new ProgressBar();
        public ProgressForm(string text)
        {
            Text = "Mise à jour"; FormBorderStyle = FormBorderStyle.FixedDialog; ControlBox = false; StartPosition = FormStartPosition.CenterParent; ClientSize = new Size(380, 80); BackColor = T.Bg; ForeColor = T.Text; Font = new Font("Segoe UI", 9f);
            Label l = new Label(); l.Text = text; l.SetBounds(12, 10, 356, 20); Controls.Add(l); pb.SetBounds(12, 40, 356, 22); Controls.Add(pb);
        }
        public void Set(int p) { pb.Value = Math.Max(0, Math.Min(100, p)); }
    }

    class UpdateDlg : Form
    {
        readonly CheckBox cbBackup = new CheckBox(), cbSet = new CheckBox(), cbPost = new CheckBox();
        public bool Backup { get { return cbBackup.Checked; } }
        public bool KeepSettings { get { return cbSet.Checked; } }
        public bool KeepPosts { get { return cbPost.Checked; } }
        public UpdateDlg(Updater.Info i, bool auto)
        {
            Text = "Mise à jour disponible"; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false; StartPosition = FormStartPosition.CenterParent; BackColor = T.Panel; ForeColor = T.Text; Font = new Font("Segoe UI", 9f); ClientSize = new Size(560, 430);
            Label h = new Label(); h.Text = "Version " + i.Version + " disponible (vous avez la " + MainForm.Version + ")"; h.Font = new Font("Segoe UI Semibold", 11f); h.ForeColor = T.Amber; h.SetBounds(12, 10, 536, 24); Controls.Add(h);
            TextBox n = new TextBox(); n.Multiline = true; n.ReadOnly = true; n.ScrollBars = ScrollBars.Vertical; n.Text = i.Notes.Length > 0 ? i.Notes.Replace("\n", "\r\n") : "(pas de notes de version)"; n.SetBounds(12, 40, 536, 120); n.BackColor = T.Bg; n.ForeColor = T.Text; Controls.Add(n);
            Label w = new Label(); w.SetBounds(12, 170, 536, 74); w.Text = "Seul le programme est remplacé. Vos réglages, dispositions, commandes et le journal de trafic (dossier data) restent en place ; l'ancien programme est gardé sous le nom HamRadioLink-" + MainForm.Version + ".exe (seule la version précédente est conservée, les plus anciennes sont supprimées au démarrage). Le logiciel redémarre tout seul."; Controls.Add(w);
            cbBackup.Text = "Faire une copie de sécurité de mes réglages avant (dossier data\\sauvegarde-avant-MAJ-…) — toujours faite"; cbBackup.Checked = true; cbBackup.Enabled = false; cbBackup.SetBounds(16, 250, 530, 24); Controls.Add(cbBackup);
            cbSet.Text = "Conserver les réglages du logiciel (couleurs, ports, TUNE, ROS…)"; cbSet.Checked = true; cbSet.SetBounds(16, 280, 530, 24); Controls.Add(cbSet);
            cbPost.Text = "Conserver les dispositions et commandes personnalisées de mes postes"; cbPost.Checked = true; cbPost.SetBounds(16, 308, 530, 24); Controls.Add(cbPost);
            Label u = new Label(); u.SetBounds(34, 334, 510, 36); u.ForeColor = T.Dim; u.Text = "Décochez pour repartir des valeurs d'origine : la copie de sécurité permet de retrouver les anciens fichiers."; Controls.Add(u);
            Button ok = Mk("Mettre à jour", 270, 384, 140); ok.DialogResult = DialogResult.OK; ok.BackColor = Color.FromArgb(20, 110, 40); AcceptButton = ok;
            Button no = Mk("Plus tard", 418, 384, 130); no.DialogResult = DialogResult.Cancel; CancelButton = no;
            if (auto) { Button ig = Mk("Ignorer cette version", 16, 384, 180); ig.DialogResult = DialogResult.Ignore; }
        }
        Button Mk(string t, int x, int y, int w) { Button b = new Button(); b.Text = t; b.SetBounds(x, y, w, 32); b.FlatStyle = FlatStyle.Flat; b.BackColor = T.Panel2; b.ForeColor = T.Text; b.FlatAppearance.BorderColor = T.Edge; Controls.Add(b); return b; }
    }
}
