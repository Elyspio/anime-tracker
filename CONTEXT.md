# CONTEXT

Glossaire du domaine. Uniquement du vocabulaire — aucun détail d'implémentation.

## Termes

- **Anime de saison** : anime diffusé pendant une saison donnée (année + saison hiver/printemps/été/automne), hors séries continues d'une saison précédente.
- **Bingeable** : tous les épisodes de l'anime sont sortis.
- **Date bingeable** : date de sortie du dernier épisode, annoncée quand la source la publie, estimée sinon.
- **Date annoncée** : date bingeable issue du calendrier de diffusion, et non d'un calcul. Distincte à l'écran d'une estimation.
- **Cadence** : intervalle médian observé entre les sorties d'épisodes consécutifs (sur dates distinctes — un double épisode le même jour compte pour un seul point). Ne sert plus qu'à combler la fin d'un calendrier incomplet.
- **Fin inconnue** : le nombre total d'épisodes n'est pas annoncé, ou aucun créneau ne l'est — aucune date bingeable n'est affichée.
- **Épisode sorti** : épisode dont la date de sortie est passée ou égale à aujourd'hui.
- **Jour de diffusion** : le jour où l'épisode passe au Japon. Un créneau de fin de soirée appartient au jour japonais, pas au jour UTC.
- **Format** : mode de sortie d'un anime — série TV, format court, série de plateforme, OVA, film, spécial. Seuls les formats épisodiques sont affichés par défaut : un film est bingeable le jour de sa sortie, le compte à rebours n'a rien à en dire.

## Version française

- **VF** : version doublée en français d'un épisode. Se mesure sur une plateforme, jamais sur AniList,
  qui ne publie rien du doublage. À l'écran : *French dub*.
- **Plateforme** : service de streaming qui publie l'audio disponible par épisode. Crunchyroll et ADN
  d'abord.
- **Série de plateforme** : l'entrée du catalogue d'une plateforme que l'on a reconnue comme un anime
  de saison. Elle regroupe souvent plusieurs saisons ; seule la saison ou la partie qui correspond à
  l'anime est retenue.
- **Appariement** : le lien entre un anime de saison et sa série de plateforme, avec la correspondance
  numéro d'épisode AniList vers épisode de plateforme. Automatique, corrigeable à la main. Un mauvais
  appariement est pire qu'une absence.
- **Mesure VF** : l'ensemble des numéros d'épisodes dont la VF est disponible sur une plateforme, avec la
  région et la date d'observation.
- **VF à jour** (*Up to date*) : au moins un épisode est sorti, et une même plateforme a la VF de tous
  les épisodes sortis.
- **VF complète** (*Complete*) : le total annoncé est connu, et une même plateforme a la VF de tous les
  épisodes, du premier au dernier. Incluse dans la VF à jour.
- **VF inconnue** : aucune série reconnue, alignement non démontré ou mesure impossible (blocage
  anti-bot, restriction géographique, erreur). Jamais présentée comme une VF absente.
- **Override** : correction d'un appariement par l'admin — `Auto` (aucune), `Pinned` (série imposée),
  `Blocked` (aucun appariement). Survit aux synchros.
- **Run VF** : exécution de la synchro VF d'une saison, mise en file par un refresh AniList réussi.
  Distinct du run de refresh, avec son propre statut.

## Appréciation

- **Note** : moyenne des notes attribuées par les membres, sur 10. Absente tant que personne n'a noté l'anime.
- **Nombre de notes** : combien de membres ont noté l'anime. C'est ce qui dit si une note veut dire quelque chose.
- **Popularité** : combien de membres ont inscrit l'anime dans une liste, qu'ils l'aient noté ou non. Toujours plus grand que le nombre de notes, et ne mesure pas la même chose.

## Rafraîchissement

- **Refresh** : récupération d'une saison entière depuis la source, épisodes compris, en une opération.
- **Run de refresh** : une exécution de refresh, de sa mise en file à son arrêt. Porte la saison touchée, le nombre d'animes stockés et la raison de son arrêt.
- **Run interrompu** : run dont le processus s'est arrêté en cours de route. Personne ne le poursuit ; il est clos au démarrage suivant.
