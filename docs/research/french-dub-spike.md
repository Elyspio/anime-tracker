# Spike VF — Crunchyroll et ADN depuis la sortie NordVPN Suisse

Réalisé le 2026-09-29, à travers le pod qBittorrent (client NordVPN, **Zurich**, AS25369 Hydra
Communications, un hébergeur). Sondes jetables, non versionnées. Aucun résultat ci-dessous n'a été
mesuré depuis la France.

**Conclusion courte.** Les deux plateformes publient bien la VF épisode par épisode, et un mapping
automatique sûr est possible. Mais Crunchyroll est protégé par Cloudflare d'une façon **instable et
peu prévisible**, aucun repli par le solveur ne tient, et deux critères de l'ADR 0003 ne sont pas
encore atteints (recoupement humain, stabilité dans le temps). L'ADR reste *proposé*.

> **Suite, le 2026-09-30 — ce que ce rapport a mal lu, et ce qui a été mesuré depuis.**
>
> - **Cloudflare n'est pas instable, il défie la reprise de session TLS.** Mesuré depuis .NET 10 sous
>   Linux : cinq connexions neuves passent sans reprise TLS, toutes celles qui suivent la première sont
>   défiées avec. Les résultats « contradictoires » ci-dessous (`ParseAdd`, HTTP/2) venaient de là :
>   chaque essai ouvrait un nouveau client, donc une session reprise. Une fois la reprise coupée,
>   HTTP/2 et l'user agent .NET restent défiés ; la façon de poser l'en-tête ne compte plus. Depuis
>   Windows, tout est défié : c'est l'empreinte TLS.
> - **La région ne compte pas.** Les VF de 36 shows sont identiques depuis la France et la Suisse, le
>   catalogue ADN aussi. Le propriétaire a précisé que le VPN sert à protéger l'adresse du réseau
>   local d'un bannissement, pas à choisir un pays.
> - **Le solveur a été retiré** du pod, sur décision du propriétaire.
> - **Crunchyroll se trompe parfois de date** : l'épisode 1 de « The Oblivious Saint » est daté de
>   2025. `premium_available_date`, juste, sert de seconde date d'alignement.
> - **Deux synchros réelles de l'été 2026**, à un jour d'écart, ont donné les mêmes appariements et des
>   VF qui avancent comme attendu (Clevatess II de 12/13 à 13/13).
>
> L'[ADR 0003](../adr/0003-vf-from-platforms-through-vpn-egress.md) est accepté sur cette base.

## Ce qui a été monté

Le solveur (FlareSolverr 3.5.2) et un proxy sortant (tinyproxy, liste blanche de domaines) tournent
comme conteneurs du pod qBittorrent : ils partagent son réseau, donc la sortie NordVPN. Le Service
`dub-solver` les expose dans le namespace `apps` (8191 solveur, 8888 proxy). Les mesures passent par un
`kubectl port-forward`. Les changements sont dans le dépôt d'infrastructure
(`kubernetes/apps/torrent`), non commités.

- Le proxy refuse `example.com` et tout `CONNECT` hors du port 443 (vérifié).
- Chaque conteneur attend l'interface `nordlynx` avant de servir, et n'est prêt que tant qu'elle existe :
  avant le tunnel, le trafic sortirait par l'adresse du cluster.

## Crunchyroll

### Accès

- Un jeton anonyme s'obtient avec l'identifiant client public que la page d'accueil publie dans sa
  propre configuration (`accountAuthClientId`). L'identifiant est lu dans la page, pas écrit en dur :
  celui que je croyais connaître était faux.
- Le jeton dit `"country":"CH"` : la région vue est bien la Suisse.

### Forme des données

- `GET /content/v2/cms/series/{id}/seasons` : chaque saison porte `versions[]` (une par langue audio,
  `fr-FR` comprise) et `number_of_episodes`.
- `GET /content/v2/cms/seasons/{id}/episodes` : chaque épisode porte `versions[].audio_locale`. **C'est la
  mesure utile** : les langues audio disponibles pour cet épisode.
- **`is_dubbed` ne veut rien dire pour le français.** Mushoku Tensei III épisode 12 : `is_dubbed: true`,
  mais `versions` = `ja-JP`, `en-US` seulement. Il faut lire `versions`.
- **`episode_number` continue d'une saison à l'autre** (13, 25…), `sequence_number` est relatif à la
  saison (1, 2, 3…). L'alignement avec AniList doit utiliser le second.
- Les dates de disponibilité sont masquées à un client anonyme (`9998-11-30`) et tout est
  `premium_only` : **une VF « annoncée mais non lisible » n'est pas identifiable**. On sait qu'une
  version existe, pas qu'on peut la lancer.

