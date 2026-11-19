using System.Collections.Generic;
namespace HamRadioLink
{
    // Genere depuis analyse\civ\*.civ (manuels CI-V Icom) : commande|sous-commande|donnees|nom|notes. Ne pas modifier a la main.
    public static class CivCatalogs
    {
        public static readonly Dictionary<string, string> D = new Dictionary<string, string>();
        static CivCatalogs()
        {
            D["ic7000"] = @"
00||BCD frequency data (length not detailed in this manual)|Send frequency data (transceive)
01||mode data (same as command 06)|Send mode data (transceive)
02||none (read)|Read band edge frequencies
03||BCD frequency data|Read operating frequency
04||mode data|Read operating mode
05||BCD frequency data|Set operating frequency
06|00|none|Select LSB
06|01|none|Select USB
06|02|none|Select AM
06|03|none|Select CW
06|04|none|Select RTTY
06|05|none|Select FM
06|07|none|Select CW-R
06|08|none|Select RTTY-R
07||none|Select VFO mode
07|00|none|Select VFO A
07|01|none|Select VFO B
07|A0|none|Equalize VFO A and VFO B
07|B0|none|Exchange VFO A and VFO B
08||none|Select memory mode
08||BCD 0001~0105|Select memory channel
08||BCD 0106 or 0107|Select the call channel
08|A0|bank number 1~5|Set the bank number
09||none|Memory write
0A||none|Memory to VFO
0B||none|Memory clear
0C||none|Read offset frequency
0D||offset frequency data|Set offset frequency
0E|00|none|Scan stop
0E|01|none|Programmed/memory scan start
0E|02|none|Programmed scan start
0E|22|none|Memory scan start
0E|23|none|Select memory scan start
0E|B0|none|Set as non-select channel
0E|B1|none|Set as select channel
0E|D0|none|Set scan resume OFF
0E|D3|none|Set scan resume ON
0F|00|none|Turn the split function OFF
0F|01|none|Turn the split function ON
0F|10|none|Select simplex operation
0F|11|none|Select -DUP operation
0F|12|none|Select +DUP operation
10|00|none|Select 10 Hz (1 Hz) tuning step
10|01|none|Select 100 Hz tuning step
10|02|none|Select 1 kHz tuning step
10|03|none|Select 5 kHz tuning step
10|04|none|Select 9 kHz tuning step
10|05|none|Select 10 kHz tuning step
10|06|none|Select 12.5 kHz tuning step
10|07|none|Select 20 kHz tuning step
10|08|none|Select 25 kHz tuning step
10|09|none|Select 100 kHz tuning step
10|10|none|Select MHz step
11||1 byte 00/12|Select/read attenuator
13|00|none|Announce with voice synthesizer: all data
13|01|none|Announce with voice synthesizer: frequency and S-meter level
13|02|none|Announce with voice synthesizer: receive mode
14|01|Level data (0~255)|AF level
14|02|Level data (0~255)|RF level
14|03|Level data (0~255)|SQL level
14|06|Level data (0~255)|NR level
14|07|Level data (0~255)|Inner PBT / IF shift
14|08|Level data (0~255)|Outside PBT
14|09|Level data (0~255)|CW pitch
14|0A|Level data (0~255)|RF power
14|0B|Level data (0~255)|MIC gain
14|0C|Level data (0~255)|Key speed
14|0D|Level data (0~255)|NOTCH (NF1) frequency
14|0E|Level data (0~10)|COMP level
14|0F|Level data (20~130)|Break-in delay
14|12|Level data (0~255)|NB level
14|15|Level data (0~255)|Monitor gain
14|16|Level data (0~255)|VOX gain
14|17|Level data (0~255)|Anti-VOX gain
14|18|Level data (0~255)|Contrast (LCD)
14|19|Level data (0~255)|Bright (LCD)
14|1A|Level data (0~255)|NOTCH (NF2) frequency
15|01|none (read)|Read squelch condition
15|02|none (read)|Read S-meter level
15|11|none (read)|Read RF power meter
15|12|none (read)|Read SWR meter
15|13|none (read)|Read ALC meter
15|14|none (read)|Read COMP meter
16|02|1 byte 0/1|Preamp
16|12|1 byte 0~3|AGC selection
16|22|1 byte 0/1|Noise blanker
16|40|1 byte 0/1|Noise reduction
16|41|1 byte 0/1|Auto notch
16|42|1 byte 0/1|Repeater tone
16|43|1 byte 0/1|Tone squelch
16|44|1 byte 0/1|Speech compressor
16|45|1 byte 0/1|Monitor
16|46|1 byte 0/1|VOX function
16|47|1 byte 0~2|Break-in
16|48|1 byte 0/1|Manual notch (NF1)
16|4B|1 byte 0/1|DTCS
16|4C|1 byte 0/1|VSC
16|4F|1 byte 0/1|Twin Peak Filter
16|50|1 byte 0/1|Dial lock function
16|51|1 byte 0/1|Manual notch (NF2)
19|00|none (reply ID)|Read the transceiver ID
1A|00|memory number + data (see p.148)|Send/read memory contents
1A|01|band code + register code + data (p.148)|Send/read band stacking register contents
1A|02|channel 01~04 + ASCII (p.148)|Send/read memory keyer contents
1A|03|1 byte|Send/read the selected filter width
1A|04|1 byte|Send/read the selected AGC time constant
1A|05 00 01|per description (data length not stated in manual)|RF Power setting
1A|05 00 02|per description (data length not stated in manual)|MIC Gain setting
1A|05 00 03|per description (data length not stated in manual)|of SSB TX bandwidth (lower edge) for wide
1A|05 00 04|per description (data length not stated in manual)|of SSB TX bandwidth (higher edge) for wide
1A|05 00 05|per description (data length not stated in manual)|of SSB TX bandwidth (lower edge) for middle
1A|05 00 06|per description (data length not stated in manual)|of SSB TX bandwidth (higher edge) for middle
1A|05 00 07|per description (data length not stated in manual)|of SSB TX bandwidth (lower edge) for narrow
1A|05 00 08|per description (data length not stated in manual)|of SSB TX bandwidth (higher edge) for narrow
1A|05 00 09|per description (data length not stated in manual)|Twin Peak Filter
1A|05 00 10|per description (data length not stated in manual)|RTTY mark frequency
1A|05 00 11|per description (data length not stated in manual)|RTTY shift width
1A|05 00 12|per description (data length not stated in manual)|RTTY keying polarity
1A|05 00 13|per description (data length not stated in manual)|CW key speed
1A|05 00 14|per description (data length not stated in manual)|CW Pitch setting
1A|05 00 15|per description (data length not stated in manual)|CW side tone level
1A|05 00 16|per description (data length not stated in manual)|CW side tone level limit
1A|05 00 17|per description (data length not stated in manual)|LCD contrast
1A|05 00 18|per description (data length not stated in manual)|LCD bright
1A|05 00 19|per description (data length not stated in manual)|LCD unit bright
1A|05 00 20|per description (data length not stated in manual)|LCD flicker level
1A|05 00 21|per description (data length not stated in manual)|switch backlight
1A|05 00 22|per description (data length not stated in manual)|display type
1A|05 00 23|per description (data length not stated in manual)|display font type
1A|05 00 24|per description (data length not stated in manual)|display font size
1A|05 00 25|per description (data length not stated in manual)|meter peak hold
1A|05 00 26|per description (data length not stated in manual)|filter pop up indication for PBT shifting
1A|05 00 27|per description (data length not stated in manual)|filter pop up indication for IF filter setting
1A|05 00 28|per description (data length not stated in manual)|pop up indication for 1 Hz mode
1A|05 00 29|per description (data length not stated in manual)|pop up indication for scope center/fix
1A|05 00 30|per description (data length not stated in manual)|TV pop up indication for channel Up/Down
1A|05 00 31|per description (data length not stated in manual)|TV pop up indication for P.AMP/ATT
1A|05 00 32|per description (data length not stated in manual)|indication of the voice TX memory channel names
1A|05 00 33|per description (data length not stated in manual)|indication of the keyer memory names
1A|05 00 34|per description (data length not stated in manual)|indication of the DTMF memory names
1A|05 00 35|per description (data length not stated in manual)|external display setting
1A|05 00 36|per description (data length not stated in manual)|opening message
1A|05 00 37|per description (data length not stated in manual)|my call sign setting
1A|05 00 38|per description (data length not stated in manual)|power ON check
1A|05 00 39|per description (data length not stated in manual)|current year
1A|05 00 40|per description (data length not stated in manual)|current date
1A|05 00 41|per description (data length not stated in manual)|current time
1A|05 00 42|per description (data length not stated in manual)|clock2 function
1A|05 00 43|per description (data length not stated in manual)|offset time for clock2 function
1A|05 00 44|per description (data length not stated in manual)|auto power OFF period
1A|05 00 45|per description (data length not stated in manual)|TX monitor set
1A|05 00 46|per description (data length not stated in manual)|TX monitor gain
1A|05 00 47|per description (data length not stated in manual)|confirmation beep
1A|05 00 48|per description (data length not stated in manual)|band edge beep
1A|05 00 49|per description (data length not stated in manual)|beep gain
1A|05 00 50|per description (data length not stated in manual)|beep gain limit
1A|05 00 51|per description (data length not stated in manual)|RF/SQL control set
1A|05 00 52|per description (data length not stated in manual)|quick split set
1A|05 00 53|per description (data length not stated in manual)|split offset -9.999 to +9.999 MHz (see p. 149 for de- tails)
1A|05 00 54|per description (data length not stated in manual)|split lock set
1A|05 00 55|per description (data length not stated in manual)|duplex offset 0.000 to 9.999 MHz for HF (see p. 149 for details)
1A|05 00 56|per description (data length not stated in manual)|duplex offset 0.000 to 9.999 MHz for 50 MHz band (see p. 149 for details)
1A|05 00 57|per description (data length not stated in manual)|duplex offset 0.000 to 9.999 MHz for 144 MHz band (see p. 149 for details)
1A|05 00 58|per description (data length not stated in manual)|duplex offset 0.000 to 9.999 MHz for 430 MHz band (see p. 149 for details)
1A|05 00 59|per description (data length not stated in manual)|one touch repeater set
1A|05 00 60|per description (data length not stated in manual)|auto repeater set
1A|05 00 61|per description (data length not stated in manual)|tuner auto start set
1A|05 00 62|per description (data length not stated in manual)|PTT tune set
1A|05 00 63|per description (data length not stated in manual)|[TUNER/CALL] key ac- tion set
1A|05 00 64|per description (data length not stated in manual)|[ACC] (pin 7) output ""VSEND"" set
1A|05 00 65|per description (data length not stated in manual)|speech level
1A|05 00 66|per description (data length not stated in manual)|speech language
1A|05 00 67|per description (data length not stated in manual)|speech speed
1A|05 00 68|per description (data length not stated in manual)|S-level speech
1A|05 00 69|per description (data length not stated in manual)|speech capability with [MODE] key operation
1A|05 00 70|per description (data length not stated in manual)|memopad numbers
1A|05 00 71|per description (data length not stated in manual)|scan speed
1A|05 00 72|per description (data length not stated in manual)|scan resume
1A|05 00 73|per description (data length not stated in manual)|main dial auto TS
1A|05 00 74|per description (data length not stated in manual)|[F-1] key assigment of the HM-151.
1A|05 00 75|per description (data length not stated in manual)|[F-2] key assigment of the HM-151. (Selectable functions are same as [F-1].)
1A|05 00 76|per description (data length not stated in manual)|mic. up/down speed
1A|05 00 77|per description (data length not stated in manual)|quick RIT/TX clear function
1A|05 00 78|per description (data length not stated in manual)|SSB/CW synchronous tuning function
1A|05 00 79|per description (data length not stated in manual)|CW normal side set
1A|05 00 80|per description (data length not stated in manual)|voice recorder 1st menu set
1A|05 00 81|per description (data length not stated in manual)|keyer 1st menu set
1A|05 00 82|per description (data length not stated in manual)|DTMF 1st menu set
1A|05 00 83|per description (data length not stated in manual)|SSB mode selectability
1A|05 00 84|per description (data length not stated in manual)|CW mode selectability
1A|05 00 85|per description (data length not stated in manual)|RTTY mode selectabil- ity.
1A|05 00 86|per description (data length not stated in manual)|AM mode selectability
1A|05 00 87|per description (data length not stated in manual)|FM mode selectability
1A|05 00 88|per description (data length not stated in manual)|WFM mode selectabil- ity
1A|05 00 89|per description (data length not stated in manual)|external keypad set for voice memory
1A|05 00 90|per description (data length not stated in manual)|external keypad set for keyer memory
1A|05 00 91|per description (data length not stated in manual)|external keypad type connected to [MIC] connector of controller
1A|05 00 92|per description (data length not stated in manual)|CI-V transceive set
1A|05 00 93|per description (data length not stated in manual)|reference frequency set
1A|05 00 94|per description (data length not stated in manual)|speech compresser level
1A|05 00 95|per description (data length not stated in manual)|auto voice monitor set
1A|05 00 96|per description (data length not stated in manual)|MIC memo function
1A|05 00 97|per description (data length not stated in manual)|contest number style
1A|05 00 98|per description (data length not stated in manual)|count up trigger chan- nel
1A|05 00 99|per description (data length not stated in manual)|present number (1-9999)
1A|05 01 00|per description (data length not stated in manual)|CW keyer repeat time
1A|05 01 01|per description (data length not stated in manual)|CW keyer dot/dash ratio
1A|05 01 02|per description (data length not stated in manual)|rise time
1A|05 01 03|per description (data length not stated in manual)|CW paddle polarity
1A|05 01 04|per description (data length not stated in manual)|CW keyer type
1A|05 01 05|per description (data length not stated in manual)|MIC up/down keyer (HM-103) set
1A|05 01 06|per description (data length not stated in manual)|RTTY decode USOS
1A|05 01 07|per description (data length not stated in manual)|RTTY decode new line code
1A|05 01 08|per description (data length not stated in manual)|scope max. hold
1A|05 01 09|per description (data length not stated in manual)|scope size set
1A|05 01 10|per description (data length not stated in manual)|fast sweep set
1A|05 01 11|per description (data length not stated in manual)|fast sweep audio level
1A|05 01 12|per description (data length not stated in manual)|NB level set
1A|05 01 13|per description (data length not stated in manual)|NB width set
1A|05 01 14|per description (data length not stated in manual)|NR level set
1A|05 01 15|per description (data length not stated in manual)|VOX gain
1A|05 01 16|per description (data length not stated in manual)|anti VOX gain
1A|05 01 17|per description (data length not stated in manual)|VOX delay
1A|05 01 18|per description (data length not stated in manual)|DTMF speed set
1A|05 01 19|per description (data length not stated in manual)|Break-IN delay set
1A|06|per description|Send/read SSB transmit bandwidth
1A|07|per description|Send/read DSP filter shape
1A|08|per description|Send/read manual notch filter 1 bandwidth
1A|09|per description|Send/read manual notch filter 2 bandwidth
1A|0A|per description|Send/read 9600 bps mode set
1B|00|repeater tone data (p.149)|Set/read repeater tone frequency
1B|01|TSQL tone data (p.149)|Set/read TSQL tone frequency
1B|02|DTCS data (p.149)|Set/read DTCS code and polarity
1C|00|1 byte 0/1|Set/read the transceiver condition (PTT)
1C|01|1 byte 0/1/2|Set/read antenna tuner condition
";
            D["ic7100"] = @"
00||BCD 5 bytes frequency (10 Hz/1 Hz byte first)|Send operating frequency (transceive)
01||mode byte + filter byte (see 06)|Send operating mode (transceive)
02||none (read) -> 2x BCD 5 bytes with separator 2D|Read band edge frequencies
03||BCD 5 bytes frequency|Read operating frequency
04||mode byte + filter byte|Read operating mode
05||BCD 5 bytes frequency|Set operating frequency
06|00|optional filter byte 01~03|Select LSB mode
06|01|optional filter byte 01~03|Select USB mode
06|02|optional filter byte 01~03|Select AM mode
06|03|optional filter byte 01~03|Select CW mode
06|04|optional filter byte 01~03|Select RTTY mode
06|05|optional filter byte 01~03|Select FM mode
06|06|optional filter byte 01~03|Select WFM mode
06|07|optional filter byte 01~03|Select CW-R mode
06|08|optional filter byte 01~03|Select RTTY-R mode
06|17|optional filter byte 01~03|Select DV mode
07||none|Select VFO mode
07|00|none|Select VFO A
07|01|none|Select VFO B
07|A0|none|Equalize VFO A and VFO B
07|B0|none|Exchange VFO A and VFO B
08||none|Select Memory mode
08||BCD 2 bytes 0001~0109|Select memory channel
08|A0 01|none|Select Memory Bank A
08|A0 02|none|Select Memory Bank B
08|A0 03|none|Select Memory Bank C
08|A0 04|none|Select Memory Bank D
08|A0 05|none|Select Memory Bank E
09||none|Memory write
0A||none|Memory copy to VFO
0B||none|Memory clear
0C||none|Read offset frequency
0D||offset frequency data|Send offset frequency
0E|00|none|Scan stop
0E|01|none|Programmed/memory scan start
0E|02|none|Programmed scan start
0E|03|none|F scan start
0E|12|none|Fine programmed scan start
0E|13|none|Fine F scan start
0E|22|none|Memory scan start
0E|23|none|Select memory scan start
0E|24|none|Mode select scan start
0E|A1|none|Set +-5 kHz F scan span
0E|A2|none|Set +-10 kHz F scan span
0E|A3|none|Set +-20 kHz F scan span
0E|A4|none|Set +-50 kHz F scan span
0E|A5|none|Set +-100 kHz F scan span
0E|A6|none|Set +-500 kHz F scan span
0E|A7|none|Set +-1 MHz F scan span
0E|B0|none|Set as non-select memory channel
0E|B1|none|Set as select memory channel
0E|D0|none|Set scan resume OFF
0E|D3|none|Set scan resume ON
0F||none (read) / reply 1 byte|Read split / duplex state
0F|00|none|Set split OFF
0F|01|none|Set split ON
0F|10|none|Set simplex operation
0F|11|none|Set DUP- operation
0F|12|none|Set DUP+ operation
10|00|none|Send/read 10 Hz (1 Hz) tuning step
10|01|none|Send/read 0.1 kHz tuning step
10|02|none|Send/read 1 kHz tuning step
10|03|none|Send/read 5 kHz tuning step
10|04|none|Send/read 6.25 kHz tuning step
10|05|none|Send/read 9 kHz tuning step
10|06|none|Send/read 10 kHz tuning step
10|07|none|Send/read 12.5 kHz tuning step
10|08|none|Send/read 20 kHz tuning step
10|09|none|Send/read 25 kHz tuning step
10|10|none|Send/read 50 kHz tuning step
10|11|none|Send/read 100 kHz tuning step
10|12|none|Send/read 1 MHz tuning step
11|00|none|Send/read attenuator OFF
11|12|none|Send/read 12 dB attenuator
13|00|none|Announce frequency, mode and S-meter by voice synthesizer
13|01|none|Announce frequency and S-meter by voice synthesizer
13|02|none|Announce mode by voice synthesizer
14|01|BCD 2 bytes 0000~0255|AF level
14|02|BCD 2 bytes 0000~0255|RF gain level
14|03|BCD 2 bytes 0000~0255|Squelch level
14|06|BCD 2 bytes 0000~0255|NR level
14|07|BCD 2 bytes 0000~0255|Inner TWIN PBT position
14|08|BCD 2 bytes 0000~0255|Outer TWIN PBT position
14|09|BCD 2 bytes 0000~0255|CW pitch
14|0A|BCD 2 bytes 0000~0255|RF power level
14|0B|BCD 2 bytes 0000~0255|MIC gain level
14|0C|BCD 2 bytes 0000~0255|Key speed
14|0D|BCD 2 bytes 0000~0255|Notch position
14|0E|BCD 2 bytes 0000~0255|COMP level
14|0F|BCD 2 bytes 0000~0255|Break-in delay
14|12|BCD 2 bytes 0000~0255|NB level
14|15|BCD 2 bytes 0000~0255|Monitor gain level
14|16|BCD 2 bytes 0000~0255|VOX gain level
14|17|BCD 2 bytes 0000~0255|Anti-VOX gain level
14|18|BCD 2 bytes 0000~0255|LCD contrast level
14|19|BCD 2 bytes 0000~0255|LCD backlight level
15|01|1 byte 00/01|Read squelch status
15|02|BCD 2 bytes 0000~0255|Read S-meter level
15|05|1 byte 00/01|Read various SQL function status
15|11|BCD 2 bytes 0000~0255|Read PO meter level
15|12|BCD 2 bytes 0000~0255|Read SWR meter level
15|13|BCD 2 bytes 0000~0255|Read ALC meter level
15|14|BCD 2 bytes 0000~0255|Read COMP meter level
15|15|BCD 2 bytes 0000~0255|Read Vd meter level
15|16|BCD 2 bytes 0000~0255|Read Id meter level
16|02|1 byte 00/01/02|Preamp
16|12|1 byte 01/02/03|AGC
16|22|1 byte 00/01|Noise blanker
16|40|1 byte 00/01|Noise reduction
16|41|1 byte 00/01|Auto notch
16|42|1 byte 00/01|Repeater tone
16|43|1 byte 00/01|Tone squelch
16|44|1 byte 00/01|Speech compressor
16|45|1 byte 00/01|Monitor function
16|46|1 byte 00/01|VOX function
16|47|1 byte 00/01/02|Break-in function
16|48|1 byte 00/01|Manual notch
16|4B|1 byte 00/01|DTCS
16|4C|1 byte 00/01|VSC function
16|4F|1 byte 00/01|Twin peak filter
16|50|1 byte 00/01|Dial lock function
16|56|1 byte 00/01|DSP filter type
16|57|1 byte 00/01/02|Manual notch width
16|58|1 byte 00/01/02|SSB transmit bandwidth
16|5B|1 byte 00/01/02|DSQL/CSQL (DV mode only)
17||ASCII text up to 30 chars (FF stops)|Send CW message
18|00|none|Turn OFF the transceiver
18|01|none|Turn ON the transceiver
19|00|none (reply 1 byte ID)|Read transceiver ID
1A|00|memory number BCD 2 bytes + memory data|Send/read memory channel contents
1A|01|band code + register code + data|Send/read band stacking register contents
1A|02|channel 01-04 + ASCII text|Send/read memory keyer contents
1A|03|1 byte 00~49|Send/read selected filter width
1A|04|1 byte 00~13|Send/read selected AGC time constant
1A|05 00 01|00/01|TX Monitor function setting
1A|05 00 02|0000 to 0255|TX Monitor level
1A|05 00 03|0000 to 0255 (from range in description)|Beep level
1A|05 00 04|00/01|Beep level limit setting
1A|05 00 05|00/01|Confirmation beep setting
1A|05 00 06|00 to 03|Band edge beep setting
1A|05 00 07|00 to 02|RF/SQL Control setting
1A|05 00 08|00 to 05|TX Delay setting (HF)
1A|05 00 09|00 to 05|TX Delay setting (50M)
1A|05 00 10|00 to 05|TX Delay setting (70M)
1A|05 00 11|00 to 05|TX Delay setting (144M)
1A|05 00 12|00 to 05|TX Delay setting (430M)
1A|05 00 13|00 to 05|Time-Out Timer setting
1A|05 00 14|00/01|PTT Lock function setting
1A|05 00 15|00/01|Quick Split function setting
1A|05 00 16|see p. 20-13|Split offset frequency
1A|05 00 17|00/01|Split Lock function setting
1A|05 00 18|see p. 20-13|Duplex offset fre- quency
1A|05 00 19|00/01|One Touch Repeater setting
1A|05 00 20|00 to 02|Auto Repeater set- ting
1A|05 00 21|00/01|Tuner Auto Start setting
1A|05 00 22|00/01|PTT Tune setting
1A|05 00 23|00/01|00=the Manual selection for the [TUNER] Switch function. ; 01=the Auto selection for the [TUNER] Switch function.
1A|05 00 24|00/01|[SPEECH/LOCK] key function setting
1A|05 00 25|00/01|Lock function setting
1A|05 00 26|00/01|number of memo pad channels
1A|05 00 27|00 to 02|Auto TS setting for the Dial
1A|05 00 28|00/01|microphone Up/ Down speed setting
1A|05 00 29|00 to 02|Notch function set- ting for SSB mode
1A|05 00 30|00 to 02|Notch function set- ting for AM mode
1A|05 00 31|00/01|SSB/CW Synchro- nous Tuning function setting
1A|05 00 32|00/01|CW normal side setting
1A|05 00 33|00/01|voice 1st menu
1A|05 00 34|00/01|keyer 1st menu
1A|05 00 35|00/01|Speaker output setting
1A|05 00 36|00/01|MIC AF output set- ting
1A|05 00 37|00 to 22|function of [F-1] on the HM-151
1A|05 00 38|00 to 22|function of [F-2] on the HM-151
1A|05 00 39|00/01|SSB mode selection of the [MODE] key on the HM-151
1A|05 00 40|00/01|CW mode selection of the [MODE] key on the HM-151
1A|05 00 41|00/01|RTTY mode selec- tion of the [MODE] key on the HM-151
1A|05 00 42|00/01|AM mode selection of the [MODE] key on the HM-151
1A|05 00 43|00/01|FM mode selection of the [MODE] key on the HM-151
1A|05 00 44|00/01|DV mode selection of the [MODE] key on the HM-151
1A|05 00 45|00/01|WFM mode selection of the [MODE] key on the HM-151
1A|05 00 46|00/01|Power OFF setting when no controller is connected.
1A|05 00 47|0000 to 0255|REF Adjust setting
1A|05 00 48|00 to 02|RX Call Sign SPEECH setting
1A|05 00 49|00/01|RX>CS SPEECH function setting
1A|05 00 50|00/01|S-Level SPEECH function setting
1A|05 00 51|00/01|MODE SPEECH function setting
1A|05 00 52|00/01|speech language
1A|05 00 53|00/01|Alphabet setting for SPEECH
1A|05 00 54|00/01|speech speed set- ting
1A|05 00 55|0000 to 0255|speech level
1A|05 00 56|see p. 20-11|SSB RX HPF/LPF setting
1A|05 00 57|00 to 10|SSB RX Tone (Bass) level
1A|05 00 58|00 to 10|SSB RX Tone (Treble) level
1A|05 00 59|see p. 20-11|AM RX HPF/LPF setting
1A|05 00 60|00 to 10|AM RX tone (Bass) level
1A|05 00 61|00 to 10|AM RX Tone (Treble) level
1A|05 00 62|see p. 20-11|FM RX HPF/LPF setting
1A|05 00 63|00 to 10|FM RX tone (Bass) level
1A|05 00 64|00 to 10|FM RX Tone (Treble) level
1A|05 00 65|see p. 20-11|DV RX HPF/LPF setting
1A|05 00 66|00 to 10|DV RX tone (Bass) level
1A|05 00 67|00 to 10|DV RX Tone (Treble) level
1A|05 00 68|00 to 10|WFM RX Tone (Bass) level
1A|05 00 69|00 to 10|WFM RX Tone (Treble) level
1A|05 00 70|00 to 10|CW RX HPF/LPF setting
1A|05 00 71|00 to 10|RTTY RX HPF/LPF setting
1A|05 00 72|00 to 10|SSB TX Tone (Bass) level
1A|05 00 73|00 to 10|SSB TX Tone (Treble) level
1A|05 00 74|see p. 20-11|WIDE SSB TX bandwidth
1A|05 00 75|see p. 20-11|MID SSB TX band- width
1A|05 00 76|see p. 20-11|NARROW SSB TX bandwidth
1A|05 00 77|00 to 10|AM TX tone (Bass) level
1A|05 00 78|00 to 10|AM TX Tone (Treble) level
1A|05 00 79|00 to 10|FM TX tone (Bass) level
1A|05 00 80|00 to 10|FM TX Tone (Treble) level
1A|05 00 81|00 to 10|DV TX tone (Bass) level
1A|05 00 82|00 to 10|DV TX Tone (Treble) level
1A|05 00 83|00/01|USB audio squelch setting
1A|05 00 84|00/01|ACC and USB output setting
1A|05 00 85|0000 to 0255|ACC and USB AF output Level
1A|05 00 86|0000 to 0255|ACC and USB IF output Level
1A|05 00 87|0000 to 0255|ACC modulation level
1A|05 00 88|0000 to 0255|DATA modulation level
1A|05 00 89|0000 to 0255|USB modulation level
1A|05 00 90|00 to 03|modulation input set- ting in the DATA mode OFF
1A|05 00 91|00 to 03|modulation input set- ting in the DATA mode ON
1A|05 00 92|00/01|external keypad set- ting for VOICE
1A|05 00 93|00/01|external keypad set- ting for Memory KEYER
1A|05 00 94|00/01|external keypad set- ting for RTTY Memory
1A|05 00 95|00/01|CI-V transceive setting
1A|05 00 96|00 to 02|""USB2"" (COM port) function setting
1A|05 00 97|00 to 03|[DATA1] function setting
1A|05 00 98|00/01|GPS output setting
1A|05 00 99|00/01|DV or GPS data transfer speed
1A|05 01 00|00 to 04|RTTY decode speed
1A|05 01 01|00 to 02|band setting for the [ACC] socket's pin 7 (VSEND usage)
1A|05 01 02|00/01|9600bps Mode setting
1A|05 01 03|0000 to 0255|LCD contrast setting
1A|05 01 04|0000 to 0255|LCD Backlight set- ting
1A|05 01 05|0000 to 0255|Key Backlight setting
1A|05 01 06|00/01|Meter Peak Hold setting
1A|05 01 07|00/01|PBT shifting value display setting while rotating [TWIN PBT]
1A|05 01 08|00/01|IF filter width and shifting value display setting when the IF filter is switched
1A|05 01 09|00 to 02|RX Call sign display setting
1A|05 01 10|00/01|RX message display setting
1A|05 01 11|00/01|RX position display setting when the received Auto Reply signal includes the position
1A|05 01 12|00 to 02|TX Call sign display setting
1A|05 01 13|00/01|Scroll Speed setting
1A|05 01 14|00/01|VOICE TX Name Display setting
1A|05 01 15|00/01|KEYER Memory Display setting
1A|05 01 16|00/01|Opening Message
1A|05 01 17|00/01|Power ON Check setting
1A|05 01 18|00/01|Display Language
1A|05 01 19|00/01|System Language
1A|05 01 20|20000101 to 20991231|date setting
1A|05 01 21|0000 to 2359|time setting (0000(0:00)to2359(23:59))
1A|05 01 22|00/01|GPS time correction setting
1A|05 01 23|see p. 20-13|UTC offset setting
1A|05 01 24|00/01|clock display mode
1A|05 01 25|00 to 04|Auto Power OFF function setting
1A|05 01 26|00 to 10|compression level
1A|05 01 27|00 to 14|repeat interval to transmit the recorded voice audio
1A|05 01 28|00/01|TX voice audio moni- tor function setting
1A|05 01 29|00 to 04|numbering system used for contest (serial) numbers
1A|05 01 30|01 to 04|count-up trigger channel
1A|05 01 31|0001 to 9999|current contest serial number
1A|05 01 32|0000 to 0255|CW sidetone level
1A|05 01 33|00/01|CW sidetone level limit setting
1A|05 01 34|01 to 60|CW keyer repeat time
1A|05 01 35|28 to 45|CW keyer dot/dash ratio
1A|05 01 36|00 to 03|CW Rise time setting
1A|05 01 37|00/01|paddle polarity setting
1A|05 01 38|00 to 02|keyer type setting
1A|05 01 39|00/01|Mic. up/down keyer setting
1A|05 01 40|00/01|Twin Peak Filter setting
1A|05 01 41|00 to 02|RTTY mark fre- quency
1A|05 01 42|00 to 02|RTTY shift width
1A|05 01 43|00/01|RTTY keying polarity
1A|05 01 44|00/01|RTTY decode USOS setting
1A|05 01 45|00/01|RTTY decode new line code setting
1A|05 01 46|00/01|RTTY TX USOS setting
1A|05 01 47|00/01|RTTY Decode Log setting
1A|05 01 48|00/01|RTTY Decode Log file type
1A|05 01 49|00/01|RTTY Decode Log Time Stamp setting
1A|05 01 50|00/01|RTTY Decode Log Time Stamp (Time)
1A|05 01 51|00/01|RTTY Decode Log Time Stamp (Frequency)
1A|05 01 52|00 to 03|DTMF Speed setting
1A|05 01 53|00/01|Scan speed setting
1A|05 01 54|00/01|Scan resume setting
1A|05 01 55|00 to 10|Scan pause timer setting
1A|05 01 56|00 to 06|Scan resume timer
1A|05 01 57|00/01|Dial function during a scan
1A|05 01 58|0000 to 0255|NB level
1A|05 01 59|00 to 09|NB depth
1A|05 01 60|0000 to 0255|NB width
1A|05 01 61|00 to 15|NR level for other than the DR mode
1A|05 01 62|00 to 15|NR level for the DR mode
1A|05 01 63|0000 to 0255|VOX gain
1A|05 01 64|0000 to 0255|ANTI-VOX gain
1A|05 01 65|00 to 20|VOX delay time
1A|05 01 66|0020 to 0130|BK-IN delay time
1A|05 01 67|00/01|recording mode
1A|05 01 68|00/01|squelch status for the RX voice audio recording
1A|05 01 69|00/01|QSO audio record file Split function setting
1A|05 01 70|00/01|PTT Automatic Recording function setting
1A|05 01 71|00 to 03|Skip Timer setting while playing back
1A|05 01 72|00 to 02|Standby Beep set- ting
1A|05 01 73|00 to 02|Auto Reply setting
1A|05 01 74|00/01|DV Data TX setting
1A|05 01 75|00 to 02|Digital Monitor set- ting
1A|05 01 76|00/01|Digital Repeater set- ting function setting
1A|05 01 77|00/01|RX Call Sign Auto Write setting
1A|05 01 78|00/01|RX RPT Call Sign Auto Write setting
1A|05 01 79|00/01|DV Auto Detect setting
1A|05 01 80|00/01|RX Record (RPT) setting
1A|05 01 81|00/01|BK function setting
1A|05 01 82|00/01|EMR mode setting
1A|05 01 83|0000 to 0255|EMR AF Level
1A|05 01 84|00 to 02|external GPS re- ceiver setting
1A|05 01 85|00/01|GPS Receiver Baud setting
1A|05 01 86|see p. 20-13|manually pro- grammed position
1A|05 01 87|00/01|GPS Indicator set- ting
1A|05 01 88|00/01|Position Format setting
1A|05 01 89|00/01|Distance and Altitude unit
1A|05 01 90|00 to 02|GPS speed unit
1A|05 01 91|see p. 20-13|GPS alarm area (Group)
1A|05 01 92|00 to 02|GPS alarm area (RX/Memory)
1A|05 01 93|00 to 02|GPS TX Mode setting
1A|05 01 94|00/01|GPS (RMC) Sentence setting
1A|05 01 95|00/01|GPS (GGA) Sentence setting
1A|05 01 96|00/01|GPS (GLL) Sentence setting
1A|05 01 97|00/01|GPS (GSA) Sentence setting
1A|05 01 98|00/01|GPS (VTG) Sentence setting
1A|05 01 99|00/01|GPS (GSV) Sentence setting
1A|05 02 00|see p. 20-14|GPS Message
1A|05 02 01|see p. 20-13|Unproto Address
1A|05 02 02|00/01|position data extension setting
1A|05 02 03|00 to 02|GPS-A Time Stamp setting
1A|05 02 04|00/01|GPS-A altitude setting
1A|05 02 05|00 to 03|GPS-A Symbol
1A|05 02 06|see p. 20-13|GPS-A Symbol No.1 setting
1A|05 02 07|see p. 20-13|GPS-A Symbol No.2 setting
1A|05 02 08|see p. 20-13|GPS-A Symbol No.3 setting
1A|05 02 09|see p. 20-13|GPS-A Symbol No.4 setting
1A|05 02 10|00 to 42|GPS-A SSID
1A|05 02 11|see p. 20-14|GPS-A comment
1A|05 02 12|00 to 08|GPS Auto TX inter- val setting
1A|05 02 13|00/01|QSO Log setting
1A|05 02 14|00/01|RX History Log func- tion setting
1A|05 02 15|00 to 02|QSO/RX Log CSV format setting (Separator/Decimal)
1A|05 02 16|00 to 02|QSO/RX Log CSV format setting (Date)
1A|05 02 17|00/01|Weather Alert setting (USA only)
1A|05 02 18|00 to 02|Memory Name Display setting [Systemlanguage:English]
1A|05 02 19|00/01|Display Type setting for the DR mode
1A|05 02 20|00 to 02|Compass Direction setting
1A|06|2 bytes: data mode 00/01 + filter 01~03|Send/read DATA mode setting
1B|00|BCD 3 bytes (00 + 100Hz/10Hz + 1Hz/0.1Hz)|Send/read repeater tone frequency
1B|01|BCD 3 bytes|Send/read tone squelch frequency
1B|02|3 bytes polarity + DTCS code|Send/read DTCS code and polarity
1B|07|BCD 1 byte 00~99|Send/read CSQL code (DV mode)
1C|00|1 byte 00/01|Send/read transceiver status (PTT)
1C|01|1 byte 00/01/02|Send/read antenna tuner
1C|02|1 byte 00/01|Send/read TX frequency monitor check
1E|00|none|Read number of available TX frequency bands
1E|01|band edge data|Read TX band edge frequencies
1E|02|none|Read number of user-set TX frequency bands
1E|03|band edge data|Send/read user-set TX band edge frequencies
1F|00|12 chars call + note|Send/read DV MY call sign
1F|01|8-char UR/R1/R2|Send/read DV TX call signs
1F|02|ASCII|Send/read DV TX message
20|00 00|1 byte 00/01|Send/read auto DV RX call signs output
20|00 01|see p.20-15|Output the DV RX call signs
20|00 02|see p.20-15|Read the DV RX call signs
20|01 00|1 byte 00/01|Send/read auto DV RX message output
20|01 01|see p.20-15|Output the DV RX message
20|01 02|see p.20-15|Read the DV RX message
20|02 00|1 byte 00/01|Send/read auto DV RX status output
20|02 01|see p.20-15|Output the DV RX status
20|02 02|see p.20-15|Read the DV RX status
";
            D["ic7300"] = @"
00||BCD 5 bytes frequency (10 Hz/1 Hz byte first)|Send frequency data (transceive)
01||mode byte + filter byte|Send mode data (transceive)
02||none (read) -> 2x BCD 5 bytes with separator 2D|Read band edge frequencies
03||BCD 5 bytes frequency|Read operating frequency
04||mode byte + filter byte|Read operating mode
05||BCD 5 bytes frequency|Set operating frequency
06||mode byte + optional filter byte|Operating mode selection
07||none|Select VFO mode
07|00|none|Select VFO A
07|01|none|Select VFO B
07|A0|none|Equalize VFO A and VFO B
07|B0|none|Exchange VFO A and VFO B
08||none|Select Memory mode
08||BCD 2 bytes 0001~0109|Select memory channel
09||none|Memory write
0A||none|Memory copy to VFO
0B||none|Memory clear
0E|00|none|Scan stop
0E|01|none|Programmed/memory scan start
0E|02|none|Programmed scan start
0E|03|none|F scan start
0E|12|none|Fine programmed scan start
0E|13|none|Fine F scan start
0E|22|none|Memory scan start
0E|23|none|Select memory scan start
0E|A1|none|Select F scan span +-5 kHz
0E|A2|none|Select F scan span +-10 kHz
0E|A3|none|Select F scan span +-20 kHz
0E|A4|none|Select F scan span +-50 kHz
0E|A5|none|Select F scan span +-100 kHz
0E|A6|none|Select F scan span +-500 kHz
0E|A7|none|Select F scan span +-1 MHz
0E|B0|none|Set as non-select channel
0E|B1|none|Set as select channel
0E|B1 01~03|none|Set as select channel SEL1..SEL3
0E|B2|1 byte 00~03|Set for select memory scan
0E|D0|none|Set scan resume OFF
0E|D3|none|Set scan resume ON
0F||none (read) / reply 1 byte 00/01|Read split setting
0F|00|none|Turn split OFF
0F|01|none|Turn split ON
10|00|none|Send/read tuning step OFF
10|01|none|Send/read 100 Hz tuning step
10|02|none|Send/read 1 kHz tuning step
10|03|none|Send/read 5 kHz tuning step
10|04|none|Send/read 9 kHz tuning step
10|05|none|Send/read 10 kHz tuning step
10|06|none|Send/read 12.5 kHz tuning step
10|07|none|Send/read 20 kHz tuning step
10|08|none|Send/read 25 kHz tuning step
11||1 byte 00/20|Send/read attenuator
13|00|none|Speech all data with voice synthesizer
13|01|none|Speech frequency and S-meter by voice synthesizer
13|02|none|Speech operating mode by voice synthesizer
14|01|BCD 2 bytes 0000~0255|AF level
14|02|BCD 2 bytes 0000~0255|RF gain level
14|03|BCD 2 bytes 0000~0255|Squelch level
14|06|BCD 2 bytes 0000~0255|NR level
14|07|BCD 2 bytes 0000~0255|Inner TWIN PBT position
14|08|BCD 2 bytes 0000~0255|Outer TWIN PBT position
14|09|BCD 2 bytes 0000~0255|CW pitch
14|0A|BCD 2 bytes 0000~0255|RF PWR position
14|0B|BCD 2 bytes 0000~0255|MIC gain position
14|0C|BCD 2 bytes 0000~0255|KEY SPEED level
14|0D|BCD 2 bytes 0000~0255|NOTCH position
14|0E|BCD 2 bytes 0000~0255|COMP level
14|0F|BCD 2 bytes 0000~0255|Break-in delay
14|12|BCD 2 bytes 0000~0255|NB level
14|15|BCD 2 bytes 0000~0255|Monitor gain
14|16|BCD 2 bytes 0000~0255|VOX gain
14|17|BCD 2 bytes 0000~0255|Anti-VOX gain
14|19|BCD 2 bytes 0000~0255|BRIGHT level
15|01|1 byte 00/01|Read noise or S-meter squelch status
15|02|BCD 2 bytes 0000~0255|Read S-meter level
15|05|1 byte 00/01|Read various squelch function status
15|11|BCD 2 bytes 0000~0255|Read PO meter level
15|12|BCD 2 bytes 0000~0255|Read SWR meter level
15|13|BCD 2 bytes 0000~0255|Read ALC meter level
15|14|BCD 2 bytes 0000~0255|Read COMP meter level
15|15|BCD 2 bytes 0000~0255|Read Vd meter level
15|16|BCD 2 bytes 0000~0255|Read Id meter level
16|02|1 byte 00~02|Preamp
16|12|1 byte 00~03|AGC
16|22|1 byte 00/01|Noise blanker
16|40|1 byte 00/01|Noise reduction
16|41|1 byte 00/01|Auto notch
16|42|1 byte 00/01|Repeater tone
16|43|1 byte 00/01|Tone squelch
16|44|1 byte 00/01|Speech compressor
16|45|1 byte 00/01|Monitor function
16|46|1 byte 00/01|VOX function
16|47|1 byte 00~02|BK-IN function
16|48|1 byte 00/01|Manual notch
16|4F|1 byte 00/01|Twin peak filter
16|50|1 byte 00/01|Dial lock function
16|56|1 byte 00/01|DSP filter type
16|57|1 byte 00~02|Manual notch width
16|58|1 byte 00~02|SSB transmit bandwidth
17||ASCII text up to 30 chars (FF stops)|Send CW message
18|00|none|Turn OFF the transceiver
18|01|none|Turn ON the transceiver
19|00|none (reply 1 byte ID)|Read the transceiver ID
1A|00|memory number BCD 2 bytes + memory data|Send/read memory contents
1A|01|band code + register code + data|Send/read band stacking register contents
1A|02|channel 01-08 + ASCII text|Send/read memory keyer contents
1A|03|1 byte 00~49|Send/read selected filter width
1A|04|1 byte 00~13|Send/read selected AGC time constant
1A|05 00 01|p. 19-8|SSB RX HPF/LPF settings
1A|05 00 02|00 to 10|SSB RX Tone (Bass) level
1A|05 00 03|00 to 10|SSB RX Tone (Treble) level
1A|05 00 04|p. 19-8|AM RX HPF/LPF settings
1A|05 00 05|00 to 10|AM RX Tone (Bass) level
1A|05 00 06|00 to 10|AM RX Tone (Treble) level
1A|05 00 07|p. 19-8|FM RX HPF/LPF settings
1A|05 00 08|00 to 10|FM RX Tone (Bass) level
1A|05 00 09|00 to 10|FM RX Tone (Treble) level
1A|05 00 10|p. 19-8|CW RX HPF/LPF settings
1A|05 00 11|p. 19-8|RTTY RX HPF/LPF settings
1A|05 00 12|00 to 10|SSB TX Tone (Bass) level
1A|05 00 13|00 to 10|SSB TX Tone (Treble) level
1A|05 00 14|p. 19-8|SSB TX bandwidth for wide
1A|05 00 15|p. 19-8|SSB TX bandwidth for mid
1A|05 00 16|p. 19-8|SSB TX bandwidth for narrow
1A|05 00 17|00 to 10|AM TX Tone (Bass) level
1A|05 00 18|00 to 10|AM TX Tone (Treble) level
1A|05 00 19|00 to 10|FM TX Tone (Bass) level
1A|05 00 20|00 to 10|FM TX Tone (Treble) level
1A|05 00 21|0000 to 0255|beep gain
1A|05 00 22|00/01|beep gain limit *
1A|05 00 23|00/01|confirmation beep
1A|05 00 24|00/01/02/03|00=the band edge beep OFF ; 01=the band edge beep ON (Beep sounds with a default amateur band) ; 02=the band edge beep with user setting ON ; 03=the band edge beep with user setting/TX limit ON
1A|05 00 25|00 to 02|RF/SQL Control setting
1A|05 00 26|00 to 05|TX Delay setting (HF)
1A|05 00 27|00 to 05|TX Delay setting
1A|05 00 28|00 to 05|TX Delay setting
1A|05 00 29|00 to 05|Time-Out Timer setting
1A|05 00 30|00/01|quick split set *
1A|05 00 31|p. 19-9|FM split offset -9.999 to +9.999 MHz for HF
1A|05 00 32|p. 19-9|FM split offset -9.999 to +9.999 MHz for 50 MHz
1A|05 00 33|00/01|split lock set *
1A|05 00 34|00/01|[TUNER] Switch set
1A|05 00 35|00 or 01|PTT tune set *
1A|05 00 36|00 to 02|RTTY mark frequency
1A|05 00 37|00 to 02|RTTY shift width
1A|05 00 38|00/01|RTTY keying polarity
1A|05 00 39|00/01|speech language
1A|05 00 40|00/01|speech speed
1A|05 00 41|00/01|S-level speech
1A|05 00 42|00/01|speech with a mode switch operation
1A|05 00 43|0000 to 0255|speech level
1A|05 00 44|00/01|[SPEECH/LOCK] key function setting
1A|05 00 45|00/01|Lock function setting
1A|05 00 46|00/01|memo pad numbers
1A|05 00 47|00 to 02|main dial auto TS
1A|05 00 48|00/01|mic. up/down speed
1A|05 00 49|00 or 01|quick RIT/TX clear function
1A|05 00 50|00 to 02|SSB notch operation *
1A|05 00 51|00 to 02|AM notch operation
1A|05 00 52|00/01|SSB/CW synchronous tuning function
1A|05 00 53|00/01|CW normal side set
1A|05 00 54|00/01|screen capture by the [POWER] switch
1A|05 00 55|00/01|screen capture image data saving format
1A|05 00 56|00/01|keyboard type
1A|05 00 57|00/01|calibration marker
1A|05 00 58|0000 to 0255|reference frequency
1A|05 00 59|00 or 01|AF/IF signal output to ACC/USB
1A|05 00 60|0000 to 0255|AF output level to ACC/USB
1A|05 00 61|00/01|squelch function for the AF signal output to ACC/USB
1A|05 00 62|00/01|beep and speech output setting to ACC/USB (when AF signal output is set)
1A|05 00 63|0000 to 0255|IF signal output level to ACC/USB
1A|05 00 64|0000 to 0255|MOD input level from ACC
1A|05 00 65|0000 to 0255|MOD input level from USB
1A|05 00 66|00 to 03|MOD input connector during DATA OFF
1A|05 00 67|00 to 03|MOD input connector during DATA
1A|05 00 68|00/01|external keypad setting for VOICE *
1A|05 00 69|00/01|external keypad setting for Memory KEYER
1A|05 00 70|00/01|external keypad setting for RTTY Memory
1A|05 00 71|00/01|CI-V transceive setting
1A|05 00 72|0000 to 0223|transceive CI-V Address for USB to REMOTE in hexadecimal code
1A|05 00 73|00/01|CI-V Output (for ANT) capability
1A|05 00 74|00/01|CI-V USB port setting
1A|05 00 75|00/01|echo back setting for CI-V operation from USB
1A|05 00 76|00/01|USB (serial port) function setting
1A|05 00 77|00 to 03|data transfer speed for RTTY decode output
1A|05 00 78|00 to 02|transmission control line setting for USB
1A|05 00 79|00 to 02|CW keying line setting for USB
1A|05 00 80|00 to 02|RTTY (FSK) line setting for USB
1A|05 00 81|0000 to 0255|LCD unit backlight brightness
1A|05 00 82|00/01|screen image type
1A|05 00 83|00/01|frequency readout font
1A|05 00 84|00/01|peak hold set for meter *
1A|05 00 85|00/01|memory name indication setting
1A|05 00 86|00/01|manual notch width pop-up indication setting
1A|05 00 87|00/01|PBT shifting value display setting while rotating [TWIN PBT]
1A|05 00 88|00/01|IF filter width and shifting value display setting when the IF filter is switched
1A|05 00 89|00 to 03|screen saver function
1A|05 00 90|00/01|opening message indication
1A|05 00 91|p. 19-9|opening message contents (up to 10-character)
1A|05 00 92|00/01|Power ON Check setting
1A|05 00 93|00/01|Display Language
1A|05 00 94|20000101 to 20991231|date setting
1A|05 00 95|0000 to 2359|time setting
1A|05 00 96|p. 19-11|UTC offset time
1A|05 00 97|00/01|scope indication during TX
1A|05 00 98|00 to 02|scope max. hold
1A|05 00 99|00 to 02|scope center frequency set
1A|05 01 00|00/01|scope marker position setting during fix type scope
1A|05 01 01|00/01|external monitor signal width
1A|05 01 02|00 to 03|averaging function for spectrum scope
1A|05 01 03|00/01|spectrum display type
1A|05 01 04|p. 19-8|spectrum fill color
1A|05 01 05|p. 19-8|spectrum line color
1A|05 01 06|p. 19-8|spectrum color for peak hold
1A|05 01 07|00/01|waterfall set for spectrum scope
1A|05 01 08|00 to 02|waterfall speed
1A|05 01 09|00 to 02|waterfall height when expanded scope is selected
1A|05 01 10|00 to 07|peak color level set for waterfall of the spectrum scope
1A|05 01 11|00/01|scope waterfall marker auto-hide
1A|05 01 12|p. 19-8|scope edge 1 frequencies for 0.03 to 1.60 MHz band
1A|05 01 13|p. 19-8|scope edge 2 frequencies for 0.03 to 1.60 MHz band
1A|05 01 14|p. 19-8|scope edge 3 frequencies for 0.03 to 1.60 MHz band
1A|05 01 15|p. 19-8|scope edge 1 frequencies for 1.60 to 2.00 MHz band
1A|05 01 16|p. 19-8|scope edge 2 frequencies for 1.60 to 2.00 MHz band
1A|05 01 17|p. 19-8|scope edge 3 frequencies for 1.60 to 2.00 MHz band
1A|05 01 18|p. 19-8|scope edge 1 frequencies for 2.00 to 6.00 MHz band
1A|05 01 19|p. 19-8|scope edge 2 frequencies for 2.00 to 6.00 MHz band
1A|05 01 20|p. 19-8|scope edge 3 frequencies for 2.00 to 6.00 MHz band
1A|05 01 21|p. 19-8|scope edge 1 frequencies for 6.00 to 8.00 MHz band
1A|05 01 22|p. 19-8|scope edge 2 frequencies for 6.00 to 8.00 MHz band
1A|05 01 23|p. 19-8|scope edge 3 frequencies for 6.00 to 8.00 MHz band
1A|05 01 24|p. 19-8|scope edge 1 frequencies for 8.00 to 11.00 MHz band
1A|05 01 25|p. 19-8|scope edge 2 frequencies for 8.00 to 11.00 MHz band
1A|05 01 26|p. 19-8|scope edge 3 frequencies for 8.00 to 11.00 MHz band
1A|05 01 27|p. 19-8|scope edge 1 frequencies for 11.00 to 15.00 MHz band
1A|05 01 28|p. 19-8|scope edge 2 frequencies for 11.00 to 15.00 MHz band D Command table (Continued) 19 CONTROL COMMAND Remote control (CI-V) information (Continued)
1A|05 01 29|p. 19-8|scope edge 3 frequencies for 11.00 to 15.00 MHz band
1A|05 01 30|p. 19-8|scope edge 1 frequencies for 15.00 to 20.00 MHz band
1A|05 01 31|p. 19-8|scope edge 2 frequencies for 15.00 to 20.00 MHz band
1A|05 01 32|p. 19-8|scope edge 3 frequencies for 15.00 to 20.00 MHz band
1A|05 01 33|p. 19-8|scope edge 1 frequencies for 20.00 to 22.00 MHz band
1A|05 01 34|p. 19-8|scope edge 2 frequencies for 20.00 to 22.00 MHz band
1A|05 01 35|p. 19-8|scope edge 3 frequencies for 20.00 to 22.00 MHz band
1A|05 01 36|p. 19-8|scope edge 1 frequencies for 22.00 to 26.00 MHz band
1A|05 01 37|p. 19-8|scope edge 2 frequencies for 22.00 to 26.00 MHz band
1A|05 01 38|p. 19-8|scope edge 3 frequencies for 22.00 to 26.00 MHz band
1A|05 01 39|p. 19-8|scope edge 1 frequencies for 26.00 to 30.00 MHz band
1A|05 01 40|p. 19-8|scope edge 2 frequencies for 26.00 to 30.00 MHz band
1A|05 01 41|p. 19-8|scope edge 3 frequencies for 26.00 to 30.00 MHz band
1A|05 01 42|p. 19-8|scope edge 1 frequencies for 30.00 to 45.00 MHz band
1A|05 01 43|p. 19-8|scope edge 2 frequencies for 30.00 to 45.00 MHz band
1A|05 01 44|p. 19-8|scope edge 2 frequencies for 30.00 to 45.00 MHz band
1A|05 01 45|p. 19-8|scope edge 1 frequencies for 45.00 to 60.00 MHz band
1A|05 01 46|p. 19-8|scope edge 2 frequencies for 45.00 to 60.00 MHz band
1A|05 01 47|p. 19-8|scope edge 3 frequencies for 45.00 to 60.00 MHz band
1A|05 01 48|p. 19-8|scope edge 1 frequencies for 60.00 to 74.80 MHz band
1A|05 01 49|p. 19-8|scope edge 2 frequencies for 60.00 to 74.80 MHz band
1A|05 01 50|p. 19-8|scope edge 3 frequencies for 60.00 to 74.80 MHz band
1A|05 01 51|00/01|audio FFT scope display type
1A|05 01 52|p. 19-8|Audio FFT scope waveform color
1A|05 01 53|00/01|Audio FFT scope waterfall display
1A|05 01 54|p. 19-8|Audio Oscilloscope scope waveform color
1A|05 01 55|00/01/02/03/04|Normal selection for contest number style 01 ""190ANO"" selection for contest number style 02 ""190ANT"" selection for contest number style 03 ""90NO"" selection for contest number style 04 ""90NT"" selection for contest number style
1A|05 01 56|01 to 08|count up trigger channel
1A|05 01 57|0001 to 9999|present number
1A|05 01 58|0000 to 0255|CW side tone gain
1A|05 01 59|00/01|CW side tone gain limit
1A|05 01 60|01 to 60|CW keyer repeat time
1A|05 01 61|28 to 45|CW keyer dot/dash ratio
1A|05 01 62|00 to 03|rise time
1A|05 01 63|00/01|paddle polarity
1A|05 01 64|00 to 02|keyer type
1A|05 01 65|00/01|mic. up/down keyer set
1A|05 01 66|00 to 03|averaging function for RTTY FFT scope
1A|05 01 67|p. 19-8|RTTY FFT scope waveform color
1A|05 01 68|00/01|RTTY decode USOS
1A|05 01 69|00/01|RTTY decode new line code
1A|05 01 70|00/01|RTTY TX USOS
1A|05 01 71|p. 19-8|received RTTY text font color
1A|05 01 72|p. 19-8|transmitted RTTY text font color
1A|05 01 73|00/01|RTTY log function
1A|05 01 74|00/01|file saving format for the RTTY log
1A|05 01 75|00/01|RTTY time stamp set
1A|05 01 76|00/01|RTTY Decode Log Time Stamp
1A|05 01 77|00/01|RTTY frequency stamp
1A|05 01 78|00/01|scan speed
1A|05 01 79|00/01|scan resume
1A|05 01 80|00/01|auto monitor function setting when transmitting a recorded voice memory
1A|05 01 81|01/15|repeat interval to transmit recorded voice audio
1A|05 01 82|00/01|recording mode for QSO recorder
1A|05 01 83|00/01|recording TX audio for QSO recorder
1A|05 01 84|00/01|squelch relation to recording RX audio for QSO recorder
1A|05 01 85|00/01|QSO record file split function setting
1A|05 01 86|00/01|PTT Automatic Recording function setting
1A|05 01 87|00 to 03|RX audio recording status for PTT Automatic Recording function
1A|05 01 88|00 to 03|QSO PLAY Skip time
1A|05 01 89|00 to 09|NB depth
1A|05 01 90|0000 to 0255|NB width
1A|05 01 91|00 to 20|VOX delay
1A|05 01 92|00 to 03|VOX voice delay
1A|05 01 93|00/01|MF band attenuator setting
1A|06|2 bytes: data mode 00/01 + filter 01~03|Send/read DATA mode setting
1A|07|1 byte 00/01|Send/read IP+ function setting
1B|00|BCD 3 bytes|Send/read repeater tone frequency
1B|01|BCD 3 bytes|Set/read TSQL tone frequency
1C|00|1 byte 00/01|Send/read transceiver status (PTT)
1C|01|1 byte 00~02|Send/read antenna tuner
1C|02|1 byte 00/01|Send/read transmit frequency monitor
1C|03|BCD 5 bytes frequency|Read transmit frequency
1C|04|1 byte 00/01|Send/read command to disable antenna controller output from REMOTE
1E|00|none|Read number of available TX frequency bands
1E|01|band edge data|Read TX band edge frequencies
1E|02|none|Read number of user-set TX frequency bands
1E|03|band edge data|Send/read user-set TX band edge frequencies
21|00|BCD RIT frequency (p.19-11)|Send/read RIT frequency
21|01|1 byte 00/01|Send/read RIT setting
21|02|1 byte 00/01|Send/read TX (delta TX) setting
25||1 byte 00/01 + BCD 5 bytes|Send/read selected or unselected VFO frequency
26||1 byte 00/01 + mode + data-mode 00/01 + filter 01~03|Send/read selected or unselected VFO operating mode and filter
27|00|scope waveform data (p.19-12)|Read scope waveform data
27|10|1 byte 00/01|Send/read scope ON/OFF status
27|11|1 byte 00/01|Send/read scope wave data output
27|12|1 byte 00|Send/read Main or Sub scope setting
27|13|1 byte 00|Send/read Single/Dual scope setting
27|14|1 byte 00/01|Send/read Scope Center mode or Fixed mode
27|15|BCD span data|Send/read span in Center mode
27|16|1 byte 01~03|Send/read edge number in Fixed mode
27|17|1 byte 00/01|Send/read scope hold
27|19|sign + level data|Send/read scope reference level
27|1A|1 byte 00~02|Send/read sweep speed
27|1B|1 byte 00/01|Send/read scope indication during TX in Center mode
27|1C|1 byte 00~02|Send/read scope center frequency setting in Center mode
27|1D|1 byte 00/01|Send/read scope VBW
27|1E|edge frequency data|Send/read scope fixed edge frequencies
";
            D["ic7300mk2"] = @"
00||FREQ BCD 5 bytes (see p.16)|Set and output the current operating frequency (transceive)
01||MODE bytes mode[,filter] (see p.16)|Set and output the current operating mode (transceive)
02||BAND EDGE (see p.16): lower 5B + 2D + upper 5B|Read the current upper and lower edge frequencies
03||FREQ BCD 5 bytes|Read the current operating frequency
04||MODE 2 bytes mode,filter|Read the current operating mode
05||FREQ BCD 5 bytes|Set the current operating frequency
06||MODE bytes mode[,filter]|Set the operating mode
07||(none)|Switch to the VFO mode
07|00||Switch to the VFO mode and select VFO A
07|01||Switch to the VFO mode and select VFO B
07|A0||Equalize VFO A and VFO B
07|B0||Exchange VFO A with VFO B
08||(none)|Switch to the Memory mode
08||BCD 2 bytes 00 01 ~ 00 99|Select the Memory channel
08||BCD 2 bytes 01 00|Select the Programmed Scan Edge channel P1
08||BCD 2 bytes 01 01|Select the Programmed Scan Edge channel P2
09||(none)|Store displayed contents into the selected Memory channel
0A||(none)|Copy the selected Memory channel contents to the VFO
0B||(none)|Clear the selected Memory channel (blank)
0E|00||Stop the current scan
0E|01||Start a Programmed or Memory scan
0E|02||Start a Programmed scan
0E|03||Start a F scan
0E|12||Start a Fine Programmed scan
0E|13||Start a Fine F scan
0E|22||Start a Memory scan
0E|23||Start a Select Memory scan
0E|A1~A7|see p.16|Set the frequency span for F scan
0E|B0||Release the Select Memory channel
0E|B1||Set the channel as a Select Memory channel
0E|B1|01 ~ 03|Set the channel as a specific Select Memory channel
0E|B2|00 ~ 03|Set whether all or one Select Memory channel is scanned
0E|D0||Turn Scan Resume OFF
0E|D3||Turn Scan Resume (Close&Delay) ON
0F||(none) request / reply 00/01|Read the ON/OFF status of the Split function
0F|00||Turn the Split function OFF
0F|01||Turn the Split function ON
10||00 ~ 08|Set or read the tuning step
11||00/20|Set or read the 20 dB attenuator
12|00|00/01|Set or read the receiving antenna (RX ANT) ON/OFF
13|00||Announce S-meter level, operating frequency and mode
13|01||Announce S-meter level and operating frequency
13|02||Announce the current operating mode
14|01|BCD 2 bytes 00 00 ~ 02 55|Set or read the AF gain
14|02|BCD 2 bytes 00 00 ~ 02 55|Set or read the RF gain
14|03|BCD 2 bytes 00 00 ~ 02 55|Set or read the squelch level
14|05|BCD 2 bytes 00 00 ~ 02 55|Set or read the peak frequency of the Audio Peak Filter
14|06|BCD 2 bytes 00 00 ~ 02 55|Set or read the Noise Reduction level
14|07|BCD 2 bytes 00 00 ~ 02 55|Set or read Digital Twin PBT shift (inner: PBT1)
14|08|BCD 2 bytes 00 00 ~ 02 55|Set or read Digital Twin PBT shift (outer: PBT2)
14|09|BCD 2 bytes 00 00 ~ 02 55|Set or read the CW pitch frequency
14|0A|BCD 2 bytes 00 00 ~ 02 55|Set or read the RF power
14|0B|BCD 2 bytes 00 00 ~ 02 55|Set or read the microphone gain
14|0C|BCD 2 bytes 00 00 ~ 02 55|Set or read the keying speed
14|0D|BCD 2 bytes 00 00 ~ 02 55|Set or read the Notch filter setting
14|0E|BCD 2 bytes 00 00 ~ 02 55|Set or read the Speech Compressor level
14|0F|BCD 2 bytes 00 00 ~ 02 55|Set or read the Break-in delay
14|12|BCD 2 bytes 00 00 ~ 02 55|Set or read the Noise Blanker level
14|15|BCD 2 bytes 00 00 ~ 02 55|Set or read the gain of the monitored audio (Monitor level)
14|16|BCD 2 bytes 00 00 ~ 02 55|Set or read the VOX sensitivity (VOX gain)
14|17|BCD 2 bytes 00 00 ~ 02 55|Set or read the ANTI VOX sensitivity
14|19|BCD 2 bytes 00 00 ~ 02 55|Set or read the LCD backlight brightness
15|01|00/01|Read Open/Closed status of Noise or S-meter squelch
15|02|BCD 2 bytes 00 00 ~ 02 55|Read the S-meter level
15|05|00/01|Read Open/Closed status of the squelch (various squelch functions)
15|07|00/01|Read the status of the OVF indicator
15|11|BCD 2 bytes 00 00 ~ 02 55|Read the Po meter level
15|12|BCD 2 bytes 00 00 ~ 02 55|Read the SWR meter level
15|13|BCD 2 bytes 00 00 ~ 02 55|Read the ALC meter level
15|14|BCD 2 bytes 00 00 ~ 02 55|Read the COMP meter level
15|15|BCD 2 bytes 00 00 ~ 02 55|Read the Vd meter level
15|16|BCD 2 bytes 00 00 ~ 02 55|Read the Id meter level
16|02|00 ~ 02|Set or read the preamp
16|12|01 ~ 03|Set or read the AGC time constant
16|22|00/01|Set or read the Noise Blanker ON/OFF
16|32|00,01,02,03|Audio peak filter (APF)
16|40|00/01|Set or read the Noise Reduction ON/OFF
16|41|00/01|Set or read the Auto Notch ON/OFF
16|42|00/01|Set or read the Repeater tone ON/OFF
16|43|00/01|Set or read the Tone squelch ON/OFF
16|44|00/01|Set or read the Speech Compressor ON/OFF
16|45|00/01|Set or read the Monitor ON/OFF
16|46|00/01|Set or read the VOX function ON/OFF
16|47|00 ~ 02|Set or read the Break-in function in CW mode
16|48|00/01|Set or read the Manual Notch ON/OFF
16|4F|00/01|Set or read the Twin Peak Filter ON/OFF
16|50|00/01|Set or read the Lock function ON/OFF
16|56|00/01|Set or read the IF filter shape
16|57|00 ~ 02|Set or read the filter width of the Manual Notch
16|58|00 ~ 02|Set or read the SSB transmit filter bandwidth
16|65|00/01|Set or read the IP Plus function
16|66|00/01|Set or read the TX Inhibit function
17||ASCII text up to 30 chars (see p.16)|Send CW messages
18|00||Turn the transceiver OFF
18|01||Turn the transceiver ON
19|00||Read the transceiver ID
1A|00|see p.17|Set or read a specific Memory channel content
1A|01|see p.19|Set or read a specific band stacking register content
1A|02|see p.19|Set or read a specific Keyer Memory content
1A|03|see p.19|Set or read the IF filter width
1A|04|see p.20|Set or read the AGC time constant
1A|05 00 01|See p. 20.|[SET > Tone Control/TBW > RX / SSB] Sets or reads the RX HPF/LPF settings.
1A|05 00 02|BCD 1 byte 00 ~ 10|[SET > Tone Control/TBW > RX] Sets or reads the RX Tone (Bass) level.
1A|05 00 03|BCD 1 byte 00 ~ 10|[SET > Tone Control/TBW > RX] Sets or reads the RX Tone (Treble) level.
1A|05 00 04|See p. 20.|[SET > Tone Control/TBW > RX / AM] Sets or reads the RX HPF/LPF settings.
1A|05 00 05|BCD 1 byte 00 ~ 10|[SET > Tone Control/TBW > RX] Sets or reads the RX Tone (Bass) level.
1A|05 00 06|BCD 1 byte 00 ~ 10|[SET > Tone Control/TBW > RX] Sets or reads the RX Tone (Treble) level.
1A|05 00 07|See p. 20.|[SET > Tone Control/TBW > RX / FM] Sets or reads the RX HPF/LPF settings.
1A|05 00 08|BCD 1 byte 00 ~ 10|[SET > Tone Control/TBW > RX] Sets or reads the RX Tone (Bass) level.
1A|05 00 09|BCD 1 byte 00 ~ 10|[SET > Tone Control/TBW > RX] Sets or reads the RX Tone (Treble) level.
1A|05 00 10|See p. 20.|[SET > Tone Control/TBW > RX / CW] Sets or reads the RX HPF/LPF settings.
1A|05 00 11|See p. 20.|[SET > Tone Control/TBW > RX / RTTY] Sets or reads the RX HPF/LPF settings.
1A|05 00 12|BCD 1 byte 00 ~ 10|[SET > Tone Control/TBW > TX / SSB] Sets or reads the TX Tone (Bass) level.
1A|05 00 13|BCD 1 byte 00 ~ 10|[SET > Tone Control/TBW > TX] Sets or reads the TX Tone (Treble) level.
1A|05 00 14|See p. 20.|[SET > Tone Control/TBW > TX] Sets or reads the TBW (WIDE) setting.
1A|05 00 15|See p. 20.|[SET > Tone Control/TBW > TX] Sets or reads the TBW (MID) setting.
1A|05 00 16|See p. 20.|[SET > Tone Control/TBW > TX] Sets or reads the TBW (NAR) setting.
1A|05 00 17|See p. 20.|[SET > Tone Control/TBW > TX / SSB-D] Sets or reads the TBW setting.
1A|05 00 18|BCD 1 byte 00 ~ 10|[SET > Tone Control/TBW > TX / AM] Sets or reads the TX Tone (Bass) level.
1A|05 00 19|BCD 1 byte 00 ~ 10|[SET > Tone Control/TBW > TX] Sets or reads the TX Tone (Treble) level.
1A|05 00 20|BCD 1 byte 00 ~ 10|[SET > Tone Control/TBW > TX / FM] Sets or reads the TX Tone (Bass) level.
1A|05 00 21|BCD 1 byte 00 ~ 10|[SET > Tone Control/TBW > TX] Sets or reads the TX Tone (Treble) level.
1A|05 00 22|BCD 2 bytes 00 00 ~ 02 55|[SET > Function] Sets or reads the Beep Level.
1A|05 00 23|BCD 1 byte 00/01|[SET > Function] Sets or reads the Beep Level Limit setting.
1A|05 00 24|BCD 1 byte 00/01|[SET > Function] Sets or reads the Beep
1A|05 00 25|BCD 1 byte 00 ~ 03|[SET > Function] Sets or reads the Band Edge Beep setting.
1A|05 00 26|BCD 1 byte 00 ~ 02|[SET > Function] Sets or reads the RF/SQL Control setting.
1A|05 00 27|BCD 1 byte 00/01|[SET > Function] Sets or reads the Cancel CI-V Remote Set Levels setting.
1A|05 00 28|BCD 1 byte 00/01|[SET > Function] Sets or reads the MF Band ATT setting.
1A|05 00 29|BCD 1 byte 00 ~ 05|[SET > Function > TX Delay / HF] Sets or reads the TX Delay setting.
1A|05 00 30|BCD 1 byte 00 ~ 05|[SET > Function > TX Delay / 50M] Sets or reads the TX Delay setting.
1A|05 00 31|BCD 1 byte 00 ~ 05|[SET > Function > TX Delay / 70M] Sets or reads the TX Delay setting.
1A|05 00 32|BCD 1 byte 00 ~ 05|[SET > Function] Sets or reads the Time-Out Timer
1A|05 00 33|BCD 1 byte 00/01|[SET > Function > SPLIT] Sets or reads the Quick SPLIT setting.
1A|05 00 34|See p. 20.|[SET > Function > SPLIT] Sets or reads the FM SPLIT Offset
1A|05 00 35|See p. 20.|[SET > Function > SPLIT] Sets or reads the FM SPLIT Offset
1A|05 00 36|BCD 1 byte 00/01|[SET > Function > SPLIT] Sets or reads the SPLIT LOCK setting.
1A|05 00 37|BCD 1 byte 00/01|[SET > Function > Tuner] Sets or reads the [TUNER] Switch setting.
1A|05 00 38|BCD 1 byte 00/01|[SET > Function > Tuner] Sets or reads the PTT Start setting.
1A|05 00 39|BCD 1 byte 00 ~ 02|[SET > Function] Sets or reads the RTTY Mark Frequency setting.
1A|05 00 40|BCD 1 byte 00 ~ 02|[SET > Function] Sets or reads the RTTY Shift Width setting.
1A|05 00 41|BCD 1 byte 00/01|[SET > Function] Sets or reads the RTTY Keying Polarity setting.
1A|05 00 42|BCD 1 byte 00/01|[SET > Function > SPEECH] Sets or reads the SPEECH Language setting.
1A|05 00 43|BCD 1 byte 00/01|[SET > Function > SPEECH] Sets or reads the SPEECH Speed setting.
1A|05 00 44|BCD 1 byte 00/01|[SET > Function > SPEECH] Sets or reads the S-Level SPEECH setting.
1A|05 00 45|BCD 1 byte 00/01|[SET > Function > SPEECH] Sets or reads the MODE SPEECH setting.
1A|05 00 46|BCD 2 bytes 00 00 ~ 02 55|[SET > Function > SPEECH] Sets or reads the SPEECH Level setting.
1A|05 00 47|BCD 1 byte 00/01|[SET > Function] Sets or reads the [SPEECH/LOCK] Switch setting.
1A|05 00 48|BCD 1 byte 00/01|[SET > Function] Sets or reads the Lock Function setting.
1A|05 00 49|BCD 1 byte 00/01|[SET > Function] Sets or reads the Memo Pad Quantity setting.
1A|05 00 50|BCD 1 byte 00 ~ 02|[SET > Function] Sets or reads the MAIN DIAL Auto TS setting.
1A|05 00 51|BCD 1 byte 00/01|[SET > Function] Sets or reads the MIC Up/Down Speed setting.
1A|05 00 52|BCD 1 byte 00/01|[SET > Function] Sets or reads the Quick RIT/TX Clear setting.
1A|05 00 53|BCD 1 byte 00 ~ 02|[SET > Function] Sets or reads the [NOTCH] Switch
1A|05 00 54|BCD 1 byte 00 ~ 02|[SET > Function] Sets or reads the [NOTCH] Switch
1A|05 00 55|BCD 1 byte 00/01|[SET > Function] Sets or reads the SSB/CW Synchronous Tuning setting.
1A|05 00 56|BCD 1 byte 00/01|[SET > Function] Sets or reads the CW Normal Side setting.
1A|05 00 57|See p. 21.|[SET > Function > Front Key Customize] Sets or reads the [VOX/BK-IN] setting.
1A|05 00 58|See p. 23.|[SET > Function > Front Key Customize] Sets or reads the [AUTOTUNE] setting.
1A|05 00 59|See p. 21.|[SET > Function > Front Key Customize] Sets or reads the [] setting.
1A|05 00 60|See p. 21.|[SET > Function > Front Key Customize] Sets or reads the [] setting.
1A|05 00 61|See p. 21.|[SET > Function > MIC Key Customize] Sets or reads the [UP] setting.
1A|05 00 62|See p. 21.|[SET > Function > MIC Key Customize] Sets or reads the [DN] setting.
1A|05 00 63|BCD 1 byte 00/01|[SET > Function] Sets or reads the Screen Capture [POWER] SW setting.
1A|05 00 64|BCD 1 byte 00/01|[SET > Function] Sets or reads the Screen Capture File Type setting.
1A|05 00 65|BCD 1 byte 00/01|[SET > Function] Sets or reads the Keyboard Type setting.
1A|05 00 66|BCD 1 byte 00 ~ 02|[SET > Function] Sets or reads the Full Keyboard Layout setting.
1A|05 00 67|BCD 1 byte 00/01|[SET > Function] Sets or reads the Calibration Marker setting.
1A|05 00 68|BCD 2 bytes 00 00 ~ 02 55|[SET > Function] Sets or reads the REF Adjust setting.
1A|05 00 69|BCD 1 byte 00/01|[SET > Connectors > USB AF/IF Output] Sets or reads the Output Select setting.
1A|05 00 70|BCD 2 bytes 00 00 ~ 02 55|[SET > Connectors > USB AF/IF Output] Sets or reads the AF Output Level setting.
1A|05 00 71|BCD 1 byte 00/01|[SET > Connectors > USB AF/IF Output] Sets or reads the AF SQL setting.
1A|05 00 72|BCD 1 byte 00/01|[SET > Connectors > USB AF/IF Output] Sets or reads the AF Beep/Speech... Output setting.
1A|05 00 73|BCD 2 bytes 00 00 ~ 02 55|[SET > Connectors > USB AF/IF Output] Sets or reads the IF Output Level setting.
1A|05 00 74|BCD 1 byte 00/01|[SET > Connectors > ACC AF/IF Output] Sets or reads the Output Select setting.
1A|05 00 75|BCD 2 bytes 00 00 ~ 02 55|[SET > Connectors > ACC AF/IF Output] Sets or reads the AF Output Level setting.
1A|05 00 76|BCD 1 byte 00/01|[SET > Connectors > ACC AF/IF Output] Sets or reads the AF SQL setting.
1A|05 00 77|BCD 1 byte 00/01|[SET > Connectors > ACC AF/IF Output] Sets or reads the AF Beep/Speech... Output setting.
1A|05 00 78|BCD 2 bytes 00 00 ~ 02 55|[SET > Connectors > ACC AF/IF Output] Sets or reads the IF Output Level setting.
1A|05 00 79|BCD 1 byte 00/01|[SET > Connectors > LAN AF/IF Output] Sets or reads the Output Select setting.
1A|05 00 80|BCD 1 byte 00/01|[SET > Connectors > LAN AF/IF Output] Sets or reads the AF SQL setting.
1A|05 00 81|BCD 2 bytes 00 00 ~ 02 55|[SET > Connectors > MOD Input] Sets or reads the USB MOD Level setting.
1A|05 00 82|BCD 2 bytes 00 00 ~ 02 55|[SET > Connectors > MOD Input] Sets or reads the ACC MOD Level setting.
1A|05 00 83|BCD 2 bytes 00 00 ~ 02 55|[SET > Connectors > MOD Input] Sets or reads the LAN MOD Level setting.
1A|05 00 84|BCD 1 byte 00 ~ 05|[SET > Connectors > MOD Input] Sets or reads the DATA OFF MOD setting.
1A|05 00 85|BCD 1 byte 00 ~ 05|[SET > Connectors > MOD Input] Sets or reads the DATA MOD setting.
1A|05 00 86|BCD 1 byte 00/01|[SET > Connectors > External Keypad] Sets or reads the VOICE setting.
1A|05 00 87|BCD 1 byte 00/01|[SET > Connectors > External Keypad] Sets or reads the KEYER setting.
1A|05 00 88|BCD 1 byte 00/01|[SET > Connectors > External Keypad] Sets or reads the RTTY setting.
1A|05 00 89|BCD 1 byte 00/01|[SET > Connectors > CI-V] Sets or reads the CI-V Transceive setting.
1A|05 00 90|BCD 2 bytes 00 00 ~ 02 23|[SET > Connectors > CI-V] Sets or reads the USB/LANREMOTE Transceive Address setting.
1A|05 00 91|BCD 1 byte 00/01|[SET > Connectors > CI-V] Sets or reads the CI-V Output (for ANT) setting.
1A|05 00 92|BCD 1 byte 00/01|[SET > Connectors > CI-V] Sets or reads the CI-V USB (A) Echo Back setting.
1A|05 00 93|BCD 1 byte 00/01|[SET > Connectors > CI-V] Sets or reads the CI-V USB (B) Echo Back setting.
1A|05 00 94|BCD 1 byte 00/01|[SET > Connectors] Sets or reads the USB (B) Function setting.
1A|05 00 95|BCD 1 byte 00/01|[SET > Connectors] Sets or reads the SEND Relay Output setting.
1A|05 00 96|BCD 1 byte 00 ~ 04|[SET > Connectors > USB SEND/Keying] Sets or reads the the USB SEND setting.
1A|05 00 97|BCD 1 byte 00 ~ 04|[SET > Connectors > USB SEND/Keying] Sets or reads the USB Keying (CW) setting.
1A|05 00 98|BCD 1 byte 00 ~ 04|[SET > Connectors > USB SEND/Keying] Sets or reads the USB Keying
1A|05 00 99|BCD 1 byte 00/01|[SET > Connector] Sets or reads the PTT Port Function setting.
1A|05 01 00|BCD 1 byte 00/01|[SET > Network] Sets or reads the DHCP (valid after restart) setting.
1A|05 01 01|See p. 21.|[SET > Network] Sets or reads the IP Address (valid after restart) setting. L Valid when DHCP is set to OFF
1A|05 01 02|See p. 21.|[SET > Network] Reads the IP address obtained by DHCP (valid after restart). L When DHCP is set to OFF
1A|05 01 03|BCD 1 byte 01 ~ 30|[SET > Network] Sets or reads the Subnet Mask (valid after restart) setting.
1A|05 01 04|See p. 21.|[SET > Network] Sets or reads the Default Gateway
1A|05 01 05|See p. 21.|[SET > Network] Sets or reads the Primary DNS Server (valid after restart) setting. L Valid when DHCP is set to OFF
1A|05 01 06|See p. 21.|[SET > Network] Sets or reads the Secondary DNS Server (valid after restart) setting. L Valid when DHCP is set to OFF
1A|05 01 07|See p. 18.|[SET > Network] Sets or reads the Network Name setting.
1A|05 01 08|BCD 1 byte 00/01|[SET > Network > Remote Settings] Sets or reads the Network Control
1A|05 01 09|BCD 1 byte 00/01|[SET > Network > Remote Settings] Sets or reads the Power OFF Setting
1A|05 01 10|BCD 3 bytes 00 00 01 ~ 06 55 35|[SET > Network > Remote Settings] Control Port (UDP) (valid after restart)
1A|05 01 11|BCD 3 bytes 00 00 01 ~ 06 55 35|[SET > Network > Remote Settings] Serial Port (UDP) (valid after restart)
1A|05 01 12|BCD 3 bytes 00 00 01 ~ 06 55 35|[SET > Network > Remote Settings] Audio Port (UDP) (valid after restart)
1A|05 01 13|BCD 1 byte 00/01|[SET > Network > Remote Settings] Sets or reads the Internet Access Line (valid after restart) setting.
1A|05 01 14|See p. 18.|[SET > Network > Remote Settings] Sets or reads the the Network Radio Name setting.
1A|05 01 15|BCD 2 bytes 00 00 ~ 02 55|[SET > Display] Sets or reads the LCD Backlight setting.
1A|05 01 16|BCD 1 byte 00/01|[SET > Display] Sets or reads the Display Type setting.
1A|05 01 17|BCD 1 byte 00/01|[SET > Display] Sets or reads the Display Font setting.
1A|05 01 18|BCD 1 byte 00/01|[SET > Display] Sets or reads the Meter Peak Hold setting.
1A|05 01 19|BCD 1 byte 00/01|[SET > Display] Sets or reads the Memory Name setting.
1A|05 01 20|BCD 1 byte 00/01|[SET > Display] Sets or reads the MN-Q Popup (MN OFFON) setting.
1A|05 01 21|BCD 1 byte 00/01|[SET > Display] Sets or reads the BW Popup (PBT) setting.
1A|05 01 22|BCD 1 byte 00/01|[SET > Display] Sets or reads the BW Popup (FIL) setting.
1A|05 01 23|BCD 1 byte 00 ~ 03|[SET > Display] Sets or reads the Screen Saver setting.
1A|05 01 24|BCD 1 byte 00/01|[SET > Display > External Display] Sets or reads the External Display setting.
1A|05 01 25|BCD 1 byte 00 ~ 02|[SET > Display > External Display] Sets or reads the Resolution setting.
1A|05 01 26|BCD 1 byte 00/01|[SET > Display > External Display] Sets or reads the Audio Output setting.
1A|05 01 27|BCD 1 byte 00/01|[SET > Display] Sets or reads the Opening Message setting.
1A|05 01 28|See p. 18.|[SET > Display] Sets or reads the My Call setting.
1A|05 01 29|BCD 1 byte 00/01|[SET > Display] Sets or reads the Power ON Check setting.
1A|05 01 30|BCD 1 byte 00/01|[SET > Display] Sets or reads the Display Language setting.
1A|05 01 31|BCD 1 byte 00/01|[SET > Display] Sets or reads the System Language setting.
1A|05 01 32|BCD 4 bytes 20 20 01 01 ~ 20 99 12 31|[SET > Time Set > Date/Time] Date setting
1A|05 01 33|BCD 2 bytes 00 00 ~ 23 59|[SET > Time Set > Date/Time] Sets or reads the Time setting.
1A|05 01 34|BCD 1 byte 00/01|[SET > Time Set > Date/Time] Sets or reads the NTP Function setting.
1A|05 01 35|See p. 18.|[SET > Time Set > Date/Time] Sets or reads the NTP Server Address setting.
1A|05 01 36|See p. 22.|[SET > Time Set] Sets or reads the UTC Offset setting.
1A|05 01 37|BCD 1 byte 00/01|[SCOPE SET: SCOPE > EXPD/SET (Touch for 1 second)] Sets or reads the Scope during Tx
1A|05 01 38|BCD 1 byte 00 ~ 02|[SCOPE SET: SCOPE > EXPD/SET (Touch for 1 second)] Sets or reads the the Max Hold setting.
1A|05 01 39|BCD 1 byte 00 ~ 02|[SCOPE SET: SCOPE > EXPD/SET (Touch for 1 second)] Sets or reads the CENTER Type Display setting.
1A|05 01 40|BCD 1 byte 00/01|[SCOPE SET: SCOPE > EXPD/SET (Touch for 1 second)] Sets or reads the Marker Position
1A|05 01 41|BCD 1 byte 00/01|[SCOPE SET: SCOPE > EXPD/SET (Touch for 1 second)] Sets or reads the VBW setting.
1A|05 01 42|BCD 1 byte 00 ~ 03|[SCOPE SET: SCOPE > EXPD/SET (Touch for 1 second)] Sets or reads the Averaging setting.
1A|05 01 43|BCD 1 byte 00/01|[SCOPE SET: SCOPE > EXPD/SET (Touch for 1 second)] Sets or reads the Waveform Type setting.
1A|05 01 44|See p. 22.|[SCOPE SET: SCOPE > EXPD/SET (Touch for 1 second)] Sets or reads the Waveform Color
1A|05 01 45|See p. 22.|[SCOPE SET: SCOPE > EXPD/SET (Touch for 1 second)] Sets or reads the Waveform Color
1A|05 01 46|See p. 22.|[SCOPE SET: SCOPE > EXPD/SET (Touch for 1 second)] Sets or reads the Waveform Color
1A|05 01 47|BCD 1 byte 00/01|[SCOPE SET: SCOPE > EXPD/SET (Touch for 1 second)] Sets or reads the Waterfall Display setting.
1A|05 01 48|BCD 1 byte 00 ~ 02|[SCOPE SET: SCOPE > EXPD/SET (Touch for 1 second)] Sets or reads the Waterfall Speed setting.
1A|05 01 49|BCD 1 byte 00 ~ 02|[SCOPE SET: SCOPE > EXPD/SET (Touch for 1 second)] Sets or reads the Waterfall Size
1A|05 01 50|BCD 1 byte 00 ~ 07|[SCOPE SET: SCOPE > EXPD/SET (Touch for 1 second)] Sets or reads the Waterfall Peak Color Level setting.
1A|05 01 51|BCD 1 byte 00/01|[SCOPE SET: SCOPE > EXPD/SET (Touch for 1 second)] Sets or reads the Waterfall Marker Auto-hide setting.
1A|05 01 52|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 0.03 ~ 1.60 MHz] Scope Fixed Edge No.1 setting
1A|05 01 53|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 0.03 ~ 1.60 MHz] Scope Fixed Edge No.2 setting
1A|05 01 54|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 0.03 ~ 1.60 MHz] Scope Fixed Edge No.3 setting
1A|05 01 55|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 0.03 ~ 1.60 MHz] Scope Fixed Edge No.4 setting
1A|05 01 56|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 1.60 ~ 2.00 MHz] Scope Fixed Edge No.1 setting
1A|05 01 57|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 1.60 ~ 2.00 MHz] Scope Fixed Edge No.2 setting
1A|05 01 58|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 1.60 ~ 2.00 MHz] Scope Fixed Edge No.3 setting
1A|05 01 59|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 1.60 ~ 2.00 MHz] Scope Fixed Edge No.4 setting
1A|05 01 60|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 2.00 ~ 6.00 MHz] Scope Fixed Edge No.1 setting
1A|05 01 61|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 2.00 ~ 6.00 MHz] Scope Fixed Edge No.2 setting
1A|05 01 62|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 2.00 ~ 6.00 MHz] Scope Fixed Edge No.3 setting
1A|05 01 63|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 2.00 ~ 6.00 MHz] Scope Fixed Edge No.4 setting
1A|05 01 64|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 6.00 ~ 8.00 MHz] Scope Fixed Edge No.1 setting
1A|05 01 65|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 6.00 ~ 8.00 MHz] Scope Fixed Edge No.2 setting
1A|05 01 66|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 6.00 ~ 8.00 MHz] Scope Fixed Edge No.3 setting
1A|05 01 67|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 6.00 ~ 8.00 MHz] Scope Fixed Edge No.4 setting
1A|05 01 68|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 8.00 ~ 11.00 MHz] Scope Fixed Edge No.1 setting
1A|05 01 69|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 8.00 ~ 11.00 MHz] Scope Fixed Edge No.2 setting
1A|05 01 70|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 8.00 ~ 11.00 MHz] Scope Fixed Edge No.3 setting
1A|05 01 71|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 8.00 ~ 11.00 MHz] Scope Fixed Edge No.4 setting
1A|05 01 72|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 11.00 ~ 15.00 MHz] Scope Fixed Edge No.1 setting
1A|05 01 73|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 11.00 ~ 15.00 MHz] Scope Fixed Edge No.2 setting
1A|05 01 74|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 11.00 ~ 15.00 MHz] Scope Fixed Edge No.3 setting
1A|05 01 75|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 11.00 ~ 15.00 MHz] Scope Fixed Edge No.4 setting
1A|05 01 76|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 15.00 ~ 20.00 MHz] Scope Fixed Edge No.1 setting
1A|05 01 77|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 15.00 ~ 20.00 MHz] Scope Fixed Edge No.2 setting
1A|05 01 78|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 15.00 ~ 20.00 MHz] Scope Fixed Edge No.3 setting
1A|05 01 79|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 15.00 ~ 20.00 MHz] Scope Fixed Edge No.4 setting
1A|05 01 80|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 20.00 ~ 22.00 MHz] Scope Fixed Edge No.1 setting
1A|05 01 81|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 20.00 ~ 22.00 MHz] Scope Fixed Edge No.2 setting
1A|05 01 82|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 20.00 ~ 22.00 MHz] Scope Fixed Edge No.3 setting
1A|05 01 83|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 20.00 ~ 22.00 MHz] Scope Fixed Edge No.4 setting
1A|05 01 84|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 22.00 ~ 26.00 MHz] Scope Fixed Edge No.1 setting
1A|05 01 85|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 22.00 ~ 26.00 MHz] Scope Fixed Edge No.2 setting
1A|05 01 86|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 22.00 ~ 26.00 MHz] Scope Fixed Edge No.3 setting
1A|05 01 87|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 22.00 ~ 26.00 MHz] Scope Fixed Edge No.4 setting
1A|05 01 88|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 26.00 ~ 30.00 MHz] Scope Fixed Edge No.1 setting
1A|05 01 89|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 26.00 ~ 30.00 MHz] Scope Fixed Edge No.2 setting
1A|05 01 90|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 26.00 ~ 30.00 MHz] Scope Fixed Edge No.3 setting
1A|05 01 91|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 26.00 ~ 30.00 MHz] Scope Fixed Edge No.4 setting
1A|05 01 92|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 30.00 ~ 45.00 MHz] Scope Fixed Edge No.1 setting
1A|05 01 93|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 30.00 ~ 45.00 MHz] Scope Fixed Edge No.2 setting
1A|05 01 94|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 30.00 ~ 45.00 MHz] Scope Fixed Edge No.3 setting
1A|05 01 95|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 30.00 ~ 45.00 MHz] Scope Fixed Edge No.4 setting
1A|05 01 96|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 45.00 ~ 60.00 MHz] Scope Fixed Edge No.1 setting
1A|05 01 97|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 45.00 ~ 60.00 MHz] Scope Fixed Edge No.2 setting
1A|05 01 98|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 45.00 ~ 60.00 MHz] Scope Fixed Edge No.3 setting
1A|05 01 99|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 45.00 ~ 60.00 MHz] Scope Fixed Edge No.4 setting
1A|05 02 00|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 60.00 ~ 74.80 MHz] Scope Fixed Edge No.1 setting
1A|05 02 01|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 60.00 ~ 74.80 MHz] Scope Fixed Edge No.2 setting
1A|05 02 02|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 60.00 ~ 74.80 MHz] Scope Fixed Edge No.3 setting
1A|05 02 03|see p.22 (Fixed edge freq: lower+higher edge, 5 bytes each)|[SCOPE > EXPD/SET > Fixed Edges 60.00 ~ 74.80 MHz] Scope Fixed Edge No.4 setting
1A|05 02 04|BCD 1 byte 00/01|[AUDIO SCOPE SET: AUDIO > EXPD/SET (Touch for 1 second)] Sets or reads the FFT Scope Waveform Type setting.
1A|05 02 05|See p. 22.|[AUDIO SCOPE SET: AUDIO > EXPD/SET (Touch for 1 second)] Sets or reads the FFT Scope Waveform Color setting.
1A|05 02 06|BCD 1 byte 00/01|[AUDIO SCOPE SET: AUDIO > EXPD/SET (Touch for 1 second)] Sets or reads the FFT Scope Waterfall Display setting.
1A|05 02 07|See p. 22.|[AUDIO SCOPE SET: AUDIO > EXPD/SET (Touch for 1 second)] Sets or reads the Oscilloscope Waveform Color setting.
1A|05 02 08|BCD 1 byte 00 ~ 04|[KEYER 001:  KEYER/DECODE > KEYER > EDIT/SET > 001 SET] Sets or reads the Number Style setting.
1A|05 02 09|BCD 1 byte 00/01|[KEYER 001:  KEYER/DECODE > KEYER > EDIT/SET > 001 SET] Sets or reads the Count Up Trigger
1A|05 02 10|BCD 1 byte 00/01|[KEYER 001:  KEYER/DECODE > KEYER > EDIT/SET > 001 SET] Sets or reads the Count Up Trigger
1A|05 02 11|BCD 1 byte 00/01|[KEYER 001:  KEYER/DECODE > KEYER > EDIT/SET > 001 SET] Sets or reads the Count Up Trigger
1A|05 02 12|BCD 1 byte 00/01|[KEYER 001:  KEYER/DECODE > KEYER > EDIT/SET > 001 SET] Sets or reads the Count Up Trigger
1A|05 02 13|BCD 1 byte 00/01|[KEYER 001:  KEYER/DECODE > KEYER > EDIT/SET > 001 SET] Sets or reads the Count Up Trigger
1A|05 02 14|BCD 1 byte 00/01|[KEYER 001:  KEYER/DECODE > KEYER > EDIT/SET > 001 SET] Sets or reads the Count Up Trigger
1A|05 02 15|BCD 1 byte 00/01|[KEYER 001:  KEYER/DECODE > KEYER > EDIT/SET > 001 SET] Sets or reads the Count Up Trigger
1A|05 02 16|BCD 1 byte 00/01|[KEYER 001:  KEYER/DECODE > KEYER > EDIT/SET > 001 SET] Sets or reads the Count Up Trigger
1A|05 02 17|BCD 2 bytes 00 01 ~ 99 99|[KEYER 001:  KEYER/DECODE > KEYER > EDIT/SET > 001 SET] Sets or reads the Present Number setting.
1A|05 02 18|BCD 2 bytes 00 00 ~ 02 25|[SET > CW-KEY Set] Sets or reads the Side Tone Level setting.
1A|05 02 19|BCD 1 byte 00/01|[SET > CW-KEY Set] Sets or reads the Side Tone Level Limit setting.
1A|05 02 20|BCD 1 byte 01 ~ 60|[SET > CW-KEY Set] Sets or reads the Keyer Repeat time setting.
1A|05 02 21|BCD 1 byte 28 ~ 45|[SET > CW-KEY Set] Sets or reads the Dot/Dash Ratio setting.
1A|05 02 22|BCD 1 byte 00 ~ 03|[SET > CW-KEY Set] Sets or reads the Rise Time setting.
1A|05 02 23|BCD 1 byte 00/01|[SET > CW-KEY Set] Sets or reads the Paddle Polarity setting.
1A|05 02 24|BCD 1 byte 00 ~ 02|[SET > CW-KEY Set] Sets or reads the Key Type setting.
1A|05 02 25|BCD 1 byte 00/01|[SET > CW-KEY Set] Sets or reads the MIC Up/Down Keyer setting.
1A|05 02 26|BCD 1 byte 00/01|[KEYER DECODE SET:  KEYER/DECODE > KEYER > EDIT/SET] Sets or reads the Decode Display setting.
1A|05 02 27|BCD 1 byte 00/01|[KEYER DECODE SET:  KEYER/DECODE > KEYER > EDIT/SET] Sets or reads the Japanese Morse Decode setting.
1A|05 02 28|BCD 1 byte 00 ~ 03|[RTTY DECODE > EXPD/SET (Touch for 1 second)] Sets or reads the FFT Scope Averaging setting.
1A|05 02 29|See p. 22.|[RTTY DECODE > EXPD/SET (Touch for 1 second)] Sets or reads the FFT Scope Waveform Color setting.
1A|05 02 30|BCD 1 byte 00/01|[RTTY DECODE > EXPD/SET (Touch for 1 second)] Sets or reads the Decode USOS setting.
1A|05 02 31|BCD 1 byte 00/01|[RTTY DECODE > EXPD/SET (Touch for 1 second)] Sets or reads the Decode New Line Code setting.
1A|05 02 32|BCD 1 byte 00/01|[RTTY DECODE > EXPD/SET (Touch for 1 second)] Sets or reads the TX USOS setting.
1A|05 02 33|See p. 22.|[RTTY DECODE > EXPD/SET (Touch for 1 second)] Sets or reads the Font Color
1A|05 02 34|See p. 22.|[RTTY DECODE > EXPD/SET (Touch for 1 second)] Sets or reads the Font Color
1A|05 02 35|BCD 1 byte 00/01|[RTTY DECODE > TX MEM > EDIT/SET > 001 SET] Sets or reads the Count Up Trigger
1A|05 02 36|BCD 1 byte 00/01|[RTTY DECODE > TX MEM > EDIT/SET > 001 SET] Sets or reads the Count Up Trigger
1A|05 02 37|BCD 1 byte 00/01|[RTTY DECODE > TX MEM > EDIT/SET > 001 SET] Sets or reads the Count Up Trigger
1A|05 02 38|BCD 1 byte 00/01|[RTTY DECODE > TX MEM > EDIT/SET > 001 SET] Sets or reads the Count Up Trigger
1A|05 02 39|BCD 1 byte 00/01|[RTTY DECODE > TX MEM > EDIT/SET > 001 SET] Sets or reads the Count Up Trigger
1A|05 02 40|BCD 1 byte 00/01|[RTTY DECODE > TX MEM > EDIT/SET > 001 SET] Sets or reads the Count Up Trigger
1A|05 02 41|BCD 1 byte 00/01|[RTTY DECODE > TX MEM > EDIT/SET > 001 SET] Sets or reads the Count Up Trigger
1A|05 02 42|BCD 1 byte 00/01|[RTTY DECODE > TX MEM > EDIT/SET > 001 SET] Sets or reads the Count Up Trigger
1A|05 02 43|BCD 2 bytes 00 01 ~ 99 99|[RTTY DECODE > TX MEM > EDIT/SET > 001 SET] Sets or reads the Present Number setting.
1A|05 02 44|BCD 1 byte 00/01|[RTTY DECODE > < 1 > > LOG] Sets or reads the Decode Log setting.
1A|05 02 45|BCD 1 byte 00/01|[RTTY DECODE > < 1 > > LOG > Log Set] Sets or reads the File Type setting.
1A|05 02 46|BCD 1 byte 00/01|[RTTY DECODE > < 1 > > LOG > Log Set] Sets or reads the Time Stamp setting.
1A|05 02 47|BCD 1 byte 00/01|[RTTY DECODE > < 1 > > LOG > Log Set] Sets or reads the Time Stamp (Time) setting.
1A|05 02 48|BCD 1 byte 00/01|[RTTY DECODE > < 1 > > LOG > Log Set] Sets or reads the Time Stamp
1A|05 02 49|See p. 22.|[CW DECODE SET:  KEYER/DECODE > CW DECODE > SET] Sets or reads the FFT Scope Waveform Color setting.
1A|05 02 50|See p. 22.|[CW DECODE SET:  KEYER/DECODE > CW DECODE > SET] Sets or reads the Signal Level Waveform Color setting.
1A|05 02 51|See p. 22.|[CW DECODE SET:  KEYER/DECODE > CW DECODE > SET] Sets or reads the Font Color
1A|05 02 52|See p. 22.|[CW DECODE SET:  KEYER/DECODE > CW DECODE > SET] Sets or reads the Font Color
1A|05 02 53|BCD 1 byte 00/01|[SCAN SET: SCAN > SET] Sets or read the SCAN Speed setting.
1A|05 02 54|BCD 1 byte 00/01|[SCAN SET: SCAN > SET] Sets or reads the SCAN Resume setting.
1A|05 02 55|BCD 2 bytes 00 00 ~ 02 25|[VOICE TX: VOICE > TX LEVEL] Sets or reads the TX LEVEL setting.
1A|05 02 56|BCD 1 byte 00/01|[VOICE TX SET: VOICE > REC/SET > SET] Sets or reads the Auto Monitor setting.
1A|05 02 57|BCD 1 byte 01 ~ 15|[VOICE TX SET: VOICE > REC/SET > SET] Sets or reads the Repeat Time setting.
1A|05 02 58|BCD 1 byte 00/01|[RECORD > Recorder Set] Sets or reads the TX REC Audio setting.
1A|05 02 59|BCD 1 byte 00/01|[RECORD > Recorder Set] Sets or reads the RX REC Condition setting.
1A|05 02 60|BCD 1 byte 00/01|[RECORD > Recorder Set] Sets or reads the File Split setting.
1A|05 02 61|BCD 1 byte 00/01|[RECORD > Recorder Set] Sets or reads the PTT Auto REC setting.
1A|05 02 62|BCD 1 byte 00 ~ 03|[RECORD > Recorder Set] Sets or reads the PRE-REC for PTT Auto REC setting.
1A|05 02 63|BCD 1 byte 00 ~ 03|[RECORD > Player Set] Sets or reads the Skip Time setting.
1A|05 02 64|See p. 22.|[MENU SET:] Sets or reads the CW [KEYER/DECODE] Key setting.
1A|05 02 65|BCD 1 byte 00 ~ 09|[NB: FUNCTION > NB (Touch for 1 second)] Sets or reads the DEPTH setting.
1A|05 02 66|BCD 2 bytes 00 00 ~ 02 25|[NB: FUNCTION > NB (Touch for 1 second)] Sets or reads the WIDTH setting.
1A|05 02 67|BCD 1 byte 00 ~ 20|[VOX: FUNCTION > VOX (Touch for 1 second)] Sets or reads the Delay setting.
1A|05 02 68|BCD 1 byte 00 ~ 03|[VOX: FUNCTION > VOX (Touch for 1 second)] Sets or reads the Voice delay setting.
1A|05 02 69|BCD 1 byte 00/01|[APF: FUNCTION > 2 > APF (Touch for 1 second)] Sets or reads the TYPE setting.
1A|05 02 70|BCD 1 byte 00 ~ 06|[APF: FUNCTION > 2 > APF (Touch for 1 second)] Sets or reads the AF LEVEL setting.
1A|06|see p.22|Set or read the DATA mode and filter width settings
1A|07|00/01|Set or read the NTP server access
1A|08|00 ~ 02|Read the NTP server access result
1B|00|see p.23|Set or read the Repeater tone frequency
1B|01|see p.23|Set or read the TSQL tone frequency
1C|00|00/01|Set or read the RX/TX status of the transceiver (PTT)
1C|01|00 ~ 02|Set or read the internal antenna tuner status
1C|02|00/01|Set or read XFC (transmit frequency monitor)
1C|03|see p.16|Read the transmit frequency
1C|04|00/01|Set or read the CI-V Output (for ANT)
1E|00||Read the number of available TX frequency bands
1E|01|see p.16|Read upper and lower edge frequencies of a TX frequency band
1E|02||Read the maximum number of User Band Edges
1E|03|see p.16|Set or read upper and lower edge frequencies of a User Band Edge
21|00|see p.23|Set or read the RIT frequency
21|01|00/01|Set or read the RIT function ON/OFF
21|02|00/01|Set or read the TX (delta TX) function ON/OFF
25||see p.23: byte 00=Selected VFO 01=Unselected VFO + FREQ 5 bytes|Set or read the operating frequency of the Selected or Unselected VFO
26||see p.24: byte 00/01 + mode + data mode 00=OFF 01=ON + filter 01-03|Set or read operating mode, data mode and IF filter width of the Selected or Unselected VFO
27|00|see p.24|Output the scope waveform data to the controller
27|10|00/01|Set or read the spectrum scope ON/OFF
27|11|00/01|Set or read scope waveform data output to PC
27|12|00 (Main only)|Set or read the Main or Sub scope
27|13|00 (Single only)|Set or read Single or Dual scope
27|14|see p.25|Set or read scope mode
27|15|see p.25|Set or read Span (Center/SCROLL-C)
27|16|see p.25|Set or read Scope Edge Number (Fixed/SCROLL-F)
27|17|see p.25|Set or read Scope Hold ON/OFF
27|19|see p.25|Set or read Scope Reference level
27|1A|see p.25|Set or read Scope Sweep speed
27|1B|00/01|Set or read Scope during Tx (CENTER TYPE)
27|1C|00 ~ 02|Set or read CENTER Type Display
27|1D|see p.25|Set or read Scope VBW
27|1E|see p.26|Set or read Scope Fixed Edge frequencies
27|20|00/01|Set or read Marker Position (FIX Type/SCROLL Type)
28|00|00|Stop the Voice TX memory transmission
28|00|01 ~ 08|Start a Voice TX memory transmission
";
            D["ic7410"] = @"
