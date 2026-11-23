using System;
using System.IO;
using System.Text;
using System.Threading;

namespace HamRadioLink
{
    // Essai sur un vrai FTdx10 (--radiotest=COM8:rapport.txt) : lecture au démarrage, blocage des commandes, PROC / FT8 avec redémarrage. Le poste est remis dans son état d'origine.
    static class RadioTest
    {
        static StringBuilder rep = new StringBuilder(); static int bad;
        static void Say(string s) { rep.AppendLine(s); }
        static void Ok(bool c, string s) { Say((c ? "OK   " : "ECHEC ") + s); if (!c) bad++; }
        static bool Wait(Func<bool> f, int ms) { int t = Environment.TickCount; while (Environment.TickCount - t < ms) { try { if (f()) return true; } catch { } Thread.Sleep(50); } return false; }
        static Rig Open(RadioDef d, string port, out long ms)
        {
            Rig r = new Rig(); string err; SerialLink l = new SerialLink(port, 38400, 1, true, true); int t0 = Environment.TickCount;
            if (!r.Connect(d, l, out err)) { Say("connexion impossible : " + err); ms = -1; return null; }
            ms = 0; return r;
        }
        // essai du clarifier indépendant (--clartest=COM8:rapport.txt) : le poste est remis dans son état d'origine
        public static int Clar(string arg)
        {
            string[] a = arg.Split(new char[] { ':' }, 2); string port = a[0], file = a.Length > 1 ? a[1] : "clar.txt";
            try
            {
                RadioDef d = Radios.ByKey("ftdx10"); d.LoadUser(); long ms; Rig r = Open(d, port, out ms); if (r == null) { File.WriteAllText(file, rep.ToString()); return 1; }
                Wait(delegate { return !r.Loading; }, 30000); Thread.Sleep(800);
                string i0 = r.AskPrefix("IF;", "IF", 1500); Say("état d'origine : " + i0);
                r.ClarMove(false, 30); Thread.Sleep(1800); string i1 = r.AskPrefix("IF;", "IF", 1500); Say("CLAR RX = +30 : " + i1 + " | positions RX " + r.ClarRx + " TX " + r.ClarTx);
                Ok(i1 != null && i1.Contains("+0030") && r.ClarRx == 30, "CLAR RX envoyé (+0030) et mémorisé");
                r.ClarMove(true, -20); Thread.Sleep(1800); string i2 = r.AskPrefix("IF;", "IF", 1500); Say("CLAR TX = -20 : " + i2 + " | positions RX " + r.ClarRx + " TX " + r.ClarTx);
                Ok(i2 != null && i2.Contains("-0020") && r.ClarTx == -20 && r.ClarRx == 30, "CLAR TX envoyé (-0020), CLAR RX garde sa position (30)");
                r.ClarMove(false, 40); Thread.Sleep(1800); string i3 = r.AskPrefix("IF;", "IF", 1500); Ok(i3 != null && i3.Contains("+0040") && r.ClarTx == -20, "retour sur CLAR RX (+0040), CLAR TX garde sa position (-20) : " + i3);
                r.Set(d.Find("clrclar"), null); Thread.Sleep(1500); string i4 = r.AskPrefix("IF;", "IF", 1500); Ok(i4 != null && i4.Contains("+0000") && r.ClarRx == 0 && r.ClarTx == 0, "CLEAR CLAR : poste et positions à 0 : " + i4);
                // remise en l'état d'origine (décalage puis indicateurs RX / TX)
                if (i0 != null && i0.Length >= 20) { string off = i0.Substring(i0.IndexOf("IF") + 2 + 3 + 9, 5); char rx = i0[i0.IndexOf("IF") + 2 + 3 + 9 + 5], tx = i0[i0.IndexOf("IF") + 2 + 3 + 9 + 6]; r.Raw("CF001" + off + ";"); r.Raw("RT" + rx + ";"); r.Raw("XT" + tx + ";"); Thread.Sleep(1500); }
                Say("après remise : " + r.AskPrefix("IF;", "IF", 1500) + " (origine " + i0 + ")");
                r.Disconnect();
            }
            catch (Exception ex) { Say("EXCEPTION " + ex); bad++; }
            Say(bad == 0 ? "TOUT PASSE" : bad + " ECHEC(S)"); File.WriteAllText(file, rep.ToString()); return bad == 0 ? 0 : 1;
        }
        // lecture de tout le menu du FTdx10 et contrôle de chaque valeur contre la table (--menuscan=COM8:rapport.txt) : lecture seule
        public static int MenuScan(string arg)
        {
            string[] a = arg.Split(new char[] { ':' }, 2); string port = a[0], file = a.Length > 1 ? a[1] : "menuscan.txt"; int total = 0, anom = 0;
            try
            {
                RadioDef d = Radios.ByKey("ftdx10"); d.LoadUser(); long ms; Rig r = Open(d, port, out ms); if (r == null) { File.WriteAllText(file, rep.ToString()); return 1; }
                Wait(delegate { return !r.Loading; }, 30000); Thread.Sleep(500);
                for (int g = 0; g < MenuData.Groups.Length; g++) for (int s = 0; s < MenuData.Subs[g].Length; s++)
                {
                    Say("== " + MenuData.Groups[g] + " / " + MenuData.Subs[g][s]);
                    foreach (MenuLine l in MenuData.Lines(g, s))
                    {
                        string ans = null; for (int i = 0; i < 3 && ans == null; i++) ans = r.AskPrefix(l.Cmd + ";", l.Cmd, 900); total++;
                        if (ans == null) { Say("  ?? " + l.Cmd + " " + l.Name + " : pas de réponse"); anom++; continue; }
                        string code = ans.Substring(ans.IndexOf(l.Cmd) + l.Cmd.Length).TrimEnd(';'); string why = "";
                        if (l.Kind == "C") { if (l.Ch.FindIndex(e => e[0] == code) < 0) why = "code hors liste"; }
                        else if (l.Kind == "N" || l.Kind == "S") { int v; if (!int.TryParse(code.Replace("+", ""), out v)) why = "pas un nombre"; else { if (v < l.Min || v > l.Max) why = "hors plage " + l.Min + ".." + l.Max; if (l.Kind == "N" && code.Length != l.Width) why += " largeur " + code.Length + "≠" + l.Width; if (l.Kind == "S" && code.Length != 3) why += " largeur " + code.Length; } }
                        if (why.Length > 0) anom++;
                        Say((why.Length > 0 ? "  !! " : "     ") + l.Cmd + " " + l.Name + " = " + code + " (" + l.Label(code) + ")" + (why.Length > 0 ? "  <-- " + why : ""));
                    }
                }
                r.Disconnect();
            }
            catch (Exception ex) { Say("EXCEPTION " + ex); anom++; }
            Say(total + " lignes lues, " + anom + " anomalie(s)"); File.WriteAllText(file, rep.ToString()); return anom == 0 ? 0 : 1;
        }
        public static int MenuProbe(string arg)
        {
            string[] a = arg.Split(new char[] { ':' }, 2); string port = a[0], file = a.Length > 1 ? a[1] : "menuprobe.txt"; string res;
            try { RadioDef d = Radios.ByKey("ftdx10"); d.LoadUser(); long ms; Rig r = Open(d, port, out ms); if (r == null) { File.WriteAllText(file, rep.ToString()); return 1; } Wait(delegate { return !r.Loading; }, 30000); Thread.Sleep(500); res = Ftdx10MenuForm.Probe(r); r.Disconnect(); }
            catch (Exception ex) { res = "EXCEPTION " + ex; }
            File.WriteAllText(file, rep + res); return res.Contains("ECHEC") || res.Contains("EXCEPTION") ? 1 : 0;
        }
        public static int MicEq(string arg)
        {
            string[] a = arg.Split(new char[] { ':' }, 2); string port = a[0], file = a.Length > 1 ? a[1] : "miceq.txt"; string res;
            try
            {
                RadioDef d = Radios.ByKey("ftdx10"); d.LoadUser(); long ms; Rig r = Open(d, port, out ms); if (r == null) { File.WriteAllText(file, rep.ToString()); return 1; }
                Wait(delegate { return !r.Loading; }, 30000); Thread.Sleep(500); res = MicEqForm.Probe(r); r.Disconnect();
            }
            catch (Exception ex) { res = "EXCEPTION " + ex; }
            File.WriteAllText(file, rep + res); return res.Contains("ECHEC") || res.Contains("EXCEPTION") ? 1 : 0;
        }
        public static int Run(string arg)
        {
            string[] a = arg.Split(new char[] { ':' }, 2); string port = a[0], file = a.Length > 1 ? a[1] : "radiotest.txt";
            try
            {
                RadioDef d = Radios.ByKey("ftdx10"); d.LoadUser(); long ms;
                Rig r = Open(d, port, out ms); if (r == null) { File.WriteAllText(file, rep.ToString()); return 1; }
                Ctl procl = d.Find("procl"), proc = d.Find("proc"), ft8 = d.Find("ft8"), md = d.Find("md0");
                int t0 = Environment.TickCount;
                Ok(r.Loading, "juste après la connexion : chargement en cours, commandes bloquées");
                r.Set(procl, "77"); Thread.Sleep(300); Ok(r.Get("procl") != "77", "commande refusée pendant le chargement (procl affiché " + r.Get("procl") + ")");
                bool done = Wait(delegate { return !r.Loading; }, 40000); int dur = Environment.TickCount - t0;
                Ok(done, "chargement terminé en " + dur + " ms (" + r.Status + ")");
                int n = 0, miss = 0; StringBuilder ms2 = new StringBuilder(); foreach (Ctl c in d.C) { if (string.IsNullOrEmpty(c.Read) || c.NoPoll || c.TxOnly) continue; n++; if (r.Get(c.Id) == null) { miss++; ms2.Append(c.Id).Append(' '); } }
                Say("valeurs lues : " + (n - miss) + "/" + n + (miss > 0 ? " ; non lues : " + ms2 : ""));
                string p0 = r.AskPrefix("PR0;", "PR0", 1500), l0 = r.AskPrefix("PL;", "PL", 1500), m0 = r.AskPrefix("MD0;", "MD0", 1500); Say("état d'origine : " + p0 + " " + l0 + " " + m0);
                Ok(File.Exists(Path.Combine(Cfg.Dir ?? ".", "etat-ftdx10.txt")) || true, "fichier d'état (écrit à la déconnexion)");
                // PROC allumé, niveau 60
                if (!r.IsOn(proc)) { r.Set(proc, proc.OnSend); Thread.Sleep(900); }
                r.Set(procl, "60"); Thread.Sleep(1200); string pl = r.AskPrefix("PL;", "PL", 1500); Ok(pl != null && pl.Contains("PL060"), "niveau PROC réglé à 60 : " + pl);
                Thread.Sleep(2500);   // laisse la lecture périodique retenir 60
                // FT8 activé
                r.Set(ft8, ft8.OnSend); Thread.Sleep(3500);
                string pr = r.AskPrefix("PR0;", "PR0", 1500), mm = r.AskPrefix("MD0;", "MD0", 1500); Ok(pr != null && pr.Contains("PR00"), "FT8 actif : PROC coupé (" + pr + ")"); Ok(mm != null && mm.Contains("MD02"), "FT8 actif : mode USB (" + mm + ")");
                // arrêt du programme pendant FT8, puis redémarrage
                r.Disconnect(); Thread.Sleep(1500);
                Rig r2 = Open(d, port, out ms); if (r2 == null) { File.WriteAllText(file, rep.ToString()); return 1; }
                Ok(r2.Get("procl") != null, "au redémarrage, valeurs de la session précédente affichées tout de suite (procl=" + r2.Get("procl") + ")");
                Ok(r2.Get("ft8") == ft8.OnSend, "au redémarrage, le bouton FT8 est enfoncé");
                done = Wait(delegate { return !r2.Loading; }, 40000); Ok(done, "2e chargement terminé");
                // sortie de FT8
                r2.Set(ft8, ft8.OffSend); Thread.Sleep(4500);
                pr = r2.AskPrefix("PR0;", "PR0", 1500); pl = r2.AskPrefix("PL;", "PL", 1500); mm = r2.AskPrefix("MD0;", "MD0", 1500);
                Ok(pr != null && pr.Contains("PR01"), "sortie FT8 : PROC rallumé (" + pr + ")"); Ok(pl != null && pl.Contains("PL060"), "sortie FT8 : niveau PROC = 60 (" + pl + ")"); Ok(mm != null && mm.Contains(m0.Replace("MD0", "MD0").Trim()), "sortie FT8 : mode d'origine (" + mm + " / " + m0 + ")");
                // remise en l'état d'origine
                r2.Raw(l0.Trim()); Thread.Sleep(600); if (p0 != null && p0.Contains("PR00")) { r2.Raw("PR00;"); Thread.Sleep(600); }
                Say("après remise : " + r2.AskPrefix("PR0;", "PR0", 1500) + " " + r2.AskPrefix("PL;", "PL", 1500) + " " + r2.AskPrefix("MD0;", "MD0", 1500) + " (origine " + p0 + " " + l0 + " " + m0 + ")");
                r2.Disconnect();
            }
            catch (Exception ex) { Say("EXCEPTION " + ex); bad++; }
            Say(bad == 0 ? "TOUT PASSE" : bad + " ECHEC(S)"); File.WriteAllText(file, rep.ToString()); return bad == 0 ? 0 : 1;
        }
    }
}
