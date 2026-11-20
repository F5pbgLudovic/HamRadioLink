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
            if (cat == "ft710") { Ctl pl = r.Find("procl"); if (pl != null) pl.Min = 1; }
            if (cat == "ft950") { Ctl pr = r.Find("proc"); if (pr != null) { pr.Set = "PR{v};"; pr.Read = "PR;"; pr.Parse = @"^PR(?<v>[0-2]);$"; } Ctl ml = r.Find("monil"); if (ml != null) { ml.Min = 1; ml.Max = 255; } }
            if (cat == "ftdx3000")
            {
                Ctl rf = r.Find("rfil");
                if (rf != null) { if (key == "ftdx1200") { rf.Items = "AUTO=0|4|5|6,15 kHz=1,6 kHz=2,3 kHz=3"; rf.Parse = @"^RF0(?<v>[0-6]);$"; } else { rf.Items = "AUTO=0|4|5|6|9|A,15 kHz=1,6 kHz=2,3 kHz=3,600 Hz=4|7,300 Hz=5|8"; rf.Parse = @"^RF0(?<v>[0-9A]);$"; } }
                Ctl ml = r.Find("monil"); if (ml != null) { ml.Min = 1; ml.Max = 100; }
            }
        }

        static RadioDef YaesuModel(string key, string name, string cat, string id, int digits, long maxHz, int baud, string modes)
        {
            RadioDef r = Ftdx10(); r.Key = key; r.Name = name; r.Catalog = cat; r.IdAnswer = id; r.FreqDigits = digits; r.MaxFreq = maxHz; r.DefBaud = baud;
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
            FixYaesu(r, cat, key);
            List<Ctl> keep = new List<Ctl>();
            foreach (Ctl c in r.C)
            {
                if (!c.HasCat || Conforms(cs, c)) keep.Add(c); else r.Notes.Add(c.Id + " (" + c.Label + ") : commande absente du catalogue de " + name + ", élément retiré");
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
        static RadioDef Ftdx101() { return YaesuModel("ftdx101", "Yaesu FTdx101D/MP", "ftdx101", "ID0681;", 9, 75000000, 38400, MODES_NEW); }
        static RadioDef Ftdx3000() { return YaesuModel("ftdx3000", "Yaesu FTdx3000", "ftdx3000", "ID0460;", 8, 60000000, 4800, MODES_OLD); }
        // modèles voisins : pas de catalogue propre, ceux du FTdx3000 (FTdx1200, FTdx9000) et du FT-710 (FTX-1) servent de référence
        static RadioDef Neigh(RadioDef r, string ref0) { r.Notes.Add("pas de manuel CAT analysé pour ce poste : commandes contrôlées contre le catalogue du " + ref0 + ", à vérifier sur le poste"); return r; }
        static RadioDef Ftdx1200() { RadioDef r = YaesuModel("ftdx1200", "Yaesu FTdx1200", "ftdx3000", "ID0583;", 8, 60000000, 4800, MODES_1200); r.Key = "ftdx1200"; return Neigh(r, "FTdx3000"); }
        static RadioDef Ftdx9000() { RadioDef r = YaesuModel("ftdx9000", "Yaesu FTdx9000", "ftdx3000", "ID0101;", 8, 60000000, 4800, MODES_OLD); return Neigh(r, "FTdx3000"); }
        static RadioDef Ftx1() { RadioDef r = YaesuModel("ftx1", "Yaesu FTX-1", "ft710", "ID0840;", 9, 75000000, 38400, MODES_NEW); return Neigh(r, "FT-710"); }
    }
}
