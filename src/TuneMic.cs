using System;
using System.Drawing;
using System.Windows.Forms;

namespace HamRadioLink
{
    // Bouton TUNE : tonalité de 1500 Hz envoyée sur la carte son d'émission du poste (la même que « Micro vers le poste »),
    // PTT par CAT (ou VOX du poste), pendant 5 s à 1 min. Réglages dans Paramètres ⚙ > TUNE ; couleurs repos / émission aussi.
    static class TuneMic
    {
        public static event Action Changed;
        static TuneTone tone; static Rig rig; static Ctl ptt; static bool owned; static DateTime until; static Timer tm;
        public static int Level { get { return Math.Max(1, Math.Min(100, Cfg.GetInt("tune.niveau", 15))); } }
        public static int Seconds { get { return Math.Max(5, Math.Min(60, Cfg.GetInt("tune.duree", 10))); } }
        static bool? forceVox; static bool safe; static Ctl pwr; static string pwrOrig;
        public static bool Vox { get { return forceVox.HasValue ? forceVox.Value : Cfg.GetBool("tune.vox", true); } }
        // Test de la puissance de sécurité (Paramètres > Alerte ROS) : TUNE 1500 Hz avec la puissance réduite au % réglé, en PTT ou en VOX ; la puissance d'origine est remise à la fin
        public static void StartSafe(Rig r, bool vox) { if (tone != null) { Stop("TUNE arrêté"); return; } safe = true; forceVox = vox; Start(r); if (tone == null) { safe = false; forceVox = null; } }
        // 0 = couleur par défaut (comme la touche PTT : vert au repos, rouge en émission)
        public static Color ColOff { get { int v = Cfg.GetInt("tune.coff", 0); return v != 0 ? Color.FromArgb(v) : Color.FromArgb(T.Green.R * 40 / 100, T.Green.G * 60 / 100, T.Green.B * 45 / 100); } }
        public static Color ColOn { get { int v = Cfg.GetInt("tune.con", 0); return v != 0 ? Color.FromArgb(v) : Color.FromArgb(T.Red.R * 80 / 100, T.Red.G * 55 / 100, T.Red.B * 55 / 100); } }
        public static bool ActiveFor(Rig r) { return tone != null && rig == r; }
        public static bool SafeActiveFor(Rig r) { return tone != null && rig == r && pwr != null; }
        // le % de sécurité vient de changer pendant le test : appliqué tout de suite au poste
        public static void Reapply() { int ov; try { if (tone != null && pwr != null && rig != null && rig.Link != null && int.TryParse(pwrOrig, out ov)) rig.Set(pwr, RosGuard.Reduced(pwr, ov).ToString()); } catch { } }
        public static int Left { get { return Math.Max(0, (int)Math.Ceiling((until - DateTime.UtcNow).TotalSeconds)); } }
        public static void Refresh() { Fire(); }
        static void Fire() { Action a = Changed; if (a != null) a(); }

        public static void Toggle(Rig r)
        {
            if (tone != null) { bool same = rig == r; Stop("TUNE arrêté"); if (same) return; }
            Start(r);
        }

        static void Start(Rig r)
        {
            if (r == null || r.Def == null) return;
            if (MicRoute.CardId.Length == 0) { MessageBox.Show("TUNE : choisissez d'abord la carte son d'émission du poste (Paramètres > Micro vers le poste, ou clic droit sur la touche PTT).", "TUNE"); return; }
            Ctl p = r.Def.Find("ptt"); bool pttOk = p != null && p.K == Kind.Toggle;
            if (!Vox)
            {
                if (r.Link == null) { MessageBox.Show("TUNE : connectez d'abord le poste (ou choisissez « VOX du poste » dans Paramètres > TUNE).", "TUNE"); return; }
                if (!pttOk) { MessageBox.Show("Ce poste n'a pas de touche PTT : choisissez « VOX du poste » dans Paramètres > TUNE.", "TUNE"); return; }
            }
            rig = r; ptt = pttOk ? p : null; owned = false; pwr = null;
            if (safe)
            {
                Ctl pc = RosGuard.PowerCtl(r); string o = pc != null && r.Link != null ? r.Get(pc.Id) : null; int ov;
                if (pc == null || o == null || !int.TryParse(o, out ov)) { MessageBox.Show("Test impossible : la puissance du poste n'est pas lue (poste connecté ? réglage de puissance présent ?).", "TUNE"); return; }
                pwr = pc; pwrOrig = o; r.Set(pc, RosGuard.Reduced(pc, ov).ToString()); CatLog.Event("tune", "Test sécurité : puissance " + o + " -> " + RosGuard.Reduced(pc, ov));
            }
            if (!Vox) { owned = !r.Tx; if (owned) { r.Set(ptt, ptt.OnSend); r.PttButtonStart(); } }
            tone = new TuneTone(MicRoute.CardId, 1500, Level / 100f); tone.Start();
            until = DateTime.UtcNow.AddSeconds(Seconds);
            if (tm == null) { tm = new Timer(); tm.Interval = 200; tm.Tick += delegate { Tick(); }; }
            tm.Start(); CatLog.Event("tune", "TUNE : émission " + Seconds + " s, niveau " + Level + " %, " + (Vox ? "VOX du poste" : "PTT par CAT")); Fire();
        }

        static void Tick()
        {
            TuneTone t = tone; if (t == null) { tm.Stop(); return; }
            if (t.Error != null) { Stop("TUNE : carte son d'émission indisponible (" + t.Error + ")"); return; }
            if (!Vox && rig.Link == null) { Stop("TUNE arrêté (poste déconnecté)"); return; }
            if (DateTime.UtcNow >= until) { Stop("TUNE terminé"); return; }
            Fire();
        }

        public static void Stop(string why)
        {
            TuneTone t = tone; if (t == null) return;
            tone = null; if (tm != null) tm.Stop();
            try { t.Stop(); } catch { }
            try { if (owned && ptt != null && rig != null && rig.Link != null) { rig.Set(ptt, ptt.OffSend); rig.PttButtonStop(); } } catch { }
            try { if (pwr != null && rig != null && rig.Link != null) rig.Set(pwr, pwrOrig); } catch { }
            pwr = null; safe = false; forceVox = null; owned = false; if (why != null) CatLog.Event("tune", why); Fire();
        }
    }
}
