using System.ComponentModel.DataAnnotations;
using AnimeTracker.Api.Abstractions.Interfaces.Business;
using AnimeTracker.Api.Abstractions.Models.Base.Anime;

namespace AnimeTracker.Api.Abstractions.Models.Transports;

public class Anime : AnimeBase, ITransport
{
	[Required] public required Guid Id { get; init; }
}