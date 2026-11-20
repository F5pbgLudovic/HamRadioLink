using System;
using System.Collections.Generic;

namespace HamRadioLink
{
    public static partial class Radios
    {
        // =====================================================================================  FTdx5000 (face avant complète)
        // Sources : « FTDX5000 Series CAT Operation Reference Book » EH036H121, manuel d'utilisation EH035H371 (face avant : galvanomètre,
        // afficheur VFD, trois petits afficheurs OLED « SUB DISPLAY I / II / III », deux récepteurs VFO-A / VFO-B).
        // Disposition : ligne du haut = compteur à aiguille | afficheur VFD | trois OLED ; ligne du bas = VFO-A | accord / bandes / modes | VFO-B | émission / CW.
        static RadioDef Ftdx5000()
        {
            RadioDef r = new RadioDef(); r.Key = "ftdx5000"; r.Name = "Yaesu FTdx5000 / FTdx5000D / MP"; r.IdAnswer = "ID0362;"; r.DefBaud = 4800; r.FreqDigits = 8; r.MaxFreq = 60000000;
            Bld b = new Bld(r); Ctl c;
            const string MODES = "LSB=1,USB=2,CW=3,FM=4,AM=5,RTTY-L=6,CW-R=7,PKT-L=8,RTTY-U=9,PKT-FM=A,FM-N=B,PKT-U=C";
            const string IF = @"^IF(?<mch>\d{3})(?<fa>\d{8})(?<v>[+-]\d{4})(?<clarrx>\d)(?<clartx>\d)[0-9A-F](?<vm>\d)(?<ct>\d)\d\d(?<os>\d);$";
            const string AGCI = "AUTO=4|5|6,FAST=1,MID=2,SLOW=3,OFF=0";
            const string IPOI = "IPO 1=0,AMP 1=1,AMP 2=2,IPO 2=3";
            const string RFLA = "AUTO=0|4|5|6|9|A,15 kHz=1,6 kHz=2,3 kHz=3,600 Hz=4|7,300 Hz=5|8";
            const string RFLB = "AUTO=0|4|5|6,15 kHz=1,6 kHz=2,3 kHz=3";
            const string ATTI = "OFF=0,6 dB=1,12 dB=2,18 dB=3";
            const string CONTI = "OFF=00,CONT=01,APF=02";
            string bands = BANDS.Replace("5=02,", "");

            // ---------------------------------------------------------------- ligne du haut
            b.Frame("f_met", "COMPTEUR", 10, 124, 340, 310);
            b.Gauge("gauge", "Galvanomètre", 20, 150, 320, 170);
            c = b.Row("meter", "METER", 20, 326, 320, 34, 1, "COMP=0,ALC=1,PO=2,SWR=3,ID=4,VDD=5", "MS{v};", "MS;", @"^MS(?<v>[0-5]);$"); c.Hint = "Commutateur METER du poste (MS : 0 COMP, 1 ALC, 2 PO, 3 SWR, 4 ID, 5 VDD) : l'aiguille du compteur suit la fonction choisie en émission.";
            b.Tog("dim", "DIM", 20, 366, 100, 56, "DS{v};", "DS;", @"^DS(?<v>[01]);$");
            b.Tog("moni", "MONI", 126, 366, 100, 56, "ML0{v};", "ML0;", @"^ML0(?<v>\d{3});$", "001", "000");
            b.Tog("proc", "PROC", 232, 366, 108, 56, "PR{v};", "PR;", @"^PR(?<v>[0-2]);$");

            b.Frame("f_vfd", "AFFICHEUR VFD", 360, 124, 890, 310);
            b.Freq("fa", "MAIN (VFO-A)", 372, 150, 560, 128, "FA{v:8};", "FA;", @"^FA(?<v>\d{8});$", "md0", "amber").Hint = "8 chiffres en Hz (manuel CAT FTdx5000 : FA 00030000 à 60000000).";
            b.Meter("sm", "S MAIN", 372, 282, 560, 30, 255, "SM0;", @"^SM0(?<v>\d{3});$", "s", false);
            b.Chain("chain_a", "Schéma VFO-A", 372, 316, 560, 50, "a");
            c = b.LedRd("l_busy", "BUSY A", 372, 372, 100, 22, "BY;", @"^BY(?<v>[01])(?<busyb>[01]);$", "green"); c.Hint = "BY : silencieux ouvert main (P1) et sub (P2).";
            b.Led("l_tx", "TX", 478, 372, 100, 22, "ptt", "1|2", "red");
            b.Led("l_fast", "FAST", 584, 372, 100, 22, "fast", "5|7", "amber");
            b.Led("l_lock", "LOCK", 690, 372, 100, 22, "lock", "5|7", "amber");
            b.Led("l_nar", "NAR", 796, 372, 100, 22, "nar", "1", "cyan");
            b.Led("l_nb", "NB", 902, 372, 100, 22, "nb", "1|2", "cyan");
            b.Meter("sm1", "S SUB", 1010, 150, 230, 30, 255, "SM1;", @"^SM1(?<v>\d{3});$", "s", false).Accent = "blue";
            b.Chain("chain_b", "Schéma VFO-B", 1010, 184, 230, 50, "b");
            b.Led("l_busyb", "BUSY B", 1010, 240, 112, 22, "busyb", "1", "green");
            b.Led("l_split", "SPLIT", 1126, 240, 112, 22, "split", "1", "cyan");
            b.Led("l_mem", "MEM", 1010, 266, 112, 22, "vm", "1|2|3|4", "amber");
            b.Led("l_rxant", "ANT RX A", 1126, 266, 112, 22, "rxant", "1", "amber");
            b.Led("l_rxantb", "ANT RX B", 1010, 292, 112, 22, "rxantb", "1", "amber");
            // mesures d'émission : lues pour le galvanomètre, visibles seulement en « vue config »
            string hid = "Mesure lue pour l'aiguille du compteur (visible seulement en vue config).";
            c = b.Meter("po", "Po", 372, 400, 140, 26, 255, "RM5;", @"^RM5(?<v>\d{3});$", "pct", true); c.Hidden = true; c.Hint = hid;
            c = b.Meter("swr", "SWR", 518, 400, 140, 26, 255, "RM6;", @"^RM6(?<v>\d{3});$", "pct", true); c.Hidden = true; c.Hint = hid;
            c = b.Meter("alc", "ALC", 664, 400, 140, 26, 255, "RM4;", @"^RM4(?<v>\d{3});$", "pct", true); c.Hidden = true; c.Hint = hid;
            c = b.Meter("comp", "COMP", 810, 400, 140, 26, 255, "RM3;", @"^RM3(?<v>\d{3});$", "pct", true); c.Hidden = true; c.Hint = hid;
            c = b.Meter("idd", "Id", 956, 400, 140, 26, 255, "RM7;", @"^RM7(?<v>\d{3});$", "pct", true); c.Hidden = true; c.Hint = hid;
            c = b.Meter("vdd", "Vdd", 1102, 400, 140, 26, 255, "RM8;", @"^RM8(?<v>\d{3});$", "pct", true); c.Hidden = true; c.Hint = hid;

            b.Frame("f_oled", "AFFICHEURS OLED (SUB DISPLAY I / II / III)", 1260, 124, 590, 310);
            c = b.Freq("fb", "SUB DISPLAY-I  VFO-B", 1272, 150, 566, 70, "FB{v:8};", "FB;", @"^FB(?<v>\d{8});$", "md1", "blue"); c.Fmt = "oled";
            b.Dsp("dsp_a", "SUB DISPLAY-II", 1272, 226, 566, 96, "a");
            b.Dsp("dsp_b", "SUB DISPLAY-III", 1272, 328, 566, 96, "b");

            // ---------------------------------------------------------------- VFO-A
            b.Frame("f_a", "VFO-A  (MAIN)", 10, 444, 440, 526);
            b.Knob("af", "AF", 24, 470, 80, 88, 0, 255, "AG0{v:3};", "AG0;", @"^AG0(?<v>\d{3});$", "pct").Accent = "green";
            b.Knob("rf", "RF", 108, 470, 80, 88, 0, 255, "RG0{v:3};", "RG0;", @"^RG0(?<v>\d{3});$", "pct");
            b.Knob("sql", "SQL", 192, 470, 80, 88, 0, 255, "SQ0{v:3};", "SQ0;", @"^SQ0(?<v>\d{3});$", "pct");
            b.Knob("nbl", "NB niv.", 276, 470, 80, 88, 0, 255, "NL0{v:3};", "NL0;", @"^NL0(?<v>\d{3});$", "pct");
            b.Knob("dnrl", "DNR niv.", 360, 470, 80, 88, 1, 15, "RL0{v:2};", "RL0;", @"^RL0(?<v>\d{2});$", "raw");
            b.Cho("nb", "NB", 24, 564, 80, 42, "OFF=0,NB=1,WIDE=2", "NB0{v};", "NB0;", @"^NB0(?<v>[012]);$");
            b.Tog("dnr", "DNR", 108, 564, 80, 42, "NR0{v};", "NR0;", @"^NR0(?<v>[01]);$");
            b.Tog("dnf", "DNF", 192, 564, 80, 42, "BC0{v};", "BC0;", @"^BC0(?<v>[01]);$");
            b.Tog("notch", "NOTCH", 276, 564, 80, 42, "BP00{v:3};", "BP00;", @"^BP00(?<v>\d{3});$", "001", "000");
            b.Cho("cont", "CONT/APF", 360, 564, 80, 42, CONTI, "CO00{v};", "CO00;", @"^CO00(?<v>\d{2});$");
            b.Cho("att", "ATT", 24, 612, 100, 52, ATTI, "RA0{v};", "RA0;", @"^RA0(?<v>[0-3]);$");
            c = b.Cho("ipo", "IPO / AMP", 128, 612, 100, 52, IPOI, "PA0{v};", "PA0;", @"^PA0(?<v>[0-3]);$"); c.Hint = "FTdx5000 : PA P2 = 0 IPO 1, 1 AMP 1, 2 AMP 2, 3 IPO 2.";
            c = b.Cho("rfil", "R.FLT", 232, 612, 100, 52, RFLA, "RF0{v};", "RF0;", @"^RF0(?<v>[0-9A]);$"); c.Hint = "FTdx5000 : envoi 0 AUTO, 1 15 kHz, 2 6 kHz, 3 3 kHz, 4 600 Hz, 5 300 Hz (main seulement). La réponse utilise une autre table : 1-3 fixes, 4-6 AUTO 15/6/3 kHz, 7 600 Hz, 8 300 Hz, 9/A AUTO 600/300 Hz.";
            b.Cho("agc", "AGC", 336, 612, 100, 52, AGCI, "GT0{v};", "GT0;", @"^GT0(?<v>[0-6]);$");
            b.Tog("nar", "NAR", 24, 670, 100, 42, "NA0{v};", "NA0;", @"^NA0(?<v>[01]);$");
            c = b.Tog("vrf", "VRF", 128, 670, 100, 42, "VF0{v}+0;", "VF0;", @"^VF0(?<v>[01])\d{3}[01];$"); c.Hint = "VRF (préselecteur) : VF P1 P2 P3 P4 = récepteur, 0 OFF / 1 ON, signe, pas 0-9 ; ici ON/OFF avec décalage +0. Non essayé sur un vrai FTdx5000.";
            c = b.Cho("ant", "ANT", 232, 670, 100, 42, "ANT 1=1,ANT 2=2,ANT 3=3,ANT 4=4", "AN0{v};", "AN0;", @"^AN0(?<v>[1-4])(?<rxant>[01]);$"); c.Hint = "FTdx5000 : AN P1 (0 = main) P2 = 1 à 4 ; AN05 = antenne RX. La réponse AN0 P3 P4 donne l'antenne (P3) et l'état de l'antenne RX (P4).";
            c = b.Act("rxbtn", "ANT RX", 336, 670, 100, 42, "AN05;"); c.Hint = "AN05; sélectionne l'antenne de réception séparée (ANT RX) du récepteur main. Choisir une antenne ANT 1-4 la retire.";
            c = b.Knob("shift", "SHIFT", 24, 718, 100, 96, -1000, 1000, "IS0{v:+4};", "IS0;", @"^IS0(?<v>[+-]\d{4});$", "hz"); c.Step = 20; c.Hint = "IF SHIFT : -1000 à +1000 Hz par pas de 20 Hz (reflété sur l'afficheur SUB DISPLAY-II).";
            c = b.Knob("width", "WIDTH", 128, 718, 100, 96, 0, 25, "SH0{v:2};", "SH0;", @"^SH0(?<v>\d{2});$", "wd"); c.Hint = "IF WIDTH : numéro de filtre 00 (défaut) à 25 ; la largeur en Hz dépend du mode (tableau SH du manuel CAT) et s'affiche sur le SUB DISPLAY-II.";
            b.Knob("notchf", "NOTCH", 232, 718, 100, 96, 1, 400, "BP01{v:3};", "BP01;", @"^BP01(?<v>\d{3});$", "x10hz");
            c = b.Knob("contf", "CONT", 336, 718, 100, 96, 1, 40, "CO01{v:2};", "CO01;", @"^CO01(?<v>\d{2});$", "x100hz"); c.Hint = "CONTOUR : 01 à 40 = 100 à 4000 Hz.";
            b.Tog("lock", "LOCK", 24, 822, 100, 44, "LK{v};", "LK;", @"^LK(?<v>[0-7]);$", "1|5|7", "0|4|6").Hint = "FTdx5000 : LK0/LK1 = verrouillage VFO-A (LK2/LK3 = VFO-B) ; la réponse vaut 4 à 7 (5 et 7 = VFO-A verrouillé).";
            b.Tog("fast", "FAST", 128, 822, 100, 44, "FS{v};", "FS;", @"^FS(?<v>[0-7]);$", "1|5|7", "0|4|6").Hint = "FTdx5000 : FS0/FS1 = pas rapide VFO-A (FS2/FS3 = VFO-B) ; la réponse vaut 4 à 7 (5 et 7 = VFO-A rapide).";
            b.Tog("cs", "SPOT", 232, 822, 100, 44, "CS{v};", "CS;", @"^CS(?<v>[01]);$");

            // ---------------------------------------------------------------- accord / bandes / modes
            b.Frame("f_tune", "ACCORD / BANDES / MODES", 460, 444, 520, 526);
            b.Dial("dial", "MAIN", 470, 470, 220, 220, "fa", "step");
            c = b.Cho("step", "PAS", 470, 696, 220, 40, "1 Hz=1,10 Hz=10,100 Hz=100,500 Hz=500,1 kHz=1000,5 kHz=5000,10 kHz=10000", "", "", ""); c.Local = true;
            b.Act("vm", "V/M", 740, 470, 104, 40, "VM;"); b.Act("swap", "A/B", 850, 470, 104, 40, "SV;");
            b.Act("a2b", "A=B", 740, 514, 104, 40, "AB;"); b.Tog("split", "SPLIT", 850, 514, 104, 40, "FT{v};", "FT;", @"^FT(?<v>[01]);$", "3|1", "2|0").Accent = "cyan";
            b.Tog("txw", "TXW", 740, 558, 104, 40, "TS{v};", "TS;", @"^TS(?<v>[01]);$"); b.Row("vs", "VFO ACTIF", 850, 558, 104, 40, 1, "A=0,B=1", "VS{v};", "VS;", @"^VS(?<v>[01]);$");
            b.Tog("clarrx", "CLAR RX", 740, 602, 104, 40, "RT{v};", "RT;", @"^RT(?<v>[01]);$").Period = 6000; b.Tog("clartx", "CLAR TX", 850, 602, 104, 40, "XT{v};", "XT;", @"^XT(?<v>[01]);$").Period = 6000;
            b.Act("clr", "CLAR CLEAR", 740, 646, 104, 40, "RC;"); b.Act("qmb", "QMB RCL", 850, 646, 104, 40, "QR;");
            b.Act("qmbs", "QMB STO", 740, 690, 104, 40, "QI;"); b.Act("qs", "Q.SPLIT", 850, 690, 104, 40, "QS;");
            c = b.Knob("clar", "CLAR", 740, 736, 104, 108, -9999, 9999, "RC;{p:RU:4:;}{n:RD:4:;}", "IF;", IF, "hz"); c.Step = 10; c.Period = 300; c.Hint = "Pas de commande CF : le décalage s'écrit avec RC; puis RU (plus) ou RD (moins) ; il est lu avec IF; qui met aussi à jour fréquence A, mode A, CLAR RX/TX et V/M.";
            c = b.Cho("dual", "RÉCEPTION", 850, 736, 104, 108, "MAIN=0,MAIN MUET=1,DUAL=2,SUB SEUL=3", "FR{v};", "FR;", @"^FR(?<v>[0-3]);$"); c.Hint = "FR : 0 = main seul, 1 = main muet, 2 = main + sub (dual), 3 = main muet + sub.";
            b.Act("mchu", "MCH ▲", 470, 744, 108, 38, "CH0;"); b.Act("mchd", "MCH ▼", 586, 744, 108, 38, "CH1;");
            b.Act("bu", "BAND ▲ A", 470, 786, 108, 38, "BU0;"); b.Act("bd", "BAND ▼ A", 586, 786, 108, 38, "BD0;");
            b.Act("bu1", "BAND ▲ B", 470, 828, 108, 38, "BU1;"); b.Act("bd1", "BAND ▼ B", 586, 828, 108, 38, "BD1;");
            c = b.Row("band", "BANDE", 470, 872, 500, 32, 1, bands, "BS{v};", "", ""); c.Hint = "BS P1 : 00 (1,8 MHz) à 11 (GEN) ; pas de bande 5 MHz sur le FTdx5000 (code 02 absent). Pas de lecture : la bande allumée vient de la fréquence.";
            b.Row("md0", "MODE MAIN", 470, 908, 500, 56, 2, MODES, "MD0{v};", "MD0;", @"^MD0(?<v>[0-9A-F]);$").Period = 1500;

            // ---------------------------------------------------------------- VFO-B
            b.Frame("f_b", "VFO-B  (SUB)", 990, 444, 420, 526);
            c = b.Knob("afb", "AF", 1004, 470, 76, 88, 0, 255, "AG1{v:3};", "AG1;", @"^AG1(?<v>\d{3});$", "pct"); c.Accent = "blue";
            c = b.Knob("rfb", "RF", 1084, 470, 76, 88, 0, 255, "RG1{v:3};", "RG1;", @"^RG1(?<v>\d{3});$", "pct"); c.Accent = "blue";
            c = b.Knob("sqlb", "SQL", 1164, 470, 76, 88, 0, 255, "SQ1{v:3};", "SQ1;", @"^SQ1(?<v>\d{3});$", "pct"); c.Accent = "blue";
            b.Knob("nlb", "NB niv.", 1244, 470, 76, 88, 0, 255, "NL1{v:3};", "NL1;", @"^NL1(?<v>\d{3});$", "pct");
            b.Knob("rlb", "DNR niv.", 1324, 470, 76, 88, 1, 15, "RL1{v:2};", "RL1;", @"^RL1(?<v>\d{2});$", "raw");
            b.Cho("nbb", "NB", 1004, 564, 76, 42, "OFF=0,NB=1,WIDE=2", "NB1{v};", "NB1;", @"^NB1(?<v>[012]);$");
            b.Tog("dnrb", "DNR", 1084, 564, 76, 42, "NR1{v};", "NR1;", @"^NR1(?<v>[01]);$");
            b.Tog("dnfb", "DNF", 1164, 564, 76, 42, "BC1{v};", "BC1;", @"^BC1(?<v>[01]);$");
            b.Tog("notchb", "NOTCH", 1244, 564, 76, 42, "BP10{v:3};", "BP10;", @"^BP10(?<v>\d{3});$", "001", "000");
            b.Cho("contb", "CONT/APF", 1324, 564, 76, 42, CONTI, "CO10{v};", "CO10;", @"^CO10(?<v>\d{2});$");
            b.Cho("attb", "ATT", 1004, 612, 96, 52, ATTI, "RA1{v};", "RA1;", @"^RA1(?<v>[0-3]);$");
            b.Cho("ipob", "IPO / AMP", 1104, 612, 96, 52, IPOI, "PA1{v};", "PA1;", @"^PA1(?<v>[0-3]);$");
            b.Cho("rfilb", "R.FLT", 1204, 612, 96, 52, RFLB, "RF1{v};", "RF1;", @"^RF1(?<v>[0-6]);$");
            b.Cho("agcb", "AGC", 1304, 612, 96, 52, AGCI, "GT1{v};", "GT1;", @"^GT1(?<v>[0-6]);$");
            b.Tog("narb", "NAR", 1004, 670, 96, 42, "NA1{v};", "NA1;", @"^NA1(?<v>[01]);$");
            b.Tog("vrfs", "VRF", 1104, 670, 96, 42, "VF1{v}+0;", "VF1;", @"^VF1(?<v>[01])\d{3}[01];$").Hint = "VRF du récepteur sub (voir VRF du main). Non essayé sur un vrai FTdx5000.";
            c = b.Cho("antb", "ANT", 1204, 670, 96, 42, "ANT 1=1,ANT 2=2,ANT 3=3,ANT 4=4", "AN1{v};", "AN1;", @"^AN1(?<v>[1-4])(?<rxantb>[01]);$"); c.Hint = "AN P1 = 1 (sub) P2 = 1 à 4 ; AN15 = antenne RX du récepteur sub.";
            b.Act("rxbtnb", "ANT RX", 1304, 670, 96, 42, "AN15;");
            c = b.Knob("shiftb", "SHIFT", 1004, 718, 96, 96, -1000, 1000, "IS1{v:+4};", "IS1;", @"^IS1(?<v>[+-]\d{4});$", "hz"); c.Step = 20;
            b.Knob("widthb", "WIDTH", 1104, 718, 96, 96, 0, 25, "SH1{v:2};", "SH1;", @"^SH1(?<v>\d{2});$", "wd");
            b.Knob("notchfb", "NOTCH", 1204, 718, 96, 96, 1, 400, "BP11{v:3};", "BP11;", @"^BP11(?<v>\d{3});$", "x10hz");
            b.Knob("contfb", "CONT", 1304, 718, 96, 96, 1, 40, "CO11{v:2};", "CO11;", @"^CO11(?<v>\d{2});$", "x100hz");
            b.Row("md1", "MODE SUB", 1004, 822, 396, 58, 2, MODES, "MD1{v};", "MD1;", @"^MD1(?<v>[0-9A-F]);$");

            // ---------------------------------------------------------------- émission / CW
            b.Frame("f_tx", "ÉMISSION / CW", 1420, 444, 430, 526);
            b.Knob("mic", "MIC", 1434, 470, 78, 88, 0, 255, "MG{v:3};", "MG;", @"^MG(?<v>\d{3});$", "pct").Accent = "amber";
            c = b.Knob("pwr", "RF PWR", 1516, 470, 78, 88, 5, 100, "PC{v:3};", "PC;", @"^PC(?<v>\d{3});$", "watt"); c.Accent = "red"; c.Hint = "Le manuel CAT imprime PC = 000 à 255 ; 5 à 100 (watts) conservé comme sur le FT-2000 (validé sur le vrai FT-2000). À vérifier sur le FTdx5000 (clic droit).";
            b.Knob("procl", "PROC", 1598, 470, 78, 88, 0, 255, "PL{v:3};", "PL;", @"^PL(?<v>\d{3});$", "pct").Accent = "amber";
            b.Knob("monil", "MONI", 1680, 470, 78, 88, 1, 255, "ML1{v:3};", "ML1;", @"^ML1(?<v>\d{3});$", "pct");
            b.Knob("voxg", "VOX", 1762, 470, 78, 88, 0, 255, "VG{v:3};", "VG;", @"^VG(?<v>\d{3});$", "pct");
            b.Knob("ks", "SPEED", 1434, 564, 78, 88, 4, 60, "KS{v:3};", "KS;", @"^KS(?<v>\d{3});$", "wpm");
            b.Knob("kp", "PITCH", 1516, 564, 78, 88, 0, 75, "KP{v:2};", "KP;", @"^KP(?<v>\d{2});$", "pitch10");
            c = b.Knob("vd", "VOX DLY", 1598, 564, 78, 88, 20, 5000, "VD{v:4};", "VD;", @"^VD(?<v>\d{4});$", "ms"); c.Step = 20;
            c = b.Knob("sd", "BK DLY", 1680, 564, 78, 88, 20, 5000, "SD{v:4};", "SD;", @"^SD(?<v>\d{4});$", "ms"); c.Step = 20; c.Hint = "FTdx5000 : 0020 à 5000 ms.";
            c = b.Knob("classab", "BIAS A", 1762, 564, 78, 88, 1, 100, "CA1{v:3};", "CA1;", @"^CA1(?<v>\d{3});$", "raw"); c.Hint = "CA P1 = 1 : niveau de polarisation classe A, 001 à 100.";
            b.Act("onoff", "ON", 1434, 658, 78, 44, "PS1;").Tip = "FTdx5000 : PS1; allume (à envoyer deux fois à 1 s d'écart selon le manuel), PS0; éteint.";
            b.Act("poff", "OFF", 1516, 658, 78, 44, "PS0;");
            b.Tog("tuner", "TUNE", 1598, 658, 78, 44, "AC00{v};", "AC;", @"^AC00(?<v>[0-2]);$", "1|2", "0").Accent = "amber";
            b.Act("tunego", "TUNE GO", 1680, 658, 78, 44, "AC002;");
            b.Tog("vox", "VOX", 1762, 658, 78, 44, "VX{v};", "VX;", @"^VX(?<v>[01]);$");
            b.Tog("mox", "MOX", 1434, 708, 78, 46, "MX{v};", "MX;", @"^MX(?<v>[01]);$").Accent = "red";
            c = b.Tog("ptt", "ÉMISSION (PTT)", 1516, 708, 162, 46, "TX{v};", "TX;", @"^TX(?<v>[0-2]);$", "1|2", "0"); c.Accent = "red"; c.Period = 250;
            c = b.Tog("classa", "CLASS-A", 1684, 708, 156, 46, "CA0{v};", "CA0;", @"^CA0(?<v>\d{3});$", "001", "000"); c.Accent = "amber"; c.Hint = "CA P1 P2 : P1 0 = classe A marche/arrêt (000/001). Attention : la classe A échauffe fortement l'étage final.";
            b.Tog("kr", "KEYER", 1434, 760, 78, 40, "KR{v};", "KR;", @"^KR(?<v>[01]);$");
            b.Tog("bi", "BK-IN", 1516, 760, 78, 40, "BI{v};", "BI;", @"^BI(?<v>[01]);$");
            b.Act("pb0", "STOP", 1598, 760, 78, 40, "PB00;");
            for (int i = 1; i <= 5; i++) b.Act("ky" + i, "KY " + i, 1434 + (i - 1) * 82, 806, 78, 34, "KY" + i + ";");
            for (int i = 1; i <= 5; i++) b.Act("pb" + i, "VOIX " + i, 1434 + (i - 1) * 82, 846, 78, 34, "PB0" + i + ";");
            c = b.Cho("txbpf", "TX BPF", 1434, 888, 130, 38, "50-3000=0,100-2900=1,200-2800=2,300-2700=3,400-2600=4,3000 WB=5", "EX104{v};", "EX104;", @"^EX104(?<v>\d);$"); c.Hint = "Menu 104 SSB-TX-BPF du FTdx5000 (le 085 du FT-2000 serait un autre réglage). Numéro relevé dans le manuel CAT, non essayé sur un vrai FTdx5000.";
            c = b.Cho("notchw", "NOTCH LARG.", 1570, 888, 130, 38, "ÉTROIT=0,LARGE=1", "EX114{v};", "EX114;", @"^EX114(?<v>\d);$"); c.Hint = "Menu 114 IF-NOTCH-WIDTH du FTdx5000 (0 NARROW, 1 WIDE). Non essayé sur un vrai FTdx5000.";
            c = b.Cho("voxsrc", "VOX SOURCE", 1706, 888, 134, 38, "MICRO=0,DATA=1", "EX174{v};", "EX174;", @"^EX174(?<v>\d);$"); c.Hint = "Menu 174 VOX SELECT du FTdx5000 (0 MIC, 1 DATA). Non essayé sur un vrai FTdx5000.";
            c = b.Cho("os", "DÉCALAGE FM", 1434, 930, 130, 36, "SIMPLEX=0,PLUS=1,MOINS=2", "OS0{v};", "OS0;", @"^OS0(?<v>[0-2]);$"); c.Hint = "OS P1 P2 : décalage relais (mode FM seulement).";
            c = b.Cho("ctcss", "CTCSS", 1570, 930, 130, 36, "ARRÊT=0,ENC/DEC=1,ENC=2", "CT0{v};", "CT0;", @"^CT0(?<v>[0-2]);$"); c.Hint = "CT P1 P2 : sous-porteuse CTCSS (mode FM seulement).";
            b.Fin(); return r;
        }
    }
}
