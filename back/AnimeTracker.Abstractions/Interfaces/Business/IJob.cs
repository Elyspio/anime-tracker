using System.Linq.Expressions;

namespace AnimeTracker.Abstractions.Interfaces.Business;

public interface IJob
{
	Task Execute();
}