# Historique des versions

## 1.11 — 2026-11-23

**Égaliseur audio (émission et réception)**
- Toujours 10 curseurs, mais le spectre s'arrête à **10 kHz** : 50, 100, 200, 400, 800 Hz, 1,6, 3,15, 5, 7, 10 kHz (courbe de 20 Hz à 12 kHz). Préréglages réécrits. Les anciens réglages de bandes ne sont pas repris (clés `fx.*.h0..h9`), l'égaliseur repart à plat.
- **Échelle corrigée** : monter un curseur = gain (+), le descendre = atténuation (l'ancien sens était inversé).

## 1.10 — 2026-11-23

**Noise gate à l'émission** (fenêtre de l'égaliseur audio, onglet Émission, sous le compresseur) : coupe le bruit entre les mots. Réglages : seuil, atténuation, attaque, maintien, relâche. Ordre de la chaîne d'émission : noise gate, égaliseur 10 bandes, compresseur / limiteur. Essai `--fxtest` complété (signal sous / sur le seuil).

## 1.9 — 2026-11-23

**Égaliseur audio 10 bandes sur le PC (émission et réception)**
- **Émission** : clic droit sur PTT > « Égaliseur 10 bandes + compresseur / limiteur (émission)… ». S'applique au micro envoyé au poste (case « Micro du PC vers le poste »). Égaliseur 31 Hz à 16 kHz (±12 dB) puis compresseur / limiteur (seuil, rapport, gain de sortie, plafond, attaque, relâche) avec affichage de la réduction de gain.
- **Réception** : clic droit sur le volume AF (ou sur PTT) > « Égaliseur 10 bandes (réception)… ». S'applique au son du poste rejoué sur le PC (case « Écouter la réception du poste sur le PC »).
- Une seule fenêtre non bloquante (choix Émission / Réception), courbe de réponse en direct, préréglages (voix claire, graves coupés, chaleureux, aigus adoucis, DX), clic droit sur un curseur = remise à 0. Réglages mémorisés (fx.tx.* / fx.rx.*), communs à tous les postes.
- Ne change rien au micro branché directement sur le poste ni au haut-parleur du poste.
- Essai automatique `--fxtest=dossier` (gains mesurés sur sinus, compresseur, limiteur, fenêtre).


## 1.8 — 2026-11-23

**FTdx10 (et autres postes) : nouvelles fenêtres et réglages**
- **Touche MENU** (entre METER et TUNE) : fenêtre non bloquante qui reprend l'organisation du menu du poste (page MENU RAPIDE, puis RADIO / CW / OPERATION / DISPLAY SETTING et leurs sous-pages, 197 lignes d'après le manuel CAT). Valeurs lues dans le poste, modifiables, relues toutes les 5 s ; un refus du poste est annoncé par un bandeau rouge. Hauteur maximale de l'écran.
- **Égaliseur micro** (clic droit sur MIC EQ) : trois bandes (fréquence, gain, largeur), jeu « PROC éteint / allumé », filtre d'émission SSB, courbe de correction en direct (lignes ou barres), « Restore old menu settings ». Constaté : le poste refuse par CAT les gains sous −10 dB (défaut connu du firmware) ; la fenêtre affiche tout de même les valeurs −11 à −20 réglées sur le poste et signale le refus.
- **NB** (clic droit) : niveau, largeur et réjection ; **DNR** (clic droit) : niveau. Valeurs réelles lues à l'ouverture.
- **VOX** (clic droit) : source micro / data ; **MOX** (clic droit) : mêmes réglages que PTT (délai de sécurité) ; **NOTCH** : clic droit = largeur large / étroite (affichée sur la touche) ; clic gauche sur le mode du VFO B = liste des modes ; **bouton 70** (début de la bande 70 MHz, remplace GEN) ; bouton **PAS** : clic gauche = suivant, clic droit = précédent, Ctrl + clic droit = verrou ; « NAR » aligné sur la ligne du mode.
- **CLAR RX et CLAR TX indépendants** (le poste n'a qu'un décalage : le programme garde les deux positions et envoie celle du bouton tourné), touche **CLEAR CLAR**, molette rapide = pas de 100 Hz.
- **Grand S-mètre à aiguille** (FTdx10 et FT-2000, ajout possible à tout poste par le bouton « S-mètre » de la vue config) : fond = image de l'afficheur pour la fonction de METER (PO, COMP, ALC, VDD, ID, SWR), aiguille redessinée (réception : signal ; émission : mesure choisie). Clic droit sur un vumètre de gauche = le remplacer par le S-mètre (et inversement), clic gauche = fonction suivante. Les retouches de l'aiguille de repos sont dessinées à la main.
- **Étalonnage du ROS** (roue crantée > Alerte ROS, tout en bas, tous les postes) : plusieurs mesures ou « Passer », extrapolation ; l'alerte ROS et les vumètres l'utilisent. Étalonnage de WIDTH tous les 100 Hz (200 à 4000 Hz).
- **Au démarrage** : tant que tous les paramètres du poste ne sont pas lus, aucune commande n'est envoyée (statut « Lecture des paramètres… n/N ») ; les positions de la dernière session s'affichent tout de suite (fichier `etat-<poste>.txt`). Une lecture refusée est retentée toutes les 15 s.
- **FT8 / PROC** : l'état d'avant FT8 (PROC, niveau de PROC, MIC EQ, mode) est mémorisé sur disque, même si le programme est arrêté pendant FT8 ; le bouton FT8 s'affiche enfoncé au redémarrage. Constaté sur le FTdx10 : PROC coupé, le poste lit 000 et revient à son niveau de menu s'il n'est pas réécrit.
- **Dispositions par défaut intégrées** : FTdx10 (rangée ATT à MODE VFO-B alignée et centrée, CLAR sous NOTCH/CONT…) et FT-2000 (rangée ATT à NAR B centrée dans « ÉCRAN », cinq potentiomètres rentrés dans leur cadre) ; étalonnages FT-2000 et FTdx10 intégrés (plus de fichier à part).

