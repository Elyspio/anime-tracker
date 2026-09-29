# Plan — filtre « French dub » (VF)

Statut : **brouillon révisé après une revue (gpt-6-sol), non implémenté.** Aucun code n'est écrit. Un
spike jetable valide d'abord les sources (tranche 1) ; ses critères de sortie sont bloquants.

## Besoin

« Ajouter un filtre pour vérifier que tous les épisodes bingeable sont sortis avec les voix
françaises — un mode *French VA only*. » En plus : pouvoir naviguer vers le site de streaming depuis
l'application, et pas seulement vers AniList.

## Pourquoi c'est un vrai chantier

AniList ne dit **rien** du doublage. Vérifié : la requête actuelle n'en demande pas, et
`externalLinks.language` est vide sur tous les liens de streaming d'une saison (langue du lien, de
toute façon, pas de l'audio d'un épisode). La seule information proche est `voiceActors(language:
FRENCH)` sur les personnages : « un doubleur français est crédité », ce qui ne dit pas qu'un épisode
est sorti en VF. Il faut donc une **seconde source**.

Mesuré sur les 50 animes les plus populaires de l'automne 2026 (requête AniList, `externalLinks`) :

| Plateforme  | Shows avec un lien |
| ----------- | ------------------ |
| Crunchyroll | 23                 |
| Netflix     | 6                  |
| YouTube     | 5                  |
| HIDIVE      | 3                  |
| ADN         | 0                  |

Sur les 23 liens Crunchyroll, **12 seulement** portent un `/series/<id>` exploitable ; 9 pointent
vers la page d'accueil nue, les autres vers un slug sans id. Un mapping « lien AniList strict »
couvrirait donc ~24 % des shows populaires. ADN n'apparaît jamais dans les liens AniList.

## Décisions verrouillées

### Sens du mode

- **Un seul contrôle ordonné** dans la barre de filtres : `French dub` = `Any` / `Up to date` /
  `Complete`. `Complete` est inclus dans `Up to date`.
- **La comparaison porte sur des ensembles d'épisodes, jamais sur des totaux.** Pour une plateforme
  donnée, on dispose de l'ensemble des numéros AniList dont la VF est disponible (après alignement,
  voir plus bas). Les VF 1, 2 et 4 sans la 3 ne valent pas « 3/3 ».
- **Up to date** : au moins un épisode est sorti au Japon (sinon « tous les épisodes sortis » serait
  vrai par vacuité), et **chaque** numéro sorti figure dans l'ensemble VF d'**une même plateforme**.
- **Complete** : le total annoncé par AniList est connu, et **chaque** numéro de 1 à N figure dans
  l'ensemble VF d'une même plateforme. La condition porte sur la **donnée** (total connu), pas sur le
  statut `UnknownEnd` : `BingePredictor` produit `UnknownEnd` aussi pour un total connu sans créneau
  publié, et ce cas-là peut être `Complete`.
- **Une seule plateforme** doit satisfaire la condition (on regarde à un seul endroit). Pas d'union.
- **VF inconnue** : aucune série résolue, alignement non démontré, ou mesure impossible (voir
  « Région ») → aucun badge, **exclu** de `Up to date` et `Complete`. Une VF **inconnue** n'est jamais
  présentée comme une VF **absente**.
- **Reporté** : compte à rebours VF. Les plateformes publient probablement la présence, pas la date.

### Sources

- **Crunchyroll** (API non officielle, `audio_locales` par épisode, `fr-FR`) et **ADN**, chacune
  derrière un port `IDubSource`, **un adapter par plateforme**, un `Add*` par projet, câblés dans
  `Web/Program.cs` uniquement.
- Netflix, Prime Video, Disney+ : hors v1 (pas d'API publique connue d'audio par épisode).
- **Nautiljon reste exclu** (ADR 0002).
- **Le solver est assumé par le propriétaire**, et le déploiement passe par NordVPN (voir
  « Déploiement »). Cela contredit
  l'ADR 0002 (« aucun solver, aucun navigateur ») : l'ADR 0003 amende ce point, limité aux sources
  VF ; **AniList reste sans solver.** Les conditions d'utilisation de Crunchyroll interdisent
  l'extraction automatisée et le contournement des protections d'accès : c'est un **risque assumé
  par le propriétaire, écrit noir sur blanc dans l'ADR 0003**, pas une autorisation que l'ADR
  conférerait. Les critères de sortie du spike ci-dessous le bornent.

### Mapping AniList vers plateforme

