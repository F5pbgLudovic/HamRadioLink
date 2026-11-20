# Historique des versions

## 1.1 — 2026-11-20

### Bouton TUNE
- **Bouton TUNE sur les 125 postes**, en bas à droite de la face avant (190 × 62, déplaçable en vue de configuration) : tonalité de 1500 Hz envoyée sur la carte son d'émission du poste (celle de « Micro vers le poste »), PTT par CAT ou VOX du poste, 5 à 60 s ; « ● TUNE » et compte à rebours pendant l'émission, second clic = arrêt (arrêt aussi si le poste est déconnecté).
- **Paramètres ⚙ > Bouton TUNE** : niveau (1 à 100 %), durée, case « VOX du poste », couleur au repos (vert) et pendant le TUNE (rouge), retour aux couleurs par défaut.

### Connexion
- Quand le poste ne répond plus (« Plus de réponse du poste »), le bouton passe à **CONNECTER** sur fond rouge ; un clic relance la connexion ; si le poste répond de nouveau, il redevient « DÉCONNECTER ».

### Vérification
- Autotest : 163 753 vérifications (bouton TUNE présent et dans la fenêtre pour chaque poste). **Pas essayé en réel** : l'émission de TUNE sur un vrai poste.

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
- **Envois en ligne** : QRZ.com, Club Log (clé API de logiciel nécessaire), eQSL, LoTW (TQSL) ; **Envoyer à tous les logs en ligne** dans une fenêtre de progression non bloquante (annulable) ; colonnes « eQSL sent », « LOTW sent », « Club Log sent », « QRZ.com sent » (les deux dernières gardées dans un fichier à part) ; clic droit > Marquer envoyé / non envoyé, Exporter la sélection (ADIF) ou **pour LoTW (.tq8 signé par TQSL)** ; repli QRZ.com : fichier ADI + page d'import.
- **Thèmes** : 28 thèmes (de Windows 3.11 au plus moderne, mer, nature, rétro) appliqués à toutes les fenêtres du journal ; couleurs personnalisables (tablette de 64 couleurs + RVB), mémorisées par thème.
- **Télécommande prioritaire** : les traitements longs du journal (import, marquage, modification d'une colonne, distances, pays, signature LoTW) tournent sur un autre fil avec leur propre connexion, par petits paquets ; fermer la télécommande laisse le journal ouvert (postes déconnectés), relancer HamRadioLink la ramène (une seule copie du programme à la fois).
- Données dans `data\journal\` ; réglages `jn.*` dans `settings.ini`.

### Vérification
- **Autotest** : 159 229 vérifications (commandes de chaque poste contre un simulateur strict, partage, serveur, TCI sur un vrai WebSocket simulé, anciens Yaesu, verrous, journal, délai TX) ; essai du journal sur base isolée (34 contrôles : WSJT-X, ADIF, fenêtres, thèmes, envois simulés, traitements en arrière-plan).
- Essayé par l'auteur sur FT-2000 et FTdx10 réels, avec un vrai DX Cluster, de vrais contacts WSJT-X et un vrai journal. **Pas essayé en réel** : les envois vers QRZ.com, Club Log, eQSL et LoTW, les autres postes, un vrai serveur TCI, la création de paires com0com par le programme.
