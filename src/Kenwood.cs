using System;
using System.Collections.Generic;

namespace HamRadioLink
{
    public static partial class Radios
    {
        // Postes Kenwood TS-480 (HX/SAT) et TS-890S. Sources : « TS-480 PC Control Command » et « TS-890S PC Command » (E:\...\kenwood\).
        // Catalogues dans catalogues\ts480.new.cat / ts890.new.cat. Jamais essayé sur un vrai poste : tout se corrige par clic droit en « config vue ».
        static RadioDef Ts480() { return Kenwood(false); }
        static RadioDef Ts890() { return Kenwood(true); }

        static RadioDef Kenwood(bool t890)
        {
            RadioDef r = new RadioDef(); r.Key = t890 ? "ts890" : "ts480"; r.Name = t890 ? "Kenwood TS-890S" : "Kenwood TS-480HX / SAT"; r.Catalog = r.Key;
            r.IdAnswer = t890 ? "ID024;" : "ID020;"; r.DefBaud = t890 ? 115200 : 57600; r.FreqDigits = 11; r.MaxFreq = t890 ? 75000000 : 60000000; r.Init = t890 ? "AI2;" : "AI0;";
            Bld b = new Bld(r); Ctl c;
            const string MODES = "LSB=1,USB=2,CW=3,FM=4,AM=5,FSK=6,CW-R=7,FSK-R=9";
            const string MODES890 = "LSB=1,USB=2,CW=3,FM=4,AM=5,FSK=6,CW-R=7,FSK-R=9,PSK=A,PSK-R=B,LSB-D=C,USB-D=D,FM-D=E,AM-D=F";
            string md = t890 ? "OM0" : "MD";

            // ---- alimentation / émission
            b.Frame("f_pwr", "ALIMENTATION / ÉMISSION", 10, 124, 210, 430);
            c = b.Act("onoff", "ON", 24, 152, 118, 42, "PS1;"); b.Act("poff", "OFF", 150, 152, 56, 42, "PS0;");
            c.Tip = t890 ? "TS-890 en COM : envoyer d'abord « ; », attendre ~100 ms, puis PS1;. Si le poste ne s'allume pas, essayez le gabarit « ;{wait:150}PS1; » (clic droit)." : "TS-480 : PS1; allume ; si le poste est en veille CPU il faut d'abord « ; ; ; ; PS1; » (clic droit pour modifier).";
            b.Tog("tuner", "TUNE (AT)", 24, 202, 182, 42, "AC11{v};", "AC;", @"^AC[01]{2}(?<v>[01]);$").Accent = "amber";
            b.Tog("vox", "VOX", 24, 252, 86, 44, "VX{v};", "VX;", @"^VX(?<v>[01]);$");
            b.Tog("lock", "LOCK", 120, 252, 86, 44, t890 ? "LK{v};" : "LK{v}0;", "LK;", t890 ? @"^LK(?<v>[01]);$" : @"^LK(?<v>[01])[01];$");
            if (t890)
            {
                c = b.Tog("ptt", "ÉMISSION (PTT)", 24, 306, 182, 54, "{v};", "", @"^(?:(?<v>TX\d)|(?<v>RX));$", "TX0|TX1|TX2", "RX"); c.Hint = "TS-890 : pas de lecture de l'état émission, le poste l'annonce (TX0; / RX;) quand l'information automatique est active (AI2; envoyé à la connexion).";
            }
            else
            {
                c = b.Tog("ptt", "ÉMISSION (PTT)", 24, 306, 182, 54, "{v};", "IF;", @"^IF(?<fa>\d{11}).{15}(?<v>[01])[1-9]", "TX0|1", "RX|0"); c.Hint = "TS-480 : TX0; émet (micro), RX; revient en réception. L'état est lu dans la réponse à IF; (caractère P8).";
            }
            c.Accent = "red"; c.Period = 250;
            b.Tog("fine", "FINE", 24, 370, 86, 40, "FS{v};", "FS;", @"^FS(?<v>[01]);$");
            b.Tog("ts", "TF-SET", 120, 370, 86, 40, "TS{v};", "TS;", @"^TS(?<v>[01]);$");

            // ---- écran
            b.Frame("f_scr", "ÉCRAN", 230, 124, 1010, 430);
            b.Freq("fa", "VFO-A", 244, 150, 640, 150, "FA{v:11};", "FA;", @"^FA(?<v>\d{11});$", t890 ? "md0" : "md0", "amber").Hint = "11 chiffres en Hz (ex. FA00014074000;).";
            b.Freq("fb", "VFO-B", 894, 150, 336, 150, "FB{v:11};", "FB;", @"^FB(?<v>\d{11});$", t890 ? "md1" : "md0", "blue");
            if (t890)
            {
                b.Meter("sm", "S / Po", 244, 310, 480, 34, 70, "SM;", @"^SM(?<v>\d{4});$", "s", false).Hint = "SM : 0000 à 0070 (points) ; en émission c'est la puissance.";
                b.LedRd("l_busy", "BUSY", 744, 430, 110, 26, "BY;", @"^BY(?<v>[01]);$", "green");
            }
            else
            {
                b.Meter("sm", "S", 244, 310, 480, 34, 30, "SM0;", @"^SM0(?<v>\d{4});$", "s", false).Hint = "SM0 : 0000 à 0030 environ (le manuel indique 0000-0020 à vérifier).";
                b.Meter("swr", "SWR", 244, 348, 480, 34, 10, "RM1;", @"^RM1(?<v>\d{4});$", "pct", true);
                b.Meter("comp", "COMP", 736, 310, 494, 34, 10, "RM2;", @"^RM2(?<v>\d{4});$", "pct", true);
                b.Meter("alc", "ALC", 736, 348, 494, 34, 10, "RM3;", @"^RM3(?<v>\d{4});$", "pct", true);
                b.LedRd("l_busy", "BUSY", 744, 430, 110, 26, "BY;", @"^BY(?<v>[01])0;$", "green");
            }
            b.Led("l_tx", "TX", 862, 430, 110, 26, "ptt", t890 ? "TX0|TX1|TX2" : "1", "red");
            b.Cho("att", "ATT", 244, 470, 150, 76, t890 ? "OFF=0,6 dB=1,12 dB=2,18 dB=3" : "OFF=00,ATT=01", "RA{v};", "RA;", t890 ? @"^RA(?<v>[0-3]);$" : @"^RA(?<v>\d\d)\d\d;$");
            b.Cho("pre", "PRÉAMPLI", 400, 470, 150, 76, t890 ? "OFF=0,PRE 1=1,PRE 2=2" : "OFF=0,ON=1", "PA{v};", "PA;", t890 ? @"^PA(?<v>[0-2]);$" : @"^PA(?<v>[01])\d;$");
            if (t890) b.Cho("agc", "AGC", 556, 470, 150, 76, "OFF=0,SLOW=1,MID=2,FAST=3", "GC{v};", "GC;", @"^GC(?<v>[0-3]);$");
            else b.Cho("agc", "AGC", 556, 470, 150, 76, "OFF=000,FAST=001,SLOW=002", "GT{v};", "GT;", @"^GT(?<v>\d{3});$");
            b.Cho("nr", "NR", 712, 470, 150, 76, "OFF=0,NR 1=1,NR 2=2", "NR{v};", "NR;", @"^NR(?<v>[0-2]);$");
            if (t890) b.Tog("nb", "NB 1", 868, 470, 100, 76, "NB1{v};", "NB1;", @"^NB1(?<v>[01]);$"); else b.Tog("nb", "NB", 868, 470, 100, 76, "NB{v};", "NB;", @"^NB(?<v>[01]);$");
            if (t890) b.Tog("notch", "NOTCH", 974, 470, 120, 76, "NT{v};", "NT;", @"^NT(?<v>[01]);$"); else b.Cho("bc", "BEAT CANC.", 974, 470, 150, 76, "OFF=0,BC 1=1,BC 2=2", "BC{v};", "BC;", @"^BC(?<v>[0-2]);$");

            // ---- VFO
            b.Frame("f_vfo", "VFO / RIT / XIT", 1250, 124, 600, 430);
            const int rx = 1264; const int bw = 108;
            b.Row("fr", "RX", rx, 152, 280, 40, 1, t890 ? "A=0,B=1,MEM=3" : "A=0,B=1,MEM=2", "FR{v};", "FR;", @"^FR(?<v>[0-3]);$");
            b.Row("ft", "TX", rx + 292, 152, 280, 40, 1, t890 ? "A=0,B=1,MEM=3" : "A=0,B=1,MEM=2", "FT{v};", "FT;", @"^FT(?<v>[0-3]);$").Hint = "FR ≠ FT = émission sur l'autre VFO (split).";
            b.Tog("rit", "RIT", rx, 200, bw, 40, "RT{v};", "RT;", @"^RT(?<v>[01]);$").Period = 6000;
            b.Tog("xit", "XIT", rx + 116, 200, bw, 40, "XT{v};", "XT;", @"^XT(?<v>[01]);$").Period = 6000;
            b.Act("clr", "RIT/XIT RAZ", rx + 232, 200, bw, 40, "RC;");
            b.Act("rup", "RIT ▲ 10 Hz", rx + 348, 200, bw, 40, t890 ? "RU00010;" : "RU00010;"); b.Act("rdn", "RIT ▼ 10 Hz", rx + 464, 200, 108, 40, "RD00010;");
            b.Act("bu", "BAND ▲", rx, 248, bw, 40, "BU;"); b.Act("bd", "BAND ▼", rx + 116, 248, bw, 40, "BD;");
            if (!t890) { b.Act("a2b", "A=B", rx + 232, 248, bw, 40, "VV;"); b.Tog("ca", "CW ZERO-BEAT", rx + 348, 248, 224, 40, "CA{v};", "CA;", @"^CA(?<v>[01]);$"); }
            b.Knob("ks", "SPEED", rx, 300, 80, 90, t890 ? 4 : 10, 60, "KS{v:3};", "KS;", @"^KS(?<v>\d{3});$", "wpm");

            // ---- bande / mode
            b.Frame("f_mode", "BANDE / MODE", 10, 564, 830, 190);
            c = b.Row("band", "BANDE", 24, 590, 802, 52, 1, "1.8=00001840000|00,3.5=00003573000|01,5=00005357000|02,7=00007074000|03,10=00010136000|04,14=00014074000|05,18=00018100000|06,21=00021074000|07,24.9=00024915000|08,28=00028074000|09,50=00050313000|10", "FA{v};", "", "");
            c.Hint = "Chaque touche envoie FA (fréquence VFO-A) avec la fréquence type de la bande ; elle s'allume d'après la fréquence.";
            b.Row("md0", "MODE", 24, 650, 802, 90, 2, t890 ? MODES890 : MODES, t890 ? "OM0{v};" : "MD{v};", t890 ? "OM0;" : "MD;", t890 ? @"^OM0(?<v>[1-9A-F]);$" : @"^MD(?<v>[1-9]);$").Period = 1500;
            if (t890) { Ctl m1 = b.Cho("md1", "MODE B", 0, 0, 10, 10, MODES890, "OM1{v};", "OM1;", @"^OM1(?<v>[1-9A-F]);$"); m1.Hidden = true; m1.X = 24; m1.Y = 746; m1.W = 40; m1.H = 20; }

            // ---- accord
            b.Frame("f_dial", "ACCORD", 850, 564, 340, 406);
            b.Dial("dial", "VFO-A", 870, 592, 300, 300, "fa", "step");
            c = b.Cho("step", "PAS", 870, 900, 300, 56, "1 Hz=1,10 Hz=10,100 Hz=100,500 Hz=500,1 kHz=1000,5 kHz=5000,10 kHz=10000", "", "", ""); c.Local = true;

            // ---- réception / émission
            b.Frame("f_rx", "RÉCEPTION / ÉMISSION", 10, 764, 830, 206);
            b.Knob("af", "AF", 24, 792, 90, 110, 0, 255, t890 ? "AG{v:3};" : "AG0{v:3};", t890 ? "AG;" : "AG0;", t890 ? @"^AG(?<v>\d{3});$" : @"^AG0(?<v>\d{3});$", "pct");
            b.Knob("rf", "RF", 122, 792, 90, 110, 0, t890 ? 255 : 100, "RG{v:3};", "RG;", @"^RG(?<v>\d{3});$", "pct");
            b.Knob("sql", "SQL", 220, 792, 90, 110, 0, 255, t890 ? "SQ{v:3};" : "SQ0{v:3};", t890 ? "SQ;" : "SQ0;", t890 ? @"^SQ(?<v>\d{3});$" : @"^SQ0(?<v>\d{3});$", "pct");
            b.Knob("mic", "MIC", 318, 792, 90, 110, 0, t890 ? 100 : 100, "MG{v:3};", "MG;", @"^MG(?<v>\d{3});$", "pct");
            c = b.Knob("pwr", "PUISSANCE", 416, 792, 100, 110, 5, t890 ? 100 : 100, "PC{v:3};", "PC;", @"^PC(?<v>\d{3});$", "watt"); c.Hint = t890 ? "PC : 5 à 100 W (AM 5 à 25 W)." : "PC : 5 à 100 W (TS-480SAT) ou 5 à 200 W (TS-480HX) ; AM 25 / 50 W max.";
            if (t890) { b.Knob("nbl", "NB niv.", 524, 792, 90, 110, 1, 20, "NL1{v:3};", "NL1;", @"^NL1(?<v>\d{3});$", "raw"); }
            else { b.Tog("dnl", "DNL", 524, 800, 90, 40, "DL{v}01;", "DL;", @"^DL(?<v>[01])\d\d;$"); b.Tog("proc", "PROC", 524, 848, 90, 40, "PR{v};", "PR;", @"^PR(?<v>[01]);$"); }
            b.Knob("sh", "COUPURE HAUTE", 622, 792, 100, 110, 0, t890 ? 27 : 13, t890 ? "SH0{v:3};" : "SH{v:2};", t890 ? "SH0;" : "SH;", t890 ? @"^SH0(?<v>\d{3});$" : @"^SH(?<v>\d{2});$", "raw").Hint = "Numéro dans la table du manuel (SSB TS-480 : 00 = 1000 Hz … 13 = 5000 Hz).";
            b.Knob("sl", "COUPURE BASSE", 730, 792, 100, 110, 0, t890 ? 35 : 11, t890 ? "SL0{v:2};" : "SL{v:2};", t890 ? "SL0;" : "SL;", t890 ? @"^SL0(?<v>\d{2});$" : @"^SL(?<v>\d{2});$", "raw").Hint = "Numéro dans la table du manuel (SSB TS-480 : 00 = 0 Hz … 11 = 1000 Hz).";

            { RadioDef probe = new RadioDef(); probe.Key = r.Key; probe.Catalog = r.Catalog; probe.IdAnswer = r.IdAnswer; CatalogSim cs = new CatalogSim(probe, new List<string>(), new List<string>(), new Dictionary<string, string>()); List<Ctl> keep = new List<Ctl>();
              foreach (Ctl q in r.C) { if (!q.HasCat || Conforms(cs, q)) keep.Add(q); else r.Notes.Add(q.Id + " (" + q.Label + ") : commande absente du catalogue de " + r.Name + ", élément retiré"); }
              r.C = keep.FindAll(delegate(Ctl q) { return !(q.K == Kind.Led && q.Local && q.Ref.Length > 0 && !keep.Exists(delegate(Ctl z) { return z.Id == q.Ref; })); }); }
            b.Fin();
            return r;
        }
    }
}
