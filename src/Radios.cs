using System;
using System.Collections.Generic;

namespace HamRadioLink
{
    // Constructeur de face avant : chaque appel crée un élément avec ses commandes CAT par défaut.
    class Bld
    {
        public RadioDef R; public Bld(RadioDef r) { R = r; }
        Ctl N(Kind k, string id, string label, int x, int y, int w, int h)
        {
            Ctl c = new Ctl(); c.K = k; c.Id = id; c.Label = label; c.X = x; c.Y = y; c.W = w; c.H = h; R.C.Add(c); return c;
        }
        public Ctl Frame(string id, string title, int x, int y, int w, int h) { Ctl c = N(Kind.Frame, id, title, x, y, w, h); c.Local = true; return c; }
        public Ctl Text(string id, string title, int x, int y, int w, int h) { Ctl c = N(Kind.Text, id, title, x, y, w, h); c.Local = true; return c; }
        // bascule : set = commande avec {v}, read = lecture, parse = réponse (groupe v)
        public Ctl Tog(string id, string label, int x, int y, int w, int h, string set, string read, string parse) { return Tog(id, label, x, y, w, h, set, read, parse, "1", "0"); }
        public Ctl Tog(string id, string label, int x, int y, int w, int h, string set, string read, string parse, string on, string off)
        {
            Ctl c = N(Kind.Toggle, id, label, x, y, w, h); c.Set = set; c.Read = read; c.Parse = parse; c.OnVal = on; c.OffVal = off; return c;
        }
        public Ctl Act(string id, string label, int x, int y, int w, int h, string set) { Ctl c = N(Kind.Action, id, label, x, y, w, h); c.Set = set; return c; }
        public Ctl Knob(string id, string label, int x, int y, int w, int h, int min, int max, string set, string read, string parse, string fmt)
        {
            Ctl c = N(Kind.Knob, id, label, x, y, w, h); c.Min = min; c.Max = max; c.Set = set; c.Read = read; c.Parse = parse; c.Fmt = fmt; return c;
        }
        public Ctl Cho(string id, string label, int x, int y, int w, int h, string items, string set, string read, string parse)
        {
            Ctl c = N(Kind.Choice, id, label, x, y, w, h); c.Items = items; c.Set = set; c.Read = read; c.Parse = parse; return c;
        }
        public Ctl Row(string id, string label, int x, int y, int w, int h, int rows, string items, string set, string read, string parse)
        {
            Ctl c = Cho(id, label, x, y, w, h, items, set, read, parse); c.Row = true; c.Rows = rows; return c;
        }
        public Ctl Freq(string id, string label, int x, int y, int w, int h, string set, string read, string parse, string modeId, string accent)
        {
            Ctl c = N(Kind.Freq, id, label, x, y, w, h); c.Set = set; c.Read = read; c.Parse = parse; c.Ref = modeId; c.Accent = accent; c.Period = 160; return c;
        }
        public Ctl Meter(string id, string label, int x, int y, int w, int h, int max, string read, string parse, string fmt, bool txOnly)
        {
            Ctl c = N(Kind.Meter, id, label, x, y, w, h); c.Max = max; c.Read = read; c.Parse = parse; c.Fmt = fmt; c.TxOnly = txOnly; c.Period = txOnly ? 140 : 130; return c;
        }
        public Ctl Led(string id, string label, int x, int y, int w, int h, string src, string on, string accent)
        {
            Ctl c = N(Kind.Led, id, label, x, y, w, h); c.Local = true; c.Ref = src; c.OnVal = on; c.Accent = accent; return c;
        }
        public Ctl LedRd(string id, string label, int x, int y, int w, int h, string read, string parse, string accent)
        {
            Ctl c = N(Kind.Led, id, label, x, y, w, h); c.Read = read; c.Parse = parse; c.Accent = accent; c.Period = 500; return c;
        }
        public Ctl Gauge(string id, string label, int x, int y, int w, int h) { Ctl c = N(Kind.Gauge, id, label, x, y, w, h); c.Local = true; return c; }
        public Ctl Chain(string id, string label, int x, int y, int w, int h, string side) { Ctl c = N(Kind.Chain, id, label, x, y, w, h); c.Local = true; c.Ref = side; return c; }
        public Ctl Dsp(string id, string label, int x, int y, int w, int h, string side) { Ctl c = N(Kind.Dsp, id, label, x, y, w, h); c.Local = true; c.Ref = side; return c; }
        public Ctl Dial(string id, string label, int x, int y, int w, int h, string freqId, string stepId)
        {
            Ctl c = N(Kind.Dial, id, label, x, y, w, h); c.Local = true; c.Ref = freqId; c.Items = stepId; return c;
        }
        // finalise : les valeurs actuelles deviennent les valeurs par défaut
        public void Fin()
        {
            foreach (Ctl c in R.C)
            {
                c.DSet = c.Set; c.DRead = c.Read; c.DParse = c.Parse; c.DItems = c.Items; c.DOnVal = c.OnVal; c.DOffVal = c.OffVal; c.DMin = c.Min; c.DMax = c.Max; c.DLabel = c.Label;
                c.DX = c.X; c.DY = c.Y; c.DW = c.W; c.DH = c.H;
            }
        }
    }

