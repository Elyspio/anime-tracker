# La VF vient des plateformes, derrière un solveur et un VPN

Statut : **proposé.** Passe à *accepté* quand les critères de sortie ci-dessous seront tous atteints,
plateforme par plateforme. Le [rapport du spike](../research/french-dub-spike.md) en atteint quatre sur
sept ; le recoupement humain, la stabilité dans le temps et la VF « annoncée mais non lisible »
restent ouverts, et **il contredit une partie de cette décision** (voir « Décision »). Amende
[0002](0002-anilist-replaces-nautiljon.md) sur un point : « aucun solver, aucun navigateur ».

## Contexte

Le produit répond à « quand tous les épisodes seront-ils sortis ? ». Le propriétaire veut aussi savoir
s'ils sont sortis **en version française** : un filtre `French dub` et la navigation vers le site de
streaming.

AniList ne dit **rien** du doublage. La requête n'en demande pas, `externalLinks.language` est vide sur
tous les liens de streaming d'une saison, et `voiceActors(language: FRENCH)` dit seulement qu'un
doubleur français est crédité, pas qu'un épisode est sorti. Il faut une seconde source, et seules les
plateformes de streaming publient l'audio disponible épisode par épisode.

Mesuré sur les 50 animes les plus populaires de l'automne 2026 : 23 ont un lien Crunchyroll, dont 12
portent un `/series/<id>` exploitable ; 9 pointent vers la page d'accueil nue. AniList a marqué
`isDisabled` 25 des 37 liens de streaming, mais ce drapeau est **un faux positif** pour Crunchyroll : les
7 liens profonds qu'il a désactivés pointent tous vers une série qui existe et correspond au show.
ADN n'apparaît dans aucun lien AniList. Sur une saison terminée les liens sont bien mieux fournis
(36 des 50 premiers de l'été). Le mapping ne peut donc pas s'appuyer sur AniList seul pour la saison à
venir, celle que le filtre sert le plus.

## Décision

- **Une source par plateforme** — Crunchyroll et ADN d'abord —, chacune derrière un port
  `IDubSource` et un projet d'adapter. Elles mesurent la **présence** de la VF par épisode. Netflix,
  Prime Video et Disney+ restent hors périmètre : aucune API publique connue d'audio par épisode.
- **Le solveur est admis pour ces sources, et pour elles seules — mais la mesure ne le justifie pas.**
  Le propriétaire l'a accepté par précaution. Le spike montre qu'il n'apporte rien à Crunchyroll : le
  cookie qu'il obtient est inutilisable par un client HTTP et aggrave l'échec, et il ne peut pas poser
  d'en-tête d'autorisation. Le retirer allègerait le pod ; c'est une décision à reprendre, pas un acquis.
  Le refresh AniList reste sans identifiant, sans solveur, sans navigateur. Une source VF qui échoue ne
  fait échouer ni le refresh AniList ni l'affichage : la dernière mesure est conservée.
- **Tout le trafic VF passe par le pod qBittorrent existant, dont la sortie NordVPN est en Suisse**
  (choix du propriétaire, en connaissance de cause : un pod n'a qu'une sortie, donc le solveur et le
  proxy héritent de la Suisse). Ils sont deux conteneurs de plus dans ce pod, sous la liste blanche de
  domaines d'un proxy. Le pod de l'application reste direct : un client VPN mettrait AniList, Keycloak
  et le trafic entrant derrière le tunnel, et la limite d'AniList (30 requêtes par minute et par IP)
  serait partagée avec d'autres clients. **La France reste à comparer** : rien n'a été mesuré depuis une
  IP française, donc on ignore si le catalogue de VF y diffère.
- **La VF est mesurée depuis le serveur, pas depuis le lecteur.** La région et la locale de la
  requête sont enregistrées avec chaque mesure et le badge dit ce qui a été vérifié. Une réponse
  anti-bot, une restriction ou une erreur donne **inconnu**, jamais « pas de VF ». Ce garde-fou est
  nécessaire : Cloudflare défie Crunchyroll de façon instable, selon des détails du client HTTP (le
  rapport du spike en donne la table).
- **Un VPN ne vérifie rien.** Il change la région vue, pas la véracité de ce qui revient : c'est la
  leçon de l'ADR 0002, et le recoupement à la main reste le critère de sortie du spike.
- **Un appariement automatique se corrige.** Un override admin à trois états (`Auto`, `Pinned`,
  `Blocked`) survit aux synchros ; un faux appariement est pire qu'une absence.

## Risque assumé

Les conditions d'utilisation de Crunchyroll interdisent l'extraction automatisée et le contournement
des protections d'accès. **Le propriétaire assume ce risque ; ce document ne l'autorise pas.** Il peut
faire bloquer l'adresse de sortie, casser sans préavis ou renvoyer des données trompeuses. L'isolement
derrière un port limite les dégâts, il ne change rien à la nature du risque.

## Critères de sortie du spike

Bloquants, **pour la plateforme concernée** — sans voie acceptable, elle est écartée :

- la réponse est **vérifiable** : recoupement à la main d'une dizaine de shows contre le site public,
  sans écart ;
- la réponse **distingue** un catalogue réellement vide d'un blocage anti-bot ou d'une restriction ;
- le **besoin d'un solveur ou d'un navigateur** est documenté ;
- la **couverture** est mesurée sur une vraie saison (part des 50 premiers que le mapping résout) ;
- la **région vue** depuis la sortie retenue est confirmée, et comparée à ce que voit un spectateur
  français — le spike ne part **pas** de l'IP résidentielle, dont le résultat serait trop optimiste
  face à l'anti-bot, mais une passe courte de comparaison de catalogue reste nécessaire ;
- la **stabilité** : la sonde est rejouée plusieurs jours, sur plusieurs IP de sortie ;
- une VF **annoncée mais non lisible** est identifiable.

## Conséquences

- **Deux conteneurs de plus dans le pod qBittorrent**, dans le dépôt d'infrastructure : solveur et
  proxy, plus un Service `dub-solver` et, pour les tests depuis un poste, une route Traefik interne
  `solver.apps.elylan` sans authentification (comme le collecteur de télémétrie). Ce dépôt-ci ne reçoit
  jamais d'identifiant NordVPN. Le pod de l'application reste en réplique unique, `Recreate`. Une
  panne du solveur ou du VPN rend le pod qBittorrent non prêt : le blast radius touche le torrent.
- **`RefreshRun` gagne un type** (`Season`, `Dub`). L'exclusion concurrente et le 409 se font par
  (saison, type). Un refresh AniList réussi met en file une synchro VF qui a son propre run.
- **La VF est stockée à part**, clé `(id AniList, plateforme)`, parce que `AnimeRepository.Refresh`
  remplace la saison en bloc. On stocke la mesure, jamais le verdict `Up to date` / `Complete`, qui
  se calcule à la lecture.
- **L'unique action privilégiée n'est plus le refresh** : l'override du mapping est une seconde
  route admin.
- **Un refresh n'est plus « une requête d'une seconde »** pour la VF : la synchro prend des minutes.
  Le refresh AniList reste inchangé.
