using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace HamRadioLink
{
    public static partial class Radios
    {
        // Icom (CI-V) et Xiegu (compatible CI-V). Une même face avant, dont les éléments dépendent du modèle (lettres « flags »).
        // Les 8 modèles dont le manuel CI-V est dans analyse\civ (IC-7000, 7100, 7300, 7300MK2, 7410, 7610, 7760, 9700) sont vérifiés commande par commande
        // contre leur catalogue ; les autres utilisent le catalogue du modèle voisin (indiqué dans les notes du rapport d'essai) : jamais essayés sur un vrai poste.
        // flags : s S-mètre · p PTT · a ATT · r préampli · g AGC · b NB · n NR · q notch · l niveaux AF/RF/SQL/PWR/MIC · o mètres Po/SWR/ALC · t tuner
        //         v VFO non sélectionné (25) · x RIT/XIT · d DATA · w marche/arrêt · u deux récepteurs · e COMP/VOX/MON/LOCK · k manip CW · c niveaux NB/NR
        // bandes : H HF · 6 50 MHz · V 144 · U 430 · L 1296 · M 2400 · N 5760 · T 222
        class IM
        {
            public string Key, Brand, Name, Flags, Bands, Cat; public int Addr, Baud, Digits = 10;
            public IM(string brand, string key, string name, int addr, int baud, string flags, string bands, string cat) { Brand = brand; Key = key; Name = name; Addr = addr; Baud = baud; Flags = flags; Bands = bands; Cat = cat; }
        }
        const string F4 = "spagrlbnqotvxdwekc", F3 = "spagrlbnqotxwekc", F2 = "sparlxk";
        static List<IM> IcomModels()
        {
            List<IM> l = new List<IM>(); string I = "Icom", X = "Xiegu";
            l.Add(new IM(I, "ic7000", "IC-7000", 0x70, 19200, F3, "H6VU", "ic7000"));
            l.Add(new IM(I, "ic7760", "IC-7760", 0xB2, 115200, F4 + "u", "H6", "ic7760"));
            l.Add(new IM(I, "ic7300", "IC-7300", 0x94, 115200, F4, "H6", "ic7300"));
            l.Add(new IM(I, "ic7300mk2", "IC-7300MK2", 0xB6, 115200, F4, "H6", "ic7300mk2"));
            l.Add(new IM(I, "ic7610", "IC-7610", 0x98, 115200, F4 + "u", "H6", "ic7610"));
            l.Add(new IM(I, "ic705", "IC-705", 0xA4, 115200, F4, "H6VU", "ic9700"));
            l.Add(new IM(I, "ic905", "IC-905", 0xAC, 115200, F4, "VULMN", "ic9700"));
            l.Add(new IM(I, "ic9700", "IC-9700", 0xA2, 115200, F4 + "u", "VUL", "ic9700"));
            l.Add(new IM(I, "ic9100", "IC-9100", 0x7C, 19200, F3 + "du", "H6VUL", "ic7410"));
            l.Add(new IM(I, "ic7100", "IC-7100", 0x88, 19200, F3 + "d", "H6VU", "ic7100"));
            l.Add(new IM(I, "ic7200", "IC-7200", 0x76, 19200, F3, "H6", "ic7410"));
            l.Add(new IM(I, "ic7410", "IC-7410", 0x80, 19200, F3 + "d", "H6", "ic7410"));
            l.Add(new IM(I, "ic7400", "IC-7400", 0x66, 19200, F3, "H6V", "ic7410"));
            l.Add(new IM(I, "ic7600", "IC-7600", 0x7A, 19200, F3 + "u", "H6", "ic7410"));
            l.Add(new IM(I, "ic7700", "IC-7700", 0x74, 19200, F3, "H6", "ic7410"));
            l.Add(new IM(I, "ic7800", "IC-7800", 0x6A, 19200, F3 + "u", "H6", "ic7410"));
            l.Add(new IM(I, "ic7850", "IC-7850", 0x8E, 115200, F4 + "u", "H6", "ic7610"));
            l.Add(new IM(I, "ic7851", "IC-7851", 0x8E, 115200, F4 + "u", "H6", "ic7610"));
            l.Add(new IM(I, "ic756", "IC-756", 0x50, 19200, F3, "H6", "ic7000"));
            l.Add(new IM(I, "ic756pro", "IC-756PRO", 0x5C, 19200, F3, "H6", "ic7000"));
            l.Add(new IM(I, "ic756proii", "IC-756PROII", 0x64, 19200, F3, "H6", "ic7000"));
            l.Add(new IM(I, "ic756proiii", "IC-756PROIII", 0x6E, 19200, F3, "H6", "ic7000"));
            l.Add(new IM(I, "ic746", "IC-746", 0x56, 9600, F2, "H6V", "ic7000"));
            l.Add(new IM(I, "ic746pro", "IC-746PRO", 0x66, 9600, F3, "H6V", "ic7000"));
            l.Add(new IM(I, "ic718", "IC-718", 0x5E, 9600, F2, "H", "ic7000"));
            l.Add(new IM(I, "ic78", "IC-78", 0x62, 9600, F2, "H", "ic7000"));
            l.Add(new IM(I, "ic703", "IC-703", 0x68, 9600, F2, "H6", "ic7000"));
            l.Add(new IM(I, "ic706mkiig", "IC-706MKIIG", 0x58, 9600, F2 + "p", "H6VU", "ic7000"));
            l.Add(new IM(I, "ic706mkii", "IC-706MKII", 0x4E, 9600, F2, "H6V", "ic7000"));
            l.Add(new IM(I, "ic706", "IC-706", 0x48, 9600, F2, "H6V", "ic7000"));
            l.Add(new IM(I, "ic707", "IC-707", 0x3E, 9600, F2, "H", "ic7000"));
            l.Add(new IM(I, "ic736", "IC-736", 0x40, 9600, F2, "H6", "ic7000"));
            l.Add(new IM(I, "ic737", "IC-737", 0x3C, 9600, F2, "H", "ic7000"));
            l.Add(new IM(I, "ic738", "IC-738", 0x44, 9600, F2, "H", "ic7000"));
            l.Add(new IM(I, "ic775", "IC-775", 0x46, 9600, F2, "H", "ic7000"));
            l.Add(new IM(I, "ic820h", "IC-820H", 0x42, 9600, F2, "VU", "ic7000"));
            l.Add(new IM(I, "ic821h", "IC-821H", 0x4C, 9600, F2, "VU", "ic7000"));
            l.Add(new IM(I, "ic910h", "IC-910H", 0x60, 9600, F2 + "u", "VUL", "ic7000"));
            l.Add(new IM(I, "ic970", "IC-970", 0x2E, 9600, F2, "VUL", "ic7000"));
            l.Add(new IM(I, "ic725", "IC-725", 0x28, 1200, "", "H", "ic7000"));
            l.Add(new IM(I, "ic726", "IC-726", 0x30, 1200, "", "H6", "ic7000"));
            l.Add(new IM(I, "ic728", "IC-728", 0x38, 1200, "", "H", "ic7000"));
            l.Add(new IM(I, "ic729", "IC-729", 0x3A, 1200, "", "H6", "ic7000"));
            IM m735 = new IM(I, "ic735", "IC-735", 0x04, 1200, "", "H", "ic7000"); m735.Digits = 8; l.Add(m735);
            l.Add(new IM(I, "ic751", "IC-751/751A", 0x1C, 1200, "", "H", "ic7000"));
            l.Add(new IM(I, "ic761", "IC-761", 0x1E, 1200, "", "H", "ic7000"));
            l.Add(new IM(I, "ic765", "IC-765", 0x2C, 1200, "", "H6", "ic7000"));
            l.Add(new IM(I, "ic781", "IC-781", 0x26, 1200, "", "H", "ic7000"));
            l.Add(new IM(I, "ic271", "IC-271", 0x20, 1200, "", "V", "ic7000"));
            l.Add(new IM(I, "ic275", "IC-275", 0x10, 1200, "", "V", "ic7000"));
            l.Add(new IM(I, "ic375", "IC-375", 0x12, 1200, "", "T", "ic7000"));
            l.Add(new IM(I, "ic471", "IC-471", 0x22, 1200, "", "U", "ic7000"));
            l.Add(new IM(I, "ic475", "IC-475", 0x14, 1200, "", "U", "ic7000"));
            l.Add(new IM(I, "ic575", "IC-575", 0x16, 1200, "", "6", "ic7000"));
            l.Add(new IM(I, "ic1275", "IC-1275", 0x18, 1200, "", "L", "ic7000"));
            l.Add(new IM(X, "g90", "G90", 0x88, 19200, "spl", "H", "ic7000"));
            l.Add(new IM(X, "x108g", "X108G", 0x70, 19200, "spl", "H6V", "ic7000"));
            l.Add(new IM(X, "x5105", "X5105", 0x70, 19200, "spl", "H6", "ic7000"));
            l.Add(new IM(X, "x6100", "X6100", 0xA4, 19200, "spl", "H6", "ic7300"));
            l.Add(new IM(X, "x6200", "X6200", 0xA4, 19200, "spl", "H6", "ic7300"));
            return l;
        }
        public static List<string> IcomKeys() { List<string> k = new List<string>(); foreach (IM m in IcomModels()) k.Add(m.Key); return k; }
        public static RadioDef IcomByKey(string key) { foreach (IM m in IcomModels()) if (m.Key == key) return Icom(m); return null; }

        const string BandH = "1.8=0001840000|00,3.5=0003573000|01,5=0005357000|02,7=0007074000|03,10=0010136000|04,14=0014074000|05,18=0018100000|06,21=0021074000|07,24.9=0024915000|08,28=0028074000|09";
        static string BandItems(string bands)
        {
            List<string> l = new List<string>();
            foreach (char ch in bands)
                switch (ch)
                {
                    case 'H': l.Add(BandH); break; case '6': l.Add("50=0050313000|10"); break; case 'V': l.Add("144=0144300000|11"); break; case 'U': l.Add("430=0433000000"); break;
                    case 'L': l.Add("1296=1296100000"); break; case 'M': l.Add("2400=2400100000"); break; case 'N': l.Add("5760=5760100000"); break; case 'T': l.Add("222=0222100000"); break;
                }
            return string.Join(",", l.ToArray());
        }
        static long MaxHz(string bands) { long m = 30000000; if (bands.IndexOf('6') >= 0) m = 54000000; if (bands.IndexOf('T') >= 0) m = 225000000; if (bands.IndexOf('V') >= 0) m = 148000000; if (bands.IndexOf('U') >= 0) m = 470000000; if (bands.IndexOf('L') >= 0) m = 1300000000; if (bands.IndexOf('M') >= 0) m = 2450000000; if (bands.IndexOf('N') >= 0) m = 5850000000; if (bands == "V" || bands == "T") return m; return m; }

        // l'élément existe-t-il dans le catalogue du manuel ? (commande = premiers octets hexadécimaux du gabarit)
        static bool CivConforms(CivSim sim, Ctl c)
        {
            if (!c.HasCat) return true; string t = c.Set.Length > 0 ? c.Set : c.Read; string h = Regex.Match(Regex.Replace(t, "^(FE)+;?", ""), "^[0-9A-F]+").Value; if (h.Length < 2) return true;
            return sim.Known(h);
        }

        static RadioDef Icom(IM m)
        {
            RadioDef r = new RadioDef(); r.Key = m.Key; r.Name = m.Brand + " " + m.Name; bool own = CivCatalogs.D.ContainsKey(m.Key); r.Civ = m.Addr; r.CivCat = own ? m.Key : "*"; r.IdCmd = "1900;"; r.IdAnswer = "1900" + m.Addr.ToString("X2") + ";"; r.Init = ""; r.DefBaud = m.Baud; r.FreqDigits = m.Digits; r.MaxFreq = MaxHz(m.Bands);
            string f = m.Flags; int D = m.Digits; Bld b = new Bld(r); Ctl c; bool dual = f.IndexOf('u') >= 0;
            string att = "OFF=00,20 dB=20", pre = "OFF=00,PRÉAMP 1=01,PRÉAMP 2=02", agc = "FAST=01,MID=02,SLOW=03";
            switch (m.Key)
            {
                case "ic7000": att = "OFF=00,12 dB=12"; pre = "OFF=00,ON=01"; agc = "OFF=00,SLOW=01,MID=02,FAST=03"; break;
                case "ic7100": att = "OFF=00,12 dB=12"; break;
                case "ic7610": case "ic7760": case "ic7850": case "ic7851": att = "OFF=00,6 dB=06,12 dB=12,18 dB=18,24 dB=24,30 dB=30,36 dB=36,42 dB=42"; break;
                case "ic9700": case "ic705": case "ic905": att = m.Key == "ic9700" ? "OFF=00,10 dB=10" : att; pre = "OFF=00,PRÉAMP=01,EXT=02,PRÉAMP+EXT=03"; break;
                case "ic706": case "ic706mkii": case "ic706mkiig": case "ic703": case "ic718": case "ic746": case "ic78": case "ic775": case "ic707": case "ic736": case "ic737": case "ic738": pre = "OFF=00,ON=01"; break;
            }
            if (m.Brand == "Xiegu") { att = "OFF=00,ATT=20"; pre = "OFF=00,ON=01"; }
            string modes = "LSB=00,USB=01,AM=02,CW=03,RTTY=04,FM=05,CW-R=07,RTTY-R=08"; if (m.Key == "ic7100" || m.Key == "ic705" || m.Key == "ic905" || m.Key == "ic9700") modes += ",DV=17"; if (m.Key == "ic9700" || m.Key == "ic905") modes += ",DD=22";
            string fz = "^0[03](?<v>\\d{" + D + "});$", f25 = "^2501(?<v>\\d{" + D + "});$";

            // ---- alimentation / émission
            if (Regex.IsMatch(f, "[wtepk]")) b.Frame("f_pwr", "ALIMENTATION / ÉMISSION", 10, 124, 210, 430);
            if (f.IndexOf('w') >= 0) { c = b.Act("onoff", "ON", 24, 152, 118, 42, "FEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFE;1801;"); c.Tip = "L'allumage par CI-V demande d'envoyer d'abord plusieurs FE (le poste dort) : à ajuster par clic droit si besoin."; b.Act("poff", "OFF", 150, 152, 56, 42, "1800;"); }
            if (f.IndexOf('t') >= 0) { b.Tog("tuner", "TUNER", 24, 202, 182, 42, "1C01{v};", "1C01;", @"^1C01(?<v>0[0-2]);$", "01|02", "00").Accent = "amber"; b.Act("tune", "TUNE (lancer)", 24, 478, 182, 40, "1C0102;"); }
            if (f.IndexOf('e') >= 0)
            {
                b.Tog("vox", "VOX", 24, 252, 86, 44, "1646{v};", "1646;", @"^1646(?<v>0[01]);$", "01", "00");
                b.Tog("lock", "LOCK", 120, 252, 86, 44, "1650{v};", "1650;", @"^1650(?<v>0[01]);$", "01", "00");
                b.Tog("comp", "COMP", 24, 370, 86, 40, "1644{v};", "1644;", @"^1644(?<v>0[01]);$", "01", "00");
                b.Tog("mon", "MONI", 120, 370, 86, 40, "1645{v};", "1645;", @"^1645(?<v>0[01]);$", "01", "00");
            }
            if (f.IndexOf('p') >= 0) { c = b.Tog("ptt", "ÉMISSION (PTT)", 24, 306, 182, 54, "1C00{v};", "1C00;", @"^1C00(?<v>0[01]);$", "01", "00"); c.Accent = "red"; c.Period = 250; }
            if (f.IndexOf('k') >= 0) b.Cho("bk", "BK-IN", 24, 418, 182, 52, "OFF=00,SEMI=01,FULL=02", "1647{v};", "1647;", @"^1647(?<v>0[0-2]);$");

            // ---- écran
            b.Frame("f_scr", "ÉCRAN", 230, 124, 1010, 430); bool vb = f.IndexOf('v') >= 0;
            b.Freq("fa", dual ? "PRINCIPAL" : "VFO SÉLECTIONNÉ", 244, 150, vb ? 640 : 986, 150, "05{v:" + D + "};", "03;", fz, "md0", "amber").Hint = D + " chiffres en Hz ; envoyé en BCD par CivLink (14074000 Hz = 0014074000).";
            if (vb) b.Freq("fb", dual ? "SECONDAIRE" : "VFO NON SÉLECT.", 894, 150, 336, 150, "2501{v:" + D + "};", "2501;", f25, "md0", "blue");
            if (f.IndexOf('s') >= 0) b.Meter("sm", "S", 244, 310, 480, 34, 241, "1502;", @"^1502(?<v>\d{4});$", "s", false).Hint = "0000 = S0, 0120 = S9, 0241 = S9+60 dB.";
            if (f.IndexOf('o') >= 0)
            {
                b.Meter("po", "PO", 736, 310, 494, 34, 213, "1511;", @"^1511(?<v>\d{4});$", "pct", true);
                b.Meter("swr", "SWR", 244, 348, 480, 34, 255, "1512;", @"^1512(?<v>\d{4});$", "pct", true).Hint = "0000 = 1,0 ; 0048 = 1,5 ; 0080 = 2,0 ; 0120 = 3,0.";
                b.Meter("alc", "ALC", 736, 348, 494, 34, 120, "1513;", @"^1513(?<v>\d{4});$", "pct", true);
            }
            if (f.IndexOf('s') >= 0) b.LedRd("l_busy", "SQUELCH", 744, 430, 110, 26, "1501;", @"^1501(?<v>0[01]);$", "green");
            if (f.IndexOf('p') >= 0) b.Led("l_tx", "TX", 862, 430, 110, 26, "ptt", "01", "red");
            if (f.IndexOf('a') >= 0) b.Cho("att", "ATT", 244, 470, 150, 76, att, "11{v};", "11;", @"^11(?<v>[0-9]{2});$");
            if (f.IndexOf('r') >= 0) b.Cho("pre", "PRÉAMPLI", 400, 470, 150, 76, pre, "1602{v};", "1602;", @"^1602(?<v>0[0-3]);$");
            if (f.IndexOf('g') >= 0) b.Cho("agc", "AGC", 556, 470, 150, 76, agc, "1612{v};", "1612;", @"^1612(?<v>0[0-3]);$");
            if (f.IndexOf('b') >= 0) b.Tog("nb", "NB", 712, 470, 100, 76, "1622{v};", "1622;", @"^1622(?<v>0[01]);$", "01", "00");
            if (f.IndexOf('n') >= 0) b.Tog("nr", "NR", 818, 470, 100, 76, "1640{v};", "1640;", @"^1640(?<v>0[01]);$", "01", "00");
            if (f.IndexOf('q') >= 0) { b.Tog("notch", "NOTCH AUTO", 924, 470, 150, 76, "1641{v};", "1641;", @"^1641(?<v>0[01]);$", "01", "00"); b.Tog("mnotch", "NOTCH MAN.", 1080, 470, 150, 76, "1648{v};", "1648;", @"^1648(?<v>0[01]);$", "01", "00"); }

            // ---- VFO
            b.Frame("f_vfo", "VFO / RIT / XIT", 1250, 124, 600, 430); const int rx = 1264; const int bw = 108;
            b.Tog("split", "SPLIT", rx, 152, bw, 40, "0F{v};", "0F;", @"^0F(?<v>[0-9A-F]{2});$", "01", "00").Period = 3000;
            b.Act("swap", dual ? "MAIN ⇄ SUB" : "A ⇄ B", rx + 116, 152, bw, 40, "07B0;"); b.Act("a2b", "A = B", rx + 232, 152, bw, 40, "07A0;");
            if (f.IndexOf('x') >= 0)
            {
                b.Tog("rit", "RIT", rx, 200, bw, 40, "2101{v};", "2101;", @"^2101(?<v>0[01]);$", "01", "00").Period = 6000;
                b.Tog("xit", "XIT", rx + 116, 200, bw, 40, "2102{v};", "2102;", @"^2102(?<v>0[01]);$", "01", "00").Period = 6000;
                b.Act("ritclr", "RIT/XIT RAZ", rx + 232, 200, bw, 40, "2100000000;");
            }
            if (dual) b.Row("vsel", "BANDE ACTIVE", rx, 248, 224, 40, 1, "MAIN=D0,SUB=D1", "07{v};", "", "");
            if (f.IndexOf('d') >= 0) b.Cho("data", "DATA", rx + 232, 248, bw, 40, "OFF=00,DATA=01", "1A06{v}01;", "1A06;", @"^1A06(?<v>0[01])").Hint = "Le filtre est remis sur FIL1 à chaque changement (1A 06 données + filtre).";
            if (f.IndexOf('k') >= 0) { b.Knob("ks", "VITESSE", rx, 300, 80, 90, 0, 255, "140C{v:4};", "140C;", @"^140C(?<v>\d{4});$", "raw"); b.Knob("kp", "TONALITÉ", rx + 90, 300, 80, 90, 0, 255, "1409{v:4};", "1409;", @"^1409(?<v>\d{4});$", "raw"); }
            if (f.IndexOf('c') >= 0) { b.Knob("nbl", "NB niv.", rx + 180, 300, 80, 90, 0, 255, "1412{v:4};", "1412;", @"^1412(?<v>\d{4});$", "pct"); b.Knob("nrl", "NR niv.", rx + 270, 300, 80, 90, 0, 255, "1406{v:4};", "1406;", @"^1406(?<v>\d{4});$", "pct"); }

            // ---- bande / mode
            b.Frame("f_mode", "BANDE / MODE", 10, 564, 830, 190);
            c = b.Row("band", "BANDE", 24, 590, 802, 52, 1, D == 10 ? BandItems(m.Bands) : Regex.Replace(BandItems(m.Bands), @"=\d{2}(\d{" + D + "})", "=$1"), "05{v};", "", ""); c.Hint = "Chaque touche envoie la fréquence type de la bande au VFO sélectionné ; elle s'allume d'après la fréquence.";
            b.Row("md0", "MODE", 24, 650, 802, 90, 2, modes, "06{v};", "04;", @"^0[14](?<v>[0-9A-F]{2})").Period = 1500;

            // ---- accord
            b.Frame("f_dial", "ACCORD", 850, 564, 340, 406); b.Dial("dial", "VFO", 870, 592, 300, 300, "fa", "step");
            c = b.Cho("step", "PAS", 870, 900, 300, 56, "1 Hz=1,10 Hz=10,100 Hz=100,500 Hz=500,1 kHz=1000,5 kHz=5000,10 kHz=10000", "", "", ""); c.Local = true;

            // ---- réception / émission
            if (f.IndexOf('l') >= 0)
            {
                b.Frame("f_rx", "RÉCEPTION / ÉMISSION", 10, 764, 830, 206);
                b.Knob("af", "AF", 24, 792, 90, 110, 0, 255, "1401{v:4};", "1401;", @"^1401(?<v>\d{4});$", "pct");
                b.Knob("rf", "RF", 122, 792, 90, 110, 0, 255, "1402{v:4};", "1402;", @"^1402(?<v>\d{4});$", "pct");
                b.Knob("sql", "SQL", 220, 792, 90, 110, 0, 255, "1403{v:4};", "1403;", @"^1403(?<v>\d{4});$", "pct");
                b.Knob("mic", "MIC", 318, 792, 90, 110, 0, 255, "140B{v:4};", "140B;", @"^140B(?<v>\d{4});$", "pct");
                b.Knob("pwr", "PUISSANCE", 416, 792, 100, 110, 0, 255, "140A{v:4};", "140A;", @"^140A(?<v>\d{4});$", "pct").Hint = "0000 à 0255 (0255 = puissance maximale du poste).";
                if (f.IndexOf('e') >= 0) { b.Knob("compl", "COMP niv.", 524, 792, 90, 110, 0, 255, "140E{v:4};", "140E;", @"^140E(?<v>\d{4});$", "pct"); b.Knob("voxl", "VOX gain", 622, 792, 90, 110, 0, 255, "1416{v:4};", "1416;", @"^1416(?<v>\d{4});$", "pct"); b.Knob("monl", "MONI niv.", 720, 792, 90, 110, 0, 255, "1415{v:4};", "1415;", @"^1415(?<v>\d{4});$", "pct"); }
            }

            // ---- contrôle contre le catalogue du manuel
            { RadioDef probe = new RadioDef(); probe.Key = r.Key; probe.Civ = r.Civ; probe.CivCat = r.CivCat; probe.FreqDigits = r.FreqDigits; probe.IdAnswer = r.IdAnswer; CivSim cs = new CivSim(probe, new List<string>(), new List<string>(), new Dictionary<string, string>()); List<Ctl> keep = new List<Ctl>();
              foreach (Ctl q in r.C) { if (!own || !q.HasCat || CivConforms(cs, q)) keep.Add(q); else r.Notes.Add(q.Id + " (" + q.Label + ") : commande absente du catalogue CI-V du " + m.Name + ", élément retiré"); }
              r.C = keep.FindAll(delegate(Ctl q) { return !(q.K == Kind.Led && q.Local && q.Ref.Length > 0 && !keep.Exists(delegate(Ctl z) { return z.Id == q.Ref; })); });
              if (!own) r.Notes.Add("pas de manuel CI-V analysé pour ce modèle : commandes contrôlées contre l'ensemble des 8 manuels Icom disponibles ; face avant d'après la famille (" + m.Cat + "), à vérifier sur le poste"); }
            // le VFO non sélectionné disparu : le VFO principal prend toute la largeur
            if (r.Find("fb") == null) { Ctl a = r.Find("fa"); if (a != null) a.W = 986; }
            b.Fin();
            return r;
        }
    }
}
