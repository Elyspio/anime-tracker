using System.ComponentModel.DataAnnotations;
using Example.Api.Abstractions.Interfaces.Business;
using Example.Api.Abstractions.Models.Base;

namespace Example.Api.Abstractions.Models.Transports;

public class Todo : TodoBase, ITransport
{
	[Required] public required Guid Id { get; init; }
}