00||BCD 5 bytes frequency (10 Hz/1 Hz byte first)|Send operating frequency (transceive)
01||mode byte + filter byte|Send operating mode (transceive)
02||none (read) -> 2x BCD 5 bytes with separator 2D|Read band edge frequencies
03||BCD 5 bytes frequency|Read operating frequency
04||mode byte + filter byte|Read operating mode
05||BCD 5 bytes frequency|Send operating frequency
06||mode byte + optional filter byte|Send operating mode
07||none|Select VFO mode
07|00|none|Select VFO A
07|01|none|Select VFO B
07|A0|none|Equalize VFO A and VFO B
07|B0|none|Exchange VFO A and VFO B
08||none|Select Memory mode
08||BCD 2 bytes 0001~0101|Select memory channel
09||none|Memory write
0A||none|Memory copy to VFO
0B||none|Memory clear
0E|00|none|Scan stop
0E|01|none|Programmed/memory scan start
0E|02|none|Programmed scan start
0E|03|none|F scan start
0E|12|none|Fine programmed scan start
0E|13|none|Fine F scan start
0E|22|none|Memory scan start
0E|23|none|Select memory scan start
0E|A1|none|Set F scan span +-5 kHz
0E|A2|none|Set F scan span +-10 kHz
0E|A3|none|Set F scan span +-20 kHz
0E|A4|none|Set F scan span +-50 kHz
0E|A5|none|Set F scan span +-100 kHz
0E|A6|none|Set F scan span +-500 kHz
0E|A7|none|Set F scan span +-1 MHz
0E|B0|none|Set as non-select channel
0E|B1|none|Set as select channel
0E|D0|none|Set scan resume OFF
0E|D3|none|Set scan resume ON
0F||none (read) / reply 1 byte 00/01|Read Split function
0F|00|none|Set Split function OFF
0F|01|none|Set Split function ON
10|00|none|Send/read tuning step 10 Hz (1 Hz)
10|01|none|Send/read tuning step 100 Hz
10|02|none|Send/read tuning step 1 kHz
10|03|none|Send/read tuning step 5 kHz
10|04|none|Send/read tuning step 9 kHz
10|05|none|Send/read tuning step 10 kHz
10|06|none|Send/read tuning step 12.5 kHz
10|07|none|Send/read tuning step 20 kHz
10|08|none|Send/read tuning step 25 kHz
11|00|none|Send/read attenuator OFF
11|20|none|Send/read 20 dB attenuator
12|00|none|Send/read ANT1 selection
12|01|none|Send/read ANT2 selection
13|00|none|Announce frequency, mode and S-meter with voice synthesizer
13|01|none|Announce frequency and S-meter with voice synthesizer
13|02|none|Announce mode with voice synthesizer
14|01|BCD 2 bytes 0000~0255|AF position
14|02|BCD 2 bytes 0000~0255|RF position
14|03|BCD 2 bytes 0000~0255|SQL position
14|06|BCD 2 bytes 0000~0255|NR position
14|07|BCD 2 bytes 0000~0255|Inner TWIN PBT position
14|08|BCD 2 bytes 0000~0255|Outer TWIN PBT position
14|09|BCD 2 bytes 0000~0255|CW PITCH position
14|0A|BCD 2 bytes 0000~0255|RF PWR position
14|0B|BCD 2 bytes 0000~0255|MIC position
14|0C|BCD 2 bytes 0000~0255|KEY SPEED position
14|0D|BCD 2 bytes 0000~0255|NOTCH position
14|0E|BCD 2 bytes 0000~0255|COMP position
14|0F|BCD 2 bytes 0000~0255|BK-IN DELAY position
14|12|BCD 2 bytes 0000~0255|NB position
14|15|BCD 2 bytes 0000~0255|MONI GAIN position
14|16|BCD 2 bytes 0000~0255|VOX gain
14|17|BCD 2 bytes 0000~0255|Anti-VOX gain
14|18|BCD 2 bytes 0000~0255|CONTRAST level
14|19|BCD 2 bytes 0000~0255|BRIGHT level
15|01|1 byte 00/01|Read squelch state
15|02|BCD 2 bytes 0000~0255|Read S-meter level
15|11|BCD 2 bytes 0000~0255|Read RF power meter
15|12|BCD 2 bytes 0000~0255|Read SWR meter
15|13|BCD 2 bytes 0000~0255|Read ALC meter
15|14|BCD 2 bytes 0000~0255|Read COMP meter
16|02|1 byte 00/01/02|Preamp
16|12|1 byte 01/02/03|AGC
16|22|1 byte 00/01|Noise blanker
16|40|1 byte 00/01|Noise reduction
16|41|1 byte 00/01|Auto notch
16|42|1 byte 00/01|Repeater tone
16|43|1 byte 00/01|Tone squelch
16|44|1 byte 00/01|Speech compressor
16|45|1 byte 00/01|Monitor function
16|46|1 byte 00/01|VOX function
16|47|1 byte 00/01/02|BK-IN function
16|48|1 byte 00/01|Manual notch
16|4C|1 byte 00/01|VSC function
16|4F|1 byte 00/01|Twin peak filter
16|50|1 byte 00/01|Dial lock function
16|55|1 byte 00/01/02|1st IF filter
16|56|1 byte 00/01|DSP filter type
16|57|1 byte 00/01/02|Manual notch width
16|58|1 byte 00/01/02|SSB transmit bandwidth
17||ASCII text up to 30 chars (FF stops)|Send CW message
19|00|none (reply 1 byte ID)|Read the transceiver ID
1A|00|memory number BCD 2 bytes + memory data|Send/read memory contents
1A|01|band code + register code + data|Send/read band stacking register contents
1A|02|channel 01-04 + ASCII text|Send/read memory keyer contents
1A|03|1 byte 00~49|Send/read selected filter width
1A|04|1 byte 00~13|Send/read selected AGC time constant
1A|05 00 01|0000 to 0255|LCD contrast
1A|05 00 02|0000 to 0255|LCD backlight brightness level
1A|05 00 03|0000 to 0255|beep level
1A|05 00 04|00/01|00=beep level limit OFF ; 01=beep level limit ON
1A|05 00 05|00/01|00=confirmation beep OFF ; 01=confirmation beep ON
1A|05 00 06|00/01/02/03|00=band edge beep OFF ; 01=band edge beep ON (Beep sounds with a default band) ; 02=band edge beep with user set- ting ON ; 03=band edge beep with user set- ting/TX limit ON
1A|05 00 07|0050 to 0200|beep audio frequency
1A|05 00 08|00/01/02|00=Auto selection for [RF/SQL] ; 01=SQL selection for [RF/SQL] ; 02=RF+SQL selection for [RF/SQL]
1A|05 00 09|00/01|00=Meter Peak Hold function OFF ; 01=Meter Peak Hold function ON
1A|05 00 10|00/01/02/03/04/05|00=Time-Out Timer (CI-V) OFF ; 01=3 min. Time-Out Timer (CI-V) ; 02=5 min. Time-Out Timer (CI-V) ; 03=10 min. Time-Out Timer (CI-V) ; 04=20 min. Time-Out Timer (CI-V) ; 05=30 min. Time-Out Timer (CI-V)
1A|05 00 11|00/01|00=Quick Split function OFF ; 01=Quick Split function ON
1A|05 00 12||see p. 107 Send/read FM Split offset frequency (HF)
1A|05 00 13||see p. 107 Send/read FM Split offset frequency
1A|05 00 14|00/01|00=Split Lock function OFF ; 01=Split Lock function ON
1A|05 00 15|00/01|00=Tuner Auto Start OFF ; 01=Tuner Auto Start ON
1A|05 00 16|00/01|00=PTT Tune OFF ; 01=PTT Tune ON
1A|05 00 17|00/01/02|00=antenna selection OFF ; 01=manual antenna selection ; 02=auto antenna selection
1A|05 00 18|0000 to 0255|voice synthesizer level
1A|05 00 19|00/01|00=English selection for voice syn- thesizer speech language ; 01=Japanese selection for voice synthesizer speech language
1A|05 00 20|00/01|00=speech speed slow ; 01=speech speed fast
1A|05 00 21|00/01|00=S-meter level announcement OFF ; 01=S-meter level announcement ON
1A|05 00 22|00/01|00=operating mode announcement (after pushing mode switch) OFF ; 01=operating mode announcement (after pushing mode switch) ON
1A|05 00 23|00/01|00=number of memo pad channels ; 01=number of memo pad channels
1A|05 00 24|00/01/02|00=auto TS for [DIAL] OFF ; 01=auto TS for [DIAL] Low ; 02=auto TS for [DIAL] High
1A|05 00 25|00/01|00=Low selection for microphone Up/Down speed ; 01=High selection for microphone Up/Down speed
1A|05 00 26|00/01|00=Quick RIT/TX clear OFF ; 01=Quick RIT/TX clear ON
1A|05 00 27|00/01/02|00=Auto Notch selection for SSB ; 01=Manual Notch selection for SSB ; 02=Auto/Manual Notch selection for SSB
1A|05 00 28|00/01/02|00=Auto Notch selection for AM ; 01=Manual Notch selection for AM ; 02=Auto/Manual Notch selection for AM
1A|05 00 29|00/01|00=manual notch filter width pop- up OFF ; 01=manual notch filter width pop- up ON 104
1A|05 00 30|00/01|00=BW Popup (PBT) setting OFF ; 01=BW Popup (PBT) setting ON
1A|05 00 31|00/01|00=BW Popup (FIL) setting OFF ; 01=BW Popup (FIL) setting ON
1A|05 00 32|00/01|00=SSB/CW Synchronous Tuning function OFF ; 01=SSB/CW Synchronous Tuning function ON
1A|05 00 33|00/01|00=LSB selection for CW normal side ; 01=USB selection for CW normal side
1A|05 00 34|00/01|00=KEYER-Root selection for keyer 1st menu ; 01=KEYER-SEND selection for keyer 1st menu
1A|05 00 35|00/01|00=external keypad OFF ; 01=KEYER SEND selection for the external keypad
1A|05 00 36|00/01|00=USB audio squelch OFF (OPEN) ; 01=USB audio squelch ON
1A|05 00 37|0000 to 0255|USB modulation level
1A|05 00 38|00/01/02/03|00=MIC selection for the modula- tion input during DATA mode OFF ; 01=ACC selection for the modula- tion input during DATA mode OFF ; 02=MIC+ACC selection for the modulation input during DATA mode OFF ; 03=USB selection for the modula- tion input during DATA mode OFF
1A|05 00 39|00/01/02/03|00=MIC selection for the modula- tion input during DATA mode operation ; 01=ACC selection for the modula- tion input during DATA mode operation ; 02=MIC+ACC selection for the modulation input during DATA mode op- eration ; 03=USB selection for the modula- tion input during DATA mode operation
1A|05 00 40|00/01|00=CI-V transceive OFF ; 01=CI-V transceive ON
1A|05 00 41|00/01|00=CI-V selection for ""USB"" (COM port) function ; 01=RTTY selection for ""USB"" (COM port) function
1A|05 00 42|00/01/02/03/04|00=300 bps selection for RTTY De- code Baud rate ; 01=1200 bps selection for RTTY Decode Baud rate ; 02=4800 bps selection for RTTY Decode Baud rate ; 03=9600 bps selection for RTTY Decode Baud rate ; 04=19200 bps selection for RTTY Decode Baud rate
1A|05 00 43|00/01|00=Calibration marker OFF ; 01=Calibration marker ON
1A|05 00 44|0000 to 0255|reference frequency
1A|05 00 45||see p. 107 Send/read SSB RX HPF/LPF setting
1A|05 00 46|00 to 10|SSB RX Tone (Bass) level
1A|05 00 47|00 to 10|SSB RX Tone (Treble) level
1A|05 00 48|00 to 10|SSB TX Tone (Bass) level
1A|05 00 49|00 to 10|SSB TX Tone (Treble) level
1A|05 00 50||see p. 107 Send/read SSB TX bandwidth for WIDE
1A|05 00 51||see p. 107 Send/read SSB TX bandwidth for MID
1A|05 00 52||see p. 107 Send/read SSB TX bandwidth for NAR- ROW
1A|05 00 53||see p. 107 Send/read AM RX HPF/LPF setting
1A|05 00 54|00 to 10|AM RX tone (Bass) level
1A|05 00 55|00 to 10|AM RX Tone (Treble) level
1A|05 00 56|00 to 10|AM TX tone (Bass) level
1A|05 00 57|00 to 10|AM TX Tone (Treble) level
1A|05 00 58||see p. 107 Send/read FM RX HPF/LPF setting
1A|05 00 59|00 to 10|FM RX tone (Bass) level
1A|05 00 60|00 to 10|FM RX Tone (Treble) level
1A|05 00 61|00 to 10|FM TX tone (Bass) level
1A|05 00 62|00 to 10|FM TX Tone (Treble) level
1A|05 00 63||see p. 107 Send/read CW RX HPF/LPF setting
1A|05 00 64||see p. 107 Send/read RTTY RX HPF/LPF setting
1A|05 00 65|00/01/02/03/04|00=Normal selection for contest number style ; 01=""190 ANO"" selection for con- test number style ; 02=""190 ANT"" selection for con- test number style ; 03=""90 NO"" selection for contest number style ; 04=""90 NT"" selection for contest number style
1A|05 00 66|01/02/03/04|01=M1 selection for count up trig- ger channel ; 02=M2 selection for count up trig- ger channel ; 03=M3 selection for count up trig- ger channel ; 04=M4 selection for count up trig- ger channel
1A|05 00 67|0001 to 9999|present number
1A|05 00 68|0000 to 0255|CW sidetone gain
1A|05 00 69|00/01|00=CW sidetone gain limit OFF ; 01=CW sidetone gain limit ON
1A|05 00 70|01 to 60|CW keyer repeat time
1A|05 00 71|00/01|00=Normal selection for message display ; 01=Message selection for message display
1A|05 00 72|28 to 45|CW keyer dot/dash ratio
1A|05 00 73|00/01/02/03|00=2 msec. selection for Rise time ; 01=4 msec. selection for Rise time ; 02=6 msec. selection for Rise time ; 03=8 msec. selection for Rise time 105
1A|05 00 74|00/01|00=Normal selection for paddle po- larity ; 01=Reverse selection for paddle polarity
1A|05 00 75|00/01/02|00=Straight selection for keyer type ; 01=BUG-Key selection for keyer type ; 02=ELEC-Key selection for keyer type
1A|05 00 76|00/01|00=Mic. up/down keyer OFF ; 01=Mic. up/down keyer ON
1A|05 00 77|00/01/02|00=1275 Hz selection for RTTY mark frequency ; 01=1615 Hz selection for RTTY mark frequency ; 02=2125 Hz selection for RTTY mark frequency
1A|05 00 78|00/01/02|00=170 Hz selection for RTTY shift width ; 01=200 Hz selection for RTTY shift width ; 02=425 Hz selection for RTTY shift width
1A|05 00 79|00/01|00=Normal selection for RTTY key- ing polarity ; 01=Reverse selection for RTTY keying polarity
1A|05 00 80|00/01|00=RTTY decode USOS OFF ; 01=RTTY decode USOS ON
1A|05 00 81|00/01|00=""CR,LF,CR+LF"" selection for RTTY decode new line code ; 01=""CR+LF"" selection for RTTY decode new line code
1A|05 00 82|00/01|00=2 lines selection for number of RTTY decoder line ; 01=3 lines selection for number of RTTY decoder line
1A|05 00 83|00/01|00=Scan speed low ; 01=Scan speed high
1A|05 00 84|00/01|00=Scan resume OFF ; 01=Scan resume ON
1A|05 00 85|00 to 09|NB depth
1A|05 00 86|0000 to 0255|NB width
1A|05 00 87|0000 to 0255|VOX gain
1A|05 00 88|0000 to 0255|ANTI-VOX gain
1A|05 00 89|00 to 20|VOX delay time
1A|05 00 90|00/01/02/03|00=VOX voice delay OFF ; 01=Short selection for VOX voice delay ; 02=Mid selection for VOX voice delay ; 03=Long selection for VOX voice delay
1A|06|2 bytes: data mode 00/01 + filter 01~03|Send/read DATA mode selection
1B|00|BCD 3 bytes|Send/read repeater tone frequency
1B|01|BCD 3 bytes|Send/read tone squelch frequency
1C|00|1 byte 00/01|Send/read transceiver status (PTT)
1C|01|1 byte 00/01/02|Send/read antenna tuner
1C|02|1 byte 00/01|Send/read transmit frequency monitor check
1E|00|none|Read number of available TX frequency bands
1E|01|band edge data|Read TX band edge frequencies
1E|02|none|Read number of user-set TX frequency bands
1E|03|band edge data|Send/read user-set TX band edge frequencies
";
            D["ic7610"] = @"