### Cloudflare

`POST /auth/v1/token` depuis .NET 10 sous Linux, à travers la même sortie, trois essais par ligne :

| Configuration du client                                       | Résultat |
| ------------------------------------------------------------- | -------- |
| HTTP/1.1, User-Agent Chrome posé par requête                  | 200 ×3   |
| HTTP/1.1, `DefaultRequestHeaders.UserAgent.ParseAdd` (Windows) | 403 ×3   |
| HTTP/1.1, `ParseAdd`, User-Agent renvoyé par le solveur       | 200 ×3   |
| HTTP/2, quel que soit l'en-tête                               | 403 ×3   |
| User-Agent par défaut de .NET                                 | 403 ×3   |
| Node (undici), User-Agent Chrome                              | 403      |
| curl, User-Agent Chrome                                       | 200      |

Un 403 de ce type porte `cf-mitigated: challenge` et une page « Just a moment » : il se distingue
d'un catalogue vide. Mais **le comportement dépend de détails du client que je n'ai pas su isoler**, et
il changera avec un runtime ou une règle Cloudflare. Il ne faut pas l'appeler un contrat.

### Le solveur

- FlareSolverr résout la page d'accueil en 7 s et rend un `cf_clearance` (« Challenge not detected » :
  un vrai navigateur passe sans défi).
- **Ce cookie ne sert à rien à un client HTTP, il aggrave l'échec** : présenté par .NET, il fait passer
  200 ×3 à 403 ×3. Il est lié à l'empreinte du navigateur qui l'a obtenu.
- FlareSolverr n'accepte pas d'en-tête personnalisé, donc pas d'`Authorization: Basic` ni de
  `Bearer` : il ne peut pas faire les appels d'API lui-même.
- La page de série rendue par le solveur est une coquille SPA sans données de doublage.

Le solveur n'a donc **aucun rôle utile** dans le flux mesuré. Ce qui fait passer, c'est le client.

### Mapping sur les 50 premiers shows

| Saison                  | Résolus par lien | Par recherche | Sans correspondance |
| ----------------------- | ---------------- | ------------- | ------------------- |
| Été 2026 (terminée)     | 36               | 0             | 14                  |
| Automne 2026 (à venir)  | 5                | 6             | 39                  |

- **Accord avec AniList : 8 sur 8** séries retrouvées par recherche coïncident avec l'id du lien AniList,
  0 désaccord. Sur les 23 shows que AniList dit être chez Crunchyroll à l'automne, la recherche en
  retrouve 11.
- Les échecs sont surtout des titres : Crunchyroll nomme la série de la franchise en anglais (« Black
  Clover » pour « Black Clover 2nd Season »), AniList n'a pas encore de titre anglais pour une saison
  récente, et la recherche est approximative dès qu'un marqueur de saison figure dans la requête. Il faut
  retirer les marqueurs de saison et, à défaut de titre anglais, remonter à un titre de la franchise.
- **Le drapeau `isDisabled` d'AniList est un faux positif** pour Crunchyroll : les 7 liens profonds qu'il
  a désactivés à l'automne pointent tous vers une série qui existe et correspond (7 sur 7).
  Sur les 50 premiers, 12 shows portent un lien profond, activé ou non.

### Alignement saison et épisodes

Sur l'été 2026, 36 shows résolus :

- **28** ont une saison dont le nombre d'épisodes égale le total annoncé ;
- **7** sont ambigus par le nombre (plusieurs saisons de 12) ou n'ont pas de saison au bon compte ;
- **la date du premier épisode lève les 7 cas sur 7** : `episode_air_date` de Crunchyroll contre le
  premier créneau AniList lu en heure japonaise, à un jour près.

Le nombre d'épisodes seul est donc une mauvaise clé ; la date du premier épisode est la bonne. Le
plan l'interdisait déjà implicitement (« ne pas se contenter du titre et du nombre ») ; il le dit
maintenant.

### Tableau de recoupement — à vérifier contre le site

**Non fait : c'est le critère « recoupement à la main » de l'ADR.** Voici les VF françaises que la sonde
lit, à comparer à ce que affiche crunchyroll.com. Les séries sont identifiées par leur id.

