# AniList remplace le scraping de Nautiljon

Remplace [0001](0001-bounded-parallel-scraping.md), dont l'objet — paralléliser un scrape sans
augmenter le débit — disparaît avec le scrape lui-même.

Nautiljon **empoisonne les données** quand il détecte du scraping. La même fiche, demandée depuis
deux adresses, rend deux séries différentes : d'un côté Mashle saison 3, un manga diffusé à partir
de janvier 2027 chez A-1 Pictures ; de l'autre une série de 1987 chez Tatsunoko, origine « jeu
vidéo », genres science-fiction et militaire. Le HTML est valide, tous les sélecteurs matchent, le
compteur d'échecs reste à zéro. C'est un échec silencieux : rien, côté application, ne permet de
distinguer une ligne saine d'une ligne fabriquée.

Changer d'adresse IP — via un VPN, par exemple — ne répond pas au problème. Ça peut faire retomber
le drapeau, mais ça ne fournit aucun moyen de **vérifier** que ce qui revient est vrai. Le seul
contrôle possible serait de comparer à une autre source ; autant prendre l'autre source.

Nous basculons donc sur l'API GraphQL publique d'AniList. Elle ne demande aucune authentification,
et une requête paginée rend une saison entière — notes, nombre de votes, popularité, studios,
genres, format, contenu adulte — **calendrier de diffusion épisode par épisode compris, dates
futures incluses**. C'est ce dernier point qui a départagé les candidats : l'API officielle de
MyAnimeList n'expose aucune liste d'épisodes, et Jikan, qui en a une, est un proxy scrapeur au-dessus
de MyAnimeList dont il hérite les pannes — il répondait 504 pendant l'évaluation.

## Conséquences

- **Tout l'appareil de scraping disparaît.** FlareSolverr et son conteneur, le plafond de
  concurrence, le walk parallèle, la règle de skip et l'horodatage de dernier scrape n'ont plus
  d'objet : un refresh est une à deux requêtes et dure environ une seconde, contre des dizaines de
  minutes. Le job reste néanmoins passé par la file : c'est ce qui donne au bouton et au job
  nocturne un seul chemin, et ce qui laisse une trace écrite de chaque exécution.
- **La prédiction gagne un statut `Announced`.** Quand le calendrier couvre les N épisodes annoncés,
  la date de fin est celle du diffuseur et non plus une extrapolation. Le produit a toujours prétendu
  distinguer une mesure d'une estimation ; jusqu'ici il ne le pouvait pas, faute de dates futures.
  L'extrapolation par cadence médiane subsiste pour les calendriers incomplets.
- **Les dates sont datées en heure du Japon.** AniList publie chaque créneau en timestamp Unix, et
  les diffusions de fin de soirée — 01:30 un samedi, l'habitude de tout un genre — basculent au jour
  précédent si on les lit en UTC. Un épisode réel de la fixture le montre : `2026-09-20 15:00 UTC`
  est le 21 septembre à Tokyo.
- **L'identité change, la base est repartie de zéro.** Les animes sont désormais clés sur
  l'identifiant AniList et non plus sur une URL Nautiljon, et les données stockées étaient de toute
  façon partiellement fabriquées. Le volume a été supprimé plutôt que migré.
- **Les vignettes ne demandent plus de contournement.** Le CDN d'AniList sert les images quel que
  soit le `Referer` ; l'attribut `referrerPolicy="no-referrer"` posé pour Nautiljon a été retiré.
- **L'interface passe en anglais.** Les genres arrivent en anglais et une table de correspondance
  aurait été un point d'entretien permanent pour un seul écran ; le produit est donc anglophone,
  tandis que ces documents restent en français.
