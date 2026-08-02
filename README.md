# Anime Tracker

Quand pourrai-je binger cet animé ?

L'application récupère la liste des animés d'une saison depuis
[Nautiljon](https://www.nautiljon.com), suit la sortie de leurs épisodes, et affiche pour chacun la
date à laquelle le dernier épisode sera sorti — le moment où la série devient **bingeable**.

La saison est triée par ordre de disponibilité : ce qui est regardable maintenant en premier, puis
« dans 3 semaines », « dans 7 semaines », et enfin les séries dont le nombre d'épisodes n'est pas
annoncé, pour lesquelles aucune date n'est estimée.

## Fonctionnement du calcul

La date bingeable est extrapolée à partir de la **cadence réellement observée** : l'intervalle
médian entre les sorties déjà passées, appliqué aux épisodes restants.

- Médiane et non moyenne : une semaine de pause ne décale pas toute l'estimation.
- Dates distinctes : un double épisode diffusé le même jour ne fait pas croire à une cadence nulle.
- Nombre total d'épisodes non annoncé : aucune date n'est affichée, seulement « Fin inconnue ».

Le calcul est refait à chaque lecture, jamais stocké.

## Stack

| | |
|---|---|
| Backend | .NET 10, ASP.NET Core, MongoDB, Hangfire, Aspire 13 |
| Frontend | React 19, TypeScript, MUI 9, TanStack Query, Vite+ |
| Auth | Keycloak (OIDC) — lecture anonyme, rafraîchissement réservé au rôle `anime-tracker-admin` |
| Scraping | Nautiljon via FlareSolverr (contournement Cloudflare) |

## Développement

```bash
aspire run
```

Démarre MongoDB (replica set à un nœud, identifiants `aspire`/`aspire`), Keycloak, FlareSolverr,
l'API et le serveur Vite. Comptes Keycloak locaux : `admin`/`admin` (peut rafraîchir),
`user`/`user` (ne peut pas).

La base est vide au premier lancement : connectez-vous en `admin` et lancez un rafraîchissement pour
récupérer la saison courante.

```bash
dotnet test back/AnimeTracker.slnx
cd front && pnpm test
```

Les conventions de contribution sont dans [AGENTS.md](AGENTS.md), le vocabulaire du domaine dans
[CONTEXT.md](CONTEXT.md).