00||FREQ BCD 5 bytes (see p.10)|Send frequency data (transceive)
01||MODE 2 bytes: mode,filter (see p.10)|Send mode data (transceive)
02||BAND EDGE (see p.10): edge no. + lower 5B BCD + 2D + upper 5B BCD|Read band edge frequencies
03||FREQ BCD 5 bytes|Read operating frequency
04||MODE 2 bytes: mode,filter|Read operating mode
05||FREQ BCD 5 bytes|Set operating frequency
06||MODE 1-2 bytes: mode[,filter]|Set operating mode
07||(none)|Select the VFO mode
07|B0||Exchange main and sub bands
07|B1||Equalize main and sub bands
07|C0||Turn OFF Dualwatch
07|C1||Turn ON Dualwatch
07|C2|00 or 01|Send/read the dualwatch setting
07|D0||Select the main band
07|D1||Select the sub band
07|D2|00 or 01|Send/read main/sub band selection
08||(none)|Select the Memory mode
08||BCD 2 bytes 0001 to 0099|Select the Memory channel
08||BCD 2 bytes 0100|Select program scan edge channel P1
08||BCD 2 bytes 0101|Select program scan edge channel P2
09||(none)|Memory write
0A||(none)|Memory copy to VFO
0B||(none)|Memory clear
0E|00||Cancel the scan
0E|01||Start a Programmed/memory scan
0E|02||Start a Programmed scan
0E|03||Start a F scan
0E|12||Start a Fine programmed scan
0E|13||Start a Fine F scan
0E|22||Start a Memory scan
0E|23||Start a Select memory scan
0E|A1~A7||Select F scan span
0E|B0||Clear the Select channel setting
0E|B1||Set as select channel
0E|B1|01 to 03|Set the channel as a Select channel
0E|B2|00 to 03|Set the Select memory scan channel
0E|D0||Set Scan resume OFF
0E|D3||Set Scan resume ON
0F||(none) request|Read Split setting
0F|00||Turn OFF the Split function
0F|01||Turn ON the Split function
10||00 to 08|Send/read the tuning step
11||BCD 1 byte 00,03,06,09,12,15,18,21,24,27,30,33,36,39,42,45|Send/read attenuator
12|00|00 or 01|Select/read ANT1 selection
12|01|00 or 01|Select/read ANT2 selection
13|00||Speech all data with voice synthesizer
13|01||Speech operating frequency and S meter level
13|02||Speech operating mode
14|01|BCD 2 bytes 0000~0255|Send/read AF level
14|02|BCD 2 bytes 0000~0255|Send/read RF gain
14|03|BCD 2 bytes 0000~0255|Send/read squelch level
14|05|BCD 2 bytes 0000~0255|Send/read APF level
14|06|BCD 2 bytes 0000~0255|Send/read NR level
14|07|BCD 2 bytes 0000~0255|Send/read inner TWIN PBT position
14|08|BCD 2 bytes 0000~0255|Send/read outer TWIN PBT position
14|09|BCD 2 bytes 0000~0255|Send/read CW pitch
14|0A|BCD 2 bytes 0000~0255|Send/read RF power
14|0B|BCD 2 bytes 0000~0255|Send/read MIC gain
14|0C|BCD 2 bytes 0000~0255|Send/read keying speed
14|0D|BCD 2 bytes 0000~0255|Send/read Notch filter setting
14|0E|BCD 2 bytes 0000~0255|Send/read COMP level
14|0F|BCD 2 bytes 0000~0255|Send/read Break-IN delay
14|12|BCD 2 bytes 0000~0255|Send/read NB level
14|13|BCD 2 bytes 0000~0255|Send/read DIGI-SEL shift amount
14|14|BCD 2 bytes 0000~0255|Send/read DRIVE gain
14|15|BCD 2 bytes 0000~0255|Send/read Monitor audio [MONI] level
14|16|BCD 2 bytes 0000~0255|Send/read VOX gain
14|17|BCD 2 bytes 0000~0255|Send/read Anti VOX gain
14|19|BCD 2 bytes 0000~0255|Send/read LCD backlight brightness
15|01|00 or 01|Read noise or S-meter squelch status
15|02|BCD 2 bytes 0000 to 0255|Read S-meter level
15|05|00 or 01|Read various squelch (tone squelch etc) status
15|07|00 or 01|Read the Overflow status
15|11|BCD 2 bytes 0000~0255|Read PO meter level
15|12|BCD 2 bytes 0000~0255|Read SWR meter level
15|13|BCD 2 bytes 0000~0255|Read ALC meter level
15|14|BCD 2 bytes 0000~0255|Read COMP meter level
15|15|BCD 2 bytes 0000~0255|Read Vd meter level
15|16|BCD 2 bytes 0000~0255|Read Id meter level
16|02|00,01,02|Preamp
16|12|01 ~ 03|Set the AGC time constant
16|22|00 or 01|Set the Noise blanker
16|32|00,01,02,03|Audio peak filter
16|40|00 or 01|Set the Noise reduction
16|41|00 or 01|Set the Auto Notch function
16|42|00 or 01|Set the Repeater tone
16|43|00 or 01|Set the Tone squelch
16|44|00 or 01|Set the Speech compressor
16|45|00 or 01|Set the Monitor [MONI] function
16|46|00 or 01|Set the VOX function
16|47|00,01,02|BK-IN function
16|48|00 or 01|Set the Manual Notch function
16|4E|00 or 01|Set the DIGI-SEL function
16|4F|00 or 01|Set the Twin peak filter
16|50|00 or 01|Set the Dial lock function
16|53|00 or 01|Set the ANT-RX I/O
16|56|00 or 01|Set the DSP IF filter type
16|57|00 ~ 02|Set the Manual Notch width
16|58|00 ~ 02|Set the SSB transmit bandwidth
16|5E|00 or 01|MAIN/SUB Tracking function
16|65|00 or 01|Set the IP Plus function
17||ASCII text up to 30 chars (see p.10)|Send CW messages
18|00||Turn OFF the transceiver
18|01||Turn ON the transceiver
19|00||Read the transceiver ID
1A|00|see p.11|Send/read memory contents
1A|01|see p.10|Send/read band stacking register contents
1A|02|see p.13|Send/read memory keyer contents
1A|03|see p.12|Send/read the selected IF filter width
1A|04|see p.12|Send/read the selected AGC time constant
1A|05 00 01|see p. 12|Tone Control > RX > Send/read SSB RX HPF/LPF settings
1A|05 00 02|BCD 1 byte 00 ~ 10|Tone Control > RX > Send/read SSB RX Tone (Bass) level
1A|05 00 03|BCD 1 byte 00 ~ 10|Tone Control > RX > Send/read SSB RX Tone (Treble) level
1A|05 00 04|see p. 12|Tone Control > RX > Send/read AM RX HPF/LPF settings
1A|05 00 05|BCD 1 byte 00 ~ 10|Tone Control > RX > Send/read AM RX Tone (Bass) level
1A|05 00 06|BCD 1 byte 00 ~ 10|Tone Control > RX > Send/read AM RX Tone (Treble) level
1A|05 00 07|see p. 12|Tone Control > RX > Send/read FM RX HPF/LPF settings
1A|05 00 08|BCD 1 byte 00 ~ 10|Tone Control > RX > Send/read FM RX Tone (Bass) level
1A|05 00 09|BCD 1 byte 00 ~ 10|Tone Control > RX > Send/read FM RX Tone (Treble) level
1A|05 00 10|see p. 12|Tone Control > RX > Send/read CW RX HPF/LPF settings
1A|05 00 11|see p. 12|Tone Control > RX > Send/read RTTY RX HPF/LPF settings
1A|05 00 12|see p. 12|Tone Control > RX > Send/read PSK RX HPF/LPF settings
1A|05 00 13|BCD 1 byte 00 ~ 10|Tone Control > TX > Send/read SSB TX Tone (Bass) level
1A|05 00 14|BCD 1 byte 00 ~ 10|Tone Control > TX > Send/read SSB TX Tone (Treble) level
1A|05 00 15|see p. 12|Tone Control > TX > Send/read SSB TX bandwidth for wide
1A|05 00 16|see p. 12|Tone Control > TX > Send/read SSB TX bandwidth for mid
1A|05 00 17|see p. 12|Tone Control > TX > Send/read SSB TX bandwidth for narrow
1A|05 00 18|BCD 1 byte 00 ~ 10|Tone Control > TX > Send/read AM TX Tone (Bass) level
1A|05 00 19|BCD 1 byte 00 ~ 10|Tone Control > TX > Send/read AM TX Tone (Treble) level
1A|05 00 20|BCD 1 byte 00 ~ 10|Tone Control > TX > Send/read FM TX Tone (Bass) level
1A|05 00 21|BCD 1 byte 00 ~ 10|Tone Control > TX > Send/read FM TX Tone (Treble) level
1A|05 00 22|BCD 2 bytes 0000 ~ 0255|Function > Beep Level
1A|05 00 23|BCD 1 byte 00 or 01|Function > Beep Level Limit
1A|05 00 24|BCD 1 byte 00 or 01|Function > Beep (Confirmation)
1A|05 00 25|BCD 1 byte 00 or 01|Function > Band Edge Beep
1A|05 00 26|BCD 2 bytes 0050 ~ 0200|Function > Beep Sound (MAIN)
1A|05 00 27|BCD 2 bytes 0050 ~ 0200|Function > Beep Sound (SUB)
1A|05 00 28|BCD 1 byte 00 ~ 02|Function > RF/SQL Control
1A|05 00 29|BCD 1 byte 00 ~ 05|Function > TX Delay > HF
1A|05 00 30|BCD 1 byte 00 ~ 05|Function > TX Delay > 50M
1A|05 00 31|BCD 1 byte 00 ~ 05|Function > Time-Out Timer (CI-V)
1A|05 00 32|BCD 1 byte 00 or 01|Function > Quick Dualwatch
1A|05 00 33|BCD 1 byte 00 or 01|Function > SPLIT > Quick SPLIT
1A|05 00 34|BCD 1 byte 00 or 01|Function > SPLIT > Display Keypad on Quick SPLIT
1A|05 00 35|see p. 12|Function > SPLIT > FM SPLIT Offset (HF)
1A|05 00 36|see p. 12|Function > SPLIT > FM SPLIT Offset (50M)
1A|05 00 37|BCD 1 byte 00 or 01|Function > SPLIT > SPLIT LOCK
1A|05 00 38|BCD 1 byte 00 or 01|Function > Tuner > PTT Start
1A|05 00 39|BCD 1 byte 00 or 01|Function > Transverter Function
1A|05 00 40|see p. 12|Function > Transverter Offset
1A|05 00 41|BCD 1 byte 00 ~ 02|Function > RTTY Mark Frequency
1A|05 00 42|BCD 1 byte 00 ~ 02|Function > RTTY Shift Width
1A|05 00 43|BCD 1 byte 00 or 01|Function > RTTY Keying Polarity
1A|05 00 44|BCD 1 byte 00 ~ 02|Function > PSK Tone Frequency
1A|05 00 45|BCD 1 byte 00 or 01|Function > SPEECH > SPEECH Language
1A|05 00 46|BCD 1 byte 00 or 01|Function > SPEECH > SPEECH Speed
1A|05 00 47|BCD 1 byte 00 or 01|Function > SPEECH > S-Level SPEECH
1A|05 00 48|BCD 1 byte 00 or 01|Function > SPEECH > MODE SPEECH
1A|05 00 49|BCD 2 bytes 0000 ~ 0255|Function > SPEECH > SPEECH Level
1A|05 00 50|BCD 1 byte 00 or 01|Function > [SPEECH/LOCK] Switch
1A|05 00 51|BCD 1 byte 00 or 01|Function > Lock Function
1A|05 00 52|BCD 1 byte 00 or 01|Function > Memo Pad Quantity
1A|05 00 53|BCD 1 byte 00 to 02|Function > MAIN DIAL Auto TS
1A|05 00 54|BCD 1 byte 00 or 01|Function > MAIN DIAL Select (USB DIAL-SUB Only)
1A|05 00 55|BCD 1 byte 00 or 01|Function > MAIN/SUB Tracking [MAIN/SUB] Switch
1A|05 00 56|BCD 1 byte 00 or 01|Function > MIC Up/Down Speed
1A|05 00 57|BCD 1 byte 00 or 01|Function > Quick RIT/TX Clear
1A|05 00 58|BCD 1 byte 00 ~ 02|Function > [NOTCH] Switch (SSB)
1A|05 00 59|BCD 1 byte 00 ~ 02|Function > [NOTCH] Switch (AM)
1A|05 00 60|BCD 1 byte 00 or 01|Function > FILTER Screen MAIN/SUB Select
1A|05 00 61|BCD 1 byte 00 or 01|Function > SSB/CW Synchronous Tuning
1A|05 00 62|BCD 1 byte 00 or 01|Function > CW Normal Side
1A|05 00 63|BCD 1 byte 00 or 01|Function > Screen Keyboard Type
1A|05 00 64|BCD 1 byte 00 ~ 02|Function > Screen Full Keyboard Layout
1A|05 00 65|BCD 1 byte 00 or 01|Function > Screen Capture [POWER] Switch
1A|05 00 66|BCD 1 byte 00 or 01|Function > Screen Capture Keyboard [Print Screen]
1A|05 00 67|BCD 1 byte 00 or 01|Function > Screen Capture Storage Media
1A|05 00 68|BCD 1 byte 00 or 01|Function > Screen Capture File Type
1A|05 00 69|BCD 1 byte 00 or 01|Function > Calibration Marker
1A|05 00 70|BCD 2 bytes 0000 ~ 0511|Function > REF Adjust
1A|05 00 71|BCD 1 byte 00 ~ 30|Connectors > Phones > Level
1A|05 00 72|BCD 1 byte 00 or 01|Connectors > Phones > L/R Mix
1A|05 00 73|BCD 1 byte 00 or 01|Connectors > ACC AF/IF Output > AF/SQL Output Select
1A|05 00 74|BCD 1 byte 00 or 01|Connectors > ACC AF/IF Output > Output Select
1A|05 00 75|BCD 1 byte 00 or 01|Connectors > ACC AF/IF Output > AF/IF XFC Output (SPLIT ON)
1A|05 00 76|BCD 2 bytes 0000 ~ 0255|Connectors > ACC AF/IF Output > AF Output Level
1A|05 00 77|BCD 1 byte 00 or 01|Connectors > ACC AF/IF Output > AF SQL
1A|05 00 78|BCD 1 byte 00 or 01|Connectors > ACC AF/IF Output > AF Beep/Speech... Output
1A|05 00 79|BCD 2 bytes 0000 ~ 0255|Connectors > ACC AF/IF Output > ACC IF Output Level
1A|05 00 80|BCD 1 byte 00 or 01|Connectors > USB AF/IF Output > Output Select
1A|05 00 81|BCD 1 byte 00 or 01|Connectors > USB AF/IF Output > AF/IF XFC Output (SPLIT ON)
1A|05 00 82|BCD 2 bytes 0000 ~ 0255|Connectors > USB AF/IF Output > AF Output Level
1A|05 00 83|BCD 1 byte 00 or 01|Connectors > USB AF/IF Output > AF SQL
1A|05 00 84|BCD 1 byte 00 or 01|Connectors > USB AF/IF Output > AF Beep/Speech... Output
1A|05 00 85|BCD 2 bytes 0000 ~ 0255|Connectors > USB AF/IF Output > IF Output Level
1A|05 00 86|BCD 1 byte 00 or 01|Connectors > LAN AF/IF Output > Output Select
1A|05 00 87|BCD 1 byte 00 or 01|Connectors > LAN AF/IF Output > AF SQL
1A|05 00 88|BCD 2 bytes 0000 ~ 0255|Connectors > MOD Input > ACC MOD Level
1A|05 00 89|BCD 2 bytes 0000 ~ 0255|Connectors > MOD Input > USB MOD Level
1A|05 00 90|BCD 2 bytes 0000 ~ 0255|Connectors > MOD Input > LAN MOD Level
1A|05 00 91|BCD 1 byte 00 ~ 05|Connectors > MOD Input > DATA OFF MOD
1A|05 00 92|BCD 1 byte 00 ~ 05|Connectors > MOD Input > DATA1 MOD
1A|05 00 93|BCD 1 byte 00 ~ 05|Connectors > MOD Input > DATA2 MOD
1A|05 00 94|BCD 1 byte 00 ~ 05|Connectors > MOD Input > DATA3 MOD
1A|05 00 95|BCD 1 byte 00 ~ 04|Connectors > USB SEND/Keying > USB SEND
1A|05 00 96|BCD 1 byte 00 ~ 04|Connectors > USB SEND/Keying > USB Keying (CW)
1A|05 00 97|BCD 1 byte 00 ~ 04|Connectors > USB SEND/Keying > USB Keying (RTTY)
1A|05 00 98|BCD 1 byte 00 or 01|Connectors > External Keypad > VOICE
1A|05 00 99|BCD 1 byte 00 or 01|Connectors > External Keypad > KEYER
1A|05 01 00|BCD 1 byte 00 or 01|Connectors > External Keypad > RTTY
1A|05 01 01|BCD 1 byte 00 or 01|Connectors > External Keypad > PSK
1A|05 01 02|BCD 1 byte 00 or 01|Connectors > Keyboard/Mouse > Keyboard [F1] ~ [F8] (VOICE)
1A|05 01 03|BCD 1 byte 00 or 01|Connectors > Keyboard/Mouse > Keyboard [F1] ~ [F8] (KEYER)
1A|05 01 04|BCD 1 byte 00 ~ 10|Connectors > Keyboard/Mouse > Keyboard Type
1A|05 01 05|BCD 2 bytes 0010 ~ 0100|Connectors > Keyboard/Mouse > Keyboard Repeat Delay
1A|05 01 06|BCD 1 byte 00 ~ 31|Connectors > Keyboard/Mouse > Keyboard Repeat Rate
1A|05 01 07|BCD 1 byte 00 ~ 02|Connectors > Keyboard/Mouse > Mouse Pointer Speed
1A|05 01 08|BCD 1 byte 00 or 01|Connectors > Keyboard/Mouse > Mouse Pointer Acceleration
1A|05 01 09|BCD 1 byte 00 or 01|Connectors > USB DIAL > USB DIAL Select
1A|05 01 10|BCD 1 byte 00 ~ 02|Connectors > USB DIAL > USB DIAL Auto TS
1A|05 01 11|BCD 1 byte 00 or 01|Connectors > USB DIAL > USB DIAL [TRANSMIT] Switch
1A|05 01 12|BCD 1 byte 00 or 01|Connectors > CI-V > CI-V Transceive
1A|05 01 13|BCD 2 bytes 0000 ~ 0223|Connectors > CI-V > CI-V USB/ LANREMOTE Transceive Address
1A|05 01 14|BCD 1 byte 00 or 01|Connectors > CI-V > CI-V Output (for ANT)
1A|05 01 15|BCD 1 byte 00 or 01|Connectors > CI-V > CI-V USB Port
1A|05 01 16|BCD 1 byte 00 or 01|Connectors > CI-V > CI-V USB Echo Back
1A|05 01 17|BCD 1 byte 00 ~ 07|Connectors > External Meter > External Meter Output (MAIN)
1A|05 01 18|BCD 1 byte 00 ~ 07|Connectors > External Meter > External Meter Output (SUB)
1A|05 01 19|BCD 2 bytes 0000 ~ 0255|Connectors > External Meter > External Meter Level (MAIN)
1A|05 01 20|BCD 2 bytes 0000 ~ 0255|Connectors > External Meter > External Meter Level (SUB)
1A|05 01 21|BCD 1 byte 00 ~ 03|Connectors > Decode Baud Rate
1A|05 01 22|BCD 1 byte 00 or 01|Connectors > SEND Relay Type
1A|05 01 23|BCD 1 byte 00 ~ 02|Connectors > ACC BAND Voltage Output
1A|05 01 24|BCD 1 byte 00 or 01|Connectors > MIC Input DC Bias
1A|05 01 25|BCD 1 byte 00 or 01|Connectors > REF IN
1A|05 01 26|BCD 1 byte 00 or 01|Network > DHCP (valid after restart)
1A|05 01 27|BCD 8 bytes 0000000000 000001 ~ 0255025502 550254|Network > IP Address (valid after restart)
1A|05 01 28|BCD 8 bytes 0000000000 000001 ~ 0255025502 550254|Network > DHCP (valid after restart) Read the IP address set by the DHCP server
1A|05 01 29|BCD 1 byte 01 ~ 30|Network > Subnet Mask (valid after restart)
1A|05 01 30|BCD 8 bytes 0000000000 000001 ~ 0255025502 550254, FF|Network > Default Gateway (valid after restart)
1A|05 01 31|BCD 8 bytes 0000000000 000001 ~ 0255025502 550254, FF|Network > Primary DNS Server (valid after restart)
1A|05 01 32|BCD 8 bytes 0000000000 000001 ~ 0255025502 550254, FF|Network > 2nd DNS Server (valid after restart)
1A|05 01 33|see p. 11|Network > Network Name
1A|05 01 34|BCD 1 byte 00 or 01|Network > Network Control (valid after restart)
1A|05 01 35|BCD 1 byte 00 or 01|Network > Power OFF Setting (for Remote Control)
1A|05 01 36|BCD 2 bytes 000001 ~ 065535|Network > Control Port (UDP) (valid after restart)
1A|05 01 37|BCD 2 bytes 000001 ~ 065535|Network > Serial Port (UDP) (valid after restart)
1A|05 01 38|BCD 2 bytes 000001 ~ 065535|Network > Audio Port (UDP) (valid after restart)
1A|05 01 39|BCD 1 byte 00 or 01|Network > Internet Access Line (valid after restart)
1A|05 01 40|see p. 11|Network > Network Radio Name
1A|05 01 41|BCD 2 bytes 0000 ~ 0255|Display > LCD Backlight
1A|05 01 42|BCD 2 bytes 0000 ~ 0255|Display > LED Bright
1A|05 01 43|BCD 1 byte 00 or 01|Display > Display Type
1A|05 01 44|BCD 1 byte 00 or 01|Display > Display Font
1A|05 01 45|BCD 1 byte 00 ~ 02|Display > Meter Response (Standard, Edgewise)
1A|05 01 46|BCD 1 byte 00 ~ 02|Display > Meter Type (Normal Screen)
1A|05 01 47|BCD 1 byte 00 or 01|Display > Meter Type (Expand Screen)
1A|05 01 48|BCD 1 byte 00 or 01|Display > Meter Peak Hold (Bar)
1A|05 01 49|BCD 1 byte 00 or 01|Display > Memory Name
1A|05 01 50|BCD 1 byte 00 or 01|Display > APF-Width Popup (APF OFFON)
1A|05 01 51|BCD 1 byte 00 ~ 03|Display > Screen Saver
1A|05 01 52|BCD 1 byte 00 or 01|Display > External Display
1A|05 01 53|BCD 1 byte 00 or 01|Display > External Display Resolution
1A|05 01 54|BCD 1 byte 00 or 01|Display > Opening Message
1A|05 01 55|see p. 11|Display > My Call
1A|05 01 56|BCD 1 byte 00 or 01|Display > Power ON Check
1A|05 01 57|BCD 1 byte 00 or 01|Display > Display Language
1A|05 01 58|BCD 2 bytes 20000101 ~ 20991231|Time Set > Date/Time > Date
1A|05 01 59|BCD 2 bytes 0000 ~ 2359|Time Set > Date/Time > Time
1A|05 01 60|BCD 1 byte 00 or 01|Time Set > Date/Time > NTP Function
1A|05 01 61|see p. 11|Time Set > Date/Time > NTP Server Address
1A|05 01 62|see p. 13|Time Set > UTC Offset
1A|05 01 63|BCD 1 byte 00 or 01|Time Set > CLOCK2 Function
1A|05 01 64|see p. 13|Time Set > CLOCK2 UTC Offset
1A|05 01 65|see p. 11|Time Set > CLOCK2 Name
1A|05 01 66|BCD 1 byte 00 or 01|SCOPE > Scope during Tx (CENTER TYPE)
1A|05 01 67|BCD 1 byte 00 ~ 02|SCOPE > Max Hold
1A|05 01 68|BCD 1 byte 00 ~ 02|SCOPE > CENTER Type Display
1A|05 01 69|BCD 1 byte 00 or 01|SCOPE > Marker Position (Fix Type)
1A|05 01 70|BCD 1 byte 00 ~ 03|SCOPE > Averaging
1A|05 01 71|BCD 1 byte 00 or 01|SCOPE > Waveform Type
1A|05 01 72|see p. 12|SCOPE > Waveform Color (Current)
1A|05 01 73|see p. 12|SCOPE > Waveform Color (Line)
1A|05 01 74|see p. 12|SCOPE > Waveform Color (Max Hold)
1A|05 01 75|BCD 1 byte 00 or 01|SCOPE > Waterfall Display
1A|05 01 76|BCD 1 byte 00 ~ 02|SCOPE > Waterfall Speed
1A|05 01 77|BCD 1 byte 00 ~ 02|SCOPE > Waterfall Size (Expand Screen)
1A|05 01 78|BCD 1 byte 00 ~ 09|SCOPE > Waterfall Peak Color Level
1A|05 01 79|BCD 1 byte 00 or 01|SCOPE > Waterfall Marker Auto-hide
1A|05 01 80|BCD 1 byte 00 or 01|SCOPE > Dual Scope Type
1A|05 01 81|BCD 1 byte 00 or 01|SCOPE > Dual Scope Auto Select
1A|05 01 82|see p. 12|SCOPE > Fixed Edges > 0.03 - 1.60 No.1
1A|05 01 83|see p. 12|SCOPE > Fixed Edges > 0.03 - 1.60 No.2
1A|05 01 84|see p. 12|SCOPE > Fixed Edges > 0.03 - 1.60 No.3
1A|05 01 85|see p. 12|SCOPE > Fixed Edges > 1.60 - 2.00 No.1
1A|05 01 86|see p. 12|SCOPE > Fixed Edges > 1.60 - 2.00 No.2
1A|05 01 87|see p. 12|SCOPE > Fixed Edges > 1.60 - 2.00 No.3
1A|05 01 88|see p. 12|SCOPE > Fixed Edges > 2.00 - 6.00 No.1
1A|05 01 89|see p. 12|SCOPE > Fixed Edges > 2.00 - 6.00 No.2
1A|05 01 90|see p. 12|SCOPE > Fixed Edges > 2.00 - 6.00 No.3
1A|05 01 91|see p. 12|SCOPE > Fixed Edges > 6.00 - 8.00 No.1
1A|05 01 92|see p. 12|SCOPE > Fixed Edges > 6.00 - 8.00 No.2
1A|05 01 93|see p. 12|SCOPE > Fixed Edges > 6.00 - 8.00 No.3
1A|05 01 94|see p. 12|SCOPE > Fixed Edges > 8.00 - 11.00 No.1
1A|05 01 95|see p. 12|SCOPE > Fixed Edges > 8.00 - 11.00 No.2
1A|05 01 96|see p. 12|SCOPE > Fixed Edges > 8.00 - 11.00 No.3
1A|05 01 97|see p. 12|SCOPE > Fixed Edges > 11.00 - 15.00 No.1
1A|05 01 98|see p. 12|SCOPE > Fixed Edges > 11.00 - 15.00 No.2
1A|05 01 99|see p. 12|SCOPE > Fixed Edges > 11.00 - 15.00 No.3
1A|05 02 00|see p. 12|SCOPE > Fixed Edges > 15.00 - 20.00 No.1
1A|05 02 01|see p. 12|SCOPE > Fixed Edges > 15.00 - 20.00 No.2
1A|05 02 02|see p. 12|SCOPE > Fixed Edges > 15.00 - 20.00 No.3
1A|05 02 03|see p. 12|SCOPE > Fixed Edges > 20.00 - 22.00 No.1
1A|05 02 04|see p. 12|SCOPE > Fixed Edges > 20.00 - 22.00 No.2
1A|05 02 05|see p. 12|SCOPE > Fixed Edges > 20.00 - 22.00 No.3
1A|05 02 06|see p. 12|SCOPE > Fixed Edges > 22.00 - 26.00 No.1
1A|05 02 07|see p. 12|SCOPE > Fixed Edges > 22.00 - 26.00 No.2
1A|05 02 08|see p. 12|SCOPE > Fixed Edges > 22.00 - 26.00 No.3
1A|05 02 09|see p. 12|SCOPE > Fixed Edges > 26.00 - 30.00 No.1
1A|05 02 10|see p. 12|SCOPE > Fixed Edges > 26.00 - 30.00 No.2
1A|05 02 11|see p. 12|SCOPE > Fixed Edges > 26.00 - 30.00 No.3
1A|05 02 12|see p. 12|SCOPE > Fixed Edges > 30.00 - 45.00 No.1
1A|05 02 13|see p. 12|SCOPE > Fixed Edges > 30.00 - 45.00 No.2
1A|05 02 14|see p. 12|SCOPE > Fixed Edges > 30.00 - 45.00 No.3
1A|05 02 15|see p. 12|SCOPE > Fixed Edges > 45.00 - 60.00 No.1
1A|05 02 16|see p. 12|SCOPE > Fixed Edges > 45.00 - 60.00 No.2
1A|05 02 17|see p. 12|SCOPE > Fixed Edges > 45.00 - 60.00 No.3
1A|05 02 18|BCD 1 byte 00 or 01|AUDIO SCOPE SET > FFT Scope Waveform Type
1A|05 02 19|see p. 12|AUDIO SCOPE SET > FFT Scope Waveform Color
1A|05 02 20|BCD 1 byte 00 or 01|AUDIO SCOPE SET > FFT Scope Waterfall Display
1A|05 02 21|see p. 12|AUDIO SCOPE SET > Oscilloscope Waveform Color
1A|05 02 22|BCD 1 byte 00 ~ 04|KEYER 001 > Number Style
1A|05 02 23|BCD 1 byte 01 ~ 08|KEYER 001 > Count Up Trigger
1A|05 02 24|BCD 2 bytes 0001 ~ 9999|KEYER 001 > Present Number
1A|05 02 25|BCD 2 bytes 0000 ~ 0255|CW-KEY SET > Side Tone Level
1A|05 02 26|BCD 1 byte 00 or 01|CW-KEY SET > Side Tone Level Limit
1A|05 02 27|BCD 1 byte 01 ~ 60|CW-KEY SET > Keyer Repeat time
1A|05 02 28|BCD 1 byte 28 ~ 45|CW-KEY SET > Dot/Dash Ratio
1A|05 02 29|BCD 1 byte 00 ~ 03|CW-KEY SET > Rise Time
1A|05 02 30|BCD 1 byte 00 or 01|CW-KEY SET > Paddle Polarity
1A|05 02 31|BCD 1 byte 00 ~ 02|CW-KEY SET > Key Type
1A|05 02 32|BCD 1 byte 00 or 01|CW-KEY SET > MIC Up/Down Keyer
1A|05 02 33|BCD 1 byte 00 ~ 03|RTTY DECODE SET > FFT Scope Averaging
1A|05 02 34|see p. 12|RTTY DECODE SET > FFT Scope Waveform Color
1A|05 02 35|BCD 1 byte 00 or 01|RTTY DECODE SET > Decode USOS
1A|05 02 36|BCD 1 byte 00 or 01|RTTY DECODE SET > Decode New Line Code
1A|05 02 37|BCD 1 byte 00 ~ 02|RTTY DECODE SET > Diddle
1A|05 02 38|BCD 1 byte 00 or 01|RTTY DECODE SET > TX USOS
1A|05 02 39|BCD 1 byte 00 or 01|RTTY DECODE SET > Auto CR+LF by TX
1A|05 02 40|BCD 1 byte 00 or 01|RTTY DECODE SET > Time Stamp
1A|05 02 41|BCD 1 byte 00 or 01|RTTY DECODE SET > Time Stamp (Time)
1A|05 02 42|BCD 1 byte 00 or 01|RTTY DECODE SET > Time Stamp (Frequency)
1A|05 02 43|see p. 12|RTTY DECODE SET > Font Color (Receive)
1A|05 02 44|see p. 12|RTTY DECODE SET > Font Color (Transmit)
1A|05 02 45|see p. 12|RTTY DECODE SET > Font Color (Time Stamp)
1A|05 02 46|see p. 12|RTTY DECODE SET > Font Color (TX Buffer)
1A|05 02 47|BCD 1 byte 00 or 01|RTTY DECODE LOG > Decode Log
1A|05 02 48|BCD 1 byte 00 or 01|RTTY DECODE LOG > Log Set > File Type
1A|05 02 49|BCD 1 byte 00 ~ 03|PSK DECODE SET > FFT Scope Averaging
1A|05 02 50|see p. 12|PSK DECODE SET > FFT Scope Waveform Color
1A|05 02 51|BCD 1 byte 00 or 01|PSK DECODE SET > AFC Range
1A|05 02 52|BCD 1 byte 00 or 01|PSK DECODE SET > Time Stamp
1A|05 02 53|BCD 1 byte 00 or 01|PSK DECODE SET > Time Stamp (Time)
1A|05 02 54|BCD 1 byte 00 or 01|PSK DECODE SET > Time Stamp (Frequency)
1A|05 02 55|see p. 12|PSK DECODE SET > Font Color (Receive)
1A|05 02 56|see p. 12|PSK DECODE SET > Font Color (Transmit)
1A|05 02 57|see p. 12|PSK DECODE SET > Font Color (Time Stamp)
1A|05 02 58|see p. 12|PSK DECODE SET > Font Color (TX Buffer)
1A|05 02 59|BCD 1 byte 00 or 01|PSK DECODE LOG > Decode Log
1A|05 02 60|BCD 1 byte 00 or 01|PSK DECODE LOG > Log Set > File Type
1A|05 02 61|BCD 1 byte 00 or 01|SCAN SET > SCAN Speed
1A|05 02 62|BCD 1 byte 00 or 01|SCAN SET > SCAN Resume
1A|05 02 63|BCD 2 bytes 0000 ~ 0255|VOICE TX > TX LEVEL
1A|05 02 64|BCD 1 byte 00 or 01|VOICE TX SET > Auto Monitor
1A|05 02 65|BCD 1 byte 01 ~ 15|VOICE TX SET > Repeat Time
1A|05 02 66|BCD 1 byte 00 or 01|Recorder Set > REC Mode
1A|05 02 67|BCD 1 byte 00 or 01|Recorder Set > TX REC Audio
1A|05 02 68|BCD 1 byte 00 or 01|Recorder Set > RX REC Condition
1A|05 02 69|BCD 1 byte 00 or 01|Recorder Set > File Split
1A|05 02 70|BCD 1 byte 00 or 01|Recorder Set > PTT Auto REC
1A|05 02 71|BCD 1 byte 00 ~ 03|Recorder Set > PRE-REC for PTT Auto REC
1A|05 02 72|BCD 1 byte 00 ~ 03|Player Set > Skip Time
1A|05 02 73|BCD 1 byte 05 ~ 30|Instant Replay Set > REC Time
1A|05 02 74|BCD 1 byte 03 ~ 10|Instant Replay Set > Play Time
1A|05 02 75|BCD 1 byte 00 or 01|TYPE SET > RX-ANT Connectors
1A|05 02 76|see p. 13|ANTENNA MEMORY
1A|05 02 77|see p. 13|ANTENNA MEMORY (1.60 MHz to 2.00 MHz)
1A|05 02 78|see p. 13|ANTENNA MEMORY (2.00 MHz to 6.00 MHz)
1A|05 02 79|see p. 13|ANTENNA MEMORY (6.00 MHz to 8.00 MHz)
1A|05 02 80|see p. 13|ANTENNA MEMORY (8.00 MHz to 11.00 MHz)
1A|05 02 81|see p. 13|ANTENNA MEMORY (11.00 MHz to 15.00 MHz)
1A|05 02 82|see p. 13|ANTENNA MEMORY (15.00 MHz to 20.00 MHz)
1A|05 02 83|see p. 13|ANTENNA MEMORY (20.00 MHz to 22.00 MHz)
1A|05 02 84|see p. 13|ANTENNA MEMORY (22.00 MHz to 26.00 MHz)
1A|05 02 85|see p. 13|ANTENNA MEMORY (26.00 MHz to 30.00 MHz)
1A|05 02 86|see p. 13|ANTENNA MEMORY (30.00 MHz to 45.00 MHz)
1A|05 02 87|see p. 13|ANTENNA MEMORY (45.00 MHz to 60.00 MHz)
1A|05 02 88|BCD 1 byte 00 or 01|Temporary memory (TEMP-M) function
1A|05 02 89|BCD 1 byte 00 ~ 02|Send the Antenna selection mode ([ANT] SW)
1A|05 02 90|BCD 1 byte 00 ~ 09|NB depth
1A|05 02 91|BCD 2 bytes 0000 ~ 0255|NB width
1A|05 02 92|BCD 1 byte 00 ~ 20|VOX delay
1A|05 02 93|BCD 1 byte 00 ~ 03|VOX voice delay
1A|05 02 94|BCD 1 byte 00 or 01|APF > TYPE
1A|05 02 95|BCD 1 byte 00 ~ 06|APF > AF LEVEL
1A|06|see p.12|DATA mode with filter set
1A|07|00 or 01|NTP server access
1A|08|00 ~ 02|Read NTP server access result
1A|09|00 or 01|AF Mute
1B|00|see p.13|Repeater tone frequency
1B|01|see p.13|TSQL tone frequency
1C|00|00 or 01|Transceiver's status
1C|01|00 ~ 02|Antenna tuner
1C|02|00 or 01|Transmit frequency monitor (XFC)
1C|03|see p.10|Read transmit frequency
1C|04|00 or 01|CI-V Output (for ANT)
1E|00||Read number of available TX frequency band
1E|01|see p.10|Read TX band edge frequencies
1E|02||Read number of user-set TX frequency band
1E|03|see p.10|Send/read user-set TX band edge frequencies
21|00|see p.13|RIT frequency
21|01|00 or 01|RIT setting
21|02|00 or 01|TX setting (delta TX)
25||see p.13: 1 byte 00=MAIN 01=SUB + freq BCD 5 bytes|Send/read the main or sub band frequency
26||see p.13: 1 byte 00=MAIN 01=SUB + mode,datamode,filter|Send/read operating mode and filter setting (both Main and Sub)
27|00|see p.14|Read the Scope waveform data
27|10|00 or 01|Scope ON/OFF status
27|11|00 or 01|Scope wave data output
27|12|00 or 01|Main or Sub scope setting
27|13|00 or 01|Single/Dual scope setting
27|14|see p.14|Scope Center mode or Fixed mode setting
27|15|see p.14|Span setting in the Center mode Scope
27|16|see p.14|Edge number setting in the Fixed mode Scope
27|17|see p.14|Scope hold function ON or OFF
27|19|see p.15|Scope Reference level setting
27|1A|see p.15|Sweep speed setting
27|1B|00 or 01|SCOPE > Scope during Tx (CENTER TYPE)
27|1C|00 ~ 02|SCOPE > CENTER Type Display
27|1D|see p.15|Scope VBW setting
27|1E|see p.15|Scope Fixed edge frequencies
27|1F|see p.15|Scope RBW setting
28|00|00 ~ 08|Voice TX Memory
";
            D["ic7760"] = @"
