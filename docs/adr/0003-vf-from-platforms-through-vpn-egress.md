# La VF vient des plateformes, par une sortie VPN

Statut : **accepté**, le 2026-09-30. Précise [0002](0002-anilist-replaces-nautiljon.md) : AniList reste la
seule source du calendrier, et reste sans identifiant, sans solveur, sans navigateur. Le
[rapport du spike](../research/french-dub-spike.md) donne les mesures ; ce document dit ce qui en a été
retenu.

## Contexte

Le produit répond à « quand tous les épisodes seront-ils sortis ? ». Le propriétaire veut aussi savoir
s'ils sont sortis **en version française** : un filtre `French dub`, et un moyen de filer vers le site
de streaming.

AniList ne dit **rien** du doublage. `externalLinks.language` est vide sur tous les liens de streaming,
et `voiceActors(language: FRENCH)` dit qu'un doubleur est crédité, pas qu'un épisode est sorti. Seules
les plateformes publient l'audio disponible épisode par épisode : Crunchyroll dans les versions de
chaque épisode, ADN dans ses langues (`vf`).

## Décision

- **Une source par plateforme**, Crunchyroll et ADN, chacune derrière le port `IDubPlatformAdapter` et
  dans son propre projet. Elles mesurent la **présence** de la VF par épisode. Netflix, Prime Video et
  Disney+ restent hors périmètre : aucune API publique connue d'audio par épisode.
- **Tout le trafic des plateformes passe par un proxy sortant**, un conteneur du pod qBittorrent dont le
  client NordVPN sort en Suisse. Le but est un seul : que les plateformes ne voient jamais l'adresse du
  réseau local, pour qu'aucun bannissement ne la touche. **Le pays ne compte pas** — mesuré : les VF de
  36 shows sont identiques depuis la France et depuis la Suisse, le catalogue ADN aussi. Rien de la
  région n'est donc modélisé ni affiché. Sans `Dub:Proxy` configuré, une plateforme n'est **pas
  interrogée du tout** : il n'y a pas de repli sur l'adresse locale.
- **Pas de solveur.** Un FlareSolverr a été déployé puis retiré : le cookie qu'il obtient est lié à
  l'empreinte de son navigateur et fait échouer un client HTTP qui le présente, et il ne sait pas porter
  un en-tête d'autorisation. Ce qui passe Cloudflare, c'est la forme du client.
- **Cloudflare, mesuré depuis .NET 10 sous Linux** : il défie une session TLS reprise, HTTP/2 et
  l'user agent de .NET. Le client Crunchyroll coupe la reprise TLS, parle HTTP/1.1 et se présente comme
  un navigateur. **Depuis Windows, tout est défié** quels que soient ces réglages : la synchro VF ne
  fonctionne que sur un hôte Linux, ce qu'est le déploiement.
- **Un défi, un refus, une panne ou un proxy injoignable rendent la plateforme indisponible** pour le
  reste du run : les mesures déjà stockées restent, le run finit en échec et le dit. Jamais une réponse
  de ce genre ne vaut « pas de VF ».
- **Un appariement exige le titre et la date.** Le titre est comparé largement (marqueurs de saison,
  sous-titres et numéraux retirés, car les plateformes nomment la franchise là où AniList nomme la
  saison) ; une série n'est retenue que si l'une de ses saisons a un épisode sorti à trois jours près du
  premier épisode AniList. Ce même épisode fixe la numérotation, ce qui règle les saisons numérotées à
  la suite (13, 14…) et les saisons regroupées.
- **Un appariement se corrige** : un override admin (`Pinned`, `Blocked`) survit aux synchros, et les
  appariements faits sur le seul titre sont listés pour relecture.

## Risque assumé

Les conditions d'utilisation de Crunchyroll interdisent l'extraction automatisée. **Le propriétaire
assume ce risque ; ce document ne l'autorise pas.** La plateforme peut bloquer l'adresse de sortie,
changer son API ou ses règles anti-bot sans préavis. Le port isole la panne ; il ne change rien à la
nature du risque.

## Conséquences

- **Un conteneur de plus dans le pod qBittorrent** (dépôt d'infrastructure, `kubernetes/apps/torrent`) :
  un tinyproxy qui n'accepte que les domaines des plateformes et `CONNECT` vers 443, joignable dans le
  cluster en `dub-proxy.apps.svc.cluster.local:8888` et depuis le LAN en `10.0.1.123:8888`. Il attend
  l'interface NordVPN avant de servir. Une panne du VPN rend le pod qBittorrent non prêt.
- **`RefreshRun` gagne un type** (`Season`, `Dub`). Un refresh AniList réussi met en file la synchro VF
  de la saison, qui a son propre run ; l'exclusion et le 409 se font par saison et par type.
- **La VF est stockée à part** (`DubMatch`, `DubOverride`, clé id AniList + plateforme), parce que
  `AnimeRepository.Refresh` remplace la saison en bloc. On stocke la mesure, jamais le verdict
  `Up to date` / `Complete`, calculé à la lecture.
- **Une seconde action privilégiée** : l'override d'un appariement.
- **Une synchro VF dure quelques minutes** (trois pour l'été 2026, 119 animes). Le refresh AniList reste
  d'une seconde.
