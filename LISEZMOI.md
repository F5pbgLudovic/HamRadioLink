# HamRadioLink 1.15 — télécommande de postes radio

Programme Windows portable (un seul .exe, rien à installer) qui reproduit la face avant d'un poste radio et le pilote par CAT, comme si on était devant.
Projet indépendant : il ne dépend d aucun autre logiciel de pilotage de poste.
Même philosophie et même aspect que IC7760Link. Postes disponibles : **Yaesu** FT-2000, FTdx5000 (face avant complète), FTdx10, FT-450D, FT-710, FT-891, FT-950, FT-991A, FTdx101D/MP, FTdx3000 et **Kenwood** TS-480HX/SAT, TS-890S. Voir plus bas pour les Icom, Xiegu, Kenwood, Elecraft et les anciens Yaesu. Un **journal de trafic** est intégré (bouton LOG, voir `AIDE.md`).

## Lancer
1. Double-clic sur `HamRadioLink.exe`.
2. Menu **Poste** : choisir le poste.
3. En haut à droite, **Liaison** : choisir l'interface, le port, puis **CONNECTER**.

| Interface | Usage |
|---|---|
| Port COM (CAT direct) | Le cas normal : port COM, vitesse, bits de stop, RTS, DTR. RTS est coché par défaut (certains postes n'écoutent le CAT que s'il est actif). |
| CAT sur TCP (brut) | Les mêmes commandes CAT dans une connexion réseau (serveur série-réseau, port COM déporté). |
| Poste virtuel (simulateur) | Simule le poste choisi en appliquant strictement les formats des manuels CAT. Pour essayer sans matériel. |

Pour que WSJT-X, JTDX, JS8Call ou SDRLink pilotent le poste de HamRadioLink, activez le bouton **Serveur** (serveur de commande IP, port 7809) et choisissez dans ce logiciel le type de poste qui se connecte à un serveur IP (adresse 127.0.0.1, port 7809). Hamlib, OmniRig et FLRig ne sont pas proposés : leur jeu de commandes est réduit. **TCI** l'est pour les logiciels SDR (poste « Poste commandé en TCI », liaison TCI, port 40001). Seules les liaisons qui transportent toutes les commandes du poste sont gardées. Pour qu'un autre logiciel (OmniRig, log, WSJT-X) pilote le même poste, utilisez le bouton **Partage** (port COM virtuel et / ou serveur TCP) : HamRadioLink vérifie que **com0com** est présent, propose de l'installer (installateur signé embarqué, installation silencieuse), ne liste que les paires libres, en crée une au besoin et indique le port à choisir dans l'autre logiciel.

### Réglages du poste
* **FT-2000** : câble CAT RS-232 (ou interface USB↔série). Vitesse = menu **027 CAT RATE** (4800 par défaut, 38400 recommandé). **029 CAT RTS** : ENABLE. Le programme affiche un avertissement si le poste répond avec un autre ID (`ID0251;` attendu).
* **FTdx5000** : prise CAT RS-232 (pas d'USB sur ce poste ; interface USB-série possible). Vitesse = menu **CAT RATE** (4800 d'usine), **CAT RTS** actif. `ID0362;` attendu. Même famille que le FT-2000 (deux récepteurs), avec ses différences : préampli IPO 1 / AMP 1 / AMP 2 / IPO 2, roofing 15 k / 6 k / 3 k / 600 / 300 Hz, antennes 1 à 4 + ANT RX, verrouillage et pas rapide par VFO, délais VOX / BK de 20 à 5000 ms (pas de 20), menus **104** (TX BPF), **114** (largeur du notch), **174** (VOX micro/data), VRF, CLASS-A et niveau, décalage FM et CTCSS. Pas de bande 5 MHz.
  **Face avant FTdx5000** : en haut, le **galvanomètre à aiguille** (S-mètre en réception, fonction choisie par METER en émission : COMP / ALC / PO / SWR / ID / VDD), l'**afficheur VFD** (fréquence VFO-A, S-mètres, **schéma de principe** ANT / ATT / FLT / IPO / R.FLT / AGC de chaque récepteur, voyants BUSY / TX / FAST / LOCK / NAR / NB / SPLIT / MEM / ANT RX) et les **trois petits afficheurs OLED** (SUB DISPLAY-I : fréquence VFO-B ; II et III : passe-bande FI des VFO-A et VFO-B avec SHIFT, WIDTH en Hz selon le mode, NOTCH, CONTOUR, APF, DNF). En bas : VFO-A, accord / bandes / modes, VFO-B (ses propres AF / RF / SQL / NB / DNR / DNF / NOTCH / CONTOUR / ATT / IPO / R.FLT / AGC / ANT / VRF / SHIFT / WIDTH) et émission / CW. Les mesures d'émission (Po, SWR, ALC, COMP, Id, Vdd) sont lues pour l'aiguille et ne s'affichent en barres qu'en « vue config ».