00||FREQ BCD 5 bytes (see p.17)|Send the frequency data (transceive)
01||MODE bytes (see p.17)|Send the mode data (transceive)
02||BAND EDGE (see p.17)|Read the band edge frequencies
03||FREQ BCD 5 bytes|Read the operating frequency
04||MODE 2 bytes mode,filter|Read the operating mode
05||FREQ BCD 5 bytes|Set the operating frequency
06||MODE bytes mode[,filter]|Set the operating mode
07||(none)|Select the VFO mode
07|B0||Exchange Main and Sub bands
07|B1||Equalize Main and Sub bands
07|C0||Turn OFF Dualwatch
07|C1||Turn ON Dualwatch
07|C2|00/01|Send/read the Dualwatch setting
07|D0||Select the Main band
07|D1||Select the Sub band
07|D2|00 or 01|Send/read Main/Sub band selection
08||(none)|Select the Memory mode
08||BCD 2 bytes 00 01 ~ 00 99|Select the Memory channel
08||BCD 2 bytes 01 00|Select program scan edge channel P1
08||BCD 2 bytes 01 01|Select program scan edge channel P2
09||(none)|Memory write
0A||(none)|Memory copy to VFO
0B||(none)|Memory clear
0E|00||Cancel the scan
0E|01||Start a Programmed/memory scan
0E|02||Start a Programmed scan
0E|03||Start a F scan
0E|12||Start a Fine programmed scan
0E|13||Start a Fine F scan
0E|22||Start a Memory scan
0E|23||Start a Select memory scan
0E|A1~A7||Select F scan span
0E|B0||Clear the Select channel setting
0E|B1||Set as select channel
0E|B1|01 ~ 03|Set the channel as a Select channel
0E|B2|00 ~ 03|Set the Select memory scan channel
0E|D0||Set Scan resume OFF
0E|D3||Set Scan resume ON (Close&Delay)
0F||(none) request, reply 00/01|Read Split setting
0F|00||Set Split function OFF
0F|01||Set Split function ON
10||00 ~ 08|Send/read the tuning step
11||BCD 1 byte 00,03,06,09,12,15,18,21,24,27,30,33,36,39,42,45|Send/read attenuator
12|00|00/01|Select/read ANT1 selection
12|01|00/01|Select/read ANT2 selection
12|02|00/01|Select/read ANT3 selection
12|03|00/01|Select/read ANT4 selection
13|00||Speak all data by the voice synthesizer
13|01||Speak the operating frequency and S meter level
13|02||Speak the operating mode
14|01|BCD 2 bytes 00 00 ~ 02 55|Send/read the AF level
14|02|BCD 2 bytes 00 00 ~ 02 55|Send/read the RF gain level
14|03|BCD 2 bytes 00 00 ~ 02 55|Send/read the squelch level
14|05|BCD 2 bytes 00 00 ~ 02 55|Send/read the APF position
14|06|BCD 2 bytes 00 00 ~ 02 55|Send/read the NR level
14|07|BCD 2 bytes 00 00 ~ 02 55|Send/read TWIN PBT (PBT1) position
14|08|BCD 2 bytes 00 00 ~ 02 55|Send/read TWIN PBT (PBT2) position
14|09|BCD 2 bytes 00 00 ~ 02 55|Send/read CW pitch
14|0A|BCD 2 bytes 00 00 ~ 02 55|Send/read the selected band's RF power
14|0B|BCD 2 bytes 00 00 ~ 02 55|Send/read MIC gain
14|0C|BCD 2 bytes 00 00 ~ 02 55|Send/read keying speed
14|0D|BCD 2 bytes 00 00 ~ 02 55|Send/read Notch filter setting
14|0E|BCD 2 bytes 00 00 ~ 02 55|Send/read the COMP level
14|0F|BCD 2 bytes 00 00 ~ 02 55|Send/read the Break-IN delay
14|12|BCD 2 bytes 00 00 ~ 02 55|Send/read NB level
14|13|BCD 2 bytes 00 00 ~ 02 55|Send/read the DIGI-SEL shift amount
14|14|BCD 2 bytes 00 00 ~ 02 55|Send/read DRIVE Gain
14|15|BCD 2 bytes 00 00 ~ 02 55|Send/read Monitor audio [MONI] level
14|16|BCD 2 bytes 00 00 ~ 02 55|Send/read the VOX gain
14|17|BCD 2 bytes 00 00 ~ 02 55|Send/read the Anti VOX gain
14|19|BCD 2 bytes 00 00 ~ 02 55|Send/read LCD backlight brightness
15|01|00/01|Read noise or S-meter squelch status
15|02|BCD 2 bytes 00 00 ~ 02 55|Read S-meter level
15|05|00/01|Read various squelch (tone squelch etc) status
15|07|00/01|Read the OVF status
15|11|BCD 2 bytes 00 00 ~ 02 55|Read the Po meter level
15|12|BCD 2 bytes 00 00 ~ 02 55|Read SWR meter level
15|13|BCD 2 bytes 00 00 ~ 02 55|Read ALC meter level
15|14|BCD 2 bytes 00 00 ~ 02 55|Read COMP meter level
15|15|BCD 2 bytes 00 00 ~ 02 55|Read Vd meter level
15|16|BCD 2 bytes 00 00 ~ 02 55|Read Id meter level
16|02|00,01,02|Preamp
16|12|01 ~ 03|Send/read the AGC time constant
16|22|00/01|Send/read the Noise blanker
16|32|00,01,02,03|Audio peak filter
16|40|00/01|Send/read the Noise reduction
16|41|00/01|Send/read the Auto Notch function
16|42|00/01|Send/read the Repeater tone
16|43|00/01|Send/read the Tone squelch
16|44|00/01|Send/read the Speech compressor
16|45|00/01|Send/read the Monitor [MONI] function
16|46|00/01|Send/read the VOX function
16|47|00,01,02|BK-IN function
16|48|00/01|Send/read the Manual Notch function
16|4E|00/01|Send/read the DIGI-SEL function
16|4F|00/01|Send/read the Twin peak filter
16|50|00/01|Send/read the Dial Lock function
16|53|00/01|Send/read the ANT-RX I/O
16|56|00/01|Send/read DSP IF filter type
16|57|00 ~ 02|Send/read the Manual Notch width
16|58|00 ~ 02|Send/read SSB transmit bandwidth
16|5E|00/01|MAIN/SUB Tracking function
16|65|00/01|Set the IP Plus function
16|66|00/01|Send/read the TX Inhibit function
16|67|00/01|Send/read the DPD function
17||ASCII text up to 30 chars (see p.17)|Send CW messages
18|00||Turn OFF the transceiver
18|01||Turn ON the transceiver
19|00||Read the transceiver ID
1A|00|see p.19|Send/read memory contents
1A|01|see p.18|Send/read band stacking register contents
1A|02|see p.20|Send/read memory keyer contents
1A|03|see p.20|Send/read the selected IF filter width
1A|04|see p.20|Send/read the selected AGC time constant
1A|05 00 01|See p. 20.|[SET > Tone Control/TBW] RX > SSB > Send/read RX HPF/LPF settings
1A|05 00 02|BCD 1 byte 00 ~ 10|[SET > Tone Control/TBW] RX > SSB > Send/read RX Tone (Bass) level
1A|05 00 03|BCD 1 byte 00 ~ 10|[SET > Tone Control/TBW] RX > SSB > Send/read RX Tone (Treble) level
1A|05 00 04|See p. 20.|[SET > Tone Control/TBW] RX > AM > Send/read RX HPF/LPF settings
1A|05 00 05|BCD 1 byte 00 ~ 10|[SET > Tone Control/TBW] RX > AM > Send/read RX Tone (Bass) level
1A|05 00 06|BCD 1 byte 00 ~ 10|[SET > Tone Control/TBW] RX > AM > Send/read RX Tone (Treble) level
1A|05 00 07|See p. 20.|[SET > Tone Control/TBW] RX > FM > Send/read RX HPF/LPF settings
1A|05 00 08|BCD 1 byte 00 ~ 10|[SET > Tone Control/TBW] RX > FM > Send/read RX Tone (Bass) level
1A|05 00 09|BCD 1 byte 00 ~ 10|[SET > Tone Control/TBW] RX > FM > Send/read RX Tone (Treble) level
1A|05 00 10|See p. 20.|[SET > Tone Control/TBW] RX > CW > Send/read RX HPF/LPF settings
1A|05 00 11|See p. 20.|[SET > Tone Control/TBW] RX > RTTY > Send/read RX HPF/LPF settings
1A|05 00 12|See p. 20.|[SET > Tone Control/TBW] RX > PSK > Send/read RX HPF/LPF settings
1A|05 00 13|BCD 1 byte 00 ~ 10|[SET > Tone Control/TBW] TX > SSB > Send/read TX Tone (Bass) level
1A|05 00 14|BCD 1 byte 00 ~ 10|[SET > Tone Control/TBW] TX > SSB > Send/read TX Tone (Treble) level
1A|05 00 15|See p. 20.|[SET > Tone Control/TBW] TX > SSB > Send/read TX bandwidth for wide
1A|05 00 16|See p. 20.|[SET > Tone Control/TBW] TX > SSB > Send/read TX bandwidth for mid
1A|05 00 17|See p. 20.|[SET > Tone Control/TBW] TX > SSB > Send/read TX bandwidth for narrow
1A|05 00 18|See p. 20.|[SET > Tone Control/TBW] TX > SSB-D > Send/read TX bandwidth
1A|05 00 19|BCD 1 byte 00 ~ 10|[SET > Tone Control/TBW] TX > AM > Send/read TX Tone (Bass) level
1A|05 00 20|BCD 1 byte 00 ~ 10|[SET > Tone Control/TBW] TX > AM > Send/read TX Tone (Treble) level
1A|05 00 21|BCD 1 byte 00 ~ 10|[SET > Tone Control/TBW] TX > FM > Send/read TX Tone (Bass) level
1A|05 00 22|BCD 1 byte 00 ~ 10|[SET > Tone Control/TBW] TX > FM > Send/read TX Tone (Treble) level
1A|05 00 23|BCD 2 bytes 00 00 ~ 02 55|[SET > Function] Send/read the Beep Level setting
1A|05 00 24|BCD 1 byte 00/01|[SET > Function] Send/read the Beep Level Limit setting
1A|05 00 25|BCD 1 byte 00/01|[SET > Function] Send/read the Beep (Confirmation) setting
1A|05 00 26|BCD 1 byte 00 ~ 03|[SET > Function] Send/read the Band Edge Beep setting
1A|05 00 27|BCD 2 bytes 00 50 ~ 02 00|[SET > Function] Function > Beep Sound (MAIN)
1A|05 00 28|BCD 2 bytes 00 50 ~ 02 00|[SET > Function] Function > Beep Sound (SUB)
1A|05 00 29|BCD 1 byte 00/01|[SET > Function] Send/read the Speaker MAIN/SUB Mix
1A|05 00 30|BCD 1 byte 00 ~ 02|[SET > Function] RF/SQL Control
1A|05 00 31|BCD 1 byte 00/01|[SET > Function] Send/read the Cancel CI-V Remote Set Levels
1A|05 00 32|BCD 2 bytes 00 02 ~ 02 00|[SET > Function] TX Power Limit > 1.8M
1A|05 00 33|BCD 2 bytes 00 02 ~ 02 00|[SET > Function] TX Power Limit > 1.8M (DATA)
1A|05 00 34|BCD 2 bytes 00 02 ~ 02 00|[SET > Function] TX Power Limit > 3.5M
1A|05 00 35|BCD 2 bytes 00 02 ~ 02 00|[SET > Function] TX Power Limit > 3.5M (DATA)
1A|05 00 36|BCD 2 bytes 00 02 ~ 02 00|[SET > Function] TX Power Limit > 5M
1A|05 00 37|BCD 2 bytes 00 02 ~ 02 00|[SET > Function] TX Power Limit > 5M (DATA)
1A|05 00 38|BCD 2 bytes 00 02 ~ 02 00|[SET > Function] TX Power Limit > 7M
1A|05 00 39|BCD 2 bytes 00 02 ~ 02 00|[SET > Function] TX Power Limit > 7M (DATA)
1A|05 00 40|BCD 2 bytes 00 02 ~ 02 00|[SET > Function] TX Power Limit > 10M
1A|05 00 41|BCD 2 bytes 00 02 ~ 02 00|[SET > Function] TX Power Limit > 10M (DATA)
1A|05 00 42|BCD 2 bytes 00 02 ~ 02 00|[SET > Function] TX Power Limit > 14M
1A|05 00 43|BCD 2 bytes 00 02 ~ 02 00|[SET > Function] TX Power Limit > 14M (DATA)
1A|05 00 44|BCD 2 bytes 00 02 ~ 02 00|[SET > Function] TX Power Limit > 18M
1A|05 00 45|BCD 2 bytes 00 02 ~ 02 00|[SET > Function] TX Power Limit > 18M (DATA)
1A|05 00 46|BCD 2 bytes 00 02 ~ 02 00|[SET > Function] TX Power Limit > 21M
1A|05 00 47|BCD 2 bytes 00 02 ~ 02 00|[SET > Function] TX Power Limit > 21M (DATA)
1A|05 00 48|BCD 2 bytes 00 02 ~ 02 00|[SET > Function] TX Power Limit > 24M
1A|05 00 49|BCD 2 bytes 00 02 ~ 02 00|[SET > Function] TX Power Limit > 24M (DATA)
1A|05 00 50|BCD 2 bytes 00 02 ~ 02 00|[SET > Function] TX Power Limit > 28M
1A|05 00 51|BCD 2 bytes 00 02 ~ 02 00|[SET > Function] TX Power Limit > 28M (DATA)
1A|05 00 52|BCD 2 bytes 00 02 ~ 02 00|[SET > Function] TX Power Limit > 50M
1A|05 00 53|BCD 2 bytes 00 02 ~ 02 00|[SET > Function] TX Power Limit > 50M (DATA)
1A|05 00 54|BCD 1 byte 00 ~ 05|[SET > Function] TX Delay > HF
1A|05 00 55|BCD 1 byte 00 ~ 05|[SET > Function] TX Delay > 50M
1A|05 00 56|BCD 1 byte 00 ~ 05|[SET > Function] Send/read the Time-Out Timer (CI-V) setting
1A|05 00 57|BCD 1 byte 00/01|[SET > Function] Send/read the Quick Dualwatch
1A|05 00 58|BCD 1 byte 00/01|[SET > Function] SPLIT > Send/read the Quick SPLIT setting
1A|05 00 59|BCD 1 byte 00/01|[SET > Function] SPLIT > Display Keypad on Quick SPLIT
1A|05 00 60|See p. 21.|[SET > Function] SPLIT > FM SPLIT Offset (HF)
1A|05 00 61|See p. 21.|[SET > Function] SPLIT > FM SPLIT Offset (50M)
1A|05 00 62|BCD 1 byte 00/01|[SET > Function] SPLIT > Send/read the SPLIT LOCK setting
1A|05 00 63|BCD 1 byte 00/01|[SET > Function] Tuner > Send/read the PTT Start setting
1A|05 00 64|BCD 1 byte 00/01|[SET > Function] Send/read the Transverter Function setting
1A|05 00 65|See p. 21.|[SET > Function] Send/read the Transverter Offset setting
1A|05 00 66|BCD 1 byte 00/01|[SET > Function] Send/read the IC-PW2 Dual Connection Mode setting
1A|05 00 67|BCD 1 byte 00 ~ 02|[SET > Function] Send/read the RTTY Mark Frequency setting
1A|05 00 68|BCD 1 byte 00 ~ 02|[SET > Function] Send/read the RTTY Shift Width setting
1A|05 00 69|BCD 1 byte 00/01|[SET > Function] Send/read the RTTY Keying Polarity setting
1A|05 00 70|BCD 1 byte 00 ~ 02|[SET > Function] Send/read the PSK Tone Frequency setting
1A|05 00 71|BCD 1 byte 00/01|[SET > Function] SPEECH > Send/read the SPEECH Language setting
1A|05 00 72|BCD 1 byte 00/01|[SET > Function] SPEECH > Send/read the SPEECH Speed setting
1A|05 00 73|BCD 1 byte 00/01|[SET > Function] SPEECH > Send/read the S-Level SPEECH setting
1A|05 00 74|BCD 1 byte 00/01|[SET > Function] SPEECH > Send/read the MODE SPEECH setting
1A|05 00 75|BCD 2 bytes 00 00 ~ 02 55|[SET > Function] SPEECH > Send/read the SPEECH Level setting
1A|05 00 76|BCD 1 byte 00/01|[SET > Function] Send/read the [SPEECH/LOCK] Switch setting
1A|05 00 77|BCD 1 byte 00/01|[SET > Function] Send/read the Lock Function setting
1A|05 00 78|BCD 1 byte 00/01|[SET > Function] Send/read the Memo Pad Quantity setting
1A|05 00 79|BCD 1 byte 00 ~ 02|[SET > Function] Send/read the MAIN DIAL Auto TS setting
1A|05 00 80|BCD 1 byte 00/01|[SET > Function] Send/read the MAIN DIAL Select
1A|05 00 81|BCD 1 byte 00/01|[SET > Function] Send/read the MIC Up/Down Speed setting
1A|05 00 82|BCD 1 byte 00/01|[SET > Function] Send/read the Quick RIT/TX Clear setting
1A|05 00 83|BCD 1 byte 00 ~ 02|[SET > Function] Send/read the [NOTCH] Switch (SSB) setting
1A|05 00 84|BCD 1 byte 00 ~ 02|[SET > Function] Send/read the [NOTCH] Switch (AM) setting
1A|05 00 85|BCD 1 byte 00/01|[SET > Function] Send/read the FILTER Screen MAIN/SUB Select setting
1A|05 00 86|BCD 1 byte 00/01|[SET > Function] Send/read the SSB/CW Synchronous Tuning setting
1A|05 00 87|BCD 1 byte 00/01|[SET > Function] Send/read the CW Normal Side setting
1A|05 00 88|See p. 21.|[SET > Function > Front Key Customize] Send/read the [VOX/BK-IN] Key setting
1A|05 00 89|See p. 21.|[SET > Function > Front Key Customize] Send/read the [AUTOTUNE] Key setting
1A|05 00 90|See p. 21.|[SET > Function > MIC Key Customize] Send/read the [UP] Key setting
1A|05 00 91|See p. 21.|[SET > Function > MIC Key Customize] Send/read the [DN] Key setting
1A|05 00 92|BCD 1 byte 00/01|[SET > Function] Send/read the Screen Keyboard Type setting
1A|05 00 93|BCD 1 byte 00 ~ 02|[SET > Function] Send/read the Screen Full Keyboard Layout setting
1A|05 00 94|BCD 1 byte 00/01|[SET > Function] Send/read the Screen Capture [POWER] Switch setting
1A|05 00 95|BCD 1 byte 00/01|[SET > Function] Send/read the Screen Capture Keyboard [Print Screen] setting
1A|05 00 96|BCD 1 byte 00/01|[SET > Function] Send/read the Screen Capture Storage Media setting
1A|05 00 97|BCD 1 byte 00/01|[SET > Function] Send/read the Screen Capture File Type setting
1A|05 00 98|BCD 1 byte 00/01|[SET > Function] Send/read the Calibration Marker setting
1A|05 00 99|BCD 2 bytes 00 00 ~ 05 11|[SET > Function] Send/read the REF Adjust setting
1A|05 01 00|BCD 1 byte 00 ~ 30|[SET > Connectors] Phones > Send/read the Level setting
1A|05 01 01|BCD 1 byte 00/01|[SET > Connectors] Phones > Send/read the L/R Mix setting
1A|05 01 02|BCD 2 bytes 00 00 ~ 02 55|[SET > Connectors] Phones > Send/read the Phone L/R Mix Level setting
1A|05 01 03|BCD 1 byte 00/01|[SET > Connectors] USB AF/IF Output > Send/read the Output Select setting
1A|05 01 04|BCD 1 byte 00/01|[SET > Connectors] USB AF/IF Output > Send/read the AF/IF XFC Output
1A|05 01 05|BCD 2 bytes 00 00 ~ 02 55|[SET > Connectors] USB AF/IF Output > Send/read the AF Output Level setting
1A|05 01 06|BCD 1 byte 00/01|[SET > Connectors] USB AF/IF Output > Send/read the AF SQL setting
1A|05 01 07|BCD 1 byte 00/01|[SET > Connectors] USB AF/IF Output > Send/read the AF Beep/Speech... Output setting
1A|05 01 08|BCD 2 bytes 00 00 ~ 02 55|[SET > Connectors] USB AF/IF Output > Send/read the IF Output Level setting
1A|05 01 09|BCD 1 byte 00/01|[SET > Connectors] LINE-OUT AF/IF Output > Send/read the AF Output Select setting
1A|05 01 10|BCD 1 byte 00/01|[SET > Connectors] LINE-OUT AF/IF Output > Send/read the Output Select setting
1A|05 01 11|BCD 1 byte 00/01|[SET > Connectors] LINE-OUT AF/IF Output > Send/read the AF/IF XFC Output
1A|05 01 12|BCD 2 bytes 00 00 ~ 02 55|[SET > Connectors] LINE-OUT AF/IF Output > Send/read the AF Output Level setting
1A|05 01 13|BCD 1 byte 00/01|[SET > Connectors] LINE-OUT AF/IF Output > Send/read the AF SQL setting
1A|05 01 14|BCD 1 byte 00/01|[SET > Connectors] LINE-OUT AF/IF Output > Send/read the AF Beep/Speech... Output setting
1A|05 01 15|BCD 2 bytes 00 00 ~ 02 55|[SET > Connectors] LINE-OUT AF/IF Output > Send/read the IF Output Level setting
1A|05 01 16|BCD 1 byte 00/01|[SET > Connectors] ACC AF/IF Output > Send/read the AF/SQL Output Select setting
1A|05 01 17|BCD 1 byte 00/01|[SET > Connectors] ACC AF/IF Output > Send/read the Output Select setting
1A|05 01 18|BCD 1 byte 00/01|[SET > Connectors] ACC AF/IF Output > Send/read the AF/IF XFC Output
1A|05 01 19|BCD 2 bytes 00 00 ~ 02 55|[SET > Connectors] ACC AF/IF Output > Send/read the AF Output Level setting
1A|05 01 20|BCD 1 byte 00/01|[SET > Connectors] ACC AF/IF Output > Send/read the AF SQL setting
1A|05 01 21|BCD 1 byte 00/01|[SET > Connectors] ACC AF/IF Output > Send/read the AF Beep/Speech... Output setting
1A|05 01 22|BCD 2 bytes 00 00 ~ 02 55|[SET > Connectors] ACC AF/IF Output > Send/read the IF Output Level setting
1A|05 01 23|BCD 1 byte 00/01|[SET > Connectors] LAN AF/IF Output > Send/read the Output Select setting
1A|05 01 24|BCD 1 byte 00/01|[SET > Connectors] LAN AF/IF Output > Send/read the AF SQL setting
1A|05 01 25|BCD 2 bytes 00 00 ~ 02 55|[SET > Connectors] MOD Input > Send/read the USB MOD Level setting
1A|05 01 26|BCD 2 bytes 00 00 ~ 02 55|[SET > Connectors] MOD Input > Send/read the LINE-IN MOD Level setting
1A|05 01 27|BCD 2 bytes 00 00 ~ 02 55|[SET > Connectors] MOD Input > Send/read the ACC MOD Level setting
1A|05 01 28|BCD 2 bytes 00 00 ~ 02 55|[SET > Connectors] MOD Input > Send/read the LAN MOD Level setting
1A|05 01 29|BCD 1 byte 00 ~ 09|[SET > Connectors] MOD Input > Send/read the DATA OFF MOD setting
1A|05 01 30|BCD 1 byte 00 ~ 09|[SET > Connectors] MOD Input > Send/read the DATA1 MOD setting
1A|05 01 31|BCD 1 byte 00 ~ 09|[SET > Connectors] MOD Input > Send/read the DATA2 MOD setting
1A|05 01 32|BCD 1 byte 00 ~ 09|[SET > Connectors] MOD Input > Send/read the DATA3 MOD setting
1A|05 01 33|BCD 1 byte 00 ~ 04|[SET > Connectors] USB SEND/Keying > Send/read the USB SEND setting
1A|05 01 34|BCD 1 byte 00 ~ 04|[SET > Connectors] USB SEND/Keying > Send/read the USB Keying (CW) setting
1A|05 01 35|BCD 1 byte 00 ~ 04|[SET > Connectors] USB SEND/Keying > Send/read the USB Keying (RTTY) setting
1A|05 01 36|BCD 1 byte 00/01|[SET > Connectors] External Keypad > Send/read the VOICE setting
1A|05 01 37|BCD 1 byte 00/01|[SET > Connectors] External Keypad > Send/read the KEYER setting
1A|05 01 38|BCD 1 byte 00/01|[SET > Connectors] External Keypad > Send/read the RTTY setting
1A|05 01 39|BCD 1 byte 00/01|[SET > Connectors] External Keypad > Send/read the PSK setting
1A|05 01 40|BCD 1 byte 00/01|[SET > Connectors] Keyboard/Mouse > Send/read the Keyboard [F1]-[F8]
1A|05 01 41|BCD 1 byte 00/01|[SET > Connectors] Keyboard/Mouse > Send/read the Keyboard [F1]-[F8]
1A|05 01 42|BCD 1 byte 00 ~ 10|[SET > Connectors] Keyboard/Mouse > Send/read the Keyboard Type setting
1A|05 01 43|BCD 2 bytes 00 10 ~ 01 00|[SET > Connectors] Keyboard/Mouse > Send/read the Keyboard Repeat Delay setting
1A|05 01 44|BCD 1 byte 00 ~ 31|[SET > Connectors] Keyboard/Mouse > Send/read the Keyboard Repeat Rate setting
1A|05 01 45|BCD 1 byte 00 ~ 02|[SET > Connectors] Keyboard/Mouse > Send/read the Mouse Pointer Speed setting
1A|05 01 46|BCD 1 byte 00/01|[SET > Connectors] Keyboard/Mouse > Send/read the Mouse Pointer Acceleration setting
1A|05 01 47|BCD 1 byte 00/01|[SET > Connectors] USB DIAL > Send/read the USB DIAL Select setting
1A|05 01 48|BCD 1 byte 00 ~ 02|[SET > Connectors] USB DIAL > Send/read the USB DIAL Auto TS setting
1A|05 01 49|BCD 1 byte 00/01|[SET > Connectors] USB DIAL > Send/read the USB DIAL [TRANSMIT] Switch setting
1A|05 01 50|BCD 1 byte 00/01|[SET > Connectors] CI-V > Send/read the CI-V Transceive Switch setting
1A|05 01 51|BCD 2 bytes 00 00 ~ 02 23|[SET > Connectors] CI-V > Send/read the CI-V USB/LAN REMOTE Transceive Address setting
1A|05 01 52|BCD 1 byte 00/01|[SET > Connectors] CI-V > Send/read the CI-V Output (for ANT) setting
1A|05 01 53|BCD 1 byte 00/01|[SET > Connectors] CI-V > Send/read the CI-V USB (A) Echo Back setting
1A|05 01 54|BCD 1 byte 00/01|[SET > Connectors] CI-V > Send/read the CI-V USB (B) Echo Back setting
1A|05 01 55|BCD 1 byte 00/01|[SET > Connectors] CI-V > Send/read the USB (B) Function setting
1A|05 01 56|BCD 1 byte 00/01|[SET > Connectors] Send/read the SEND Relay Type setting
1A|05 01 57|BCD 1 byte 00 ~ 02|[SET > Connectors] Send/read the ACC BAND Voltage Output setting
1A|05 01 58|BCD 1 byte 00/01|[SET > Connectors] Send/read the MIC Input DC Bias setting
1A|05 01 59|BCD 1 byte 00/01|[SET > Connectors] Send/read the PTT Port Function setting
1A|05 01 60|BCD 1 byte 00/01|[SET > Connectors] Send/read the REF IN setting
1A|05 01 61|BCD 1 byte 00/01|[SET > Network] Send/read the DHCP (valid after restart) setting
1A|05 01 62|See p. 21.|[SET > Network] Send/read the IP Address (LAN)
1A|05 01 63|See p. 21.|[SET > Network] Read the IP address obtained through the DHCP (valid after restart) setting. L When the DHCP (valid after restart) is set to OFF, the manually set IP address (LAN) is returned.
1A|05 01 64|See p. 21.|[SET > Network] Send/read the IP Address (Controller)
1A|05 01 65|See p. 21.|[SET > Network] Send/read the IP Address (RF Deck)
1A|05 01 66|BCD 1 byte 01 ~ 30|[SET > Network] Send/read the Subnet Mask (valid after restart) setting
1A|05 01 67|See p. 21.|[SET > Network] Send/read the Default Gateway
1A|05 01 68|See p. 21.|[SET > Network] Send/read the Primary DNS Server
1A|05 01 69|See p. 21.|[SET > Network] Send/read the Secondary DNS Server (valid after restart) setting
1A|05 01 70|BCD 1 byte 00 ~ 05|[SET > Network > Controller Connection (via LAN) > Audio Buffer] Send/read the Wired LAN (in Same Segment) setting
1A|05 01 71|BCD 1 byte 00/01|[SET > Network > Controller Connection (via LAN) > RF Deck side Settings] Send/read the RF deck's static IP address setting
1A|05 01 72|See p. 21.|[SET > Network > Controller Connection (via LAN) > RF Deck side Settings] Send/read the IP Address (RF Deck)
1A|05 01 73|BCD 1 byte 01 ~ 30|[SET > Network > Controller Connection (via LAN) > RF Deck side Settings] Send/read the Subnet Mask (RF Deck) (valid after restart) setting
1A|05 01 74|See p. 21.|[SET > Network > Controller Connection (via LAN) > RF Deck side Settings] Send/read the Default Gateway (RF Deck) (valid after restart) setting
1A|05 01 75|BCD 1 byte 00 ~ 04|[SET > Network > Controller Connection (via LAN) > Audio Buffer] Send/read the Wired LAN (from Different Segment) setting
1A|05 01 76|See p. 19.|[SET > Network] Send/read the Network Name (valid after restart) setting
1A|05 01 77|BCD 1 byte 00/01|[SET > Network > Remote Settings] Send/read the Network Control
1A|05 01 78|BCD 1 byte 00/01|[SET > Network > Remote Settings] Send/read the Shutdown Function setting
1A|05 01 79|BCD 3 bytes 00 00 01 ~ 06 55 35|[SET > Network > Remote Settings] Send/read the Control Port (UDP)
1A|05 01 80|BCD 3 bytes 00 00 01 ~ 06 55 35|[SET > Network > Remote Settings] Send/read the Serial Port (UDP)
1A|05 01 81|BCD 3 bytes 00 00 01 ~ 06 55 35|[SET > Network > Remote Settings] Send/read the Audio Port (UDP)
1A|05 01 82|BCD 1 byte 00/01|[SET > Network > Remote Settings] Send/read the Internet Access Line
1A|05 01 83|See p. 19.|[SET > Network > Remote Settings] Send/read the Network Radio Name setting
1A|05 01 84|BCD 2 bytes 00 00 ~ 02 55|[SET > Display] Send/read the LCD Backlight setting
1A|05 01 85|BCD 2 bytes 00 00 ~ 02 55|[SET > Display] Send/read the LED Bright setting
1A|05 01 86|BCD 1 byte 00/01|[SET > Display] Send/read the Display Font setting
1A|05 01 87|BCD 1 byte 00 ~ 02|[SET > Display] Send/read the Meter Response
1A|05 01 88|BCD 1 byte 00 ~ 02|[SET > Display] Send/read the Meter Type (Normal Screen) setting
1A|05 01 89|BCD 1 byte 00/01|[SET > Display] Send/read the Meter Type (Expand Screen) setting
1A|05 01 90|BCD 1 byte 00/01|[SET > Display] Send/read the Meter Peak Hold
1A|05 01 91|BCD 1 byte 00/01|[SET > Display] Send/read the Memory Name setting
1A|05 01 92|BCD 1 byte 00/01|[SET > Display > Filter Effect Screen] Send/read the Waveform Type setting
1A|05 01 93|See p. 22.|[SET > Display > Filter Effect Screen] Send/read the Waveform Color setting 11
1A|05 01 94|BCD 1 byte 00 ~ 03|[SET > Display] Send/read the Screen Saver setting
1A|05 01 95|BCD 1 byte 00/01|[SET > Display] Send/read the External Display setting
1A|05 01 96|BCD 1 byte 00/01|[SET > Display] Send/read the External Display Resolution setting
1A|05 01 97|BCD 1 byte 00/01|[SET > Display] Send/read the Opening Message setting
1A|05 01 98|See p. 19.|[SET > Display] Send/read the My Call setting
1A|05 01 99|BCD 1 byte 00/01|[SET > Display] Send/read the Display Language setting
1A|05 02 00|BCD 4 bytes 20 20 01 01 ~ 20 99 12 31|[SET > Time Set] Date/Time > Send/read the Date setting
1A|05 02 01|BCD 2 bytes 00 00 ~ 23 59|[SET > Time Set] Date/Time > Send/read the Time setting
1A|05 02 02|BCD 1 byte 00/01|[SET > Time Set] Date/Time > Send/read the NTP Function setting
1A|05 02 03|See p. 19.|[SET > Time Set] Date/Time > Send/read the NTP Server Address setting
1A|05 02 04|See p. 22.|[SET > Time Set] Send/read the UTC Offset setting
1A|05 02 05|BCD 1 byte 00/01|[SET > Time Set] Send/read the CLOCK2 Function setting
1A|05 02 06|See p. 22.|[SET > Time Set] Send/read the CLOCK2 UTC Offset setting
1A|05 02 07|See p. 19.|[SET > Time Set] Send/read the CLOCK2 Name setting
1A|05 02 08|BCD 1 byte 00/01|[SCOPE SET: SCOPE > EXPD/SET (Touch for 1 second)] Send/read the Scope during Tx
1A|05 02 09|BCD 1 byte 00 ~ 02|[SCOPE SET: SCOPE > EXPD/SET (Touch for 1 second)] Send/read the Max Hold setting
1A|05 02 10|BCD 1 byte 00 ~ 02|[SCOPE SET: SCOPE > EXPD/SET (Touch for 1 second)] Send/read the CENTER Type Display setting
1A|05 02 11|BCD 1 byte 00/01|[SCOPE SET: SCOPE > EXPD/SET (Touch for 1 second)] Send/read the Marker Position
1A|05 02 12|BCD 1 byte 00 ~ 02|[SCOPE SET: SCOPE > EXPD/SET (Touch for 1 second)] Send/read the Averaging setting
1A|05 02 13|BCD 1 byte 00/01|[SCOPE SET: SCOPE > EXPD/SET (Touch for 1 second)] Send/read the Waveform Type setting
1A|05 02 14|See p. 22.|[SCOPE SET: SCOPE > EXPD/SET (Touch for 1 second)] Send/read the Waveform Color
1A|05 02 15|See p. 22.|[SCOPE SET: SCOPE > EXPD/SET (Touch for 1 second)] Send/read the Waveform Color
1A|05 02 16|See p. 22.|[SCOPE SET: SCOPE > EXPD/SET (Touch for 1 second)] Send/read the Waveform Color
1A|05 02 17|BCD 1 byte 00/01|[SCOPE SET: SCOPE > EXPD/SET (Touch for 1 second)] Send/read the Waterfall Display setting
1A|05 02 18|BCD 1 byte 00 ~ 02|[SCOPE SET: SCOPE > EXPD/SET (Touch for 1 second)] Send/read the Waterfall Speed setting
1A|05 02 19|BCD 1 byte 00 ~ 02|[SCOPE SET: SCOPE > EXPD/SET (Touch for 1 second)] Send/read the Waterfall Size
1A|05 02 20|BCD 1 byte 00 ~ 09|[SCOPE SET: SCOPE > EXPD/SET (Touch for 1 second)] Send/read the Waterfall Peak Color Level setting
1A|05 02 21|BCD 1 byte 00/01|[SCOPE SET: SCOPE > EXPD/SET (Touch for 1 second)] Send/read the Waterfall Marker Auto-hide setting
1A|05 02 22|BCD 1 byte 00/01|[SCOPE SET: SCOPE > EXPD/SET (Touch for 1 second)] Send/read the Dual Scope Type setting
1A|05 02 23|BCD 1 byte 00/01|[SCOPE SET: SCOPE > EXPD/SET (Touch for 1 second)] Send/read the Dual Scope Auto Select setting
1A|05 02 24|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 0.03 - 1.60 > Send/read the No.1 setting
1A|05 02 25|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 0.03 - 1.60 > Send/read the No.2 setting
1A|05 02 26|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 0.03 - 1.60 > Send/read the No.3 setting
1A|05 02 27|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 0.03 - 1.60 > Send/read the No.4 setting
1A|05 02 28|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 1.60 - 2.00 > Send/read the No.1 setting
1A|05 02 29|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 1.60 - 2.00 > Send/read the No.2 setting
1A|05 02 30|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 1.60 - 2.00 > Send/read the No.3 setting
1A|05 02 31|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 1.60 - 2.00 > Send/read the No.4 setting
1A|05 02 32|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 2.00 - 6.00 > Send/read the No.1 setting
1A|05 02 33|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 2.00 - 6.00 > Send/read the No.2 setting
1A|05 02 34|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 2.00 - 6.00 > Send/read the No.3 setting
1A|05 02 35|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 2.00 - 6.00 > Send/read the No.4 setting
1A|05 02 36|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 6.00 - 8.00 > Send/read the No.1 setting
1A|05 02 37|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 6.00 - 8.00 > Send/read the No.2 setting
1A|05 02 38|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 6.00 - 8.00 > Send/read the No.3 setting
1A|05 02 39|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 6.00 - 8.00 > Send/read the No.4 setting 12
1A|05 02 40|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 8.00 - 11.00 > Send/read the No.1 setting
1A|05 02 41|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 8.00 - 11.00 > Send/read the No.2 setting
1A|05 02 42|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 8.00 - 11.00 > Send/read the No.3 setting
1A|05 02 43|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 8.00 - 11.00 > Send/read the No.4 setting
1A|05 02 44|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 11.00 - 15.00 > Send/read the No.1 setting
1A|05 02 45|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 11.00 - 15.00 > Send/read the No.2 setting
1A|05 02 46|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 11.00 - 15.00 > Send/read the No.3 setting
1A|05 02 47|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 11.00 - 15.00 > Send/read the No.4 setting
1A|05 02 48|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 15.00 - 20.00 > Send/read the No.1 setting
1A|05 02 49|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 15.00 - 20.00 > Send/read the No.2 setting
1A|05 02 50|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 15.00 - 20.00 > Send/read the No.3 setting
1A|05 02 51|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 15.00 - 20.00 > Send/read the No.4 setting
1A|05 02 52|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 20.00 - 22.00 > Send/read the No.1 setting
1A|05 02 53|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 20.00 - 22.00 > Send/read the No.2 setting
1A|05 02 54|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 20.00 - 22.00 > Send/read the No.3 setting
1A|05 02 55|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 20.00 - 22.00 > Send/read the No.4 setting
1A|05 02 56|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 22.00 - 26.00 > Send/read the No.1 setting
1A|05 02 57|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 22.00 - 26.00 > Send/read the No.2 setting
1A|05 02 58|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 22.00 - 26.00 > Send/read the No.3 setting
1A|05 02 59|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 22.00 - 26.00 > Send/read the No.4 setting
1A|05 02 60|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 26.00 - 30.00 > Send/read the No.1 setting
1A|05 02 61|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 26.00 - 30.00 > Send/read the No.2 setting
1A|05 02 62|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 26.00 - 30.00 > Send/read the No.3 setting
1A|05 02 63|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 26.00 - 30.00 > Send/read the No.4 setting
1A|05 02 64|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 30.00 - 45.00 > Send/read the No.1 setting
1A|05 02 65|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 30.00 - 45.00 > Send/read the No.2 setting
1A|05 02 66|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 30.00 - 45.00 > Send/read the No.3 setting
1A|05 02 67|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 30.00 - 45.00 > Send/read the No.4 setting
1A|05 02 68|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 45.00 - 60.00 > Send/read the No.1 setting
1A|05 02 69|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 45.00 - 60.00 > Send/read the No.2 setting
1A|05 02 70|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 45.00 - 60.00 > Send/read the No.3 setting
1A|05 02 71|See p. 22.|[SCOPE > EXPD/SET (Touch for 1 second) > Fixed Edges] 45.00 - 60.00 > Send/read the No.4 setting
1A|05 02 72|BCD 1 byte 00/01|[AUDIO SCOPE SET: AUDIO > SET] Send/read the FFT Scope Waveform Type setting
1A|05 02 73|See p. 22.|[AUDIO SCOPE SET: AUDIO > SET] Send/read the FFT Scope Waveform Color setting
1A|05 02 74|BCD 1 byte 00/01|[AUDIO SCOPE SET: AUDIO > SET] Send/read the FFT Scope Waterfall Display setting
1A|05 02 75|See p. 22.|[AUDIO SCOPE SET: AUDIO > SET] Send/read the Oscilloscope Waveform Color setting
1A|05 02 76|BCD 1 byte 00 ~ 04|[KEYER 001: KEYER > EDIT/SET > 001 SET] Send/read the Number Style setting
1A|05 02 77|BCD 1 byte 00/01|[KEYER 001: KEYER > EDIT/SET > 001 SET] Send/read the Count Up Trigger (M1) setting
1A|05 02 78|BCD 1 byte 00/01|[KEYER 001: KEYER > EDIT/SET > 001 SET] Send/read the Count Up Trigger (M2) setting
1A|05 02 79|BCD 1 byte 00/01|[KEYER 001: KEYER > EDIT/SET > 001 SET] Send/read the Count Up Trigger (M3) setting
1A|05 02 80|BCD 1 byte 00/01|[KEYER 001: KEYER > EDIT/SET > 001 SET] Send/read the Count Up Trigger (M4) setting
1A|05 02 81|BCD 1 byte 00/01|[KEYER 001: KEYER > EDIT/SET > 001 SET] Send/read the Count Up Trigger (M5) setting
1A|05 02 82|BCD 1 byte 00/01|[KEYER 001: KEYER > EDIT/SET > 001 SET] Send/read the Count Up Trigger (M6) setting
1A|05 02 83|BCD 1 byte 00/01|[KEYER 001: KEYER > EDIT/SET > 001 SET] Send/read the Count Up Trigger (M7) setting
1A|05 02 84|BCD 1 byte 00/01|[KEYER 001: KEYER > EDIT/SET > 001 SET] Send/read the Count Up Trigger (M8) setting
1A|05 02 85|BCD 2 bytes 00 01 ~ 99 99|[KEYER 001: KEYER > EDIT/SET > 001 SET] Send/read the Present Number setting
1A|05 02 86|BCD 2 bytes 00 00 ~ 02 55|[SET > CW-KEY Set] Send/read the Side Tone Level setting
1A|05 02 87|BCD 1 byte 00/01|[SET > CW-KEY Set] Send/read the Side Tone Level Limit setting
1A|05 02 88|BCD 1 byte 01 ~ 60|[SET > CW-KEY Set] Send/read the Keyer Repeat time setting
1A|05 02 89|BCD 1 byte 28 ~ 45|[SET > CW-KEY Set] Send/read the Dot/Dash Ratio setting
1A|05 02 90|BCD 1 byte 00 ~ 03|[SET > CW-KEY Set] Send/read the Rise Time setting
1A|05 02 91|BCD 1 byte 00/01|[SET > CW-KEY Set] Send/read the Paddle Polarity setting
1A|05 02 92|BCD 1 byte 00 ~ 02|[SET > CW-KEY Set] Send/read the Key Type setting
1A|05 02 93|BCD 1 byte 00/01|[SET > CW-KEY Set] Send/read the MIC Up/Down Keyer setting
1A|05 02 94|BCD 1 byte 00 ~ 03|[RTTY DECODE SET: DECODE > <MENU1> > SET] Send/read the FFT Scope Averaging setting
1A|05 02 95|See p. 22.|[RTTY DECODE SET: DECODE > <MENU1> > SET] Send/read the FFT Scope Waveform Color setting
1A|05 02 96|BCD 1 byte 00/01|[RTTY DECODE SET: DECODE > <MENU1> > SET] Send/read the Decode USOS setting
1A|05 02 97|BCD 1 byte 00/01|[RTTY DECODE SET: DECODE > <MENU1> > SET] Send/read the Decode LineFeed Code setting
1A|05 02 98|BCD 1 byte 00 ~ 02|[RTTY DECODE SET: DECODE > <MENU1> > SET] Send/read the Diddle setting
1A|05 02 99|BCD 1 byte 00/01|[RTTY DECODE SET: DECODE > <MENU1> > SET] Send/read the TX USOS setting
1A|05 03 00|BCD 1 byte 00/01|[RTTY DECODE SET: DECODE > <MENU1> > SET] Send/read the TX Auto CR+LF by TX setting
1A|05 03 01|BCD 1 byte 00/01|[RTTY DECODE SET: DECODE > <MENU1> > SET] Send/read the Time Stamp setting
1A|05 03 02|BCD 1 byte 00/01|[RTTY DECODE SET: DECODE > <MENU1> > SET] Send/read the Time Stamp (Time) setting
1A|05 03 03|BCD 1 byte 00/01|[RTTY DECODE SET: DECODE > <MENU1> > SET] Send/read the Time Stamp
1A|05 03 04|See p. 22.|[RTTY DECODE SET: DECODE > <MENU1> > SET] Send/read the Font Color (Receive) setting
1A|05 03 05|See p. 22.|[RTTY DECODE SET: DECODE > <MENU1> > SET] Send/read the Font Color (Transmit) setting
1A|05 03 06|See p. 22.|[RTTY DECODE SET: DECODE > <MENU1> > SET] Send/read the Font Color
1A|05 03 07|See p. 22.|[RTTY DECODE SET: DECODE > <MENU1> > SET] Send/read the Font Color (TX Buffer) setting
1A|05 03 08|BCD 1 byte 00/01|[RTTY 001: DECODE > TX MEM > EDIT/SET > 001 SET] Send/read the Count Up Trigger
1A|05 03 09|BCD 1 byte 00/01|[RTTY 001: DECODE > TX MEM > EDIT/SET > 001 SET] Send/read the Count Up Trigger
1A|05 03 10|BCD 1 byte 00/01|[RTTY 001: DECODE > TX MEM > EDIT/SET > 001 SET] Send/read the Count Up Trigger
1A|05 03 11|BCD 1 byte 00/01|[RTTY 001: DECODE > TX MEM > EDIT/SET > 001 SET] Send/read the Count Up Trigger
1A|05 03 12|BCD 1 byte 00/01|[RTTY 001: DECODE > TX MEM > EDIT/SET > 001 SET] Send/read the Count Up Trigger
1A|05 03 13|BCD 1 byte 00/01|[RTTY 001: DECODE > TX MEM > EDIT/SET > 001 SET] Send/read the Count Up Trigger
1A|05 03 14|BCD 1 byte 00/01|[RTTY 001: DECODE > TX MEM > EDIT/SET > 001 SET] Send/read the Count Up Trigger
1A|05 03 15|BCD 1 byte 00/01|[RTTY 001: DECODE > TX MEM > EDIT/SET > 001 SET] Send/read the Count Up Trigger
1A|05 03 16|BCD 2 bytes 00 01 ~ 99 99|[RTTY 001: DECODE > TX MEM > EDIT/SET > 001 SET] Send/read the Present Number setting
1A|05 03 17|BCD 1 byte 00/01|[RTTY DECODE LOG: DECODE > <MENU1> > LOG] Send/read the Decode Log setting
1A|05 03 18|BCD 1 byte 00/01|[RTTY DECODE LOG: DECODE > <MENU1> > LOG] Send/read the File Type setting
1A|05 03 19|BCD 1 byte 00 ~ 03|[PSK DECODE SET: DECODE > <MENU1> > SET] Send/read the FFT Scope Averaging setting
1A|05 03 20|See p. 22.|[PSK DECODE SET: DECODE > <MENU1> > SET] Send/read the FFT Scope Waveform Color setting
1A|05 03 21|BCD 1 byte 00/01|[PSK DECODE SET: DECODE > <MENU1> > SET] Send/read the AFC Range setting
1A|05 03 22|BCD 1 byte 00/01|[PSK DECODE SET: DECODE > <MENU1> > SET] Send/read the Time Stamp setting
1A|05 03 23|BCD 1 byte 00/01|[PSK DECODE SET: DECODE > <MENU1> > SET] Send/read the Time Stamp (Time) setting
1A|05 03 24|BCD 1 byte 00/01|[PSK DECODE SET: DECODE > <MENU1> > SET] Send/read the Time Stamp
1A|05 03 25|See p. 22.|[PSK DECODE SET: DECODE > <MENU1> > SET] Send/read the Font Color (Receive) setting
1A|05 03 26|See p. 22.|[PSK DECODE SET: DECODE > <MENU1> > SET] Send/read the Font Color (Transmit) setting
1A|05 03 27|See p. 22.|[PSK DECODE SET: DECODE > <MENU1> > SET] Send/read the Font Color
1A|05 03 28|See p. 22.|[PSK DECODE SET: DECODE > <MENU1> > SET] Send/read the Font Color (TX Buffer) setting 14
1A|05 03 29|BCD 1 byte 00/01|[PSK DECODE LOG: DECODE > <MENU1> > LOG] Send/read the Decode Log setting
1A|05 03 30|BCD 1 byte 00/01|[PSK DECODE LOG: DECODE > <MENU1> > LOG] Log Set > Send/read the File Type setting
1A|05 03 31|BCD 1 byte 00/01|[SCAN SET: SCAN > SET] Send/read the SCAN Speed setting
1A|05 03 32|BCD 1 byte 00/01|[SCAN SET: SCAN > SET] Send/read the SCAN Resume setting
1A|05 03 33|BCD 2 bytes 00 00 ~ 02 55|[VOICE TX: VOICE > TX LEVEL] Send/read the TX LEVEL setting
1A|05 03 34|BCD 1 byte 00/01|[VOICE TX SET: VOICE > REC/SET > SET] Send/read the Auto Monitor setting
1A|05 03 35|BCD 1 byte 01 ~ 15|[VOICE TX SET: VOICE > REC/SET > SET] Send/read the Repeat Time setting
1A|05 03 36|BCD 1 byte 00/01|[RECORD > Recorder Set] Send/read the TX REC Audio setting
1A|05 03 37|BCD 1 byte 00/01|[RECORD > Recorder Set] Send/read the RX REC Condition setting
1A|05 03 38|BCD 1 byte 00/01|[RECORD > Recorder Set] Send/read the File Split setting
1A|05 03 39|BCD 1 byte 00/01|[RECORD > Recorder Set] Send/read the PTT Auto REC setting
1A|05 03 40|BCD 1 byte 00 ~ 03|[RECORD > Recorder Set] Send/read the PRE-REC for PTT Auto REC setting
1A|05 03 41|BCD 1 byte 00 ~ 03|[RECORD > Player Set] Send/read the Skip Time setting
1A|05 03 42|BCD 1 byte 05 ~ 30|[RECORD > Instant Replay Set] Send/read the REC Time setting
1A|05 03 43|BCD 1 byte 03 ~ 10|[RECORD > Instant Replay Set] Send/read the Play Time setting
1A|05 03 44|BCD 1 byte 00/01|[TYPE SET: ANTENNA > TYPE] Send/read the ANT2 Connector setting
1A|05 03 45|BCD 1 byte 00/01|[TYPE SET: ANTENNA > TYPE] Send/read the ANT3 Connector setting
1A|05 03 46|BCD 1 byte 00/01|[TYPE SET: ANTENNA > TYPE] Send/read the ANT4 Connector setting
1A|05 03 47|BCD 1 byte 00/01|[TYPE SET: ANTENNA > TYPE] Send/read the RX-ANT Connectors setting
1A|05 03 48|BCD 1 byte 00 ~ 03|[TYPE SET: ANTENNA > TYPE] Send/read the External Antenna Tuner Connection setting
1A|05 03 49|See p. 22.|[ANTENNA] ANTENNA MEMORY
1A|05 03 50|See p. 22.|[ANTENNA] ANTENNA MEMORY
1A|05 03 51|See p. 22.|[ANTENNA] ANTENNA MEMORY
1A|05 03 52|See p. 22.|[ANTENNA] ANTENNA MEMORY
1A|05 03 53|See p. 22.|[ANTENNA] ANTENNA MEMORY
1A|05 03 54|See p. 22.|[ANTENNA] ANTENNA MEMORY
1A|05 03 55|See p. 22.|[ANTENNA] ANTENNA MEMORY
1A|05 03 56|See p. 22.|[ANTENNA] ANTENNA MEMORY
1A|05 03 57|See p. 22.|[ANTENNA] ANTENNA MEMORY
1A|05 03 58|See p. 22.|[ANTENNA] ANTENNA MEMORY
1A|05 03 59|See p. 22.|[ANTENNA] ANTENNA MEMORY
1A|05 03 60|See p. 22.|[ANTENNA] ANTENNA MEMORY
1A|05 03 61|BCD 1 byte 00/01|[ANTENNA] Send/read the Temporary memory
1A|05 03 62|BCD 1 byte 00/01|[ANTENNA] Send/read the Antenna selection mode ([ANT] SW) setting
1A|05 03 63|BCD 1 byte 00 ~ 09|[NB: Function > NB (Touch for 1 second)] Send/read the DEPTH setting
1A|05 03 64|BCD 2 bytes 00 00 ~ 02 55|[NB: Function > NB (Touch for 1 second)] Send/read the WIDTH setting
1A|05 03 65|BCD 1 byte 00 ~ 20|[VOX: Function > VOX (Touch for 1 second)] Send/read the VOX delay setting
1A|05 03 66|BCD 1 byte 00 ~ 03|[VOX: Function > VOX (Touch for 1 second)] Send/read the VOX Voice delay setting
1A|05 03 67|BCD 1 byte 00/01|[APF: Function > APF (Touch for 1 second)] Send/read the TYPE setting
1A|05 03 68|BCD 1 byte 00 ~ 06|[APF: Function > APF (Touch for 1 second)] Send/read the AF LEVEL setting
1A|05 03 69|BCD 1 byte 00 ~ 08|[SET > Network > Controller Connection (via LAN)] Send/read the Connection Route
1A|05 03 70|BCD 1 byte 00/01|[SET > Network > Controller Connection (via LAN)] Send/read The Route includes Wireless LAN setting
1A|05 03 71|See p. 19.|[SET > Network > Controller Connection (via LAN) > Port Forwarding Settings] Send/read the Connection Destination (RF Deck WAN Address) (valid after restart) setting
1A|05 03 72|BCD 1 byte 00 ~ 12|[SET > Network > Controller Connection (via LAN) > Audio Buffer] Send/read the Wired LAN (via Internet) setting
1A|05 03 73|BCD 1 byte 00 ~ 12|[SET > Network > Controller Connection (via LAN) > Audio Buffer] Send/read the Includes Wireless LAN setting
1A|05 03 74|BCD 1 byte 00/01|[SET > Network > Controller Connection (via LAN) > Audio Buffer] Send/read the Audio Retransmission setting
1A|06|see p.22|Send/read the DATA mode setting
1A|07|00/01|Send/read the NTP server access
1A|08|00 ~ 02|Read the NTP server access result
1A|09|00/01|Send/read the AF Mute setting
1A|0A|00/01|Read the OVF indicator status
1A|0C|see p.22|Send/read the TX Power Limit setting
1A|0D|00 ~ 04, FF|Read the External Tuner Connection
1B|00|see p.23|Send/read the Repeater tone frequency
1B|01|see p.23|Send/read the TSQL tone frequency
1C|00|00/01|Send/read the transceiver's status (PTT)
1C|01|00 ~ 02|Send/read the Antenna tuner setting
1C|02|00/01|Send/read the Transmit frequency monitor (XFC)
1C|03|see p.17|Read the transmit frequency
1C|04|00/01|Send/read the CI-V Output (for ANT) setting
1E|00||Read number of available TX frequency band
1E|01|see p.17|Read TX band edge frequencies
1E|02||Read number of user-set TX frequency band
1E|03|see p.17|Send/read user-set TX band edge frequencies
21|00|see p.23|Send/read the RIT/TX frequency
21|01|00/01|Send/read the RIT setting
21|02|00/01|Send/read the TX setting
25||see p.23: byte 00=MAIN 01=SUB + FREQ 5 bytes|Send/read the Main or Sub band frequency
26||see p.23: byte 00=MAIN 01=SUB + mode + data mode (00=OFF 01=D1 02=D2 03=D3) + filter 01-03|Send/read operating mode and filter settings (both Main and Sub bands)
27|00|see p.24|Read the Scope waveform data
27|10|00/01|Send/read the Scope ON/OFF status
27|11|00/01|Send/read the Scope wave data output setting
27|12|00/01|Send/read the Main or Sub scope setting
27|13|00/01|Send/read the Single/Dual scope setting
27|14|see p.24|Scope Center / Fixed / SCROLL-C / SCROLL-F mode setting
27|15|see p.25|Send/read Span setting (Center/SCROLL-C)
27|16|see p.25|Send/read Scope Edge Number (Fixed/SCROLL-F)
27|17|see p.25|Send/read Scope Hold ON/OFF
27|19|see p.25|Send/read Scope Reference level
27|1A|see p.25|Send/read Scope Sweep speed
27|1B|00/01|Send/read the Scope during Tx (CENTER TYPE)
27|1C|00 ~ 02|Send/read the CENTER Type Display
27|1D|see p.25|Send/read the Scope VBW setting
27|1E|see p.26|Send/read the Scope Fixed Edge frequencies
27|1F|see p.25|Send/read the Scope RBW setting
27|20|00/01|Send/read Marker Position (FIX Type/SCROLL Type)
28|00|00 ~ 08|Transmit the Voice TX Memory
29|(prefix)|BCD 1 byte 00=MAIN 01=SUB then a supported command (cmd [sub] [data])|Directly specify the Main or Sub band and send/read the supported command
";
            D["ic9700"] = @"
