# Plan — filtre « French dub » (VF)

Statut : **livré**, en quatre PR empilées (voir « Tranches »). Les versions précédentes de ce plan, avec
la région mesurée, le solveur et la revue gpt-6-sol, sont dans l'historique git ; celle-ci décrit ce qui
est construit. Décision : [ADR 0003](../adr/0003-vf-from-platforms-through-vpn-egress.md). Mesures :
[rapport du spike](../research/french-dub-spike.md).

## Besoin

« Ajouter un filtre pour vérifier que tous les épisodes bingeable sont sortis avec les voix
françaises. » Et pouvoir filer vers le site de streaming depuis l'application, pas seulement AniList.

## Ce qu'on voit à l'écran (en anglais, comme tout le produit)

- **Filtre `French dub`** : `Any` / `Up to date` / `Complete`.
  - *Up to date* : au moins un épisode est sorti au Japon, et **chacun** d'eux est en VF sur **une même**
    plateforme.
  - *Complete* : le total annoncé est connu, et les épisodes 1 à N sont en VF sur une même plateforme.
    Condition sur la donnée, pas sur `UnknownEnd` : un total connu sans créneau publié peut être complet.
  - On compare des **ensembles**, jamais des comptes : les VF 1, 2 et 4 ne font pas « 3/3 ».
  - Un anime jamais apparié a une VF **inconnue** : exclu des deux filtres, jamais présenté comme sans VF.
- **Badge encre** `FR dub 8/12 · Crunchyroll` sur la carte, colonne dans le tableau ; jamais vert.
- **Popover au clic** : AniList, puis la série vérifiée par la synchro (avec son nombre d'épisodes en VF)
  à la place du lien AniList vers la même plateforme, puis les autres liens de streaming d'AniList.
- **Tiroir « Dub matches »** pour un utilisateur connecté : appariements faits sur le seul titre, séries
  trouvées sans saison qui s'aligne, plateformes listées par AniList mais introuvables, overrides. On y
  épingle une série, bloque une plateforme ou revient à l'automatique ; l'API refuse (403) qui n'est pas
  admin.
- **Tiroir des runs** : chaque run dit s'il est de saison ou de VF.

## Comment la VF est mesurée

1. Un refresh AniList réussi met en file la **synchro VF** de la saison (run `Dub`, un seul actif par
   saison ; un refresh reste possible pendant qu'elle tourne).
2. Pour chaque anime déjà diffusé, sur chaque plateforme configurée, les séries sont essayées dans
   l'ordre : série épinglée (seule), série appariée la veille, liens AniList, puis recherche par titre
   (jusqu'à trois formulations, anglais d'abord).
3. Une série n'est retenue que si une de ses saisons a un épisode sorti à **trois jours près** du premier
   épisode AniList (date de diffusion, ou date de mise en ligne quand la première est fausse). Cet
   épisode fixe la numérotation : saisons numérotées à la suite et saisons regroupées sont renumérotées.
   Deux saisons candidates, deux épisodes sur un même numéro : **inconnu**.
4. La mesure — numéros d'épisodes présents et en VF — est stockée à part (`DubMatch`). Le verdict est
   calculé à la lecture.
5. Un défi anti-bot, un refus, une panne ou le proxy injoignable arrêtent la plateforme pour ce run : les
   mesures stockées restent, le run finit en échec avec la raison.

## Où ça passe

- **Proxy sortant** dans le pod qBittorrent (sortie NordVPN) : `dub-proxy.apps.svc.cluster.local:8888`
  dans le cluster, `10.0.1.123:8888` depuis le LAN (DNS interne suggéré : `dub-proxy.apps.elylan`).
  Configuré dans l'application par `Dub:Proxy` ; sans lui, aucune plateforme n'est interrogée.
- **Passerelle Crunchyroll** : un nginx dans le même pod, port 8889, auquel l'application parle en HTTP
  simple (`Crunchyroll:Gateway`). C'est lui qui ouvre la connexion TLS : celle de .NET est défiée par
  Cloudflare sous Windows à chaque fois. Avec la passerelle, la synchro marche aussi depuis l'AppHost.

## Tranches

| PR | Branche                    | Contenu |
| -- | -------------------------- | ------- |
| #4 | `feat/french-dub-adr-0003` | ADR, spike, ce plan, `AGENTS.md`, `CONTEXT.md` |
| #5 | `feat/streaming-links`     | Liens de streaming AniList, popover, logos |
|    | `feat/dub-backend`         | Modèle, runs typés, synchro, Crunchyroll, overrides, API (sur #5) |
|    | `feat/dub-frontend`        | Filtre, badge, popover VF, tiroir des cas (sur le back) |
|    | `feat/dub-adn`             | Adapter ADN (sur le front) |

Ordre de fusion : #4, #5, puis la pile. Le chart `anime-tracker` du dépôt d'infrastructure doit porter
`Dub:Proxy` et `Crunchyroll:Gateway` avant le déploiement de la synchro.

## Reste ouvert

- **Recoupement humain** du tableau de VF du rapport contre le site : jamais fait par une personne. Les
  deux synchros réelles se recoupent entre elles et avec les liens AniList.
- **Stabilité dans le temps** : la synchro nocturne le dira ; un défi Cloudflare se lira dans le tiroir
  des runs.
- **Compte à rebours VF** : reporté. Les plateformes publient la présence, pas de date de sortie à venir.
