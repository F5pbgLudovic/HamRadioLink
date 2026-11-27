# HamRadioLink — mode d'emploi

## 1. Premier lancement
1. Extraire le zip dans un dossier (pas besoin d'installer). Lancer `HamRadioLink.exe`.
2. Menu **Poste** : choisir votre modèle (la face avant change, avec ses réglages de liaison).
3. En haut à droite, **Liaison** : choisir l'interface, puis **CONNECTER**.

Pour essayer sans poste : **Poste virtuel (simulateur)** puis CONNECTER. Le simulateur applique strictement les formats des manuels.

## 2. Se connecter au poste

| Interface | Quand l'utiliser |
|---|---|
| **Port COM** | Le cas normal (câble CAT, USB). Réglez le port, la vitesse (la même que dans le menu du poste), les bits de stop, RTS / DTR. |
| **CAT sur TCP** | Serveur série-réseau, port COM déporté : les mêmes commandes dans une connexion réseau. |
| **Poste virtuel** | Essai sans matériel. |

Hamlib, OmniRig et FLRig ne sont pas proposés : ils ne transportent qu'un jeu réduit de commandes. HamRadioLink ne garde que les liaisons qui laissent passer **toutes** les commandes du poste.

### Partage du poste (bouton « Partage »)
HamRadioLink garde la liaison complète avec le poste et ouvre en plus :
- un **port COM virtuel** (paire **com0com**) ;
- et / ou un **serveur TCP** (par défaut cet ordinateur seulement ; case « réseau local » pour l'ouvrir).
Dans la liste, **aucune paire n'est cachée** : chacune porte son état — libre, « utilisée par HamRadioLink (poste …) » (tenue ouverte par le partage d'un poste connecté ; elle se libère à la déconnexion de ce poste), ou « occupée par un autre programme » (ExpertSDR3, log…). Une paire occupée par un autre programme n'est pas supprimée : elle est seulement refusée comme paire de partage. Cochez *Activer le partage* : si com0com n'est pas installé, HamRadioLink explique pourquoi il en a besoin et propose de l'installer lui-même (installateur signé embarqué, installation silencieuse : Windows demande seulement l'autorisation d'administrateur, et parfois de confirmer le pilote). Sous *Port COM virtuel*, la liste ne montre que les **paires libres** (COMn ↔ COMm, hors port du poste) ; s'il n'y en a aucune, « Créer une paire… » propose deux numéros libres que vous pouvez changer ; « Supprimer la paire… » enlève la paire choisie (confirmation d'abord ; seules les paires listées, donc libres, peuvent être supprimées). La fenêtre indique alors le port que HamRadioLink utilise et **celui à choisir dans OmniRig ou tout autre logiciel** (même vitesse). OK, puis CONNECTER (le partage s'applique à la connexion). Les commandes passent sans filtre ; chaque réponse est rendue à celui qui a posé la question (le client ne reçoit pas les lectures de HamRadioLink). Disponible pour Yaesu / Kenwood / Elecraft (texte), Icom / Xiegu (CI-V) et Yaesu 5 octets ; pas pour les anciens Yaesu à blocs d'état. Sur le poste virtuel, seulement pour les postes à commandes texte. Un client qui active le mode AI du poste reçoit toutes les réponses.

### Réglages selon la marque
- **Yaesu** : vitesse = menu *CAT RATE* du poste (4800 d'usine sur FT-2000 / FTdx5000, 38400 sur FTdx10). Activer *CAT RTS* sur FT-2000 / FTdx5000. FTdx10 : utiliser le port « Enhanced » du double port Silicon Labs CP210x.
- **Icom / Xiegu (CI-V)** : réglez dans le poste *CI-V baud rate* et *CI-V address*. Dans HamRadioLink, la case **Adresse CI-V** n'est à toucher que si vous avez changé l'adresse d'usine. Le programme fabrique lui-même les trames `FE FE … FD` et la conversion BCD des fréquences. Désactivez *CI-V transceive* si vous voyez des réponses étrangères.
- **Yaesu FT-817 / 818 / 857 / 897 (binaire 5 octets)** : menu *CAT RATE* du poste (4800 / 9600 / 38400, 9600 proposé), **2 bits de stop** (réglés d office pour ces postes). Le programme traduit lui-même ses commandes en trames de 5 octets. Seul le jeu de base est géré : fréquence (pas de 10 Hz), mode, PTT, verrouillage, split, CLAR (marche/arrêt), A/B, S-mètre, puissance, squelch, SWR élevé. Le poste ne dit pas quel VFO est actif ni l état de LOCK / SPLIT / CLAR : ces touches s allument d après vos clics. **Aucun manuel CAT n a servi** : à vérifier sur le poste.
- **Kenwood / Elecraft** : vitesse selon le menu du poste ; pour le TS-890, l'état d'émission est annoncé par le poste (`AI2;`).

Si le programme affiche « Aucune réponse du poste » : vérifier le port COM, la vitesse, RTS et le câble.

### Serveur de commande (bouton « Serveur »)
HamRadioLink ouvre un **serveur de commande IP** (port 7809) que **WSJT-X, JTDX, JS8Call, SDRLink** et d'autres logiciels de radioamateur savent utiliser : ils pilotent alors le poste choisi dans HamRadioLink, **quel que soit son modèle** (Yaesu, Icom, Kenwood, Xiegu, Elecraft, TCI…) : fréquence A / B, mode, émission (PTT), split, volume. Bouton **Serveur** → cocher *Activer le serveur de commande* → OK ; dans l'autre logiciel, choisissez le type de poste qui se connecte à un serveur IP (son nom exact dépend du logiciel), adresse 127.0.0.1, port 7809. Par défaut seul cet ordinateur est autorisé (case « réseau local » pour l'ouvrir). Si un autre programme occupe déjà le port 7809, fermez-le ou changez de port. Le **poste du serveur** se choisit dans la fenêtre (« Poste ») et reste le même, même si vous changez de poste à l'écran : le logiciel client ne voit pas le poste changer. Un organe **verrouillé** (clic droit ; pour une rangée, la touche verrouillée) ne bloque pas le logiciel connecté (verrous = interface seulement) ; chaque connexion et déconnexion est écrit dans le journal (Paramètres → Journal). **Plusieurs postes** : ce premier serveur reste sur le **premier poste ouvert** ; dans la même fenêtre, un **second serveur** (autre port, 7810 par défaut) peut piloter un **second poste** choisi dans la liste. Deux formes du protocole sont acceptées (texte simple, et messages à en-tête de 16 octets) ; essayé seulement contre des clients simulés (pas contre WSJT-X réel).

### Poste commandé en TCI (ExpertSDR3, Thetis…)
Dans la liste **Poste**, choisissez *Poste commandé en TCI* : un seul choix pour tous les logiciels SDR qui offrent un serveur **TCI** (ExpertSDR3 / SunSDR, Thetis pour les ANAN et Apache…). Dans **Liaison** : *TCI*, adresse (127.0.0.1 si le logiciel SDR est sur le même PC) et port (40001 par défaut ; Thetis : celui de « TCI Server »), puis CONNECTER. La face avant reprend ce que TCI sait faire : VFO A et B, mode, PTT, TUNE, S-mètre, DRIVE, volume, MUTE, SPLIT, RIT. Les changements faits dans le logiciel SDR sont suivis en direct. **Flex** n'a pas de TCI natif : utilisez SmartCAT (émulation Kenwood TS-2000) par un port COM virtuel avec le poste *Kenwood TS-2000*. TCI n'a été essayé que contre un serveur TCI simulé.

## 3. La vue normale
Chaque bouton, potentiomètre ou afficheur reproduit un organe de la face avant et envoie sa commande.
- **Potentiomètres** : glisser verticalement ou molette ; double-clic = zéro (Maj = fin).
- **Fréquence** : clic en haut / bas d'un chiffre, molette, double-clic = saisie directe.
- **Molette d'accord** : glisser en rond ou molette ; le pas se règle dessous.
- **Clavier** : flèches ← → = pas, ↑ ↓ = 1 kHz. **Échap** coupe l'émission.
- **Moniteur CAT** (bouton en haut) : tous les échanges, et une zone pour envoyer une commande à la main (le `;` est ajouté).

## 4. La « vue config » (modifier la face avant)
Menu **Vue** → *vue config*.
Mise à jour : Paramètres ⚙ > *Mise à jour* (automatique au démarrage ou bouton ; un clic, copie de sécurité, seul le programme est remplacé).
Alerte ROS : Paramètres ⚙ > *Alerte ROS* (seuil, délai, bips, chute de puissance + test). Message d'état : clic droit pour ignorer un avertissement.
Un avertissement s'affiche au passage en vue config (risque d'abîmer le « skin »).
- **Clic droit sur un élément** : le champ *Nom affiché* est un **menu déroulant** de toutes les commandes de la face avant du poste et de ses menus ; choisir une commande remplit envoi (SET), lecture, réponse attendue et valeurs. Un **S-mètre** prend l'échelle S et la case est agrandie si besoin (un élément ajouté devient un vu-mètre ; un élément d'origine non-vu-mètre refuse et indique d'utiliser *Ajouter*).
- **Déplacer / redimensionner** : tout se déplace à la souris ; le coin bas-droit change la taille. Cliquer dans un grand cadre sans toucher un bouton ne les fait plus disparaître.
- **Valider** enregistre la disposition ; **Retour arrière** annule le dernier déplacement (autant de fois que nécessaire) ; **Réinit** remet tout par défaut (positions, commandes modifiées, éléments ajoutés), après confirmation.
- En quittant la vue config, en changeant de poste ou en fermant le programme avec des modifications non enregistrées, il demande : **Oui** (enregistrer), **Non** (abandonner) ou **Annuler** (rester).

### Clic droit : voir et modifier une commande
Une fenêtre montre la commande CAT / CI-V de l'élément : envoi, lecture, format de réponse, valeurs d'un choix, min / max d'un curseur.
- **Tester la lecture** envoie la lecture au poste et affiche la réponse.
- **Essai** : colle une réponse pour vérifier qu'elle correspond au format.
- **Défaut** remet les valeurs d'origine. Une pastille verte marque un élément modifié.

Gabarits d'envoi : `{v}` valeur · `{v:3}` sur 3 chiffres · `{v:+4}` signe et 4 chiffres · `{p:RU:4:;}` / `{n:RD:4:;}` texte + 4 chiffres selon le signe · `{wait:1000}` pause en ms.
Format de réponse : expression régulière ; le groupe `v` est la valeur de l'élément, tout autre groupe nommé comme un élément met aussi cet élément à jour.

### Ajouter un élément
Bouton **Ajouter** : choisir le type (bouton, touche allumée / éteinte, potentiomètre, choix en rangée, vumètre, voyant), le nom, puis la commande dans la liste. **La liste ne montre que les commandes du poste sélectionné** (ses éléments, puis le catalogue de son manuel), avec un filtre de recherche. L'élément arrive en haut à gauche, la fenêtre de clic droit s'ouvre pour l'ajuster, puis on le déplace. Clic droit sur un élément ajouté : **Supprimer**. **Valider** enregistre (`data\<poste>.custom.ini`).

## 4 bis. Vumètres, verrous, paramètres, export / import
- **Vumètres** : clic droit sur un vumètre = barres ↔ aiguille (mémorisé pour chacun). En vue config, le clic droit ouvre l'éditeur de commande comme avant.
- **Réinitialiser les verrous** (⚙ Paramètres → *Réinitialiser les verrous*) : un clic, puis confirmation, et **tous** les verrous posés par clic droit (tous les postes, organes entiers et touches de rangées) sautent. La touche Lock du haut n'est pas concernée.
- **Journal des commandes** (⚙ Paramètres → *Journal des commandes*) : un fichier **par jour** dans le dossier `logs` (à côté du programme, ou `%APPDATA%\HamRadioLink\logs`), supprimé après 7 jours (réglable). Il contient les ordres envoyés au poste, les réponses qui changent, les erreurs (`?;`) et les événements : connexion / déconnexion, partage, paires com0com (état de chaque paire, création, suppression), ordres du serveur de commande. Les relectures périodiques identiques ne sont pas écrites (mode compact ; mode détaillé en option) et un plafond de 25 Mo par jour évite les fichiers énormes. Envoyez ce fichier pour comprendre un problème. **Touches actives** : fond bleu marine foncé (réglable : « Touche active (fond) »).
- **Bouton CONNECTER / DÉCONNECTER** : fond **vert** quand le poste répond, **rouge** quand la connexion est absente ou perdue (poste non connecté, câble débranché, poste éteint…). Les deux couleurs se règlent dans ⚙ Paramètres → Couleurs (« Bouton de connexion »).
- **Niveau du voile des verrous** : ⚙ Paramètres → *Verrous* → curseur 10 à 100 % (67 % par défaut) ; la couleur du voile se règle dans *Couleurs de l'interface*.
- **Touche PTT** : toute la case verte au repos, rouge pendant l'émission. **Clic droit sur PTT = délai de sécurité TX** (désactivé, 30 s, 1, 2, 3, 5, 10, 15, 20, 25 ou 30 minutes ; réglage mémorisé par poste) : une émission lancée par la touche est **coupée automatiquement** à l'échéance, un compte à rebours « ● TX m:ss » remplace le nom de la touche pendant l'émission et un message prévient. Une émission lancée ailleurs (micro du poste, logiciel connecté) n'est pas touchée. Pour verrouiller la touche PTT : Ctrl + clic droit (ou l'entrée du menu).
- **Micro du PC vers le poste** (clic droit sur PTT, ou Paramètres ⚙ > « Micro vers le poste » pour les anciens postes sans touche PTT) : envoie en continu le micro choisi sur la carte son d'émission du poste (sortie du PC reliée à l'entrée données / micro du poste), en WASAPI partagé : les autres logiciels gardent le micro et rien n'est rejoué sur le PC. Choisissez le micro, la carte son, puis cochez la case. Le poste n'émet ce son qu'en émission, avec sa source de modulation réglée sur USB / arrière. Réglage commun à tous les postes, mémorisé (`micro.*` dans `settings.ini`).
- **Bouton TUNE** (en bas à droite de la face avant de chaque poste, 190 × 62, déplaçable en vue de configuration) : un clic émet une tonalité de 1500 Hz sur la carte son d'émission du poste (celle de « Micro vers le poste ») avec le PTT par CAT, ou le VOX du poste, pendant 5 à 60 s ; le bouton affiche « ● TUNE » et le compte à rebours, un second clic arrête. Réglages dans **Paramètres ⚙ > Bouton TUNE** : niveau, durée, case VOX du poste, couleur au repos (vert) et pendant le TUNE (rouge). Si le poste ne répond plus, le bouton CONNECTER devient rouge et un clic relance la connexion.
- **Touche Lock** (à droite de « Serveur ») : bloque **tout le programme** sauf elle-même — clic, molette, clavier, fenêtres ouvertes (le logiciel connecté au serveur de commande garde la main : WSJT-X, SDRLink…) — et passe en **rouge** tant qu'elle est active (couleur réglable : Paramètres → Couleurs, « Touche Lock verrouillée »). Un second clic déverrouille. Le verrou n'est pas mémorisé : au prochain lancement le programme est déverrouillé. Fermer la fenêtre verrouillée demande confirmation. Le partage de port COM / TCP, lui, continue de transmettre (il ne passe pas par l'interface).
- **Plusieurs postes en même temps** : choisissez un autre poste dans la liste *Poste* alors que le premier est connecté (la liaison est propre à chaque poste) : le premier reste connecté et ouvert. À droite de *Vue*, dans **toutes les vues** (normale et vue config), une **coche par poste ouvert** (☑ = actif, ☐ = autre ; un clic active ce poste ; **fond vert quand il est connecté**) — dès qu'il y a un poste — et, avec deux postes, deux **icônes de scission** (côte à côte, ou l'un sous l'autre) : les deux faces avant s'affichent ensemble, chacune réduite en gardant **les mêmes proportions** que l'affichage entier. Toucher un élément d'un poste le rend actif. Un poste déconnecté en arrière-plan se ferme. Le *vue config* ne travaille que sur un poste (la scission se ferme). **Le partage se règle poste par poste** : le bouton *Partage* règle le poste actif (ex. paire COM20 ↔ COM21 pour le FT-2000, COM22 ↔ COM23 pour le FTdx10) ; changer la paire d'un poste ne touche pas l'autre. Une paire déjà attribuée à un autre poste est refusée (même chose pour un port TCP).
- **Boutons de postes mémorisés** : après une **connexion réussie**, le bouton du poste reste en haut (même après fermeture du programme) ; un clic le rouvre en rappelant ses réglages de liaison (type, port COM, vitesse, bits d'arrêt, RTS / DTR, adresse réseau, adresse CI-V), un **clic droit le retire**. Ces réglages sont mémorisés pour chaque poste dès la connexion, et aussi quand on quitte le poste ou le programme.
- **Connexion automatique** : **clic droit sur CONNECTER** (le mot AUTO apparaît) : ce poste se connecte tout seul au lancement, avec sa propre liaison ; plusieurs postes peuvent l'avoir.
- **Verrous** : clic droit sur **n'importe quel organe** (bouton, potentiomètre, choix, rangée de bandes / modes, afficheur de fréquence, molette d'accord) = verrouillé : l'organe est recouvert d'un **voile rouge foncé** (couleur réglable : Paramètres → Couleurs, « Voile d'un organe verrouillé ») avec un cadre rouge et ne répond plus (clic, molette, et flèches du clavier pour une fréquence) ; encore un clic droit pour libérer. Pour un choix à liste, **Ctrl + clic droit** ouvre la liste des valeurs. Mémorisé par poste. Échap (arrêt de l'émission) reste toujours actif.
- **⚙ Paramètres** : *Couleurs de l'interface* (12 couleurs, effet immédiat), *Vumètres* (tous en aiguille / en barres), *Molette souris* (1 à 10 % de la course par cran, 2 % par défaut, Maj = plus fin), *Verrous* (tout déverrouiller), *Commande manuelle* (envoi d'une commande brute ; CI-V : octets en hexadécimal).
- **⇅ Export / import** : *Exporter* enregistre dans un fichier `.ini` les réglages du logiciel, les dispositions de tous les postes et, si le poste est connecté, ses réglages (boutons, potentiomètres, choix). *Importer* restaure le fichier et redémarre ; les réglages du poste sont renvoyés à la prochaine connexion (jamais l'émission ni l'extinction).
  **Par parties** : l'export propose des cases (logiciel et dispositions, réglages du poste, **menu interne du poste** pour les Yaesu FT-2000, FTdx5000, FT-450D, FT-950, FT-991A et FTdx3000 — relu menu par menu, environ une minute) ; l'import lit le fichier, propose les parties qu'il contient, refuse les réglages d'un autre poste et envoie les réglages au poste connecté sans redémarrer (il ne redémarre que pour les réglages du logiciel). Le menu interne n'a été essayé que sur le poste simulé.
- **Icom IC-7760** : ce poste ouvre la fenêtre d'IC7760Link (cascade, spectre, vumètres, bandes/mémoires, CW/voix) avec ses propres ⚙ et ⇅ ; réglages dans `data\ic7760.ini`.

## 4 sexies. Le journal de trafic (bouton « LOG » — gardez une sauvegarde ADIF)
- **LOG** (sous « Serveur ») ouvre le journal : une liste de tous vos QSO (recherche rapide en haut à droite, Filtre, Disposition pour les colonnes), des onglets pour plusieurs journaux, **Ajouter** (Ctrl+N) ou double-clic pour voir / modifier un QSO. La fréquence et le mode du poste actuel sont proposés.
- **Base…** : choisir la base MariaDB / MySQL (serveur, port, utilisateur, mot de passe, base). Le bouton « Tester » vérifie tout : MariaDB installé sur l'ordinateur ?, moteur intégré présent ?, port à l'écoute ?, connexion, base, table, nombre de QSO. Sans MariaDB installé, HamRadioLink utilise son propre MariaDB intégré (installé dans son dossier, sans service Windows ; menu Fichier → Moteur MariaDB… pour le mettre à jour).
- **Importer / partager…** : retrouve les journaux MariaDB d'un autre logiciel de journal (copie : l'original n'est que lu ; ou partage direct de la même base), reprend votre station, importe des fichiers ADIF (un onglet par fichier). Faites une sauvegarde ADIF avant : l'assistant la propose.
- Menus : Fichier (import / export ADIF…), Édition, Affichage, Journal (diplômes, statistiques, doublons, sauvegarde ADIF), Outils (**Vérifier les distances**, services en ligne, **Envoyer à tous les logs en ligne**, envoi de la sélection à QRZ.com / Club Log / eQSL / LoTW), **Configuration**, **Thèmes**, Aide.
- **Ligne du haut** : l'onglet du journal, « + », puis Ajouter, Supprimer, QRZ.com, Suivi des diplômes (tableau DXCC en couleurs : vert = confirmé, orange = contacté), Filtre (indicatif, bande, mode, pays, dates, **texte libre**), Tout et ▲ (remonter en haut de la liste). Clic droit sur un contact : Supprimer.
- **Fenêtre Ajouter** : les infos de l'indicatif s'affichent 0,05 s après la frappe (nom, QTH, locator, commentaire du dernier contact…) ; la liste du dessous montre les contacts de l'indicatif et de ses variantes /P /M /MM /QRP (case **Recherche approfondie** pour xx/indicatif/xxx) ; double-clic sur un ancien contact = l'ouvrir pour le modifier (bouton **Mise à jour**) ; boutons **qrz.com** et **qrzcq** = ouvrir la page de l'indicatif.
- **Configuration** : **Station** (indicatif, prénom, locator : la distance de chaque contact est calculée à sa réception ; Outils > Vérifier les distances complète les anciens), **DX Cluster** (clusters enregistrés, connexion / reconnexion automatiques, réglette, trames, curseurs VFO A / B), Importer / partager…, Sauvegarde ADIF, Base…, Ports….
- **DX Cluster** : une réglette à droite montre les indicatifs de la bande (Auto = bande du poste, ou bande choisie) avec les curseurs VFO A (rouge) et VFO B (bleu) ; **molette = zoom**, glisser = déplacer, **clic sur un indicatif = le poste se règle** (fréquence et mode), clic droit = filtres (numérique, CW, phonie, FreeDV). Les trames s'affichent en bas, avec les boutons **Cluster** (clusters actifs) et **Alerte** (plusieurs lignes de texte reliées par « ou » / « et », un bip retentit sur un spot en direct qui correspond — attention : le bip sort par la carte son de Windows).
- **Thèmes** : 53 thèmes (de Windows 3.11 au plus moderne, mer, nature, rétro) appliqués à toutes les fenêtres du journal ; **Thèmes > Personnaliser les couleurs** : tablette de 64 couleurs + RVB, mémorisé pour chaque thème.
- **Fermer la télécommande** : si le journal est ouvert, il reste ouvert (postes déconnectés, aucun lien avec le poste) ; un nouveau clic sur HamRadioLink ramène la télécommande (une seule copie du programme à la fois).
- **QSO de WSJT-X reçus automatiquement** : chaque contact validé arrive dans le journal courant (doublons ignorés). HamRadioLink écoute en UDP : **2333** (diffusion « N1MM Logger+ » de WSJT-X : Réglages > Rapports, cochez la case et mettez le port 2333), **2237** (Serveur UDP de WSJT-X, au même endroit), **7825** (ADIF brut), + 2 ports libres. Un port ne peut être écouté que par un seul programme à la fois. Réglages : **Paramètres ⚙ > Réglages des ports** (ou bouton « Ports… » du journal), 0 = inutilisé.
- **Envois en ligne** (Outils) : QRZ.com (clé API du journal, abonnement QRZ), Club Log (votre indicatif et un **jeton** personnel « cl_… » créé sur clublog.org > Settings > API ; plus de mot de passe ni de clé de logiciel), eQSL, LoTW (TQSL installé et nom de la « Station Location ») se règlent dans **Outils > Online services**. **Envoyer à tous les logs en ligne** envoie la sélection (ou ce qui n'est pas encore marqué « envoyé ») dans une fenêtre de progression qui ne bloque pas le journal. Colonnes « eQSL sent », « LOTW sent », « Club Log sent », « QRZ.com sent ». Clic droit sur des contacts : Exporter la sélection (ADIF) ou **pour LoTW** (fichier .tq8 signé par TQSL, à déposer à la main sur le site de LoTW), Envoyer la sélection à…, Marquer envoyé / non envoyé. Si QRZ.com refuse l'envoi, un fichier ADI est créé et la page d'import de QRZ.com s'ouvre.
- Données : dossier `data\journal` ; réglages `jn.*` dans `settings.ini`.

## 5. Mes réglages
Tout est dans le dossier `data` à côté de l'exe : `<poste>.layout.ini` (positions), `<poste>.cat.ini` (commandes modifiées), `<poste>.custom.ini` (éléments ajoutés), `settings.ini` (liaison). Supprimer un fichier = remettre cette partie par défaut. Le bouton **⇅** exporte/importe tout dans un seul fichier. Pour sauvegarder ou changer de PC, copiez le dossier `data`.

## 6. Mises à jour
Remplacez `HamRadioLink.exe` par le nouveau (gardez `data`). Si vous compilez vous-même : `build.ps1` renomme l'ancien exe en `HamRadioLink-<version>.exe` dans `dist`.

## 7. Dépannage
- **Pas de réponse** : port, vitesse, câble, RTS ; sur Icom, adresse CI-V et vitesse CI-V.
- **« ?; » dans le moniteur** : le poste refuse la commande. Une lecture refusée 3 fois est abandonnée automatiquement. Corrigez la commande par clic droit.
- **Un bouton ne fait rien sur mon poste** : normal pour un modèle non vérifié sur manuel ; ouvrez le clic droit, regardez la commande, comparez avec le manuel CAT / CI-V du poste, corrigez-la.
- **Signaler un problème** : [issue GitHub](https://github.com/F5pbgLudovic/HamRadioLink/issues) avec le modèle, la liaison et un extrait du Moniteur CAT.

## 8. Options de la ligne de commande
`--radio=<poste>` (ex. `ftdx10`, `ic7610`, `ts890`) · `--virtual` · `--data=<dossier>` · `--selftest=rapport.txt` · `--dump=fichier.txt` (liste des commandes) · `--shot=image.png,ms,actions` (capture).

## 9. Prudence
Ce logiciel envoie des commandes à votre poste, y compris pour **émettre**. Essayez d'abord sur le poste virtuel, puis le poste sur **charge fictive à faible puissance**. Les commandes des postes non vérifiés sur manuel peuvent être fausses.

73 de F5PBG

## 4 septies. Nouveautés de la 1.2 (échelles, étalonnage, menu du poste)
- **Paramètres ⚙ > Réglage S-mètre puissance / Réglage autres S-mètres / Réglage Width / CONT / RF PWR** : puissance max du poste, étalonnage du wattmètre (10 W, 20 W… jusqu'à la puissance max), des vumètres SWR, ALC, COMP, Id, Vdd, et des boutons Width, CONT, RF PWR (affichés en Hz / en W). « Pleine déviation de l'échelle Po » plus petite que la puissance max : le bouton RF PWR s'arrête à cette valeur.
- **Vue config** : le bloc LIAISON est masqué ; **Aligner** donne la même taille aux boutons et potentiomètres de même forme d'une section (Retour arrière pour annuler).
- **Bouton MENU** (postes Yaesu FT-2000, FTdx5000, FT-450, FT-950, FT-991A, FTdx3000) : table des menus du poste (commande EX). « Télécharger les valeurs du poste » à la première utilisation ; double-clic ou clic sur une valeur pour la modifier (relecture de contrôle) ; clic droit : renommer un menu, définir ses valeurs possibles. **Attention : chaque valeur modifiée est envoyée immédiatement au poste.**
- **LOG** : boutons ronds − / + / ↺ de zoom du texte (liste, réglette du cluster, trames du bas), bouton ↔ pour ajuster les colonnes, bouton QRZCQ, 12 nouveaux thèmes.

## 4 octies. Nouveautés de la 1.3 (FT-991A)
- **FT-991A** : boutons de bande **144** et **430 MHz** ; potentiomètre **CLAR** (pas de 10 Hz) ; potentiomètre **DT GAIN** (menu 073, niveau des modes DATA) ; bouton **MIC EQ** (égaliseur du micro) ; **CTCSS / DCS** (mode, fréquence CTCSS, code DCS) ; **C4FM TX (AMS)** (menu 090).
- Le reste du C4FM (GM, écran propre) n'a pas de commande CAT : non reproduit. Rien de tout cela n'est encore essayé sur un vrai FT-991A : en cas d'écart (ex. MIC EQ à l'envers), clic droit en vue config pour corriger.
- Le potentiomètre CLAR par RC / RU / RD est aussi rendu aux autres postes Yaesu dont le catalogue n'a pas la commande CF.

## 4 nonies. Nouveautés de la 1.4 (FTdx10, FT-991A DF5DP, TUNE)
- **FTdx10** : bouton **MIC EQ** ; **PROC** retrouve son niveau au rallumage ; bouton **FT8/FT4...** (grille des modes, après DATA-FM-N) : USB + PROC et MIC EQ coupés ; au retour, mode, PROC (niveau compris) et MIC EQ d'avant. Choisir un autre mode sort aussi de FT8/FT4. Ordres espacés de 350 ms. Même bouton sur les postes Yaesu dont la grille des modes a une case libre.
- **Poste « Yaesu FT-991A DF5DP »** : copie du FT-991A avec sa propre disposition (`data\ft991a-df5dp.*`). Clic droit sur CTCSS ENC / FREQ CTCSS / CODE DCS : liste de toutes les valeurs ; Ctrl + clic droit : verrouiller.
- **Fréquences sur 9 chiffres** (postes au-delà de 100 MHz) ; **listes longues** (plus de 20 valeurs) avec ascenseur et molette.
- **TUNE** : case « Changer la puissance pendant le TUNE » (Paramètres > TUNE) : valeur de consigne envoyée avant l'émission, puissance d'avant remise après.
- **Paire com0com** : si la création échoue, le message affiche la réponse de com0com (et le journal la garde).
- Rien de nouveau n'est encore essayé sur un vrai poste.

## 4 decies. Nouveautés de la 1.5 (journal : langues, annuler, copier, thèmes)
- **Langue du journal** : **Configuration > Langue / Language...** (23 langues, français par défaut), prise en compte à la réouverture du journal. Seul le journal est traduit.
- **Cluster** : liste **5 à 30 min** à côté de « Auto » = durée d'affichage des spots. **Thèmes** : 53 (13 modernes ajoutés) ; la liste reste ouverte pour comparer, clic ailleurs ou Échap pour sortir ; « Personnaliser » toujours visible.
- **Clic droit sur des QSO sélectionnés > Changer la bande / fréquence** : FT8 → fréquence FT8 de la bande, autre mode → début de bande.
- **↶ ↷** (Ctrl+Z / Ctrl+Y) : annuler / refaire ajout, modification, suppression (50 opérations). **Ctrl+C / X / V** copier, couper, coller des lignes ; **Ctrl+A** tout sélectionner ; **Ctrl+N** ajouter ; **Entrée** valide la fenêtre d'ajout.
- **Serveur de commande** : poste non connecté → les ordres « set » sont refusés, le poste garde sa fréquence.
- Non couverts par les tests automatiques : copier / coller, changement de bande par lot, fenêtre des thèmes. Traductions non relues par des locuteurs natifs.

## 4 undecies. Nouveautés de la 1.6 (dispositions intégrées)
- **Dispositions par défaut dans l'exe** : FT-991A DF5DP, FTdx10, FTdx5000 et FT-2000 n'ont plus besoin de fichiers dans data ; la disposition est celle de l'exe.
- **FT-991A / DF5DP inversés** : au premier démarrage, les anciens fichiers data\ft991a*.layout.ini et .cat.ini sont rangés dans data\ancien-ft991a (récupérables). Vos déplacements de touches suivants sont toujours enregistrés (**Valider**).

## 4 duodecies. Nouveautés de la 1.7

Club Log n'exige plus de clé de logiciel : **Outils > Online services > Club Log** = votre indicatif + un **jeton** « cl_… » (clublog.org > Settings > API > Create token ; copiez-le tout de suite, il n'est montré qu'une fois). Le jeton se traite comme un mot de passe. Un jeton inutilisé 6 mois est supprimé par Club Log.

Les envois en ligne se font par **sessions de 100 QSO** (pause de 3 s entre deux sessions) pour tous les services. Dans Online services, « Demander un jeton » ouvre la page de Club Log et « Aide » explique la marche à suivre ; ces fenêtres ne bloquent pas le reste du programme.

**HRDLog.net, QRZCQ.com, HamQTH** (1.7) : réglés dans Online services (HRDLog : indicatif + code d'envoi reçu par e-mail ; QRZCQ : indicatif + clé API ; HamQTH : identifiant + mot de passe HamQTH), disponibles dans Envoyer la sélection à / Marquer, avec leurs colonnes « envoyé ».

**World Radio League** : réglé dans Online services (clé API créée sur WRL : Integrations > Developer API > Generate API key ; boutons « Demander une clé » et « Aide »), colonne « WRL sent », menus Envoyer / Marquer.

**Envoyer à tous les logs en ligne** : fenêtre avec les services (coches ; non réglés grisés) et la date de départ (jusqu'à aujourd'hui). **Outils > Envoi automatique** : envoi des nouveaux contacts (saisis ou reçus de WSJT-X), services cochés, délai : instantané / 30 min / 1 h / 2 h / à la fermeture du log. Le dernier contact traité et l'heure du dernier envoi sont mémorisés (settings.ini) : après une coupure, l'envoi reprend sans repartir du début du journal. À l'activation, seuls les contacts suivants sont envoyés.

**Bouton Alertes** (panneau danger, après le « ? ») : allumé quand il y a de nouveaux messages (perte de liaison, alerte ROS avec la valeur du ROS, carte son indisponible…), éteint sinon. Un clic ouvre la liste horodatée ; OK ferme, Effacer vide la liste. Messages conservés 7 jours, puis effacés automatiquement. Valable pour tous les postes.

**Loupe** (barre d'outils, après « Refaire ») : ouvre une fenêtre de recherche qui ne bloque pas le journal. Choisissez le champ (Date du QSO, Indicatif, Texte : nom, QTH, commentaire…), tapez la valeur, puis « Suivant » / « Précédent » pour monter et descendre dans le tableau (Entrée = suivant, Maj+Entrée = précédent). Date : 22/11/2026, 2026-11-22, 2026-11 ou 2026.

**Réception du poste sur le PC** (1.7) : clic droit sur le bouton de volume AF d'un poste : « Écouter la réception du poste sur le PC » (activer / désactiver), « Carte son du poste » (réglage propre à chaque poste), et « Couper le haut-parleur du poste (le son reste sur le PC) » qui met le volume AF du transceiver à 0 par CAT tant que l'écoute est active (volume rétabli ensuite et à l'arrêt du programme). Ctrl + clic droit : verrouiller le bouton. La loupe du journal : bouton « Début » (haut du tableau) et, pour une date absente, aller à la date la plus proche.

Quand « Couper le haut-parleur du poste » est actif, le **bouton de volume AF règle le volume du son joué sur le PC** (le poste reste à 0 ; son volume d'origine est rétabli à la désactivation et à l'arrêt du programme).

## 4 terdecies. Nouveautés de la 1.8

**Touche MENU du FTdx10** (entre METER et TUNE) : fenêtre qui reprend le menu du poste. Onglets MENU RAPIDE (réglages d'accueil), RADIO / CW / OPERATION / DISPLAY SETTING et leurs sous-pages ; chaque ligne montre la valeur lue dans le poste et la modifie ; les changements faits sur le poste apparaissent (relecture toutes les 5 s). Un bandeau rouge signale ce que le poste refuse. La fenêtre prend la hauteur de l'écran.

**Clic droit sur les touches du FTdx10** : MIC EQ = égaliseur du micro (courbe en direct ; le poste refuse par CAT les gains sous −10 dB, défaut connu : réglez-les sur le poste), NB = niveau / largeur / réjection, DNR = niveau, VOX = source micro ou data, MOX = délai de sécurité comme PTT, NOTCH = largeur large / étroite (affichée sur la touche), PAS = valeur précédente. Ctrl + clic droit verrouille la touche. Clic gauche sur le mode du VFO B : liste des modes. Touche 70 : début de la bande 70 MHz. CLAR RX et CLAR TX sont deux réglages indépendants (le programme envoie celui que vous tournez, avec seul son clarifier actif) ; CLEAR CLAR remet les deux à 0 ; tourner la molette vite = pas de 100 Hz.

**Grand S-mètre à aiguille** (FTdx10, FT-2000 ; autres postes : bouton « S-mètre » de la vue config) : clic droit sur un vumètre de gauche = remplacer les barres par le S-mètre (et inversement) ; clic gauche = fonction suivante (PO, SWR, ALC, COMP, ID, VDD, commande METER du poste). L'aiguille suit le signal en réception et la mesure choisie en émission. En vue config, on voit soit le S-mètre soit les barres.

**Au démarrage**, aucune commande n'est envoyée au poste avant la lecture de tous ses paramètres ; les positions de la dernière session s'affichent aussitôt. **FT8** : l'état d'avant (PROC, niveau de PROC, MIC EQ, mode) est mémorisé sur disque et rétabli en sortant de FT8, même après un arrêt du programme.

**Roue crantée > Alerte ROS** (en bas) : étalonnage du ROS pour tous les postes (plusieurs mesures ou « Passer », extrapolation) ; WIDTH s'étalonne tous les 100 Hz.

**Journal** : fenêtre Add (RST proposés selon le mode, thème propre à la fenêtre) ; menu Fichier réorganisé avec « Sauvegarder la base maintenant… » (fichier .sql daté, dossier et nombre de copies au choix, sauvegarde automatique à la fermeture / chaque jour / semaine / mois) et « Restaurer la base maintenant… » (dossier des sauvegardes ou autre chemin, avec sauvegarde préalable) ; journal de destination des QSO reçus (Réglages des ports) ; Online services : case « Ne pas transmettre les commentaires ».

## 4 quaterdecies. Nouveautés de la 1.9

**Égaliseur audio 10 bandes sur le PC** : une fenêtre (clic droit sur PTT > « Égaliseur 10 bandes + compresseur / limiteur (émission) », ou clic droit sur le volume AF > « Égaliseur 10 bandes (réception) ») avec 10 curseurs de 50 Hz à 10 kHz (±12 dB, haut = gain), la courbe de réponse en direct, des préréglages et « À plat » (clic droit sur un curseur = remise à 0).

- **Émission** : égaliseur puis **compresseur / limiteur** (seuil, rapport, gain de sortie, plafond, attaque, relâche ; la réduction de gain s'affiche). Actif sur le micro envoyé au poste, donc la case « Micro du PC vers le poste » doit être cochée.
- **Réception** : actif sur le son du poste rejoué sur le PC (case « Écouter la réception du poste sur le PC »).
- Rien ne change pour un micro branché directement sur le poste ni pour le haut-parleur du poste. Les réglages sont mémorisés et communs à tous les postes.

## 4 quindecies. Nouveautés de la 1.10

**Noise gate à l'émission** : dans la fenêtre de l'égaliseur audio (onglet Émission), un panneau « Noise gate » sous le compresseur coupe le bruit entre les mots (seuil, atténuation, attaque, maintien, relâche). Ordre de la chaîne : noise gate, égaliseur 10 bandes, compresseur / limiteur. Comme le reste, il agit sur le micro envoyé au poste (« Micro du PC vers le poste » coché).

## 4 sexdecies. Nouveautés de la 1.11

**Égaliseur audio** : les 10 curseurs couvrent maintenant **50 Hz à 10 kHz** (50, 100, 200, 400, 800 Hz, 1,6, 3,15, 5, 7, 10 kHz) et le sens est corrigé : **monter un curseur = gain, le descendre = atténuation**. Préréglages refaits ; les anciens réglages de bandes ne sont pas repris (l'égaliseur repart à plat).

## 4 septdecies. Nouveautés de la 1.12

**Import ADIF** : les fichiers dont la fréquence est en kHz ou en Hz (OPSLOG, par exemple) s'importent maintenant sans l'erreur « out of range value for column col_freq ». L'échelle (MHz, kHz, Hz) est choisie d'après la bande du contact ; une fréquence incompatible avec la bande, ou supérieure à 2,147 GHz, n'est pas enregistrée et sa valeur d'origine est ajoutée au commentaire (« [FREQ … ?] »).

## 4 octodecies. Nouveautés de la 1.13

**Exports tolérants** : si un contact pose problème lors d'un export ADIF (fichier ou sauvegarde), il est ignoré et l'export continue ; un message indique combien de contacts ont été ignorés. Même principe pour les envois en ligne vers QRZ.com, HRDLog.net, HamQTH et eQSL : un contact refusé n'arrête plus l'envoi. Les contacts refusés sont marqués **No** dans la colonne « envoyé » du service concerné (et seront réessayés à l'envoi suivant).


## 4 novodecies. Nouveautés de la 1.14

**Thèmes** : 115 thèmes classés du plus clair (n°1, pour le jour) au plus sombre (nuit). Dans la fenêtre des couleurs : « Créer un nouveau thème… », « Exporter ce thème… » (fichier `.hrltheme`), « Supprimer ce thème ». Dans la fenêtre des thèmes : « Importer un thème… ». Vos thèmes sont dans `data\themes`.

**Fenêtre Add** : l'**azimut** s'affiche à droite de la distance (locator du contact, sinon centre de son pays). L'heure de fin suit l'horloge du PC tant que « now » est cochée (défaut pour un nouveau contact) ; à la réouverture d'un contact elle est décochée, la recocher relance l'horloge. Le bouton **Rotor** n'apparaît que si un rotor est réellement présent et l'oriente vers le correspondant.

**Menu Rotor** (à droite de Thèmes) : choix du modèle (Yaesu, WiMo… plus de 40 ; protocoles GS-232, DCU / Hy-Gain, SPID, Easycomm, serveur réseau), port ou adresse, aller à un azimut, chemin long, STOP, suivi automatique du contact. Le bouton rouge **Étalonnage** ouvre une fenêtre séparée (confirmation avec « Annuler » : risque de dérégler le calage) : valeur réelle saisie directement, ou réglage guidé par + / −. Rien n'est bloquant. Hy-Gain, SPID et Easycomm n'ont pas été essayés sur matériel.

**Mise à jour** : seule la version précédente est gardée (`HamRadioLink-x.y.exe`) ; les plus anciennes sont supprimées au démarrage.

**FT-2000** : la fenêtre des menus relit les menus visibles à l'ouverture, au défilement et toutes les 5 s (« Lecture en cours… »). Nouveau bouton **EXT-GND PA** sous KEYER (menu 146).

## 4 vicies. Nouveautés de la 1.15

**Journal > Outils > Vérifier les doublons** (en bas du menu).
1. Le programme cherche les contacts de même indicatif et de même bande dont l'heure de début diffère de 120 secondes au plus.
2. Une fenêtre affiche le rapport (une ligne par doublon, avec ascenseurs). **Cliquez sur une ligne** : le journal se place sur le contact gardé ; cliquez à droite du mot « supprimé » pour voir le contact supprimé. Si un filtre est actif, retirez-le.
3. Rien n'est supprimé tant que vous n'avez pas cliqué sur le bouton rouge **Supprimer les doublons** (puis confirmé). **Fermer sans supprimer** abandonne.
4. On garde le contact le plus renseigné, on lui reporte les champs manquants de l'autre, puis l'autre est supprimé. Le rapport est aussi enregistré dans `data\doublons-….txt`.

La suppression est définitive : faites d'abord une sauvegarde ADIF (Configuration > Sauvegarde).

## 4 unvicies. Nouveautés de la 1.16

**Journal : colonne Commentaire sur plusieurs lignes.** Un commentaire long passe à la ligne à la largeur de la colonne, entre deux mots (jamais au milieu d'un mot) ; la ligne du contact s'agrandit. Réduisez la colonne : le texte revient à la ligne. Le bouton **↔** limite cette colonne à environ 70 caractères. Les autres colonnes sont inchangées.

## 6 duovicies. Nouveautés de la 1.20 à 1.24

- **1.24 (volumes, FTdx10, étalonnage)** : clic gauche sur un bouton de volume (AF, AF SUB, VOLUME) = coupe ce bouton (MUTE), nouveau clic = rétablit. FTdx10 : bouton LEVEL du scope (-30 à +30 dB) et bouton SCOPE (CENTER / CURSOR / FIX : clic gauche suivant, clic droit précédent) ; aiguilles ALC et VDD du grand S-mètre calées sur les barres. Les fenêtres d'étalonnage ne bloquent plus la fenêtre principale (PTT possible).

- **1.23 (mise à jour)** : le téléchargement ne bloque plus la fenêtre (tâche de fond). Si la mise à jour échoue, télécharger HamRadioLink.exe sur la page des versions GitHub et remplacer l'ancien (le dossier data reste).

- **1.21 / 1.22 (serveur de commande)** : mode du VFO B (Mode2) ; FT-2000 : MD03/MD07 comme la touche CW du poste.

- **Masquer un journal** : Affichage > « Masquer le journal X » (une ligne par journal). Il reste toujours un journal affiché.
- **Serveur HRD DDE pour HDSDR** : fenêtre Serveur > case « Serveur HRD DDE ». Dans HDSDR : Options > RF Front-End > « controlled by HRD (DDE) » et Options > DDE to HDSDR > format HRD. HDSDR suit alors la fréquence du poste et la lui renvoie (le mode est suivi aussi ; l'émission n'est jamais commandée).

## 6 duovicies. Nouveautés de la 1.19

**Nettoyer « Incoming » (QRZ.com)** : Outils, tout en bas. Choisissez le nombre de jours, cliquez « Ouvrir QRZ dans Edge » (connectez-vous si besoin), puis « Nettoyer la liste Incoming » : les demandes dont le QSO est plus ancien sont rejetées après confirmation (irréversible).

**Loupe** : case « Mot entier » ; recherche de texte dans les cellules affichées, de gauche à droite et en descendant, jusqu'à la première trouvée (colonnes « xxx sent » comprises). **Marquage envoyé / non envoyé** d'une grosse sélection : barre de progression. **Fenêtres** ouvertes sur l'écran du journal. **Envoi à tous** : case « sélectionnés » cochée d'office.

## 5 duovicies. Nouveautés de la 1.18

**World Radio League.** L'envoi se fait maintenant QSO par QSO (service `POST /v1/contacts`, 60 par minute, 5 000 par jour) au lieu d'un import ADIF par lot, limité à 20 par jour par le service. Les doublons sont comptés « déjà présents » et marqués envoyés ; un QSO refusé est marqué No sans arrêter l'envoi ; pause automatique si la limite par minute est atteinte. Les autres envois en ligne ne changent pas.

**Fenêtre d'envoi.** Pour World Radio League : progression par lots de 10, nombre réel envoyé et nombre de refusés. Quand tout est déjà marqué envoyé, elle affiche « rien à envoyer, tous déjà marqués envoyés » (au lieu de « 0 / 0 terminé ») et une ligne indique combien de QSO sont ignorés (avec un exemple). Dans « Envoyer à tous les logs en ligne », la case « Seulement les xxx QSO sélectionnés » est cochée d'office quand des lignes sont sélectionnées.

## 4 duovicies. Nouveautés de la 1.17

**Liaison avec SDRLink (port COM virtuel).** HamRadioLink n'est plus bloqué quand le logiciel branché sur le port virtuel arrête de lire : les données sont abandonnées après 500 ms et la lecture du poste continue. Fin des pertes de liaison cycliques.

**Grand S-mètre à aiguille.** Sous l'aiguille, une ligne jaune `Po: … SWR: … S…` (les mots restent fixes, les valeurs bougent). Dans la case SUB (VFO-B), le rapport S du récepteur secondaire en jaune et un petit barregraphe (FT-2000, FTdx5000, FTdx101, FTdx9000, TS-2000). La couleur jaune se règle dans Paramètres > Couleurs. L'échelle Po va de 0 à la puissance max du poste (Paramètres > Réglage S-mètre puissance) et l'aiguille indique les watts réels ; à 200 W ou plus, la position Vdd affiche 50 V. En émission, le S MAIN retombe à zéro.

**FT-2000** : l'état d'émission `TX3` est reconnu (Po et S-mètre ne se figent plus quand le PTT du poste et du CAT sont actifs ensemble).

**Journal des commandes** : réinitialisé à 25 Mo (ancien en `.prev`), 24 h d'archives au plus.

**Fenêtres** : plein écran au premier démarrage, puis taille et position de la dernière fermeture (télécommande et journal).

**Journal : rapports** : 59+5, 59+10, 59+15 … 59+55, 59+60.




