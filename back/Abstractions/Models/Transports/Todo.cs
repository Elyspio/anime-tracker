using System.ComponentModel.DataAnnotations;
using AnimeTracker.Api.Abstractions.Interfaces.Business;
using AnimeTracker.Api.Abstractions.Models.Base;

namespace AnimeTracker.Api.Abstractions.Models.Transports;

public class Todo : TodoBase, ITransport
{
	[Required] public required Guid Id { get; init; }
}