    public static partial class Radios
    {
        static List<RadioDef> Base()
        {
            List<RadioDef> l = new List<RadioDef>(); l.Add(Ft2000()); l.Add(Ftdx5000()); l.Add(Ftdx10()); l.Add(Ft450()); l.Add(Ft710()); l.Add(Ft891()); l.Add(Ft950()); l.Add(Ft991a()); l.Add(Ftdx101()); l.Add(Ftdx3000()); l.Add(Ftdx1200()); l.Add(Ftdx9000()); l.Add(Ftx1()); l.Add(Ts480()); l.Add(Ts890()); l.Add(Ts590()); l.Add(Ts990()); l.Add(Ts2000()); l.Add(Tsb2000()); l.Add(Ts870()); l.Add(Ts570()); l.Add(Ts850()); l.Add(Ts50()); l.Add(Ts60()); l.Add(Ts450()); l.Add(Ts690()); l.Add(Ts790()); l.Add(Ts930()); l.Add(Ts940()); l.Add(Ts950()); l.Add(Ts950sdx()); l.Add(Ts440()); l.Add(Ts140()); l.Add(Ts680()); l.Add(Ts711()); l.Add(Ts811()); l.Add(K4()); l.Add(K3()); l.Add(Kx3()); l.Add(Kx2()); l.Add(K2()); return l;
        }
                // Section « alimentation / émission » : la touche ÉMISSION (PTT) gagne en HAUTEUR (pas en largeur) ; les touches situées sous elle descendent
        // d'autant, sans changer de forme, pour occuper l'espace libre en bas du cadre ; l'espace restant en bas égale la marge de gauche (disposition symétrique).
        // Idempotent : une seconde application ne change plus rien.
        public static void Harmonize(RadioDef r) { Harmonize0(r); AddTune(r); }
        // Bouton TUNE (local, sans commande CAT) ajouté à tous les postes : 190 x 62 (taille de VOX SOURCE du FT-2000), le plus bas et le plus à droite possible sans recouvrir un autre bouton
        static void AddTune(RadioDef r)
        {
            if (r.Find("tunemic") != null) return;
            const int w = 190, h = 62; int px = -1, py = -1;
            for (int y = 980 - h - 6; px < 0 && y >= 124; y -= 6) for (int x = 1860 - w - 6; x >= 6; x -= 6) if (TuneFree(r, x, y, w, h)) { px = x; py = y; break; }
            if (px < 0) { px = 1860 - w - 6; py = 980 - h - 6; }
            Ctl t = new Ctl(); t.K = Kind.Action; t.Id = "tunemic"; t.Label = t.DLabel = "TUNE"; t.Local = true; t.X = t.DX = px; t.Y = t.DY = py; t.W = t.DW = w; t.H = t.DH = h;
            t.DSet = t.DRead = t.DParse = t.DItems = ""; t.DOnVal = "1"; t.DOffVal = "0";
            t.Tip = "Tonalité de 1500 Hz sur la carte son d'émission du poste, avec PTT (ou VOX). Réglages : Paramètres ⚙ > Bouton TUNE."; r.C.Add(t);
        }
        static bool TuneFree(RadioDef r, int x, int y, int w, int h)
        {
            foreach (Ctl c in r.C) if (c.K != Kind.Frame && !c.Hidden && x < c.X + c.W + 2 && x + w + 2 > c.X && y < c.Y + c.H + 2 && y + h + 2 > c.Y) return false;
            return true;
        }
        public static void Harmonize0(RadioDef r)
        {
            Ctl p = r.Find("ptt"); if (p == null || p.K != Kind.Toggle) return; Ctl fr = null;
            foreach (Ctl c in r.C) if (c.K == Kind.Frame && c.X <= p.X && c.Y <= p.Y && c.X + c.W >= p.X + p.W && c.Y + c.H >= p.Y + p.H && (fr == null || c.W * c.H < fr.W * fr.H)) fr = c;
            if (fr == null) return;
            List<Ctl> inner = new List<Ctl>(); int side = int.MaxValue, maxBottom = 0;
            foreach (Ctl c in r.C) if (c != fr && c.K != Kind.Frame && !c.Hidden && c.X >= fr.X && c.Y >= fr.Y && c.X + c.W <= fr.X + fr.W && c.Y + c.H <= fr.Y + fr.H) { inner.Add(c); side = Math.Min(side, c.X - fr.X); maxBottom = Math.Max(maxBottom, c.Y + c.H); }
            side = Math.Max(8, Math.Min(side, 20)); int delta = fr.Y + fr.H - side - maxBottom; if (delta < 6) return; delta = Math.Min(delta, 80);
            int pb = p.Y + p.H; foreach (Ctl c in inner) if (c != p && c.Y >= pb) { c.Y += delta; c.DY += delta; }
            p.H += delta; p.DH += delta;
        }
        public static List<RadioDef> All() { List<RadioDef> l = Base(); foreach (IM m in IcomModels()) l.Add(Icom(m)); l.AddRange(Y5All()); l.AddRange(LegAll()); l.Add(TciDef()); l.Sort(delegate(RadioDef a, RadioDef b) { return NatCmp(a.Name, b.Name); }); foreach (RadioDef x in l) Harmonize(x); return l; }
        // ordre alphabétique « naturel » : marque, puis lettres, puis numéros dans l'ordre des nombres (IC-705 avant IC-7000, FT-450D avant FT-2000)
        public static int NatCmp(string a, string b)
        {
            int i = 0, j = 0;
            while (i < a.Length && j < b.Length)
            {
                if (char.IsDigit(a[i]) && char.IsDigit(b[j]))
                {
                    int si = i, sj = j; while (i < a.Length && char.IsDigit(a[i])) i++; while (j < b.Length && char.IsDigit(b[j])) j++;
                    long x = long.Parse(a.Substring(si, Math.Min(i - si, 15))), y = long.Parse(b.Substring(sj, Math.Min(j - sj, 15))); if (x != y) return x < y ? -1 : 1;
                }
                else { int c = char.ToUpperInvariant(a[i]).CompareTo(char.ToUpperInvariant(b[j])); if (c != 0) return c; i++; j++; }
            }
            return (a.Length - i).CompareTo(b.Length - j);
        }
        public static RadioDef ByKey(string k) { RadioDef x = ByKey0(k); if (x != null) Harmonize(x); return x; }
        static RadioDef ByKey0(string k) { RadioDef i = IcomByKey(k); if (i != null) return i; i = Y5ByKey(k); if (i != null) return i; i = LegByKey(k); if (i != null) return i; if (k == "tci") return TciDef(); foreach (RadioDef r in Base()) if (r.Key == k) return r; return null; }

        const string BANDS = "1.8=00,3.5=01,5=02,7=03,10=04,14=05,18=06,21=07,24.5=08,28=09,50=10,GEN=11";
        public static readonly long[] BandLo = { 1800000, 3500000, 5250000, 7000000, 10100000, 14000000, 18068000, 21000000, 24890000, 28000000, 50000000 };
        public static readonly long[] BandHi = { 2000000, 3999999, 5499999, 7299999, 10149999, 14349999, 18168000, 21449999, 24989999, 29699999, 54000000 };
        public static string BandCode(long f) { for (int i = 0; i < BandLo.Length; i++) if (f >= BandLo[i] && f <= BandHi[i]) return i.ToString("00"); return "11"; }

