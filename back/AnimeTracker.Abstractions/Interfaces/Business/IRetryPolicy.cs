namespace AnimeTracker.Api.Abstractions.Interfaces.Business;

public interface IRetryPolicy
{
	public int Count { get; set; }
	public double[] Delays { get; }
}