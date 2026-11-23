using System;
using System.Collections.Generic;

namespace HamRadioLink
{
    public static partial class Radios
    {
        // Postes Yaesu « famille FTdx10 » : la face avant du FTdx10 sert de modèle ; chaque élément dont une commande n'existe pas
        // (ou n'a pas le bon format) dans le catalogue officiel du poste est retiré. Le nombre de chiffres de la fréquence et les
        // modes sont adaptés. Rien n'est essayé sur un vrai poste : tout se corrige par clic droit en « vue config ».
        const string MODES_NEW = "LSB=1,USB=2,CW-U=3,FM=4,AM=5,RTTY-L=6,CW-L=7,DATA-L=8,RTTY-U=9,DATA-FM=A,FM-N=B,DATA-U=C,AM-N=D,PSK=E,DATA-FM-N=F";
        const string MODES_991 = "LSB=1,USB=2,CW-U=3,FM=4,AM=5,RTTY-L=6,CW-L=7,DATA-L=8,RTTY-U=9,DATA-FM=A,FM-N=B,DATA-U=C,AM-N=D,C4FM=E";   // FT-991A : E = C4FM, pas de F
        const string MODES_891 = "LSB=1,USB=2,CW-U=3,FM=4,AM=5,RTTY-L=6,CW-L=7,DATA-L=8,RTTY-U=9,FM-N=B,DATA-U=C,AM-N=D";   // FT-891 : pas de A, E ni F
        const string MODES_450 = "LSB=1,USB=2,CW=3,FM=4,AM=5,DATA-L=6,CW-R=7,USER-L=8,DATA-U=9,USER-U=C";   // FT-450D : les codes A (DATA-FM) et D (AM-N) n'existent pas
        const string MODES_1200 = "LSB=1,USB=2,CW=3,FM=4,AM=5,RTTY-L=6,CW-R=7,DATA-L=8,RTTY-U=9,FM-N=B,DATA-U=C";   // FTdx1200 : pas de A ni D
        const string MODES_OLD = "LSB=1,USB=2,CW=3,FM=4,AM=5,RTTY-L=6,CW-R=7,DATA-L=8,RTTY-U=9,DATA-FM=A,FM-N=B,DATA-U=C,AM-N=D";

