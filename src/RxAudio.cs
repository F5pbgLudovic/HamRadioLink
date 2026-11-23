using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace HamRadioLink
{
    // Réception du poste sur le PC : le son de l'entrée de la carte son du poste (carte USB du transceiver) est rejoué sur la sortie par défaut de Windows.
    // Réglage POSTE PAR POSTE au clic droit sur le bouton de volume (AF) : activer / désactiver, choisir la carte son du poste, et « couper le haut-parleur du poste »
    // (le volume AF du transceiver est mis à 0 par CAT, puis rétabli ; le son reste sur le PC). Clés settings.ini : rx.<poste>.on / .carte / .cartenom / .coupe / .afsave.
    static class RxAudio
    {
        static readonly Dictionary<string, RigMonitor> mons = new Dictionary<string, RigMonitor>();
        static DateTime lastSet = DateTime.MinValue;
        static string K(string key, string s) { return "rx." + key + "." + s; }
        public static bool On(string key) { return Cfg.GetBool(K(key, "on"), false); }
        public static string CardId(string key) { return Cfg.Get(K(key, "carte"), ""); }
        public static string CardName(string key) { return Cfg.Get(K(key, "cartenom"), ""); }
        public static bool Mute(string key) { return Cfg.GetBool(K(key, "coupe"), false); }

        // Mode « volume sur le PC » : haut-parleur du poste coupé + écoute active -> le bouton AF règle le volume de l'écoute sur le PC (le poste reste à 0).
        public static bool Active(Rig rig) { if (rig == null || rig.Def == null) return false; string k = rig.Def.Key; return On(k) && Mute(k) && CardId(k).Length > 0 && rig.Def.Find("af") != null; }
        static DateTime dirtyAt = DateTime.MinValue; static bool dirty;
        static float GainOf(string key, Ctl af)
        {
            int v; if (af == null || !int.TryParse(Cfg.Get(K(key, "vol"), ""), out v)) return 1f; int span = Math.Max(1, af.Max - af.Min);
            return Math.Max(0f, Math.Min(1f, (v - af.Min) / (float)span));
        }
        static void ApplyGain(string key, Ctl af) { RigMonitor m; lock (mons) { if (mons.TryGetValue(key, out m) && m != null) m.Gain = Active4(key) ? GainOf(key, af) : 1f; } }
        static bool Active4(string key) { return On(key) && Mute(key) && CardId(key).Length > 0; }
        // Appelée par Rig.Set pour le bouton AF : en mode « volume sur le PC », la valeur règle le volume de l'écoute et n'est PAS envoyée au poste.
        public static bool Local(Rig rig, Ctl c, string v)
        {
            if (c == null || c.Id != "af" || !Active(rig)) return false;
            string key = rig.Def.Key; Cfg.Set(K(key, "vol"), v); dirty = true; dirtyAt = DateTime.Now; ApplyGain(key, c); return true;
        }
        public static void AddItems(ContextMenuStrip m, Rig rig)
        {
            string key = rig.Def.Key; string card = CardId(key).Length > 0 ? CardName(key) : "carte son à choisir";
            ToolStripMenuItem hd = new ToolStripMenuItem("Réception du poste sur le PC (" + rig.Def.Name + ")"); hd.Enabled = false; m.Items.Add(hd); m.Items.Add(new ToolStripSeparator());
            ToolStripMenuItem on = new ToolStripMenuItem("Écouter la réception du poste sur le PC (« " + card + " » → haut-parleurs de Windows)"); on.Checked = On(key);
            on.Click += delegate { Set(key, !On(key)); }; m.Items.Add(on);
            ToolStripMenuItem cards = new ToolStripMenuItem("Carte son du poste (entrée du PC reliée au poste)");
            try
            {
                foreach (AudioEndpoints.Endpoint e in AudioEndpoints.All())
                {
                    if (!e.Capture) continue;
                    string id = e.Id, name = e.FullName ?? e.Name ?? id; ToolStripMenuItem it = new ToolStripMenuItem(name); it.Checked = id == CardId(key);
                    it.Click += delegate { Cfg.Set(K(key, "carte"), id); Cfg.Set(K(key, "cartenom"), name); Cfg.Save(); CatLog.Event(key, "réception : carte son « " + name + " »"); Update(key); };
                    cards.DropDownItems.Add(it);
                }
            }
            catch (Exception ex) { CatLog.Event(key, "lecture des cartes son impossible : " + ex.Message); }
            if (cards.DropDownItems.Count == 0) cards.DropDownItems.Add(new ToolStripMenuItem("Aucune entrée son trouvée dans Windows") { Enabled = false });
            m.Items.Add(cards);
            ToolStripMenuItem mu = new ToolStripMenuItem("Couper le haut-parleur du poste (le son reste sur le PC)"); mu.Checked = Mute(key); mu.Enabled = rig.Def.Find("af") != null;
            mu.ToolTipText = "Met le volume AF du transceiver à 0 tant que l'écoute sur le PC est active ; le volume d'origine est rétabli à la désactivation.";
            mu.Click += delegate { Cfg.SetBool(K(key, "coupe"), !Mute(key)); Cfg.Save(); CatLog.Event(key, Mute(key) ? "réception : haut-parleur du poste coupé" : "réception : haut-parleur du poste rétabli"); Tick(rig); }; m.Items.Add(mu);
            m.Items.Add(new ToolStripSeparator());
            ToolStripMenuItem eqr = new ToolStripMenuItem("Égaliseur 10 bandes (réception)…"); eqr.Checked = FxCfg.Active(false);
            eqr.Click += delegate { AudioFxForm.Open(Application.OpenForms.Count > 0 ? Application.OpenForms[0] : null, false); }; m.Items.Add(eqr);
        }

        public static void Set(string key, bool on)
        {
            if (on && CardId(key).Length == 0) { MessageBox.Show("Choisissez d'abord la carte son du poste (clic droit sur le bouton de volume > Carte son du poste).", "Réception du poste"); return; }
            Cfg.SetBool(K(key, "on"), on); Cfg.Save(); CatLog.Event(key, on ? "réception sur le PC activée" : "réception sur le PC désactivée"); Update(key);
        }

        // démarre, arrête ou rouvre l'écoute d'un poste selon son réglage
        public static void Update(string key)
        {
            lock (mons)
            {
                RigMonitor cur; mons.TryGetValue(key, out cur); bool want = On(key) && CardId(key).Length > 0;
                if (cur != null && (!want || cur.CaptureId != CardId(key))) { mons.Remove(key); try { cur.Stop(); } catch { } cur = null; }
                if (want && cur == null)
                {
                    RigMonitor r = new RigMonitor(CardId(key), false); string k = key; { RadioDef rd = Radios.All().Find(x => x.Key == key); if (rd != null && Active4(key)) r.Gain = GainOf(key, rd.Find("af")); }
                    r.Problem += delegate(string msg) { if (msg != null) { CatLog.Event(k, "réception : carte son indisponible : " + msg); AlertLog.Add(k, "Carte son du poste indisponible (réception sur le PC) : " + msg); } };
                    mons[key] = r;
                }
            }
        }
        public static void StartAll() { foreach (RadioDef d in Radios.All()) if (On(d.Key)) { try { Update(d.Key); } catch { } } }

        // Volume AF du poste : mis à 0 si « couper le haut-parleur » + écoute active (le bouton AF règle alors le volume de l'écoute sur le PC) ;
        // l'ancien volume du poste est mémorisé (settings.ini) et rétabli ensuite.
        public static void Tick(Rig rig)
        {
            try
            {
                if (dirty && (DateTime.Now - dirtyAt).TotalSeconds > 2) { dirty = false; Cfg.Save(); }
                if (rig == null || rig.Def == null || !rig.Connected) return;
                string key = rig.Def.Key; Ctl af = rig.Def.Find("af"); if (af == null) return;
                bool want = Active(rig); string sk = K(key, "afsave"), saved = Cfg.Get(sk, ""), v = rig.Get("af");
                if (want)
                {
                    if (saved.Length == 0)
                    {   // entrée dans le mode : on retient le volume réel du poste (lu avant que le bouton ne passe en volume PC)
                        if (v == null) return;
                        Cfg.Set(sk, v); if (Cfg.Get(K(key, "vol"), "").Length == 0) Cfg.Set(K(key, "vol"), v); Cfg.Save(); ApplyGain(key, af);
                        rig.SetRadio(af, "0"); lastSet = DateTime.Now;
                    }
                    else if ((DateTime.Now - lastSet).TotalSeconds > 2) { lastSet = DateTime.Now; rig.SetRadio(af, "0"); }   // le poste reste à 0 (une commande toutes les 2 s au plus)
                    string pc = Cfg.Get(K(key, "vol"), ""); if (pc.Length > 0 && rig.Get("af") != pc) rig.ShowLocal("af", pc);   // le bouton montre le volume de l'écoute sur le PC
                }
                else if (saved.Length > 0) { rig.SetRadio(af, saved); rig.ShowLocal("af", saved); Cfg.Set(sk, ""); Cfg.Save(); ApplyGain(key, af); }
            }
            catch { }
        }

        // arrêt du programme : le volume du poste est rétabli, les écoutes sont arrêtées
        public static void Shutdown(IEnumerable<Rig> rigs)
        {
            try
            {
                foreach (Rig r in rigs)
                {
                    if (r == null || r.Def == null || !r.Connected) continue;
                    string sk = K(r.Def.Key, "afsave"), saved = Cfg.Get(sk, ""); Ctl af = r.Def.Find("af");
                    if (saved.Length > 0 && af != null) { r.SetRadio(af, saved); Cfg.Set(sk, ""); Cfg.Save(); System.Threading.Thread.Sleep(120); }
                }
            }
            catch { }
            lock (mons) { foreach (RigMonitor m in mons.Values) { try { m.Stop(); } catch { } } mons.Clear(); }
        }
    }
}
