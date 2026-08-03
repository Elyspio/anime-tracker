# Anime Tracker

Quand pourrai-je binger cet animé ?

L'application récupère la liste des animés d'une saison depuis
[AniList](https://anilist.co), suit la sortie de leurs épisodes, et affiche pour chacun la date à
laquelle le dernier épisode sera sorti — le moment où la série devient **bingeable**.

La saison est triée par ordre de disponibilité : ce qui est regardable maintenant en premier, puis
« dans 3 semaines », « dans 7 semaines », et enfin les séries dont le nombre d'épisodes n'est pas
annoncé, pour lesquelles aucune date n'est estimée.

## Fonctionnement du calcul

AniList publie le calendrier de diffusion, dates futures comprises. Quand il couvre les N épisodes
annoncés, la date bingeable est **celle du diffuseur** — une donnée, pas une estimation, et l'écran
le dit. Sinon, la fin manquante est extrapolée à partir de la **cadence réellement observée** :
l'intervalle médian entre les créneaux publiés.

- Médiane et non moyenne : une semaine de pause ne décale pas toute l'estimation.
- Dates distinctes : un double épisode diffusé le même jour ne fait pas croire à une cadence nulle.
- Nombre total d'épisodes non annoncé : aucune date n'est affichée, seulement « fin inconnue ».
- Les épisodes sont datés au jour de diffusion japonais : un créneau de fin de soirée appartient à
  la nuit japonaise, pas au jour UTC.

Le calcul est refait à chaque lecture, jamais stocké.

## Stack

| | |
|---|---|
| Backend | .NET 10, ASP.NET Core, MongoDB, Hangfire, Aspire 13 |
| Frontend | React 19, TypeScript, MUI 9, TanStack Query, Vite+ |
| Auth | Keycloak (OIDC) — lecture anonyme, rafraîchissement réservé au rôle `anime-tracker-admin` |
| Source | API GraphQL publique d'AniList, sans authentification |

## Développement

```bash
aspire run
```

Démarre MongoDB (replica set à un nœud, identifiants `aspire`/`aspire`), Keycloak, l'API et le
serveur Vite. Comptes Keycloak locaux : `admin`/`admin` (peut rafraîchir), `user`/`user` (ne peut
pas).

La base est vide au premier lancement : connectez-vous en `admin` et lancez un rafraîchissement pour
récupérer la saison courante. Il dure environ une seconde.

```bash
dotnet test back/AnimeTracker.slnx
cd front && pnpm test
```

L'API n'est pas rechargée à chaud : après une modification du backend, il faut relancer. Tant
qu'elle tourne, elle garde aussi ses DLL ouvertes et `dotnet build` échoue en copie (`MSB3021`).

```bash
aspire ps      # ce qui tourne
aspire stop    # --all pour tout arrêter
aspire run
```

Les conventions de contribution sont dans [AGENTS.md](AGENTS.md), le vocabulaire du domaine dans
[CONTEXT.md](CONTEXT.md), et les décisions d'architecture dans [docs/adr](docs/adr).