        // =====================================================================================  FTdx10
        // Source : « FTDX10 CAT Operation Reference Manual » 2308-F (E:\...\Yeasu\FTdx10\FTDX10_CAT_OM_ENG_2308-F.pdf)
        static RadioDef Ftdx10()
        {
            RadioDef r = new RadioDef(); r.Key = "ftdx10"; r.Name = "Yaesu FTdx10"; r.IdAnswer = "ID0761;"; r.DefBaud = 38400; r.FreqDigits = 9; r.MaxFreq = 75000000;
            Bld b = new Bld(r); Ctl c;
            const string MODES = "LSB=1,USB=2,CW-U=3,FM=4,AM=5,RTTY-L=6,CW-L=7,DATA-L=8,RTTY-U=9,DATA-FM=A,FM-N=B,DATA-U=C,AM-N=D,PSK=E,DATA-FM-N=F";
            // IF : mémoire (3), fréquence A (9), décalage clarifier signé (5), RX clar, TX clar, mode, VFO/mém., CTCSS, 00, décalage relais
            const string IF = @"^IF(?<mch>.{3})(?<fa>\d{9})(?<v>[+-]\d{4})(?<clarrx>\d)(?<clartx>\d)[0-9A-F](?<vm>\d)(?<ct>\d)\d\d(?<os>\d);$";

            // ---- alimentation / émission
            b.Frame("f_pwr", "ALIMENTATION / ÉMISSION", 10, 124, 210, 430);
            c = b.Act("onoff", "ON", 24, 152, 118, 42, "PS1;{wait:1100}PS1;"); b.Act("poff", "OFF", 150, 152, 56, 42, "PS0;"); c.Tip = "Allumage : la doc demande un premier PS1; puis, entre 1 et 2 s plus tard, un second. L'extinction se fait avec PS0; (clic droit pour modifier).";
            b.Tog("tuner", "TUNE", 24, 202, 182, 42, "AC00{v};", "AC;", @"^AC00(?<v>[0-2]);$", "1|2", "0").Accent = "amber";
            b.Act("tunego", "TUNE START", 24, 252, 182, 36, "AC002;");
            b.Tog("vox", "VOX", 24, 298, 86, 44, "VX{v};", "VX;", @"^VX(?<v>[01]);$");
            b.Tog("mox", "MOX", 120, 298, 86, 44, "MX{v};", "MX;", @"^MX(?<v>[01]);$").Accent = "red";
            c = b.Tog("ptt", "ÉMISSION (PTT)", 24, 350, 182, 54, "TX{v};", "TX;", @"^TX(?<v>[0-2]);$", "1|2", "0"); c.Accent = "red"; c.Period = 250;
            b.Tog("moni", "MONI", 24, 412, 86, 40, "ML0{v};", "ML0;", @"^ML0(?<v>\d{3});$", "001", "000");
            c = b.Tog("proc", "PROC", 120, 412, 86, 40, "PR0{v};", "PR0;", @"^PR0(?<v>[0-2]);$", "1", "0"); c.Hint = "Le manuel CAT FTdx10 donne PR P2 = 1 (OFF) / 2 (ON), mais l'essai sur un vrai FTdx10 a validé PR01 = marche et PR00 = arrêt. Si le bouton agit à l'envers sur votre poste, mettez allumé = 2 et éteint = 1 ici.";
            b.Tog("lock", "LOCK", 24, 460, 86, 40, "LK{v};", "LK;", @"^LK(?<v>[01]);$");
            b.Tog("fine", "FINE", 120, 460, 86, 40, "FN{v};", "FN;", @"^FN(?<v>[01]);$");

            // ---- écran : fréquences, mesures, boutons tactiles
            b.Frame("f_scr", "ÉCRAN", 230, 124, 1010, 430);
            b.Freq("fa", "VFO-A", 244, 150, 640, 150, "FA{v:9};", "FA;", @"^FA(?<v>\d{9});$", "md0", "amber").Hint = "9 chiffres en Hz (manuel CAT FTdx10 p.13, FA P1 = 000030000 à 075000000).";
            b.Freq("fb", "VFO-B", 894, 150, 336, 150, "FB{v:9};", "FB;", @"^FB(?<v>\d{9});$", "md1", "blue");
            b.Meter("sm", "S", 244, 310, 480, 34, 255, "SM0;", @"^SM0(?<v>\d{3});$", "s", false);
            b.Meter("po", "Po", 244, 348, 480, 34, 255, "RM5;", @"^RM5(?<v>\d{3})000;$", "pct", true);
            b.Meter("swr", "SWR", 244, 386, 480, 34, 255, "RM6;", @"^RM6(?<v>\d{3})000;$", "pct", true);
            b.Meter("vdd", "Vdd", 244, 424, 480, 34, 255, "RM8;", @"^RM8(?<v>\d{3})000;$", "pct", true);
            b.Meter("alc", "ALC", 736, 310, 494, 34, 255, "RM4;", @"^RM4(?<v>\d{3})000;$", "pct", true);
            b.Meter("comp", "COMP", 736, 348, 494, 34, 255, "RM3;", @"^RM3(?<v>\d{3})000;$", "pct", true);
            b.Meter("idd", "Id", 736, 386, 494, 34, 255, "RM7;", @"^RM7(?<v>\d{3})000;$", "pct", true);
            b.LedRd("l_busy", "BUSY", 744, 430, 110, 26, "BY;", @"^BY(?<v>[01])0;$", "green");
            b.Led("l_tx", "TX", 862, 430, 110, 26, "ptt", "1|2", "red");
            b.Led("l_mem", "MEM", 980, 430, 110, 26, "vm", "1|2|3", "amber");
            b.Led("l_split", "SPLIT", 1098, 430, 110, 26, "split", "1|2", "cyan");
            b.Cho("att", "ATT", 244, 470, 150, 76, "OFF=0,6 dB=1,12 dB=2,18 dB=3", "RA0{v};", "RA0;", @"^RA0(?<v>[0-3]);$");
            b.Cho("ipo", "IPO", 400, 470, 150, 76, "IPO=0,AMP 1=1,AMP 2=2", "PA0{v};", "PA0;", @"^PA0(?<v>[0-2]);$");
            c = b.Cho("rfil", "R.FIL", 556, 470, 150, 76, "12 kHz=1|6,3 kHz=2|7,500 Hz=4|9,300 Hz=5|A", "RF0{v};", "RF0;", @"^RF0(?<v>[1-9A]);$");
            c.Hint = "Manuel : RF P2 = 1:12 kHz, 2:3 kHz, 4:500 Hz, 5:300 Hz (option) ; la réponse peut utiliser 6 à A. À vérifier sur le poste.";
            b.Cho("agc", "AGC", 712, 470, 150, 76, "AUTO=4|5|6,FAST=1,MID=2,SLOW=3,OFF=0", "GT0{v};", "GT0;", @"^GT0(?<v>[0-6]);$");
            b.Tog("narrow", "NAR", 868, 470, 100, 76, "NA0{v};", "NA0;", @"^NA0(?<v>[01]);$");
            b.Cho("md1", "MODE VFO-B", 974, 470, 256, 76, MODES, "MD1{v};", "MD1;", @"^MD1(?<v>[0-9A-F]);$");

            // ---- touches VFO / mémoires
            b.Frame("f_vfo", "VFO / MÉMOIRES", 1250, 124, 600, 430);
            const int rx = 1264; const int bw = 108;
            b.Act("vm", "V/M", rx, 152, bw, 40, "VM;");
            b.Act("swap", "A/B", rx + 116, 152, bw, 40, "SV;");
            b.Act("a2b", "A=B", rx + 232, 152, bw, 40, "AB;");
            b.Act("b2a", "B=A", rx + 348, 152, bw, 40, "BA;");
            b.Row("vs", "VFO ACTIF", rx + 464, 152, 108, 40, 1, "A=0,B=1", "VS{v};", "VS;", @"^VS(?<v>[01]);$");
            b.Tog("clarrx", "CLAR RX", rx, 200, bw, 40, "RT{v};", "RT;", @"^RT(?<v>[01]);$").Period = 6000;
            b.Tog("clartx", "CLAR TX", rx + 116, 200, bw, 40, "XT{v};", "XT;", @"^XT(?<v>[01]);$").Period = 6000;
            b.Act("clr", "CLAR CLEAR", rx + 232, 200, bw, 40, "RC;");
            b.Tog("split", "SPLIT", rx + 348, 200, bw, 40, "ST{v};", "ST;", @"^ST(?<v>[0-2]);$", "1|2", "0").Accent = "cyan";
            b.Tog("txw", "TXW", rx + 464, 200, 108, 40, "TS{v};", "TS;", @"^TS(?<v>[01]);$");
            b.Act("qmb", "QMB", rx, 248, bw, 40, "QR;");
            b.Act("qmbs", "QMB STO", rx + 116, 248, bw, 40, "QI;");
            b.Act("qs", "Q.SPLIT", rx + 232, 248, bw, 40, "QS;");
            b.Act("mchu", "MCH ▲", rx + 348, 248, bw, 40, "CH0;");
            b.Act("mchd", "MCH ▼", rx + 464, 248, 108, 40, "CH1;");
            b.Act("bu", "BAND ▲", rx, 296, bw, 40, "BU0;");
            b.Act("bd", "BAND ▼", rx + 116, 296, bw, 40, "BD0;");
            b.Act("zin", "ZIN", rx + 232, 296, bw, 40, "ZI0;");
            b.Tog("cs", "C.S SPOT", rx + 348, 296, bw, 40, "CS{v};", "CS;", @"^CS(?<v>[01]);$");
            b.Tog("dnf", "DNF", 1446, 764, 110, 44, "BC0{v};", "BC0;", @"^BC0(?<v>[01]);$");
            b.Act("ky1", "KY 1", rx, 344, 70, 36, "KY1;"); b.Act("ky2", "KY 2", rx + 76, 344, 70, 36, "KY2;"); b.Act("ky3", "KY 3", rx + 152, 344, 70, 36, "KY3;");
            b.Act("ky4", "KY 4", rx + 228, 344, 70, 36, "KY4;"); b.Act("ky5", "KY 5", rx + 304, 344, 70, 36, "KY5;");
            b.Act("pb1", "VOIX 1", rx, 388, 70, 36, "PB01;"); b.Act("pb2", "VOIX 2", rx + 76, 388, 70, 36, "PB02;"); b.Act("pb3", "VOIX 3", rx + 152, 388, 70, 36, "PB03;");
            b.Act("pb4", "VOIX 4", rx + 228, 388, 70, 36, "PB04;"); b.Act("pb5", "VOIX 5", rx + 304, 388, 70, 36, "PB05;"); b.Act("pb0", "STOP", rx + 380, 388, 70, 36, "PB00;");
            b.Tog("kr", "KEYER", rx, 432, bw, 40, "KR{v};", "KR;", @"^KR(?<v>[01]);$");
            b.Tog("bi", "BK-IN", rx + 116, 432, bw, 40, "BI{v};", "BI;", @"^BI(?<v>[01]);$");
            c = b.Knob("ks", "SPEED", rx + 236, 428, 70, 80, 4, 60, "KS{v:3};", "KS;", @"^KS(?<v>\d{3});$", "wpm");
            c = b.Knob("kp", "PITCH", rx + 312, 428, 70, 80, 0, 75, "KP{v:2};", "KP;", @"^KP(?<v>\d{2});$", "pitch10");

            // ---- bande et mode
            b.Frame("f_mode", "BANDE / MODE (VFO-A)", 10, 564, 830, 190);
            c = b.Row("band", "BANDE", 24, 590, 802, 52, 1, BANDS, "BS{v};", "", ""); c.Hint = "Manuel : BS P1 = 00 (1,8 MHz) à 11 (GEN). Pas de lecture : la bande allumée vient de la fréquence.";
            b.Row("md0", "MODE", 24, 650, 802, 94, 2, MODES, "MD0{v};", "MD0;", @"^MD0(?<v>[0-9A-F]);$").Period = 1500;

            // ---- molette principale
            b.Frame("f_dial", "ACCORD", 850, 564, 340, 406);
            b.Dial("dial", "VFO-A", 870, 592, 300, 300, "fa", "step");
            c = b.Cho("step", "PAS", 870, 900, 300, 56, "1 Hz=1,10 Hz=10,100 Hz=100,500 Hz=500,1 kHz=1000,5 kHz=5000,10 kHz=10000", "", "", ""); c.Local = true;
            c.Items = "1 Hz=1,10 Hz=10,100 Hz=100,500 Hz=500,1 kHz=1000,5 kHz=5000,10 kHz=10000";

            // ---- réception
            b.Frame("f_rx", "RÉCEPTION / ÉMISSION", 10, 764, 830, 206);
            c = b.Knob("af", "AF", 24, 796, 94, 96, 0, 255, "AG0{v:3};", "AG0;", @"^AG0(?<v>\d{3});$", "pct"); c.Accent = "green";
            b.Knob("rf", "RF", 124, 796, 94, 96, 0, 255, "RG0{v:3};", "RG0;", @"^RG0(?<v>\d{3});$", "pct");
            b.Knob("sql", "SQL", 224, 796, 94, 96, 0, 100, "SQ0{v:3};", "SQ0;", @"^SQ0(?<v>\d{3});$", "raw");
            b.Knob("mic", "MIC", 324, 796, 94, 96, 0, 100, "MG{v:3};", "MG;", @"^MG(?<v>\d{3});$", "raw").Accent = "amber";
            c = b.Knob("pwr", "PWR", 424, 796, 94, 96, 5, 100, "PC{v:3};", "PC;", @"^PC(?<v>\d{3});$", "watt"); c.Accent = "red";
            b.Knob("procl", "PROC", 524, 796, 94, 96, 0, 100, "PL{v:3};", "PL;", @"^PL(?<v>\d{3});$", "raw").Accent = "amber";
            b.Knob("monil", "MONI", 624, 796, 94, 96, 0, 100, "ML1{v:3};", "ML1;", @"^ML1(?<v>\d{3});$", "raw");
            b.Knob("voxg", "VOX", 724, 796, 94, 96, 0, 100, "VG{v:3};", "VG;", @"^VG(?<v>\d{3});$", "raw");
            b.Tog("nb", "NB", 24, 906, 94, 52, "NB0{v};", "NB0;", @"^NB0(?<v>[01]);$");
            b.Tog("dnr", "DNR", 124, 906, 94, 52, "NR0{v};", "NR0;", @"^NR0(?<v>[01]);$");
            c = b.Knob("nbl", "NB niv.", 224, 902, 94, 66, 0, 10, "NL0{v:3};", "NL0;", @"^NL0(?<v>\d{3});$", "raw");
            c = b.Knob("dnrl", "DNR niv.", 324, 902, 94, 66, 1, 15, "RL0{v:2};", "RL0;", @"^RL0(?<v>\d{2});$", "raw");

            // ---- DSP
            b.Frame("f_dsp", "DSP / FILTRES", 1200, 564, 650, 406);
            c = b.Knob("shift", "SHIFT", 1214, 592, 110, 112, -1200, 1200, "IS00{v:+4};", "IS0;", @"^IS00(?<v>[+-]\d{4});$", "hz"); c.Step = 20; c.Hint = "IS P3 = signe, P4 = 0 à 1200 Hz par pas de 20 Hz.";
            c = b.Knob("width", "WIDTH", 1330, 592, 110, 112, 0, 23, "SH00{v:2};", "SH0;", @"^SH00(?<v>\d{2});$", "w"); c.Hint = "SH P3 = 00 à 23 : numéro de largeur (tableau 3 du manuel), pas des Hz.";
            c = b.Knob("notchf", "NOTCH", 1446, 592, 110, 112, 1, 320, "BP01{v:3};", "BP01;", @"^BP01(?<v>\d{3});$", "x10hz");
            c = b.Knob("contf", "CONT", 1562, 592, 110, 112, 10, 3200, "CO01{v:4};", "CO01;", @"^CO01(?<v>\d{4});$", "hz"); c.Step = 10;
            c = b.Knob("apff", "APF", 1678, 592, 110, 112, 0, 50, "CO03{v:4};", "CO03;", @"^CO03(?<v>\d{4});$", "apf");
            b.Tog("notch", "NOTCH", 1214, 716, 110, 44, "BP00{v:3};", "BP00;", @"^BP00(?<v>\d{3});$", "001", "000");
            b.Tog("cont", "CONT", 1330, 716, 110, 44, "CO00{v:4};", "CO00;", @"^CO00(?<v>\d{4});$", "0001", "0000");
            b.Tog("apf", "APF", 1446, 716, 110, 44, "CO02{v:4};", "CO02;", @"^CO02(?<v>\d{4});$", "0001", "0000");
            b.Knob("vd", "VOX DLY", rx + 388, 428, 70, 80, 0, 33, "VD{v:2};", "VD;", @"^VD(?<v>\d{2});$", "raw").Hint = "Délai VOX : numéro 00 à 33 (00 = 30 ms ... 33 = 3000 ms), 2 chiffres comme validé sur un vrai FTdx10 (le manuel imprime 4 chiffres).";
            b.Knob("sd", "BK DLY", rx + 464, 428, 70, 80, 0, 33, "SD{v:2};", "SD;", @"^SD(?<v>\d{2});$", "raw").Hint = "Délai break-in CW : numéro 00 à 33 (33 = 3000 ms), 2 chiffres (format validé sur un vrai FTdx10, bornes du manuel).";
            c = b.Cho("meter", "METER", 1214, 856, 190, 66, "PO=0,COMP=1,ALC=2,VDD=3,ID=4,SWR=5", "MS{v}0;", "MS;", @"^MS(?<v>[0-5])0;$"); c.Hint = "Commutateur METER du poste (MS P1 P2 : 0 PO, 1 COMP, 2 ALC, 3 VDD, 4 ID, 5 SWR). Les mesures affichées ici viennent de RM et ne dépendent pas de ce réglage.";
            c = b.Knob("clar", "CLAR", 1562, 712, 110, 108, -9990, 9990, "CF001{v:+4};", "IF;", IF, "hz"); c.Step = 10; c.Period = 300; c.Hint = "Le décalage est lu avec IF; (réponse complète, 28 caractères) qui met aussi à jour fréquence A, mode A, CLAR RX/TX et V/M.";
            b.Fin(); return r;
        }

