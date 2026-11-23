using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Media;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace HamRadioLink
{
    // Alerte ROS : en émission, si le ROS lu sur le poste dépasse le seuil (après un délai réglable), bips continus (2000 Hz, volume max, toutes les 0,25 s par défaut)
    // jusqu'à ce que l'opérateur les arrête ; option : faire chuter la puissance à un % de la puissance réglée. Réglages : Paramètres > Alerte ROS.
    static class RosGuard
    {
        public static bool On { get { return Cfg.GetBool("ros.on", true); } }
        public static double Threshold { get { return Math.Max(1.1, Math.Min(20, Cfg.GetInt("ros.seuil", 200) / 100.0)); } }
        public static int DelayMs { get { return Math.Max(0, Math.Min(10000, Cfg.GetInt("ros.delai", 300))); } }
        public static int Freq { get { return Math.Max(200, Math.Min(8000, Cfg.GetInt("ros.freq", 2000))); } }
        public static int Volume { get { return Math.Max(1, Math.Min(100, Cfg.GetInt("ros.vol", 100))); } }
        public static int IntervalMs { get { return Math.Max(50, Math.Min(2000, Cfg.GetInt("ros.pause", 50))); } }
        public static bool Drop { get { return Cfg.GetBool("ros.chute", true); } }
        public static int DropPct { get { return Math.Max(0, Math.Min(100, Cfg.GetInt("ros.pct", 0))); } }
        public static bool TestVox { get { return Cfg.GetBool("ros.vox", true); } }

        // Lecture du poste (0 à 255 chez Icom / Yaesu, 0 à 30 chez Kenwood) -> ROS, par interpolation de la table du manuel Icom (0 = 1,0 ; 48 = 1,5 ; 80 = 2,0 ; 120 = 3,0 ; 255 = infini, ramené à 10)
        static readonly double[] tr = { 0, 48, 80, 120, 255 }, tv = { 1.0, 1.5, 2.0, 3.0, 10.0 };
        public static double RatioOf(double raw, double max)
        {
            if (max <= 0) return -1; double r = Math.Max(0, Math.Min(255, raw * 255.0 / max));
            for (int i = 1; i < tr.Length; i++) if (r <= tr[i]) return tv[i - 1] + (tv[i] - tv[i - 1]) * (r - tr[i - 1]) / (tr[i] - tr[i - 1]);
            return tv[tv.Length - 1];
        }
        // ROS actuel du poste, -1 si le poste n'en donne pas
        public static double Ratio(Rig r)
        {
            if (r == null || r.Def == null) return -1; Ctl c = r.Def.Find("swr"); if (c == null || !c.HasCat) return -1;
            string s = r.Get("swr"); int v; if (s == null || !int.TryParse(s, out v)) return -1; double q = MeterCal.SwrRatio(r.Def.Key, v * 255.0 / Math.Max(1, c.Max)); return q > 10 && MeterCal.Points(r.Def.Key, "swr").Count == 0 ? 10 : Math.Min(q, 100);
        }
        public static Ctl PowerCtl(Rig r) { return r == null ? null : PowerCtl(r.Def); }
        public static Ctl PowerCtl(RadioDef def)
        {
            if (def == null) return null; Ctl c = def.Find("pwr"); if (c != null && c.K == Kind.Knob && c.HasCat && c.Set.Length > 0) return c;
            foreach (Ctl x in def.C) if (x.K == Kind.Knob && x.HasCat && !x.Local && x.Set.Length > 0 && Regex.IsMatch(x.Label ?? "", @"(?i)puiss|power|pwr")) return x;
            return null;
        }
        public static int Reduced(Ctl c, int v) { int top = Math.Max(v, c.Min); return c.Min + (int)Math.Round((top - c.Min) * DropPct / 100.0); }   // 0 % = minimum du poste, 100 % = puissance réglée, linéaire entre les deux
        // vrai quand le ROS reste au-dessus du seuil depuis assez longtemps ; since = instant (ms) du premier dépassement, 0 = pas de dépassement
        public static bool ShouldTrigger(double ratio, ref int since, int nowMs, double threshold, int delayMs)
        {
            if (ratio < threshold) { since = 0; return false; }
            if (since == 0) since = nowMs == 0 ? 1 : nowMs;
            return unchecked(nowMs - since) >= delayMs;
        }

        // ----- son : bips générés en mémoire
        public static byte[] Wav(int hz, int vol, int ms)
        {
            const int sr = 44100; int n = sr * ms / 1000; MemoryStream m = new MemoryStream(); BinaryWriter w = new BinaryWriter(m);
            w.Write(new char[] { 'R', 'I', 'F', 'F' }); w.Write(36 + n * 2); w.Write(new char[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' }); w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(sr); w.Write(sr * 2); w.Write((short)2); w.Write((short)16);
            w.Write(new char[] { 'd', 'a', 't', 'a' }); w.Write(n * 2); double amp = 32767.0 * vol / 100.0; int fade = sr / 300;
            for (int i = 0; i < n; i++) { double e = Math.Min(1.0, Math.Min(i, n - 1 - i) / (double)fade); w.Write((short)Math.Round(Math.Sin(2 * Math.PI * hz * i / sr) * amp * e)); }
            w.Flush(); return m.ToArray();
        }
        static volatile bool alarm; static Thread th; static System.Windows.Forms.Timer tm; static Func<List<Rig>> rigs; static readonly Dictionary<Rig, int> since = new Dictionary<Rig, int>();
        static Rig dropRig; static Ctl dropCtl; static string dropOrig; static Form alertForm; static Label alertLbl; static Button btnRest;
        public static bool Alarming { get { return alarm; } }
        public static string Notice;                 // message à afficher (alerte terminée toute seule)
        public static int ClearMs { get { return Math.Max(500, Math.Min(30000, Cfg.GetInt("ros.eteint", 1500))); } } static int clearSince;
        public static bool KeepReduced { get { return Cfg.GetBool("ros.garder", false); } }
        // le ROS est repassé sous le seuil depuis assez longtemps : l'alerte s'éteint sans action de l'opérateur
        public static bool ShouldClear(double ratio, ref int since, int nowMs, double threshold, int holdMs)
        {
            if (ratio < 0 || ratio >= threshold) { since = 0; return false; }
            if (since == 0) since = nowMs == 0 ? 1 : nowMs;
            return unchecked(nowMs - since) >= holdMs;
        }
        public static void Test(Rig r) { if (!alarm) Trigger(r, Threshold + 1); }
        public static string AlarmText { get { double r = Ratio(alarmRig); return "ALERTE ROS" + (r > 0 ? "  " + r.ToString("0.0") : ""); } }
        static void BeepLoop(int count)
        {
            byte[] wav = Wav(Freq, Volume, 120); int pause = IntervalMs;
            using (SoundPlayer p = new SoundPlayer(new MemoryStream(wav)))
                for (int i = 0; (count == 0 ? alarm : i < count); i++) { try { p.PlaySync(); } catch { } Thread.Sleep(pause); }
        }
        public static void TestBeep() { Thread t = new Thread(delegate() { BeepLoop(4); }); t.IsBackground = true; t.Start(); }

        public static void Start(Func<List<Rig>> getRigs)
        {
            rigs = getRigs; if (tm != null) return; tm = new System.Windows.Forms.Timer(); tm.Interval = 100; tm.Tick += delegate { try { Tick(); } catch { } }; tm.Start();
        }
        static void Tick() { Tick(Environment.TickCount); }
        public static bool Silent;                   // essais automatiques : ni bips ni fenêtre
        public static void Tick(int now)
        {
            if (alarm) { if (alertLbl != null) alertLbl.Text = Msg(alarmRig, Ratio(alarmRig)); double rc = Ratio(alarmRig); bool idle = alarmRig == null || !alarmRig.Tx; if (ShouldClear(idle && (rc < 0 || rc >= Threshold) ? 1.0 : rc, ref clearSince, now, Threshold, ClearMs)) { bool keep = KeepReduced; string w = "Alerte ROS terminée : " + (idle && (rc < 0 || rc >= Threshold) ? "l'émission est terminée" : "le ROS est revenu à " + rc.ToString("0.0")) + (dropCtl != null ? (keep ? ". Puissance laissée réduite (" + dropOrig + " avant), comme demandé dans les paramètres." : ". Puissance rétablie à " + dropOrig + ".") : "."); CatLog.Event("ros", w); Notice = w; Stop(!keep); } return; }
            if (!On || rigs == null) return;
            foreach (Rig r in rigs())
            {
                int s; since.TryGetValue(r, out s);
                if (r.Link == null || !r.Tx) { since[r] = 0; continue; }
                double ro = Ratio(r); if (ro < 0) { since[r] = 0; continue; }
                bool go = ShouldTrigger(ro, ref s, now, Threshold, DelayMs); since[r] = s;
                if (go) { Trigger(r, ro); return; }
            }
        }
        static Rig alarmRig;
        static string Msg(Rig r, double ro) { return "ROS " + (ro < 0 ? "?" : ro.ToString("0.0")) + " — seuil " + Threshold.ToString("0.0") + "\n" + (r != null && r.Def != null ? CmdServer.ModelName(r.Def) : "") + (dropCtl != null ? "\nPuissance réduite à " + DropPct + " % (était " + dropOrig + ")." : ""); }
        static void Trigger(Rig r, double ro)
        {
            alarm = true; clearSince = 0; alarmRig = r; dropRig = null; dropCtl = null; dropOrig = null;
            if (Drop)
            {
                Ctl pc = PowerCtl(r); string o = pc != null ? r.Get(pc.Id) : null; int ov;
                if (pc != null && o != null && int.TryParse(o, out ov)) { try { r.Set(pc, Reduced(pc, ov).ToString()); dropRig = r; dropCtl = pc; dropOrig = o; } catch { } }
            }
            CatLog.Event("ros", "ALERTE ROS " + ro.ToString("0.0") + " (seuil " + Threshold.ToString("0.0") + ")" + (dropCtl != null ? " : puissance " + dropOrig + " -> " + Reduced(dropCtl, int.Parse(dropOrig)) : ""));
            AlertLog.Add(r != null && r.Def != null ? r.Def.Name : "", "ALERTE ROS déclenchée : ROS " + ro.ToString("0.0") + " détecté (seuil " + Threshold.ToString("0.0") + ")" + (dropCtl != null ? ", puissance réduite de " + dropOrig + " à " + Reduced(dropCtl, int.Parse(dropOrig)) : ""));
            if (Silent) return;
            th = new Thread(delegate() { BeepLoop(0); }); th.IsBackground = true; th.Start();
            ShowForm(r, ro);
        }
        static void ShowForm(Rig r, double ro)
        {
            Form f = new Form(); alertForm = f; f.Text = "ALERTE ROS"; f.TopMost = true; f.FormBorderStyle = FormBorderStyle.FixedDialog; f.ControlBox = false; f.MaximizeBox = false; f.MinimizeBox = false; f.StartPosition = FormStartPosition.CenterScreen;
            f.BackColor = Color.FromArgb(150, 0, 0); f.ForeColor = Color.White; f.ClientSize = new Size(520, 190); f.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            alertLbl = new Label(); alertLbl.SetBounds(12, 12, 496, 90); alertLbl.Font = new Font("Segoe UI", 14f, FontStyle.Bold); alertLbl.Text = Msg(r, ro); f.Controls.Add(alertLbl);
            Button b = new Button(); b.Text = "ARRÊTER L'ALERTE"; b.SetBounds(12, 120, 240, 54); b.FlatStyle = FlatStyle.Flat; b.BackColor = Color.White; b.ForeColor = Color.Black; b.Click += delegate { Stop(false); }; f.Controls.Add(b);
            btnRest = new Button(); btnRest.Text = "Arrêter et rétablir la puissance"; btnRest.SetBounds(262, 120, 246, 54); btnRest.FlatStyle = FlatStyle.Flat; btnRest.BackColor = Color.FromArgb(230, 230, 230); btnRest.ForeColor = Color.Black; btnRest.Enabled = dropCtl != null; btnRest.Click += delegate { Stop(true); }; f.Controls.Add(btnRest);
            f.FormClosing += delegate(object s, FormClosingEventArgs e) { if (alarm && e.CloseReason == CloseReason.UserClosing) e.Cancel = true; };
            f.Show();
        }
        // Le % de chute vient d'être modifié (réglette) : pris en compte aussitôt, même en émission (alerte en cours ou test TUNE de sécurité).
        // Les envois sont regroupés (150 ms) pour ne pas saturer la liaison pendant qu'on déplace la réglette.
        static System.Windows.Forms.Timer reTm;
        public static void ReapplyDrop()
        {
            if (reTm == null) { reTm = new System.Windows.Forms.Timer(); reTm.Interval = 150; reTm.Tick += delegate { reTm.Stop(); ReapplyNow(); }; }
            reTm.Stop(); reTm.Start();
        }
        public static void ReapplyNow()
        {
            int ov;
            if (alarm && dropRig != null && dropCtl != null && dropRig.Link != null && int.TryParse(dropOrig, out ov))
            {
                try { dropRig.Set(dropCtl, Reduced(dropCtl, ov).ToString()); } catch { }
                CatLog.Event("ros", "puissance de sécurité modifiée : " + DropPct + " % -> " + Reduced(dropCtl, ov));
                if (alertLbl != null) { try { alertLbl.Text = Msg(alarmRig, Ratio(alarmRig)); } catch { } }
            }
            TuneMic.Reapply();
        }
        public static void Stop(bool restore)
        {
            bool was = alarm; alarm = false;
            if (restore && dropRig != null && dropCtl != null && dropRig.Link != null) { try { dropRig.Set(dropCtl, dropOrig); } catch { } }
            if (was) CatLog.Event("ros", "alerte ROS arrêtée" + (restore ? " (puissance rétablie)" : ""));
            dropRig = null; dropCtl = null; dropOrig = null; alarmRig = null;
            Form f = alertForm; alertForm = null; alertLbl = null; btnRest = null; if (f != null) { try { f.Close(); f.Dispose(); } catch { } }
        }
    }
}