* **FTdx10** : câble USB. Windows crée deux ports « Silicon Labs Dual CP210x » : utiliser le port **Enhanced** pour le CAT (le port Standard sert au PTT/CW). 38400 bauds par défaut (menu CAT RATE). `ID0761;` attendu.

## Les deux vues (menu **Vue**)
* **normal** : utilisation. Chaque bouton, potentiomètre ou affichage reproduit un organe de la face avant et envoie sa commande CAT. Potentiomètres : glisser verticalement, molette, double-clic = zéro (Maj = fin). Fréquence : clic en haut/bas d'un chiffre, molette, double-clic = saisie. Molette d'accord : glisser en rond ou molette, pas choisi dessous. Flèches ← → ↑ ↓ : pas / 1 kHz. Échap coupe l'émission.
* **vue config** :
  * tous les éléments se **déplacent à la souris** (le coin bas-droit change la taille). Un déplacement n est **enregistré que par « Valider »** ; **« Retour arrière »** annule le dernier déplacement (autant de fois que nécessaire) ; **« Réinit »** remet **tout** par défaut comme à l installation (positions, tailles et commandes CAT modifiées, après confirmation). En quittant la vue config, changeant de poste ou fermant avec des déplacements non validés, le programme demande : valider, abandonner ou rester ;
  * **clic droit** sur un élément : fenêtre avec la **commande CAT visible** (envoi, lecture, format de réponse, valeurs d'un choix, min/max d'un curseur). On peut tout modifier. Le bouton **Défaut** remet les valeurs d'origine. **Tester la lecture** envoie la lecture au poste et affiche la réponse ; la zone « Essai » vérifie qu'une réponse collée correspond au format ;
  * une pastille verte marque un élément dont la commande a été modifiée ; l'infobulle montre SET et READ.

Gabarits d'envoi : `{v}` valeur · `{v:3}` sur 3 chiffres · `{v:+4}` signe + 4 chiffres · `{p:RU:4:;}` / `{n:RD:4:;}` texte + 4 chiffres selon le signe · `{wait:1000}` pause en ms.
Format de réponse : expression régulière ; le groupe `v` est la valeur de l'élément, tout autre groupe nommé comme un élément (ex. `fa`, `md0`, `clarrx`) met aussi cet élément à jour (utilisé par `IF;`).
Fichiers personnels dans le dossier `data` : `<poste>.cat.ini` (commandes modifiées), `<poste>.layout.ini` (positions). Les supprimer = tout remettre par défaut.

**Moniteur CAT** : tous les échanges, plus une zone pour envoyer une commande à la main (le `;` est ajouté).

## Ce qui est lu et écrit
Fréquences A/B (clic sur le chiffre, molette, clavier), modes A/B, S-mètre, mesures émission (Po, SWR, ALC, COMP, Id, Vdd), PTT/MOX/VOX/TUNE, ATT/IPO/AGC/roofing, NB/DNR/DNF/NOTCH/CONTOUR/APF/SHIFT/WIDTH, CLAR (lu avec `IF;`), SPLIT/TXW/V-M/A-B, bandes, AF/RF/SQL, MIC/puissance/PROC/MONI/VOX, manip CW (keyer, vitesse, hauteur, mémoires KY, délais), mémoires vocales, commutateur METER. FT-2000 en plus : sub-récepteur complet (AF, RF, SQL, AGC, NB, NAR), réception dual, antenne, DIM, FAST, menus 085/094/148.
La liste exacte de toutes les commandes envoyées : `commandes-CAT.txt` (fabriqué par `HamRadioLink.exe --dump=fichier.txt`).
**Pas de spectre ni de cascade** : ces postes ne les envoient pas par CAT (FTdx10 : seulement via SCU-LAN10).