        static string Sample(Ctl c)
        {
            switch (c.K)
            {
                case Kind.Freq: return "14074000";
                case Kind.Knob: return Math.Max(c.Min, 1).ToString();
                case Kind.Toggle: return c.OnSend;
                case Kind.Choice: { Item[] l = c.ItemList; return l.Length > 0 ? l[0].Code : "0"; }
            }
            return "0";
        }
        static bool Conforms(CatalogSim cs, Ctl c)
        {
            if (c.Set.Length > 0)
                foreach (string s in Tpl.Steps(Tpl.Fill(c.Set, Sample(c)))) { if (s.Length == 0 || s[0] == (char)0) continue; foreach (string one in s.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)) if (!cs.Valid(one)) return false; }
            if (c.Read.Length > 0) foreach (string one in c.Read.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)) if (!cs.Valid(one)) return false;
            return true;
        }

        // Corrections propres à chaque poste, d'après les catalogues des manuels CAT (relecture : valeurs, bornes et tables que le contrôle de syntaxe ne voit pas)
        static void FixYaesu(RadioDef r, string cat, string key)
        {
            Action<string> drop = delegate(string id) { Ctl x = r.Find(id); if (x != null) { r.C.Remove(x); r.Notes.Add(id + " : élément retiré (commande absente ou différente dans le manuel CAT de ce poste)"); } };
            bool old3 = cat == "ft450" || cat == "ft950" || cat == "ftdx3000", cat5 = cat == "ft450" || cat == "ft891" || cat == "ft950" || cat == "ftdx3000";
            if (old3 || cat == "ft891" || cat == "ft991a") { drop("md1"); Ctl fbq = r.Find("fb"); if (fbq != null) { fbq.Ref = "md0"; fbq.Hint = "Le mode du VFO-B n'est pas lisible par CAT sur ce poste (MD1 absent du manuel) : le mode affiché est celui du VFO-A."; } }                           // MD1 n'existe pas : MD P1 = 0 (MAIN) seulement
            if (cat == "ft450" || cat == "ft891" || cat == "ft991a") { Ctl a = r.Find("att"); if (a != null) { a.Items = "OFF=0,ON=1"; a.Parse = @"^RA0(?<v>[01]);$"; a.Hint = "RA P2 = 0 OFF / 1 ON (catalogue du manuel)."; } }
            if (cat == "ft450" || cat == "ft891") { Ctl p = r.Find("ipo"); if (p != null) { p.Items = "IPO=0,AMP=1"; p.Parse = @"^PA0(?<v>[01]);$"; p.Hint = "PA P2 = 0 / 1 (catalogue du manuel) : le sens exact est à confirmer sur le poste."; } }
            if (cat == "ftdx101") { drop("meter"); Ctl w = r.Find("width"); if (w != null) w.Max = 21; Ctl rf = r.Find("rfil"); if (rf != null) rf.Items = "12 kHz=1|6,3 kHz=2|7,1.2 kHz=3|8,600 Hz=4|9,300 Hz=5|A"; }
            if (cat == "ft450") { drop("monil"); Ctl dl = r.Find("dnrl"); if (dl != null) dl.Max = 11; }
            if (cat5) { Ctl bd = r.Find("band"); if (bd != null) bd.Items = bd.Items.Replace("5=02,", ""); }   // pas de bande 5 MHz (02 : invalide)
            if (cat == "ft891") { Ctl rg = r.Find("rf"); if (rg != null) rg.Max = 30; }
            if (cat == "ft950" || cat == "ftdx3000")
            {
                Ctl rf = r.Find("rfil"); if (rf != null) { rf.Items = "AUTO=0|4|5|6,15 kHz=1,6 kHz=2,3 kHz=3"; rf.Parse = @"^RF0(?<v>[0-6]);$"; rf.Hint = "RF P2 = 0 AUTO, 1 15 kHz, 2 6 kHz, 3 3 kHz (réponses 4 à 6 : AUTO 15 / 6 / 3 kHz)."; }
                Ctl sq = r.Find("sql"); if (sq != null) sq.Max = 255; Ctl nl = r.Find("nbl"); if (nl != null) nl.Max = cat == "ft950" ? 255 : 100;
            }
            if (cat == "ft950") { Ctl mg = r.Find("mic"); if (mg != null) mg.Max = 255; Ctl kp = r.Find("kp"); if (kp != null) kp.Max = 15; }
            // d'après les manuels CAT officiels (relecture n° 2)
            if (cat == "ft450")
            {
                Ctl sp = r.Find("split"); if (sp != null) { sp.Set = "FT{v};"; sp.Read = "FT;"; sp.Parse = @"^FT(?<v>[01]);$"; sp.OnVal = "1"; sp.OffVal = "0"; sp.Hint = "FT-450D : ST est le pas de syntonisation ; le split se fait par FT (1 / 0)."; }
                Ctl ag = r.Find("agc"); if (ag != null) ag.Items = "AUTO=4|5|6,FAST=1,SLOW=2|3,OFF=0";
                foreach (string k in new[] { "ky1", "ky2", "ky3", "ky4", "ky5", "nbl", "proc", "procl" }) drop(k);   // KY 1-5, NL et PR / PL n'existent pas (KY6-8 seulement)
                Ctl kpp = r.Find("kp"); if (kpp != null) { kpp.Min = 2; kpp.Max = 10; kpp.Step = 2; }
            }
            if (cat == "ft450" || cat == "ft950" || cat == "ftdx3000")   // MS : un seul chiffre (pas de P2), sans PO sauf FT-450D
            {
                Ctl ms = r.Find("meter"); if (ms != null) { ms.Set = "MS{v};"; ms.Read = "MS;"; ms.Parse = @"^MS(?<v>[0-5]);$"; ms.Items = cat == "ft450" ? "ALC=1,PO=2,SWR=3" : "COMP=0,ALC=1,SWR=3,ID=4,VDD=5"; }
                foreach (string k in new[] { "vd", "sd" }) { Ctl dl = r.Find(k); if (dl != null) { dl.Set = dl.Set.Replace("{v:2}", "{v:4}"); dl.Parse = @"^" + k.ToUpperInvariant() + @"(?<v>\d{4});$"; dl.Min = cat == "ft450" && k == "vd" ? 100 : 30; dl.Max = 3000; dl.Step = cat == "ft450" && k == "vd" ? 100 : 10; dl.Fmt = "ms"; dl.Hint = "4 chiffres en ms (manuel CAT du poste)."; } }
            }
            if (cat == "ft991a" || cat == "ft891")   // manuels CAT : IS, SH, MS, SD, VD (relecture n° 2)
            {
                Ctl sh = r.Find("shift"); if (sh != null) { if (cat == "ft991a") { sh.Set = "IS0{v:+4};"; sh.Parse = @"^IS0(?<v>[+-]\d{4});$"; } else { sh.Set = "IS01{v:+4};"; sh.Parse = @"^IS0[01](?<v>[+-]\d{4});$"; } }
                Ctl wd = r.Find("width"); if (wd != null) { wd.Max = 21; if (cat == "ft991a") { wd.Set = "SH0{v:2};"; wd.Parse = @"^SH0(?<v>\d{2});$"; } }
                Ctl ms = r.Find("meter"); if (ms != null) { ms.Set = "MS{v};"; ms.Read = "MS;"; ms.Parse = @"^MS(?<v>[0-5]);$"; ms.Items = cat == "ft991a" ? "COMP=0,ALC=1,PO=2,SWR=3,ID=4,VDD=5" : "COMP=0,ALC=1,PO=2,SWR=3,ID=4"; }
                foreach (string k in new[] { "vd", "sd" }) { Ctl dl = r.Find(k); if (dl != null) { dl.Set = dl.Set.Replace("{v:2}", "{v:4}"); dl.Parse = @"^" + k.ToUpperInvariant() + @"(?<v>\d{4});$"; dl.Min = 30; dl.Max = 3000; dl.Step = 10; dl.Fmt = "ms"; dl.Hint = "4 chiffres en ms, 0030 à 3000 (manuel CAT du poste)."; } }
            }
            if (cat == "ft991a") Extra991(r);
            if (cat == "ft710") { Ctl pl = r.Find("procl"); if (pl != null) pl.Min = 1; }
            if (cat == "ft950") { Ctl pr = r.Find("proc"); if (pr != null) { pr.Set = "PR{v};"; pr.Read = "PR;"; pr.Parse = @"^PR(?<v>[0-2]);$"; } Ctl ml = r.Find("monil"); if (ml != null) { ml.Min = 1; ml.Max = 255; } }
            if (cat == "ftdx3000")
            {
                Ctl rf = r.Find("rfil");
                if (rf != null) { if (key == "ftdx1200") { rf.Items = "AUTO=0|4|5|6,15 kHz=1,6 kHz=2,3 kHz=3"; rf.Parse = @"^RF0(?<v>[0-6]);$"; } else { rf.Items = "AUTO=0|4|5|6|9|A,15 kHz=1,6 kHz=2,3 kHz=3,600 Hz=4|7,300 Hz=5|8"; rf.Parse = @"^RF0(?<v>[0-9A]);$"; } }
                Ctl ml = r.Find("monil"); if (ml != null) { ml.Min = 1; ml.Max = 100; }
            }
        }

        // FT-991A (manuel CAT 1711-D) : bandes 144 / 430 MHz (BS 15 / 16), DT GAIN (menu 073), égaliseur micro (PR1), CTCSS / DCS (CT, CN), AMS du C4FM (menu 090)
        // Le C4FM n'a pas d'autre commande CAT (ni GM ni X) : l'écran propre à ce mode n'est pas reproduit.
        static void Extra991(RadioDef r)
        {
            Ctl bd = r.Find("band"); if (bd != null && !bd.Items.Contains("144=")) { bd.Items += ",144=15,430=16"; bd.Hint = "Manuel : BS P1 = 00 (1,8 MHz) à 11 (GEN), 15 = 144 MHz, 16 = 430 MHz. Pas de lecture : la bande allumée vient de la fréquence."; }
            Bld b = new Bld(r); Ctl c;
            c = b.Knob("dtg", "DT GAIN", 660, 902, 94, 66, 0, 100, "EX073{v:3};", "EX073;", @"^EX073(?<v>\d{3});$", "raw"); c.Accent = "amber"; c.Hint = "DT GAIN : niveau du signal envoyé en modes DATA (menu 073 DATA OUT LEVEL, 0 à 100). Sert à régler l'entrée audio des modes numériques en DATA-USB / DATA-LSB.";
            string tones = "67.0 69.3 71.9 74.4 77.0 79.7 82.5 85.4 88.5 91.5 94.8 97.4 100.0 103.5 107.2 110.9 114.8 118.8 123.0 127.3 131.8 136.5 141.3 146.2 151.4 156.7 159.8 162.2 165.5 167.9 171.3 173.8 177.3 179.9 183.5 186.2 189.9 192.8 196.6 199.5 203.5 206.5 210.7 218.1 225.7 229.1 233.6 241.8 250.3 254.1";
            string dcs = "023 025 026 031 032 036 043 047 051 053 054 065 071 072 073 074 114 115 116 122 125 131 132 134 143 145 152 155 156 162 165 172 174 205 212 223 225 226 243 244 245 246 251 252 255 261 263 265 266 271 274 306 311 315 325 331 332 343 346 351 356 364 365 371 411 412 413 423 431 432 445 446 452 454 455 462 464 465 466 503 506 516 523 526 532 546 565 606 612 624 627 631 632 654 662 664 703 712 723 731 732 734 743 754";
            System.Text.StringBuilder t1 = new System.Text.StringBuilder(), t2 = new System.Text.StringBuilder(); int i = 0;
            foreach (string s in tones.Split(' ')) t1.Append(i > 0 ? "," : "").Append(s).Append(" Hz=").Append((i++).ToString("000"));
            i = 0; foreach (string s in dcs.Split(' ')) t2.Append(i > 0 ? "," : "").Append("DCS ").Append(s).Append("=").Append((i++).ToString("000"));
            c = b.Cho("ctcss", "CTCSS / DCS", 1414, 856, 200, 60, "ARRÊT=0,CTCSS ENC/DEC=1,CTCSS ENC=2,DCS ENC/DEC=3,DCS ENC=4", "CT0{v};", "CT0;", @"^CT0(?<v>[0-4]);$"); c.Hint = "CT P1 P2 : sous-porteuse CTCSS ou code DCS (modes FM). ENC = émission seulement, ENC/DEC = émission et squelch à la réception.";
            c = b.Cho("ctone", "FRÉQ. CTCSS", 1620, 856, 200, 60, t1.ToString(), "CN00{v};", "CN00;", @"^CN00(?<v>\d{3});$"); c.Hint = "CN P2 = 0 : numéro de la fréquence CTCSS (tableau 1 du manuel, 67,0 à 254,1 Hz).";
            c = b.Cho("dcsc", "CODE DCS", 1414, 924, 200, 40, t2.ToString(), "CN01{v};", "CN01;", @"^CN01(?<v>\d{3});$"); c.Hint = "CN P2 = 1 : numéro du code DCS (tableau 2 du manuel, 023 à 754).";
            c = b.Cho("ams", "C4FM TX (AMS)", 1620, 924, 200, 40, "AUTO (AMS)=0,MANUEL=1,DN voix+données=2,VW voix large=3,ANALOGIQUE=4", "EX090{v};", "EX090;", @"^EX090(?<v>[0-4]);$"); c.Hint = "Menu 090 AMS TX MODE : choix du mode d'émission C4FM. Le manuel CAT n'offre rien d'autre pour le C4FM (pas de GM, pas d'écran propre).";
        }

        static RadioDef YaesuModel(string key, string name, string cat, string id, int digits, long maxHz, int baud, string modes)
        {
            RadioDef r = Ftdx10(); foreach (string xid in new[] { "clartxk", "clrclar", "menu", "bigmeter" }) { Ctl xc = r.Find(xid); if (xc != null) r.C.Remove(xc); } { Ctl cl0 = r.Find("clar"); if (cl0 != null) { cl0.Label = "CLAR"; cl0.DLabel = "CLAR"; } }   /* CLAR TX et CLEAR CLAR sont propres au FTdx10 */ r.Key = key; r.Name = name; r.Catalog = cat; r.IdAnswer = id; r.FreqDigits = digits; r.MaxFreq = maxHz; r.DefBaud = baud;
            RadioDef probe = new RadioDef(); probe.Catalog = cat; probe.IdAnswer = id;
            CatalogSim cs = new CatalogSim(probe, new List<string>(), new List<string>(), new Dictionary<string, string>());
            string d = digits.ToString();
            if (!cs.Valid("ST0;") && r.Find("split") != null)
            {
                Ctl s = r.Find("split"); bool a = false;   // FT2 / FT3 sont absolus (manuels : FT-991A, FT-950, FTdx3000…) ; FT0 / FT1 ne sont que des réponses s.Set = "FT{v};"; s.Read = "FT;"; s.Parse = a ? @"^FT(?<v>[01]);$" : @"^FT(?<v>[0-3]);$"; s.OnVal = a ? "1" : "3|1"; s.OffVal = a ? "0" : "2|0";
                s.Hint = "Ce poste n'a pas de commande ST (split) : SPLIT = FT (fonction TX : émission sur le VFO-B). À vérifier sur le poste.";
            }
            foreach (Ctl c in r.C)
            {
                c.Set = c.Set.Replace("{v:9}", "{v:" + d + "}"); c.Parse = c.Parse.Replace(@"\d{9}", @"\d{" + d + "}").Replace("000;$", "(?:000)?;$");
                if (c.Items.Contains("PSK=E")) c.Items = modes;
            }
            // pas de commande CF dans le catalogue : le décalage du clarifier s'écrit avec RC puis RU / RD (comme le FT-2000) et se lit avec IF
            Ctl cl = r.Find("clar");
            if (cl != null && !cs.Valid("CF001+0010;") && cs.Valid("RU0010;") && cs.Valid("RD0010;") && cs.Valid("RC;"))
            { cl.Set = "RC;{p:RU:4:;}{n:RD:4:;}"; cl.Min = -9999; cl.Max = 9999; cl.Hint = "Pas de commande CF sur ce poste : le décalage s'écrit avec RC; puis RU (plus) ou RD (moins), par pas de 10 Hz ; il est lu avec IF; qui met aussi à jour fréquence A, mode A, CLAR RX/TX."; }
            FixYaesu(r, cat, key);
            List<Ctl> keep = new List<Ctl>();
            foreach (Ctl c in r.C)
            {
                if (c.Id == "ft8")
                {
                    // FT8/FT4... prend la case libre de la grille des modes (nombre de modes impair, sur 2 rangées) ; pas de case libre : pas de bouton
                    Ctl m0 = r.Find("md0"); int n = m0 != null ? m0.ItemList.Length : 0, cols = (n + 1) / 2;
                    if (m0 != null && n > 0 && n % 2 == 1)
                    {
                        float cw = (m0.W - 4 * (cols - 1)) / (float)cols, ch = (m0.H - 4) / 2f; int cx = n % cols, cy = n / cols;
                        c.X = c.DX = m0.X + (int)(cx * (cw + 4)); c.Y = c.DY = m0.Y + (int)(cy * (ch + 4)); c.W = c.DW = (int)cw; c.H = c.DH = (int)ch; keep.Add(c);
                    }
                }
                else if (!c.HasCat || Conforms(cs, c)) keep.Add(c); else r.Notes.Add(c.Id + " (" + c.Label + ") : commande absente du catalogue de " + name + ", élément retiré");
            }
            keep = keep.FindAll(delegate(Ctl q) { return !(q.K == Kind.Led && q.Local && q.Ref.Length > 0 && !keep.Exists(delegate(Ctl z) { return z.Id == q.Ref; })); });
            r.C = keep;
            if (!cs.Valid("AI0;")) r.Init = "";
            new Bld(r).Fin();
            return r;
        }

        static RadioDef Ft450() { return YaesuModel("ft450", "Yaesu FT-450D", "ft450", "ID0241;", 8, 56000000, 4800, MODES_450); }
        static RadioDef Ft710() { return YaesuModel("ft710", "Yaesu FT-710", "ft710", "ID0800;", 9, 75000000, 38400, MODES_NEW); }
        static RadioDef Ft891() { return YaesuModel("ft891", "Yaesu FT-891", "ft891", "ID0650;", 9, 56000000, 9600, MODES_891); }
        static RadioDef Ft950() { return YaesuModel("ft950", "Yaesu FT-950", "ft950", "ID0310;", 8, 56000000, 4800, MODES_OLD); }
        static RadioDef Ft991a() { return YaesuModel("ft991a", "Yaesu FT-991A", "ft991a", "ID0670;", 9, 470000000, 4800, MODES_991); }
        // copie conforme du FT-991A, avec sa propre disposition (fichiers datat991a-df5dp.*) pour deux affichages distincts
        static RadioDef Ft991aDf5dp() { return YaesuModel("ft991a-df5dp", "Yaesu FT-991A DF5DP", "ft991a", "ID0670;", 9, 470000000, 4800, MODES_991); }
        static RadioDef Ftdx101() { return YaesuModel("ftdx101", "Yaesu FTdx101D/MP", "ftdx101", "ID0681;", 9, 75000000, 38400, MODES_NEW); }
        static RadioDef Ftdx3000() { return YaesuModel("ftdx3000", "Yaesu FTdx3000", "ftdx3000", "ID0460;", 8, 60000000, 4800, MODES_OLD); }
        // modèles voisins : pas de catalogue propre, ceux du FTdx3000 (FTdx1200, FTdx9000) et du FT-710 (FTX-1) servent de référence
        static RadioDef Neigh(RadioDef r, string ref0) { r.Notes.Add("pas de manuel CAT analysé pour ce poste : commandes contrôlées contre le catalogue du " + ref0 + ", à vérifier sur le poste"); return r; }
        static RadioDef Ftdx1200() { RadioDef r = YaesuModel("ftdx1200", "Yaesu FTdx1200", "ftdx3000", "ID0583;", 8, 60000000, 4800, MODES_1200); r.Key = "ftdx1200"; return Neigh(r, "FTdx3000"); }
        static RadioDef Ftdx9000() { RadioDef r = YaesuModel("ftdx9000", "Yaesu FTdx9000", "ftdx3000", "ID0101;", 8, 60000000, 4800, MODES_OLD); return Neigh(r, "FTdx3000"); }
        static RadioDef Ftx1() { RadioDef r = YaesuModel("ftx1", "Yaesu FTX-1", "ft710", "ID0840;", 9, 75000000, 38400, MODES_NEW); return Neigh(r, "FT-710"); }
    }
}
