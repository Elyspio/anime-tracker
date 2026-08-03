# Le refresh d'une saison est un walk parallèle borné par un plafond unique

> **Remplacé par [0002](0002-anilist-replaces-nautiljon.md).** Nautiljon empoisonnait les données
> qu'il servait ; la source a été abandonnée, et avec elle tout le scraping que cette décision
> organisait. Conservé pour expliquer ce que le code a porté entre-temps.

Un refresh de saison était un parcours strictement séquentiel — un worker Hangfire, une boucle
`foreach`, deux secondes entre chaque anime — soit des dizaines de minutes pendant lesquelles la
saison se remplissait au compte-gouttes. Le raisonnement d'origine était juste sur le fond : le
débit envoyé à Nautiljon ne doit pas dépendre du nombre de choses qui veulent scraper. Il était
faux sur la forme, parce qu'il faisait porter cette garantie par la structure du code appelant.

Le plafond vit désormais dans `FlareSolverrClient`, le seul point de passage de toute requête vers
le site : un sémaphore de trois places, tenu y compris pendant le backoff d'un 429. Au-dessus, tout
peut être parallèle sans négocier avec personne — deux workers Hangfire, quatre marcheurs par
saison. Ces nombres règlent la profondeur de la file devant le portail, jamais le débit qui en
sort. Un refresh complet passe d'environ quarante minutes à un peu plus d'un quart d'heure, et deux
saisons peuvent se rafraîchir en même temps sans que la seconde attende la première.

## Conséquences non évidentes

- **Le nombre de places est limité par le solveur, pas par la politesse.** FlareSolverr pilote un
  vrai navigateur : chaque requête concurrente est une session de plus et une part de sa RAM. Trois
  est un compromis face à une instance unique ; le monter suppose de monter aussi le solveur.
- **Le parallélisme du walk est délibérément supérieur au plafond.** Les tâches en trop attendent
  sur le sémaphore et non sur le site, ce qui évite que le solveur se retrouve inoccupé entre deux
  animes. C'est le contraire d'un bug.
- **Un échec ne se rejoue plus tout seul.** Les dix tentatives par défaut de Hangfire rejouaient un
  walk entier, dix fois, contre un site qui nous tolère. `AutomaticRetry` est à zéro, et un anime
  dont la page se parse mal est compté en échec sans faire tomber le reste de la passe.
- **La règle de skip fait plus pour la durée que le parallélisme.** Un anime clos — total annoncé,
  tous les épisodes stockés et diffusés — ne peut plus changer et n'est jamais re-scrapé. Le reste
  est laissé tranquille s'il a été scrapé il y a moins de douze heures, sauf demande explicite.