1. Lien `externalLinks` d'AniList portant un id de série de la plateforme.
2. Sinon, recherche dans le catalogue de la plateforme. Un candidat n'est retenu que si le **titre
   normalisé** (romaji, anglais, natif, synonymes) **ET** le **nombre d'épisodes ou l'année**
   concordent. Sinon : VF inconnue.
3. **Override admin** (voir « Stockage »).

Un mauvais appariement est **pire** qu'une absence : c'est le poison silencieux de l'ADR 0002.

**Alignement des épisodes.** Une série de plateforme regroupe souvent plusieurs saisons, et l'entrée
AniList est un cour. Le mapping ne s'arrête pas à la série : il **résout la saison ou la partie
précise**, puis conserve une **correspondance explicite « numéro AniList vers épisode de plateforme »**.
Cas refusés (VF inconnue) : numérotation absolue, saisons regroupées non séparables, épisodes
fractionnés, spéciaux et récapitulatifs. Un **surplus** d'épisodes côté plateforme (24 contre 12) est
un **signal à examiner**, pas une preuve de couverture.

### Stockage

- **Collections séparées**, clé stable **`(id AniList, plateforme)`**. `AnimeRepository.Refresh`
  remplace les documents qu'il reçoit mais **ne supprime pas** ceux devenus absents : la VF a donc sa
  propre politique de réconciliation.
- Deux natures de donnée, séparées :
  - **Mesure** (numéros d'épisodes doublés, série et saison résolues, correspondance des numéros,
    mode d'appariement, région et locale de la requête, date d'observation). Remplacée à chaque
    synchro. Supprimée quand l'anime a disparu de la saison.
  - **Override admin**, à trois états : `Auto` (aucun override), `Pinned(url)`, `Blocked`. Ne
    disparaît jamais tout seul. Le corps de l'API distingue explicitement « aucun override » de
    « bloquer tout appariement ».
- L'URL d'un `Pinned` est **validée contre le domaine de la plateforme** (liste blanche, pas de
  redirection suivie vers un autre domaine).
- **Revalidation** : un id de série mis en cache est revérifié à chaque synchro ; un 404 le fait
  tomber et relance la recherche. Un `Pinned` invalide est signalé dans « cas à corriger », pas écrasé.
- **Jamais** le verdict `Up to date` / `Complete` : c'est une fonction pure calculée à la lecture,
  comme `BingePredictor`.

### Région et fiabilité de la mesure

- La VF dépend du titre, de l'abonnement et du **territoire**. La mesure est prise depuis le serveur
  déployé, pas depuis le spectateur : on enregistre la région et la locale de la requête, et le badge
  dit **ce qui a été vérifié** (« Checked on ADN, FR, 2026-09-29 »), pas une promesse pour le lecteur.
- Une **réponse anti-bot, une restriction géographique ou une erreur** est traitée comme **inconnue**,
  jamais comme catalogue vide ni comme absence de VF. La mesure précédente est conservée.
- L'abonnement du spectateur n'est pas modélisé : le badge dit « disponible sur la plateforme », pas
  « gratuit ».
- Une plateforme non suivie n'est jamais présentée comme « sans VF » : le libellé nomme la plateforme
  mesurée.
- **Vérifier au spike** que la sortie du serveur déployé est bien vue depuis la France ; ne pas le
  supposer. La sortie déployée est un serveur **NordVPN France** (voir « Déploiement ») : le spike est
  lancé depuis cette même sortie et nulle part ailleurs.
- **La région enregistrée est déclarée en configuration** (`Dub:Region`) et corroborée au spike ; elle
  n'est pas devinée à l'exécution par un appel à un service de géolocalisation tiers.
- **Un VPN ne vérifie rien.** Il change la région vue, pas la véracité de ce qui revient (ADR 0002) :
  le recoupement à la main reste le critère de sortie du spike.

### Synchro et runs

- Le **refresh AniList reste inchangé et rapide** (~1 s).
- **`RefreshRun` gagne un type** (`Season` ou `Dub`). Les runs existants, sans type, se lisent
  comme `Season` (aucune migration de données).
- **Exclusion concurrente par (saison, type)** : `GetActive` prend le type. Un 409 renvoie le run
  actif **du même type** que celui demandé. Aujourd'hui `GetActive` et `findRunFor` (front) ignorent
  le type : les deux changent.
- **Enchaînement** : en fin d'écriture AniList, le service **crée le run `Dub` en file avant de le
  mettre en file d'attente Hangfire**. Si la mise en file échoue, ce run est marqué `Failed` avec le
  message : la trace écrite existe, et le run AniList reste `Succeeded` puisque ses données sont
  stockées. Transitions à figer dans `AnimeServiceTests`.
