using System;
using System.Windows.Forms;

namespace HamRadioLink
{
    // textes : français seulement dans HamRadioLink
    static class Lang
    {
        public static string T(string fr, string en) { return fr; }
        public static string F(string fr, string en, params object[] a) { return string.Format(fr, a); }
    }

    // Micro du PC vers le poste : le micro choisi est envoyé en continu sur la carte son reliée au poste (WASAPI partagé,
    // rien n'est rejoué sur le PC). Réglage au clic droit sur PTT ; mémorisé dans settings.ini (micro.*).
    static class MicRoute
    {
        static RigMonitor mon;
        public static string MicId { get { return Cfg.Get("micro.id", ""); } }
        public static string MicName { get { return Cfg.Get("micro.nom", ""); } }
        public static string CardId { get { return Cfg.Get("micro.carte", ""); } }
        public static string CardName { get { return Cfg.Get("micro.cartenom", ""); } }
        public static bool On { get { return Cfg.GetBool("micro.on", false); } }

        public static void AddItems(ContextMenuStrip m)
        {
            m.Items.Add(new ToolStripSeparator());
            string mic = MicId.Length > 0 ? MicName : "micro à choisir", card = CardId.Length > 0 ? CardName : "carte son à choisir";
            ToolStripMenuItem on = new ToolStripMenuItem("Micro du PC vers le poste (« " + mic + " » → « " + card + " »)"); on.Checked = On;
            on.Click += delegate { Set(!On); };
            m.Items.Add(on);
            ToolStripMenuItem mics = new ToolStripMenuItem("Micro du PC"), cards = new ToolStripMenuItem("Carte son d'émission du poste (sortie du PC reliée au poste)");
            try
            {
                foreach (AudioEndpoints.Endpoint e in AudioEndpoints.All())
                {
                    string id = e.Id, name = e.FullName ?? e.Name ?? id; ToolStripMenuItem it = new ToolStripMenuItem(name);
                    if (e.Capture)
                    {
                        it.Checked = id == MicId;
                        it.Click += delegate { Cfg.Set("micro.id", id); Cfg.Set("micro.nom", name); Cfg.Save(); CatLog.Event("micro", "micro « " + name + " »"); Update(); };
                        mics.DropDownItems.Add(it);
                    }
                    else
                    {
                        it.Checked = id == CardId;
                        it.Click += delegate { Cfg.Set("micro.carte", id); Cfg.Set("micro.cartenom", name); Cfg.Save(); CatLog.Event("micro", "carte son du poste « " + name + " »"); Update(); };
                        cards.DropDownItems.Add(it);
                    }
                }
            }
            catch (Exception ex) { CatLog.Event("micro", "lecture des cartes son impossible : " + ex.Message); }
            if (mics.DropDownItems.Count == 0) mics.DropDownItems.Add(new ToolStripMenuItem("Aucune entrée son trouvée dans Windows") { Enabled = false });
            if (cards.DropDownItems.Count == 0) cards.DropDownItems.Add(new ToolStripMenuItem("Aucune sortie son trouvée dans Windows") { Enabled = false });
            m.Items.Add(mics); m.Items.Add(cards);
        }

        public static void Set(bool on)
        {
            if (on && (MicId.Length == 0 || CardId.Length == 0)) { MessageBox.Show("Choisissez d'abord le micro et la carte son d'émission du poste (clic droit sur PTT).", "Micro vers le poste"); return; }
            Cfg.SetBool("micro.on", on); Cfg.Save(); CatLog.Event("micro", on ? "activé" : "désactivé"); Update();
        }

        // démarre, arrête ou rouvre l'aiguillage selon le réglage (aussi appelée au lancement)
        public static void Update()
        {
            bool want = On && MicId.Length > 0 && CardId.Length > 0;
            if (mon != null && (!want || mon.CaptureId != MicId || mon.OutputId != CardId)) Stop();
            if (want && mon == null)
            {
                RigMonitor r = new RigMonitor(MicId, CardId, false, true, "Micro vers le poste");
                r.Problem += delegate(string msg) { if (msg != null) CatLog.Event("micro", "carte son indisponible : " + msg); };
                mon = r;
            }
        }
        public static void Stop() { if (mon == null) return; RigMonitor r = mon; mon = null; r.Stop(); CatLog.Event("micro", "arrêté"); }
    }
}
