using System.Text;
using System.Text.RegularExpressions;

namespace AnimeTracker.Core.Services.Dub;

/// <summary>
///     Which platform series may be an anime, by title. Deliberately lenient: a platform names a
///     franchise once ("Black Clover", "Mushoku Tensei: Jobless Reincarnation") where AniList names
///     every season ("Black Clover 2nd Season", "Mushoku Tensei III: Isekai Ittara Honki Dasu"), so
///     season markers, subtitles and bracketed notes are dropped. A title never makes a match on its
///     own: the season still has to line up with the anime's first episode (<see cref="DubAligner" />),
///     and that date is what tells two shows with the same name apart.
/// </summary>
public static partial class DubTitles
{
	/// <summary>A search costs a request; three phrasings find what a fourth would.</summary>
	private const int MaxQueries = 3;

	private static readonly HashSet<string> RomanNumerals = ["ii", "iii", "iv", "v", "vi", "vii", "viii", "ix"];

	/// <summary>Lower case, accents off, season markers, brackets and punctuation gone, trailing season number dropped.</summary>
	public static string Normalize(string title)
	{
		var text = title.Normalize(NormalizationForm.FormKD);
		// Latin combining accents only, then recompose: left decomposed, が would lose its dakuten
		// and match か. The frontend's title search draws the same line.
		text = LatinAccents().Replace(text, "").Normalize(NormalizationForm.FormC).ToLowerInvariant();
		text = Brackets().Replace(text, " ");
		text = SeasonMarkers().Replace(text, " ");
		text = NonWord().Replace(text, " ");

		var tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();

		// "Clevatess II", "Mob ni Kibishii Sekai desu 2": a trailing numeral numbers the season, not
		// the show. Both sides lose it, so "Mob Psycho 100" still equals itself.
		while (tokens.Count > 1 && (RomanNumerals.Contains(tokens[^1]) || tokens[^1].All(char.IsAsciiDigit)))
			tokens.RemoveAt(tokens.Count - 1);

		return string.Join(' ', tokens);
	}

	/// <summary>
	///     Every form a title can be matched under: the whole title, and the part before its subtitle
	///     when that part says enough on its own — "Re" from "Re:Zero" would match half a catalogue.
	/// </summary>
	public static IReadOnlySet<string> Keys(IEnumerable<string> titles)
	{
		var keys = new HashSet<string>();

		foreach (var title in titles)
		{
			Add(keys, Normalize(title));

			var head = SubtitleSeparator().Split(title, 2)[0];
			if (head.Length < title.Length) Add(keys, Normalize(head));
		}

		return keys;

		static void Add(HashSet<string> keys, string key)
		{
			if (key.Length >= 4 && key.Contains(' ')) keys.Add(key);
			else if (key.Length >= 6) keys.Add(key);
		}
	}

	public static bool Matches(IEnumerable<string> animeTitles, IEnumerable<string> seriesTitles)
	{
		return Keys(animeTitles).Overlaps(Keys(seriesTitles));
	}

	/// <summary>
	///     What to type in a platform's search box, most telling first. English before romaji: the
	///     French catalogues list most simulcasts under their English title. Normalised, because search
	///     engines stumble on "Season 2" and "III" as much as the matching does.
	/// </summary>
	public static IReadOnlyList<string> Queries(string title, IEnumerable<string> alternativeTitles)
	{
		var english = alternativeTitles.FirstOrDefault(IsLatin);
		IEnumerable<string?> phrasings =
		[
			english is null ? null : Normalize(english),
			Normalize(title),
			english is null ? null : Normalize(SubtitleSeparator().Split(english, 2)[0]),
			Normalize(SubtitleSeparator().Split(title, 2)[0])
		];

		return phrasings
			.OfType<string>()
			.Where(query => query.Length >= 3)
			.Distinct()
			.Take(MaxQueries)
			.ToArray();
	}

	/// <summary>The English title is the first Latin-script alternative; the native one never is.</summary>
	private static bool IsLatin(string title)
	{
		var letters = title.Where(char.IsLetter).ToArray();

		return letters.Length > 0 && letters.Count(letter => letter <= 'ɏ') >= letters.Length * 0.8;
	}

	[GeneratedRegex(@"[̀-ͯ]")]
	private static partial Regex LatinAccents();

	[GeneratedRegex(@"\([^)]*\)|\[[^\]]*\]")]
	private static partial Regex Brackets();

	[GeneratedRegex(@"\b\d+(st|nd|rd|th)\s+(season|part|cour)\b|\b(season|saison|part|partie|cour)\s*\d+\b|\b(the\s+)?(first|second|third|fourth|fifth|final)\s+(season|part|cour)\b")]
	private static partial Regex SeasonMarkers();

	[GeneratedRegex(@"[^\p{L}\p{N}]+")]
	private static partial Regex NonWord();

	[GeneratedRegex(@"\s*[:：]\s*|\s+[-–—]\s+")]
	private static partial Regex SubtitleSeparator();
}
