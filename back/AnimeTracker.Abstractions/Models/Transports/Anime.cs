using System.ComponentModel.DataAnnotations;
using AnimeTracker.Abstractions.Interfaces.Business;
using AnimeTracker.Abstractions.Models.Base.Anime;

namespace AnimeTracker.Abstractions.Models.Transports;

public class Anime : AnimeBase, ITransport
{
	[Required] public required Guid Id { get; init; }
}