**Journal**
- **Fenêtre Add** : RST proposés par mode (phonie 51 à 59 puis 59+10 à 59+60, CW et modes à clavier 519 à 599, modes WSJT en dB, SSTV en RSV) ; thème propre à la fenêtre ; libellés et champs DXCC / CQ / ITU recadrés ; boutons à coins lissés dans tous les thèmes arrondis.
- **Sauvegarde / restauration de la base** (Fichier) : fichier .sql daté par journal, dossier au choix, nombre de copies conservé, sauvegarde automatique (fermeture / jour / semaine / mois, désactivable) ; restauration depuis le dossier des sauvegardes ou un autre chemin, avec sauvegarde préalable. « Moteur MariaDB » masqué hors moteur intégré.
- **Fichier** réorganisé ; « Importer un ADIF » demande le journal quand il y en a plusieurs ; **journal de destination des QSO reçus** réglable dans Réglages des ports ; touche loupe carrée ; raccourcis des menus affichés dans leur colonne.
- **Services en ligne** : case « Ne pas transmettre les commentaires » (tous les envois).

**Divers** : correction des étapes d'étalonnage de CONT et RF PWR (supprimées par erreur pendant le développement) ; essais automatiques élargis (cohérence des 126 postes, ROS, S-mètre).
## 1.7 — 2026-11-23

