using System.Linq.Expressions;

namespace AnimeTracker.Api.Abstractions.Interfaces.Business;

public interface IJob
{
	Task Execute();
}