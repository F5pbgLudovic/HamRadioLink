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
- **LOG** (sous « Serveur ») ouvre le journal : une liste de tous vos QSO (recherche rapide en haut à droite, Filtre, Disposition pour les colonnes), des onglets pour plusieurs journaux, **Ajouter** (Ctrl+A) ou double-clic pour voir / modifier un QSO. La fréquence et le mode du poste actuel sont proposés.
- **Base…** : choisir la base MariaDB / MySQL (serveur, port, utilisateur, mot de passe, base). Le bouton « Tester » vérifie tout : MariaDB installé sur l'ordinateur ?, moteur intégré présent ?, port à l'écoute ?, connexion, base, table, nombre de QSO. Sans MariaDB installé, HamRadioLink utilise son propre MariaDB intégré (installé dans son dossier, sans service Windows ; menu Fichier → Moteur MariaDB… pour le mettre à jour).
- **Importer / partager…** : retrouve les journaux MariaDB d'un autre logiciel de journal (copie : l'original n'est que lu ; ou partage direct de la même base), reprend votre station, importe des fichiers ADIF (un onglet par fichier). Faites une sauvegarde ADIF avant : l'assistant la propose.
- Menus : Fichier (import / export ADIF…), Édition, Affichage, Journal (diplômes, statistiques, doublons, sauvegarde ADIF), Outils (**Vérifier les distances**, services en ligne, **Envoyer à tous les logs en ligne**, envoi de la sélection à QRZ.com / Club Log / eQSL / LoTW), **Configuration**, **Thèmes**, Aide.
- **Ligne du haut** : l'onglet du journal, « + », puis Ajouter, Supprimer, QRZ.com, Suivi des diplômes (tableau DXCC en couleurs : vert = confirmé, orange = contacté), Filtre (indicatif, bande, mode, pays, dates, **texte libre**), Tout et ▲ (remonter en haut de la liste). Clic droit sur un contact : Supprimer.
- **Fenêtre Ajouter** : les infos de l'indicatif s'affichent 0,05 s après la frappe (nom, QTH, locator, commentaire du dernier contact…) ; la liste du dessous montre les contacts de l'indicatif et de ses variantes /P /M /MM /QRP (case **Recherche approfondie** pour xx/indicatif/xxx) ; double-clic sur un ancien contact = l'ouvrir pour le modifier (bouton **Mise à jour**) ; boutons **qrz.com** et **qrzcq** = ouvrir la page de l'indicatif.
- **Configuration** : **Station** (indicatif, prénom, locator : la distance de chaque contact est calculée à sa réception ; Outils > Vérifier les distances complète les anciens), **DX Cluster** (clusters enregistrés, connexion / reconnexion automatiques, réglette, trames, curseurs VFO A / B), Importer / partager…, Sauvegarde ADIF, Base…, Ports….
- **DX Cluster** : une réglette à droite montre les indicatifs de la bande (Auto = bande du poste, ou bande choisie) avec les curseurs VFO A (rouge) et VFO B (bleu) ; **molette = zoom**, glisser = déplacer, **clic sur un indicatif = le poste se règle** (fréquence et mode), clic droit = filtres (numérique, CW, phonie, FreeDV). Les trames s'affichent en bas, avec les boutons **Cluster** (clusters actifs) et **Alerte** (plusieurs lignes de texte reliées par « ou » / « et », un bip retentit sur un spot en direct qui correspond — attention : le bip sort par la carte son de Windows).
- **Thèmes** : 40 thèmes (de Windows 3.11 au plus moderne, mer, nature, rétro) appliqués à toutes les fenêtres du journal ; **Thèmes > Personnaliser les couleurs** : tablette de 64 couleurs + RVB, mémorisé pour chaque thème.
- **Fermer la télécommande** : si le journal est ouvert, il reste ouvert (postes déconnectés, aucun lien avec le poste) ; un nouveau clic sur HamRadioLink ramène la télécommande (une seule copie du programme à la fois).
- **QSO de WSJT-X reçus automatiquement** : chaque contact validé arrive dans le journal courant (doublons ignorés). HamRadioLink écoute en UDP : **2333** (diffusion « N1MM Logger+ » de WSJT-X : Réglages > Rapports, cochez la case et mettez le port 2333), **2237** (Serveur UDP de WSJT-X, au même endroit), **7825** (ADIF brut), + 2 ports libres. Un port ne peut être écouté que par un seul programme à la fois. Réglages : **Paramètres ⚙ > Réglages des ports** (ou bouton « Ports… » du journal), 0 = inutilisé.
- **Envois en ligne** (Outils) : QRZ.com (clé API du journal, abonnement QRZ), Club Log (e-mail, mot de passe et **clé API de logiciel** à demander au support de Club Log), eQSL, LoTW (TQSL installé et nom de la « Station Location ») se règlent dans **Outils > Online services**. **Envoyer à tous les logs en ligne** envoie la sélection (ou ce qui n'est pas encore marqué « envoyé ») dans une fenêtre de progression qui ne bloque pas le journal. Colonnes « eQSL sent », « LOTW sent », « Club Log sent », « QRZ.com sent ». Clic droit sur des contacts : Exporter la sélection (ADIF) ou **pour LoTW** (fichier .tq8 signé par TQSL, à déposer à la main sur le site de LoTW), Envoyer la sélection à…, Marquer envoyé / non envoyé. Si QRZ.com refuse l'envoi, un fichier ADI est créé et la page d'import de QRZ.com s'ouvre.
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