        // =====================================================================================  FT-2000
        // Source : « FT-2000 CAT Operation Reference Book » EH025H121 (E:\...\Yeasu\FT2000\FT-2000_CAT_Operation_reference_book.zip)
        static RadioDef Ft2000() { return Dual(false); }
        // =====================================================================================  FTdx5000 (même famille que le FT-2000 : deux récepteurs)
        // Source : « FTDX5000 Series CAT Operation Reference Book » EH036H121
        static RadioDef Dual(bool x5)
        {
            RadioDef r = new RadioDef(); r.Key = x5 ? "ftdx5000" : "ft2000"; r.Name = x5 ? "Yaesu FTdx5000 / FTdx5000D / MP" : "Yaesu FT-2000 / FT-2000D"; r.IdAnswer = x5 ? "ID0362;" : "ID0251;"; r.DefBaud = 4800; r.FreqDigits = 8; r.MaxFreq = 60000000;
            Bld b = new Bld(r); Ctl c;
            const string MODES = "LSB=1,USB=2,CW=3,FM=4,AM=5,RTTY-L=6,CW-R=7,PKT-L=8,RTTY-U=9,PKT-FM=A,FM-N=B,PKT-U=C";
            // IF : mémoire (3), fréquence A (8), décalage clarifier signé (5), RX clar, TX clar, mode, VFO/mém., CTCSS, 00, décalage relais
            const string IF = @"^IF(?<mch>\d{3})(?<fa>\d{8})(?<v>[+-]\d{4})(?<clarrx>\d)(?<clartx>\d)[0-9A-F](?<vm>\d)(?<ct>\d)\d\d(?<os>\d);$";

            b.Frame("f_pwr", "ALIMENTATION / ÉMISSION", 10, 124, 210, 430);
            b.Act("poff", "OFF", 150, 152, 56, 42, "PS0;"); b.Act("onoff", "ON", 24, 152, 118, 42, "PS1;").Tip = "FT-2000 : PS1; allume (le FP-2000 doit être sur I), PS0; éteint.";
            b.Tog("tuner", "TUNE", 24, 202, 182, 42, "AC00{v};", "AC;", @"^AC00(?<v>[0-2]);$", "1|2", "0").Accent = "amber";
            b.Act("tunego", "TUNE START", 24, 252, 182, 36, "AC002;");
            b.Tog("vox", "VOX", 24, 298, 86, 44, "VX{v};", "VX;", @"^VX(?<v>[01]);$");
            b.Tog("mox", "MOX", 120, 298, 86, 44, "MX{v};", "MX;", @"^MX(?<v>[01]);$").Accent = "red";
            c = b.Tog("ptt", "ÉMISSION (PTT)", 24, 350, 182, 54, "TX{v};", "TX;", @"^TX(?<v>[0-2]);$", "1|2", "0"); c.Accent = "red"; c.Period = 250;
            b.Tog("moni", "MONI", 24, 412, 86, 40, "ML0{v};", "ML0;", @"^ML0(?<v>\d{3});$", "001", "000");
            b.Tog("proc", "PROC", 120, 412, 86, 40, "PR{v};", "PR;", @"^PR(?<v>[01]);$");
            if (x5)
            {
                c = b.Tog("lock", "LOCK", 24, 460, 86, 40, "LK{v};", "LK;", @"^LK(?<v>[0-7]);$", "1|5|7", "0|4|6"); c.Hint = "FTdx5000 : LK0/LK1 = verrouillage VFO-A (LK2/LK3 = VFO-B) ; la réponse vaut 4 à 7 (5 et 7 = VFO-A verrouillé).";
                c = b.Tog("fast", "FAST", 120, 460, 86, 40, "FS{v};", "FS;", @"^FS(?<v>[0-7]);$", "1|5|7", "0|4|6"); c.Hint = "FTdx5000 : FS0/FS1 = pas rapide VFO-A (FS2/FS3 = VFO-B) ; la réponse vaut 4 à 7 (5 et 7 = VFO-A rapide).";
            }
            else
            {
                b.Tog("lock", "LOCK", 24, 460, 86, 40, "LK{v};", "LK;", @"^LK(?<v>[01]);$");
                b.Tog("fast", "FAST", 120, 460, 86, 40, "FS{v};", "FS;", @"^FS(?<v>[01]);$");
            }
            b.Tog("dim", "DIM", 24, 506, 86, 40, "DS{v};", "DS;", @"^DS(?<v>[01]);$");
            if (x5) { c = b.Cho("ant", "ANT", 120, 506, 86, 40, "ANT 1=1,ANT 2=2,ANT 3=3,ANT 4=4", "AN0{v};", "AN0;", @"^AN0(?<v>[1-4])(?<rxant>[01]);$"); c.Hint = "FTdx5000 : AN P1 (0 = main) P2 = 1 à 4 ; AN05 = antenne RX. La réponse AN0 P3 P4 donne l'antenne (P3) et l'état de l'antenne RX (P4)."; }
            else b.Cho("ant", "ANT", 120, 506, 86, 40, "ANT 1=1,ANT 2=2", "AN0{v};", "AN0;", @"^AN0(?<v>[12])[01];$");

            b.Frame("f_scr", "ÉCRAN", 230, 124, 1010, 430);
            b.Freq("fa", "MAIN (VFO-A)", 244, 150, 560, 150, "FA{v:8};", "FA;", @"^FA(?<v>\d{8});$", "md0", "amber").Hint = "8 chiffres en Hz (manuel CAT FT-2000 : FA14250000;).";
            b.Freq("fb", "SUB (VFO-B)", 814, 150, 416, 150, "FB{v:8};", "FB;", @"^FB(?<v>\d{8});$", "md1", "blue");
            b.Meter("sm", "S MAIN", 244, 310, 480, 34, 255, "SM0;", @"^SM0(?<v>\d{3});$", "s", false);
            b.Meter("sm1", "S SUB", 244, 348, 480, 34, 255, "SM1;", @"^SM1(?<v>\d{3});$", "s", false).Accent = "blue";
            b.Meter("po", "Po", 244, 386, 480, 34, 255, "RM5;", @"^RM5(?<v>\d{3});$", "pct", true);
            b.Meter("swr", "SWR", 244, 424, 480, 34, 255, "RM6;", @"^RM6(?<v>\d{3});$", "pct", true);
            b.Meter("alc", "ALC", 736, 310, 494, 34, 255, "RM4;", @"^RM4(?<v>\d{3});$", "pct", true);
            b.Meter("comp", "COMP", 736, 348, 494, 34, 255, "RM3;", @"^RM3(?<v>\d{3});$", "pct", true);
            b.Meter("idd", "Id", 736, 386, 494, 34, 255, "RM7;", @"^RM7(?<v>\d{3});$", "pct", true);
            b.Meter("vdd", "Vdd", 736, 424, 494, 34, 255, "RM8;", @"^RM8(?<v>\d{3});$", "pct", true);
            c = b.LedRd("l_busy", "BUSY A", 244, 462, 100, 22, "BY;", @"^BY(?<v>[01])(?<busyb>[01]);$", "green"); c.Hint = "BY : silencieux ouvert main (P1) et sub (P2).";
            b.Led("l_busyb", "BUSY B", 352, 462, 100, 22, "busyb", "1", "green");
            b.Led("l_tx", "TX", 460, 462, 90, 22, "ptt", "1|2", "red");
            b.Led("l_mem", "MEM", 558, 462, 90, 22, "vm", "1|2|3|4", "amber");
            b.Led("l_split", "SPLIT", 656, 462, 90, 22, "split", "1", "cyan");
            if (x5) b.Led("l_rxant", "ANT RX", 754, 462, 90, 22, "rxant", "1", "amber");
            b.Cho("att", "ATT", 244, 490, 120, 58, "OFF=0,6 dB=1,12 dB=2,18 dB=3", "RA0{v};", "RA0;", @"^RA0(?<v>[0-3]);$");
            if (x5) { c = b.Cho("ipo", "IPO / AMP", 370, 490, 120, 58, "IPO 1=0,AMP 1=1,AMP 2=2,IPO 2=3", "PA0{v};", "PA0;", @"^PA0(?<v>[0-3]);$"); c.Hint = "FTdx5000 : PA P2 = 0 IPO 1, 1 AMP 1, 2 AMP 2, 3 IPO 2."; }
            else b.Cho("ipo", "IPO", 370, 490, 120, 58, "IPO=0,AMP 1=1,AMP 2=2", "PA0{v};", "PA0;", @"^PA0(?<v>[0-2]);$");
            if (x5) { c = b.Cho("rfil", "R.FLT", 496, 490, 120, 58, "AUTO=0|4|5|6|9|A,15 kHz=1,6 kHz=2,3 kHz=3,600 Hz=4|7,300 Hz=5|8", "RF0{v};", "RF0;", @"^RF0(?<v>[0-9A]);$"); c.Hint = "FTdx5000 : envoi 0 AUTO, 1 15 kHz, 2 6 kHz, 3 3 kHz, 4 600 Hz, 5 300 Hz (main seulement). La réponse utilise une autre table : 1-3 fixes, 4-6 AUTO 15/6/3 kHz, 7 600 Hz, 8 300 Hz, 9/A AUTO 600/300 Hz."; }
            else b.Cho("rfil", "R.FLT", 496, 490, 120, 58, "AUTO=0|4|5|6,15 kHz=1,6 kHz=2,3 kHz=3", "RF0{v};", "RF0;", @"^RF0(?<v>[0-6]);$");
            b.Cho("agc", "AGC A", 622, 490, 120, 58, "AUTO=4|5|6,FAST=1,MID=2,SLOW=3,OFF=0", "GT0{v};", "GT0;", @"^GT0(?<v>[0-6]);$");
            b.Cho("agcb", "AGC B", 748, 490, 120, 58, "AUTO=4|5|6,FAST=1,MID=2,SLOW=3,OFF=0", "GT1{v};", "GT1;", @"^GT1(?<v>[0-6]);$");
            b.Cho("dual", "RÉCEPTION", 874, 490, 160, 58, "MAIN=0,MAIN MUET=1,DUAL=2,SUB SEUL=3", "FR{v};", "FR;", @"^FR(?<v>[0-3]);$").Hint = "FR : 0 = main seul, 1 = main muet, 2 = main + sub (dual), 3 = main muet + sub.";
            b.Tog("nar", "NAR A", 1040, 490, 94, 58, "NA0{v};", "NA0;", @"^NA0(?<v>[01]);$");
            b.Tog("narb", "NAR B", 1140, 490, 90, 58, "NA1{v};", "NA1;", @"^NA1(?<v>[01]);$");

            b.Frame("f_vfo", "VFO / MÉMOIRES", 1250, 124, 600, 430);
            const int rx = 1264; const int bw = 108;
            b.Act("vm", "V/M", rx, 152, bw, 40, "VM;");
            b.Act("swap", "A/B", rx + 116, 152, bw, 40, "SV;");
            b.Act("a2b", "A=B", rx + 232, 152, bw, 40, "AB;");
            b.Row("vs", "VFO ACTIF", rx + 348, 152, 224, 40, 1, "A=0,B=1", "VS{v};", "VS;", @"^VS(?<v>[01]);$");
            b.Tog("clarrx", "CLAR RX", rx, 200, bw, 40, "RT{v};", "RT;", @"^RT(?<v>[01]);$").Period = 6000;
            b.Tog("clartx", "CLAR TX", rx + 116, 200, bw, 40, "XT{v};", "XT;", @"^XT(?<v>[01]);$").Period = 6000;
            b.Act("clr", "CLAR CLEAR", rx + 232, 200, bw, 40, "RC;");
            if (x5) { c = b.Tog("split", "SPLIT", rx + 348, 200, bw, 40, "FT{v};", "FT;", @"^FT(?<v>[01]);$", "3|1", "2|0"); c.Accent = "cyan"; c.Hint = "FTdx5000 : FT2 = émission sur la bande main, FT3 = émission sur la bande sub (split) ; la réponse FT P2 vaut 0 ou 1."; }
            else { c = b.Tog("split", "SPLIT", rx + 348, 200, bw, 40, "FT{v};", "FT;", @"^FT(?<v>[01]);$", "3|1", "2|0"); c.Accent = "cyan"; c.Hint = "FT2 / FT3 sont absolus (FT0 / FT1 sont des bascules) : SPLIT envoie FT3 (allumé) ou FT2 (éteint) ; la lecture FT; répond 1 ou 0 (manuel CAT du FT-2000)."; }
            b.Tog("txw", "TXW", rx + 464, 200, 108, 40, "TS{v};", "TS;", @"^TS(?<v>[01]);$");
            b.Act("qmb", "QMB RCL", rx, 248, bw, 40, "QR;");
            b.Act("qmbs", "QMB STO", rx + 116, 248, bw, 40, "QI;");
            b.Act("qs", "Q.SPLIT", rx + 232, 248, bw, 40, "QS;");
            b.Act("mchu", "MCH ▲", rx + 348, 248, bw, 40, "CH0;");
            b.Act("mchd", "MCH ▼", rx + 464, 248, 108, 40, "CH1;");
            b.Act("bu", "BAND ▲ A", rx, 296, bw, 40, "BU0;");
            b.Act("bd", "BAND ▼ A", rx + 116, 296, bw, 40, "BD0;");
            b.Act("bu1", "BAND ▲ B", rx + 232, 296, bw, 40, "BU1;");
            b.Act("bd1", "BAND ▼ B", rx + 348, 296, bw, 40, "BD1;");
            b.Tog("cs", "SPOT", rx + 464, 296, 108, 40, "CS{v};", "CS;", @"^CS(?<v>[01]);$");
            b.Act("ky1", "KY 1", rx, 344, 70, 36, "KY1;"); b.Act("ky2", "KY 2", rx + 76, 344, 70, 36, "KY2;"); b.Act("ky3", "KY 3", rx + 152, 344, 70, 36, "KY3;");
            b.Act("ky4", "KY 4", rx + 228, 344, 70, 36, "KY4;"); b.Act("ky5", "KY 5", rx + 304, 344, 70, 36, "KY5;");
            b.Act("pb1", "VOIX 1", rx, 388, 70, 36, "PB01;"); b.Act("pb2", "VOIX 2", rx + 76, 388, 70, 36, "PB02;"); b.Act("pb3", "VOIX 3", rx + 152, 388, 70, 36, "PB03;");
            b.Act("pb4", "VOIX 4", rx + 228, 388, 70, 36, "PB04;"); b.Act("pb5", "VOIX 5", rx + 304, 388, 70, 36, "PB05;"); b.Act("pb0", "STOP", rx + 380, 388, 70, 36, "PB00;");
            b.Tog("kr", "KEYER", rx, 432, bw, 40, "KR{v};", "KR;", @"^KR(?<v>[01]);$");
            b.Tog("bi", "BK-IN", rx + 116, 432, bw, 40, "BI{v};", "BI;", @"^BI(?<v>[01]);$");
            b.Knob("ks", "SPEED", rx + 236, 428, 70, 80, 4, 60, "KS{v:3};", "KS;", @"^KS(?<v>\d{3});$", "wpm");
            b.Knob("kp", "PITCH", rx + 312, 428, 70, 80, 0, 15, "KP{v:2};", "KP;", @"^KP(?<v>\d{2});$", "pitch50");

            b.Frame("f_mode", "BANDE / MODE (MAIN)", 10, 564, 830, 190);
            c = b.Row("band", "BANDE", 24, 590, 802, 52, 1, x5 ? BANDS.Replace("5=02,", "") : BANDS, "BS{v};", "", ""); c.Hint = "Manuel : BS P1 = 00 (1,8 MHz) à 11 (GEN). Pas de lecture : la bande allumée vient de la fréquence.";
            b.Row("md0", "MODE MAIN", 24, 650, 802, 44, 1, MODES, "MD0{v};", "MD0;", @"^MD0(?<v>[0-9A-F]);$").Period = 1500;
            b.Row("md1", "MODE SUB", 24, 700, 802, 44, 1, MODES, "MD1{v};", "MD1;", @"^MD1(?<v>[0-9A-F]);$");

            b.Frame("f_dial", "ACCORD", 850, 564, 340, 406);
            b.Dial("dial", "MAIN", 870, 592, 300, 300, "fa", "step");
            c = b.Cho("step", "PAS", 870, 900, 300, 56, "1 Hz=1,10 Hz=10,100 Hz=100,500 Hz=500,1 kHz=1000,5 kHz=5000,10 kHz=10000", "", "", ""); c.Local = true;

            b.Frame("f_rx", "RÉCEPTION / ÉMISSION", 10, 764, 830, 206);
            b.Knob("af", "AF MAIN", 24, 796, 94, 96, 0, 255, "AG0{v:3};", "AG0;", @"^AG0(?<v>\d{3});$", "pct").Accent = "green";
            b.Knob("rf", "RF MAIN", 124, 796, 94, 96, 0, 255, "RG0{v:3};", "RG0;", @"^RG0(?<v>\d{3});$", "pct");
            b.Knob("sql", "SQL MAIN", 224, 796, 94, 96, 0, 255, "SQ0{v:3};", "SQ0;", @"^SQ0(?<v>\d{3});$", "pct");
            c = b.Knob("afb", "AF SUB", 324, 796, 94, 96, 0, 255, "AG1{v:3};", "AG1;", @"^AG1(?<v>\d{3});$", "pct"); c.Accent = "blue";
            c = b.Knob("rfb", "RF SUB", 424, 796, 94, 96, 0, 255, "RG1{v:3};", "RG1;", @"^RG1(?<v>\d{3});$", "pct"); c.Accent = "blue";
            c = b.Knob("sqlb", "SQL SUB", 524, 796, 94, 96, 0, 255, "SQ1{v:3};", "SQ1;", @"^SQ1(?<v>\d{3});$", "pct"); c.Accent = "blue";
            b.Knob("mic", "MIC", 624, 796, 94, 96, 0, 255, "MG{v:3};", "MG;", @"^MG(?<v>\d{3});$", "pct").Accent = "amber";
            c = b.Knob("pwr", "RF PWR", 724, 796, 94, 96, 5, 100, "PC{v:3};", "PC;", @"^PC(?<v>\d{3});$", "watt"); c.Accent = "red"; c.Hint = "Le manuel CAT imprime PC = 000 à 255, mais 5 à 100 (watts) a été validé sur un vrai FT-2000.";
            b.Cho("nb", "NB MAIN", 24, 906, 94, 52, "OFF=0,NB=1,NB WIDE=2", "NB0{v};", "NB0;", @"^NB0(?<v>[012]);$");
            b.Cho("nbb", "NB SUB", 124, 906, 94, 52, "OFF=0,NB=1,NB WIDE=2", "NB1{v};", "NB1;", @"^NB1(?<v>[012]);$");
            b.Tog("dnr", "DNR", 224, 906, 94, 52, "NR0{v};", "NR0;", @"^NR0(?<v>[01]);$");
            b.Knob("nbl", "NB niv.", 324, 902, 94, 66, 0, 255, "NL0{v:3};", "NL0;", @"^NL0(?<v>\d{3});$", "pct");
            b.Knob("dnrl", "DNR niv.", 424, 902, 94, 66, 1, 15, "RL0{v:2};", "RL0;", @"^RL0(?<v>\d{2});$", "raw");
            b.Knob("procl", "PROC", 524, 902, 94, 66, 0, 255, "PL{v:3};", "PL;", @"^PL(?<v>\d{3});$", "pct").Accent = "amber";
            b.Knob("monil", "MONI", 624, 902, 94, 66, 1, 255, "ML1{v:3};", "ML1;", @"^ML1(?<v>\d{3});$", "pct");
            b.Knob("voxg", "VOX", 724, 902, 94, 66, 0, 255, "VG{v:3};", "VG;", @"^VG(?<v>\d{3});$", "pct");

            b.Frame("f_dsp", "DSP / FILTRES", 1200, 564, 650, 406);
            c = b.Knob("shift", "SHIFT", 1214, 592, 110, 112, -1000, 1000, "IS0{v:+4};", "IS0;", @"^IS0(?<v>[+-]\d{4});$", "hz"); c.Step = 20; c.Hint = "-1000 à +1000 Hz, par pas de 20 Hz (réglage validé sur un vrai FT-2000).";
            c = b.Knob("width", "WIDTH", 1330, 592, 110, 112, 0, 31, "SH0{v:2};", "SH0;", @"^SH0(?<v>\d{2});$", "raw"); c.Hint = "SH P2 = 00 (sens anti-horaire) à 31 ; 16 = centre.";
            c = b.Knob("notchf", "NOTCH", 1446, 592, 110, 112, 1, 400, "BP01{v:3};", "BP01;", @"^BP01(?<v>\d{3});$", "x10hz");
            c = b.Knob("contf", "CONT", 1562, 592, 110, 112, 1, 40, "CO01{v:2};", "CO01;", @"^CO01(?<v>\d{2});$", "raw");
            b.Tog("notch", "NOTCH", 1214, 716, 110, 44, "BP00{v:3};", "BP00;", @"^BP00(?<v>\d{3});$", "001", "000");
            b.Tog("cont", "CONTOUR", 1330, 716, 110, 44, "CO00{v:2};", "CO00;", @"^CO00(?<v>\d{2});$", "01", "00");
            b.Tog("dnf", "DNF", 1446, 716, 110, 44, "BC0{v};", "BC0;", @"^BC0(?<v>[01]);$");
            c = b.Knob("vd", "VOX DLY", rx + 388, 428, 70, 80, x5 ? 20 : 0, 5000, "VD{v:4};", "VD;", @"^VD(?<v>\d{4});$", "ms"); c.Step = 20;
            c = b.Knob("sd", "BK DLY", rx + 464, 428, 70, 80, x5 ? 20 : 0, 5000, "SD{v:4};", "SD;", @"^SD(?<v>\d{4});$", "ms"); c.Hint = x5 ? "FTdx5000 : 0020 à 5000 ms." : "0000 = full break-in ; 0001 à 5000 ms."; if (x5) c.Step = 20;
            c = b.Cho("meter", "METER", 1214, 900, 190, 62, "COMP=0,ALC=1,PO=2,SWR=3,ID=4,VDD=5", "MS{v};", "MS;", @"^MS(?<v>[0-5]);$"); c.Hint = "Commutateur METER du poste (MS : 0 COMP, 1 ALC, 2 PO, 3 SWR, 4 ID, 5 VDD). Les mesures affichées ici viennent de RM.";
            if (x5)
            {
                c = b.Cho("txbpf", "TX BPF", 1214, 832, 190, 62, "50-3000=0,100-2900=1,200-2800=2,300-2700=3,400-2600=4,3000 WB=5", "EX104{v};", "EX104;", @"^EX104(?<v>\d);$"); c.Hint = "Menu 104 SSB-TX-BPF du FTdx5000 (le 085 du FT-2000 serait un autre réglage). Numéros relevés dans le manuel CAT, non essayés sur un vrai FTdx5000.";
                c = b.Cho("notchw", "NOTCH LARGEUR", 1414, 832, 190, 62, "ÉTROIT=0,LARGE=1", "EX114{v};", "EX114;", @"^EX114(?<v>\d);$"); c.Hint = "Menu 114 IF-NOTCH-WIDTH du FTdx5000 (0 NARROW, 1 WIDE). Non essayé sur un vrai FTdx5000.";
                c = b.Cho("voxsrc", "VOX SOURCE", 1614, 832, 190, 62, "MICRO=0,DATA=1", "EX174{v};", "EX174;", @"^EX174(?<v>\d);$"); c.Hint = "Menu 174 VOX SELECT du FTdx5000 (0 MIC, 1 DATA). Non essayé sur un vrai FTdx5000.";
            }
            else
            {
                c = b.Cho("txbpf", "TX BPF", 1214, 832, 190, 62, "50-3000=0,100-2900=1,200-2800=2,300-2700=3,400-2600=4,50-4000=5", "EX085{v};", "EX085;", @"^EX085(?<v>\d);$"); c.Hint = "Menu 085 (filtre passe-bande d'émission), valeurs validées sur un vrai FT-2000.";
                c = b.Cho("notchw", "NOTCH LARGEUR", 1414, 832, 190, 62, "ÉTROIT=0,LARGE=1", "EX094{v};", "EX094;", @"^EX094(?<v>\d);$"); c.Hint = "Menu 094 (largeur du notch), validé sur un vrai FT-2000.";
                c = b.Cho("voxsrc", "VOX SOURCE", 1614, 832, 190, 62, "MICRO=0,DATA=1", "EX148{v};", "EX148;", @"^EX148(?<v>\d);$"); c.Hint = "Menu 148 (VOX sur micro ou sur data), validé sur un vrai FT-2000.";
            }
            if (x5)
            {
                c = b.Tog("vrf", "VRF MAIN", 1214, 770, 110, 44, "VF0{v}+0;", "VF0;", @"^VF0(?<v>[01])\d{3}[01];$"); c.Hint = "VRF (préselecteur) : VF P1 P2 P3 P4 = récepteur, 0 OFF / 1 ON, signe, pas 0-9 ; ici ON/OFF avec décalage +0. Non essayé sur un vrai FTdx5000.";
                c = b.Tog("vrfs", "VRF SUB", 1330, 770, 110, 44, "VF1{v}+0;", "VF1;", @"^VF1(?<v>[01])\d{3}[01];$"); c.Hint = "VRF du récepteur sub (voir VRF MAIN). Non essayé sur un vrai FTdx5000.";
                c = b.Tog("classa", "CLASS-A", 1446, 770, 110, 44, "CA0{v};", "CA0;", @"^CA0(?<v>\d{3});$", "001", "000"); c.Accent = "amber"; c.Hint = "CA P1 P2 : P1 0 = classe A marche/arrêt (000/001). Attention : la classe A échauffe fortement l'étage final.";
                c = b.Knob("classab", "BIAS A", 1678, 592, 110, 112, 1, 100, "CA1{v:3};", "CA1;", @"^CA1(?<v>\d{3});$", "raw"); c.Hint = "CA P1 = 1 : niveau de polarisation classe A, 001 à 100.";
                c = b.Act("rxantb", "ANT RX", 1678, 716, 160, 44, "AN05;"); c.Hint = "AN05; sélectionne l'antenne de réception séparée (ANT RX). Choisir une antenne ANT 1-4 la retire.";
                c = b.Cho("os", "DÉCALAGE FM", 1414, 900, 190, 62, "SIMPLEX=0,PLUS=1,MOINS=2", "OS0{v};", "OS0;", @"^OS0(?<v>[0-2]);$"); c.Hint = "OS P1 P2 : décalage relais (mode FM seulement).";
                c = b.Cho("ctcss", "CTCSS", 1614, 900, 190, 62, "ARRÊT=0,ENC/DEC=1,ENC=2", "CT0{v};", "CT0;", @"^CT0(?<v>[0-2]);$"); c.Hint = "CT P1 P2 : sous-porteuse CTCSS (mode FM seulement).";
            }
            c = b.Knob("clar", "CLAR", 1562, 712, 110, 108, -9999, 9999, "RC;{p:RU:4:;}{n:RD:4:;}", "IF;", IF, "hz"); c.Step = 10; c.Period = 300; c.Hint = "FT-2000 : pas de commande CF. Le décalage s'écrit avec RC; puis RU (plus) ou RD (moins) ; il est lu avec IF; (27 caractères) qui met aussi à jour fréquence A, mode A, CLAR RX/TX et V/M.";
            b.Fin(); return r;
        }
    }
}
