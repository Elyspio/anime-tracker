using System.Collections.Concurrent;

namespace AnimeTracker.Api.Abstractions.Common.Extensions;

/// <summary>
///     Provides extension methods for working with list data types.
/// </summary>
public static class ListExtension
{
	/// <summary>
	///     Executes a provided asynchronous method concurrently on the elements of an IEnumerable and returns a ParallelResult
	///     object.
	/// </summary>
	/// <typeparam name="TInput">Input type.</typeparam>
	/// <typeparam name="TRet">Return type.</typeparam>
	/// <param name="elements">The elements to process.</param>
	/// <param name="action">Asynchronous method to apply on every element.</param>
	/// <param name="token"></param>
	/// <returns>A task that represents the operation and returns a <see cref="ParallelResult{TInput, TRet}" /></returns>
	public static async Task<ParallelResult<TInput, TRet>> Parallelize<TInput, TRet>(this IEnumerable<TInput> elements, Func<TInput, CancellationToken, Task<TRet>> action,
		CancellationToken token = default) where TInput : notnull
	{
		var innerExceptions = new ConcurrentDictionary<TInput, Exception>();

		var results = new ConcurrentBag<TRet>();


		await Parallel.ForEachAsync(elements, token, async (input, t) =>
		{
			try
			{
				var result = await action(input, t);
				results.Add(result);
			}
			catch (Exception e)
			{
				innerExceptions[input] = e;
			}
		});

		return new ParallelResult<TInput, TRet>
		{
			Data = results.ToList(),
			Exceptions = innerExceptions.ToDictionary(pair => pair.Key, pair => pair.Value),
			Status = innerExceptions.Any() ? ParallelStatus.Faulted : ParallelStatus.Succeed
		};
	}

	/// <summary>
	///     Executes a provided asynchronous method concurrently on the elements of an IEnumerable and returns a ParallelResult
	///     object (no return data).
	/// </summary>
	/// <typeparam name="TInput">Input type.</typeparam>
	/// <param name="elements">The elements to process.</param>
	/// <param name="action">Asynchronous method to apply on every element.</param>
	/// <param name="token"></param>
	/// <returns>A task that represents the operation and returns a <see cref="ParallelResult{TInput}" /></returns>
	public static async Task<ParallelResult<TInput>> Parallelize<TInput>(this IEnumerable<TInput> elements, Func<TInput, CancellationToken, Task> action,
		CancellationToken token = default) where TInput : notnull
	{
		var innerExceptions = new ConcurrentDictionary<TInput, Exception>();


		await Parallel.ForEachAsync(elements, token, async (input, t) =>
		{
			try
			{
				await action(input, t);
			}
			catch (Exception e)
			{
				innerExceptions[input] = e;
			}
		});

		return new ParallelResult<TInput>
		{
			Exceptions = innerExceptions.ToDictionary(pair => pair.Key, pair => pair.Value),
			Status = innerExceptions.Any() ? ParallelStatus.Faulted : ParallelStatus.Succeed
		};
	}

	/// <summary>
	///     Represents the result of a parallel operation with a collection of return data and exceptions.
	/// </summary>
	/// <typeparam name="TInput">Input type.</typeparam>
	/// <typeparam name="TRet">Return type.</typeparam>
	public sealed class ParallelResult<TInput, TRet> where TInput : notnull
	{
		/// <summary>
		///     Gets or sets the list of return data.
		/// </summary>
		public required List<TRet> Data { get; set; }

		/// <summary>
		///     Gets or sets the list of exceptions that occurred during processing.
		/// </summary>
		public required Dictionary<TInput, Exception> Exceptions { get; set; }

		/// <summary>
		///     Gets or sets the overall status of the parallel operation.
		/// </summary>
		public ParallelStatus Status { get; set; }
	}

	/// <summary>
	///     Represents the result of a parallel operation with a collection of exceptions.
	/// </summary>
	/// <typeparam name="TInput">Input type.</typeparam>
	public sealed class ParallelResult<TInput> where TInput : notnull
	{
		/// <summary>
		///     Gets or sets the list of exceptions that occurred during processing.
		/// </summary>
		public required Dictionary<TInput, Exception> Exceptions { get; set; }

		/// <summary>
		///     Gets or sets the overall status of the parallel operation.
		/// </summary>
		public ParallelStatus Status { get; set; }
	}
}

/// <summary>
///     Enumerates the possible status of a parallel operation.
/// </summary>
public enum ParallelStatus
{
	/// <summary>
	///     Indicates that the parallel operation succeeded without unhandled exceptions.
	/// </summary>
	Succeed,

	/// <summary>
	///     Indicates that the parallel operation encountered unhandled exceptions.
	/// </summary>
	Faulted
}