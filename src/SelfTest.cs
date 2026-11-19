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

        public static int Run(string report)
        {
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
                if (Environment.GetEnvironmentVariable("HRL_ONLY") == "lock") { foreach (RadioDef d in Radios.All()) LockChecks(d); throw new OperationCanceledException(); }
                if (Environment.GetEnvironmentVariable("HRL_ONLY") == "tci") { RadioDef t = Radios.ByKey("tci"); Definitions(t); Roundtrip(t); TciChecks(t); Persistence(t); throw new OperationCanceledException(); }
                if (Environment.GetEnvironmentVariable("HRL_ONLY") == "leg") { foreach (RadioDef d in Radios.All()) if (d.Leg != null) { foreach (string nt in d.Notes) sb.AppendLine("NOTE " + d.Key + " : " + nt); Definitions(d); Roundtrip(d); LegChecks(d); Persistence(d); LockChecks(d); } throw new OperationCanceledException(); }
                Templates(); CivCodec(); Y5Codec(); int nCiv = 0;
                foreach (RadioDef d in Radios.All()) { foreach (string nt in d.Notes) sb.AppendLine("NOTE " + d.Key + " : " + nt); Definitions(d); Roundtrip(d); if (d.Civ >= 0) CivChecks(d, ++nCiv); else if (d.Bin) Y5Checks(d); else if (d.Leg != null) LegChecks(d); else if (d.Tci) TciChecks(d); else { StateMapping(d); EndToEnd(d); } Persistence(d); LockChecks(d); }
                CustomTest(); Strictness();
                Editors();
                DeadReads(); MenuBackup();
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
            StringBuilder o = new StringBuilder(); o.AppendLine("HamRadioLink - commandes CAT par défaut de chaque élément (celles que le clic droit en config vue permet de modifier)"); o.AppendLine();
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
                Check(c.X >= 0 && c.Y >= 118 && c.X + c.W <= 1860 && c.Y + c.H <= 980, d.Key + " : " + c.Id + " hors de la fenêtre (" + c.X + "," + c.Y + "," + c.W + "," + c.H + ")");
                if (c.Set.Length > 0) Check(Tpl.Check(c.Set) == null, d.Key + " : gabarit invalide " + c.Id);
                if (c.Parse.Length > 0) { try { new Regex(c.Parse); } catch { Check(false, d.Key + " : regex invalide " + c.Id); } Check(c.Read.Length > 0 || (d.Catalog == "ts890" && c.Id == "ptt"), d.Key + " : " + c.Id + " a une réponse mais pas de lecture"); }
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
                    Check(!ov, d.Key + " : " + a.Id + " et " + b.Id + " se chevauchent");
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
            Wait(delegate { return rig.Connected && rig.Get("fa") != null && rig.Get("af") != null; }, 6000);
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
            Wait(delegate { return r2.Connected && r2.Get("fa") != null && r2.Get("md0") != null; }, 9000);
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
            rig.Disconnect(); Info("Export / import par parties : menu EXnnn lu, restauré, parties choisies");
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
                w.SimClick(System.Windows.Forms.MouseButtons.Right); Check(w.Locked, d.Key + " : " + c.Id + " (" + c.K + ") : le clic droit ne verrouille pas");
                long v0 = rig.Version; w.SimClick(System.Windows.Forms.MouseButtons.Left); w.SimWheel(120); w.SimWheel(-120);
                Check(rig.Version == v0, d.Key + " : " + c.Id + " (" + c.K + ") : une commande part malgré le verrou");
                w.SimClick(System.Windows.Forms.MouseButtons.Right); Check(!w.Locked, d.Key + " : " + c.Id + " : le second clic droit ne déverrouille pas"); HW.CtrlOverride = false;
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
            foreach (int back in new int[] { 1, 5, 12 }) File.WriteAllText(Path.Combine(dir, "hamradiolink-" + now.AddDays(-back).ToString("yyyy-MM-dd") + ".log"), "x");
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
            foreach (string n in new string[] { "on", "comOn", "com", "baud", "stop", "tcpOn", "tcpPort", "net" }) { Cfg.Set("share." + n, ""); }
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