00||FREQ BCD 5 bytes (see p.13)|Send the frequency data (transceive)
01||MODE bytes (see p.13)|Send the mode data (transceive)
02||BAND EDGE (see p.13)|Read the band edge frequencies
03||FREQ BCD 5 bytes|Read the operating frequency
04||MODE 2 bytes mode,filter|Read the operating mode
05||FREQ BCD 5 bytes|Set the operating frequency
06||MODE bytes mode[,filter]|Set the operating mode
07||(none)|Select the VFO mode
07|00||Select VFO A
07|01||Select VFO B
07|A0||Equalize VFO A and VFO B
07|B0||Exchange MAIN and SUB Bands
07|D0||Select the main band
07|D1||Select the sub band
07|D2|00 or 01|Send/read main/sub band selection
08||(none)|Select the Memory mode
08||BCD 2 bytes 0001 to 0099|Select the Memory channel (including satellite mode)
08||BCD 2 bytes 0100 to 0105|Select program scan edge channel 1A/1B to 3A/3B
08||BCD 2 bytes 0106, 0107|Select call channel C1/C2
09||(none)|Memory write
0A||(none)|Memory copy to VFO
0B||(none)|Memory clear
0C||see p.13|Read frequency offset (duplex offset)
0D||see p.13|Send frequency offset
0E|00||Cancel the scan
0E|01||Start a Programmed/memory scan
0E|02||Start a Programmed scan
0E|03||Start a F scan
0E|12||Start a Fine programmed scan
0E|13||Start a Fine F scan
0E|22||Start a Memory scan
0E|23||Start a Select memory scan
0E|24||Start a Mode Select scan
0E|A1~A7||Select F scan span
0E|B0||Clear the Select channel setting
0E|B1||Set as select channel
0E|B1|01 to 03|Set the channel as a Select channel
0E|B2|00 to 03|Set the Select memory scan channel
0E|D0||Set Scan resume OFF
0E|D3||Set Scan resume ON (Close&Delay)
0F||(none) request, reply 00/01/11/12/13|Read Split / duplex setting
0F|00||Set Split function OFF
0F|01||Set Split function ON
0F|10||Set the simplex operation
0F|11||Set DUP- operation
0F|12||Set DUP+ operation
0F|13||Set DD Repeater Simplex mode (RPS)
10||00 to 11|Send/read the tuning step
11||BCD 1 byte 00 or 10|Send/read attenuator
13|00||Speech all data with voice synthesizer
13|01||Speech the operating frequency and S meter level
13|02||Speech the operating mode
14|01|BCD 2 bytes 0000 ~ 0255|Send/read the AF level
14|02|BCD 2 bytes 0000 ~ 0255|Send/read the RF gain level
14|03|BCD 2 bytes 0000 ~ 0255|Send/read the squelch level
14|06|BCD 2 bytes 0000 ~ 0255|Send/read the NR level
14|07|BCD 2 bytes 0000 ~ 0255|Send/read TWIN PBT (PBT1) position
14|08|BCD 2 bytes 0000 ~ 0255|Send/read TWIN PBT (PBT2) position
14|09|BCD 2 bytes 0000 ~ 0255|Send/read CW pitch
14|0A|BCD 2 bytes 0000 ~ 0255|Send/read RF power
14|0B|BCD 2 bytes 0000 ~ 0255|Send/read MIC gain
14|0C|BCD 2 bytes 0000 ~ 0255|Send/read keying speed
14|0D|BCD 2 bytes 0000 ~ 0255|Send/read Notch filter setting
14|0E|BCD 2 bytes 0000 ~ 0255|Send/read the COMP level
14|0F|BCD 2 bytes 0000 ~ 0255|Send/read the Break-IN delay
14|12|BCD 2 bytes 0000 ~ 0255|Send/read NB level
14|15|BCD 2 bytes 0000 ~ 0255|Send/read Monitor audio [MONI] level
14|16|BCD 2 bytes 0000 ~ 0255|Send/read the VOX gain
14|17|BCD 2 bytes 0000 ~ 0255|Send/read the Anti VOX gain
14|19|BCD 2 bytes 0000 ~ 0255|Send/read LCD backlight brightness
15|01|00 or 01|Read noise or S-meter squelch status
15|02|BCD 2 bytes 0000 to 0255|Read S-meter level
15|05|00 or 01|Read various squelch (tone squelch etc) status
15|07|00 or 01|Read the OVF status
15|11|BCD 2 bytes 0000 ~ 0255|Read the PO meter level
15|12|BCD 2 bytes 0000 ~ 0255|Read SWR meter level
15|13|BCD 2 bytes 0000 ~ 0255|Read ALC meter level
15|14|BCD 2 bytes 0000 ~ 0255|Read COMP meter level
15|15|BCD 2 bytes 0000 ~ 0255|Read Vd meter level
15|16|BCD 2 bytes 0000 ~ 0255|Read Id meter level
16|02|00 to 03|Send/read the Preamp / External Preamp
16|12|01 to 03|Send/read the AGC time constant
16|22|00 or 01|Send/read the Noise blanker
16|40|00 or 01|Send/read the Noise reduction
16|41|00 or 01|Send/read the Auto Notch function
16|42|00 or 01|Send/read the Repeater tone
16|43|00 or 01|Send/read the Tone squelch
16|44|00 or 01|Send/read the Speech compressor
16|45|00 or 01|Send/read the Monitor [MONI] function
16|46|00 or 01|Send/read the VOX function
16|47|00 to 02|Send/read the BK-IN function
16|48|00 or 01|Send/read the Manual Notch function
16|4A|00 or 01|Send/read the AFC function
16|4B|00 or 01|Send/read the DTCS function
16|4F|00 or 01|Send/read the Twin peak filter
16|50|00 or 01|Send/read the Dial lock function
16|56|00 or 01|Send/read the DSP IF filter type
16|57|00 to 02|Send/read the Manual Notch width
16|58|00 to 02|Send/read SSB transmit bandwidth
16|59|00 or 01|Send/read the sub band (the Dualwatch function)
16|5A|00 or 01|Send/read the satellite mode
16|5B|00 to 02|Send/read the DSQL/CSQL setting (DV mode only)
16|5C|00 to 02|Set the GPS TX mode
16|(sub byte not printed in the manual; probably 5D - UNSURE)|00 to 03, 06 to 09|Set the Tone squelch function
16|65|00 or 01|Set the IP Plus function
17||ASCII text up to 30 chars (see p.10)|Send CW messages
18|00||Turn OFF the transceiver
18|01||Turn ON the transceiver
19|00||Read the transceiver ID
1A|00|see pp.14,15|Send/read memory contents
1A|01|see p.15|Send/read band stacking register contents
1A|02|see pp.15,16|Send/read memory keyer contents
1A|03|see p.16|Send/read the selected IF filter width
1A|04|see p.16|Send/read the selected AGC time constant
1A|05 00 01|See p. 16|SET > Tone Control/TBW > RX > Send/read SSB RX HPF/LPF settings
1A|05 00 02|BCD 1 byte 00 to 10|SET > Tone Control/TBW > RX > Send/read SSB RX Tone (Bass) level
1A|05 00 03|BCD 1 byte 00 to 10|SET > Tone Control/TBW > RX > Send/read SSB RX Tone (Treble) level
1A|05 00 04|See p. 16|SET > Tone Control/TBW > RX > Send/read AM RX HPF/LPF settings
1A|05 00 05|BCD 1 byte 00 to 10|SET > Tone Control/TBW > RX > Send/read AM RX Tone (Bass) level
1A|05 00 06|BCD 1 byte 00 to 10|SET > Tone Control/TBW > RX > Send/read AM RX Tone (Treble) level
1A|05 00 07|See p. 16|SET > Tone Control/TBW > RX > Send/read FM RX HPF/LPF settings
1A|05 00 08|BCD 1 byte 00 to 10|SET > Tone Control/TBW > RX > Send/read FM RX Tone (Bass) level
1A|05 00 09|BCD 1 byte 00 to 10|SET > Tone Control/TBW > RX > Send/read FM RX Tone (Treble) level
1A|05 00 10|See p. 16|SET > Tone Control/TBW > RX > Send/read DV RX HPF/LPF settings
1A|05 00 11|BCD 1 byte 00 to 10|SET > Tone Control/TBW > RX > Send/read DV RX Tone (Bass) level
1A|05 00 12|BCD 1 byte 00 to 10|SET > Tone Control/TBW > RX > Send/read Auto DV RX Tone (Treble) level
1A|05 00 13|See p. 16|SET > Tone Control/TBW > RX > Send/read CW RX HPF/LPF settings
1A|05 00 14|See p. 16|SET > Tone Control/TBW > RX > Send/read RTTY RX HPF/LPF settings
1A|05 00 15|BCD 1 byte 00 to 10|SET > Tone Control/TBW > TX > Send/read SSB TX Tone (Bass) level
1A|05 00 16|BCD 1 byte 00 to 10|SET > Tone Control/TBW > TX > Send/read SSB TX Tone (Treble) level
1A|05 00 17|See p. 16|SET > Tone Control/TBW > TX > Send/read SSB TX bandwidth for wide
1A|05 00 18|See p. 16|SET > Tone Control/TBW > TX > Send/read SSB TX bandwidth for mid
1A|05 00 19|See p. 16|SET > Tone Control/TBW > TX > Send/read SSB TX bandwidth for narrow
1A|05 00 20|See p. 16|SET > Tone Control/TBW > TX > SSB-D TX passband width
1A|05 00 21|BCD 1 byte 00 to 10|SET > Tone Control/TBW > TX > Send/read AM TX Tone (Bass) level
1A|05 00 22|BCD 1 byte 00 to 10|SET > Tone Control/TBW > TX > Send/read AM TX Tone (Treble) level
1A|05 00 23|BCD 1 byte 00 to 10|SET > Tone Control/TBW > TX > Send/read FM TX Tone (Bass) level
1A|05 00 24|BCD 1 byte 00 to 10|SET > Tone Control/TBW > TX > Send/read FM TX Tone (Treble) level
1A|05 00 25|BCD 1 byte 00 to 10|SET > Tone Control/TBW > TX > Send/read DV TX Tone (Bass) level
1A|05 00 26|BCD 1 byte 00 to 10|SET > Tone Control/TBW > TX > Send/read DV TX Tone (Treble) level
1A|05 00 27|BCD 2 bytes 0000 ~ 0255|SET > Function > Beep Level
1A|05 00 28|BCD 1 byte 00 or 01|SET > Function > Beep Level Limit
1A|05 00 29|BCD 1 byte 00 or 01|SET > Function > Beep (Confirmation)
1A|05 00 30|BCD 1 byte 00 or 01|SET > Function > Band Edge Beep
1A|05 00 31|BCD 2 bytes 0050 ~ 0200|SET > Function > Beep Sound (MAIN)
1A|05 00 32|BCD 2 bytes 0050 ~ 0200|SET > Function > Beep Sound (SUB)
1A|05 00 33|BCD 1 byte 00 or 01|SET > Function > Sub Band Mute (TX) > Speaker/Phones
1A|05 00 34|BCD 1 byte 00 or 01|SET > Function > Sub Band Mute (TX) > USB
1A|05 00 35|BCD 1 byte 00 or 01|SET > Function > Sub Band Mute (TX) > LAN
1A|05 00 36|BCD 1 byte 00 to 02|SET > Function > RF/SQL Control
1A|05 00 37|BCD 1 byte 00 or 01|SET > Function > FM/DV Center Error function
1A|05 00 38|BCD 1 byte 00 to 05|SET > Function > TX Delay > 144M
1A|05 00 39|BCD 1 byte 00 to 05|SET > Function > TX Delay > 430M
1A|05 00 40|BCD 1 byte 00 to 05|SET > Function > TX Delay > 1200M
1A|05 00 41|BCD 1 byte 00 to 05|SET > Function > Time-Out Timer
1A|05 00 42|BCD 1 byte 00 or 01|SET > Function > PTT Lock
1A|05 00 43|BCD 1 byte 00 or 01|SET > Function > SPLIT > Quick SPLIT
1A|05 00 44|See p. 16|SET > Function > SPLIT > FM SPLIT Offset
1A|05 00 45|BCD 1 byte 00 or 01|SET > Function > SPLIT > SPLIT LOCK
1A|05 00 46|BCD 1 byte 00 or 01|SET > Function > Auto Repeater
1A|05 00 47|BCD 1 byte 00 to 02|SET > Function > RTTY Mark Frequency
1A|05 00 48|BCD 1 byte 00 to 02|SET > Function > RTTY Shift Width
1A|05 00 49|BCD 1 byte 00 or 01|SET > Function > RTTY Keying Polarity
1A|05 00 50|BCD 1 byte 00 or 01|SET > Function > SPEECH > SPEECH Language
1A|05 00 51|BCD 1 byte 00 or 01|SET > Function > SPEECH > Alphabet
1A|05 00 52|BCD 1 byte 00 or 01|SET > Function > SPEECH > SPEECH Speed
1A|05 00 53|BCD 1 byte 00 to 02|SET > Function > SPEECH > RX Call Sign SPEECH
1A|05 00 54|BCD 1 byte 00 or 01|SET > Function > SPEECH > RX>CS SPEECH
1A|05 00 55|BCD 1 byte 00 or 01|SET > Function > SPEECH > S-Level SPEECH
1A|05 00 56|BCD 1 byte 00 or 01|SET > Function > SPEECH > MODE SPEECH
1A|05 00 57|BCD 2 bytes 0000 ~ 0255|SET > Function > SPEECH > SPEECH Level
1A|05 00 58|BCD 1 byte 00 or 01|SET > Function > [SPEECH/LOCK] Switch
1A|05 00 59|BCD 1 byte 00 or 01|SET > Function > Lock Function
1A|05 00 60|BCD 1 byte 00 or 01|SET > Function > Memo Pad Quantity
1A|05 00 61|BCD 1 byte 00 to 02|SET > Function > MAIN DIAL Auto TS
1A|05 00 62|BCD 1 byte 00 or 01|SET > Function > MIC Up/Down Speed
1A|05 00 63|BCD 1 byte 00 or 01|SET > Function > AFC Limit
1A|05 00 64|BCD 1 byte 00 to 02|SET > Function > [NOTCH] Switch (SSB)
1A|05 00 65|BCD 1 byte 00 to 02|SET > Function > [NOTCH] Switch (AM)
1A|05 00 66|BCD 1 byte 00 or 01|SET > Function > SSB/CW Synchronous Tuning
1A|05 00 67|BCD 1 byte 00 or 01|SET > Function > CW Normal Side
1A|05 00 68|BCD 1 byte 00 or 01|SET > Function > Screen Keyboard Type
1A|05 00 69|BCD 1 byte 00 to 02|SET > Function > Screen Full Keyboard Layout
1A|05 00 70|BCD 1 byte 00 or 01|SET > Function > Screen Capture [POWER] Switch
1A|05 00 71|BCD 1 byte 00 or 01|SET > Function > Screen Capture File Type
1A|05 00 72|BCD 2 bytes 0000 ~ 0255|SET > Function > REF Adjust
1A|05 00 73|BCD 2 bytes 0000 ~ 0255|SET > Function > REF Adjust (FINE)
1A|05 00 74|BCD 1 byte 00 to 03|SET > DV/DD Set > Standby Beep
1A|05 00 75|BCD 1 byte 00 to 02|SET > DV/DD Set > Auto Reply
1A|05 00 76|BCD 1 byte 00 or 01|SET > DV/DD Set > DV Data TX
1A|05 00 77|BCD 1 byte 00 or 01|SET > DV/DD Set > DV Fast Data > Fast Data
1A|05 00 78|BCD 1 byte 00 or 01|SET > DV/DD Set > DV Fast Data > GPS Data Speed
1A|05 00 79|BCD 1 byte 00 to 10|SET > DV/DD Set > DV Fast Data > TX Delay (PTT)
1A|05 00 80|BCD 1 byte 00 to 02|SET > DV/DD Set > Digital Monitor
1A|05 00 81|BCD 1 byte 00 or 01|SET > DV/DD Set > Digital Repeater Set
1A|05 00 82|BCD 1 byte 00 or 01|SET > DV/DD Set > DV Auto Detect
1A|05 00 83|BCD 1 byte 00 or 01|SET > DV/DD Set > RX Record (RPT)
1A|05 00 84|BCD 1 byte 00 or 01|SET > DV/DD Set > BK
1A|05 00 85|BCD 1 byte 00 or 01|SET > DV/DD Set > EMR
1A|05 00 86|BCD 2 bytes 0000 ~ 0255|SET > DV/DD Set > EMR AF Level
1A|05 00 87|BCD 1 byte 00 or 01|SET > DV/DD Set > DD TX Inhibit (Power ON)
1A|05 00 88|BCD 1 byte 00 or 01|SET > DV/DD Set > DD Packet Output
1A|05 00 89|BCD 1 byte 00 or 01|SET > QSO/RX Log > QSO Log
1A|05 00 90|BCD 1 byte 00 or 01|SET > QSO/RX Log > RX History Log
1A|05 00 91|BCD 1 byte 00 to 02|SET > QSO/RX Log > CSV Format > Separator/Decimal
1A|05 00 92|BCD 1 byte 00 to 02|SET > QSO/RX Log > CSV Format > Date
1A|05 00 93|BCD 1 byte 00 or 01|SET > Connectors > External P.AMP > 144M
1A|05 00 94|BCD 1 byte 00 or 01|SET > Connectors > External P.AMP > 430M
1A|05 00 95|BCD 1 byte 00 or 01|SET > Connectors > External P.AMP > 1200M
1A|05 00 96|BCD 1 byte 00 or 01|SET > Connectors > External Speaker Separate
1A|05 00 97|BCD 1 byte 00 to 30|SET > Connectors > Phones > Level
1A|05 00 98|BCD 1 byte 00 to 02|SET > Connectors > Phones > L/R Mix
1A|05 00 99|BCD 1 byte 00 or 01|SET > Connectors > ACC AF/IF Output > AF/SQL Output Select
1A|05 01 00|BCD 1 byte 00 or 01|SET > Connectors > ACC AF/IF Output > Output Select
1A|05 01 01|BCD 2 bytes 0000 ~ 0255|SET > Connectors > ACC AF/IF Output > AF Output Level
1A|05 01 02|BCD 1 byte 00 or 01|SET > Connectors > ACC AF/IF Output > AF SQL
1A|05 01 03|BCD 1 byte 00 or 01|SET > Connectors > ACC AF/IF Output > AF Beep/Speech... Output
1A|05 01 04|BCD 2 bytes 0000 ~ 0255|SET > Connectors > ACC AF/IF Output > ACC IF Output Level
1A|05 01 05|BCD 1 byte 00 or 01|SET > Connectors > USB AF/IF Output > Output Select
1A|05 01 06|BCD 2 bytes 0000 ~ 0255|SET > Connectors > USB AF/IF Output > AF Output Level
1A|05 01 07|BCD 1 byte 00 or 01|SET > Connectors > USB AF/IF Output > AF SQL
1A|05 01 08|BCD 1 byte 00 or 01|SET > Connectors > USB AF/IF Output > AF Beep/Speech... Output
1A|05 01 09|BCD 2 bytes 0000 ~ 0255|SET > Connectors > USB AF/IF Output > IF Output Level
1A|05 01 10|BCD 1 byte 00 or 01|SET > Connectors > LAN AF/IF Output > Output Select
1A|05 01 11|BCD 1 byte 00 or 01|SET > Connectors > LAN AF/IF Output > AF SQL
1A|05 01 12|BCD 2 bytes 0000 ~ 0255|SET > Connectors > MOD Input > ACC MOD Level
1A|05 01 13|BCD 2 bytes 0000 ~ 0255|SET > Connectors > MOD Input > USB MOD Level
1A|05 01 14|BCD 2 bytes 0000 ~ 0255|SET > Connectors > MOD Input > LAN MOD Level
1A|05 01 15|BCD 1 byte 00 ~ 05|SET > Connectors > MOD Input > DATA OFF MOD
1A|05 01 16|BCD 1 byte 00 ~ 05|SET > Connectors > MOD Input > DATA MOD
1A|05 01 17|BCD 1 byte 00 or 01|SET > Connectors > ACC SEND Output > 144M
1A|05 01 18|BCD 1 byte 00 or 01|SET > Connectors > ACC SEND Output > 430M
1A|05 01 19|BCD 1 byte 00 or 01|SET > Connectors > ACC SEND Output > 1200M
1A|05 01 20|BCD 1 byte 00 to 04|SET > Connectors > USB SEND/Keying > USB SEND
1A|05 01 21|BCD 1 byte 00 to 04|SET > Connectors > USB SEND/Keying > USB Keying (CW)
1A|05 01 22|BCD 1 byte 00 to 04|SET > Connectors > USB SEND/Keying > USB Keying (RTTY)
1A|05 01 23|BCD 1 byte 00 or 01|SET > Connectors > USB SEND/Keying > Inhibit Timer at USB connection
1A|05 01 24|BCD 1 byte 00 or 01|SET > Connectors > External Keypad > VOICE
1A|05 01 25|BCD 1 byte 00 or 01|SET > Connectors > External Keypad > KEYER
1A|05 01 26|BCD 1 byte 00 or 01|SET > Connectors > External Keypad > RTTY
1A|05 01 27|BCD 1 byte 00 or 01|SET > Connectors > CI-V > CI-V Transceive
1A|05 01 28|BCD 2 bytes 0000 ~ 0223|SET > Connectors > CI-V > CI-V USB/ LANREMOTE Transceive Address
1A|05 01 29|BCD 1 byte 00 or 01|SET > Connectors > CI-V > CI-V USB Port
1A|05 01 30|BCD 1 byte 00 or 01|SET > Connectors > CI-V > CI-V USB Echo Back
1A|05 01 31|BCD 1 byte 00 or 01|SET > Connectors > CI-V > CI-V DATA Echo Back
1A|05 01 32|BCD 1 byte 00 to 02|SET > Connectors > CI-V > USB (B)/DATA Function > USB (B) Function
1A|05 01 33|BCD 1 byte 00 to 04|SET > Connectors > CI-V > USB (B)/DATA Function > DATA Function
1A|05 01 34|BCD 1 byte 00 or 01|SET > Connectors > CI-V > USB (B)/DATA Function > GPS Out
1A|05 01 35|BCD 1 byte 00 or 01|SET > Connectors > CI-V > USB (B)/DATA Function > DV Data/GPS Out Baud Rate
1A|05 01 36|BCD 1 byte 00 to 03|SET > Connectors > CI-V > USB (B)/DATA Function > RTTY Decode Baud Rate
1A|05 01 37|BCD 1 byte 00 or 01|SET > Network > DHCP (Valid after Restart)
1A|05 01 38|BCD 8 bytes 0000000000 000001 ~ 0255025502 550254|SET > Network > IP Address (Valid after Restart)
1A|05 01 39|BCD 8 bytes 0000000000 000001 ~ 0255025502 550254|SET > Network > DHCP (Valid after Restart) Read the IP address set by the DHCP server
1A|05 01 40|BCD 1 byte 01 ~ 30|SET > Network > Subnet Mask (Valid after Restart)
1A|05 01 41|BCD 8 bytes 0000000000 000001 ~ 0255025502 550254, FF|SET > Network > Default Gateway (Valid after Restart)
1A|05 01 42|BCD 8 bytes 0000000000 000001 ~ 0255025502 550254, FF|SET > Network > Primary DNS Server (Valid after Restart)
1A|05 01 43|BCD 8 bytes 0000000000 000001 ~ 0255025502 550254, FF|SET > Network > 2nd DNS Server (Valid after Restart)
1A|05 01 44|See p. 15|SET > Network > Network Name
1A|05 01 45|BCD 1 byte 00 or 01|SET > Network > Network Control (Valid after Restart)
1A|05 01 46|BCD 1 byte 00 or 01|SET > Network > Power OFF Setting (for Remote Control)
1A|05 01 47|BCD 2 bytes 000001 ~ 065535|SET > Network > Control Port (UDP) (Valid after Restart)
1A|05 01 48|BCD 2 bytes 000001 ~ 065535|SET > Network > Serial Port (UDP) (Valid after Restart)
1A|05 01 49|BCD 2 bytes 000001 ~ 065535|SET > Network > Audio Port (UDP) (Valid after Restart)
1A|05 01 50|BCD 1 byte 00 or 01|SET > Network > Internet Access Line (Valid after Restart)
1A|05 01 51|See p. 15|SET > Network > Network Radio Name
1A|05 01 52|BCD 2 bytes 0000 ~ 0255|SET > Display > LCD Backlight
1A|05 01 53|BCD 1 byte 00 or 01|SET > Display > Display Type
1A|05 01 54|BCD 1 byte 00 or 01|SET > Display > Display Font
1A|05 01 55|BCD 1 byte 00 or 01|SET > Display > Meter Peak Hold (Bar)
1A|05 01 56|BCD 1 byte 00 or 01|SET > Display > Memory Name
1A|05 01 57|BCD 1 byte 00 or 01|SET > Display > MN-Q Popup (MN OFFON)
1A|05 01 58|BCD 1 byte 00 or 01|SET > Display > BW Popup (PBT)
1A|05 01 59|BCD 1 byte 00 or 01|SET > Display > BW Popup (FIL)
1A|05 01 60|BCD 1 byte 00 to 03|SET > Display > RX Call Sign Display
1A|05 01 61|BCD 1 byte 00 or 01|SET > Display > RX Position Indicator
1A|05 01 62|BCD 1 byte 00 to 02|SET > Display > RX Position Display
1A|05 01 63|BCD 1 byte 00 to 04|SET > Display > RX Position Display Timer
1A|05 01 64|BCD 1 byte 00 or 01|SET > Display > Reply Position Display
1A|05 01 65|BCD 1 byte 00 to 02|SET > Display > TX Call Sign Display
1A|05 01 66|BCD 1 byte 00 or 01|SET > Display > Scroll Speed
1A|05 01 67|BCD 1 byte 00 to 03|SET > Display > Screen Saver
1A|05 01 68|BCD 1 byte 00 or 01|SET > Display > Opening Message
1A|05 01 69|BCD 1 byte 00 or 01|SET > Display > Power ON Check
1A|05 01 70|BCD 1 byte 00 or 01|SET > Display > Display Unit > Latitude/Longitude
1A|05 01 71|BCD 1 byte 00 or 01|SET > Display > Display Unit > Altitude/Distance
1A|05 01 72|BCD 1 byte 00 to 02|SET > Display > Display Unit > Speed
1A|05 01 73|BCD 1 byte 00 or 01|SET > Display > Display Unit > Temperature
1A|05 01 74|BCD 1 byte 00 to 03|SET > Display > Display Unit > Barometric
1A|05 01 75|BCD 1 byte 00 or 01|SET > Display > Display Unit > Rainfall
1A|05 01 76|BCD 1 byte 00 to 03|SET > Display > Display Unit > Wind Speed
1A|05 01 77|BCD 1 byte 00 or 01|SET > Display > Display Language
1A|05 01 78|BCD 1 byte 00 or 01|SET > Display > System Language
1A|05 01 79|BCD 2 bytes 20000101 ~ 20991231|SET > Time Set > Date/Time > Date
1A|05 01 80|BCD 2 bytes 0000 ~ 2359|SET > Time Set > Date/Time > Time
1A|05 01 81|BCD 1 byte 00 or 01|SET > Time Set > Date/Time > NTP Function
1A|05 01 82|See p. 15|SET > Time Set > Date/Time > NTP Server Address
1A|05 01 83|BCD 1 byte 00 or 01|SET > Time Set > Date/Time > GPS Time Correct
1A|05 01 84|See p. 16|SET > Time Set > UTC Offset
1A|05 01 85|BCD 1 byte 00 to 02|SET > SD Card > Import/Export > CSV Format > Separator/Decimal
1A|05 01 86|BCD 1 byte 00 to 02|SET > SD Card > Import/Export > CSV Format > Date
1A|05 01 87|BCD 1 byte 00 or 01|SCOPE > Scope during Tx (CENTER TYPE)
1A|05 01 88|BCD 1 byte 00 to 02|SCOPE > Max Hold
1A|05 01 89|BCD 1 byte 00 ~ 02|SCOPE > CENTER Type Display
1A|05 01 90|BCD 1 byte 00 or 01|SCOPE > Marker Position (Fix Type)
1A|05 01 91|See p. 16|SCOPE > VBW
1A|05 01 92|BCD 1 byte 00 to 03|SCOPE > Averaging
1A|05 01 93|BCD 1 byte 00 or 01|SCOPE > Waveform Type
1A|05 01 94|See p. 16|SCOPE > Waveform Color (Current)
1A|05 01 95|See p. 16|SCOPE > Waveform Color (Line)
1A|05 01 96|See p. 16|SCOPE > Waveform Color (Max Hold)
1A|05 01 97|BCD 1 byte 00 or 01|SCOPE > Waterfall Display
1A|05 01 98|BCD 1 byte 00 to 02|SCOPE > Waterfall Speed
1A|05 01 99|BCD 1 byte 00 to 02|SCOPE > Waterfall Size (Expand Screen)
1A|05 02 00|BCD 1 byte 00 to 07|SCOPE > Waterfall Peak Color Level
1A|05 02 01|BCD 1 byte 00 or 01|SCOPE > Waterfall Marker Auto-hide
1A|05 02 02|See p. 17|SCOPE > Fixed Edges > 144M > No.1:
1A|05 02 03|See p. 17|SCOPE > Fixed Edges > 144M > No.2:
1A|05 02 04|See p. 17|SCOPE > Fixed Edges > 144M > No.3:
1A|05 02 05|See p. 17|SCOPE > Fixed Edges > 430M > No.1:
1A|05 02 06|See p. 17|SCOPE > Fixed Edges > 430M > No.2:
1A|05 02 07|See p. 17|SCOPE > Fixed Edges > 430M > No.3:
1A|05 02 08|See p. 17|SCOPE > Fixed Edges > 1200M > No.1:
1A|05 02 09|See p. 17|SCOPE > Fixed Edges > 1200M > No.2:
1A|05 02 10|See p. 17|SCOPE > Fixed Edges > 1200M > No.3:
1A|05 02 11|BCD 1 byte 00 or 01|AUDIO SCOPE SET > FFT Scope Waveform Type
1A|05 02 12|See p. 16|AUDIO SCOPE SET > FFT Scope Waveform Color
1A|05 02 13|BCD 1 byte 00 or 01|AUDIO SCOPE SET > FFT Scope Waterfall Display
1A|05 02 14|See p. 16|AUDIO SCOPE SET > Oscilloscope Waveform Color
1A|05 02 15|BCD 2 bytes 0000 ~ 0255|VOICE TX > TX LEVEL
1A|05 02 16|BCD 1 byte 00 or 01|VOICE TX SET > Auto Monitor
1A|05 02 17|BCD 1 byte 01 to 15|VOICE TX SET > Repeat Time
1A|05 02 18|BCD 1 byte 00 to 04|KEYER 001 > Number Style
1A|05 02 19|BCD 1 byte 01 to 08|KEYER 001 > Count Up Trigger
1A|05 02 20|BCD 2 bytes 0001 to 9999|KEYER 001 > Present Number
1A|05 02 21|BCD 2 bytes 0000 ~ 0255|CW-KEY SET > Side Tone Level
1A|05 02 22|BCD 1 byte 00 or 01|CW-KEY SET > Side Tone Level Limit
1A|05 02 23|BCD 1 byte 01 to 60|CW-KEY SET > Keyer Repeat time
1A|05 02 24|BCD 1 byte 28 to 45|CW-KEY SET > Dot/Dash Ratio
1A|05 02 25|BCD 1 byte 00 to 03|CW-KEY SET > Rise Time
1A|05 02 26|BCD 1 byte 00 or 01|CW-KEY SET > Paddle Polarity
1A|05 02 27|BCD 1 byte 00 to 02|CW-KEY SET > Key Type
1A|05 02 28|BCD 1 byte 00 or 01|CW-KEY SET > MIC Up/Down Keyer
1A|05 02 29|BCD 1 byte 00 to 03|RTTY DECODE SET > FFT Scope Averaging
1A|05 02 30|See p. 16|RTTY DECODE SET > FFT Scope Waveform Color
1A|05 02 31|BCD 1 byte 00 or 01|RTTY DECODE SET > Decode USOS
1A|05 02 32|BCD 1 byte 00 or 01|RTTY DECODE SET > Decode New Line Code
1A|05 02 33|BCD 1 byte 00 or 01|RTTY DECODE SET > TX USOS
1A|05 02 34|BCD 1 byte 00 or 01|RTTY DECODE SET > Displayed Characters during Tx (Satellite)
1A|05 02 35|See p. 16|RTTY DECODE SET > Font Color (Receive)
1A|05 02 36|See p. 16|RTTY DECODE SET > Font Color (Transmit)
1A|05 02 37|BCD 1 byte 00 or 01|RTTY DECODE LOG > Decode Log
1A|05 02 38|BCD 1 byte 00 or 01|RTTY DECODE LOG > Log Set > File Type
1A|05 02 39|BCD 1 byte 00 or 01|RTTY DECODE SET > Log Set > Time Stamp
1A|05 02 40|BCD 1 byte 00 or 01|RTTY DECODE SET > Log Set > Time Stamp (Time)
1A|05 02 41|BCD 1 byte 00 or 01|RTTY DECODE SET > Log Set > Time Stamp (Frequency)
1A|05 02 42|BCD 1 byte 00 or 01|QSO RECORDER > Recorder Set > TX REC Audio
1A|05 02 43|BCD 1 byte 00 or 01|QSO RECORDER > Recorder Set > RX REC Condition
1A|05 02 44|BCD 1 byte 00 or 01|QSO RECORDER > Recorder Set > File Split
1A|05 02 45|BCD 1 byte 00 or 01|QSO RECORDER > Recorder Set > REC Operation
1A|05 02 46|BCD 1 byte 00 or 01|QSO RECORDER > Recorder Set > PTT Auto REC
1A|05 02 47|BCD 1 byte 00 to 03|QSO RECORDER > Recorder Set > PRE-REC for PTT Auto REC
1A|05 02 48|BCD 1 byte 00 to 03|QSO RECORDER > Player Set > Skip Time
1A|05 02 49|BCD 1 byte 00 or 01|SCAN SET > SCAN Speed
1A|05 02 50|BCD 1 byte 00 or 01|SCAN SET > SCAN Resume
1A|05 02 51|BCD 1 byte 00 to 10|SCAN SET > Pause Timer
1A|05 02 52|BCD 1 byte 00 to 06|SCAN SET > Resume Timer
1A|05 02 53|BCD 1 byte 00 to 04|SCAN SET > Temporary Skip Timer
1A|05 02 54|BCD 1 byte 00 or 01|SCAN SET > MAIN DIAL Operation (SCAN)
1A|05 02 55|BCD 1 byte 00 to 02|GPS > GPS Set > GPS Select
1A|05 02 56|BCD 1 byte 00 or 01|GPS > GPS Set > GPS Receiver Baud Rate
1A|05 02 57|See p. 17|GPS > GPS Set > Manual Position
1A|05 02 58|BCD 1 byte 00 to 02|GPS > GPS TX Mode
1A|05 02 59|See p. 17|GPS > GPS TX Mode > D-PRS > Unproto Address (Up to 56 characters)
1A|05 02 60|BCD 1 byte 00 to 03|GPS > GPS TX Mode > D-PRS > TX Format
1A|05 02 61|BCD 1 byte 00 to 04|GPS > GPS TX Mode > D-PRS > TX Format > Position > Symbol
1A|05 02 62|See p. 17|GPS > GPS TX Mode > D-PRS > TX Format > Position > the GPS-A Symbol No.1 setting (2 characters)
1A|05 02 63|See p. 17|GPS > GPS TX Mode > D-PRS > TX Format > Position > the GPS-A Symbol No.2 setting (2 characters)
1A|05 02 64|See p. 17|GPS > GPS TX Mode > D-PRS > TX Format > Position > the GPS-A Symbol No.3 setting (2 characters)
1A|05 02 65|See p. 17|GPS > GPS TX Mode > D-PRS > TX Format > Position > the GPS-A Symbol No.4 setting (2 characters)
1A|05 02 66|BCD 1 byte 00 to 42|GPS > GPS TX Mode > D-PRS > TX Format > Position > SSID
1A|05 02 67|BCD 1 byte 00 to 03|GPS > GPS TX Mode > D-PRS > TX Format > Position > Comment
1A|05 02 68|See p. 17|GPS > GPS TX Mode > D-PRS > TX Format > Position > Comment 1 (Up to 43 characters)
1A|05 02 69|See p. 17|GPS > GPS TX Mode > D-PRS > TX Format > Position > Comment 2 (Up to 43 characters)
1A|05 02 70|See p. 17|GPS > GPS TX Mode > D-PRS > TX Format > Position > Comment 3 (Up to 43 characters)
1A|05 02 71|See p. 17|GPS > GPS TX Mode > D-PRS > TX Format > Position > Comment 4 (Up to 43 characters)
1A|05 02 72|BCD 1 byte 00 to 02|GPS > GPS TX Mode > D-PRS > TX Format > Position > Time Stamp
1A|05 02 73|BCD 1 byte 00 or 01|GPS > GPS TX Mode > D-PRS > TX Format > Position > Altitude
1A|05 02 74|BCD 1 byte 00 to 02|GPS > GPS TX Mode > D-PRS > TX Format > Position > Data Extension
1A|05 02 75|BCD 1 byte 00 to 09|GPS > GPS TX Mode > D-PRS > TX Format > Position > Power
1A|05 02 76|BCD 1 byte 00 to 09|GPS > GPS TX Mode > D-PRS > TX Format > Position > Height
1A|05 02 77|BCD 1 byte 00 to 09|GPS > GPS TX Mode > D-PRS > TX Format > Position > Gain
1A|05 02 78|BCD 1 byte 00 to 08|GPS > GPS TX Mode > D-PRS > TX Format > Position > Directivity
1A|05 02 79|See p. 17|GPS > GPS TX Mode > D-PRS > TX Format > Object > Object Name (Up to 9 characters)
1A|05 02 80|BCD 1 byte 00 or 01|GPS > GPS TX Mode > D-PRS > TX Format > Object > Data Type
1A|05 02 81|See p. 17|GPS > GPS TX Mode > D-PRS > TX Format > Object > Symbol (2 characters)
1A|05 02 82|See p. 17|GPS > GPS TX Mode > D-PRS > TX Format > Object > Comment (Up to 43 characters)
1A|05 02 83|See p. 17|GPS > GPS TX Mode > D-PRS > TX Format > Object > Position
1A|05 02 84|BCD 1 byte 00 to 02|GPS > GPS TX Mode > D-PRS > TX Format > Object > Data Extension
1A|05 02 85|BCD 1 byte 000 to 360|GPS > GPS TX Mode > D-PRS > TX Format > Object > Course (0º to 360º; 1 degree steps)
1A|05 02 86|BCD 1 byte 00 to 1850|GPS > GPS TX Mode > D-PRS > TX Format > Object > Speed (0 km/h to 1850 km/h)
1A|05 02 87|BCD 1 byte 00 to 09|GPS > GPS TX Mode > D-PRS > TX Format > Object > Power
1A|05 02 88|BCD 1 byte 00 to 09|GPS > GPS TX Mode > D-PRS > TX Format > Object > Height
1A|05 02 89|BCD 1 byte 00 to 09|GPS > GPS TX Mode > D-PRS > TX Format > Object > Gain
1A|05 02 90|BCD 1 byte 00 to 08|GPS > GPS TX Mode > D-PRS > TX Format > Object > Directivity
1A|05 02 91|BCD 1 byte 00 to 42|GPS > GPS TX Mode > D-PRS > TX Format > Object > SSID
1A|05 02 92|BCD 1 byte 00 or 01|GPS > GPS TX Mode > D-PRS > TX Format > Object > Time Stamp
1A|05 02 93|See p. 17|GPS > GPS TX Mode > D-PRS > TX Format > Item > Item Name (Up to 9 characters)
1A|05 02 94|BCD 1 byte 00 or 01|GPS > GPS TX Mode > D-PRS > TX Format > Item > Data Type
1A|05 02 95|See p. 17|GPS > GPS TX Mode > D-PRS > TX Format > Item > Symbol (2 characters)
1A|05 02 96|See p. 17|GPS > GPS TX Mode > D-PRS > TX Format > Item > Comment (Up to 43 characters)
1A|05 02 97|See p. 17|GPS > GPS TX Mode > D-PRS > TX Format > Item > Position
1A|05 02 98|BCD 1 byte 00 to 02|GPS > GPS TX Mode > D-PRS > TX Format > Item > Data Extension
1A|05 02 99|BCD 1 byte 000 to 360|GPS > GPS TX Mode > D-PRS > TX Format > Item > Course (0º to 360º; 1 degree steps)
1A|05 03 00|BCD 1 byte 00 to 1850|GPS > GPS TX Mode > D-PRS > TX Format > Item > Speed (0 km/h to 1850 km/h)
1A|05 03 01|BCD 1 byte 00 to 09|GPS > GPS TX Mode > D-PRS > TX Format > Item > Power
1A|05 03 02|BCD 1 byte 00 to 09|GPS > GPS TX Mode > D-PRS > TX Format > Item > Height
1A|05 03 03|BCD 1 byte 00 to 09|GPS > GPS TX Mode > D-PRS > TX Format > Item > Gain
1A|05 03 04|BCD 1 byte 00 to 08|GPS > GPS TX Mode > D-PRS > TX Format > Item > Directivity
1A|05 03 05|BCD 1 byte 00 to 42|GPS > GPS TX Mode > D-PRS > TX Format > Item > SSID
1A|05 03 06|See p. 17|GPS > GPS TX Mode > D-PRS > TX Format > Weather > Symbol (2 characters)
1A|05 03 07|BCD 1 byte 00 to 42|GPS > GPS TX Mode > D-PRS > TX Format > Weather > SSID
1A|05 03 08|See p. 17|GPS > GPS TX Mode > D-PRS > TX Format > Weather > Comment (Up to 43 characters)
1A|05 03 09|BCD 1 byte 00 to 02|GPS > GPS TX Mode > D-PRS > TX Format > Weather > Time Stamp
1A|05 03 10|BCD 1 byte 00 or 01|GPS > GPS TX Mode > NMEA > GPS Sentence (RMC)
1A|05 03 11|BCD 1 byte 00 or 01|GPS > GPS TX Mode > NMEA > GPS Sentence (CGA)
1A|05 03 12|BCD 1 byte 00 or 01|GPS > GPS TX Mode > NMEA > GPS Sentence (GLL)
1A|05 03 13|BCD 1 byte 00 or 01|GPS > GPS TX Mode > NMEA > GPS Sentence (GSA)
1A|05 03 14|BCD 1 byte 00 or 01|GPS > GPS TX Mode > NMEA > GPS Sentence (VTG)
1A|05 03 15|BCD 1 byte 00 or 01|GPS > GPS TX Mode > NMEA > GPS Sentence (GSV)
1A|05 03 16|See p. 17|GPS > GPS TX Mode > NMEA > GPS Message (Up to 20 characters)
1A|05 03 17|See p. 17|GPS > GPS Alarm> Alarm Area (Group)
1A|05 03 18|BCD 1 byte 00 to 03|GPS > GPS Alarm> Alarm Area (RX/Memory)
1A|05 03 19|BCD 1 byte 00 to 08|GPS > GPS Auto TX 00=OFF, 01**=5 sec., 02=10 sec., 03=30 sec., 04=1 min., 05=3 min., 06=5 min., 07=10 min., 08=30 min.) ** When 4 kinds of GPS sentences are selected, you cannot select ""01.""
1A|05 03 20|BCD 1 byte 00 to 03|DTMF SET > DTMF Speed
1A|05 03 21|BCD 2 bytes 0000 to 0255|Set the NB LEVEL (144 MHz)
1A|05 03 22|BCD 1 byte 00 to 09|Set the NB DEPTH (144 MHz)
1A|05 03 23|BCD 2 bytes 0000 to 0255|Set the NB WIDTH (144 MHz)
1A|05 03 24|BCD 2 bytes 0000 to 0255|Set the NB LEVEL (430 MHz)
1A|05 03 25|BCD 1 byte 00 to 09|Set the NB DEPTH (430 MHz)
1A|05 03 26|BCD 2 bytes 0000 to 0255|Set the NB WIDTH (430 MHz)
1A|05 03 27|BCD 2 bytes 0000 to 0255|Set the NB LEVEL (1200 MHz)
1A|05 03 28|BCD 1 byte 00 to 09|Set the NB DEPTH (1200 MHz)
1A|05 03 29|BCD 2 bytes 0000 to 0255|Set the NB WIDTH (1200 MHz)
1A|05 03 30|BCD 1 byte 00 to 20|Set the VOX DELAY
1A|05 03 31|BCD 1 byte 00 to 03|Set the VOX voice delay
1A|05 03 32|BCD 1 byte 00 or 01|Set the TX PWR LIMIT (144M) function
1A|05 03 33|BCD 2 bytes 0000 to 0255|Set the TX PWR LIMIT (144M)
1A|05 03 34|BCD 1 byte 00 or 01|Set the TX PWR LIMIT (430M) function
1A|05 03 35|BCD 2 bytes 0000 to 0255|Set the TX PWR LIMIT (430M)
1A|05 03 36|BCD 1 byte 00 or 01|Set the TX PWR LIMIT (1200M) function
1A|05 03 37|BCD 2 bytes 0000 to 0255|Set the TX PWR LIMIT (1200M)
1A|05 03 38|BCD 1 byte 00 or 01|Set the Received Call sign Display (""Name"" or ""Call Sign"")
1A|05 03 39|BCD 1 byte 00 to 02|Set the Compass Direction
1A|06|see p.17|DATA mode with filter set
1A|07|see p.18|Set the Satellite memory contents
1A|08|00 or 01|NTP server access
1A|09|00 to 02|Read NTP server access result
1A|0A|00 or 01|Read the OVF indicator status
1B|00|see p.19|Send/read the Repeater tone frequency
1B|01|see p.19|Send/read the TSQL tone frequency
1B|02|see p.19|Send/read the DTCS code and polarity
1B|07|see p.19|Send/read the CSQL code (DV mode)
1C|00|00 or 01|Send/read the transceiver's status (PTT)
1C|02|00 or 01|Send/read the Transmit frequency monitor (XFC)
1C|03|see p.13|Read the transmit frequency
1E|00||Read number of available TX frequency band
1E|01|see p.13|Read TX band edge frequencies
1E|02||Read number of user-set TX frequency band
1E|03|see p.13|Set the user-set TX band edge frequencies
1F|00|see p.19|SET > My Station > My Call Sign (DV)
1F|01|see p.19|SET > Display > TX Call Sign Display
1F|02|see p.19|SET > My Station > TX Message (DV)
20|00 00|00 or 01|Send/read the Auto DV RX Call signs output
20|00 01|see p.20|Output DV RX Call signs for transceive
20|00 02|see p.20|Read Auto DV RX Call signs
20|01 00|00 or 01|Send/read the Auto DV RX message output
20|01 01|see p.20|Output DV RX message for transceive
20|01 02|see p.20|Read Auto DV RX message
20|02 00|00 or 01|Send/read the Auto DV RX status output
20|02 01|see p.20|Output DV RX status for transceive
20|02 02|see p.20|Read Auto DV RX status
20|03 00|00 or 01|Send/read the Auto DV RX GPS/D-PRS data output
20|03 0100|see p.21|Output DV RX GPS/D-PRS Position for transceive
20|03 0101|see p.21|Output DV RX D-PRS Object status for transceive
20|03 0102|see p.21|Output DV RX D-PRS Item status for transceive
20|03 0103|see p.21|Output DV RX D-PRS Weather status for transceive
20|03 0200|see p.21|Read Auto DV RX GPS/D-PRS Position status
20|03 0201|see p.21|Read Auto DV RX D-PRS Object status
20|03 0202|see p.21|Read Auto DV RX D-PRS Item status
20|03 0203|see p.21|Read Auto DV RX D-PRS Weather status
20|04 00|00 or 01|Send/read Auto DV RX GPS/D-PRS message output
20|04 01|see p.22|Output DV RX D-PRS message for transceive
20|04 02|see p.22|Read Auto DV RX D-PRS message status
21|00|see p.23|RIT frequency
21|01|00 or 01|RIT setting
22|00|see p.23|Set the DV TX data (up to 30 byte)
22|01 00|00 or 01|Set the Auto DV RX data output
22|01 01|see p.23|Set the DV RX data for transceive (up to 30 byte)
22|02|00 or 01|SET > DV/DD Set > DV Data TX
22|03|00 or 01|SET > DV/DD Set > DV Fast Data > Fast Data
22|04|00 or 01|SET > DV/DD Set > DV Fast Data > GPS Data Speed
22|05|00 to 10|SET > DV/DD Set > DV Fast Data > TX Delay (PTT)
23|00|see p.23|Read the position status
23|01|00, 02, 03|GPS > GPS Set > GPS Select
23|02|see p.17|GPS > GPS Set > Manual Position
24|00 00|00 or 01|Send/read TX output power setting
24|00 01|00 or 01|Set the TX output power for transceive
25||see p.23: byte 00=Selected VFO 01=Unselected VFO + FREQ 5 bytes|Set the selected or unselected VFO frequency
26||see p.23: byte 00/01 + mode + data mode 00=OFF 01=ON + filter|Set the selected or unselected VFO's operating mode and filter
27|00|see p.24|Read the Scope waveform data
27|10|00 or 01|Send/read the Scope ON/OFF status
27|11|00 or 01|Send/read the Scope wave data output
27|12|00 or 01|Send/read the Main or Sub scope setting
27|14|see p.24|Send/read the Scope Center mode or Fixed mode setting
27|15|see p.24|Send/read Span setting in Center mode Scope
27|16|see p.24|Send/read Edge number setting in Fixed mode Scope
27|17|see p.24|Send/read Scope hold function ON/OFF status
27|19|see p.24|Send/read Scope Reference level setting
27|1A|see p.25|Send/read Sweep speed setting
27|1B|00 or 01|SCOPE > Scope during Tx (CENTER TYPE)
27|1C|00 to 02|SCOPE > CENTER Type Display
27|1D|see p.16 (as printed)|Send/read the Scope VBW setting
27|1E|see p.25|Send/read the Scope Fixed edge frequencies
28|00|00 to 08|Voice TX Memory
";
        }
    }
}
