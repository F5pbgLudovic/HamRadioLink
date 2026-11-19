# Mentions tierces / Third-party notices

HamRadioLink est écrit en C# pour le .NET Framework 4 fourni avec Windows ; il n'embarque **aucune bibliothèque tierce**.

## Catalogues de commandes
- `catalogues/*.new.cat` (Yaesu FT-450D, FT-710, FT-891, FT-950, FT-991A, FTdx101, FTdx3000 ; Kenwood TS-480, TS-890) et `doc/civ/*.civ` (Icom IC-7000, 7100, 7300, 7300MK2, 7410, 7610, 7760, 9700) listent, pour chaque commande, son code, son format et sa description.
- Ils ont été établis à partir des manuels de référence CAT / CI-V publiés par les fabricants. Les manuels d'origine appartiennent à leurs auteurs ; les catalogues sont fournis pour l'interopérabilité avec le matériel de l'utilisateur et servent au simulateur intégré et à la fenêtre « Ajouter ».
- `src/Catalogs.cs` et `src/CivCatalogs.cs` en sont des copies générées.

## IC7760Link
`src/ic7760/` reprend le code d'IC7760Link (même auteur, GPL v3, https://github.com/F5pbgLudovic/IC7760Link) dans un espace de noms séparé, pour la fenêtre du poste IC-7760.

## com0com
`vendor/com0com/` contient les installateurs signés de **com0com 3.0.0.0** (paires de ports COM virtuels, Vyacheslav Frolov, licence GPL, https://sourceforge.net/projects/com0com/, version signée distribuée par CyberCircuits). Ils sont embarqués dans HamRadioLink.exe et lancés (programme séparé, installation silencieuse) seulement si l'utilisateur accepte, dans la fenêtre « Partage du poste ». Empreintes SHA-256 : x64 `26486b28604b49a9008c54feb11b9ece0008a8287ee5caf0bcf2a62f4317128f`, x86 `de917425e022839f6374b509e3d34f3f9e9773915a44996194442337eadf4a5a`. Le code source de com0com est disponible sur la page du projet.

## TCI
Le protocole TCI est une spécification publique d'Expert Electronics (ExpertSDR3, SunSDR), reprise par Thetis. HamRadioLink n'en utilise que des commandes texte dans une connexion WebSocket.

## Marques
« Yaesu », « Icom », « Kenwood », « Elecraft », « Xiegu », « CI-V » et les noms de postes cités sont des marques de leurs propriétaires respectifs. Ce projet est indépendant et n'est ni affilié ni approuvé par eux.

## Licence du projet
GNU GPL v3 — voir [LICENSE](LICENSE).
