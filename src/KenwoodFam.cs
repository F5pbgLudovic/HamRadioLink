using System;
using System.Collections.Generic;

namespace HamRadioLink
{
    public static partial class Radios
    {
        // Autres Kenwood et Elecraft (CAT ASCII « XX...; » de la famille Kenwood). Seuls les catalogues du TS-480 et du TS-890 sont analysés :
        // chaque poste reprend la face avant de l'un des deux, réduite aux fonctions qu'il possède, et ses commandes sont contrôlées contre ce catalogue.
        // Ni les manuels de ces postes, ni le matériel n'ont servi : tout se corrige par clic droit en « config vue » ou par « Ajouter ».
        // base : 0 = TS-480, 1 = TS-890 ; rm : identifiants d'éléments retirés (le poste ne les a pas) ; mini : face avant réduite (anciens postes)
        static RadioDef Kw(string key, string name, int baud, int baseKind, string id, long maxHz, string rm, bool mini, bool noTx)
        {
            RadioDef r = Kenwood(baseKind == 1); r.Key = key; r.Name = name; r.DefBaud = baud; r.IdAnswer = id; r.MaxFreq = maxHz;
            string note = "face avant de " + (baseKind == 1 ? "TS-890" : "TS-480") + (mini ? " réduite (ancien poste)" : "") + " ; pas de manuel analysé pour ce poste : commandes contrôlées contre le catalogue du " + (baseKind == 1 ? "TS-890" : "TS-480") + ", à vérifier sur le poste";
            HashSet<string> drop = new HashSet<string>(rm.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            HashSet<string> keep = new HashSet<string>(new string[] { "f_scr", "fa", "fb", "sm", "l_tx", "ptt", "f_mode", "band", "md0", "f_dial", "dial", "step", "f_vfo", "fr", "ft", "rit", "xit", "clr", "rup", "rdn", "f_rx", "af", "rf", "sql", "mic", "swr" });
            List<Ctl> n = new List<Ctl>(); foreach (Ctl c in r.C) { if (drop.Contains(c.Id)) continue; if (mini && !keep.Contains(c.Id)) continue; n.Add(c); }
            r.C = n; if (noTx) { Ctl p = r.Find("ptt"); if (p != null) { p.Read = ""; p.Parse = ""; p.Hint = "Ce poste ne permet pas de lire l'état émission/réception : le voyant ne suit que les ordres envoyés d'ici."; } }
            if (key == "ts590")   // manuel TS-590S/SG : AGC par GC (GT = constante de temps), pas de DL, RG 000-255
            {
                r.CatExtra = "GC|AGC TIME CONSTANT|GCP1;|GC;|P1 0 OFF; 1 SLOW; 2 FAST (manuel TS-590S/SG)"; Ctl ag = r.Find("agc"); if (ag != null) { ag.Items = "OFF=0,SLOW=1,FAST=2"; ag.Set = "GC{v};"; ag.Read = "GC;"; ag.Parse = @"^GC(?<v>[0-3]);$"; ag.Hint = "GC P1 : 0 OFF, 1 SLOW, 2 FAST (manuel TS-590S/SG) ; GT est la constante de temps."; }
                Ctl dn = r.Find("dnl"); if (dn != null) r.C.Remove(dn); Ctl rg = r.Find("rf"); if (rg != null) rg.Max = 255;
            }
            if (key == "ts590" || key == "ts2000" || key == "tsb2000")   // manuels TS-590S/SG et TS-2000 : RM de 0 à 30 points, S-mètre 0 à 30
            {
                foreach (string mid in new[] { "swr", "comp", "alc" }) { Ctl mm = r.Find(mid); if (mm != null) mm.Max = 30; } Ctl sm = r.Find("sm"); if (sm != null) sm.Max = 30;
            }
            if (key == "ts590") { foreach (string k in new[] { "bu", "bd" }) { Ctl x = r.Find(k); if (x != null) r.C.Remove(x); } Ctl ks = r.Find("ks"); if (ks != null) ks.Min = 4; }   // TS-590 : BU / BD attendent un numéro de bande ; KS 4 à 60
            if (key == "ts2000" || key == "tsb2000")   // manuel TS-2000 : pas de DL, RG 000-255, SH 00-11
            {
                Ctl dn2 = r.Find("dnl"); if (dn2 != null) r.C.Remove(dn2); Ctl rg2 = r.Find("rf"); if (rg2 != null) rg2.Max = 255; Ctl shh = r.Find("sh"); if (shh != null) shh.Max = 11;
            }
            if (key == "ts990") r.Notes.Add("face avant du TS-890 inadaptée : le TS-990S a deux récepteurs (Main / Sub) et la plupart de ses commandes portent un chiffre de bande (AG0, RG0, SQ0, GC0, NB10…), sans FR / FT (split par TB) : à refaire d'après le manuel (_hors-github\audit2-kenwood-elecraft.md, rubrique 4)");
            if (baseKind == 1) { Ctl fn = r.Find("fine"); if (fn != null) fn.Parse = @"^FS(?<v>[01])[01]?;$"; }   // TS-890 : la réponse FS a deux chiffres (fine RX et TX)
            r.Notes.Add(note); new Bld(r).Fin(); return r;
        }
        const string KW_NO_LINK = "proc,dnl,nbl,ts,fine,ca";
        static RadioDef Ts590() { return Kw("ts590", "Kenwood TS-590S/SG", 115200, 0, "ID021;", 60000000, "", false, false); }
        static RadioDef Ts990() { return Kw("ts990", "Kenwood TS-990S", 115200, 1, "ID022;", 60000000, "", false, false); }
        static RadioDef Ts2000() { return Kw("ts2000", "Kenwood TS-2000", 9600, 0, "ID019;", 1300000000, "", false, false); }
        static RadioDef Tsb2000() { return Kw("tsb2000", "Kenwood TS-B2000", 9600, 0, "ID019;", 1300000000, "", false, false); }
        static RadioDef Ts870() { return Kw("ts870", "Kenwood TS-870S", 9600, 0, "ID015;", 30000000, "tuner,vox,l_busy", false, false); }
        static RadioDef Ts570() { return Kw("ts570", "Kenwood TS-570", 9600, 0, "ID017;", 30000000, "tuner,vox,l_busy", false, false); }
        static RadioDef Ts850() { return Kw("ts850", "Kenwood TS-850S", 4800, 0, "ID009;", 30000000, "", true, false); }
        static RadioDef Ts50() { return Kw("ts50", "Kenwood TS-50S", 4800, 0, "ID013;", 30000000, "", true, false); }
        static RadioDef Ts60() { return Kw("ts60", "Kenwood TS-60S", 4800, 0, "ID014;", 54000000, "", true, false); }
        static RadioDef Ts450() { return Kw("ts450", "Kenwood TS-450S", 4800, 0, "ID010;", 30000000, "", true, false); }
        static RadioDef Ts690() { return Kw("ts690", "Kenwood TS-690S", 4800, 0, "ID011;", 54000000, "", true, false); }
        static RadioDef Ts790() { return Kw("ts790", "Kenwood TS-790", 4800, 0, "", 450000000, "", true, false); }
        static RadioDef Ts930() { return Kw("ts930", "Kenwood TS-930S", 4800, 0, "", 30000000, "", true, false); }
        static RadioDef Ts940() { return Kw("ts940", "Kenwood TS-940S", 4800, 0, "", 30000000, "", true, false); }
        static RadioDef Ts950() { return Kw("ts950", "Kenwood TS-950S", 4800, 0, "", 30000000, "", true, false); }
        static RadioDef Ts950sdx() { return Kw("ts950sdx", "Kenwood TS-950SDX", 4800, 0, "", 30000000, "", true, false); }
        static RadioDef Ts440() { return Kw("ts440", "Kenwood TS-440S", 4800, 0, "", 30000000, "", true, false); }
        static RadioDef Ts140() { return Kw("ts140", "Kenwood TS-140S", 4800, 0, "", 30000000, "", true, true); }
        static RadioDef Ts680() { return Kw("ts680", "Kenwood TS-680S", 4800, 0, "", 54000000, "", true, true); }
        static RadioDef Ts711() { return Kw("ts711", "Kenwood TS-711", 4800, 0, "", 150000000, "", true, true); }
        static RadioDef Ts811() { return Kw("ts811", "Kenwood TS-811", 4800, 0, "", 450000000, "", true, true); }
        static RadioDef K4() { return Kw("k4", "Elecraft K4", 38400, 0, "ID017;", 60000000, KW_NO_LINK, false, false); }
        static RadioDef K3() { return Kw("k3", "Elecraft K3 / K3S", 38400, 0, "ID017;", 60000000, KW_NO_LINK, false, false); }
        static RadioDef Kx3() { return Kw("kx3", "Elecraft KX3", 38400, 0, "ID017;", 60000000, KW_NO_LINK, false, false); }
        static RadioDef Kx2() { return Kw("kx2", "Elecraft KX2", 38400, 0, "ID017;", 60000000, KW_NO_LINK, false, false); }
        static RadioDef K2() { return Kw("k2", "Elecraft K2", 4800, 0, "", 30000000, KW_NO_LINK, true, false); }
    }
}