- **Résultat périmé** : la synchro VF porte l'identifiant du run AniList dont elle dérive. Avant
  d'écrire, elle compare au dernier run AniList réussi de la saison ; s'il existe un plus récent, elle
  **abandonne** son écriture. Deux refreshes rapprochés ne peuvent plus écraser le neuf par du vieux.
- **Échec de la synchro VF** : dernière valeur conservée, erreur écrite sur le run
  (`exception.Message`, jamais de stack trace). **Aucun retry automatique** (`GlobalJobFilters`).
- **Front** : `findRunFor` filtre par type ; l'indicateur et le tiroir distinguent les deux ;
  la requête des animes est **invalidée quand un run `Dub` se termine** (elle l'est déjà quand un run
  observé se termine, à étendre au nouveau type) ; l'historique n'est plus limité aux 20 derniers
  runs tous types confondus, une synchro VF de plusieurs minutes ne doit pas masquer les refreshes
  AniList.
- Le bouton et le job nocturne restent l'unique chemin d'entrée : ils lancent le refresh AniList, qui
  enchaîne la VF. Pas de déclencheur séparé.

### Interface — tous les textes en anglais

Le produit est anglophone (ADR 0002) ; seuls ces documents sont en français.

- Contrôle `French dub` : `Any` / `Up to date` / `Complete`.
- **Badge neutre** (encre) sur la carte et dans le tableau : `FR dub 8/12 · ADN`. **Jamais vert** : le
  vert de succès est réservé au bingeable japonais (invariant). Tooltip : plateforme, région, date de
  la dernière synchro.
- **Popover au clic** sur une carte ou une ligne : logos des destinations, dont **AniList** et chaque
  plateforme. La marque VF apparaît sur la plateforme concernée. Le popover s'ouvre **même s'il n'y a
  que le lien AniList**.
- Liens : lien profond des plateformes résolues d'abord ; sinon les liens de streaming d'AniList
  **sans les pages d'accueil nues**.
- **Logos** : SVG embarqués dans le front (AniList, Crunchyroll, ADN, Netflix, HIDIVE, YouTube), repli
  texte pour un site inconnu. Aucune requête tierce.
- La carte est aujourd'hui **un seul lien** (`ButtonBase href`) vers AniList : elle devient un bouton
  qui ouvre le popover, dont AniList est la première entrée.
- **Tiroir « cas à corriger »**, comme celui des refreshes, visible quand on est connecté : liste les
  animes de la saison sans correspondance, à faible confiance, ou dont un `Pinned` est invalide. On
  colle l'URL de la série de la plateforme, ou on choisit « aucune ». L'UI ne décode pas le token :
  elle appelle l'API et affiche le 403.

### API

- `GET /api/animes` (anonyme) porte les champs VF calculés côté back par une classe pure et statique
  (meilleure plateforme, épisodes doublés, `upToDate`, `complete`) et la liste des liens de
  destination. Le front ne fait que filtrer.
- Liste des cas à corriger : `GET`, anonyme (donnée publique).
- Override : `PUT`, `[Authorize(AuthModule.AdminPolicy)]`, corps `{ mode: auto | pin | block, url? }`
  par plateforme. **Toute nouvelle route qui mute doit opter explicitement pour l'autorisation.**
- `POST /api/animes/refresh` est inchangé.

## Déploiement

- **Pod dédié « solveur + VPN »** : un Deployment séparé dans le cluster, contenant le solveur et le
  client NordVPN, sortie **France**. **Tout le trafic VF** (Crunchyroll et ADN) passe par lui : une
  seule région mesurée, un seul chemin.
- **Le pod de l'app reste direct.** AniList, la validation des tokens Keycloak, MongoDB et le trafic
  entrant ne passent pas par le tunnel. Raison : tous les conteneurs d'un pod partagent le même réseau,
  donc un client VPN en sidecar de l'app mettrait tout le pod derrière le tunnel, et la limite d'AniList
  (30 requêtes par minute et par IP) serait partagée avec d'autres clients du même serveur.
- L'app joint le solveur par un point d'accès configuré (`Dub:Solver:Endpoint`). **Solveur ou VPN
  injoignable = la synchro VF échoue**, la dernière valeur est conservée et l'erreur est écrite sur le
  run : ni le refresh AniList ni l'affichage ne sont touchés.
