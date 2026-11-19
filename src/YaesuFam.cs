using System;
using System.Collections.Generic;

namespace HamRadioLink
{
    public static partial class Radios
    {
        // Postes Yaesu « famille FTdx10 » : la face avant du FTdx10 sert de modèle ; chaque élément dont une commande n'existe pas
        // (ou n'a pas le bon format) dans le catalogue officiel du poste est retiré. Le nombre de chiffres de la fréquence et les
        // modes sont adaptés. Rien n'est essayé sur un vrai poste : tout se corrige par clic droit en « config vue ».
        const string MODES_NEW = "LSB=1,USB=2,CW-U=3,FM=4,AM=5,RTTY-L=6,CW-L=7,DATA-L=8,RTTY-U=9,DATA-FM=A,FM-N=B,DATA-U=C,AM-N=D,PSK=E,DATA-FM-N=F";
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

        static RadioDef YaesuModel(string key, string name, string cat, string id, int digits, long maxHz, int baud, string modes)
        {
            RadioDef r = Ftdx10(); r.Key = key; r.Name = name; r.Catalog = cat; r.IdAnswer = id; r.FreqDigits = digits; r.MaxFreq = maxHz; r.DefBaud = baud;
            RadioDef probe = new RadioDef(); probe.Catalog = cat; probe.IdAnswer = id;
            CatalogSim cs = new CatalogSim(probe, new List<string>(), new List<string>(), new Dictionary<string, string>());
            string d = digits.ToString();
            if (!cs.Valid("ST0;") && r.Find("split") != null)
            {
                Ctl s = r.Find("split"); bool a = key == "ft991a"; s.Set = "FT{v};"; s.Read = "FT;"; s.Parse = a ? @"^FT(?<v>[01]);$" : @"^FT(?<v>[0-3]);$"; s.OnVal = a ? "1" : "3|1"; s.OffVal = a ? "0" : "2|0";
                s.Hint = "Ce poste n'a pas de commande ST (split) : SPLIT = FT (fonction TX : émission sur le VFO-B). À vérifier sur le poste.";
            }
            foreach (Ctl c in r.C)
            {
                c.Set = c.Set.Replace("{v:9}", "{v:" + d + "}"); c.Parse = c.Parse.Replace(@"\d{9}", @"\d{" + d + "}").Replace("000;$", "(?:000)?;$");
                if (c.Items.Contains("PSK=E")) c.Items = modes;
            }
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

        static RadioDef Ft450() { return YaesuModel("ft450", "Yaesu FT-450D", "ft450", "ID0241;", 8, 56000000, 4800, MODES_OLD); }
        static RadioDef Ft710() { return YaesuModel("ft710", "Yaesu FT-710", "ft710", "ID0800;", 9, 75000000, 38400, MODES_NEW); }
        static RadioDef Ft891() { return YaesuModel("ft891", "Yaesu FT-891", "ft891", "ID0650;", 9, 56000000, 9600, MODES_NEW); }
        static RadioDef Ft950() { return YaesuModel("ft950", "Yaesu FT-950", "ft950", "ID0310;", 8, 56000000, 4800, MODES_OLD); }
        static RadioDef Ft991a() { return YaesuModel("ft991a", "Yaesu FT-991A", "ft991a", "ID0670;", 9, 470000000, 4800, MODES_NEW); }
        static RadioDef Ftdx101() { return YaesuModel("ftdx101", "Yaesu FTdx101D/MP", "ftdx101", "ID0681;", 9, 75000000, 38400, MODES_NEW); }
        static RadioDef Ftdx3000() { return YaesuModel("ftdx3000", "Yaesu FTdx3000", "ftdx3000", "ID0460;", 8, 60000000, 4800, MODES_OLD); }
        // modèles voisins : pas de catalogue propre, ceux du FTdx3000 (FTdx1200, FTdx9000) et du FT-710 (FTX-1) servent de référence
        static RadioDef Neigh(RadioDef r, string ref0) { r.Notes.Add("pas de manuel CAT analysé pour ce poste : commandes contrôlées contre le catalogue du " + ref0 + ", à vérifier sur le poste"); return r; }
        static RadioDef Ftdx1200() { RadioDef r = YaesuModel("ftdx1200", "Yaesu FTdx1200", "ftdx3000", "ID0583;", 8, 60000000, 4800, MODES_OLD); r.Key = "ftdx1200"; return Neigh(r, "FTdx3000"); }
        static RadioDef Ftdx9000() { RadioDef r = YaesuModel("ftdx9000", "Yaesu FTdx9000", "ftdx3000", "ID0101;", 8, 60000000, 4800, MODES_OLD); return Neigh(r, "FTdx3000"); }
        static RadioDef Ftx1() { RadioDef r = YaesuModel("ftx1", "Yaesu FTX-1", "ft710", "", 9, 75000000, 38400, MODES_NEW); return Neigh(r, "FT-710"); }
    }
}