| Titre AniList                            | Série CR     | Épisodes AniList | Épisodes FR lus |
| ---------------------------------------- | ------------ | ---------------- | --------------- |
| Mushoku Tensei III                       | G24H1N3MP    | 14               | 1 à 11          |
| BLACK TORCH                              | GT00377907   | 12               | 1 à 12          |
| Clevatess II                             | G8DHV78ZM    | 13               | 1 à 12          |
| Otomege Sekai wa Mob ni Kibishii… 2      | G0XHWM0D3    | 12               | 1 à 9           |
| Futsutsuka na Akujo… Suuguu-hen          | GT00371881   | 11               | 1 à 9           |
| Tenmaku no Jaadugar                      | GT00378078   | 12               | 1 à 7           |
| Seihantai na Kimi to Boku 2nd Season     | GT00365624   | 13               | 1 à 4           |
| Nige Jouzu no Wakagimi 2nd Season        | GQWH0M19X    | 12               | 8 épisodes, numérotés 13 à 20 (continu) |

Trois shows résolus sans VF lue : Super no Ura de Yani Suu Futari, Youjo Senki II, Kimi ga Shinu made
Koi wo Shitai.

## ADN

- Accessible depuis la Suisse, sans anti-bot : passerelle AWS, `X-Target-Distribution: fr`.
- `languages` par série **et par épisode** (`["vostf","vf"]`), avec `releaseDate` et `available` : la
  forme est plus simple et plus fiable que celle de Crunchyroll.
- Le catalogue entier tient dans 594 séries (258 avec une VF), lisible en 12 requêtes.
- **La couverture est faible** : 6 des 50 shows d'automne et 4 de l'été se retrouvent par titre, dont
  2 (automne) et 0 (été) avec de la VF. ADN ne diffuse qu'une petite partie de la saison.
- Même problème de titres (« Reincarnated as a Sword » pour « Tensei Shitara Ken Deshita »).
- Aucun lien ADN dans les `externalLinks` d'AniList : la recherche par titre est la seule voie.

## Les critères de l'ADR 0003

| Critère                                        | État |
| ---------------------------------------------- | ---- |
| Réponse vérifiable (recoupement à la main)     | **Non atteint.** Cohérence interne bonne (accord 8/8 avec AniList), mais le tableau ci-dessus reste à comparer au site par un humain. |
| Distingue un catalogue vide d'un blocage       | **Atteint.** Crunchyroll : 403 + `cf-mitigated: challenge` contre 200. ADN : JSON contre 4xx JSON. |
| Solveur ou navigateur documenté                | **Atteint, avec une surprise.** Pas requis dans la configuration qui passe, inutilisable en repli. |
| Couverture mesurée                             | **Atteint.** Voir les tableaux. |
| Région vue depuis la sortie retenue            | **Atteint pour la Suisse** (`country: CH`). **Non mesuré : ce que verrait un spectateur français.** |
| Stabilité dans le temps                        | **Non atteint.** Une seule session de quelques heures, une seule IP de sortie. |
| VF annoncée mais non lisible identifiable      | **Non atteint pour Crunchyroll** (dates masquées, tout `premium_only`). ADN expose `available`. |

## Ce que ça change au plan

1. **Alignement** : saison choisie par la date du premier épisode, épisodes numérotés par
   `sequence_number`, jamais par le nombre d'épisodes seul. Traduire l'alignement en test, avec les
   sept cas ambigus de l'été comme fixtures.
2. **Mapping** : retirer les marqueurs de saison des titres, chercher sous le titre anglais quand il
   existe, sinon remonter au titre d'une saison précédente de la franchise ; ignorer `isDisabled` ;
   verrouiller sur le double critère titre + date.
3. **Client Crunchyroll** : traiter tout 403 `cf-mitigated: challenge` comme **inconnu**, jamais comme
   catalogue vide, et alerter. Aucune tentative de repli par le solveur.
4. **Solveur** : dans l'état actuel, il n'est pas nécessaire. L'ADR 0003 doit dire que le propriétaire
   l'a accepté *par précaution* et que la mesure ne le justifie pas ; le retirer réduirait le pod.
5. **ADN** : couverture trop faible pour justifier seul une tranche ; à décider (voir les questions
   ouvertes).

## Questions ouvertes

- **Suisse ou France ?** Le propriétaire a choisi de garder le solveur dans le pod qBittorrent, donc la
  Suisse. Une passe courte depuis une IP française (ou un VPN France) dirait si le catalogue de VF
  diffère. Elle n'a pas été faite : le plan interdisait la sonde depuis l'IP résidentielle.
- **Stabilité** : rejouer la sonde sur plusieurs jours, avec plusieurs IP de sortie.
- **Recoupement humain** du tableau ci-dessus.

## Écart de procédure

Un test du filtre du proxy a envoyé deux requêtes vers `crunchyroll.com` depuis l'IP résidentielle
(via un conteneur Docker local), avant que la sortie Suisse existe. Elles n'ont servi à rien d'autre
qu'à vérifier que le domaine passait le filtre.