- **Le pod de l'app reste en réplique unique, `Recreate`** (le serveur Hangfire et le refresher sont des
  singletons). Le pod solveur est indépendant de cette contrainte.
- Le chart vit dans le **dépôt d'infrastructure**, pas ici : il faut y ajouter le Deployment solveur +
  VPN, son Service, et le secret NordVPN. **Ce dépôt ne reçoit jamais d'identifiant NordVPN.**
- En développement, l'AppHost lance un solveur local sans VPN : la région mesurée n'y est pas garantie
  française, ce que la région enregistrée avec chaque mesure rend visible.
- Les identifiants et jetons de plateforme obtenus par la synchro ne sont pas persistés au-delà de la
  synchro en cours.

## Invariants à amender, avant qu'une tranche ne les contredise

`AGENTS.md` dit aujourd'hui : la source runtime est AniList seule, un refresh se fait en une
récupération d'environ une seconde, l'unique action privilégiée est le refresh, aucun solver. Le plan
change chacun de ces points. **`AGENTS.md`, `CONTEXT.md` et l'ADR 0003 sont amendés dans la tranche 1,
et aucune tranche suivante ne fusionne avant.** `CONTEXT.md` gagne : VF, plateforme, `Up to date`,
`Complete`, run VF, override.

## Hypothèses tacites rendues explicites

- Les métadonnées audio d'une plateforme décrivent une VF **publiée et regardable**. À confirmer au
  spike (une VF annoncée mais non lisible ne doit pas compter).
