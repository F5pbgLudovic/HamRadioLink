using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace HamRadioLink
{
    // Autotest : HamRadioLink.exe --selftest=rapport.txt
    public static class SelfTest
    {
        static StringBuilder sb; static int ok, ko;
        static void Check(bool c, string what) { if (c) ok++; else { ko++; sb.AppendLine("ÉCHEC : " + what); } }
        static void Info(string s) { sb.AppendLine(s); }
        // ---- cohérence de TOUS les postes : dispositions, S-mètre, tables du menu, boutons spéciaux (--consist=rapport.txt lance seulement cette partie)
        public static int ConsistOnly(string file) { sb = new StringBuilder(); ok = ko = 0; Rig.NoGate = true; Consistency(); sb.AppendLine(ko == 0 ? "COHÉRENCE : TOUT PASSE (" + ok + " vérifications)" : ko + " ÉCHEC(S) sur " + (ok + ko)); File.WriteAllText(file, sb.ToString()); return ko == 0 ? 0 : 1; }
        static void Consistency()
        {
            int radios = 0, outsideTot = 0, big = 0, menus = 0; List<string> out0 = new List<string>();
            foreach (RadioDef d in Radios.All())
            {
                radios++; List<Ctl> frames = d.C.FindAll(delegate(Ctl c) { return c.K == Kind.Frame; });
                // chaque élément tient entièrement dans un cadre (les cadres sont les zones de la face avant)
                int outside = 0; List<string> ids = new List<string>();
                if (frames.Count > 0) foreach (Ctl c in d.C)
                {
                    if (c.K == Kind.Frame || c.Hidden || c.Id == "tunemic") continue; bool inside = false;   // TUNE est volontairement posé dans un coin libre, hors cadre
                    foreach (Ctl f in frames) if (c.X >= f.X - 1 && c.Y >= f.Y - 1 && c.X + c.W <= f.X + f.W + 1 && c.Y + c.H <= f.Y + f.H + 1) { inside = true; break; }
                    if (!inside) { outside++; ids.Add(c.Id); }
                }
                outsideTot += outside; if (outside > 0) out0.Add(d.Key + " : " + string.Join(" ", ids.ToArray()));
                if (d.Key == "ftdx10" || d.Key == "ft2000") Check(outside == 0, d.Key + " : éléments hors de leur zone (cadre) : " + string.Join(" ", ids.ToArray()));
                // grand S-mètre : présent par défaut sur le FTdx10 et le FT-2000 seulement (les autres postes l'ajoutent par la vue config) ; recouvre au moins un vumètre à barres ; toutes les fonctions de METER ont leur image
                Ctl bm = d.Find("bigmeter"); bool want = d.Key == "ftdx10" || d.Key == "ft2000"; if (want || bm != null) { big += bm != null ? 1 : 0; Check(want == (bm != null), d.Key + " : grand S-mètre " + (bm != null ? "présent" : "absent") + " (attendu : " + (want ? "oui" : "non") + ")"); }
                if (bm != null)
                {
                    int under = 0; foreach (Ctl m in d.C) if (m.K == Kind.Meter && m.X + m.W / 2 >= bm.X && m.X + m.W / 2 <= bm.X + bm.W && m.Y + m.H / 2 >= bm.Y && m.Y + m.H / 2 <= bm.Y + bm.H) under++;
                    Check(under >= 3, d.Key + " : le grand S-mètre recouvre " + under + " vumètre(s) à barres (au moins 3 attendus)");
                    Ctl mc = d.Find("meter"); if (mc != null) foreach (Item it in mc.ItemList) Check(HBigMeter.HasImage(it.Label), d.Key + " : METER « " + it.Label + " » n'a pas d'image de S-mètre");
                }
                // touche MENU : propre au FTdx10, ou menu EX du poste ; jamais deux
                Ctl mn = d.Find("menu"); if (mn != null) menus++; Check(mn == null || mn.Local, d.Key + " : touche MENU non locale");
                // chaque vumètre à barres a sa commande de lecture, chaque Choice sa commande METER cohérente
                foreach (Ctl c in d.C) if (c.K == Kind.Meter && c.Id != "bigmeter") Check(c.Read.Length > 0 && c.Parse.Length > 0 && c.Max > 0, d.Key + " : vumètre " + c.Id + " sans lecture ou sans maximum");
                // les touches à bascule ont une valeur « marche » et une valeur « arrêt » différentes et leur gabarit contient {v}
                foreach (Ctl c in d.C) if (c.K == Kind.Toggle && !c.Local && c.Set.Length > 0) Check(c.OnSend != c.OffSend, d.Key + " : " + c.Id + " marche = arrêt");
            }
            Info("Cohérence des " + radios + " postes : " + (radios - out0.Count) + " sans élément hors de sa zone, " + big + " avec grand S-mètre, " + menus + " avec touche MENU");
            foreach (string s in out0) Info("  (info) hors zone : " + s);
        }


        public static int Run(string report)
        { Rig.NoGate = true;
            sb = new StringBuilder(); ok = ko = 0;
            string tmp = Path.Combine(Path.GetTempPath(), "HamRadioLink-selftest-" + Guid.NewGuid().ToString("N").Substring(0, 8)); Directory.CreateDirectory(tmp); Cfg.Dir = tmp;
            try
            {
                if (Environment.GetEnvironmentVariable("HRL_ONLY") == "share") { ShareTest(); ShareCfgTest(); throw new OperationCanceledException(); }   // essai rapide du partage seul
                YLegLink.NoDelay = true; CatLog.On = false;
                if (Environment.GetEnvironmentVariable("HRL_ONLY") == "ptt") { PttTimeoutTest(); throw new OperationCanceledException(); }
                if (Environment.GetEnvironmentVariable("HRL_ONLY") == "journal") { Journal.JournalTest.Run(Check, Info, tmp); throw new OperationCanceledException(); }   // journal (MariaDB intégré isolé)
                if (Environment.GetEnvironmentVariable("HRL_ONLY") == "menu") { MenuBackup(); throw new OperationCanceledException(); }
                if (Environment.GetEnvironmentVariable("HRL_ONLY") == "log") { CatLogTest(); throw new OperationCanceledException(); }
                if (Environment.GetEnvironmentVariable("HRL_ONLY") == "srv") { ServerTest(); throw new OperationCanceledException(); }
                if (Environment.GetEnvironmentVariable("HRL_ONLY") == "pttall") { foreach (RadioDef d in Radios.All()) { Ctl p = d.Find("ptt"); Check(p != null && p.K == Kind.Toggle, "touche PTT (menu clic droit : micro vers le poste) absente : " + d.Key + (d.Leg != null ? " [ancien]" : "")); } throw new OperationCanceledException(); }
                if (Environment.GetEnvironmentVariable("HRL_ONLY") == "lock") { foreach (RadioDef d in Radios.All()) LockChecks(d); throw new OperationCanceledException(); }
                if (Environment.GetEnvironmentVariable("HRL_ONLY") == "dead") { DeadReads(); throw new OperationCanceledException(); }
                if (Environment.GetEnvironmentVariable("HRL_ONLY") == "maj") { UpdateChecks(); RosRestoreChecks(); ScaleChecks(); throw new OperationCanceledException(); }
                if (Environment.GetEnvironmentVariable("HRL_ONLY") == "pickdead") { RadioDef q = Radios.ByKey("ftdx10"); PickChecks(q); DeadReads(); throw new OperationCanceledException(); }
                if (Environment.GetEnvironmentVariable("HRL_ONLY") == "pick") { foreach (RadioDef d in Radios.All()) PickChecks(d); throw new OperationCanceledException(); }
                if (Environment.GetEnvironmentVariable("HRL_ONLY") == "tci") { RadioDef t = Radios.ByKey("tci"); Definitions(t); Roundtrip(t); TciChecks(t); Persistence(t); throw new OperationCanceledException(); }
                if (Environment.GetEnvironmentVariable("HRL_ONLY") == "leg") { foreach (RadioDef d in Radios.All()) if (d.Leg != null) { foreach (string nt in d.Notes) sb.AppendLine("NOTE " + d.Key + " : " + nt); Definitions(d); Roundtrip(d); LegChecks(d); Persistence(d); LockChecks(d); } throw new OperationCanceledException(); }
                Templates(); CivCodec(); Y5Codec(); int nCiv = 0;
                foreach (RadioDef d in Radios.All()) { foreach (string nt in d.Notes) sb.AppendLine("NOTE " + d.Key + " : " + nt); Definitions(d); Roundtrip(d); if (d.Civ >= 0) CivChecks(d, ++nCiv); else if (d.Bin) Y5Checks(d); else if (d.Leg != null) LegChecks(d); else if (d.Tci) TciChecks(d); else { StateMapping(d); EndToEnd(d); } Persistence(d); LockChecks(d); }
                CustomTest(); Strictness();
                Editors();
                DeadReads(); MenuBackup(); UpdateChecks(); AlertChecks(); RosRestoreChecks(); ScaleChecks(); foreach (RadioDef d in Radios.All()) { PickChecks(d); RosChecks(d); }
                ShareTest(); ShareCfgTest(); ServerTest(); Com0ComTest(); CatLogTest(); PttTimeoutTest();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { ko++; sb.AppendLine("EXCEPTION : " + ex); }
            sb.AppendLine(); sb.AppendLine(ko == 0 ? "TOUS LES TESTS PASSENT (" + ok + " vérifications)" : ko + " ÉCHEC(S) sur " + (ok + ko) + " vérifications");
            File.WriteAllText(report, sb.ToString(), Encoding.UTF8);
            try { Directory.Delete(tmp, true); } catch { }
            return ko == 0 ? 0 : 1;
        }

        // Liste de toutes les commandes (HamRadioLink.exe --dump=fichier.txt)
        public static void Dump(string path)
        {
            StringBuilder o = new StringBuilder(); o.AppendLine("HamRadioLink - commandes CAT par défaut de chaque élément (celles que le clic droit en vue config permet de modifier)"); o.AppendLine();
            foreach (RadioDef d in Radios.All())
            {
                o.AppendLine("==================== " + d.Name + "  (ID attendu " + d.IdAnswer + ", " + d.FreqDigits + " chiffres de fréquence, vitesse conseillée " + d.DefBaud + ")");
                foreach (Ctl c in d.C)
                {
                    if (c.K == Kind.Frame) continue;
                    o.AppendLine(c.Id.PadRight(9) + " " + c.K.ToString().PadRight(7) + " " + c.Label);
                    if (c.Set.Length > 0) o.AppendLine("            envoi   : " + c.Set + (c.K == Kind.Knob ? "     [" + c.Min + " à " + c.Max + (c.Step > 1 ? ", pas " + c.Step : "") + "]" : ""));
                    if (c.Read.Length > 0) o.AppendLine("            lecture : " + c.Read + "   réponse : " + c.Parse);
                    if (c.K == Kind.Toggle) o.AppendLine("            allumé  : " + c.OnVal + "   éteint : " + c.OffVal);
                    if (c.K == Kind.Choice && !c.Local) o.AppendLine("            valeurs : " + c.Items);
                    if (c.Hint.Length > 0) o.AppendLine("            note    : " + c.Hint);
                }
                o.AppendLine();
            }
            File.WriteAllText(path, o.ToString(), Encoding.UTF8);
        }

        // ---- gabarits {v}, {v:N}, {v:+N}, {p:..}, {n:..}, {wait}
        static void Templates()
        {
            Check(Tpl.Fill("FA{v:9};", "14250000") == "FA014250000;", "{v:9}");
            Check(Tpl.Fill("FA{v:8};", "14250000") == "FA14250000;", "{v:8}");
            Check(Tpl.Fill("IS00{v:+4};", "-120") == "IS00-0120;", "{v:+4} négatif");
            Check(Tpl.Fill("IS00{v:+4};", "20") == "IS00+0020;", "{v:+4} positif");
            Check(Tpl.Fill("IS00{v:+4};", "0") == "IS00+0000;", "{v:+4} zéro");
            Check(Tpl.Fill("RC;{p:RU:4}{n:RD:4}", "150") == "RC;RU0150", "{p}{n} positif");
            Check(Tpl.Fill("RC;{p:RU:4}{n:RD:4}", "-150") == "RC;RD0150", "{p}{n} négatif");
            Check(Tpl.Fill("MD0{v};", "A") == "MD0A;", "{v} lettre");
            Check(Tpl.Fill("AB;", null) == "AB;", "sans jeton");
            List<string> st = Tpl.Steps("PS1;{wait:1100}PS1;"); Check(st.Count == 3 && st[1] == "\0" + "1100", "pauses {wait}");
            Check(Tpl.Check("FA{v:9};") == null, "contrôle gabarit valide"); Check(Tpl.Check("FA{x};") != null, "contrôle gabarit invalide"); Check(Tpl.Check("FA{v;") != null, "accolade ouverte");
            Info("Gabarits : vérifiés");
        }

        // ---- cohérence interne de la définition
        static void Definitions(RadioDef d)
        {
            HashSet<string> ids = new HashSet<string>(); int nCat = 0;
            foreach (Ctl c in d.C)
            {
                Check(ids.Add(c.Id), d.Key + " : identifiant en double " + c.Id);
                Check(c.X >= 0 && c.Y >= 118 && c.X + c.W <= 1860 && c.Y + c.H <= (d.Key == "ft2000" ? 985 : 980), d.Key + " : " + c.Id + " hors de la fenêtre (" + c.X + "," + c.Y + "," + c.W + "," + c.H + ")");
                if (c.Set.Length > 0) Check(Tpl.Check(c.Set) == null, d.Key + " : gabarit invalide " + c.Id);
                if (c.Parse.Length > 0) { try { new Regex(c.Parse); } catch { Check(false, d.Key + " : regex invalide " + c.Id); } Check(c.Read.Length > 0 || (d.Catalog == "ts890" && c.Id == "ptt") || c.Id == "clartxk", d.Key + " : " + c.Id + " a une réponse mais pas de lecture"); }
                if (c.Read.Length > 0) { Check(c.Read.EndsWith(";"), d.Key + " : " + c.Id + " lecture sans ';'"); Check(c.Parse.Length > 0, d.Key + " : " + c.Id + " lecture sans format de réponse"); }
                if (c.K == Kind.Choice) Check(c.ItemList.Length > 0, d.Key + " : " + c.Id + " sans valeurs");
                if (c.K == Kind.Knob) Check(c.Min < c.Max, d.Key + " : " + c.Id + " min>=max");
                if (c.HasCat && (c.Set.Length > 0 || c.Read.Length > 0)) nCat++;
                if (c.K == Kind.Led && c.Ref.Length > 0) Check(d.Find(c.Ref) != null || c.Ref == "vm" || c.Ref == "busyb" || c.Ref == "rxant" || c.Ref == "rxantb", d.Key + " : voyant " + c.Id + " source inconnue " + c.Ref);
                if (c.K == Kind.Dial) { Check(d.Find(c.Ref) != null, d.Key + " : molette sans fréquence"); Check(d.Find(c.Items) != null, d.Key + " : molette sans pas"); }
                if (c.K == Kind.Freq) Check(d.Find(c.Ref) != null, d.Key + " : fréquence sans mode " + c.Ref);
            }
            // zones qui se chevauchent (hors cadres, cadre = fond)
            List<Ctl> top = d.C.FindAll(delegate(Ctl c) { return c.K != Kind.Frame; });
            for (int i = 0; i < top.Count; i++) for (int j = i + 1; j < top.Count; j++)
                {
                    Ctl a = top[i], b = top[j]; bool ov = a.X < b.X + b.W && b.X < a.X + a.W && a.Y < b.Y + b.H && b.Y < a.Y + a.H;
                    Check(!ov || a.Id == "bigmeter" || b.Id == "bigmeter" || (a.Id == "sm1" && a.Hidden) || (b.Id == "sm1" && b.Hidden) || ((a.Id == "ft8" || b.Id == "ft8") && (a.Id == "md0" || b.Id == "md0")) || (d.Key == "ft2000" && Ft2000UserOverlap(a.Id, b.Id)) || (d.Key == "ft991a-df5dp" && Df5dpOverlap(a.Id, b.Id)), d.Key + " : " + a.Id + " et " + b.Id + " se chevauchent");
                }
            Info(d.Name + " : " + d.C.Count + " éléments, " + nCat + " avec commande CAT");
        }

        // ---- chaque commande contre le poste simulé strict : envoi sans « ?; », relecture identique
        static void Roundtrip(RadioDef d)
        {
            VirtualLink vl = new VirtualLink(d); YaesuSim sim = vl.Sim; int tested = 0;
            foreach (Ctl c in d.C)
            {
                if (!c.HasCat || c.K == Kind.Frame) continue;
                List<string> vals = new List<string>();
                switch (c.K)
                {
                    case Kind.Toggle: vals.Add(c.OnSend); vals.Add(c.OffSend); break;
                    case Kind.Knob: { int st = Math.Max(1, c.Step); int mid = (int)Math.Round(((c.Min + c.Max) / 2) / (double)st) * st; vals.Add(c.Min.ToString()); vals.Add(mid.ToString()); vals.Add(c.Max.ToString()); if (c.Min < 0) vals.Add("0"); } break;
                    case Kind.Choice: foreach (Item it in c.ItemList) vals.Add(it.Code); break;
                    case Kind.Freq: vals.Add("14250000"); vals.Add("7074000"); vals.Add("1840000"); break;
                    case Kind.Action: vals.Add(null); break;
                }
                foreach (string v in vals)
                {
                    if (c.Set.Length == 0) break;
                    if (d.Key == "ftdx10" && c.Id == "band" && v == "13") continue;   // touche 70 : le programme envoie FA070000000; (voir Rig.SetRadio), pas de code BS
                    string cmd = Tpl.Fill(c.Set, v); sim.Bad.Clear(); string r = SendSteps(sim, cmd); if (d.Civ >= 0) r = r.Replace("FB;", ""); tested++;
                    Check(r == "" && sim.Bad.Count == 0, d.Key + " : « " + cmd + " » (" + c.Id + ") refusé par le poste : " + r);
                    if (c.Read.Length > 0 && c.Parse.Length > 0 && c.K != Kind.Action)
                    {
                        string a = sim.Input(c.Read); Match m = c.Rx != null ? c.Rx.Match(a) : null;
                        Check(m != null && m.Success, d.Key + " : réponse « " + a + " » à " + c.Read + " (" + c.Id + ") ne correspond pas à " + c.Parse);
                        if (m != null && m.Success && m.Groups["v"].Success)
                        {
                            string got = m.Groups["v"].Value; bool good;
                            switch (c.K)
                            {
                                case Kind.Toggle: good = c.IsOn(got) == (v == c.OnSend); break;
                                case Kind.Choice: good = FindItem(c, v).Match(got); break;
                                case Kind.Knob: good = long.Parse(got) == long.Parse(v); break;
                                case Kind.Freq: good = long.Parse(got) == long.Parse(v); break;
                                default: good = true; break;
                            }
                            Check(good, d.Key + " : relecture de " + c.Id + " : envoyé " + v + ", relu " + got + " (" + a + ")");
                        }
                    }
                }
            }
            // lectures seules (mètres, voyants...)
            foreach (Ctl c in d.C)
            {
                if (c.Read.Length == 0 || c.Parse.Length == 0) continue; string a = sim.Input(c.Read);
                Check(c.Rx.IsMatch(a), d.Key + " : lecture " + c.Read + " (" + c.Id + ") : réponse « " + a + " » non reconnue");
            }
            Check(sim.Bad.Count == 0, d.Key + " : commandes refusées : " + string.Join(" ", sim.Bad.ToArray()));
            Info(d.Name + " : " + tested + " envois vérifiés contre le simulateur strict");
        }
        static string SendSteps(YaesuSim sim, string cmd) { StringBuilder o = new StringBuilder(); foreach (string s in Tpl.Steps(cmd)) if (s.Length > 0 && s[0] != (char)0) o.Append(sim.Input(s)); return o.ToString(); }
        static Item FindItem(Ctl c, string code) { foreach (Item i in c.ItemList) if (i.Code == code) return i; return c.ItemList[0]; }

        // ---- la réponse de chaque lecture est bien rangée dans l'état (y compris IF : fréquence, mode, CLAR)
        static void StateMapping(RadioDef d)
        {
            Rig rig = new Rig(); rig.Def = d; YaesuSim sim = new YaesuSim(d);
            sim.Input(Tpl.Fill(d.Find("fa").Set, "14250000")); sim.Input(Tpl.Fill(d.Find("md0").Set, "3")); sim.Input("RT1;"); sim.Input("XT1;");
            bool cat = d.Catalog != null; bool ifRead = d.C.Exists(delegate(Ctl q) { return q.Read == "IF;"; });
            if (!cat) sim.Input(d.Key != "ftdx10" ? "RC;RU0150;" : "CF001+0150;");
            rig.Feed(sim.Input("IF;"));
            if (ifRead) { bool ifMode = d.C.Exists(delegate(Ctl q) { return q.Parse.Contains("?<md0>"); });
            Check(rig.GetL("fa") == 14250000, d.Key + " : IF → fréquence A (" + rig.Get("fa") + ")");

            if (ifMode) Check(rig.Get("md0") == "3", d.Key + " : IF → mode A (" + rig.Get("md0") + ")");
            if (!cat) Check(rig.Get("clarrx") == "1" && rig.Get("clartx") == "1", d.Key + " : IF → CLAR RX/TX");
            if (!cat) Check(rig.GetL("clar") == 150, d.Key + " : IF → décalage CLAR (" + rig.Get("clar") + ")"); }
            bool kw = (d.Catalog ?? "").StartsWith("ts"); if (!kw) {
            rig.Feed("SM0123;"); Check(rig.Get("sm") == "123", d.Key + " : S-mètre");
            rig.Feed("GT05;"); Check(rig.Get("agc") == "4", d.Key + " : AGC AUTO-MID (5) reconnu comme AUTO (" + rig.Get("agc") + ")");
            rig.Feed("TX1;"); Check(rig.Tx, d.Key + " : TX1 → émission");
            rig.Feed("TX0;"); Check(!rig.Tx, d.Key + " : TX0 → réception"); }
            rig.Feed("FA0142"); rig.Feed("50000;"); // réponse coupée en deux morceaux
            Info(d.Name + " : correspondance réponses → état vérifiée");
        }

        // ---- chaîne complète : moteur + liaison + poste simulé (thread de lecture, retenue, relecture)
        static void EndToEnd(RadioDef d)
        {
            Rig rig = new Rig(); VirtualLink vl = new VirtualLink(d); string err; string agk = d.Catalog == "ts890" ? "ag" : "ag0";
            Check(rig.Connect(d, vl, out err), d.Key + " : connexion virtuelle");
            Wait(delegate { return rig.Connected && rig.Get("fa") != null && rig.Get("af") != null && !rig.Loading; }, 20000);   // les commandes sont refusées tant que tous les paramètres du poste ne sont pas lus
            Check(rig.Connected, d.Key + " : connecté"); Check(d.IdValue.Length == 0 || rig.SeenId == d.IdValue, d.Key + " : identifiant ID lu (" + rig.SeenId + ")");
            Check(rig.GetL("fa") == 14074000, d.Key + " : fréquence A lue (" + rig.Get("fa") + ")");
            Ctl af = d.Find("af"); rig.Set(af, "180"); Wait(delegate { return vl.Sim.S[agk] == "180"; }, 3000);
            Check(vl.Sim.S[agk] == "180", d.Key + " : volume envoyé au poste (" + vl.Sim.S[agk] + ")");
            // rafale de valeurs : seule la dernière doit arriver, sans erreur
            for (int i = 0; i < 60; i++) rig.Set(af, (100 + i).ToString()); Wait(delegate { return vl.Sim.S[agk] == "159"; }, 3000);
            Check(vl.Sim.S[agk] == "159", d.Key + " : rafale de potentiomètre (dernier = " + vl.Sim.S[agk] + ")");
            Ctl fa = d.Find("fa"); rig.Set(fa, "7100000"); Wait(delegate { return vl.Sim.S["fa"].TrimStart('0') == "7100000"; }, 3000); Check(vl.Sim.S["fa"].TrimStart('0') == "7100000", d.Key + " : fréquence envoyée");
            Ctl ptt = d.Find("ptt"); rig.Set(ptt, ptt.OnSend); Wait(delegate { return vl.Sim.Tx; }, 3000); Check(vl.Sim.Tx, d.Key + " : PTT envoyé");
            if (d.Find("po") != null) { Wait(delegate { return rig.Get("po") != null && rig.GetL("po") > 0; }, 4000); Check(rig.GetL("po") > 0, d.Key + " : mesure de puissance pendant l'émission (" + rig.Get("po") + ")"); }
            Check(rig.Tx, d.Key + " : état PTT relu"); rig.Set(ptt, ptt.OffSend); Wait(delegate { return !vl.Sim.Tx; }, 3000); Check(!vl.Sim.Tx, d.Key + " : fin d'émission");
            // le poste change tout seul (molette réelle) : l'état suit
            vl.Sim.Input(Tpl.Fill(d.Find("fa").Set, "21074000")); Wait(delegate { return rig.GetL("fa") == 21074000; }, 3000); Check(rig.GetL("fa") == 21074000, d.Key + " : suivi d'une modification faite sur le poste");
            // allumage avec pause
            Ctl on = d.Find("onoff"); if (on != null) { rig.Set(on, null); Thread.Sleep(1500); Check(vl.Sim.Seen.FindAll(delegate(string s) { return s == "PS1;"; }).Count >= (on.Set.Contains("wait") ? 2 : 1), d.Key + " : allumage PS1; deux fois avec pause"); }
            Check(rig.Errors == 0 && vl.Sim.Bad.Count == 0, d.Key + " : aucune erreur « ?; » pendant la session (" + rig.Errors + " / " + string.Join(" ", vl.Sim.Bad.ToArray()) + ")");
            rig.Disconnect();
            Info(d.Name + " : session complète (connexion, lecture, écriture, rafale, PTT, suivi) vérifiée");
        }
        static void Wait(Func<bool> f, int ms) { int t = Environment.TickCount; while (Environment.TickCount - t < ms) { try { if (f()) return; } catch { } Thread.Sleep(20); } }

        // ---- CI-V : codage des trames, inversion des octets BCD, simulateur strict
        static void CivCodec()
        {
            Check(Civ.Swap("050014074000") == "050040071400", "CI-V : fréquence 14074000 → octets BCD inversés (" + Civ.Swap("050014074000") + ")");
            Check(Civ.Swap(Civ.Swap("2501" + "0007074000")) == "2501" + "0007074000" && Civ.Swap("2501" + "0007074000") == "2501" + "0040070700", "CI-V : 25 01 + fréquence");
            Check(Civ.Swap("1402" + "0128") == "1402" + "0128" && Civ.Swap("06" + "0101") == "060101", "CI-V : niveaux et modes non inversés");
            string fr = Civ.Frame(0x94, 0xE0, "050040071400"); Check(fr.Length == 11 && fr[0] == 'þ' && fr[1] == 'þ' && fr[2] == '\u0094' && fr[3] == 'à' && fr[4] == '\u0005' && fr[5] == '\u0000' && fr[6] == '@' && fr[10] == 'ý', "CI-V : trame FE FE 94 E0 05 00 40 07 14 00 FD");
            StringBuilder buf = new StringBuilder(); List<string[]> l = Civ.Parse(buf, fr.Substring(0, 6)); Check(l.Count == 0, "CI-V : trame coupée en attente");
            l = Civ.Parse(buf, fr.Substring(6) + Civ.Frame(0xE0, 0x94, "FB")); Check(l.Count == 2 && l[0][0] == "94" && l[0][1] == "E0" && l[0][2] == "050040071400" && l[1][2] == "FB", "CI-V : trames reconstituées (" + l.Count + ")");
            l = Civ.Parse(new StringBuilder(), "þþþþà\u0094ûý"); Check(l.Count == 1 && l[0][2] == "FB", "CI-V : octets FE en tête (réveil) ignorés");
            RadioDef d = Radios.ByKey("ic7300"); List<string> bad = new List<string>(), seen = new List<string>(); CivSim s = new CivSim(d, bad, seen, new Dictionary<string, string>());
            Check(s.Input("1401" + "0128;") == "FB;" && s.Input("1401;") == "14010128;", "CI-V : niveau envoyé puis relu");
            Check(s.Input("1401" + "012;") == "?;" && s.Input("1401" + "0300;") == "?;" && s.Input("1401" + "00AB;") == "?;", "CI-V : longueur, borne 0255 et BCD refusés");
            Check(s.Input("FF;") == "?;" && s.Input("1601" + "00;") == "?;" && s.Input("0300;") == "?;", "CI-V : commande inconnue / lecture avec données refusées");
            Check(s.Input("05" + "0014250000;") == "FB;" && s.Input("03;") == "030014250000;" && s.Input("05" + "14250000;") == "?;", "CI-V : fréquence sur 10 chiffres BCD");
            Check(s.Input("07B0;") == "FB;" && s.Input("03;") == "030007074000;" && s.Input("2501;") == "25010014250000;", "CI-V : échange des VFO");
            Info("CI-V : codage des trames et simulateur strict vérifiés");
        }

        // ---- un poste CI-V : état, puis session complète à travers CivLink (trames binaires, écho, réponses coupées) contre le poste simulé sur fil
        static void CivChecks(RadioDef d, int n)
        {
            Rig rig = new Rig(); rig.Def = d; int D = d.FreqDigits; string z = new string('0', D);
            bool ms = d.Key == "ic7610" || d.Key == "ic7760" || d.Key == "ic7850" || d.Key == "ic7851" || d.Key == "ic7600" || d.Key == "ic7800"; string pf = ms ? "2500" : "03";   // IC-7610 / 785x : la fréquence principale se lit par 25 00 (03 suit la bande sélectionnée)
            rig.Feed(pf + (14074000L).ToString().PadLeft(D, '0') + ";"); Check(rig.GetL("fa") == 14074000, d.Key + " : réponse " + pf + " → fréquence (" + rig.Get("fa") + ")");
            if (!ms) { rig.Feed("00" + (7074000L).ToString().PadLeft(D, '0') + ";"); Check(rig.GetL("fa") == 7074000, d.Key + " : trame transceive 00 → fréquence"); }
            rig.Feed("0401" + "01;"); Check(rig.Get("md0") == "01", d.Key + " : réponse 04 → mode USB (" + rig.Get("md0") + ")");
            rig.Feed(pf + (ms ? "" : "00") + "1407"); rig.Feed("4000;"); Check(rig.GetL("fa") == 14074000 || D == 8, d.Key + " : réponse coupée en deux");
            if (d.Find("sm") != null) { rig.Feed("15020120;"); Check(rig.GetL("sm") == 120, d.Key + " : S-mètre"); }
            if (d.Find("ptt") != null) { rig.Feed("1C0001;"); Check(rig.Tx, d.Key + " : 1C 00 01 → émission"); rig.Feed("1C0000;"); Check(!rig.Tx, d.Key + " : 1C 00 00 → réception"); }
            rig.Feed("FB;"); rig.Feed("?;"); Check(rig.Errors == 1, d.Key + " : FB accepté, ?; compté");

            List<string> bad = new List<string>(), seen = new List<string>(); Dictionary<string, string> S = new Dictionary<string, string>(); CivSim sim = new CivSim(d, bad, seen, S);
            CivWireLink wl = new CivWireLink(sim, d.Civ, n % 2 == 0); CivLink cl = new CivLink(wl, d.Civ); Rig r2 = new Rig(); string err;
            Check(r2.Connect(d, cl, out err), d.Key + " : connexion CI-V (fil simulé)");
            Wait(delegate { return r2.Connected && r2.Get("fa") != null && r2.Get("md0") != null && (d.IdValue == null || r2.SeenId == d.IdValue); }, 9000);
            Check(r2.Connected, d.Key + " : connecté"); Check(r2.SeenId == d.IdValue, d.Key + " : identifiant CI-V lu (" + r2.SeenId + " / " + d.IdValue + ")");
            Check(r2.GetL("fa") == 14074000, d.Key + " : fréquence lue sur le fil (" + r2.Get("fa") + ")");
            r2.Set(d.Find("fa"), "7100000"); Wait(delegate { return S["fa"] == "7100000"; }, 4000); Check(S["fa"] == "7100000", d.Key + " : fréquence envoyée en BCD (" + S["fa"] + ")");
            r2.Set(d.Find("md0"), "03"); Wait(delegate { return S["md"] == "03"; }, 4000); Check(S["md"] == "03", d.Key + " : mode CW envoyé");
            Ctl af = d.Find("af"); if (af != null) { for (int i = 0; i < 40; i++) r2.Set(af, (100 + i).ToString()); Wait(delegate { return S.ContainsKey("c1401") && S["c1401"] == "0139"; }, 4000); Check(S.ContainsKey("c1401") && S["c1401"] == "0139", d.Key + " : rafale de potentiomètre (dernier = " + (S.ContainsKey("c1401") ? S["c1401"] : "?") + ")"); }
            Ctl ptt = d.Find("ptt"); if (ptt != null) { r2.Set(ptt, ptt.OnSend); Wait(delegate { return sim.Tx; }, 4000); Check(sim.Tx, d.Key + " : PTT envoyé"); r2.Set(ptt, ptt.OffSend); Wait(delegate { return !sim.Tx; }, 4000); Check(!sim.Tx, d.Key + " : fin d'émission"); }
            sim.Input("05" + (21074000L).ToString().PadLeft(D, '0') + ";"); Wait(delegate { return r2.GetL("fa") == 21074000; }, 5000); Check(r2.GetL("fa") == 21074000, d.Key + " : suivi d'une modification faite sur le poste");
            Check(r2.Errors == 0 && bad.Count == 0, d.Key + " : aucune erreur pendant la session (" + r2.Errors + " / " + string.Join(" ", bad.ToArray()) + ")");
            r2.Disconnect(); Info(d.Name + " : session CI-V complète (trames, écho, lecture, écriture, rafale, PTT, suivi) vérifiée");
        }

        // ---- Yaesu 5 octets : codage des trames
        static void Y5Codec()
        {
            char rd; int ln; string f;
            f = Y5.Encode("FA014250000", out rd, out ln); Check(f == "\u0001BP\u0000\u0001" && rd == ' ', "Yaesu 5 octets : FA014250000 → 01 42 50 00 01");
            f = Y5.Encode("FA145000000", out rd, out ln); Check(f == "\u0014P\u0000\u0000\u0001", "Yaesu 5 octets : 145 MHz → 14 50 00 00 01");
            f = Y5.Encode("MD0A", out rd, out ln); Check(f == "\u000A\u0000\u0000\u0000\u0007", "Yaesu 5 octets : MD0A → 0A 00 00 00 07");
            f = Y5.Encode("TX1", out rd, out ln); Check(f == "\0\0\0\0\u0008" && rd == ' ', "Yaesu 5 octets : TX1 → 08");
            f = Y5.Encode("TX0", out rd, out ln); Check(f == "\0\0\0\0\u0088", "Yaesu 5 octets : TX0 → 88");
            f = Y5.Encode("FA", out rd, out ln); Check(f == "\0\0\0\0\u0003" && rd == 'F' && ln == 5, "Yaesu 5 octets : lecture fréquence = 03, réponse de 5 octets");
            f = Y5.Encode("SM", out rd, out ln); Check(f == "\0\0\0\0ç" && rd == 'S' && ln == 1, "Yaesu 5 octets : lecture S-mètre = E7, réponse de 1 octet");
            f = Y5.Encode("PO", out rd, out ln); Check(f == "\0\0\0\0÷" && rd == 'P' && ln == 1, "Yaesu 5 octets : lecture puissance = F7");
            Check(Y5.Encode("XX", out rd, out ln) == null, "Yaesu 5 octets : commande inconnue refusée");
            Check(Y5.Decode('F', "\u0001BP\u0000\u0001") == "FA014250000;", "Yaesu 5 octets : réponse 03 → fréquence en Hz");
            Check(Y5.Decode('M', "\u0001BP\u0000\u0004") == "MD04;", "Yaesu 5 octets : réponse 03 → mode");
            Check(Y5.Decode('S', "\u0009") == "SM120;" && Y5.Decode('S', "\u000F") == "SM241;" && Y5.Decode('S', "\u0000") == "SM0;", "Yaesu 5 octets : S-mètre 9 → S9 (120), 15 → 241");
            Check(Y5.Decode('T', "\u0007") == "TX1;" && Y5.Decode('T', "\u0087") == "TX0;" && Y5.Decode('P', "\u0087") == "PO7;" && Y5.Decode('W', "G") == "SW1;", "Yaesu 5 octets : octet F7 (émission, puissance, SWR)");
            Check(Y5.Decode('B', "\u0000") == "BY1;" && Y5.Decode('B', "\u0080") == "BY0;", "Yaesu 5 octets : octet E7 (squelch)");
            Info("Yaesu 5 octets : codage des trames vérifié");
        }
        // ---- un Yaesu 5 octets : état, puis session complète à travers Y5Link contre le poste simulé sur fil
        static void Y5Checks(RadioDef d)
        {
            Rig rig = new Rig(); rig.Def = d;
            rig.Feed("FA014074000;"); Check(rig.GetL("fa") == 14074000, d.Key + " : réponse FA → fréquence (" + rig.Get("fa") + ")");
            rig.Feed("MD01;"); Check(rig.Get("md0") == "01", d.Key + " : réponse MD → mode USB");
            rig.Feed("SM120;"); Check(rig.GetL("sm") == 120, d.Key + " : S-mètre");
            rig.Feed("TX1;"); Check(rig.Tx, d.Key + " : TX1 → émission"); rig.Feed("TX0;"); Check(!rig.Tx, d.Key + " : TX0 → réception");
            rig.Feed("FA0140"); rig.Feed("74000;"); Check(rig.GetL("fa") == 14074000, d.Key + " : réponse coupée en deux");
            VirtualLink vl = new VirtualLink(d); YaesuSim sim = vl.Sim; Y5WireLink wl = new Y5WireLink(sim); Y5Link yl = new Y5Link(wl); Rig r2 = new Rig(); string err;
            Check(r2.Connect(d, yl, out err), d.Key + " : connexion (fil simulé 5 octets)");
            Wait(delegate { return r2.Connected && r2.Get("fa") != null && r2.Get("md0") != null && r2.Get("sm") != null; }, 9000);
            Check(r2.Connected, d.Key + " : connecté"); Check(r2.GetL("fa") == 14074000, d.Key + " : fréquence lue sur le fil (" + r2.Get("fa") + ")"); Check(r2.Get("md0") == "01", d.Key + " : mode lu sur le fil (" + r2.Get("md0") + ")");
            Check(r2.GetL("sm") == 120, d.Key + " : S-mètre lu sur le fil (" + r2.Get("sm") + ")");
            r2.Set(d.Find("fa"), "7100000"); Wait(delegate { return sim.Input("FA;") == "FA007100000;"; }, 4000); Check(sim.Input("FA;") == "FA007100000;", d.Key + " : fréquence envoyée en 5 octets BCD (" + sim.Input("FA;") + ")");
            r2.Set(d.Find("md0"), "02"); Wait(delegate { return sim.Input("MD;") == "MD02;"; }, 4000); Check(sim.Input("MD;") == "MD02;", d.Key + " : mode CW envoyé");
            Ctl ptt = d.Find("ptt"); r2.Set(ptt, ptt.OnSend); Wait(delegate { return sim.Tx; }, 4000); Check(sim.Tx, d.Key + " : PTT envoyé");
            Wait(delegate { return r2.Tx; }, 4000); Check(r2.Tx, d.Key + " : émission relue par F7");
            r2.Set(ptt, ptt.OffSend); Wait(delegate { return !sim.Tx; }, 4000); Check(!sim.Tx, d.Key + " : fin d'émission");
            sim.Input("FA021074000;"); Wait(delegate { return r2.GetL("fa") == 21074000; }, 5000); Check(r2.GetL("fa") == 21074000, d.Key + " : suivi d'une modification faite sur le poste");
            Check(r2.Errors == 0 && sim.Bad.Count == 0, d.Key + " : aucune erreur pendant la session (" + r2.Errors + " / " + string.Join(" ", sim.Bad.ToArray()) + ")");
            r2.Disconnect(); Info(d.Name + " : session 5 octets complète (trames, lecture, écriture, PTT, suivi) vérifiée");
        }

        // ---- poste commandé en TCI : vraie liaison WebSocket contre un serveur TCI simulé strict (formes ExpertSDR3 puis anciennes)
        static string TS(TciServer s, string k) { lock (s.Sim.S) return s.Sim.S[k]; }
        static void TciSet(TciServer s, string k, string v) { lock (s.Sim.S) s.Sim.S[k] = v; }
        static void TciChecks(RadioDef d)
        {
            string err; TciServer srv = new TciServer(2); TciLink lk = new TciLink("127.0.0.1", srv.Port); Rig rig = new Rig();
            Check(rig.Connect(d, lk, out err), "TCI : connexion WebSocket (" + err + ")");
            Wait(delegate { return rig.Get("fa") != null && rig.Get("fb") != null && rig.Get("md0") != null && rig.Get("sm") != null && rig.Get("drive") != null && rig.Get("vol") != null; }, 12000);
            Check(rig.Connected, "TCI : connecté"); Check(rig.GetL("fa") == 14074000 && rig.GetL("fb") == 7100000, "TCI : VFO A / B lus (" + rig.Get("fa") + " / " + rig.Get("fb") + ")");
            Check(rig.Get("md0") == "usb", "TCI : mode lu (" + rig.Get("md0") + ")"); Check(rig.GetL("sm") == Tci.Scale(-73), "TCI : S-mètre dBm → échelle (" + rig.Get("sm") + ")");
            Check(rig.GetL("drive") == 50 && rig.GetL("vol") == -20, "TCI : drive et volume lus (" + rig.Get("drive") + " / " + rig.Get("vol") + ")");
            rig.Set(d.Find("fa"), "7074000"); Wait(delegate { return TS(srv, "fa") == "7074000"; }, 4000); Check(TS(srv, "fa") == "7074000", "TCI : fréquence A envoyée");
            rig.Set(d.Find("fb"), "3573000"); Wait(delegate { return TS(srv, "fb") == "3573000"; }, 4000); Check(TS(srv, "fb") == "3573000", "TCI : fréquence B envoyée");
            rig.Set(d.Find("md0"), "cw"); Wait(delegate { return TS(srv, "md") == "cw"; }, 4000); Check(TS(srv, "md") == "cw", "TCI : mode CW envoyé");
            Ctl ptt = d.Find("ptt"); rig.Set(ptt, ptt.OnSend); Wait(delegate { return TS(srv, "tx") == "1"; }, 4000); Check(TS(srv, "tx") == "1", "TCI : PTT envoyé (trx:0,true)");
            Wait(delegate { return rig.Tx; }, 4000); Check(rig.Tx, "TCI : émission relue"); rig.Set(ptt, ptt.OffSend); Wait(delegate { return TS(srv, "tx") == "0"; }, 4000); Check(TS(srv, "tx") == "0", "TCI : fin d'émission");
            for (int i = 0; i < 20; i++) rig.Set(d.Find("drive"), (60 + i).ToString()); Wait(delegate { return TS(srv, "drive") == "79"; }, 5000); Check(TS(srv, "drive") == "79", "TCI : rafale de DRIVE, dernier = " + TS(srv, "drive") + " (forme drive:0,P;)");
            rig.Set(d.Find("vol"), "-30"); Wait(delegate { return TS(srv, "vol") == "-30"; }, 4000); Check(TS(srv, "vol") == "-30", "TCI : volume envoyé");
            rig.Set(d.Find("split"), "true"); Wait(delegate { return TS(srv, "split") == "true"; }, 4000); Check(TS(srv, "split") == "true", "TCI : split envoyé");
            rig.Set(d.Find("ritoff"), "250"); Wait(delegate { return TS(srv, "ritoff") == "250"; }, 4000); Check(TS(srv, "ritoff") == "250", "TCI : décalage RIT envoyé");
            TciSet(srv, "fa", "21074000"); srv.Push("vfo:0,0,21074000;"); Wait(delegate { return rig.GetL("fa") == 21074000; }, 5000); Check(rig.GetL("fa") == 21074000, "TCI : suivi d'un changement fait dans le logiciel SDR (" + rig.Get("fa") + ")");
            TciSet(srv, "sm", "-50"); Wait(delegate { return rig.GetL("sm") == Tci.Scale(-50); }, 6000); Check(rig.GetL("sm") == Tci.Scale(-50), "TCI : nouveau S-mètre (" + rig.Get("sm") + ")");
            Check(rig.Errors == 0 && srv.Sim.Bad.Count == 0, "TCI : aucune erreur (" + rig.Errors + " / " + string.Join(" ", srv.Sim.Bad.ToArray()) + ")");
            rig.Disconnect(); srv.Dispose();
            // anciens serveurs : drive:P;
            srv = new TciServer(1); lk = new TciLink("127.0.0.1", srv.Port); rig = new Rig(); Check(rig.Connect(d, lk, out err), "TCI (ancien serveur) : connexion");
            Wait(delegate { return rig.Get("drive") != null; }, 9000); rig.Set(d.Find("drive"), "33"); Wait(delegate { return TS(srv, "drive") == "33"; }, 4000);
            Check(TS(srv, "drive") == "33" && srv.Sim.Bad.Count == 0, "TCI (ancien serveur) : drive:P; sans zéro (" + string.Join(" ", srv.Sim.Bad.ToArray()) + ")");
            rig.Disconnect(); srv.Dispose();
            // poste TCI virtuel du programme
            TciVirtualLink vl = new TciVirtualLink(); rig = new Rig(); Check(rig.Connect(d, vl, out err), "TCI virtuel : connexion (" + err + ")"); Wait(delegate { return rig.GetL("fa") == 14074000; }, 9000); Check(rig.GetL("fa") == 14074000, "TCI virtuel : fréquence lue"); rig.Disconnect();
            Info(d.Name + " : session TCI (WebSocket réel, formes ExpertSDR3 et anciennes, suivi des changements, poste virtuel) vérifiée");
        }

        // ---- anciens Yaesu à bloc d'état : codage de la fréquence, puis session complète contre le poste simulé sur le fil
        static void LegChecks(RadioDef d)
        {
            LM m = d.Leg; bool rd = m.Kind == 0 || m.Kind == 3; string err;
            if (rd) foreach (long hz in new long[] { 1840000, 7100000, 14074000, 28074000 }) { char[] blk = new char[m.Len]; string f = LegCodec.PutFreq(m, hz); for (int i = 0; i < f.Length; i++) blk[m.FOff + i] = f[i]; Check(LegCodec.GetFreq(m, new string(blk)) == hz, d.Key + " : fréquence " + hz + " écrite puis relue dans le bloc"); }
            Check(LegCodec.FromFreqCmd(m, LegCodec.FreqCmd(m, 14074000)) == 14074000, d.Key + " : commande de fréquence relue (14,074 MHz)");
            LegWireLink wl = new LegWireLink(m); YLegLink yl = new YLegLink(wl, m); Rig r2 = new Rig();
            Check(r2.Connect(d, yl, out err), d.Key + " : connexion (fil simulé)");
            if (rd) { Wait(delegate { return r2.Get("fa") != null && r2.Get("md0") != null; }, 9000); Check(r2.GetL("fa") == 14074000, d.Key + " : fréquence lue dans le bloc (" + r2.Get("fa") + ")"); Check(r2.Get("md0") == "01", d.Key + " : mode lu dans le bloc (" + r2.Get("md0") + ")"); }
            r2.Set(d.Find("fa"), "7100000"); Wait(delegate { return wl.Hz == 7100000; }, 4000); Check(wl.Hz == 7100000, d.Key + " : fréquence envoyée (" + wl.Hz + ")");
            r2.Set(d.Find("md0"), "02"); Wait(delegate { return wl.Mode == "02"; }, 4000); Check(wl.Mode == "02", d.Key + " : mode CW envoyé (" + wl.Mode + ")");
            Ctl ptt = d.Find("ptt"); if (ptt != null) { r2.Set(ptt, ptt.OnSend); Wait(delegate { return wl.Tx; }, 4000); Check(wl.Tx, d.Key + " : PTT envoyé"); if (m.TxReadable) { Wait(delegate { return r2.Tx; }, 5000); Check(r2.Tx, d.Key + " : émission relue"); } r2.Set(ptt, ptt.OffSend); Wait(delegate { return !wl.Tx; }, 4000); Check(!wl.Tx, d.Key + " : fin d'émission"); }
            if (rd) { wl.Hz = 21074000; Wait(delegate { return r2.GetL("fa") == 21074000; }, 6000); Check(r2.GetL("fa") == 21074000, d.Key + " : suivi d'une modification faite sur le poste (" + r2.Get("fa") + ")"); }
            Check(r2.Errors == 0 && wl.Bad.Count == 0, d.Key + " : aucune erreur (" + r2.Errors + " / " + string.Join(" ", wl.Bad.ToArray()) + ")");
            r2.Disconnect(); Info(d.Name + " : session sur ancien protocole binaire (fréquence, mode" + (ptt != null ? ", PTT" : "") + ") vérifiée contre le poste simulé");
        }

        // ---- éléments ajoutés par l'utilisateur : enregistrement, rechargement, suppression
        static void CustomTest()
        {
            RadioDef d = Radios.ByKey("ftdx10"); d.LoadUser(); int n0 = d.C.Count;
            Ctl c = new Ctl(); c.K = Kind.Toggle; c.Custom = true; c.Id = d.NewId(); c.Label = "Essai\tTab"; c.X = 40; c.Y = 140; c.W = 110; c.H = 40; c.Set = "NB0{v};"; c.Read = "NB0;"; c.Parse = @"^NB0(?<v>[012]);$"; c.OnVal = "1|2"; c.OffVal = "0";
            d.AddCtl(c); d.SaveCustom();
            RadioDef e = Radios.ByKey("ftdx10"); e.LoadUser(); Ctl r = e.Find(c.Id);
            Check(r != null && r.Custom && r.K == Kind.Toggle && r.Label == "Essai\tTab" && r.Set == c.Set && r.Parse == c.Parse && r.OnVal == "1|2" && r.X == 40, "élément ajouté : rechargé identique");
            Check(e.C.Count == n0 + 1, "élément ajouté : un seul de plus (" + e.C.Count + " / " + n0 + ")");
            e.RemoveCustoms(); e.SaveCustom(); RadioDef f = Radios.ByKey("ftdx10"); f.LoadUser(); Check(f.C.Count == n0 && f.Find(c.Id) == null, "élément ajouté : supprimé, fichier effacé");
            Info("éléments ajoutés par l'utilisateur : enregistrement / suppression vérifiés");
        }
        // ---- personnalisation : modification, enregistrement, rechargement, Défaut
        static void Persistence(RadioDef d)
        {
            Ctl c = d.Find("af") ?? d.Find("fa"); string pid = c.Id; string set0 = c.Set; c.Set = "AG0{v:4};"; c.Read = "AG9;"; c.Min = 10; c.X += 40; c.Y += 8; c.Label = "VOL"; d.SaveCat(); d.SaveLayout();
            RadioDef d2 = Radios.ByKey(d.Key); d2.LoadUser(); Ctl c2 = d2.Find(pid);
            Check(c2.Set == "AG0{v:4};" && c2.Read == "AG9;" && c2.Min == 10 && c2.Label == "VOL", d.Key + " : commandes modifiées relues");
            Check(c2.X == c2.DX + 40 && c2.Y == c2.DY + 8, d.Key + " : position modifiée relue");
            Check(c2.Dirty, d.Key + " : élément marqué modifié");
            c2.ResetCat(); Check(c2.Set == set0 && !c2.Dirty && c2.Label == c2.DLabel, d.Key + " : bouton Défaut");
            d2.SaveCat(); d2.SaveLayout(); RadioDef d3 = Radios.ByKey(d.Key); d3.LoadUser(); Check(d3.Find(pid).Set == set0 && d3.Find(pid).X == d3.Find(pid).DX + 40, d.Key + " : Défaut commandes enregistré, position conservée");
            d3.Find(pid).ResetPos(); d3.SaveLayout(); d3.SaveCat(); RadioDef d4 = Radios.ByKey(d.Key); d4.LoadUser(); Check(d4.Find(pid).X == d4.Find(pid).DX, d.Key + " : disposition par défaut");
            Info(d.Name + " : personnalisation (commande, position, Défaut) vérifiée");
        }

        // ---- le simulateur refuse bien les formats faux (preuve que le test précédent est strict)
        static void Strictness()
        {
            YaesuSim f10 = new YaesuSim(Radios.ByKey("ftdx10")), f20 = new YaesuSim(Radios.ByKey("ft2000")), f50 = new YaesuSim(Radios.ByKey("ftdx5000"));
            Check(f10.Input("FA14250000;") == "?;", "FTdx10 refuse FA sur 8 chiffres"); Check(f10.Input("FA014250000;") == "", "FTdx10 accepte FA sur 9 chiffres");
            Check(f20.Input("FA014250000;") == "?;", "FT-2000 refuse FA sur 9 chiffres"); Check(f20.Input("FA14250000;") == "", "FT-2000 accepte FA sur 8 chiffres");
            Check(f10.Input("IS00+100;") == "?;", "IS : trois chiffres refusé"); Check(f10.Input("IS00+0030;") == "?;", "IS : pas de 20 Hz imposé"); Check(f10.Input("AG0300;") == "?;", "AG hors bornes");
            Check(f10.Input("XX;") == "?;", "commande inconnue"); Check(f10.Input("MD0G;") == "?;", "mode inexistant");
            Check(f20.Input("ST1;") == "?;", "FT-2000 n'a pas ST (split par FT)"); Check(f10.Input("ST1;") == "", "FTdx10 a ST");
            Check(f20.Input("CF001+0100;") == "?;", "FT-2000 n'a pas CF");
            Check(f50.Input("PA03;") == "", "FTdx5000 accepte PA03 (IPO 2)"); Check(f20.Input("PA03;") == "?;", "FT-2000 refuse PA03");
            Check(f50.Input("RF04;") == "" && f50.Input("RF0;") == "RF07;", "FTdx5000 : roofing 600 Hz envoyé 4, relu 7"); Check(f50.Input("RF06;") == "?;", "FTdx5000 : RF06 refusé à l'envoi");
            Check(f50.Input("FT3;") == "" && f50.Input("FT;") == "FT1;" && f50.Input("FT1;") == "?;", "FTdx5000 : FT2/FT3 (FT1 refusé)");
            Check(f50.Input("LK1;") == "" && f50.Input("LK;") == "LK5;" && f50.Input("LK3;") == "" && f50.Input("LK;") == "LK7;", "FTdx5000 : LK réponses 4 à 7");
            Check(f50.Input("AN05;") == "" && f50.Input("AN0;") == "AN011;" && f50.Input("AN02;") == "" && f50.Input("AN0;") == "AN020;", "FTdx5000 : antenne RX");
            Check(f50.Input("VD0010;") == "?;" && f50.Input("VD0030;") == "?;" && f50.Input("VD0040;") == "", "FTdx5000 : VOX delay 20 à 5000 par 20 ms");
            Check(f50.Input("ID;") == "ID0362;", "FTdx5000 : ID0362"); Check(f50.Input("BS02;") == "?;", "FTdx5000 : pas de bande 5 MHz");
            Check(f50.Input("EX085" + "1;") == "?;" && f50.Input("EX1041;") == "", "FTdx5000 : menu 104 (et non 085)");
            Info("Stricteness du simulateur : vérifiée");
        }

        // ---- l'éditeur de commande s'ouvre pour chaque élément de chaque poste
        static void Editors()
        {
            int n = 0; foreach (RadioDef d in Radios.All()) foreach (Ctl c in d.C) { using (CmdEditor e = new CmdEditor(c, null)) { n++; } }
            Info("Éditeur de commande : ouvert pour " + n + " éléments sans erreur");
        }

        // ---- une lecture refusée par le poste (« ?; ») est abandonnée après 3 essais, sans bloquer le reste
        static void DeadReads()
        {
            RadioDef d = Radios.ByKey("ftdx10"); d.LoadUser(); d.Find("kr").Read = "ZZ;"; d.Find("kr").Period = 300; Rig rig = new Rig(); VirtualLink vl = new VirtualLink(d); string err; rig.Connect(d, vl, out err);
            Wait(delegate { return rig.ReadDisabled(d.Find("kr")); }, 8000);
            Check(rig.ReadDisabled(d.Find("kr")), "lecture refusée désactivée après 3 « ?; »"); Check(rig.Errors >= 3, "erreurs comptées (" + rig.Errors + ")");
            Wait(delegate { return rig.Get("af") != null; }, 8000); Check(rig.Get("af") != null, "les autres lectures continuent");
            string a = rig.Ask("FA;", 1500); Check(a != null && a.StartsWith("FA"), "Ask (test de lecture de l'éditeur) : " + a);
            rig.Disconnect(); d.Find("kr").Read = d.Find("kr").DRead; d.Find("kr").Period = 2200; Info("Lectures refusées : abandonnées proprement ; Ask vérifié");
        }

        // ---- export / import par parties : menu interne (EXnnn) relu sur le poste simulé, puis remis en attente puis renvoyé
        static void MenuBackup()
        {
            RadioDef d = Radios.ByKey("ft2000"); d.LoadUser(); Rig rig = new Rig(); VirtualLink vl = new VirtualLink(d); string err; rig.Connect(d, vl, out err);
            Wait(delegate { return rig.Get("af") != null; }, 8000);
            Backup.MenuFrom = 80; Backup.MenuTo = 100; string s = Backup.Dump(d, rig, false, false, true, null, null); Backup.MenuFrom = 1; Backup.MenuTo = 199;
            Check(Backup.HasMenu(d) && s.Contains("[[radiomenu:ft2000]]") && s.Contains("EX085") && s.Contains("EX094"), "export du menu interne du FT-2000 simulé (EX085 / EX094 relus)");
            Check(Backup.Parts(s).Count == 1 && Backup.Parts(s)[0] == "menu" && Backup.RadioOf(s) == "ft2000", "parties du fichier reconnues (menu seulement, poste ft2000)");
            Check(Backup.Import(s, false, false, false) == 0, "import avec aucune partie cochée : rien d'écrit");
            Check(Backup.Import(s, false, false, true) == 1, "import du menu seul : mis en attente"); int sent = Backup.ApplyPending(d, rig);
            Check(sent >= 2, "menu renvoyé au poste (" + sent + " commandes)"); Check(Cfg.Get("pendingmenu.ft2000", "").Length == 0, "attente vidée après l'envoi");
            // un seul poste : disposition + commandes + réglages d'affichage, export puis réimport sans toucher au reste
            string fl = System.IO.Path.Combine(Cfg.Dir, "ft2000.layout.ini"), fc = System.IO.Path.Combine(Cfg.Dir, "ft2000.custom.ini");
            System.IO.File.WriteAllText(fl, "af\t10\t130\t90\t40\r\n"); if (System.IO.File.Exists(fc)) System.IO.File.Delete(fc);
            Cfg.Set("analog.ft2000.sm", "1"); Cfg.Set("lock.ft2000.af", "1"); Cfg.Set("autre.reglage", "garde");
            string rs = Backup.DumpRig(d);
            Check(Backup.RigOf(rs) == "ft2000" && rs.Contains("[[rigfile:layout]]") && rs.Contains("analog.ft2000.sm=1") && !rs.Contains("autre.reglage"), "export d'un seul poste : disposition et réglages du poste, rien d'autre");
            Check(Backup.Parts(rs).Count == 0 && Backup.Import(rs, true, true, true) == 0, "fichier d'un poste seul ignoré par l'import complet");
            System.IO.File.Delete(fl); Cfg.Set("analog.ft2000.sm", "0"); Cfg.Set("lock.ft2000.xx", "1");
            Check(Backup.ImportRig(rs) >= 2, "import d'un seul poste");
            Check(System.IO.File.Exists(fl) && Cfg.Get("analog.ft2000.sm", "") == "1" && Cfg.Get("lock.ft2000.xx", "") == "" && Cfg.Get("autre.reglage", "") == "garde", "poste restauré (disposition, affichage), le reste du logiciel intact");
            System.IO.File.Delete(fl); Cfg.Set("analog.ft2000.sm", ""); Cfg.Set("lock.ft2000.af", ""); Cfg.Set("autre.reglage", "");
            rig.Disconnect(); Info("Export / import par parties : menu EXnnn lu, restauré, parties choisies, poste seul");
        }

        // ---- mise à jour : comparaison de versions, lecture de la publication, téléchargement, remplacement de l'exe, sauvegarde, script de relance ; alerte ROS qui s'éteint seule
        static void UpdateChecks()
        {
            Check(Updater.Compare("v1.10", "1.9") > 0 && Updater.Compare("1.2", "v1.2") == 0 && Updater.Compare("1.2", "1.3") < 0 && Updater.Compare("2", "1.99") > 0 && Updater.Compare("1.2.1", "1.2") > 0, "mise à jour : comparaison des numéros de version");
            
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "hrl-maj-" + Guid.NewGuid().ToString("N")); System.IO.Directory.CreateDirectory(dir);
            try
            {
                byte[] a = new byte[600000], b = new byte[610000]; a[0] = (byte)'M'; a[1] = (byte)'Z'; a[2] = 1; b[0] = (byte)'M'; b[1] = (byte)'Z'; b[2] = 2;
                string asset = System.IO.Path.Combine(dir, "HamRadioLink.exe"); System.IO.File.WriteAllBytes(asset, b);
                string json = "{\"tag_name\":\"v9.9\",\"body\":\"Notes\\r\\nligne 2 \\\"cit\\\"\",\"assets\":[{\"name\":\"HamRadioLink-9.9.zip\",\"size\":12,\"browser_download_url\":\"file:///x/HamRadioLink-9.9.zip\"},{\"name\":\"HamRadioLink.exe\",\"uploader\":{\"login\":\"x\"},\"size\":" + b.Length + ",\"browser_download_url\":\"" + new Uri(asset).AbsoluteUri + "\"}]}";
                string err; Updater.Info i = Updater.Parse(json, out err);
                Check(i != null && i.Version == "9.9" && i.Url.EndsWith("HamRadioLink.exe") && i.Size == b.Length && i.Notes.StartsWith("Notes\nligne 2 \"cit\""), "mise à jour : publication lue (version, exe parmi plusieurs fichiers, taille, notes)");
                Check(Updater.Parse("{\"tag_name\":\"v9.9\",\"assets\":[]}", out err) == null && err != null && Updater.Parse("[]", out err) == null, "mise à jour : publication sans exe ou illisible refusée");
                string jf = System.IO.Path.Combine(dir, "release.json"); System.IO.File.WriteAllText(jf, json); Updater.ApiOverride = new Uri(jf).AbsoluteUri;
                Updater.Info c = Updater.Check(out err); Check(c != null && Updater.IsNewer(c), "mise à jour : version plus récente détectée (" + err + ")");
                System.IO.File.WriteAllText(jf, json.Replace("v9.9", "v" + MainForm.Version)); c = Updater.Check(out err); Check(c != null && !Updater.IsNewer(c), "mise à jour : même version = à jour, aucun message");
                System.IO.File.WriteAllText(jf, "pas du json"); c = Updater.Check(out err); Check(c == null && err != null, "mise à jour : réponse invalide signalée");
                Updater.ApiOverride = new Uri(System.IO.Path.Combine(dir, "absent.json")).AbsoluteUri; c = Updater.Check(out err); Check(c == null && err != null, "mise à jour : adresse injoignable signalée sans planter");
                Updater.ApiOverride = null;
                string dl = System.IO.Path.Combine(dir, "dl"); System.IO.Directory.CreateDirectory(dl); int pr = 0; string f = Updater.Download(i, dl, delegate(int p) { pr = p; }, out err);
                Check(f != null && new System.IO.FileInfo(f).Length == b.Length && System.IO.File.ReadAllBytes(f)[2] == 2, "mise à jour : téléchargement identique à la publication (" + err + ")");
                i.Size = 0; System.IO.File.WriteAllBytes(asset, new byte[700000]); f = Updater.Download(i, dl, null, out err); Check(f == null && err != null && !System.IO.File.Exists(System.IO.Path.Combine(dl, "HamRadioLink.maj")), "mise à jour : fichier qui n'est pas un programme refusé et supprimé");
                System.IO.File.WriteAllBytes(asset, new byte[10]); f = Updater.Download(i, dl, null, out err); Check(f == null, "mise à jour : fichier trop petit refusé");
                // remplacement de l'exe : l'ancien est gardé sous son numéro de version
                string app = System.IO.Path.Combine(dir, "app"); System.IO.Directory.CreateDirectory(app); string exe = System.IO.Path.Combine(app, "HamRadioLink.exe"), nw = System.IO.Path.Combine(app, "HamRadioLink.maj"); System.IO.File.WriteAllBytes(exe, a); System.IO.File.WriteAllBytes(nw, b);
                string bk; bool ok = Updater.Apply(exe, nw, "1.2", new List<string>(), false, out bk, out err);
                Check(ok && System.IO.File.ReadAllBytes(exe)[2] == 2 && bk != null && System.IO.Path.GetFileName(bk) == "HamRadioLink-1.2.exe" && System.IO.File.ReadAllBytes(bk)[2] == 1 && !System.IO.File.Exists(nw), "mise à jour : nouvel exe en place, ancien conservé sous HamRadioLink-1.2.exe");
                System.IO.File.WriteAllBytes(nw, a); ok = Updater.Apply(exe, nw, "1.2", new List<string>(), false, out bk, out err); Check(ok && System.IO.Path.GetFileName(bk) != "HamRadioLink-1.2.exe" && System.IO.File.Exists(System.IO.Path.Combine(app, "HamRadioLink-1.2.exe")), "mise à jour : une sauvegarde déjà présente n'est jamais écrasée");
                ok = Updater.Apply(exe, System.IO.Path.Combine(app, "absent.maj"), "1.2", new List<string>(), false, out bk, out err); Check(!ok && err != null && System.IO.File.Exists(exe), "mise à jour : échec du remplacement = ancien exe remis en place");
                // sauvegarde et fichiers à conserver
                string dd = System.IO.Path.Combine(dir, "data"); System.IO.Directory.CreateDirectory(dd); foreach (string n in new[] { "settings.ini", "ft2000.layout.ini", "ft2000.cat.ini", "ftdx10.custom.ini", "ic7760.ini" }) System.IO.File.WriteAllText(System.IO.Path.Combine(dd, n), "x=" + n);
                string sv = Updater.BackupData(dd); Check(System.IO.Directory.GetFiles(sv, "*.ini").Length == 5 && System.IO.File.ReadAllText(System.IO.Path.Combine(sv, "settings.ini")) == "x=settings.ini", "mise à jour : copie de sécurité de tous les fichiers de réglages");
                Check(Updater.SettingsFiles(dd).Count == 1 && Updater.PostFiles(dd).Count == 3, "mise à jour : réglages du logiciel et fichiers des postes distingués (" + Updater.PostFiles(dd).Count + ")");
                string sc = Updater.BuildScript(1234, @"C:\x y\HamRadioLink.exe", new List<string> { @"C:\x y\data\settings.ini" });
                Check(sc.Contains("PID eq 1234") && sc.Contains("del /f /q \"C:\\x y\\data\\settings.ini\"") && sc.Contains("start \"\" \"C:\\x y\\HamRadioLink.exe\"") && sc.IndexOf("del /f") < sc.IndexOf("start \"\""), "mise à jour : script de relance (attend la fin, supprime ce qui n'est pas conservé, relance, s'efface)");
            }
            finally { Updater.ApiOverride = null; try { System.IO.Directory.Delete(dir, true); } catch { } }
            int s = 0; Check(!RosGuard.ShouldClear(2.0, ref s, 1000, 3.0, 1500) && !RosGuard.ShouldClear(2.0, ref s, 2499, 3.0, 1500) && RosGuard.ShouldClear(2.0, ref s, 2500, 3.0, 1500), "alerte ROS : s'éteint seule 1,5 s après le retour sous le seuil");
            s = 0; RosGuard.ShouldClear(2.0, ref s, 1000, 3.0, 1500); Check(!RosGuard.ShouldClear(3.5, ref s, 1200, 3.0, 1500) && s == 0 && !RosGuard.ShouldClear(2.0, ref s, 2600, 3.0, 1500), "alerte ROS : un nouveau dépassement relance l'attente");
            s = 0; Check(!RosGuard.ShouldClear(-1, ref s, 9000, 3.0, 0), "alerte ROS : sans lecture du ROS, l'alerte n'est jamais éteinte d'office");
        }

        // ---- échelles des vumètres d'émission et étalonnage du wattmètre (FT-2000)
        static void ScaleChecks()
        {
            RadioDef d = Radios.ByKey("ftdx10"); string k = d.Key; foreach (string z in new string[] { "pmax", "pfull", "po", "swr", "alc", "comp", "idd", "vdd" }) Cfg.Set("cal." + k + "." + z, "");
            Check(MeterCal.PMax(k) == 100 && MeterCal.Active(d) && !MeterCal.Active(Radios.ByKey("tci")), "Échelles : 100 W par défaut, actives pour les postes ayant des vumètres d'émission");
            Check(string.Join(",", MeterCal.Steps(100).ConvertAll<string>(delegate(int w) { return w.ToString(); }).ToArray()) == "10,20,30,40,50,60,70,80,90,100", "Étalonnage : paliers jusqu'à 100 W");
            Check(string.Join(",", MeterCal.Steps(50).ConvertAll<string>(delegate(int w) { return w.ToString(); }).ToArray()) == "10,20,30,40,50", "Étalonnage : poste 50 W, arrêt à 50 W");
            Check(MeterCal.Steps(200).Count == 15 && MeterCal.Steps(200)[14] == 200 && MeterCal.Steps(25)[MeterCal.Steps(25).Count - 1] == 25, "Étalonnage : poste 200 W jusqu'à 200 W, puissance max toujours en dernier");
            Check(MeterCal.VddFull(200) == 50 && MeterCal.VddNominal(200) == 50 && MeterCal.VddNominal(100) == 13.8, "Échelle Vdd : 50 V pour 200 W, 13,8 V sinon");
            Check(Math.Abs(MeterCal.SwrPos(1)) < 1e-9 && Math.Abs(MeterCal.SwrPos(3) - 0.5) < 1e-9 && MeterCal.SwrPos(1e9) > 0.999, "Échelle SWR : 1 au début, 3 au milieu, infini au bout");
            Check(MeterCal.CompDb(0) == 0 && MeterCal.CompDb(0.5) == 10 && MeterCal.CompDb(1) == 30, "Échelle COMP : 0 dB, 10 dB au milieu, 30 dB en pleine déviation");
            foreach (int pm in new int[] { 10, 50, 100, 200 })
            {
                MeterCal.SetPMax(k, pm); Sc po = MeterCal.Scale(d, d.Find("po")); Check(po != null && Math.Abs(po.Frac(255) - 1) < 1e-9 && po.Text(255) == pm + " W" && po.Pos[po.Pos.Length - 1] > 0.999 && po.Ticks.Length <= 7, "Échelle Po : pleine déviation " + pm + " W");
            }
            MeterCal.SetPMax(k, 100); Sc v = MeterCal.Scale(d, d.Find("vdd")); Check(v.Text(255) == "15.0 V", "Échelle Vdd 100 W : 15 V en pleine déviation"); MeterCal.SetPMax(k, 200); v = MeterCal.Scale(d, d.Find("vdd")); Check(v.Text(255) == "50.0 V" && v.Ticks.Length == 6, "Échelle Vdd 200 W : 50 V en pleine déviation");
            Check(MeterCal.Scale(d, d.Find("idd")).Text(255) == "15.0 A" && MeterCal.Scale(d, d.Find("comp")).Text(255) == "30 dB" && MeterCal.Scale(d, d.Find("comp")).Text(128) == "10 dB" && MeterCal.Scale(d, d.Find("swr")).Text(255) == "∞" && MeterCal.Scale(d, d.Find("swr")).Text(0) == "1.0" && MeterCal.Scale(d, d.Find("alc")).BlueRed && MeterCal.Scale(d, d.Find("sm")) == null, "Échelles Id 15 A, COMP, SWR, ALC bleu/rouge ; S-mètre inchangé");
            MeterCal.SetPMax(k, 100); MeterCal.SavePoints(k, new List<double[]> { new double[] { 100, 10 }, new double[] { 200, 50 }, new double[] { 240, 100 } });
            Sc pc = MeterCal.Scale(d, d.Find("po")); Check(pc.Text(100) == "10 W" && pc.Text(150) == "30 W" && pc.Text(255) == "100 W" && Math.Abs(pc.Frac(240) - 1) < 1e-9, "Étalonnage Po : interpolation entre les points relevés");
            MeterCal.SetPMax(k, 200); MeterCal.SavePoints(k, new List<double[]> { new double[] { 100, 20 }, new double[] { 150, 50 }, new double[] { 240, 200 } });
            MeterCal.SetPFull(k, 50); Sc p50 = MeterCal.Scale(d, d.Find("po")); Check(MeterCal.PFull(k) == 50 && Math.Abs(p50.Frac(150) - 1) < 1e-9 && Math.Abs(p50.Frac(100) - 0.4) < 1e-9 && p50.Text(100) == "20 W" && p50.Ticks[p50.Ticks.Length - 1] == "50" && p50.Frac(240) == 1, "Échelle Po 50 W sur poste 200 W étalonné : pleine déviation à 50 W, sans nouvel étalonnage");
            MeterCal.SetPFull(k, 300); Check(MeterCal.PFull(k) == 200, "Échelle Po : pleine déviation limitée à la puissance max");
            MeterCal.SetPFull(k, 60); MeterCal.SetPMax(k, 50); Check(MeterCal.PFull(k) == 50, "Échelle Po : pleine déviation ramenée si la puissance max baisse");
            MeterCal.SetPMax(k, 100);
            MeterCal.SavePoints(k, "alc", new List<double[]> { new double[] { 100, 40 } }); Sc al = MeterCal.Scale(d, d.Find("alc")); Check(al.Text(100) == "40%" && al.Text(50) == "20%" && al.Text(200) == "80%" && Math.Abs(al.Frac(250) - 1.0) < 1e-9, "Étalonnage ALC : un point, extrapolation depuis l'origine");
            MeterCal.SavePoints(k, "swr", new List<double[]> { new double[] { 48, 1.5 }, new double[] { 120, 3.0 } }); Sc sw = MeterCal.Scale(d, d.Find("swr")); Check(sw.Text(48) == "1.5" && sw.Text(120) == "3.0" && Math.Abs(sw.Frac(120) - 0.5) < 1e-9 && sw.Text(0) == "1.0" && sw.Text(255) == "∞", "Étalonnage SWR : deux points, 3 au milieu de l'échelle, prolongé jusqu'à l'infini");
            MeterCal.SavePoints(k, "comp", new List<double[]> { new double[] { 128, 10 }, new double[] { 255, 30 } }); Sc cp = MeterCal.Scale(d, d.Find("comp")); Check(cp.Text(128) == "10 dB" && cp.Text(255) == "30 dB" && Math.Abs(cp.Frac(128) - 0.5) < 1e-9 && Math.Abs(cp.Frac(255) - 1) < 1e-9, "Étalonnage COMP : 10 dB au milieu, 30 dB en pleine déviation");
            MeterCal.SavePoints(k, "idd", new List<double[]> { new double[] { 100, 6 } }); Sc id2 = MeterCal.Scale(d, d.Find("idd")); Check(id2.Text(100) == "6.0 A" && id2.Text(250) == "15.0 A" && Math.Abs(id2.Frac(250) - 1.0) < 0.001, "Étalonnage Id : un point, pleine déviation 15 A");
            MeterCal.SavePoints(k, "vdd", new List<double[]> { new double[] { 100, 13.8 } }); Sc vd2 = MeterCal.Scale(d, d.Find("vdd")); Check(vd2.Text(100) == "13.8 V" && MeterCal.Interp(new List<double[]>(), 99, 0) == 0, "Étalonnage Vdd : lecture reliée à la tension réelle");
            foreach (string mid in new string[] { "alc", "swr", "comp", "idd", "vdd" }) MeterCal.SavePoints(k, mid, new List<double[]>());
            Check(MeterCal.Scale(d, d.Find("alc")).Text(100) == "39%", "Échelles : retour aux valeurs d'origine après effacement");
            MeterCal.SavePoints(k, new List<double[]>()); Cfg.Set("cal." + k + ".pmax", ""); Cfg.Set("cal." + k + ".pfull", ""); Ver0();
            Info("Échelles des vumètres et étalonnage du wattmètre vérifiés"); RadioScaleChecks(); Consistency(); AlignChecks(); MenuChecks(); AllRigChecks();
        }
        static void Ver0() { MeterCal.Ver++; }
        // ---- fonctions étendues à tous les postes : menu EX (tables des manuels), boutons MENU, étalonnage Width / CONT / RF PWR, réglages d'origine
        static void AllRigChecks()
        {
            // menu : tables des manuels pour FT-950, FT-991A, FTdx3000 ; bouton MENU présent exactement sur les postes à menu, sans chevauchement ni sortie de fenêtre
            string[] mk = { "ft950", "ft991a", "ftdx3000" }; int tables = 0;
            foreach (string k in mk)
            {
                RigMenu.Key = k; int cnt = RigMenu.Count; int named = 0, withVals = 0, bad = 0;
                for (int n = 1; n <= cnt; n++) { string nm = RigMenu.ManualName(n); if (nm.Length > 0) named++; if (RigMenu.ManualChoices(n, false).Count > 0) withVals++; if (nm.Length > 0 && (nm.Length < 3 || nm.Contains("|"))) bad++; }
                Check(cnt >= 100 && named >= 100 && withVals >= 30 && bad == 0 && RigMenu.ManualName(1).StartsWith("AGC FAST"), "Menu " + k + " : table du manuel (" + cnt + " menus, " + named + " noms, " + withVals + " listes de valeurs)"); tables++;
            }
            RigMenu.Key = "ft950"; Check(RigMenu.Name(new Dictionary<string, string>(), 999) == "Menu 999" && RigMenu.Name(new Dictionary<string, string>(), 1).StartsWith("AGC FAST") && RigMenu.FileVals.EndsWith("menu-ft950.ini") && RigMenu.FileDefs.EndsWith("menu-ft950-defs.ini"), "Menu FT-950 : nom par défaut, fichiers propres au poste");
            RigMenu.Key = "ftdx5000"; Check(!RigMenu.HasTable && RigMenu.Count == 199 && RigMenu.Name(new Dictionary<string, string>(), 5) == "Menu 005", "Menu FTdx5000 : pas de table, numéros seuls (199 menus, lecture jusqu'à 20 absences)");
            RigMenu.Key = "ft991a"; bool crit = false; for (int n = 1; n <= RigMenu.Count; n++) if (RigMenu.Critical(n)) crit = true; Check(crit, "Menu FT-991A : des menus critiques (CAT, puissance…) sont signalés");
            RigMenu.Key = "ft2000"; Check(RigMenu.Count == 149 && RigMenu.Critical(28) && RigMenu.Name(RigMenu.LoadDefs(), 10) == "DISP BAR SEL" && RigMenu.Name(RigMenu.LoadDefs(), 17) == "DUS TX LEVEL", "Menu FT-2000 : 149 menus, noms d'origine relevés sur le poste (010, 012, 013, 016, 017)");
            int withBtn = 0, noBtn = 0, badBtn = 0; string ovl = "";
            foreach (RadioDef d in Radios.All())
            {
                Ctl m = d.Find("menu"); bool has = Backup.HasMenu(d) || d.Key == "ftdx10";
                if (has != (m != null)) badBtn++; else if (has) { withBtn++; if (!m.Local || m.K != Kind.Action || m.W < 60 || m.H < 30 || m.X < 0 || m.Y < 118 || m.X + m.W > 1860 || m.Y + m.H > 980) badBtn++; } else noBtn++;
                foreach (Ctl o in d.C) if (has && m != null && o != m && o.K != Kind.Frame && m.X < o.X + o.W && o.X < m.X + m.W && m.Y < o.Y + o.H && o.Y < m.Y + m.H) { badBtn++; ovl += d.Key + ":" + o.Id + " "; }
            }
            Check(withBtn == 8 && badBtn == 0, "Bouton MENU : sur les " + withBtn + " postes à menu (FTdx10 avec son menu propre, FT-2000, FTdx5000, FT-450, FT-950, FT-991A, FT-991A DF5DP, FTdx3000), absent des " + noBtn + " autres, sans chevauchement (" + badBtn + " défaut " + ovl + ")");
            // étalonnage Width / CONT / RF PWR : disponible sur tous les postes qui ont ces boutons ; sans étalonnage l'affichage d'origine est conservé
            int kn = 0, knBad = 0;
            foreach (RadioDef d in Radios.All())
                foreach (string id in new string[] { "width", "contf", "pwr" })
                {
                    Ctl c = d.Find(id); if (c == null || c.K != Kind.Knob) { if (MeterCal.KnobActive(d, c)) knBad++; continue; }
                    kn++; if (!MeterCal.KnobActive(d, c)) knBad++;
                    if (d.Key != "ft2000" && !(d.Key == "ftdx10" && id == "width") && (MeterCal.KnobText(d, c, c.Min) != null || MeterCal.KnobMax(d, c) != c.Max)) knBad++;
                }
            Check(kn > 20 && knBad == 0, "Width / CONT / RF PWR : " + kn + " boutons sur tous les postes, affichage d'origine inchangé tant qu'il n'est pas étalonné (" + knBad + " défaut)");
            RadioDef x = Radios.ByKey("ftdx10"); Ctl xw = x.Find("width"), xp = x.Find("pwr"); string kx = x.Key; Cfg.RemovePrefix("cal." + kx + ".");
            if (xw != null && xp != null)
            {
                MeterCal.SavePoints(kx, "width", new List<double[]> { new double[] { 2, 300 }, new double[] { 12, 2400 }, new double[] { 22, 3600 } }); MeterCal.SavePoints(kx, "rfpwr", new List<double[]> { new double[] { 10, 10 }, new double[] { 100, 100 } });
                Check(MeterCal.KnobText(x, xw, 12) == "2400 Hz" && MeterCal.KnobText(x, xw, 30) == "4000 Hz" && MeterCal.KnobText(x, xp, 55) == "55 W" && MeterCal.KnobMax(x, xp) == 100, "Width / RF PWR étalonnés sur un autre poste (FTdx10) : Hz et W relevés, bornes respectées"); Cfg.RemovePrefix("cal." + kx + "."); MeterCal.Ver++;
            }
            // réglages d'origine du FT-2000 : tout ce que l'utilisateur a réglé, sauf la pleine déviation Po à 100 W (et non 50 W)
            RadioDef ft = Radios.ByKey("ft2000"); Cfg.RemovePrefix("cal.ft2000."); MeterCal.Ver++; Sc po = MeterCal.Scale(ft, ft.Find("po")); Ctl pw = ft.Find("pwr");
            Check(MeterCal.PMax("ft2000") == 200 && MeterCal.PFull("ft2000") == 50 && po.Ticks[po.Ticks.Length - 1] == "50" && MeterCal.KnobMax(ft, pw) == 95 && MeterCal.KnobText(ft, pw, 249) == "50 W", "FT-2000 : réglages d'origine = ceux de l'utilisateur, pleine déviation Po à 50 W (" + MeterCal.PFull("ft2000") + " W, tick " + po.Ticks[po.Ticks.Length - 1] + ", max " + MeterCal.KnobMax(ft, pw) + ", " + MeterCal.KnobText(ft, pw, 249) + ")");
            { Cfg.RemovePrefix("ros."); Cfg.RemovePrefix("tune."); Cfg.RemovePrefix("meter.hold"); Cfg.RemovePrefix("wheelpct"); HW.WheelPct = Math.Max(1, Math.Min(10, Cfg.GetInt("wheelpct", 1))); string bad = ""; if (RosGuard.Threshold != 2.0) bad += " seuil=" + RosGuard.Threshold; if (RosGuard.DelayMs != 300) bad += " delai=" + RosGuard.DelayMs; if (RosGuard.IntervalMs != 50) bad += " pause=" + RosGuard.IntervalMs; if (!RosGuard.Drop) bad += " chute"; if (RosGuard.DropPct != 0) bad += " pct=" + RosGuard.DropPct; if (!RosGuard.TestVox) bad += " voxtest"; if (TuneMic.Level != 15) bad += " tune=" + TuneMic.Level; if (!TuneMic.Vox) bad += " tunevox"; if (HMeter.HoldMs != 10000) bad += " hold=" + HMeter.HoldMs; if (HW.WheelPct != 1) bad += " molette=" + HW.WheelPct; Check(bad.Length == 0, "Réglages d'origine du programme : ROS (seuil 2, délai 0,3 s, bips 50 ms, chute 0 %, test VOX), TUNE 15 % en VOX, maintien vumètre 10 s, molette 1 %" + bad); }
            Rig rr = new Rig(); rr.Def = ft; Rig r2 = new Rig(); r2.Def = x; Check(rr.PttLimit == 30 && r2.PttLimit == 0, "Délai de sécurité TX : 30 s d'origine pour le FT-2000 seulement");
            Info("Fonctions étendues à tous les postes vérifiées");
        }
        // ---- menu interne du FT-2000 (menus 001 à 149) : noms, formats, lecture / écriture contre le poste simulé, noms et valeurs personnalisés
        static void MenuChecks()
        {
            string dir = Path.Combine(Path.GetTempPath(), "hrl-menu-" + Guid.NewGuid().ToString("N").Substring(0, 8)); Directory.CreateDirectory(dir); RigMenu.Dir = dir;
            Check(Ft2000MenuNames.Names.Length == 149 && RigMenu.ManualName(1) == "AGC FAST DELAY TIME" && RigMenu.ManualName(149) == "EMERGENCY CHANNEL" && RigMenu.ManualName(76) == "28 MHz REPEATER SHIFT" && RigMenu.ManualName(139).Contains("BANDWIDTH (MID RANGE)"), "Menu FT-2000 : 149 noms du manuel");
            bool empty = false; foreach (string s in Ft2000MenuNames.Names) if (s.Trim().Length < 4) empty = true; Check(!empty, "Menu FT-2000 : aucun nom vide");
            Check(Ft2000MenuInfo.P2.Length == 149 && Ft2000MenuInfo.Bytes.Length == 149 && Ft2000MenuInfo.Bytes[0] == 4 && Ft2000MenuInfo.Bytes[6] == 1 && Ft2000MenuInfo.Bytes[14] == 7 && Ft2000MenuInfo.Bytes[123] == 13, "Menu FT-2000 : valeurs possibles et nombre de caractères du manuel pour les 149 menus");
            bool noP2 = false; foreach (string s in Ft2000MenuInfo.P2) if (s.Trim().Length < 3) noP2 = true; Check(!noP2, "Menu FT-2000 : chaque menu a ses valeurs possibles");
            List<KeyValuePair<string, string>> c7 = RigMenu.ManualChoices(7, false), c28 = RigMenu.ManualChoices(28, false), c97 = RigMenu.ManualChoices(97, false), c131 = RigMenu.ManualChoices(131, false);
            Check(c7.Count == 5 && c7[0].Key == "0" && c7[0].Value == "COOL BLUE" && c7[4].Value == "UMBER" && c28.Count == 4 && c28[3].Key == "3" && c28[3].Value == "38400 bps" && c97.Count == 12 && c97[11].Key == "11" && c97[11].Value == "2000 Hz" && c131.Count == 19 && RigMenu.ManualChoices(1, false).Count == 0 && RigMenu.ManualChoices(38, false).Count == 1 && RigMenu.ManualChoices(35, false).Count == 7 && RigMenu.ManualChoices(11, false)[1].Value == "0.5 sec", "Menu FT-2000 : listes de valeurs du manuel décomposées (couleur, CAT, filtres, égaliseur, plages)");
            Check(RigMenu.ManualChoices(144, false)[0].Value == "10 W" && RigMenu.ManualChoices(144, true)[0].Value == "20 W" && RigMenu.ManualChoices(144, true)[3].Value == "200 W" && RigMenu.ManualChoices(147, true)[3].Value == "200 W" && RigMenu.ManualChoices(147, false)[3].Value == "100 W", "Menu FT-2000 : puissances maximales distinctes FT-2000 (100 W) et FT-2000D (200 W)");
            Check(RigMenu.Format("0300", "400") == "0400" && RigMenu.Format("0300", "12345") == null && RigMenu.Format("+0000", "-25") == "-0025" && RigMenu.Format("+0000", "30") == "+0030" && RigMenu.Format("1", "7") == "7" && RigMenu.Format("1", "-1") == null && RigMenu.Format("00", "abc") == null && RigMenu.Format(null, "1") == null, "Menu FT-2000 : saisie mise au format du poste (chiffres, signe)");
            RadioDef d = Radios.ByKey("ft2000"); Ctl mn = d.Find("menu"), me = d.Find("meter");
            Check(mn != null && me != null && mn.Local && mn.K == Kind.Action && mn.W == me.W && mn.H == me.H && mn.Y == me.Y && mn.X == me.X + me.W + 10 && Radios.ByKey("ftdx10").Find("menu") != null && Radios.ByKey("ftdx5000").Find("menu") != null, "Menu FT-2000 : bouton MENU à droite de METER, même taille ; présent sur le FTdx10 (menu propre), absent des postes sans menu EX");
            d.LoadUser(); Rig rig = new Rig(); VirtualLink vl = new VirtualLink(d); string err; rig.Connect(d, vl, out err); Wait(delegate { return rig.Get("pwr") != null; }, 8000);
            int got = 0; Dictionary<int, string> vals = new Dictionary<int, string>(); for (int n = 1; n <= 149; n++) { string v = RigMenu.Read(rig, n); if (v != null) { got++; vals[n] = v; } }
            Check(got == 149 && vals[1] == "0300" && vals[86] == "+0000", "Menu FT-2000 : les 149 menus sont lus (" + got + ")");
            RigMenu.SaveVals(vals); Dictionary<int, string> back = RigMenu.LoadVals(); Check(back.Count == 149 && back[1] == "0300" && back[86] == "+0000", "Menu FT-2000 : valeurs mémorisées puis rechargées");
            string w = RigMenu.Write(rig, 1, RigMenu.Format(vals[1], "420")); string w2 = RigMenu.Write(rig, 86, RigMenu.Format(vals[86], "-25"));
            Check(w == "0420" && w2 == "-0025" && RigMenu.Read(rig, 1) == "0420", "Menu FT-2000 : valeur écrite puis relue (0300 -> 0420, +0000 -> -0025)");
            RigMenu.Write(rig, 1, "0300"); RigMenu.Write(rig, 86, "+0000");
            using (MenuDlg dlg = new MenuDlg(rig))
            {
                Check(dlg.RowsShown == 149 && dlg.TestValue(1) == "0300" && dlg.TestValue(86) == "+0000" && dlg.TestName(1) == "AGC FAST DELAY TIME" && dlg.TestName(28).Contains("⚠"), "Fenêtre Menu : 149 lignes, valeurs mémorisées, menus critiques signalés");
                Check(dlg.TestMean(7) == "COOL BLUE" && dlg.TestMean(28) == "4800 bps", "Fenêtre Menu : signification lue dans la liste du manuel (menu 7 = COOL BLUE)"); dlg.TestApply(7, "3"); Check(dlg.TestMean(7) == "CONTRAST UMBER" && RigMenu.Read(rig, 7) == "03", "Fenêtre Menu : choix dans la liste envoyé au poste (menu 7 -> 3 = CONTRAST UMBER)"); dlg.TestApply(7, "0"); Check(dlg.ClientSize.Width <= 1366 && dlg.TestFit(1920, 1080) == new System.Drawing.Size(1366, 700) && dlg.TestFit(1280, 720).Width == 1256 && dlg.TestFit(1280, 720).Height == 640 && dlg.TestFit(800, 600).Width == 776, "Fenêtre Menu : 1366 x 700 par défaut, réduite si l'écran est plus petit"); dlg.TestApply(2, "480"); Check(dlg.TestValue(2) == "0480" && RigMenu.Read(rig, 2) == "0480" && RigMenu.LoadVals()[2] == "0480", "Fenêtre Menu : modification envoyée au poste, relue et mémorisée");
                dlg.TestApply(2, "99999"); Check(dlg.TestValue(2) == "0480" && dlg.TestStatus.Contains("refusée"), "Fenêtre Menu : valeur trop longue refusée, valeur du poste inchangée");
                RigMenu.Write(rig, 2, "0300");
            }
            rig.Disconnect();
            Dictionary<string, string> defs = new Dictionary<string, string>(); defs["001.name"] = "AGC FST DLY"; defs["028.vals"] = "0=4800;1=9600;2=19200;3=38400;4=57600"; RigMenu.SaveDefs(defs); Dictionary<string, string> d2 = RigMenu.LoadDefs();
            Check(RigMenu.Name(d2, 1) == "AGC FST DLY" && RigMenu.Name(d2, 2) == "AGC FAST PEAK HOLD TIME" && RigMenu.Meaning(d2, 28, "3") == "38400" && RigMenu.Meaning(d2, 28, "03") == "38400" && RigMenu.Meaning(d2, 28, "9") == "" && RigMenu.Choices(d2, 28).Count == 5 && RigMenu.Critical(28) && !RigMenu.Critical(1), "Menu FT-2000 : noms et valeurs possibles personnalisés (clic droit)");
            RigMenu.Dir = null; try { Directory.Delete(dir, true); } catch { }
            Info("Menu interne du FT-2000 vérifié");
        }
        // chevauchements présents dans la disposition d'origine du FT-2000 (celle validée sur le poste de l'utilisateur) : tolérés, signalés à l'utilisateur
        static bool Df5dpOverlap(string a, string b)   // chevauchements voulus de la disposition intégrée du FT-991A DF5DP
        {
            string p = a + "+" + b, q = b + "+" + a;
            foreach (string s in new string[] { "fa+l_busy", "fa+l_tx", "fa+l_mem", "dial+step" }) if (s == p || s == q) return true;
            return false;
        }
        static bool Ft2000UserOverlap(string a, string b)
        {
            string p = a + "+" + b, q = b + "+" + a;
            foreach (string s in new string[] { }) if (s == p || s == q) return true;   // plus aucun chevauchement toléré sur le FT-2000
            return false;
        }
        // ---- « Aligner » : même taille pour les éléments de même forme d'une section, centre conservé, résultat stable, valable pour tous les postes
        static void AlignChecks()
        {
            RadioDef f0 = Radios.ByKey("ft2000"); Ctl k1 = f0.Find("clar"), k2 = f0.Find("clartx"), k3 = f0.Find("nbl");
            Check(k1 != null && k2 != null && k3 != null && k1.X == 1680 && k1.Y == 594 && k1.DX == 1680 && k1.W == 110 && k1.H == 112 && k2.X == 1684 && k2.Y == 772 && k2.DY == 772 && k3.X == 324 && k3.Y == 894 && k3.W == 94 && k3.H == 76, "FT-2000 : disposition d'origine = disposition validée sur le poste");
            Check(Radios.ByKey("ftdx5000").Find("clar").X != 1680, "FT-2000 : la disposition d'origine ne touche pas les autres postes");
            RadioDef t = Radios.ByKey("ft2000"); Ctl[] kn = new Ctl[] { t.Find("clar"), t.Find("shift"), t.Find("width"), t.Find("notch"), t.Find("cont") };
            Ctl cl = t.Find("clar"); int cx = cl.X + cl.W / 2, cy = cl.Y + cl.H / 2; cl.W -= 24; cl.H -= 20; cl.X += 12; cl.Y += 10;   // CLAR plus petit que ses voisins
            Ctl tb = t.Find("clartx") ?? t.Find("clar_tx");
            int n = Align.Run(t); Ctl w = t.Find("width");
            Check(n > 0 && cl.W == w.W && cl.H == w.H && Math.Abs(cl.X + cl.W / 2 - cx) <= 1 && Math.Abs(cl.Y + cl.H / 2 - cy) <= 1, "Aligner : le potentiomètre CLAR plus petit reprend la taille de ses voisins, centre conservé");
            Check(Align.Run(t) == 0, "Aligner : deuxième passage sans effet (résultat stable)");
            int bad = 0, rigs = 0;
            foreach (RadioDef d in Radios.All())
            {
                int before = d.C.Count; Align.Run(d); bool same = Align.Run(d) == 0; bool ok = same && d.C.Count == before;
                foreach (Ctl c in d.C) if (c.W < 4 || c.H < 4) ok = false;
                if (!ok) bad++; rigs++;
            }
            Check(bad == 0, "Aligner : stable et sans taille aberrante sur " + rigs + " postes (" + bad + " en défaut)");
        }
        // ---- cohérence des échelles sur TOUS les postes : monotone, bornée, graduations ordonnées, puissance max plausible, réglages d'origine du FT-2000
        static void RadioScaleChecks()
        {
            RadioDef ft = Radios.ByKey("ft2000"); string fk = ft.Key;
            Cfg.RemovePrefix("cal." + fk + "."); MeterCal.Ver++;
            Sc fpo = MeterCal.Scale(ft, ft.Find("po"));
            Check(MeterCal.PMax(fk) == 200 && MeterCal.PFull(fk) == 50 && MeterCal.Points(fk).Count == 15 && fpo.Text(224) == "200 W" && fpo.Text(115) == "50 W" && Math.Abs(fpo.Frac(115) - 1) < 1e-9 && MeterCal.Scale(ft, ft.Find("swr")).Text(77) == "1.7" && MeterCal.Scale(ft, ft.Find("alc")).Text(129) == "50%" && MeterCal.Scale(ft, ft.Find("idd")).Text(20) == "1.1 A" && MeterCal.Scale(ft, ft.Find("vdd")).Text(195) == "50.0 V", "FT-2000 : réglages d'origine (200 W, échelle Po 50 W, étalonnage relevé) appliqués sans rien enregistrer");
            Ctl wc = ft.Find("width"), pw = ft.Find("pwr");
            Check(MeterCal.KnobText(ft, wc, 1) == "200 Hz" && MeterCal.KnobText(ft, wc, 16) == "2400 Hz" && MeterCal.KnobText(ft, wc, 29) == "3790 Hz" && MeterCal.KnobText(ft, wc, 31) == "4000 Hz" && MeterCal.KnobText(ft, wc, 0) == "200 Hz" && MeterCal.KnobText(ft, wc, 22) == "3040 Hz" && MeterCal.KnobText(ft, wc, 8) == "1230 Hz" && MeterCal.KnobText(ft, pw, 95) == "50 W" && MeterCal.KnobText(ft, pw, 70) == "30 W" && MeterCal.KnobMax(ft, pw) == 95 && MeterCal.KnobText(Radios.ByKey("ftdx5000"), Radios.ByKey("ftdx5000").Find("width"), 16) == null, "Width : 200 Hz / 2400 Hz / 4000 Hz aux positions 1 / 16 / 31 ; RF PWR en W (échelle Po 50 W) ; autres postes inchangés");
            MeterCal.SavePoints(fk, "width", new List<double[]> { new double[] { 2, 400 }, new double[] { 10, 1800 }, new double[] { 20, 3000 } }); MeterCal.SavePoints(fk, "rfpwr", new List<double[]> { new double[] { 11, 10 }, new double[] { 110, 100 }, new double[] { 200, 200 } });
            MeterCal.SetPFull(fk, 200); Check(MeterCal.KnobText(ft, wc, 10) == "1800 Hz" && MeterCal.KnobText(ft, wc, 2) == "400 Hz" && MeterCal.KnobText(ft, pw, 110) == "100 W" && MeterCal.KnobText(ft, pw, 200) == "200 W" && MeterCal.KnobMax(ft, pw) == 200, "Width / RF PWR étalonnés : Hz et W relevés, pleine échelle de RF PWR = puissance max");
            MeterCal.SetPMax(fk, 100); Check(MeterCal.KnobMax(ft, pw) == 110, "RF PWR : pleine échelle suit la puissance max (100 W)"); MeterCal.SetPMax(fk, 200); MeterCal.SetPFull(fk, 50); Check(MeterCal.KnobMax(ft, pw) == 55 && MeterCal.KnobText(ft, pw, 200) == "50 W" && MeterCal.KnobText(ft, pw, 55) == "50 W" && MeterCal.KnobText(ft, pw, 30) == "27 W" && MeterCal.KnobText(ft, pw, 11) == "10 W", "RF PWR : échelle Po à 50 W -> bouton de 10 à 50 W, au-delà il reste à 50 W"); Cfg.RemovePrefix("cal." + fk + "."); MeterCal.Ver++;
            Cfg.RemovePrefix("cal.zzz."); MeterCal.Ver++; bool swrOk = Math.Abs(MeterCal.SwrRatio("zzz", 48) - 1.5) < 0.01;
            MeterCal.SavePoints("zzz", "swr", new List<double[]> { new double[] { 100, 2.0 }, new double[] { 200, 5.0 } });
            Check(swrOk && Math.Abs(MeterCal.SwrRatio("zzz", 100) - 2.0) < 0.01 && Math.Abs(MeterCal.SwrRatio("zzz", 200) - 5.0) < 0.01 && MeterCal.SwrRatio("zzz", 0) < 1.01 && MeterCal.SwrRatio("zzz", 150) > 2.0 && MeterCal.SwrRatio("zzz", 150) < 5.0 && MeterCal.SwrRatio("zzz", 255) > 5.0, "Étalonnage du ROS : table d'origine sans point, puis lecture calée sur les mesures et extrapolée entre / au-delà");
            Cfg.RemovePrefix("cal.zzz."); MeterCal.Ver++;
            int rigs = 0, meters = 0;
            foreach (RadioDef d in Radios.All())
            {
                string k = d.Key; if (k == "ft2000") continue; bool bad = false; string why = "";
                int pm = MeterCal.PMax(k); if (pm < 5 || pm > 200 || MeterCal.PFull(k) != pm) { bad = true; why += " pmax=" + pm; }
                foreach (string id in new string[] { "po", "swr", "alc", "comp", "idd", "vdd" })
                {
                    Ctl c = d.Find(id); bool has = c != null && c.K == Kind.Meter && c.Fmt == "pct"; Sc s = MeterCal.Scale(d, c);
                    if (has != (s != null)) { bad = true; why += " " + id + "=présence"; continue; }
                    if (s == null) continue; meters++;
                    if (s.Ticks.Length != s.Pos.Length || s.Ticks.Length < 3) { bad = true; why += " " + id + "=graduations"; }
                    for (int i = 1; i < s.Pos.Length; i++) if (s.Pos[i] <= s.Pos[i - 1] || s.Pos[i] > 1.0001) { bad = true; why += " " + id + "=ordre"; break; }
                    double prev = -1; for (int v = 0; v <= c.Max; v += Math.Max(1, c.Max / 40)) { double f = s.Frac(v); if (double.IsNaN(f) || f < -1e-9 || f > 1 + 1e-9 || f < prev - 1e-9 || s.Text(v).Length == 0) { bad = true; why += " " + id + "@" + v; break; } prev = f; }
                    if (Math.Abs(s.Frac(c.Max) - 1.0) > 0.03 && id != "swr" && id != "comp") { bad = true; why += " " + id + "=pleine déviation"; }
                    if (id == "po" && Math.Abs(double.Parse(s.Text(c.Max).Replace(" W", ""), System.Globalization.CultureInfo.InvariantCulture) - pm) > 0.01) { bad = true; why += " po=" + s.Text(c.Max); }
                    // avec un point d'étalonnage au milieu, l'échelle reste ordonnée et le point est restitué
                    double mid = c.Max / 2.0; double real = id == "swr" ? 2.0 : id == "po" ? pm / 2.0 : id == "alc" ? 50 : id == "comp" ? 10 : id == "idd" ? 7.5 : MeterCal.VddNominal(pm);
                    MeterCal.SavePoints(k, id, new List<double[]> { new double[] { Math.Round(mid * 255.0 / c.Max, 1), real } }); Sc s2 = MeterCal.Scale(d, c); double q = -1; bool ok2 = s2 != null;
                    for (int v = 0; ok2 && v <= c.Max; v += Math.Max(1, c.Max / 40)) { double f = s2.Frac(v); if (double.IsNaN(f) || f < q - 1e-9 || f > 1 + 1e-9) ok2 = false; q = f; }
                    if (!ok2) { bad = true; why += " " + id + "=étalonné"; }
                    MeterCal.SavePoints(k, id, new List<double[]>());
                }
                rigs++; Check(!bad, "Échelles : cohérence sur " + d.Name + " (" + k + ")" + why);
            }
            Info("Échelles d'émission : " + rigs + " postes, " + meters + " vumètres vérifiés (monotonie, bornes, graduations, puissance max, étalonnage)");
        }
        // ---- messages d alerte : ajout horodaté, non lu / lu, purge à 7 jours, effacement, pas de doublon dans la minute
        static void AlertChecks()
        {
            AlertLog.Clear(); Check(!AlertLog.Unread, "alertes : aucun message non lu après effacement");
            AlertLog.Add("FT-2000", "Perte de liaison : essai"); Check(AlertLog.Unread && AlertLog.UnreadCount == 1 && AlertLog.All().Count == 1, "alertes : un message ajoute allume le bouton");
            AlertLog.Add("FT-2000", "Perte de liaison : essai"); Check(AlertLog.All().Count == 1, "alertes : le meme message n est pas repete dans la minute");
            System.Threading.Thread.Sleep(1100); AlertLog.MarkSeen(); Check(!AlertLog.Unread, "alertes : consulter les messages eteint le bouton");
            AlertLog.Add("FTdx10", "ALERTE ROS declenchee : ROS 3.5"); Check(AlertLog.Unread && AlertLog.All().Count == 2, "alertes : un nouveau message rallume le bouton");
            AlertLog.Clear(); Check(AlertLog.All().Count == 0 && !AlertLog.Unread, "alertes : effacer vide la liste et eteint le bouton");
        }

        // ---- alerte ROS : fin d'alerte automatique ; la puissance est rétablie sauf si « laisser la puissance réduite » est cochée
        static void RosRestoreChecks()
        {
            Check(RosGuard.ClearMs == 1500 && !RosGuard.KeepReduced, "alerte ROS : extinction après 1,5 s et puissance rétablie par défaut");
            Cfg.SetInt("ros.eteint", 100); Check(RosGuard.ClearMs == 500, "alerte ROS : extinction limitée à 0,5 s minimum"); Cfg.SetInt("ros.eteint", 999999); Check(RosGuard.ClearMs == 30000, "alerte ROS : extinction limitée à 30 s"); Cfg.SetInt("ros.eteint", 3000); Check(RosGuard.ClearMs == 3000, "alerte ROS : délai d'extinction réglable (3 s)");
            RadioDef d = Radios.ByKey("ft2000"); d.LoadUser(); Rig rig = new Rig(); VirtualLink vl = new VirtualLink(d); string err; rig.Connect(d, vl, out err);
            Wait(delegate { return rig.Get("pwr") != null && rig.Get("swr") != null; }, 8000);
            Ctl pc = RosGuard.PowerCtl(rig); string p0 = rig.Get(pc.Id); RosGuard.Silent = true; Cfg.SetBool("ros.chute", true); Cfg.SetInt("ros.pct", 50);
            for (int pass = 0; pass < 2; pass++)
            {
                bool keep = pass == 1; Cfg.SetBool("ros.garder", keep); RosGuard.Notice = null; string tag = keep ? " (puissance réduite gardée)" : " (puissance rétablie)";
                RosGuard.Test(rig); Check(RosGuard.Alarming, "alerte ROS : déclenchée" + tag);
                Wait(delegate { return rig.Get(pc.Id) != p0; }, 5000); string red = rig.Get(pc.Id); Check(red != p0 && int.Parse(red) == RosGuard.Reduced(pc, int.Parse(p0)), "alerte ROS : puissance réduite à 50 % (" + p0 + " -> " + red + ")" + tag);
                if (!keep)
                {
                    Cfg.SetInt("ros.pct", 25); RosGuard.ReapplyNow(); Wait(delegate { return rig.Get(pc.Id) != red; }, 5000); string r25 = rig.Get(pc.Id);
                    Check(r25 != red && int.Parse(r25) == RosGuard.Reduced(pc, int.Parse(p0)) && int.Parse(r25) < int.Parse(red), "alerte ROS : % de sécurité modifié en cours d'alerte, appliqué aussitôt (" + red + " -> " + r25 + ")");
                    Cfg.SetInt("ros.pct", 50); RosGuard.ReapplyNow(); Wait(delegate { return rig.Get(pc.Id) == red; }, 5000); Check(rig.Get(pc.Id) == red, "alerte ROS : % de sécurité remis à 50 % aussitôt");
                }
                Info("ROS simulé = " + RosGuard.Ratio(rig) + " swr=" + rig.Get("swr")); RosGuard.Tick(1000); Check(RosGuard.Alarming, "alerte ROS : pas éteinte tout de suite" + tag);
                RosGuard.Tick(1000 + RosGuard.ClearMs + 5); Check(!RosGuard.Alarming && RosGuard.Notice != null, "alerte ROS : éteinte seule après le délai réglé" + tag);
                if (!keep) { Wait(delegate { return rig.Get(pc.Id) == p0; }, 5000); Check(rig.Get(pc.Id) == p0 && RosGuard.Notice.Contains("rétablie"), "alerte ROS : puissance nominale rétablie (" + rig.Get(pc.Id) + ")"); }
                else { System.Threading.Thread.Sleep(600); Check(rig.Get(pc.Id) == red && RosGuard.Notice.Contains("réduite"), "alerte ROS : puissance laissée réduite comme demandé (" + rig.Get(pc.Id) + ")"); rig.Set(pc, p0); Wait(delegate { return rig.Get(pc.Id) == p0; }, 5000); }
            }
            RosGuard.Silent = false; Cfg.SetInt("ros.eteint", 1500); Cfg.SetBool("ros.garder", false); Cfg.SetBool("ros.chute", false); rig.Disconnect(); Info("Alerte ROS : extinction seule, puissance rétablie ou gardée selon la coche");
        }

        // ---- alerte ROS : conversion, déclenchement avec délai, puissance de sécurité, réglages (chaque poste a-t-il lecture du ROS / réglage de puissance ?)
        static bool rosOnce;
        static void RosChecks(RadioDef d)
        {
            if (!rosOnce)
            {
                rosOnce = true; int s = 0;
                Check(Math.Abs(RosGuard.RatioOf(0, 255) - 1.0) < 0.01 && Math.Abs(RosGuard.RatioOf(48, 255) - 1.5) < 0.01 && Math.Abs(RosGuard.RatioOf(80, 255) - 2.0) < 0.01 && Math.Abs(RosGuard.RatioOf(120, 255) - 3.0) < 0.01 && RosGuard.RatioOf(255, 255) >= 9.9, "ROS : table de conversion (1,0 / 1,5 / 2,0 / 3,0 / max)");
                Check(Math.Abs(RosGuard.RatioOf(15, 30) - RosGuard.RatioOf(127.5, 255)) < 0.001, "ROS : échelle Kenwood (0-30) ramenée à 0-255");
                Check(!RosGuard.ShouldTrigger(2.9, ref s, 1000, 3.0, 0) && s == 0, "ROS : sous le seuil, pas d'alerte");
                Check(RosGuard.ShouldTrigger(3.0, ref s, 1000, 3.0, 0), "ROS : délai 0 = déclenchement immédiat");
                s = 0; Check(!RosGuard.ShouldTrigger(3.5, ref s, 1000, 3.0, 500) && !RosGuard.ShouldTrigger(3.5, ref s, 1499, 3.0, 500) && RosGuard.ShouldTrigger(3.5, ref s, 1500, 3.0, 500), "ROS : délai 0,5 s respecté");
                s = 0; RosGuard.ShouldTrigger(3.5, ref s, 1000, 3.0, 500); RosGuard.ShouldTrigger(1.2, ref s, 1300, 3.0, 500); Check(s == 0 && !RosGuard.ShouldTrigger(3.5, ref s, 1600, 3.0, 500), "ROS : retombée sous le seuil = délai relancé");
                Check(RosGuard.Threshold == 2.0 && RosGuard.DelayMs == 300 && RosGuard.Freq == 2000 && RosGuard.Volume == 100 && RosGuard.IntervalMs == 50, "ROS : valeurs par défaut (seuil 2, délai 0,3 s, 2000 Hz, volume max, bips 50 ms)");
                Cfg.SetInt("ros.delai", 99999); Check(RosGuard.DelayMs == 10000, "ROS : délai limité à 10 s"); Cfg.SetInt("ros.delai", 50); Check(RosGuard.DelayMs == 50, "ROS : pas de 0,05 s"); Cfg.SetInt("ros.delai", 0);
                byte[] w = RosGuard.Wav(2000, 100, 120); Check(w.Length == 44 + 44100 * 120 / 1000 * 2 && w[0] == 'R', "ROS : bip 2000 Hz généré (WAV)");
            }
            double rr = -1; Ctl sw = d.Find("swr"); Ctl pc = null; foreach (Ctl x in d.C) if (x.K == Kind.Knob && x.Id == "pwr") pc = x;
            if (sw != null && sw.HasCat) Check(sw.Max > 0 && RosGuard.RatioOf(sw.Max, sw.Max) >= 9.9 && RosGuard.RatioOf(0, sw.Max) == 1.0, "ROS : échelle du poste utilisable (" + d.Key + ")");
            if (pc != null && pc.HasCat && pc.Set.Length > 0) { Cfg.SetInt("ros.pct", 50); Check(RosGuard.PowerCtl(d) == pc && RosGuard.Reduced(pc, pc.Max) == pc.Min + (int)Math.Round((pc.Max - pc.Min) / 2.0), "ROS : puissance de sécurité 50 % (" + d.Key + ")"); }
        }

        // ---- éditeur de commande : menu déroulant « Nom affiché » (face avant + menus), remplissage des champs, S-mètre (échelle S + case agrandie)
        static void PickChecks(RadioDef d)
        {
            CmdEditor.Quiet = true;
            try
            {
                List<AddDialog.Entry> en = AddDialog.Entries(d); bool anyCat = false; foreach (Ctl x in d.C) if (x.HasCat && !x.Local && !x.Hidden) { anyCat = true; break; }
                if (!anyCat) return;
                Check(en.Count > 0, "liste des commandes de l'éditeur vide : " + d.Key);
                AddDialog.Entry plain = null, sm = null; foreach (AddDialog.Entry e in en) { if (plain == null && !e.Meter && e.Set.Length > 0 && !e.Set.Contains("{")) plain = e; if (sm == null && AddDialog.IsS(e)) sm = e; }
                Ctl act = null, met = null, tg = null; foreach (Ctl x in d.C) { if (x.Local || !x.HasCat || x.Hidden) continue; if (act == null && x.K == Kind.Action) act = x; if (met == null && x.K == Kind.Meter && x.Fmt == "s") met = x; if (tg == null && x.K == Kind.Toggle && x.Set.Length > 0) tg = x; }
                // 1. un bouton : choisir une commande remplit envoi et nom
                if (act != null && plain != null)
                {
                    string os = act.Set, ol = act.Label;
                    using (CmdEditor ed = new CmdEditor(act, null, d)) { ed.Pick(plain); Check(ed.Commit(), "éditeur : commande choisie acceptée (" + d.Key + ")"); }
                    Check(act.Set == plain.Set && act.Label == plain.Name, "éditeur : choisir « " + plain.Name + " » remplit envoi et nom (" + d.Key + ")");
                    act.Set = os; act.Label = ol;
                }
                // 2. une touche (toggle) : choisir un S-mètre est refusé sans rien changer
                if (tg != null && sm != null)
                {
                    string os = tg.Set; Kind ok = tg.K;
                    using (CmdEditor ed = new CmdEditor(tg, null, d)) { ed.Pick(sm); Check(ed.Warning.Length > 0 && tg.Set == os && tg.K == ok, "éditeur : S-mètre refusé sur une touche, rien modifié (" + d.Key + ")"); }
                    tg.ResetCat();
                }
                // 3. un vu-mètre S : échelle S, case agrandie si trop petite, dans la fenêtre
                if (met != null && sm != null)
                {
                    int x0 = met.X, y0 = met.Y, w0 = met.W, h0 = met.H; string f0 = met.Fmt;
                    met.W = 100; met.H = 12;
                    using (CmdEditor ed = new CmdEditor(met, null, d)) { ed.Pick(sm); Check(ed.Commit(), "éditeur : S-mètre accepté (" + d.Key + ")"); Check(ed.Resized && met.Fmt == "s" && met.W >= 240 && met.H >= 26 && met.X >= 0 && met.X + met.W <= 1860 && met.Y + met.H <= 980, "éditeur : S-mètre en échelle S, case agrandie " + met.W + "x" + met.H + " (" + d.Key + ")"); }
                    met.X = x0; met.Y = y0; met.W = w0; met.H = h0; met.Fmt = f0; met.ResetCat();
                }
                // 4. un élément ajouté (touche) devient un vu-mètre S à la sélection d'un S-mètre
                if (sm != null)
                {
                    Ctl t = new Ctl(); t.K = Kind.Toggle; t.Custom = true; t.Id = "u99"; t.Label = t.DLabel = "essai"; t.Set = t.DSet = "ZZ{v};"; t.Read = t.DRead = "ZZ;"; t.Parse = t.DParse = "^ZZ(?<v>\\d)$"; t.OnVal = t.DOnVal = "1"; t.OffVal = t.DOffVal = "0"; t.Items = t.DItems = ""; t.X = 1800; t.Y = 900; t.W = 110; t.H = 40; t.Max = t.DMax = 1;
                    using (CmdEditor ed = new CmdEditor(t, null, d)) { ed.Pick(sm); Check(ed.Commit() && ed.KindChanged, "éditeur : élément ajouté converti en vu-mètre (" + d.Key + ")"); }
                    Check(t.K == Kind.Meter && t.Fmt == "s" && t.Set.Length == 0 && t.Read == sm.Read && t.W >= 240 && t.X + t.W <= 1860 && t.Y + t.H <= 980, "éditeur : vu-mètre S correct (lecture reprise, dans la fenêtre) (" + d.Key + ")");
                    string[] cl = null; d.AddCtl(t); try { cl = d.CustomLines(); d.SetCustomLines(cl); Ctl b = d.Find("u99"); Check(b != null && b.K == Kind.Meter && b.Fmt == "s", "éditeur : vu-mètre S enregistré et relu (" + d.Key + ")"); } finally { d.RemoveCtl(d.Find("u99") ?? t); }
                }
                // 5. le format d'affichage d'un élément d'origine est enregistré puis relu
                if (met != null)
                {
                    met.DFmt = met.Fmt; met.Fmt = "pct"; d.SaveCat(); met.Fmt = met.DFmt; d.LoadUser(); Check(met.Fmt == "pct", "format d'affichage enregistré et relu (" + d.Key + ")"); met.ResetCat(); d.SaveCat();
                }
            }
            finally { CmdEditor.Quiet = false; }
        }

        // ---- verrous (clic droit) : sur chaque élément de chaque poste, le clic droit verrouille, un verrou bloque clic / molette, un second clic droit déverrouille
        static void LockChecks(RadioDef d)
        {
            Rig rig = new Rig(); rig.Def = d; HW.NoSave = true; int n = 0;
            foreach (Ctl c in d.C)
            {
                if (c.K == Kind.Frame || c.K == Kind.Meter || c.K == Kind.Led || c.K == Kind.Text || c.K == Kind.Gauge) continue;
                HW w = Make.For(c); w.Rig = rig; w.Size = new System.Drawing.Size(Math.Max(60, c.W), Math.Max(40, c.H)); w.Locked = false;
                HW.CtrlOverride = false;
                HRow hr = w as HRow;
                if (hr != null)
                {
                    // rangée (bandes, modes) : le clic droit ne verrouille QUE la case touchée, les autres cases restent actives
                    w.SimClick(System.Windows.Forms.MouseButtons.Right); Check(hr.LockCount == 1 && hr.CellLocked(0), d.Key + " : " + c.Id + " : le clic droit ne verrouille pas la case touchée");
                    long q0 = rig.Version; w.SimClick(System.Windows.Forms.MouseButtons.Left); Check(rig.Version == q0, d.Key + " : " + c.Id + " : la case verrouillée répond encore");
                    if (hr.CellCount > 1) { w.SimClickAt(System.Windows.Forms.MouseButtons.Left, hr.CellCenter(1)); Check(rig.Version != q0 && !hr.CellLocked(1), d.Key + " : " + c.Id + " : les autres cases de la rangée ont été désactivées"); }
                    w.SimClick(System.Windows.Forms.MouseButtons.Right); Check(hr.LockCount == 0, d.Key + " : " + c.Id + " : la case ne se déverrouille pas");
                    w.Dispose(); n++; continue;
                }
                bool dr = (d.Key == "ft991a-df5dp" && (c.Id == "ctcss" || c.Id == "ctone" || c.Id == "dcsc")) || (c.K == Kind.Choice && c.Id == "step");   // sur ces trois touches : clic droit = liste, Ctrl + clic droit = verrou
                HW.CtrlOverride = dr; w.SimClick(System.Windows.Forms.MouseButtons.Right); HW.CtrlOverride = false; Check(w.Locked, d.Key + " : " + c.Id + " (" + c.K + ") : le clic droit ne verrouille pas");
                long v0 = rig.Version; w.SimClick(System.Windows.Forms.MouseButtons.Left); w.SimWheel(120); w.SimWheel(-120);
                Check(rig.Version == v0, d.Key + " : " + c.Id + " (" + c.K + ") : une commande part malgré le verrou");
                HW.CtrlOverride = dr; w.SimClick(System.Windows.Forms.MouseButtons.Right); HW.CtrlOverride = false; Check(!w.Locked, d.Key + " : " + c.Id + " : le second clic droit ne déverrouille pas"); HW.CtrlOverride = false;
                if (c.K == Kind.Toggle || w is HRow) { v0 = rig.Version; w.SimClick(System.Windows.Forms.MouseButtons.Left); Check(rig.Version != v0, d.Key + " : " + c.Id + " : déverrouillé, le clic ne fait rien"); }
                w.Dispose(); n++;
            }
            HW.NoSave = false; Info(d.Name + " : verrous vérifiés sur " + n + " éléments (clic droit, clic, molette, déverrouillage)");
        }
        // ---- serveur de commande (protocole IP) : un client v5 puis v4 pilote le poste choisi
        static string CmdAsk(NetworkStream ns, string cmd, bool v5)
        {
            ns.ReadTimeout = 6000;
            if (v5)
            {
                byte[] pl = Encoding.Unicode.GetBytes(cmd + "\0"); byte[] m = new byte[16 + pl.Length];
                BitConverter.GetBytes((uint)m.Length).CopyTo(m, 0); BitConverter.GetBytes(0x1234ABCDu).CopyTo(m, 4); BitConverter.GetBytes(0xABCD1234u).CopyTo(m, 8); Array.Copy(pl, 0, m, 16, pl.Length);
                ns.Write(m, 0, m.Length);
                byte[] h = new byte[16]; int g = 0; while (g < 16) { int r = ns.Read(h, g, 16 - g); if (r <= 0) return null; g += r; }
                if (BitConverter.ToUInt32(h, 4) != 0x1234ABCDu || BitConverter.ToUInt32(h, 8) != 0xABCD1234u) return "ENTETE?";
                int n = (int)BitConverter.ToUInt32(h, 0) - 16; byte[] b = new byte[n]; g = 0; while (g < n) { int r = ns.Read(b, g, n - g); if (r <= 0) return null; g += r; }
                return Encoding.Unicode.GetString(b).TrimEnd('\0');
            }
            byte[] t = Encoding.UTF8.GetBytes(cmd + "\r"); ns.Write(t, 0, t.Length); StringBuilder sbr = new StringBuilder(); byte[] one = new byte[1];
            while (true) { int r = ns.Read(one, 0, 1); if (r <= 0) break; if (one[0] == 13) break; sbr.Append((char)one[0]); }
            return sbr.ToString();
        }
        static void ServerTest()
        {
            string err; RadioDef d = Radios.ByKey("ftdx10"); VirtualLink vl = new VirtualLink(d); Rig rig = new Rig();
            Check(rig.Connect(d, vl, out err), "Serveur : connexion du poste simulé"); Wait(delegate { return rig.Get("fa") != null && rig.Get("md0") != null; }, 9000);
            CmdServer srv = new CmdServer(rig, 0, false); TcpClient c = new TcpClient("127.0.0.1", srv.Port); NetworkStream ns = c.GetStream();
            Check(CmdAsk(ns, "get context", true) == "1", "Serveur : get context"); Check(CmdAsk(ns, "get id", true) == "HamRadioLink", "Serveur : get id");
            Check(CmdAsk(ns, "get version", true) == MainForm.Version, "Serveur : get version"); Check(CmdAsk(ns, "get radios", true) == "1:FTdx10", "Serveur : get radios (" + CmdAsk(ns, "get radios", true) + ")");
            Check(CmdAsk(ns, "get radio", true) == "FTdx10" && CmdAsk(ns, "[1] get radio", true) == "FTdx10", "Serveur : get radio, avec et sans contexte");
            Check(CmdAsk(ns, "get vfo-count", true) == "1", "Serveur : get vfo-count");
            string btn = CmdAsk(ns, "get buttons", true); Check(btn.Contains("TX") && btn.Contains("MOX"), "Serveur : boutons (" + btn + ")");
            Check(CmdAsk(ns, "get dropdowns", true) == "Mode", "Serveur : liste Mode"); string ml = CmdAsk(ns, "get dropdown-list {Mode}", true); Check(ml.Contains("USB") && ml.Contains("LSB") && ml.Contains("DATA-U"), "Serveur : modes (" + ml + ")");
            Check(CmdAsk(ns, "get frequency", true) == "14074000", "Serveur : get frequency (" + CmdAsk(ns, "get frequency", true) + ")");
            Check(CmdAsk(ns, "set frequency-hz 7074000", true) == "OK", "Serveur : set frequency-hz"); Wait(delegate { return vl.Sim.Input("FA;") == "FA007074000;"; }, 4000); Check(vl.Sim.Input("FA;") == "FA007074000;", "Serveur : le poste reçoit 7 074 000 Hz");
            Wait(delegate { return CmdAsk(ns, "get frequency", true) == "7074000"; }, 6000); Check(CmdAsk(ns, "get frequency", true) == "7074000", "Serveur : fréquence relue");
            string f2 = CmdAsk(ns, "get frequencies", true); Check(Regex.IsMatch(f2, @"^\d+-\d+$"), "Serveur : get frequencies (" + f2 + ")");
            Check(CmdAsk(ns, "set dropdown Mode DATA-U 11", true) == "OK", "Serveur : set dropdown Mode DATA-U"); Wait(delegate { return CmdAsk(ns, "get dropdown-text {Mode}", true) == "Mode:DATA-U"; }, 6000); Check(CmdAsk(ns, "get dropdown-text {Mode}", true) == "Mode:DATA-U", "Serveur : mode relu (" + CmdAsk(ns, "get dropdown-text {Mode}", true) + ")");
            if (rig.Def.Find("md1") != null)
            {
                Check(CmdAsk(ns, "get dropdown-list Mode2", true).Contains("USB"), "Serveur : get dropdown-list Mode2");
                Check(CmdAsk(ns, "set dropdown Mode2 USB 1", true) == "OK", "Serveur : set dropdown Mode2 USB"); Wait(delegate { return CmdAsk(ns, "get dropdown-text Mode2", true) == "Mode2:USB"; }, 6000);
                Check(CmdAsk(ns, "get dropdown-text Mode2", true) == "Mode2:USB", "Serveur : mode du VFO B relu (" + CmdAsk(ns, "get dropdown-text Mode2", true) + ")");
                Check(CmdAsk(ns, "get dropdown-text {Mode}", true) == "Mode:DATA-U", "Serveur : le mode du VFO A n'a pas bougé (" + CmdAsk(ns, "get dropdown-text {Mode}", true) + ")");
            }
            Check(CmdAsk(ns, "get button-select TX", true) == "0", "Serveur : TX relâché"); Check(CmdAsk(ns, "set button-select TX 1", true) == "OK", "Serveur : set button-select TX 1"); Wait(delegate { return rig.Tx; }, 4000); Check(rig.Tx && CmdAsk(ns, "get button-select MOX", true) == "1", "Serveur : émission");
            Check(CmdAsk(ns, "set button-select TX 0", true) == "OK", "Serveur : fin d'émission"); Wait(delegate { return !rig.Tx; }, 4000); Check(!rig.Tx, "Serveur : réception");
            if (d.Find("af") != null)
            {
                Check(CmdAsk(ns, "get sliders", true) == "AF gain", "Serveur : curseur AF"); string rg = CmdAsk(ns, "get slider-range FTdx10 AF~gain", true); Check(Regex.IsMatch(rg, @"^\d+,\d+$"), "Serveur : plage AF (" + rg + ")");
                Check(CmdAsk(ns, "set slider-pos FTdx10 AF~gain 100", true) == "OK", "Serveur : set slider-pos"); Wait(delegate { return CmdAsk(ns, "get slider-pos FTdx10 AF~gain", true) == "100"; }, 6000); Check(CmdAsk(ns, "get slider-pos FTdx10 AF~gain", true) == "100", "Serveur : volume relu");
            }
            Check(CmdAsk(ns, "bogus", true) == "ERROR", "Serveur : commande inconnue → ERROR");
            Cfg.SetBool("lock.ftdx10.fa", true); Check(CmdAsk(ns, "set frequency-hz 7074000", true) == "OK", "Serveur : fréquence verrouillée : le logiciel garde la main"); Cfg.SetBool("lock.ftdx10.fa", false);
            MainForm.AppLocked = true; Check(CmdAsk(ns, "set frequency-hz 7074000", true) == "OK" && CmdAsk(ns, "get frequency", true) == "7074000", "Serveur : touche Lock : ordres du logiciel toujours acceptés"); MainForm.AppLocked = false;
            Cfg.SetBool("lock.ftdx10.ptt", true); Check(CmdAsk(ns, "set button-select TX 1", true) == "OK", "Serveur : PTT verrouillé : ordre accepté"); CmdAsk(ns, "set button-select TX 0", true); Cfg.SetBool("lock.ftdx10.ptt", false);
            c.Close(); TcpClient c4 = new TcpClient("127.0.0.1", srv.Port); NetworkStream n4 = c4.GetStream(); Check(CmdAsk(n4, "get radio", false) == "FTdx10", "Serveur : ancien protocole texte (v4)"); Check(CmdAsk(n4, "[1] get frequency", false) == "7074000", "Serveur : v4 avec contexte");
            // message v5 coupé en deux paquets
            TcpClient cf = new TcpClient("127.0.0.1", srv.Port); NetworkStream nf = cf.GetStream(); byte[] pl2 = Encoding.Unicode.GetBytes("get radio\0"); byte[] m2 = new byte[16 + pl2.Length]; BitConverter.GetBytes((uint)m2.Length).CopyTo(m2, 0); BitConverter.GetBytes(0x1234ABCDu).CopyTo(m2, 4); BitConverter.GetBytes(0xABCD1234u).CopyTo(m2, 8); Array.Copy(pl2, 0, m2, 16, pl2.Length);
            nf.Write(m2, 0, 9); nf.Flush(); Thread.Sleep(120); nf.Write(m2, 9, m2.Length - 9); nf.ReadTimeout = 4000; byte[] rbuf = new byte[64]; int rn = nf.Read(rbuf, 0, 64); Check(rn >= 16 && Encoding.Unicode.GetString(rbuf, 16, rn - 16).TrimEnd('\0') == "FTdx10", "Serveur : message coupé en deux paquets");
            Check(srv.Bad.Count == 1 && srv.Bad[0] == "bogus", "Serveur : seule la commande inconnue est signalée (" + string.Join(" | ", srv.Bad.ToArray()) + ")");
            c4.Close(); cf.Close(); srv.Dispose(); rig.Disconnect();
            // Icom (CI-V sur le fil simulé)
            d = Radios.ByKey("ic7300"); List<string> bad = new List<string>(), seen = new List<string>(); Dictionary<string, string> S = new Dictionary<string, string>(); CivSim sim = new CivSim(d, bad, seen, S);
            rig = new Rig(); Check(rig.Connect(d, new CivLink(new CivWireLink(sim, d.Civ, false), d.Civ), out err), "Serveur Icom : connexion"); Wait(delegate { return rig.Get("fa") != null && rig.Get("md0") != null; }, 9000);
            srv = new CmdServer(rig, 0, false); c = new TcpClient("127.0.0.1", srv.Port); ns = c.GetStream();
            Check(CmdAsk(ns, "get radio", true) == "IC-7300", "Serveur Icom : nom du poste (" + CmdAsk(ns, "get radio", true) + ")"); Check(CmdAsk(ns, "get frequency", true) == "14074000", "Serveur Icom : fréquence");
            Check(CmdAsk(ns, "set frequency-hz 3573000", true) == "OK", "Serveur Icom : set frequency-hz"); Wait(delegate { return S.ContainsKey("fa") && S["fa"] == "3573000"; }, 5000); Check(S.ContainsKey("fa") && S["fa"] == "3573000", "Serveur Icom : le poste reçoit 3 573 000 Hz (BCD)");
            string im = CmdAsk(ns, "get dropdown-list {Mode}", true); Check(im.Contains("USB") && im.Contains("LSB"), "Serveur Icom : modes (" + im + ")");
            Check(CmdAsk(ns, "set dropdown Mode USB 1", true) == "OK", "Serveur Icom : set dropdown Mode USB"); Wait(delegate { return S.ContainsKey("md") && S["md"] == "01"; }, 5000); Check(S.ContainsKey("md") && S["md"] == "01", "Serveur Icom : le poste passe en USB");
            Check(CmdAsk(ns, "set button-select TX 1", true) == "OK", "Serveur Icom : PTT"); Wait(delegate { return sim.Tx; }, 5000); Check(sim.Tx, "Serveur Icom : le poste émet"); CmdAsk(ns, "set button-select TX 0", true); Wait(delegate { return !sim.Tx; }, 5000); Check(!sim.Tx, "Serveur Icom : fin d'émission");
            Check(rig.Errors == 0 && bad.Count == 0, "Serveur Icom : aucune erreur (" + rig.Errors + " / " + string.Join(" ", bad.ToArray()) + ")");
            c.Close(); srv.Dispose(); rig.Disconnect();
            ServerNameTest();
            // deux postes ouverts, deux serveurs : chacun pilote son poste sans toucher à l'autre ; serveur sans poste = ERROR
            RadioDef dA = Radios.ByKey("ftdx10"), dB = Radios.ByKey("ft891"); VirtualLink vA = new VirtualLink(dA), vB = new VirtualLink(dB); Rig rA = new Rig(), rB = new Rig(); rA.Connect(dA, vA, out err); rB.Connect(dB, vB, out err);
            Wait(delegate { return rA.Get("fa") != null && rB.Get("fa") != null; }, 9000);
            CmdServer sA = new CmdServer(delegate { return rA; }, 0, false), sB = new CmdServer(delegate { return rB; }, 0, false), sN = new CmdServer(delegate { return (Rig)null; }, 0, false);
            TcpClient cA = new TcpClient("127.0.0.1", sA.Port), cB = new TcpClient("127.0.0.1", sB.Port), cN = new TcpClient("127.0.0.1", sN.Port); NetworkStream nA = cA.GetStream(), nB = cB.GetStream(), nN = cN.GetStream();
            Check(CmdAsk(nA, "get radio", true) == CmdServer.ModelName(dA) && CmdAsk(nB, "get radio", true) == CmdServer.ModelName(dB), "Deux serveurs : chacun annonce son poste (" + CmdAsk(nA, "get radio", true) + " / " + CmdAsk(nB, "get radio", true) + ")");
            long fa0 = rA.GetL("fa"); Check(CmdAsk(nB, "set frequency-hz 3573000", true) == "OK", "Deux serveurs : ordre au second poste"); Wait(delegate { return rB.GetL("fa") == 3573000; }, 6000);
            Check(rB.GetL("fa") == 3573000 && rA.GetL("fa") == fa0, "Deux serveurs : le second poste change, le premier reste à " + fa0 + " (" + rA.GetL("fa") + ")");
            Check(CmdAsk(nN, "get radio", true) == "ERROR", "Serveur sans poste ouvert : ERROR");
            cA.Close(); cB.Close(); cN.Close(); sA.Dispose(); sB.Dispose(); sN.Dispose(); rA.Disconnect(); rB.Disconnect();
            Info("Serveur de commande (protocole IP v5 et v4) : ordres d'un client traduits vers un poste texte et un poste CI-V, verrous respectés");
        }

        // ---- journal : un fichier par jour, compact (relectures identiques omises) ou détaillé, purge des vieux jours, branchement sur le moteur
        static void CatLogTest()
        {
            string dir = Path.Combine(Cfg.Dir, "logtest"); Directory.CreateDirectory(dir); DateTime now = DateTime.Now;
            foreach (int back in new int[] { 1, 5, 12 }) { string pf = Path.Combine(dir, "hamradiolink-" + now.AddDays(-back).ToString("yyyy-MM-dd") + ".log"); File.WriteAllText(pf, "x"); File.SetLastWriteTime(pf, now.AddDays(-back).AddHours(back == 1 ? 1 : 0)); }   // 24 h d archives au plus : « hier » écrit il y a moins de 24 h est gardé
            Cfg.SetInt("log.days", 3); Cfg.SetBool("log.on", true); Cfg.SetBool("log.detail", false); CatLog.Init(dir);
            CatLog.Tx("ftdx10", "FA;", true); CatLog.Tx("ftdx10", "MD01;", false); for (int i = 0; i < 3; i++) CatLog.Rx("ftdx10", "FA014074000;"); CatLog.Rx("ftdx10", "?;"); CatLog.Rx("ftdx10", "?;"); CatLog.Rx("ftdx10", "FA007074000;");
            for (int i = 0; i < 3; i++) CatLog.Server("srv", "[1] get frequency", "7074000"); CatLog.Server("srv", "[1] set frequency-hz 1", "OK"); CatLog.Server("srv", "[1] set frequency-hz 1", "OK"); CatLog.Server("srv", "get radio", "ERROR"); CatLog.Server("srv", "get radio", "ERROR"); CatLog.Close();
            string txt = File.ReadAllText(CatLog.TodayFile);
            Check(txt.Contains("MD01;") && !Regex.IsMatch(txt, @"TX\s+FA;"), "Journal compact : l'ordre est écrit, la relecture périodique non");
            Check(Regex.Matches(txt, "RX\\s+FA014074000;").Count == 1 && Regex.Matches(txt, @"RX\s+\?;").Count == 2 && txt.Contains("FA007074000;"), "Journal compact : réponse identique écrite une fois, ?; toujours écrit, changement écrit");
            Check(txt.Contains("2 réponse(s) identique(s) non écrite(s)"), "Journal compact : résumé des réponses omises");
            Check(Regex.Matches(txt, "get frequency -> 7074000").Count == 1 && Regex.Matches(txt, "set frequency-hz 1 -> OK").Count == 2 && Regex.Matches(txt, "get radio -> ERROR").Count == 2, "Journal du serveur : relecture identique une fois, ordres et erreurs toujours");
            Check(!File.Exists(Path.Combine(dir, "hamradiolink-" + now.AddDays(-5).ToString("yyyy-MM-dd") + ".log")) && !File.Exists(Path.Combine(dir, "hamradiolink-" + now.AddDays(-12).ToString("yyyy-MM-dd") + ".log")) && File.Exists(Path.Combine(dir, "hamradiolink-" + now.AddDays(-1).ToString("yyyy-MM-dd") + ".log")), "Journal : les fichiers de plus de 3 jours sont supprimés, la veille est gardée");
            Cfg.SetBool("log.detail", true); CatLog.Init(dir); CatLog.Tx("ftdx10", "FA;", true); CatLog.Close(); Check(Regex.IsMatch(File.ReadAllText(CatLog.TodayFile), @"TX\s+FA;"), "Journal détaillé : la relecture périodique est écrite");
            Cfg.SetBool("log.detail", false); CatLog.Init(dir); string err; RadioDef d = Radios.ByKey("ftdx10"); VirtualLink vl = new VirtualLink(d); Rig rig = new Rig(); rig.Connect(d, vl, out err); Wait(delegate { return rig.Get("fa") != null; }, 9000); rig.Set(d.Find("md0"), "1"); Thread.Sleep(600); rig.Disconnect(); CatLog.Close();
            txt = File.ReadAllText(CatLog.TodayFile); Check(txt.Contains("connexion ouverte") && txt.Contains("déconnexion de") && Regex.IsMatch(txt, @"ftdx10\s+RX\s+FA") && Regex.IsMatch(txt, @"ftdx10\s+TX\s+MD01;"), "Journal : le moteur écrit connexion, réponses, ordres et déconnexion");
            CatLog.On = false; try { Directory.Delete(dir, true); } catch { }
            Info("Journal des commandes : un fichier par jour, compact / détaillé, purge, branchement sur le moteur vérifiés");
        }
        // ---- partage réglé POSTE PAR POSTE : deux postes, deux paires différentes, l'un ne change pas l'autre ; reprise de l'ancien réglage global
        static void ShareCfgTest()
        {
            foreach (string n in new string[] { "on", "comOn", "com", "baud", "stop", "tcpOn", "tcpPort", "net" }) { Cfg.Set("share." + n, ""); Cfg.Set(ShareOpts.K("ic7300", n), ""); }
            Cfg.SetBool(ShareOpts.K("ft2000", "on"), true); Cfg.SetBool(ShareOpts.K("ft2000", "comOn"), true); Cfg.Set(ShareOpts.K("ft2000", "com"), "COM20");
            Cfg.SetBool(ShareOpts.K("ftdx10", "on"), true); Cfg.SetBool(ShareOpts.K("ftdx10", "comOn"), true); Cfg.Set(ShareOpts.K("ftdx10", "com"), "COM22");
            ShareOpts a = ShareOpts.Load(Framing.Text, "ft2000"), b = ShareOpts.Load(Framing.Text, "ftdx10"), c = ShareOpts.Load(Framing.Text, "ic7300");
            Check(a.Com == "COM20" && b.Com == "COM22" && !c.Any, "Partage par poste : FT-2000 sur COM20, FTdx10 sur COM22, un troisième poste sans partage");
            Cfg.Set(ShareOpts.K("ftdx10", "com"), "COM24"); Check(ShareOpts.Load(Framing.Text, "ft2000").Com == "COM20" && ShareOpts.Load(Framing.Text, "ftdx10").Com == "COM24", "Partage par poste : changer la paire d'un poste ne change pas celle de l'autre");
            Cfg.SetBool("share.mig", false); Cfg.Set("share.on", "1"); Cfg.Set("share.comOn", "1"); Cfg.Set("share.com", "COM30"); ShareOpts.MigrateGlobal("ic7300");
            Check(ShareOpts.Load(Framing.Text, "ic7300").Com == "COM30" && ShareOpts.Load(Framing.Text, "ft2000").Com == "COM20", "Partage par poste : l'ancien réglage global est repris par le poste choisi, sans toucher aux autres");
            Cfg.Set("share.com", "COM31"); ShareOpts.MigrateGlobal("ft2000"); Check(ShareOpts.Load(Framing.Text, "ft2000").Com == "COM20", "Partage par poste : la reprise de l'ancien réglage ne se fait qu'une fois");
            // une paire tenue par un autre poste est signalée « autre poste », celle tenue par le même poste « ce poste »
            List<Com0Com.Pair> all = Com0Com.Pairs(); Com0Com.Pair q = null; foreach (Com0Com.Pair p in all) if (Com0Com.IsComName(p.A) && Com0Com.IsComName(p.B) && new List<string>(Com0Com.PortsNow()).Contains(p.A) && new List<string>(Com0Com.PortsNow()).Contains(p.B)) { q = p; break; }
            if (q != null)
            {
                Dictionary<string, string> held = new Dictionary<string, string>(); held[q.A] = "FTdx10";
                foreach (Com0Com.Pair r in Com0Com.AllComPairs(null, held, "FT-2000")) if (r.A == q.A) Check(r.Status == 3 && r.Owner == "FTdx10", "Paire tenue par un autre poste : état 3 (" + r.Status + ")");
                foreach (Com0Com.Pair r in Com0Com.AllComPairs(null, held, "FTdx10")) if (r.A == q.A) Check(r.Status == 1, "Paire tenue par le même poste : état 1 (" + r.Status + ")");
            }
            Info("Partage réglé poste par poste (paires différentes, migration unique, états « ce poste / autre poste ») vérifié");
        }
        // ---- délai de sécurité TX : l'émission lancée par la touche PTT est coupée à l'échéance (2 s pour l'essai), pas une émission commencée ailleurs
        static void PttTimeoutTest()
        {
            string err; RadioDef d = Radios.ByKey("ftdx10"); VirtualLink vl = new VirtualLink(d); Rig rig = new Rig(); rig.Connect(d, vl, out err); Wait(delegate { return rig.Get("fa") != null; }, 9000);
            Ctl p = d.Find("ptt"); Cfg.SetInt("ptt.timeout.ftdx10", 2);
            rig.Set(p, p.OnSend); rig.PttButtonStart(); Wait(delegate { return rig.Tx; }, 4000); Check(rig.Tx && rig.PttLeft >= 0 && rig.PttLeft <= 2, "Délai TX : émission lancée par la touche, compte à rebours (" + rig.PttLeft + " s)");
            bool cut = false; int t0 = Environment.TickCount; while (Environment.TickCount - t0 < 8000 && !cut) { cut = rig.PttCheck(); Thread.Sleep(100); }
            Wait(delegate { return !vl.Sim.Tx; }, 4000); Check(cut && !vl.Sim.Tx, "Délai TX : l'émission est coupée à l'échéance (coupée=" + cut + ")");
            Check(!rig.PttByButton, "Délai TX : le suivi s'arrête après la coupure");
            // émission lancée ailleurs (pas par la touche) : jamais coupée
            rig.Set(p, p.OnSend); Wait(delegate { return rig.Tx; }, 4000); Thread.Sleep(2600); bool cut2 = rig.PttCheck(); Check(!cut2 && rig.Tx, "Délai TX : une émission non lancée par la touche n'est pas coupée");
            rig.Set(p, p.OffSend); Wait(delegate { return !rig.Tx; }, 4000);
            // délai désactivé : pas de coupure
            Cfg.SetInt("ptt.timeout.ftdx10", 0); rig.Set(p, p.OnSend); rig.PttButtonStart(); Wait(delegate { return rig.Tx; }, 4000); Thread.Sleep(2500); Check(!rig.PttCheck() && rig.Tx && rig.PttLeft == -1, "Délai TX : désactivé, aucune coupure");
            rig.Set(p, p.OffSend); Wait(delegate { return !rig.Tx; }, 4000); Check(!rig.PttCheck() && !rig.PttByButton, "Délai TX : fin d'émission détectée, suivi arrêté");
            Cfg.SetInt("ptt.timeout.ftdx10", 0); rig.Disconnect(); Info("Délai de sécurité TX de la touche PTT (compte à rebours, coupure, désactivé, émission externe) vérifié");
        }
        // ---- noms de poste avec espaces (curseurs), liste des modes sous « Main Mode », réglages lus et écrits en même temps
        static void ServerNameTest()
        {
            string err; RadioDef d = Radios.ByKey("ft2000"); VirtualLink vl = new VirtualLink(d); Rig rig = new Rig(); rig.Connect(d, vl, out err); Wait(delegate { return rig.Get("fa") != null && rig.Get("md0") != null; }, 9000);
            CmdServer srv = new CmdServer(rig, 0, false); TcpClient c = new TcpClient("127.0.0.1", srv.Port); NetworkStream ns = c.GetStream(); string nm = CmdAsk(ns, "get radio", true);
            Check(nm.Contains(" ") && nm.Contains("/"), "Serveur : nom de poste avec espaces (" + nm + ")");
            if (d.Find("af") != null)
            {
                Check(Regex.IsMatch(CmdAsk(ns, "[1] get slider-range " + nm + " AF~gain", true), @"^\d+,\d+$"), "Serveur : curseur AF lu avec un nom de poste à espaces");
                Check(CmdAsk(ns, "[1] set slider-pos " + nm + " AF~gain 120", true) == "OK", "Serveur : curseur AF réglé avec un nom de poste à espaces"); Wait(delegate { return rig.GetL("af") == 120; }, 5000);
                Check(CmdAsk(ns, "[1] get slider-pos " + nm + " AF~gain", true) == "120", "Serveur : curseur AF relu (" + CmdAsk(ns, "[1] get slider-pos " + nm + " AF~gain", true) + ")");
            }
            Check(CmdAsk(ns, "get dropdown-text {Main Mode}", true).StartsWith("Mode:") && CmdAsk(ns, "get dropdown-list {Main Mode}", true).Contains("USB") && CmdAsk(ns, "get dropdown-text {Mode A}", true).StartsWith("Mode:"), "Serveur : liste des modes aussi sous « Main Mode » et « Mode A »");
            c.Close(); srv.Dispose(); rig.Disconnect();
            Exception ex = null; bool stop = false;
            Thread w = new Thread(delegate() { int i = 0; try { while (!stop) { Cfg.SetInt("race.k" + (i++ % 300), i); Cfg.Set("race.s" + (i % 50), "x"); } } catch (Exception e) { ex = e; } });
            Thread r = new Thread(delegate() { int i = 0; try { while (!stop) { Cfg.GetInt("race.k" + (i++ % 300), 0); Cfg.GetBool("lock.ft2000.fa", false); Cfg.Get("race.s" + (i % 50), ""); } } catch (Exception e) { ex = e; } });
            w.Start(); r.Start(); Thread.Sleep(900); stop = true; w.Join(); r.Join(); Check(ex == null, "Réglages : lecture et écriture simultanées sans erreur (" + (ex != null ? ex.Message : "ok") + ")");
        }
        // ---- com0com : lecture seule (registre, liste des ports) ; rien n'est installé ni ouvert
        static void Com0ComTest()
        {
            Check(Com0Com.IsComName("COM20") && !Com0Com.IsComName("HUB14") && !Com0Com.IsComName("CNCA0") && !Com0Com.IsComName("COM"), "com0com : noms de ports reconnus");
            foreach (Com0Com.Pair p in Com0Com.Usable(null, false)) Check(Com0Com.IsComName(p.A) && Com0Com.IsComName(p.B) && !p.A.Equals(p.B, StringComparison.OrdinalIgnoreCase), "com0com : paire utilisable valide " + p);
            int[] n = Com0Com.FreeNumbers(); Check(n[1] == n[0] + 1 && n[0] >= 20, "com0com : deux numéros libres consécutifs (COM" + n[0] + " / COM" + n[1] + ")");
            string used = null; foreach (string s in Com0Com.PortsNow()) { used = s; break; } if (used != null) foreach (Com0Com.Pair p in Com0Com.Usable(used, false)) Check(!p.A.Equals(used, StringComparison.OrdinalIgnoreCase) && !p.B.Equals(used, StringComparison.OrdinalIgnoreCase), "com0com : le port du poste (" + used + ") n'est jamais proposé");
            Info("com0com : détection, paires utilisables, numéros libres vérifiés (lecture seule : aucune installation ni ouverture de port)");
        }
        // ---- partage du poste : un client externe (TCP) pilote le même poste que HamRadioLink, sans rien perdre ni voler de réponses
        static string RdX(NetworkStream ns, int ms, Func<string, bool> done)
        {
            StringBuilder s = new StringBuilder(); int t = Environment.TickCount; byte[] b = new byte[512];
            while (Environment.TickCount - t < ms) { if (ns.DataAvailable) { int n = ns.Read(b, 0, b.Length); s.Append(Civ.L1.GetString(b, 0, n)); if (done != null && done(s.ToString())) break; } else Thread.Sleep(10); }
            return s.ToString();
        }
        static void WrX(NetworkStream ns, string s) { byte[] b = Civ.L1.GetBytes(s); ns.Write(b, 0, b.Length); ns.Flush(); }
        static bool EndSemi(string s) { return s.EndsWith(";"); }
        static bool EndFd(string s) { return s.EndsWith("ý"); }
        static bool Has5(string s) { return s.Length >= 5; }
        static bool Has1(string s) { return s.Length >= 1; }
        static void ShareTest()
        {
            string err; ShareOpts so;
            // --- Yaesu / Kenwood (texte)
            RadioDef d = Radios.ByKey("ftdx10"); VirtualLink vl = new VirtualLink(d); so = new ShareOpts(); so.Fr = Framing.Text; so.TcpPort = -1; so.LearnReads(d);
            ShareLink sl = new ShareLink(vl, so); Rig rig = new Rig();
            Check(rig.Connect(d, sl, out err), "Partage texte : connexion (" + err + ")"); Wait(delegate { return rig.Get("fa") != null && rig.Get("md0") != null; }, 9000);
            Check(sl.TcpActual > 0, "Partage texte : serveur TCP ouvert"); TcpClient c = new TcpClient("127.0.0.1", sl.TcpActual); NetworkStream ns = c.GetStream();
            string r = RdX(ns, 1500, null); Check(r.Length == 0, "Partage texte : le client ne reçoit pas les réponses destinées à HamRadioLink (" + r + ")");
            WrX(ns, "ID;"); r = RdX(ns, 3000, EndSemi); Check(r == d.IdAnswer, "Partage texte : le client lit l'identifiant (" + r + ")");
            WrX(ns, "FA;"); r = RdX(ns, 3000, EndSemi); Check(r == "FA014074000;", "Partage texte : le client lit la fréquence (" + r + ")");
            WrX(ns, "FA007074000;"); Wait(delegate { return rig.GetL("fa") == 7074000; }, 6000); Check(rig.GetL("fa") == 7074000, "Partage texte : la fréquence réglée par le client apparaît dans HamRadioLink (" + rig.Get("fa") + ")");
            rig.Set(d.Find("md0"), "1"); Wait(delegate { return vl.Sim.Input("MD0;") == "MD01;"; }, 4000);
            WrX(ns, "MD0;"); r = RdX(ns, 3000, EndSemi); Check(r == "MD01;", "Partage texte : le mode réglé par HamRadioLink est lu par le client (" + r + ")");
            for (int i = 0; i < 20; i++) { WrX(ns, "FA;"); r = RdX(ns, 3000, EndSemi); if (r != "FA007074000;") { Check(false, "Partage texte : lecture " + i + " du client = " + r); break; } }
            WrX(ns, "FA0070"); Thread.Sleep(50); WrX(ns, "75000;"); Thread.Sleep(300); WrX(ns, "FA;"); r = RdX(ns, 3000, EndSemi); Check(r == "FA007075000;", "Partage texte : commande du client coupée en deux (" + r + ")");
            Check(rig.Errors == 0 && vl.Sim.Bad.Count == 0 && sl.ExtCommands >= 23 && sl.Clients == 1, "Partage texte : aucune erreur (" + rig.Errors + " / " + string.Join(" ", vl.Sim.Bad.ToArray()) + " / " + sl.ExtCommands + " cmd client)");
            c.Close(); rig.Disconnect(); Info("Partage texte (Yaesu / Kenwood) : client TCP et HamRadioLink pilotent le même poste simulé, réponses bien réparties");

            // --- Icom (CI-V sur le fil)
            d = Radios.ByKey("ic7300"); List<string> bad = new List<string>(), seen = new List<string>(); Dictionary<string, string> S = new Dictionary<string, string>(); CivSim sim = new CivSim(d, bad, seen, S);
            so = new ShareOpts(); so.Fr = Framing.Civ; so.TcpPort = -1; sl = new ShareLink(new CivWireLink(sim, d.Civ, false), so); CivLink cl = new CivLink(sl, d.Civ); rig = new Rig();
            Check(rig.Connect(d, cl, out err), "Partage CI-V : connexion (" + err + ")"); Wait(delegate { return rig.Get("fa") != null && rig.Get("md0") != null; }, 9000);
            c = new TcpClient("127.0.0.1", sl.TcpActual); ns = c.GetStream(); r = RdX(ns, 1500, null); Check(r.Length == 0, "Partage CI-V : le client ne reçoit pas les trames de HamRadioLink (" + r.Length + " octets)");
            WrX(ns, Civ.Frame(d.Civ, Civ.Ctrl, "03")); r = RdX(ns, 3000, EndFd);
            Check(r.Length == 11 && r[2] == (char)Civ.Ctrl && r[3] == (char)d.Civ && r[4] == '\u0003' && r[5] == '\u0000' && r[6] == '@' && r[7] == '\u0007' && r[8] == '\u0014', "Partage CI-V : le client lit la fréquence 14 074 000 Hz (trame de " + r.Length + " octets)");
            WrX(ns, Civ.Frame(d.Civ, Civ.Ctrl, Civ.Swap("050007100000"))); Wait(delegate { return S.ContainsKey("fa") && S["fa"] == "7100000"; }, 4000); Check(S.ContainsKey("fa") && S["fa"] == "7100000", "Partage CI-V : fréquence envoyée par le client (" + (S.ContainsKey("fa") ? S["fa"] : "?") + ")");
            r = RdX(ns, 3000, EndFd); Check(r.Length == 6 && r[4] == 'û', "Partage CI-V : le client reçoit l'accusé FB de son réglage (" + r.Length + " octets)");
            Wait(delegate { return rig.GetL("fa") == 7100000; }, 8000); Check(rig.GetL("fa") == 7100000, "Partage CI-V : HamRadioLink voit la fréquence réglée par le client (" + rig.Get("fa") + ")");
            for (int i = 0; i < 10; i++) { WrX(ns, Civ.Frame(d.Civ, Civ.Ctrl, "03")); r = RdX(ns, 3000, EndFd); if (r.Length != 11 || r[6] != '\u0000' || r[7] != '\u0010') { Check(false, "Partage CI-V : lecture " + i + " du client incorrecte (" + r.Length + ")"); break; } }
            Check(rig.Errors == 0 && bad.Count == 0, "Partage CI-V : aucune erreur (" + rig.Errors + " / " + string.Join(" ", bad.ToArray()) + ")");
            c.Close(); rig.Disconnect(); Info("Partage CI-V (Icom / Xiegu) : trames du client et de HamRadioLink réparties sans perte");

            // --- Yaesu 5 octets
            d = Radios.ByKey("ft817"); vl = new VirtualLink(d); so = new ShareOpts(); so.Fr = Framing.Fixed5; so.TcpPort = -1; sl = new ShareLink(new Y5WireLink(vl.Sim), so); Y5Link yl = new Y5Link(sl); rig = new Rig();
            Check(rig.Connect(d, yl, out err), "Partage 5 octets : connexion (" + err + ")"); Wait(delegate { return rig.Get("fa") != null && rig.Get("md0") != null; }, 9000);
            c = new TcpClient("127.0.0.1", sl.TcpActual); ns = c.GetStream(); r = RdX(ns, 1500, null); Check(r.Length == 0, "Partage 5 octets : le client ne reçoit rien d'inattendu (" + r.Length + ")");
            bool good = true; for (int i = 0; i < 15; i++) { WrX(ns, Y5.Frame("00000000", 0x03)); r = RdX(ns, 3000, Has5); if (r != Y5.Hex("01407400") + "\u0001") { good = false; Check(false, "Partage 5 octets : lecture " + i + " du client incorrecte (" + r.Length + " octets)"); break; } }
            Check(good, "Partage 5 octets : 15 lectures de fréquence du client pendant que HamRadioLink interroge le poste");
            WrX(ns, Y5.BcdFreq(7100000) + (char)0x01); Wait(delegate { return vl.Sim.Input("FA;") == "FA007100000;"; }, 4000); Check(vl.Sim.Input("FA;") == "FA007100000;", "Partage 5 octets : fréquence envoyée par le client");
            Wait(delegate { return rig.GetL("fa") == 7100000; }, 8000); Check(rig.GetL("fa") == 7100000, "Partage 5 octets : HamRadioLink voit la fréquence réglée par le client (" + rig.Get("fa") + ")");
            WrX(ns, Y5.Frame("00000000", 0xE7)); r = RdX(ns, 3000, Has1); Check(r.Length == 1, "Partage 5 octets : le client lit le S-mètre (1 octet)");
            Check(rig.Errors == 0 && vl.Sim.Bad.Count == 0 && rig.GetL("sm") == 120, "Partage 5 octets : aucune erreur, S-mètre de HamRadioLink intact (" + rig.Errors + " / " + string.Join(" ", vl.Sim.Bad.ToArray()) + " / " + rig.Get("sm") + ")");
            c.Close(); rig.Disconnect(); Info("Partage 5 octets (FT-817 et famille) : client et HamRadioLink pilotent le même poste simulé");
        }
    }
}
