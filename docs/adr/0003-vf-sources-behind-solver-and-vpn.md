# La VF vient des plateformes, derrière un solveur et un VPN

Statut : **proposé.** Passe à *accepté* quand le rapport du spike (`docs/research/french-dub-spike.md`)
aura validé les critères de sortie ci-dessous, plateforme par plateforme. Amende
[0002](0002-anilist-replaces-nautiljon.md) sur un point : « aucun solver, aucun navigateur ».

## Contexte

Le produit répond à « quand tous les épisodes seront-ils sortis ? ». Le propriétaire veut aussi savoir
s'ils sont sortis **en version française** : un filtre `French dub` et la navigation vers le site de
streaming.

AniList ne dit **rien** du doublage. La requête n'en demande pas, `externalLinks.language` est vide sur
tous les liens de streaming d'une saison, et `voiceActors(language: FRENCH)` dit seulement qu'un
doubleur français est crédité, pas qu'un épisode est sorti. Il faut une seconde source, et seules les
plateformes de streaming publient l'audio disponible épisode par épisode.

Mesuré sur les 50 animes les plus populaires de l'automne 2026, une saison en cours : AniList a
désactivé (`isDisabled`) 25 des 37 liens de streaming, et il ne reste que 5 shows avec un lien
Crunchyroll actif, tous avec un `/series/<id>`. ADN n'apparaît dans aucun lien AniList. Sur une saison
terminée les liens sont bien fournis, mais c'est la saison en cours que le filtre sert : le mapping ne
peut pas s'appuyer sur AniList seul.

## Décision

- **Une source par plateforme** — Crunchyroll et ADN d'abord —, chacune derrière un port
  `IDubSource` et un projet d'adapter. Elles mesurent la **présence** de la VF par épisode. Netflix,
  Prime Video et Disney+ restent hors périmètre : aucune API publique connue d'audio par épisode.
- **Le solveur est admis pour ces sources, et pour elles seules.** Le refresh AniList reste sans
  identifiant, sans solveur, sans navigateur. Une source VF qui échoue ne fait échouer ni le refresh
  AniList ni l'affichage : la dernière mesure est conservée.
- **Tout le trafic VF passe par un pod dédié « solveur + VPN » à sortie France**, un Deployment séparé.
  Le pod de l'application reste direct : tous les conteneurs d'un pod partagent le réseau, donc un
  client VPN en sidecar mettrait AniList, Keycloak et le trafic entrant derrière le tunnel, et la
  limite d'AniList (30 requêtes par minute et par IP) serait partagée avec d'autres clients.
- **La VF est mesurée depuis le serveur, pas depuis le lecteur.** La région et la locale de la
  requête sont enregistrées avec chaque mesure et le badge dit ce qui a été vérifié. Une réponse
  anti-bot, une restriction ou une erreur donne **inconnu**, jamais « pas de VF ».
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
- la **région vue** depuis un serveur NordVPN France est confirmée — le spike ne part **pas** de
  l'IP résidentielle, dont le résultat serait trop optimiste ;
- la **stabilité** : la sonde est rejouée plusieurs jours, sur plusieurs IP de sortie ;
- une VF **annoncée mais non lisible** est identifiable.

## Conséquences

- **Un pod de plus**, dans le dépôt d'infrastructure : solveur, client NordVPN, Service, secret. Ce
  dépôt-ci ne reçoit jamais d'identifiant NordVPN. Le pod de l'application reste en réplique unique,
  `Recreate`.
- **`RefreshRun` gagne un type** (`Season`, `Dub`). L'exclusion concurrente et le 409 se font par
  (saison, type). Un refresh AniList réussi met en file une synchro VF qui a son propre run.
- **La VF est stockée à part**, clé `(id AniList, plateforme)`, parce que `AnimeRepository.Refresh`
  remplace la saison en bloc. On stocke la mesure, jamais le verdict `Up to date` / `Complete`, qui
  se calcule à la lecture.
- **L'unique action privilégiée n'est plus le refresh** : l'override du mapping est une seconde
  route admin.
- **Un refresh n'est plus « une requête d'une seconde »** pour la VF : la synchro prend des minutes.
  Le refresh AniList reste inchangé.