- **Club Log : envoi avec un jeton personnel** : Club Log ne demande plus de clé API de logiciel (ni d'e-mail ni de mot de passe). Dans **Outils > Online services**, saisir son **indicatif** et son **jeton** « cl_… » (à créer sur clublog.org > Settings > API > Create token, affiché une seule fois). Les QSO partent par lots de 50 vers l'API REST de Club Log ; les doublons sont comptés, un jeton refusé (401) ou un indicatif étranger au compte (403) arrête l'envoi. Non essayé avec un vrai compte.
- **Volume de l'écoute sur le PC** : quand « Couper le haut-parleur du poste » est actif, le bouton de volume (AF) règle le volume du son joué sur le PC ; le poste reste à 0 et retrouve son volume d'origine à la désactivation ou à l'arrêt du programme.
- **Réception du poste sur le PC** : clic droit sur le bouton de volume (AF) d'un poste > Écouter la réception sur le PC, carte son du poste, « Couper le haut-parleur du poste (le son reste sur le PC) ». Non essayé sur un vrai poste.
- **Loupe de recherche** dans la barre d'outils, après « Refaire » (boutons « Début », « Précédent », « Suivant » ; une date absente mène à la date la plus proche) : fenêtre non bloquante avec le choix du champ (date du QSO, indicatif, texte libre : nom, QTH, commentaire…), la valeur, et « Précédent » / « Suivant » qui montent et descendent dans le tableau (ordre et filtre courants, retour au début/à la fin signalé ; Entrée = suivant, Maj+Entrée = précédent).
- **Largeur des colonnes réglable sur toute la hauteur** (bord droit d'une colonne saisissable dans les lignes, pas seulement dans l'en-tête) ; déplacement visuel des colonnes (la colonne suit la souris, les autres s'écartent).
- **Un seul envoi à la fois** : un envoi automatique qui tombe pendant un envoi manuel (ou l'inverse) attend son tour (retenté toutes les 15 s côté automatique, message d'attente côté manuel), sans conflit ni doublon.
- **Colonnes du journal déplaçables** : maintenir le clic sur un en-tête et glisser (la colonne entière suit la souris, les autres s'écartent) ; l'ordre est enregistré et retrouvé au démarrage (un simple clic sur l'en-tête trie la liste, sans rien modifier).
- **Fenêtres du journal non bloquantes** : réglages (Station, DX Cluster, Alerte, Langue, Ports, Moteur, Base, Importer / partager), Envoi automatique et « Envoyer à tous » s'ouvrent sans bloquer le journal ; une seule fenêtre de chaque sorte à la fois. Les questions rapides (Renommer, Supprimer, filtres, fichiers) restent modales.
- **Bouton « Alertes » (panneau danger)** juste après le « ? » de la fenêtre de commande, pour tous les postes : éteint s'il n'y a pas de nouveau message, allumé (rouge) sinon. Un clic ouvre les messages horodatés (perte de liaison, alerte ROS avec la valeur du ROS détecté, carte son indisponible…), la plus récente en haut ; OK en bas, et « Effacer » à côté. Consulter les messages éteint le bouton ; ils sont conservés 7 jours puis effacés automatiquement (fichier alertes.txt du dossier data).
- **World Radio League** (clé API : Integrations > Developer API ; envoi d'un fichier ADIF en 3 étapes), avec sa colonne « WRL sent », ses entrées de menu, les boutons « Demander une clé » et « Aide ».
- **Envoyer à tous les logs en ligne** ouvre une fenêtre : services avec coche (les non réglés sont grisés), date de départ (jusqu'à aujourd'hui), option « ignorer les QSO déjà envoyés », option « seulement la sélection ».
- **Outils > Envoi automatique** : envoi automatique des nouveaux contacts (saisis ou reçus de WSJT-X…), services cochés, délai au choix (instantané, 30 min, 1 h, 2 h, à la fermeture du log). La mémoire (dernier contact traité, heure du dernier envoi) est dans settings.ini : un redémarrage reprend l'envoi interrompu sans repartir du début du journal. Non essayé avec de vrais comptes.
- **Nouveaux services d'envoi : HRDLog.net, QRZCQ.com, HamQTH** (réglages dans Online services, menus Envoyer/Marquer, colonnes « HRDLog sent », « QRZCQ sent », « HamQTH sent » comme Club Log). Non essayés sur de vrais comptes ; le format HRDLog n'est pas publié par le service (à confirmer).
- **Fenêtre d'envoi** : barre de progression propre à chaque service (sur ses seuls QSO à envoyer) et question de confirmation avant d'interrompre des transferts en cours.
- **Envois en ligne par sessions de 100 QSO** (tous les services, actuels et futurs) : la liste est découpée en séries de 100 indicatifs, avec 3 s de pause entre deux sessions, pour respecter les limites des services en ligne ; l'annulation reste possible pendant la pause. Boutons **Demander un jeton** et **Aide** (non bloquants) dans Online services.

## 1.6 — 2026-11-21

- **Dispositions de postes intégrées à l'exe** : FT-991A DF5DP (positions des touches et libellé « Signal »), FTdx10 (SHIFT, WIDTH, NOTCH F), FTdx5000 (FA, MD1), FT-2000 (TUNE MIC) ne dépendent plus de fichiers du dossier data ; ce sont les dispositions par défaut partout.
- **Correction FT-991A / FT-991A DF5DP inversés** : au premier démarrage, les anciens fichiers data\ft991a*.layout.ini / .cat.ini (qui pouvaient être inversés) sont rangés une seule fois dans data\ancien-ft991a. Vos modifications ultérieures (Valider) restent enregistrées dans data comme avant, seules les différences avec la disposition par défaut.

## 1.5 — 2026-11-21

- **Journal (LOG) traduit en 23 langues** (les mêmes que SDRLink ; français par défaut) : **Configuration > Langue / Language...**, appliquée à la réouverture du journal. Seul le journal est traduit (pas la télécommande). Traductions non relues par des locuteurs natifs.
- **Cluster** : liste déroulante **5 à 30 min** (pas de 5) à côté de « Auto » : durée d'affichage des spots près de la réglette.
- **Thèmes** : 13 nouveaux (n°41 à 53, modernes : Tokyo Night, Catppuccin, Monokai, Gruvbox, One Dark, Rosé Pine…) ; **Thèmes** ouvre une fenêtre qui reste ouverte (changement immédiat à chaque clic, molette, ascenseur, bouton « Personnaliser » toujours visible ; clic ailleurs ou Échap = fermer).
- **Changer la bande / fréquence** de plusieurs QSO sélectionnés (clic droit) : FT8 = fréquence FT8 de la bande, autres modes = début de bande (160 m à 70 cm).
- **Annuler / Refaire** : boutons ↶ ↷ après ↔ (Ctrl+Z / Ctrl+Y), 50 opérations (ajout, modification, suppression).
- **Tableau** : Ctrl+C copier, Ctrl+X couper, Ctrl+V coller des lignes, Ctrl+A tout sélectionner ; **Ctrl+N** ajoute un QSO (avant : Ctrl+A). **Entrée** valide la fenêtre d'ajout.
- Menu Édition : Couper / Coller en français.
- **Serveur de commande** : les ordres « set » (fréquence, mode…) sont refusés (ERROR) quand le poste n'est pas connecté ; le logiciel client le sait et le poste garde sa dernière fréquence. Écrit dans le journal des commandes.
- Test du journal : annulation / refaire, langues (36 vérifications, à lancer à part avec un moteur MariaDB : `HRL_ONLY=journal HRL_ENGINE=<dossier MariaDB>`).

## 1.4 — 2026-11-21

- **FTdx10** : bouton **MIC EQ** (égaliseur du micro, commande PR1) ; la touche **PROC** retrouve son niveau quand on la rallume (aussi sur les FT-2000 / FTdx5000 et la famille FTdx10) ; bouton **FT8/FT4...** dans la grille des modes, à droite de DATA-FM-N : passe en USB, coupe PROC et MIC EQ s'ils étaient actifs, puis au retour (clic de nouveau, ou choix d'un autre mode) remet le mode d'avant, PROC (avec son niveau) et MIC EQ. Les ordres partent à la suite avec 350 ms entre chacun (le poste en laissait tomber). Le bouton prend la case libre de la grille sur tout poste Yaesu dont le nombre de modes est impair ; pas de bouton s'il n'y a pas de case libre (FT-991A : 14 modes).
- **Nouveau poste « Yaesu FT-991A DF5DP »** : copie conforme du FT-991A avec ses propres fichiers (`data\ft991a-df5dp.*`) pour deux affichages distincts ; clic droit sur CTCSS ENC / FREQ CTCSS / CODE DCS = liste de toutes les valeurs, Ctrl + clic droit = verrouiller.
- **Fréquence sur 9 chiffres** pour les postes qui montent au-delà de 100 MHz (145.600.000 s'affichait 14.560.000).
- **Listes longues** (plus de 20 valeurs : CTCSS, DCS…) : fenêtre avec ascenseur et molette au clic droit.
- **Bouton TUNE** : case « Changer la puissance pendant le TUNE, puis la remettre » (Paramètres > TUNE, tous les postes) : la puissance de consigne est envoyée tout de suite, le TUNE démarre 0,7 s après, la puissance d'avant est remise à la fin ou à l'arrêt.
- **Création de paire com0com** : en cas d'échec, la réponse de com0com est écrite dans le journal et affichée avec des pistes pour Windows 11 (intégrité de la mémoire, pilote non autorisé).
- Simulateur de test : PR1 ; test du bouton MENU : 7 postes.

## 1.3 — 2026-11-20

- **FT-991A** (remarques d'un utilisateur) : boutons de bande **144** et **430 MHz** ; potentiomètre **CLAR** (RC / RU / RD, par pas de 10 Hz, lu avec IF) ; potentiomètre **DT GAIN** (menu 073) ; bouton **MIC EQ** (PR1) ; **CTCSS / DCS** (mode, fréquence CTCSS 67,0 à 254,1 Hz, code DCS) ; **C4FM TX (AMS)** (menu 090). Le reste du C4FM (GM, écran propre) n'a pas de commande CAT : non reproduit. Le potentiomètre CLAR par RC / RU / RD est aussi rendu aux autres postes dont le catalogue n'a pas la commande CF.

## 1.2 — 2026-11-20

- **Étendu à tous les postes** : échelles des vumètres d'émission (Po en watts, SWR 1 à ∞, ALC bleu / rouge, COMP en dB, Id en A, Vdd en V) avec puissance max par poste, étalonnage du wattmètre, des autres vumètres, de Width / CONT / RF PWR (Paramètres), bouton **Aligner** et bloc LIAISON masqué en vue config, zoom du texte du LOG, **menu interne du poste** (bouton MENU, commande EX) pour les 6 postes Yaesu qui le permettent (FT-2000 : 149 menus ; FT-950, FT-991A, FTdx3000 : noms et valeurs possibles du manuel ; FT-450, FTdx5000 : numéros seuls), avec bandeau rouge d'avertissement.
- **Réglages d'origine du FT-2000 / FT-2000D** : disposition des boutons, étalonnages (Po, RF PWR, SWR, ALC, Id, Vdd, Width), puissance max 200 W avec pleine déviation Po à 100 W, noms de menus relevés sur le poste, délai de sécurité TX de 30 s. Réglages d'origine du programme : alerte ROS (seuil 2, délai 0,3 s, bips 50 ms, chute de puissance à 0 %, test en VOX), TUNE à 15 % en VOX, maintien des vumètres 10 s, molette 1 %.
- **Mise à jour** (Paramètres ⚙ > Mise à jour) : case « Mise à jour automatique » (au démarrage, vérifie la dernière version GitHub ; silence si à jour, sinon propose) et bouton « Vérifier et mettre à jour maintenant ». Un clic suffit : copie de sécurité des réglages (datasauvegarde-avant-MAJ-…), téléchargement, remplacement du programme, redémarrage. Seul l'exe est remplacé : réglages, dispositions, commandes et journal restent en place ; cases pour les conserver ou repartir des valeurs d'origine ; l'ancien programme est gardé sous HamRadioLink-<version>.exe.
- **Alerte ROS** : elle s'éteint toute seule quand le ROS repasse sous le seuil (ou que l'émission s'arrête), après un délai réglable (0,5 à 30 s, 1,5 s par défaut) ; la puissance est alors rétablie à sa valeur nominale, sauf si la case « Laisser la puissance réduite de sécurité » est cochée ; un message l'indique. Le bouton « Test : TUNE 1500 Hz à la puissance de sécurité » devient rouge pendant l'émission. Le pourcentage de puissance de sécurité se règle avec une réglette de 0 % (minimum du poste) à 100 % (puissance réglée), linéaire entre les deux, et est appliqué aussitôt au poste, même en pleine alerte ou pendant le test d'émission.
- **Réglage S-mètre puissance** (Paramètres, tous les postes ayant ces vumètres ; le FT-2000 garde comme réglages d'origine l'étalonnage relevé : 200 W, échelle Po à 50 W) : puissance maximale de l'émetteur et étalonnage du wattmètre palier par palier (10, 20, … jusqu'à la puissance max, 200 W maximum) ; échelles des vumètres : Po en watts jusqu'à la puissance max, SWR de 1 à ∞ (3 au milieu), ALC bleu puis rouge à partir du milieu, COMP 0 / 10 / 30 dB, Id jusqu'à 15 A, Vdd jusqu'à 50 V (200 W) ou 15 V (nominal 13,8 V). La pleine déviation de l'échelle Po peut être choisie plus petite que la puissance max (par ex. 50 W sur un poste 200 W) sans refaire l'étalonnage. **Réglage autres S-mètres** : étalonnage de SWR, ALC, COMP, Id et Vdd (valeur réelle lue sur le poste saisie à chaque point, échelle extrapolée ; le ROS est interpolé sur le coefficient de réflexion). **Réglage Width / RF PWR** (FT-2000) : le bouton WIDTH affiche la largeur de filtre en Hz (position 1 = 200 Hz, 16 = 2400 Hz, 31 = 4000 Hz, étalonnable de 200 en 200 Hz) et le bouton RF PWR la puissance en W (étalonnage de 10 W à la puissance max, pleine échelle du bouton = puissance max). Quand la pleine déviation de l'échelle Po est inférieure à la puissance max, le bouton RF PWR prend cette valeur comme maximum (au-delà il reste au maximum). Width : 3 points seulement (200 Hz, 2400 Hz, 4000 Hz), les valeurs intermédiaires sont extrapolées et la largeur affichée ne dépasse jamais 4000 Hz. Le bouton CLAR se règle à la souris de 10 Hz en 10 Hz (3 pixels de glissement ou un cran de molette par pas). Le FT-2000 / FT-2000D a pour réglages d'origine l'étalonnage validé (vumètres, Po, RF PWR, Width). **Vue config** : le bloc LIAISON est masqué pour gagner de la place, et un bouton **Aligner** donne la même taille aux boutons (et aux potentiomètres) de même forme d'une section, de taille très proche (centre conservé, « Retour arrière » pour annuler). Le FT-2000 / FT-2000D a pour disposition d'origine celle validée sur le poste (boutons CLAR, NOTCH, CONT, DNF, DUAL, NAR, TUNER, VOX, MOX, potentiomètres du bas). **Réglage CONT** (FT-2000) : le bouton CONT affiche une fréquence en Hz (étalonnage en 3 points : 100 Hz tout à gauche, 2000 Hz au milieu, 4000 Hz tout à droite), dans la rubrique « Réglage Width / CONT / RF PWR ». **LOG** : 12 nouveaux thèmes d'affichage (n°28 à 40), bouton **QRZCQ** (ouvre la fiche du contact sur qrzcq.com, comme QRZ.com), boutons ronds − / + de zoom du texte, avec un réglage séparé pour la liste des contacts (tout à droite de la ligne des boutons), la réglette du cluster à droite et les trames du cluster en bas (après Cluster / Alerte). **LOG** : toutes les colonnes de la liste se redimensionnent à la souris (largeurs mémorisées), bouton **↔** après la flèche ▲ pour ajuster chaque colonne à son contenu, et les colonnes s'écartent avec le zoom du texte. Un bouton rond **↺** (réinit) à côté de − et + remet à 100 % le texte de toutes les zones du LOG. **Menu du FT-2000** : bouton MENU à droite de METER (même taille) ; fenêtre listant les menus 001 à 149 avec leur nom (manuel CAT) et leur valeur lue dans le poste (commande EX), modifiable (relecture de contrôle, confirmation pour les menus critiques), téléchargement des valeurs à la première connexion, clic droit pour modifier un nom ou définir les valeurs possibles. Dans la fenêtre Menu du FT-2000, les valeurs possibles de chacun des 149 menus viennent du manuel (colonne « Valeurs possibles ») ; pour un menu à liste, un clic sur la valeur ouvre la liste (FT-2000D : puissances 20/50/100/200 W) ; la signification de la valeur lue s'affiche, et le clic droit permet de corriger ou rétablir ces listes. La fenêtre Menu s'ouvre en 1366 x 700 (taille relevée sur la fenêtre de l'utilisateur), réduite si l'écran est plus petit. Un bandeau rouge d'avertissement en haut de la fenêtre Menu rappelle que chaque valeur modifiée est envoyée immédiatement au poste.

- **Journal (LOG) résident** : démarré avec le programme, fenêtre masquée ; la croix de la fenêtre la masque seulement, le bouton LOG la rouvre ; il reçoit toujours les QSO des logiciels (WSJT-WWA…) et le cluster tourne. Le programme se ferme avec la fenêtre de télécommande.
- **Boutons de fenêtre** (réduire / agrandir / fermer) qui disparaissaient en réduisant une fenêtre : cadre redessiné à chaque redimensionnement.
- **Export / import d'un seul poste** (⇅) : disposition, commandes et réglages d'affichage d'un poste, réimportables seuls ; le poste est reconnu automatiquement d'après le fichier, avec un avertissement qui nomme le poste écrasé.
- **Paramètres > Vumètres** : temps de maintien de la valeur maximale (0,5 s, puis 1 à 20 s) ; le repère et la valeur lue restent affichés ce temps-là.
- **Alerte ROS** (Paramètres ⚙ > Alerte ROS) : en émission, si le ROS lu sur le poste dépasse le seuil (3 par défaut, réglable) après un délai de 0 à 10 s (pas de 0,05 s), bips continus (2000 Hz, volume max, toutes les 0,25 s, réglables) jusqu'à l'arrêt par l'opérateur ; bandeau « ALERTE ROS » rouge clignotant sous le message d'état ; option : faire chuter la puissance à un % de la puissance réglée, avec bouton de test (TUNE 1500 Hz à la puissance de sécurité, en PTT ou en VOX). Le ROS est déduit de la lecture du poste (table du manuel Icom, approximative).
- **Message d'état** : renvoi à la ligne (plus caché par LIAISON) ; clic droit : ignorer l'avertissement « le poste répond un autre identifiant » ou les « ?; reçus » pour ce poste, les réafficher, copier. FT-2000D (ID 0252) accepté pour le FT-2000.
- **Vue config** (ex « config vue ») : avertissement à l'entrée ; clic droit : *Nom affiché* devient un menu déroulant de toutes les commandes (face avant + menus) qui remplit les champs ; S-mètre : échelle S et case agrandie.

## 1.1 — 2026-11-20

### Bouton TUNE
- **Bouton TUNE sur les 125 postes**, en bas à droite de la face avant (190 × 62, déplaçable en vue de configuration) : tonalité de 1500 Hz envoyée sur la carte son d'émission du poste (celle de « Micro vers le poste »), PTT par CAT ou VOX du poste, 5 à 60 s ; « ● TUNE » et compte à rebours pendant l'émission, second clic = arrêt (arrêt aussi si le poste est déconnecté).
- **Paramètres ⚙ > Bouton TUNE** : niveau (1 à 100 %), durée, case « VOX du poste », couleur au repos (vert) et pendant le TUNE (rouge), retour aux couleurs par défaut.

### Connexion
- Quand le poste ne répond plus (« Plus de réponse du poste »), le bouton passe à **CONNECTER** sur fond rouge ; un clic relance la connexion ; si le poste répond de nouveau, il redevient « DÉCONNECTER ».

### Vérification
- Autotest : 163 753 vérifications (bouton TUNE présent et dans la fenêtre pour chaque poste). Essayé en réel par l utilisateur : TUNE sur FT-2000, OK (autres postes non essayés).

## 1.0 — 2026-11-19 (première version publiée)

HamRadioLink 1.0 : télécommande de postes radio (face avant reproduite, pilotage CAT), serveur de commande IP, partage du poste, TCI, et **journal de trafic** intégré (bouton LOG).

### Postes et liaisons
- **125 postes** : Yaesu (36), Icom (55, CI-V), Kenwood (23), Elecraft (5), Xiegu (5) et « Poste commandé en TCI ». Face avant composée selon la famille du modèle ; pour les modèles dont le manuel CAT est analysé, les touches sont vérifiées contre le catalogue et retirées si absentes. FT-2000, FTdx10 et FTdx5000 (galvanomètre, afficheur VFD, trois afficheurs OLED) : faces avant complètes.
- **Yaesu à protocole binaire** : FT-817 / 818 / 857 / 897 (5 octets), et d'anciens modèles à bloc d'état (FT-847, FT-650, FT-100, FT-600, FT-747GX, FT-757GX, FT-767GX, FT-736R, FT-840 / 890 / 900 / 920, FT-990, FT-1000 / D / MP / Mark-V, d'après Hamlib, **sans manuel vérifié**).
- **Icom IC-7760** : même fenêtre que le projet IC7760Link (cascade, spectre, vumètres, CW / voix), réglages dans `data\ic7760.ini`.
- **Liaisons** qui transportent toutes les commandes du poste : port COM, CAT sur TCP, poste virtuel (simulateur strict), **TCI** (ExpertSDR3 / SunSDR, Thetis : VFO A / B, mode, PTT, TUNE, S-mètre, DRIVE, volume, MUTE, SPLIT, RIT).
- **Plusieurs postes connectés en même temps** (une coche par poste, deux icônes de scission, liaison propre à chaque poste, boutons de postes mémorisés, connexion automatique par clic droit sur CONNECTER). Bouton CONNECTER vert / rouge.
- Liste des postes triée par ordre alphabétique ; bouton **Ajouter** (nouveaux éléments à partir des catalogues des manuels) ; éditeur de commande par clic droit (config vue), moniteur CAT, poste virtuel.

### Partage et serveur
- **Partage du poste** (par poste) : port COM virtuel et / ou serveur TCP pour OmniRig, un logiciel de log, WSJT-X… ; installateur signé **com0com 3.0.0.0** embarqué, liste de toutes les paires avec leur état, création et suppression d'une paire.
- **Serveur de commande IP** (port 7809, second serveur possible) : WSJT-X, JTDX, JS8Call, SDRLink… pilotent le poste choisi, quel que soit son modèle. Noms de poste à espaces, liste des modes « Main Mode » / « Mode A », réglages protégés pour l'accès simultané, poste du serveur mémorisé, verrous et touche Lock = interface seulement.
- **Journal des commandes** : un fichier par jour (`logs\hamradiolink-AAAA-MM-JJ.log`), compact ou détaillé, purgé après 7 jours.

### Interface
- Titre « HamRadioLink 1.0 @F5PBG 2026 - poste ». Touche **PTT** verte / rouge avec **délai de sécurité TX** (clic droit). **Verrous** par clic droit (voile rouge réglable), touche **Lock**, vumètres barres ↔ aiguille, **Paramètres ⚙** (couleurs, molette, verrous, commande manuelle), **Export / import ⇅** (réglages, dispositions, réglages du poste).

### Export / import par parties et corrections des commandes
- **Export / import ⇅ par parties** pour tous les postes : logiciel et dispositions, réglages du poste, **menu interne du poste** (Yaesu FT-2000, FTdx5000, FT-450D, FT-950, FT-991A, FTdx3000) ; import sans redémarrage pour les réglages du poste. Fenêtre de l'**IC-7760** à jour de la version 2.9 d'IC7760Link (export du menu interne, mémoires, import à cases) et bouton **LOG** présent aussi sur ce poste.
- **Commandes contrôlées contre les manuels CAT officiels** : split du FT-2000 (FT2 / FT3), FT-450D, FT-950, FTdx3000, FTdx1200, FT-891, FT-991A, FT-710, FTdx101 (modes, AGC, split, mesures, délais, bornes, bandes) ; TS-480 / TS-590 / TS-890 / TS-2000 (AGC, FINE, RIT, bandes, échelles) ; Icom (ATT et préampli par modèle, MAIN = SUB en 07 B1, fréquences principale / secondaire en 25 00, DATA OFF en 1A06 00 00). Restent à refaire d'après leurs manuels : face avant du TS-990S, faces avant Elecraft K3 / K4, FTdx9000.

### Micro du PC vers le poste
- Clic droit sur la touche **PTT** (tous les postes qui en ont une) ou **Paramètres ⚙ > Micro vers le poste** (aussi pour les 19 anciens postes sans touche PTT) : le micro choisi est envoyé en continu sur la carte son d'émission du poste, en WASAPI partagé (les autres logiciels gardent le micro, rien n'est rejoué sur le PC). Réglage commun, mémorisé (`micro.*`).

### Journal de trafic (bouton LOG)
- Fenêtre à part : liste de QSO sans limite pratique, recherche, **Filtre** (indicatif, bande, mode, pays, dates, texte libre), colonnes au choix, onglets pour plusieurs journaux, import / export ADIF, doublons, statistiques, **Suivi des diplômes** (DXCC en couleurs, WAZ, WAC, WAS, IOTA).
- **Base MariaDB / MySQL au choix** (« Base… » avec tous les essais) ; MariaDB intégré installé automatiquement si aucun n'est présent ; **Importer / partager…** (retrouve le journal MariaDB d'un autre logiciel, copie ou partage direct, ou fichiers ADIF).
- **QSO de WSJT-X reçus automatiquement** (UDP 2333 « N1MM Logger+ », 2237 « Serveur UDP », 7825 ADIF brut, deux ports libres ; réglables dans Paramètres ⚙ et dans le journal).
- **Fenêtre Ajouter** : infos de l'indicatif 0,05 s après la frappe (nom, QTH, locator, commentaire du dernier contact), variantes /P /M /MM /QRP, « Recherche approfondie », double-clic sur un ancien contact pour le modifier (Mise à jour), boutons qrz.com / qrzcq, fréquence et mode du poste actuel.
- **Configuration** : Station (indicatif, prénom, locator : **distance calculée à la réception** ; Outils > Vérifier les distances pour les anciens), **DX Cluster**, Importer / partager, Sauvegarde ADIF, Base, Ports.
- **DX Cluster** : clusters enregistrés, connexion / reconnexion automatiques ; réglette à droite (échelle de la bande, indicatifs, curseurs VFO A / VFO B, zoom à la molette, clic sur un indicatif = le poste se règle et la fenêtre Ajouter reprend l'indicatif, filtres numérique / CW / phonie / FreeDV) ; trames en bas avec boutons Cluster et Alerte (bip sur un texte, lignes reliées par « ou » / « et »).
- **Envois en ligne** : QRZ.com, Club Log (jeton personnel), eQSL, LoTW (TQSL) ; **Envoyer à tous les logs en ligne** dans une fenêtre de progression non bloquante (annulable) ; colonnes « eQSL sent », « LOTW sent », « Club Log sent », « QRZ.com sent » (les deux dernières gardées dans un fichier à part) ; clic droit > Marquer envoyé / non envoyé, Exporter la sélection (ADIF) ou **pour LoTW (.tq8 signé par TQSL)** ; repli QRZ.com : fichier ADI + page d'import.
- **Thèmes** : 28 thèmes (de Windows 3.11 au plus moderne, mer, nature, rétro) appliqués à toutes les fenêtres du journal ; couleurs personnalisables (tablette de 64 couleurs + RVB), mémorisées par thème.
- **Télécommande prioritaire** : les traitements longs du journal (import, marquage, modification d'une colonne, distances, pays, signature LoTW) tournent sur un autre fil avec leur propre connexion, par petits paquets ; fermer la télécommande laisse le journal ouvert (postes déconnectés), relancer HamRadioLink la ramène (une seule copie du programme à la fois).
- Données dans `data\journal\` ; réglages `jn.*` dans `settings.ini`.

### Vérification
- **Autotest** : 159 229 vérifications (commandes de chaque poste contre un simulateur strict, partage, serveur, TCI sur un vrai WebSocket simulé, anciens Yaesu, verrous, journal, délai TX) ; essai du journal sur base isolée (34 contrôles : WSJT-X, ADIF, fenêtres, thèmes, envois simulés, traitements en arrière-plan).
- Essayé par l'auteur sur FT-2000 et FTdx10 réels, avec un vrai DX Cluster, de vrais contacts WSJT-X et un vrai journal. **Pas essayé en réel** : les envois vers QRZ.com, Club Log, eQSL et LoTW, les autres postes, un vrai serveur TCI, la création de paires com0com par le programme.
