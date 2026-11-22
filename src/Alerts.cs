using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace HamRadioLink
{
    // Messages d'alerte (perte de liaison, alerte ROS, carte son…) horodatés, conservés une semaine dans <données>\alertes.txt, pour tous les postes.
    // Le bouton « danger » de la fenêtre de commande est allumé tant qu'il y a des messages non consultés.
    public static class AlertLog
    {
        public class Item { public DateTime T; public string Radio, Text; }
        static readonly object lk = new object(); static List<Item> items; static DateTime seen = DateTime.MinValue; static string lastKey = ""; static DateTime lastAt = DateTime.MinValue;
        public static readonly TimeSpan Keep = TimeSpan.FromDays(7);
        static string FileA { get { return Path.Combine(Cfg.Dir ?? ".", "alertes.txt"); } }
        static string FileSeen { get { return Path.Combine(Cfg.Dir ?? ".", "alertes.lu"); } }

        static void Load()
        {
            if (items != null) return;
            items = new List<Item>();
            try
            {
                if (File.Exists(FileA))
                    foreach (string l in File.ReadAllLines(FileA, Encoding.UTF8))
                    {
                        string[] p = l.Split(new char[] { '|' }, 3); DateTime t; if (p.Length < 3 || !DateTime.TryParseExact(p[0], "yyyy-MM-dd HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out t)) continue;
                        items.Add(new Item { T = t, Radio = p[1], Text = p[2] });
                    }
                if (File.Exists(FileSeen)) { DateTime s; if (DateTime.TryParseExact(File.ReadAllText(FileSeen).Trim(), "yyyy-MM-dd HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out s)) seen = s; }
            }
            catch { }
            Purge();
        }
        static void Purge() { DateTime lim = DateTime.Now - Keep; if (items.RemoveAll(x => x.T < lim) > 0) Save(); }
        static void Save()
        {
            try
            {
                StringBuilder sb = new StringBuilder(); foreach (Item i in items) sb.Append(i.T.ToString("yyyy-MM-dd HH:mm:ss") + "|" + i.Radio.Replace("|", "/") + "|" + i.Text.Replace("\r", " ").Replace("\n", " ") + "\r\n");
                Directory.CreateDirectory(Path.GetDirectoryName(FileA)); File.WriteAllText(FileA, sb.ToString(), new UTF8Encoding(false));
            }
            catch { }
        }

        // Ajoute un message (le même message du même poste n'est pas répété dans la minute qui suit).
        public static void Add(string radio, string text)
        {
            try
            {
                lock (lk)
                {
                    Load(); string key = (radio ?? "") + "|" + text; if (key == lastKey && (DateTime.Now - lastAt).TotalSeconds < 60) return; lastKey = key; lastAt = DateTime.Now;
                    items.Add(new Item { T = DateTime.Now, Radio = radio ?? "", Text = text }); Purge(); Save();
                }
            }
            catch { }
        }
        public static bool Unread { get { lock (lk) { Load(); foreach (Item i in items) if (i.T > seen) return true; return false; } } }
        public static int UnreadCount { get { lock (lk) { Load(); int n = 0; foreach (Item i in items) if (i.T > seen) n++; return n; } } }
        public static List<Item> All() { lock (lk) { Load(); Purge(); return new List<Item>(items); } }
        public static void MarkSeen() { lock (lk) { Load(); seen = DateTime.Now; try { File.WriteAllText(FileSeen, seen.ToString("yyyy-MM-dd HH:mm:ss")); } catch { } } }
        public static void Clear() { lock (lk) { Load(); items.Clear(); Save(); seen = DateTime.Now; try { File.WriteAllText(FileSeen, seen.ToString("yyyy-MM-dd HH:mm:ss")); } catch { } } }
    }

    // Fenêtre des alertes : liste horodatée (la plus récente en haut), OK et Effacer en bas. La consulter éteint le bouton.
    public class AlertsForm : Form
    {
        readonly TextBox tb = new TextBox();
        public AlertsForm()
        {
            Text = "Alertes"; ClientSize = new Size(720, 420); StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.Sizable; MinimizeBox = false; ShowInTaskbar = false; Font = new Font("Segoe UI", 9f);
            BackColor = T.Panel; ForeColor = T.Text;
            tb.Multiline = true; tb.ReadOnly = true; tb.ScrollBars = ScrollBars.Vertical; tb.WordWrap = true; tb.Font = new Font("Consolas", 10f); tb.BackColor = T.Bg; tb.ForeColor = T.Text; tb.BorderStyle = BorderStyle.FixedSingle; tb.SetBounds(12, 12, 696, 358); tb.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; Controls.Add(tb);
            Button ok = new Button { Text = "OK", Width = 100, Height = 30, Left = 12, Top = 380, Anchor = AnchorStyles.Bottom | AnchorStyles.Left, FlatStyle = FlatStyle.System }; ok.Click += delegate { Close(); }; Controls.Add(ok);
            Button cl = new Button { Text = "Effacer", Width = 100, Height = 30, Left = 120, Top = 380, Anchor = AnchorStyles.Bottom | AnchorStyles.Left, FlatStyle = FlatStyle.System };
            cl.Click += delegate { if (MessageBox.Show(this, "Effacer tous les messages d'alerte ?", "Alertes", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) { AlertLog.Clear(); Fill(); } }; Controls.Add(cl);
            Label info = new Label { Text = "Les messages sont conservés une semaine, puis effacés automatiquement.", AutoSize = true, Left = 236, Top = 387, ForeColor = T.Dim, Anchor = AnchorStyles.Bottom | AnchorStyles.Left }; Controls.Add(info);
            AcceptButton = ok; Fill(); AlertLog.MarkSeen();
        }
        void Fill()
        {
            List<AlertLog.Item> l = AlertLog.All(); l.Reverse(); StringBuilder sb = new StringBuilder();
            if (l.Count == 0) sb.Append("Aucun message d'alerte.");
            foreach (AlertLog.Item i in l) sb.Append(i.T.ToString("dd/MM/yyyy HH:mm:ss") + "  " + (i.Radio.Length > 0 ? "[" + i.Radio + "]  " : "") + i.Text + "\r\n");
            tb.Text = sb.ToString(); tb.SelectionStart = 0; tb.SelectionLength = 0;
        }
    }
}