- Le catalogue interrogé correspond à la France.
- Identifiants, liens profonds et regroupements de saisons restent stables.
- Une absence de réponse veut dire « inconnu », jamais « pas de VF ».
- Le spectateur a accès à la plateforme (et à l'abonnement, pour ADN).
- Politesse envers les plateformes : concurrence bornée par plateforme, délai entre requêtes,
  calibrés au spike ; une série déjà résolue n'est pas re-cherchée à chaque synchro.

## Tranches (une PR chacune, depuis `main`)

La PR #3 (mise à jour de la stack) est ouverte et non fusionnée ; on part de `main`, conflits possibles
sur les fichiers de dépendances. **Ordre révisé** : les outils de correction du mauvais appariement
existent avant que la synchro ne soit visible.

1. **Spike + ADR 0003 + amendements** d'`AGENTS.md` et `CONTEXT.md`. Sonde jetable Crunchyroll puis
   ADN. **Critères de sortie, bloquants pour la source concernée :**
   - la réponse est **vérifiable** : recoupement à la main d'une dizaine de shows contre le site
     public, sans écart ;
   - la réponse **distingue** un catalogue réellement vide d'un blocage anti-bot ou d'une
     restriction ;
   - le **besoin d'un solver ou d'un navigateur** est documenté, avec les conditions d'utilisation
     citées, et accepté explicitement par le propriétaire dans l'ADR ;
   - la **couverture** mesurée sur une vraie saison (part des 50 premiers résolue par le mapping) ;
   - la **région vue** depuis un serveur **NordVPN France** est confirmée (le spike ne part **pas** de
     l'IP résidentielle : le résultat serait trop optimiste) ;
   - la **stabilité** : la sonde est rejouée plusieurs jours, sur plusieurs IP de sortie, pour savoir
     si une IP de VPN partagée est bloquée ou signalée de façon récurrente ;
   - une VF **annoncée mais non lisible** est identifiable.
   Sans voie acceptable, la source est écartée.
2. **Liens de streaming** : `externalLinks` ajouté à la requête AniList et stocké, popover, logos
   embarqués. Livre la seconde demande sans aucun risque VF.
3. **Back VF, sans exposition à l'écran** : modèle, port, collections, adapter Crunchyroll, client du
   solveur (`Dub:Solver:Endpoint`, `Dub:Region`), solveur local dans l'AppHost, type de run, exclusion
   par (saison, type), enchaînement, garde anti-périmé, mapping validé avec alignement d'épisodes,
   endpoints d'override et de cas à corriger. **En parallèle, dans le dépôt d'infrastructure** : le pod
   solveur + VPN, avant tout déploiement de cette tranche.
4. **Front VF** : `French dub`, badge, liens profonds et marque VF dans le popover, tiroir des cas à
   corriger, front sensible au type de run.
5. **ADN**, sur le même contrat éprouvé.

Si une plateforme est condamnée par le spike, seule sa tranche est perdue.

## Tests — critères d'acceptation de chaque tranche

- **Aucun test n'atteint le réseau.** Adapters pilotés par `FakeHttpMessageHandler`.
- Réponses **enregistrées** sous `Fixtures/` pour chaque plateforme (forme de l'API, à re-enregistrer
  en rejouant la requête de l'adapter) ; règles de mapping et de couverture sur des **nœuds écrits à
  la main**.
- **Couverture** (`AnimeTracker.Core.Tests`) : VF 1, 2 et 4 sans la 3 ; zéro épisode sorti ; total
  connu sans créneau publié ; 13 épisodes de plateforme contre 12 AniList ; saisons groupées ;
  numérotation absolue ; épisodes fractionnés ; double épisode ; simuldub publié plus tard dans la
  journée ; VF annoncée mais non lisible.
- **Mesure** : géorestriction ou défi anti-bot présenté comme catalogue vide ; réponse partielle ;
  série introuvable après avoir été résolue.
- **Séquence** (`AnimeServiceTests`, tous les ports substitués) : échec de mise en file du run VF ;
  deux refreshes rapprochés et résultat périmé écarté ; redémarrage avec un run VF en cours
  (`Interrupted`) ; exclusion concurrente par type et 409 du bon type ; anciens runs sans type lus
  comme `Season`.
- **Stockage** (`MongoMappingsTests`) : aller-retour BSON sans serveur, enums stockés par nom, trois
  états d'override, mesure supprimée quand l'anime disparaît, override conservé.
- **Override** : URL hors domaine, redirection vers un autre domaine, `Pinned` devenu invalide.
- Nouveaux enums exposés en JSON (type de run, mode d'override, plateforme) : `EnumContractTests`
  **et** `types.test.ts` (`Record<Union, true>`).
- **Front** : `ranking.test.ts` pour le filtre (inconnu exclu, `Complete` inclus dans `Up to date`) ;
  `refreshRuns.test.ts` pour `findRunFor` par type.
- Assertions Shouldly, doubles NSubstitute.

## Risques

- **Source non officielle, hors conditions d'utilisation.** Peut casser sans préavis ou renvoyer des
  données trompeuses. Isolée derrière un port : un échec ne bloque ni le refresh AniList ni
  l'affichage. Risque juridique assumé par le propriétaire, non atténué par la technique.
- **Faux appariement silencieux.** Mitigation : double critère, alignement explicite, override admin,
  cas à corriger visibles, tranche des outils de correction avant l'exposition. Rien à l'écran ne
  distingue une VF fabriquée d'une vraie : c'est le critère de rejet principal de l'ADR 0002.
- **Retour du solver, avec un VPN.** Un pod de plus, un chart dans le dépôt d'infrastructure, un
  secret NordVPN. Une IP de VPN partagée est plus souvent signalée par les protections anti-bot qu'une
  IP résidentielle : c'est le risque que le critère de stabilité du spike mesure.
- **Couverture réelle inconnue** tant que le spike n'a rien mesuré.
- **Faux négatifs de région et de plateforme** : la mesure vaut pour le serveur, pas pour le lecteur ;
  le badge dit ce qui a été vérifié.

## Revue gpt-6-sol — traitement

| Remarque                                                    | Traitement |
| ----------------------------------------------------------- | ---------- |
| 1. Conditions d'utilisation et solver                       | Critères de sortie bloquants au spike ; risque écrit dans l'ADR ; **le solver reste accepté par le propriétaire**, la revue demandait de ne pas s'en remettre à l'ADR seul. |
| 2. `>=` déclare une VF complète pour le mauvais cour        | Supprimé : alignement explicite numéro à numéro, surplus = signal. |
| 3. Ensembles plutôt que totaux, vacuité                     | Adopté. |
| 4. `Complete` contre `UnknownEnd`                           | Condition sur la donnée (total connu), plus sur le statut. |
| 5. Enchaînement sans résultat défini, résultat périmé       | Run VF créé avant la mise en file, garde par run AniList source. |
| 6. 409 et historique ambigus                                | Type de run, exclusion par (saison, type), historique et front adaptés. |
| 7. Cohérence du stockage                                    | Clé `(id, plateforme)`, mesure/override séparés, revalidation, trois états. |
| 8. Région du serveur contre celle du spectateur             | Région enregistrée, badge factuel, blocage = inconnu. |
| 9. Invariants modifiés sans le dire                         | Amendés en tranche 1 ; textes d'interface en anglais (corrigé : le plan les écrivait en partie en français). |
| 10. Ordre des tranches                                      | Réordonné : outils de correction avant exposition. |
| 11. Cas de test manquants                                   | Ajoutés comme critères d'acceptation. |
