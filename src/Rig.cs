using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace HamRadioLink
{
    // Moteur CAT : envoie les commandes, lit en boucle l'état du poste et le range dans un dictionnaire id -> texte.
    public class Rig
    {
        public RadioDef Def; public ILink Link;
        public readonly ConcurrentDictionary<string, string> V = new ConcurrentDictionary<string, string>();
        public volatile int Version; public volatile bool Connected; public string Status = "Déconnecté — choisissez l'interface et le port en haut à droite, puis CONNECTER (le poste virtuel sert de démonstration)"; public string SeenId = "";
        public event Action<string, bool> Log;            // ligne, true = envoyée
        public int Errors;                                // réponses « ?; » reçues
        public string LastError = "";
        public int Gap = 25;                              // pause entre deux commandes (ms)

        readonly object sync = new object();
        readonly Queue<string> outq = new Queue<string>();
        readonly Dictionary<string, string> coal = new Dictionary<string, string>();
        readonly Dictionary<string, long> lastCoal = new Dictionary<string, long>();
        readonly Dictionary<string, long> hold = new Dictionary<string, long>();
        readonly Dictionary<Ctl, long> next = new Dictionary<Ctl, long>();
        readonly Dictionary<Ctl, int> fails = new Dictionary<Ctl, int>();
        readonly HashSet<Ctl> dead = new HashSet<Ctl>(); readonly Dictionary<Ctl, long> deadAt = new Dictionary<Ctl, long>();
        readonly HashSet<Ctl> got = new HashSet<Ctl>(); volatile bool loaded; long lastSave, lastLoadMsg; int loadTotal, loadDone;
        readonly StringBuilder rx = new StringBuilder();
        readonly System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();
        volatile bool sendPoll; Thread th; volatile bool run; Ctl lastRead; long lastSentT, lastRxT, startT; string lastCmd = "";
        static readonly Regex IdRx = new Regex(@"^(?:ID(\d{3,4})|1900([0-9A-F]{2}));$");

        public string Get(string id) { string v; return V.TryGetValue(id, out v) ? v : null; }
        public long GetL(string id) { long n; string v = Get(id); return v != null && long.TryParse(v, System.Globalization.NumberStyles.AllowLeadingSign, System.Globalization.CultureInfo.InvariantCulture, out n) ? n : 0; }
        public bool IsOn(Ctl c) { return c.IsOn(Get(c.Id)); }
                // délai de sécurité TX (comme le PTT de SDRLink) : une émission LANCÉE PAR LA TOUCHE PTT est coupée au bout du délai choisi (clic droit sur PTT ; 0 = désactivé)
        public bool PttByButton; long pttSince; bool pttSaw;
        public int PttLimit { get { return Def != null ? Cfg.GetInt("ptt.timeout." + Def.Key, Def.Key == "ft2000" ? 30 : 0) : 0; } }
        public void PttButtonStart() { pttSince = clock.ElapsedMilliseconds; PttByButton = true; pttSaw = false; }
        public void PttButtonStop() { PttByButton = false; }
        public int PttLeft { get { int lim = PttLimit; if (!PttByButton || lim <= 0) return -1; return Math.Max(0, lim - (int)((clock.ElapsedMilliseconds - pttSince) / 1000)); } }
        // à appeler souvent : coupe l'émission à l'échéance ; true = coupée à l'instant
        public bool PttCheck()
        {
            if (!PttByButton) return false; Ctl p = Def != null ? Def.Find("ptt") : null; if (p == null) { PttByButton = false; return false; }
            long el = clock.ElapsedMilliseconds - pttSince;
            if (Tx) pttSaw = true; else if (pttSaw || (el > 5000 && Link != null && Connected)) { PttByButton = false; return false; }      // fin d'émission (touche, Échap, poste…) ou le poste n'a jamais émis
            int lim = PttLimit; if (lim <= 0 || el < lim * 1000L) return false;
            PttByButton = false; Set(p, p.OffSend); { Ctl mx = Def.Find("mox"); if (mx != null && IsOn(mx)) Set(mx, mx.OffSend); } CatLog.Event(Def.Key, "émission coupée après " + lim + " s (délai de sécurité TX)"); return true;
        }
        public bool Tx { get { Ctl p = Def != null ? Def.Find("ptt") : null; return p != null && p.IsOn(Get("ptt")); } }
        public void Feed(string s) { OnData(s); }
        public bool ReadDisabled(Ctl c) { return dead.Contains(c); }

        // ---- FTdx10 : deux réglages de clarifier indépendants (CLAR RX / CLAR TX). Le poste n'a qu'un décalage : le programme garde chaque position et envoie celle du bouton tourné,
        // avec le clarifier correspondant seul actif. Les positions sont mémorisées (settings.ini) et suivent les changements faits sur le poste.
        public int ClarRx, ClarTx; int clarSent = int.MinValue; bool inClar;
        void ClarSave() { if (Def == null) return; Cfg.SetInt("clar." + Def.Key + ".rx", ClarRx); Cfg.SetInt("clar." + Def.Key + ".tx", ClarTx); }
        public void ClarMove(bool tx, int v)
        {
            Ctl rx = Def.Find("clarrx"), txc = Def.Find("clartx"), cl = Def.Find("clar"); if (rx == null || txc == null || cl == null) return;
            if (tx) ClarTx = v; else ClarRx = v; ClarSave();
            inClar = true;
            try
            {
                if (tx) { if (!IsOn(txc)) Set(txc, txc.OnSend); if (IsOn(rx)) Set(rx, rx.OffSend); } else { if (!IsOn(rx)) Set(rx, rx.OnSend); if (IsOn(txc)) Set(txc, txc.OffSend); }
                clarSent = v; Set(cl, v.ToString());
            }
            finally { inClar = false; }
        }
        // décalage lu sur le poste : s'il ne vient pas de nous, il a été changé sur le poste -> mis dans la position du clarifier actif
        void ClarSeen()
        {
            Ctl rx = Def.Find("clarrx"), txc = Def.Find("clartx"); int v; string s = Get("clar");
            if (rx == null || txc == null || s == null || !int.TryParse(s, System.Globalization.NumberStyles.AllowLeadingSign, System.Globalization.CultureInfo.InvariantCulture, out v) || v == clarSent) return;
            clarSent = v; bool r = IsOn(rx), t = IsOn(txc);
            if (t && !r) ClarTx = v; else if (r && !t) ClarRx = v; else { ClarRx = v; if (r && t) ClarTx = v; }
            ClarSave();
        }

        // ---- chargement initial : toutes les commandes sont refusées tant que le poste n'a pas été lu en entier ; les positions de la dernière session s'affichent entre-temps ----
        public static bool NoGate;   // essais automatiques : pas de blocage pendant la lecture initiale
        public bool Loading { get { return !NoGate && Link != null && !loaded; } }
        string StateFile { get { return System.IO.Path.Combine(Cfg.Dir ?? ".", "etat-" + (Def != null ? Def.Key : "poste") + ".txt"); } }
        static bool Remembered(Ctl c) { return c != null && !c.Local && c.Id != "ptt" && !c.TxOnly && c.K != Kind.Freq && !c.Id.StartsWith("md") && c.Id != "fa" && c.Id != "fb" && c.K != Kind.Meter && c.K != Kind.Gauge && c.K != Kind.Led && c.K != Kind.Action; }
        public void SaveState()
        {
            try
            {
                if (Def == null || !loaded) return; StringBuilder sb = new StringBuilder();
                foreach (KeyValuePair<string, string> kv in V) { if (kv.Value == null || kv.Value.IndexOfAny(new char[] { '\r', '\n' }) >= 0) continue; if (Remembered(Def.Find(kv.Key))) sb.Append(kv.Key).Append('=').Append(kv.Value).Append('\n'); }
                System.IO.File.WriteAllText(StateFile, sb.ToString(), new UTF8Encoding(false)); lastSave = clock.ElapsedMilliseconds;
            }
            catch { }
        }
        void LoadState(RadioDef def)
        {
            try
            {
                string p = StateFile; if (!System.IO.File.Exists(p)) return;
                foreach (string l in System.IO.File.ReadAllLines(p, Encoding.UTF8)) { int i = l.IndexOf('='); if (i <= 0) continue; string id = l.Substring(0, i); if (Remembered(def.Find(id)) && !V.ContainsKey(id)) V[id] = l.Substring(i + 1); }
            }
            catch { }
        }
        // appelée par la boucle : le chargement est terminé quand chaque valeur lue en continu a répondu une fois (ou après 15 s)
        void CheckLoaded(long now)
        {
            if (loaded || !Connected || Def == null) return;
            int tot = 0, ok = 0; lock (got) { foreach (Ctl c in Def.C) { if (string.IsNullOrEmpty(c.Read) || c.NoPoll || c.TxOnly || dead.Contains(c)) continue; tot++; if (got.Contains(c)) ok++; } }
            loadTotal = tot; loadDone = ok;
            if (ok >= tot || now - startT > 15000) { loaded = true; Status = "Connecté"; Version++; CatLog.Event(Def.Key, "paramètres du poste lus (" + ok + "/" + tot + ") : commandes autorisées"); SaveState(); }
            else if (now - lastLoadMsg > 400) { lastLoadMsg = now; Status = "Lecture des paramètres du poste… " + ok + "/" + tot + " — commandes bloquées jusqu'à la fin"; Version++; }
        }
        void Fire(string s, bool tx) { Action<string, bool> l = Log; if (l != null) l(s, tx); }

        // ------------------------------------------------------------- connexion
        public bool Connect(RadioDef def, ILink link, out string err)
        {
            Disconnect();
            Def = def; Link = link; foreach (string k in new List<string>(V.Keys)) { Ctl lc = def.Find(k); string rm; if (lc == null || !lc.Local) V.TryRemove(k, out rm); } { Ctl f8 = def.Find("ft8"); if (f8 != null && f8.Local && Cfg.Get("ft8." + def.Key + ".act", "") == "1") V[f8.Id] = f8.OnSend; }   /* FT8 était actif à l'arrêt : le bouton le montre, un clic remet l'état d'avant */ loaded = false; lock (got) got.Clear(); LoadState(def); { string pk0 = Cfg.Get("pl." + def.Key, ""); if (pk0.Length > 0) plKeep = pk0; }   if (def.Key == "ftdx10") { ClarRx = Cfg.GetInt("clar.ftdx10.rx", 0); ClarTx = Cfg.GetInt("clar.ftdx10.tx", 0); clarSent = int.MinValue; } next.Clear(); fails.Clear(); dead.Clear(); deadAt.Clear(); hold.Clear(); coal.Clear(); lock (sync) outq.Clear(); lock (rx) rx.Length = 0;
            Errors = 0; LastError = ""; SeenId = ""; Gap = link.Baud >= 38400 ? 22 : (link.Baud >= 9600 ? 35 : 60);
            link.Data += OnData;
            if (!link.Open(out err)) { link.Data -= OnData; Link = null; Status = "Échec : " + err; Version++; CatLog.Event(def.Key, "connexion impossible : " + err + " (" + link.Name + ")"); return false; }
            CatLog.Event(def.Key, "connexion ouverte : " + link.Name);
            Status = "Connexion à " + link.Name + "…"; Connected = false; startT = lastRxT = clock.ElapsedMilliseconds;
            run = true; th = new Thread(Loop); th.IsBackground = true; th.Name = "CatLoop"; th.Priority = System.Threading.ThreadPriority.AboveNormal; th.Start();
            Raw(def.IdCmd); if (!string.IsNullOrEmpty(def.Init)) Raw(def.Init);
            Version++; return true;
        }
        public void Disconnect()
        {
            SaveState(); run = false; ILink l = Link; Link = null; if (l != null) CatLog.Event(Def != null ? Def.Key : "?", "déconnexion de " + l.Name);
            if (l != null) { try { l.Data -= OnData; l.Close(); } catch { } }
            Connected = false; Status = "Déconnecté — choisissez l'interface et le port en haut à droite, puis CONNECTER (le poste virtuel sert de démonstration)"; Version++;
        }

        // ------------------------------------------------------------- écriture
        // v : valeur à placer dans {v} (pour une action : null). La mise à jour locale est immédiate.
        public void Set(Ctl c, string v)
        {
            if (Loading && !(c != null && c.Id == "ptt" && v == c.OffSend)) return;   // rien n'est envoyé au poste avant la fin de la lecture de tous ses paramètres
            if (v != null && Def != null && RxAudio.Local(this, c, v)) { V[c.Id] = v; Version++; return; }   // volume AF = volume de l'écoute sur le PC quand le haut-parleur du poste est coupé
            SetRadio(c, v);
        }
        public void ShowLocal(string id, string v) { V[id] = v; Version++; }
        // écriture réelle vers le poste (sans détour par le volume de l'écoute sur le PC)
        public void SetRadio(Ctl c, string v)
        {
            if (Def != null && Def.Key == "ftdx10" && !Loading)
            {
                if (c.Id == "clrclar" || c.Id == "clr") { ClarRx = ClarTx = 0; ClarSave(); clarSent = 0; }   // CLEAR CLAR : les deux positions et le décalage du poste à 0
                else if (!inClar && v != null && (c.Id == "clarrx" || c.Id == "clartx") && c.IsOn(v))
                { Ctl cl = Def.Find("clar"); int o = c.Id == "clarrx" ? ClarRx : ClarTx; if (cl != null) { clarSent = o; Raw(Tpl.Fill(cl.Set, o.ToString())); } }   // le clarifier qu'on allume reprend sa propre position
            }
            if (Loading && !(c != null && c.Id == "ptt" && v == c.OffSend)) return;
            if (v != null) V[c.Id] = v; Version++;
            Hooks(c, v);
            if (c.Local) { if (c.K == Kind.Choice && v != null) Cfg.Set("local." + c.Id, v); return; }
            hold[c.Id] = clock.ElapsedMilliseconds + 900;
            if (c.Id == "band" && v == "13" && Def.Key == "ftdx10") { Raw("FA070000000;"); return; }   // touche 70 : début de la bande 70 MHz (Europe : 70,000 à 70,500 MHz), pas de code BS pour cette bande
            if (string.IsNullOrEmpty(c.Set)) return;
            if (c.K == Kind.Knob || c.K == Kind.Freq) { lock (sync) coal[c.Id] = v; return; }
            Raw(Tpl.Fill(c.Set, v));
        }
        // Enchaînements propres au poste : PROC retrouve son niveau ; « FT8 / FT4 » met en USB et coupe PROC / MIC EQ, puis les remet
        string plKeep, ft8Mode; bool ft8Proc, ft8Eq, ft8Quiet;
        // niveau de PROC à retrouver : seul un niveau lu PROC allumé (> 0) est retenu, et mémorisé sur disque (FTdx10 : PROC coupé, le poste lit 000 et, à la remise en marche,
        // revient à son niveau de menu s'il n'est pas réécrit)
        void KeepPl(string cur) { long n; if (Def == null || string.IsNullOrEmpty(cur) || !long.TryParse(cur, out n) || n <= 0) return; plKeep = cur; Cfg.Set("pl." + Def.Key, cur); }
        void Hooks(Ctl c, string v)
        {
            if (Def == null || v == null) return;
            if (c.Id == "proc" && !c.Local)
            {
                if (c.IsOn(v)) { Ctl pl = Def.Find("procl"); if (pl != null && !string.IsNullOrEmpty(plKeep)) Set(pl, plKeep); }
                else { string cur = Get("procl"); KeepPl(cur); }
            }
            else if (c.Id == "ft8" && c.Local)
            {
                Ctl pr = Def.Find("proc"), eq = Def.Find("meq"), md = Def.Find("md0");
                if (c.IsOn(v))
                {
                    string fk = "ft8." + Def.Key + ".";
                    if (Cfg.Get(fk + "act", "") == "1")
                    {   // FT8 déjà actif au moment de l'arrêt du programme : on garde l'état d'avant (PROC, MIC EQ, mode, niveau de PROC)
                        ft8Proc = Cfg.Get(fk + "proc", "") == "1"; ft8Eq = Cfg.Get(fk + "eq", "") == "1"; ft8Mode = Cfg.Get(fk + "mode", ""); string pk = Cfg.Get(fk + "pl", ""); if (pk.Length > 0) plKeep = pk;
                    }
                    else
                    {
                        ft8Proc = pr != null && IsOn(pr); ft8Eq = eq != null && IsOn(eq); ft8Mode = Get("md0"); string cur0 = Get("procl"); if (ft8Proc) KeepPl(cur0);
                        Cfg.Set(fk + "act", "1"); Cfg.Set(fk + "proc", ft8Proc ? "1" : "0"); Cfg.Set(fk + "eq", ft8Eq ? "1" : "0"); Cfg.Set(fk + "mode", ft8Mode ?? ""); Cfg.Set(fk + "pl", plKeep ?? ""); Cfg.Save();   // mémorisé : survit à l'arrêt du programme
                    }
                    List<KeyValuePair<Ctl, string>> q = new List<KeyValuePair<Ctl, string>>();
                    if (md != null) q.Add(new KeyValuePair<Ctl, string>(md, "2"));
                    if (ft8Proc) q.Add(new KeyValuePair<Ctl, string>(pr, pr.OffSend));
                    if (ft8Eq) q.Add(new KeyValuePair<Ctl, string>(eq, eq.OffSend));
                    Seq(q);
                }
                else
                {
                    string fk0 = "ft8." + Def.Key + ".";
                    if (!ft8Proc && !ft8Eq && string.IsNullOrEmpty(ft8Mode) && Cfg.Get(fk0 + "act", "") == "1")
                    {   // le programme a été relancé pendant FT8 : on reprend l'état mémorisé
                        ft8Proc = Cfg.Get(fk0 + "proc", "") == "1"; ft8Eq = Cfg.Get(fk0 + "eq", "") == "1"; ft8Mode = Cfg.Get(fk0 + "mode", ""); string pk = Cfg.Get(fk0 + "pl", ""); if (pk.Length > 0) plKeep = pk;
                    }
                    List<KeyValuePair<Ctl, string>> q = new List<KeyValuePair<Ctl, string>>();
                    if (!ft8Quiet && md != null && !string.IsNullOrEmpty(ft8Mode)) q.Add(new KeyValuePair<Ctl, string>(md, ft8Mode));
                    if (ft8Proc && pr != null) q.Add(new KeyValuePair<Ctl, string>(pr, pr.OnSend));
                    if (ft8Eq && eq != null) q.Add(new KeyValuePair<Ctl, string>(eq, eq.OnSend));
                    Seq(q); ft8Proc = ft8Eq = false; ft8Mode = null;
                    string fk = "ft8." + Def.Key + "."; if (Cfg.Get(fk + "act", "") == "1") { Cfg.Set(fk + "act", ""); Cfg.Save(); }
                }
            }
            else if (c.Id == "md0" && v != "2" && v != "C") { Ctl f = Def.Find("ft8"); if (f != null && IsOn(f)) { ft8Quiet = true; try { Set(f, f.OffSend); } finally { ft8Quiet = false; } } }
        }
        // Plusieurs ordres à la suite, avec une pause entre chacun : le poste en laisse tomber s'ils arrivent trop vite
        void Seq(List<KeyValuePair<Ctl, string>> q)
        {
            StringBuilder sb = new StringBuilder(); long now = clock.ElapsedMilliseconds;
            foreach (KeyValuePair<Ctl, string> p in q)
            {
                Ctl c = p.Key; string v = p.Value; if (c == null || string.IsNullOrEmpty(c.Set)) continue;
                V[c.Id] = v; hold[c.Id] = now + 4000 + 400 * q.Count; sb.Append(Tpl.Fill(c.Set, v)).Append("{wait:350}");
                if (c.Id == "proc")
                {
                    Ctl pl = Def.Find("procl");
                    if (!c.IsOn(v)) { string cur = Get("procl"); KeepPl(cur); }
                    else if (pl != null && !string.IsNullOrEmpty(plKeep) && !string.IsNullOrEmpty(pl.Set)) { V[pl.Id] = plKeep; hold[pl.Id] = now + 4000 + 400 * q.Count; sb.Append(Tpl.Fill(pl.Set, plKeep)).Append("{wait:350}"); }
                }
            }
            Version++; Raw(sb.ToString());
        }
        // envoi immédiat d'un réglage, sans regroupement avec les suivants : l'ordre part avant les commandes qui viennent après
        public void SetNow(Ctl c, string v)
        {
            if (c == null || string.IsNullOrEmpty(c.Set) || v == null) return;
            if (Loading && !(c.Id == "ptt" && v == c.OffSend)) return;
            V[c.Id] = v; hold[c.Id] = clock.ElapsedMilliseconds + 2500; Version++; Raw(Tpl.Fill(c.Set, v));
        }
        // texte brut (déjà complet, peut contenir {wait:ms})
        public void Raw(string text) { if (string.IsNullOrEmpty(text)) return; lock (sync) outq.Enqueue(text); }

        // Envoie tout de suite la lecture d'un élément et attend la réponse (essais, éditeur de commande)
        public string Ask(string cmd, int timeoutMs)
        {
            ManualResetEvent ev = new ManualResetEvent(false); string got = null; Action<string, bool> h = delegate(string s, bool tx) { if (!tx && got == null) { got = s; ev.Set(); } };
            Log += h; try { Raw(cmd); ev.WaitOne(timeoutMs); } finally { Log -= h; }
            return got;
        }

        // Comme Ask, mais n'accepte que la réponse commençant par « prefix » (les relectures périodiques du poste ne sont pas prises pour la réponse) ; « ?; » = refusé
        public string AskPrefix(string cmd, string prefix, int timeoutMs)
        {
            ManualResetEvent ev = new ManualResetEvent(false); string got = null; Action<string, bool> h = delegate(string s, bool tx) { if (!tx && got == null && s != null && (s.StartsWith(prefix) || s.Trim() == "?;")) { got = s; ev.Set(); } };
            Log += h; try { Raw(cmd); ev.WaitOne(timeoutMs); } finally { Log -= h; }
            return got != null && got.StartsWith(prefix) ? got : null;
        }
        // ------------------------------------------------------------- boucle
        void Loop()
        {
            while (run)
            {
                try
                {
                    string cmd = null; long now = clock.ElapsedMilliseconds;
                    lock (sync)
                    {
                        if (outq.Count > 0) cmd = outq.Dequeue();
                        else if (coal.Count > 0)
                        {
                            foreach (KeyValuePair<string, string> kv in coal)
                            {
                                long lc; lastCoal.TryGetValue(kv.Key, out lc);
                                if (now - lc >= 45) { Ctl c = Def.Find(kv.Key); if (c != null) { cmd = Tpl.Fill(c.Set, kv.Value); } lastCoal[kv.Key] = now; coal.Remove(kv.Key); break; }
                            }
                        }
                    }
                    CheckLoaded(now); if (loaded && now - lastSave > 15000) SaveState();
                    bool poll = false; if (cmd == null) { cmd = NextPoll(now); poll = cmd != null; } sendPoll = poll;
                    if (cmd != null) SendText(cmd);
                    Thread.Sleep(Gap);
                    // pas de réponse du tout
                    if (!Connected && clock.ElapsedMilliseconds - startT > 3500 && Status.StartsWith("Connexion")) { Status = "Aucune réponse du poste : vérifiez le port COM, la vitesse (menu CAT RATE), RTS et le câble."; Version++; CatLog.Event(Def != null ? Def.Key : "?", "aucune réponse du poste depuis la connexion"); AlertLog.Add(Def != null ? Def.Name : "?", "Liaison impossible : aucune réponse du poste depuis la connexion (port COM, vitesse, câble, RTS)"); }
                    if (Connected && clock.ElapsedMilliseconds - lastRxT > 6000) { Connected = false; Status = "Plus de réponse du poste"; Version++; CatLog.Event(Def != null ? Def.Key : "?", "plus de réponse du poste depuis 6 s"); AlertLog.Add(Def != null ? Def.Name : "?", "Perte de liaison : plus de réponse du poste depuis 6 s (câble, port COM ou poste éteint ?)"); }
                }
                catch (ThreadAbortException) { return; }
                catch (Exception ex) { Fire("ERREUR " + ex.Message, false); CatLog.Event(Def != null ? Def.Key : "?", "ERREUR " + ex.Message); }
            }
        }

        string NextPoll(long now)
        {
            Ctl best = null; long bt = long.MaxValue; bool tx = Tx;
            foreach (Ctl c in Def.C)
            {
                if (string.IsNullOrEmpty(c.Read) || c.NoPoll) continue;
                if (dead.Contains(c)) { long da; if (deadAt.TryGetValue(c, out da) && now - da > 15000) { dead.Remove(c); fails[c] = 2; } else continue; }   // une lecture refusée 3 fois (poste occupé au démarrage) est retentée toutes les 15 s
                if (c.TxOnly && !tx) continue;
                long h; if (hold.TryGetValue(c.Id, out h) && now < h) continue;
                long n; if (!next.TryGetValue(c, out n)) n = 0;
                if (n <= now && n < bt) { bt = n; best = c; }
            }
            if (best == null) return null;
            double slow = Math.Max(1.0, Gap / 22.0); int period = (int)(best.Period * slow); if (!best.TxOnly && best.K != Kind.Freq && best.Period >= 1500) period += (int)(Def.C.Count * 6 * slow);
            next[best] = now + period; lastRead = best; return best.Read;
        }

        void SendText(string text)
        {
            ILink l = Link; if (l == null) return;
            foreach (string st in Tpl.Steps(text))
            {
                if (st.Length > 0 && st[0] == '\0') { Thread.Sleep(Math.Min(5000, int.Parse(st.Substring(1)))); continue; }
                // une commande à la fois : on coupe après chaque « ; »
                int i = 0;
                while (i < st.Length)
                {
                    int j = st.IndexOf(';', i); string one = j < 0 ? st.Substring(i) : st.Substring(i, j - i + 1); i = j < 0 ? st.Length : j + 1;
                    if (one.Length == 0) continue;
                    lastCmd = one; lastSentT = clock.ElapsedMilliseconds; Fire(one, true); CatLog.Tx(Def != null ? Def.Key : "?", one, sendPoll); l.Send(one);
                    if (i < st.Length) Thread.Sleep(Gap);
                }
            }
        }

        // ------------------------------------------------------------- réception
        void OnData(string s)
        {
            List<string> msgs = new List<string>();
            lock (rx)
            {
                foreach (char ch in s) { if (ch == '\r' || ch == '\n' || ch == '\0') continue; rx.Append(ch); if (ch == ';') { msgs.Add(rx.ToString()); rx.Length = 0; } }
                if (rx.Length > 300) rx.Length = 0;
            }
            foreach (string m in msgs) Handle(m);
        }

        void Handle(string m)
        {
            Fire(m, false); CatLog.Rx(Def != null ? Def.Key : "?", m); long now = clock.ElapsedMilliseconds; lastRxT = now;
            if (m == "?;" || m == "E;" || m == "O;")
            {
                Errors++; LastError = lastCmd + " → " + m;
                if (lastRead != null && now - lastSentT < 500 && lastCmd == lastRead.Read)
                { int f; fails.TryGetValue(lastRead, out f); fails[lastRead] = ++f; if (f >= 3) { dead.Add(lastRead); deadAt[lastRead] = now; Fire("Lecture de « " + lastRead.Label + " » (" + lastRead.Read + ") refusée 3 fois : lecture désactivée.", false); } }
                return;
            }
            Match idm = IdRx.Match(m);
            if (idm.Success) { SeenId = idm.Groups[1].Success ? idm.Groups[1].Value : idm.Groups[2].Value; }
            if (!Connected) { Connected = true; Status = "Connecté"; }
            bool any = false;
            string pre = m.Substring(0, Math.Min(2, m.Length));
            foreach (Ctl c in Def.C)
            {
                Regex re = c.Rx; if (re == null) continue;
                Match mt = re.Match(m); if (!mt.Success) continue; fails.Remove(c); if (!loaded) lock (got) got.Add(c);
                foreach (string gn in re.GetGroupNames())
                {
                    if (char.IsDigit(gn[0])) continue; string key = gn == "v" ? c.Id : gn; Group g = mt.Groups[gn]; if (!g.Success) continue;
                    long h; if (hold.TryGetValue(key, out h) && now < h) continue;
                    if (key == "af" && RxAudio.Active(this)) continue;   // volume AF affiché = volume de l'écoute sur le PC (le poste, lui, est à 0)
                    string val = g.Value; Ctl tgt = key == c.Id ? c : Def.Find(key);
                    if (tgt != null && tgt.K == Kind.Choice) { foreach (Item it in tgt.ItemList) if (it.Match(val)) { val = it.Code; break; } }
                    string old; if (!V.TryGetValue(key, out old) || old != val) { V[key] = val; any = true; }
                    if (key == "procl") { Ctl pc = Def.Find("proc"); if (pc != null && pc.IsOn(Get("proc"))) KeepPl(val); }   // dernier niveau de PROC lu PROC allumé
                }
            }
            if (any && Def.Key == "ftdx10") ClarSeen();
            if (any) Version++;
        }

        // ------------------------------------------------------------- aides
        // Nom du poste d'après l'identifiant ID reçu
        public static string IdName(string id)
        {
            switch (id) { case "0761": return "FTdx10"; case "0251": return "FT-2000"; case "0252": return "FT-2000D"; case "0570": return "FT-991"; case "0310": return "FT-950"; case "020": return "TS-480"; case "024": return "TS-890"; default: return null; }
        }
    }
}