## Vérification (faite sans le poste)
`HamRadioLink.exe --selftest=rapport.txt` : **175 242 vérifications, toutes réussies** :
* chaque commande de chaque élément, avec chaque valeur (min / milieu / max, chaque choix, on/off), envoyée à un **poste simulé strict** écrit d'après les tableaux des manuels (nombre exact de chiffres, bornes, pas) : aucune commande refusée par `?;`, relecture identique à l'envoi ;
* le simulateur refuse bien les mauvais formats (FA sur 8 chiffres refusé par le FTdx10, sur 9 par le FT-2000, IS hors pas, ST/CF absents du FT-2000...) ;
* réponses → état (dont `IF;` : fréquence, mode, CLAR, V/M), réponses coupées en deux, alias (AGC AUTO = 4/5/6) ;
* session complète par la vraie chaîne (thread, retenue, rafale de potentiomètre, PTT, suivi d'un changement fait sur le poste, allumage PS1 deux fois avec pause) ;
* lecture refusée 3 fois = abandonnée sans bloquer le reste ; personnalisation, Défaut, positions ; fenêtre d'édition ouverte pour les 212 éléments ; partage du poste (texte, CI-V, 5 octets) avec un client TCP pendant que HamRadioLink interroge le poste ; anciens Yaesu à bloc d'état sur le fil simulé ; aucun élément ne se chevauche ni ne sort de la fenêtre.

### Sources croisées pour le FT-2000 et le FTdx10
1. Manuels CAT officiels : `FT-2000_CAT_Operation_reference_book` (EH025H121) et `FTDX10_CAT_OM_ENG_2308-F`.
2. Essais faits auparavant sur de vrais FT-2000 et FTdx10 (écriture, relecture, retour). Quand manuel et essai réel divergent, c est l essai réel qui a été retenu et la note est dans l éditeur (clic droit) :
   * FTdx10 **PROC** : le manuel dit `PR0` + 1 = OFF / 2 = ON, l'essai réel a validé `PR01` = marche, `PR00` = arrêt ;
   * FT-2000 **puissance** : le manuel imprime `PC` 000-255, l'essai réel 5-100 W ;
   * FT-2000 **SHIFT** : pas de 20 Hz ; FTdx10 **délais VOX/BK** : 2 chiffres (00-33 / 00-30) alors que le manuel en imprime 4 ;
   * FT-2000 **menus EX** 085 (TX BPF), 094 (largeur du notch), 148 (VOX micro/data) : numéros issus de l'essai réel.
3. Les valeurs de menus EX confirmées par l utilisateur sur son FT-2000 (NB 00/01/02, RA, EX085, EX094...).

### Pas encore vérifié sur le matériel
Seuls le FT-2000 et le FTdx10 ont été essayés par l'auteur sur de vrais postes ; tout le reste l'a été sur le simulateur seulement. Points à regarder en premier sur le poste, tous modifiables par clic droit :
* FTdx5000 (tout vient du manuel CAT EH036H121, rien n'est essayé sur le vrai poste) : menus EX104 / EX114 / EX174, VRF (`VF0` + ON/OFF + `+0`), CLASS-A (`CA0`), roofing 600 / 300 Hz (l'envoi et la réponse n'ont pas la même table), puissance `PC` (le manuel imprime 000-255 ; 5-100 conservé comme sur le FT-2000), verrouillage `LK` (réponse 4 à 7) ;
* FTdx10 : filtre de roofing `RF0` (valeurs 1/2/4/5, le manuel est ambigu), commutateur `MS` (METER), délais `VD`/`SD`, tuner (`AC002`), allumage `PS1;` (deux envois à 1,1 s d'écart — RS-232 : impossible, USB seulement) ;
* FT-2000 : lecture de `BP`, `CO`, `NB` (le manuel est contradictoire sur « lecture possible ») ; une lecture refusée 3 fois est abandonnée automatiquement, le message est dans le Moniteur CAT ;
* CLAR du FT-2000 : écriture par `RC;` puis `RU`/`RD`.

## Ajouter un autre poste
Dans `src\Radios.cs` : une fonction qui crée les éléments avec leurs commandes (voir `Ftdx10()`), un simulateur dans `src\Sim.cs` d'après le manuel, puis `--selftest`. Chaque élément est défini une seule fois (position par défaut, gabarit d'envoi, lecture, format de réponse).

## Compiler
`powershell -ExecutionPolicy Bypass -File build.ps1` (compilateur C# fourni avec Windows, rien à installer) → `dist\HamRadioLink.exe`.
Options : `--virtual`, `--radio=ft2000|ftdx5000|ftdx10|ft450|ft710|ft891|ft950|ft991a|ftdx101|ftdx3000|ts480|ts890`, `--data=dossier`, `--shot=image.png,ms,actions` (captures), `--selftest=rapport.txt`, `--dump=fichier.txt`.

## Autres Yaesu et Kenwood (FT-450D, FT-710, FT-891, FT-950, FT-991A, FTdx101D/MP, FTdx3000 ; TS-480, TS-890)
* **Même face avant que le FTdx10** pour les Yaesu : chaque élément dont la commande n'existe pas dans le **catalogue officiel du manuel CAT** du poste est retiré (liste dans le rapport de `--selftest`, lignes « NOTE »), le nombre de chiffres de la fréquence et les noms de modes sont adaptés (FT-450D / FT-950 / FTdx3000 : 8 chiffres). SPLIT passe par `FT` quand le poste n'a pas `ST`.
* **Kenwood** : face avant dédiée (TS-480 : S-mètre, SWR/COMP/ALC, RIT/XIT, NB, NR, beat canceller…; TS-890 : AGC `GC`, NB1, NOTCH, mode `OM0`/`OM1`, état émission annoncé par le poste avec `AI2;`).
* **Simulateur « piloté par catalogue »** (`src\CatSim.cs`) : il lit le catalogue du manuel (`catalogues\*.new.cat`, embarqué dans `src\Catalogs.cs`) et refuse toute commande inconnue ou dont le **nombre de caractères** ne correspond pas au format du manuel. **Les bornes (min/max) ne sont pas contrôlées** pour ces postes (le catalogue ne les contient pas). Réponses particulières Kenwood simulées (IF, SM, RM, BY, RA, PA).
* Source des catalogues : manuels CAT officiels, extraits dans `catalogues` (Yaesu, Kenwood) et `doc	v` (Icom CI-V). Deux erreurs de l extraction ont été corrigées (FB sur FT-950 et FTdx3000 : 8 chiffres).
* **À vérifier sur le vrai poste** : numéros des mesures `RM` des anciens modèles (le manuel n'est pas complet dans le catalogue), noms de modes des FT-450D/FT-950/FTdx3000 (basés sur la table commune), `ID` du TS-890S (024, non lisible dans l'extraction du manuel), plage de `PC` du TS-480HX (5 à 200 W), tables SH/SL du TS-480, baud par défaut (modifiable).

## Bouton « Ajouter » (vue config)
* Bouton **Ajouter** à côté de « vue config » : fenêtre où l'on choisit le **type** (bouton, touche allumé/éteint, potentiomètre, choix en rangée, vu-mètre, voyant), le **nom**, puis la **commande CAT** dans une liste de **toutes les commandes connues** (éléments de tous les postes, déjà prêts, puis catalogues des manuels Yaesu / Kenwood ; filtre de recherche). Pour une commande du catalogue le programme fabrique un modèle (dernier groupe de paramètres = valeur) à vérifier. L'élément est posé en haut à gauche, la fenêtre de clic droit s'ouvre pour l'ajuster, puis on le déplace.
* Comme les déplacements : **Valider** l'enregistre (`data\<poste>.custom.ini`), **Retour arrière** l'annule, **Réinit** supprime tous les éléments ajoutés. Clic droit sur un élément ajouté : bouton **Supprimer**.
* FTdx10 (et dérivés) : la touche **DNF** est maintenant sous **APF** dans le cadre DSP / FILTRES.

## Icom, Xiegu, Kenwood, Elecraft et autres Yaesu
- **Icom (CI-V)** : les 55 Icom de la liste de SDRLink (IC-7000 … IC-7851, IC-705/905/9700, IC-706, IC-746, IC-756, IC-775, séries 7xx et anciens à 1200 bauds) et 5 Xiegu (G90, X108G, X5105, X6100, X6200). La liaison binaire `FE FE <poste> E0 … FD` est assurée par le programme (menu Liaison, case **Adresse CI-V** si l'adresse du poste a été changée). Les fréquences vont en BCD sans que vous ayez à y penser.
- Chaque face avant est composée d'après la famille du modèle (récepteur double, tuner, DATA, RIT/XIT, manip CW...). Pour les 8 modèles dont le manuel CI-V est analysé (IC-7000, 7100, 7300, 7300MK2, 7410, 7610, 7760, 9700), chaque touche est vérifiée contre le catalogue du manuel et les touches inexistantes sont retirées (voir le rapport d'essai). Pour les autres, la face avant suit la famille et les commandes sont contrôlées contre l'ensemble des 8 manuels : **à vérifier sur le poste**, tout se corrige par clic droit en vue config.
- **Kenwood** : TS-590S/SG, TS-990S, TS-2000, TS-B2000, TS-870S, TS-570, TS-850S, TS-50S, TS-60S, TS-450S, TS-690S, TS-790, TS-930S, TS-940S, TS-950S/SDX, TS-440S, TS-140S, TS-680S, TS-711, TS-811 ; **Elecraft** K4, K3/K3S, KX3, KX2, K2. Seuls les manuels du TS-480 et du TS-890 sont analysés : ces postes reprennent leur face avant (réduite pour les anciens) et leurs commandes. Non vérifiés sur manuel.
- **Yaesu** en plus : FTdx1200, FTdx9000, FTX-1 (catalogues voisins).
- **Ajouter** : la liste ne propose plus que les commandes du poste choisi (ses éléments, puis le catalogue de son manuel).
- **Fermeture en vue config** : si des modifications ne sont pas enregistrées, le programme demande (Oui / Non / Annuler).
- **Correctif** : cliquer dans un grand cadre en vue config ne fait plus disparaître les boutons.
- **Nouvelle icône** (mini-récepteur) et **mises à jour dans `dist`** : l'ancien exe est renommé `HamRadioLink-<version>.exe`.

## Yaesu à protocole binaire (FT-817 / 818 / 857 / 897 en 5 octets ; anciens modèles à bloc d'état)
- Le moteur reste textuel ; `Y5Link` (src\YaesuBin.cs) traduit : `FAnnnnnnnnn;` (Hz, multiple de 10) → `P1..P4 = BCD en pas de 10 Hz`, CMD 01 ; `MDxx;` → CMD 07 ; `TX1/TX0` → 08 / 88 ; `LK` → 00 / 80 ; `SP` → 02 / 82 ; `CL` → 05 / 85 ; `VS;` → 81. Lectures : `FA;` `MD;` → CMD 03 (5 octets : fréquence + mode) ; `SM;` `BY;` → E7 (1 octet : S-mètre, squelch) ; `TX;` `PO;` `SW;` → F7 (1 octet : émission, puissance, SWR élevé). Les réponses sont rattachées à la commande en attente ; une réponse qui ne vient pas est abandonnée après 1,8 s.
- Vérifié : codage des trames, session complète contre un poste simulé sur fil binaire, mêmes contrôles que les autres postes.
- **Point faible** : aucun manuel CAT de ces postes n'était disponible ; le jeu de base vient de la connaissance du protocole. Le sens exact des bits des octets E7 / F7 (squelch, émission, SWR élevé) est à confirmer sur le poste. 2 bits de stop, 4800 / 9600 / 38400 bauds selon le menu CAT RATE.
- Les anciens FT-847, FT-650, FT-100, FT-600, FT-736R, FT-747 à FT-1000 utilisent des protocoles à bloc d'état repris de Hamlib, **sans manuel vérifié**.

## Icom IC-7760 (identique à IC7760Link)
Le poste **Icom IC-7760** reprend à l'identique la fenêtre d'IC7760Link (cascade, vumètres, commandes, export/import ⇅, options ⚙, monitoring CI-V). Les autres postes gardent la face avant habituelle. Réglages de cette vue : `data\ic7760.ini`.

## Vumètres, verrous, paramètres, export / import pour tous les postes
Vumètres barres/aiguille (clic droit), verrous (clic droit, cadre rouge), fenêtre **Paramètres ⚙** (couleurs, vumètres, molette, verrous, commande manuelle) et **Export/import ⇅** (réglages, dispositions, réglages du poste). Le poste virtuel est disponible pour tous les postes.

## Bouton TUNE (1.1)
En bas à droite de la face avant : un clic émet une tonalité de 1500 Hz sur la carte son d'émission du poste (choisie dans Paramètres ⚙ > Micro vers le poste) avec le PTT par CAT ou le VOX du poste, 5 à 60 s ; second clic = arrêt. Niveau, durée, VOX et couleurs : Paramètres ⚙ > Bouton TUNE. Détail : `AIDE.md` (français), `HELP.md` (English).
