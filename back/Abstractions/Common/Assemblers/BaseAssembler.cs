namespace AnimeTracker.Api.Abstractions.Common.Assemblers;

/// <summary>
///     Convert an object of <typeparamref name="Ta" /> to a <typeparamref name="Tb" />  and inversely
/// </summary>
/// <typeparam name="Ta"></typeparam>
/// <typeparam name="Tb"></typeparam>
public abstract class BaseAssembler<Ta, Tb> : IBaseAssembler<Ta, Tb>
{
	/// <summary>
	///     Convert an object of <typeparamref name="Ta" />  to a <typeparamref name="Tb" />
	/// </summary>
	public abstract Tb Convert(Ta obj);

	/// <summary>
	///     Convert an object of <typeparamref name="Tb" />  to a <typeparamref name="Ta" />
	/// </summary>
	public abstract Ta Convert(Tb obj);


	/// <summary>
	///     Convert a list <typeparamref name="Ta" />  to a list of <typeparamref name="Tb" />
	/// </summary>
	public IEnumerable<Tb> Convert(IEnumerable<Ta> objs)
	{
		return objs.Select(Convert).ToList();
	}

	/// <summary>
	///     Convert a enumerable of <typeparamref name="Tb" />  to a enumerable  of <typeparamref name="Ta" />
	/// </summary>
	public IEnumerable<Ta> Convert(IEnumerable<Tb> objs)
	{
		return objs.Select(Convert).ToList();
	}

	/// <summary>
	///     Convert a list of <typeparamref name="Ta" />  to a list  of <typeparamref name="Tb" />
	/// </summary>
	public List<Tb> Convert(List<Ta> objs)
	{
		return objs.Select(Convert).ToList();
	}


	/// <summary>
	///     Convert a list of <typeparamref name="Tb" />  to a list of <typeparamref name="Ta" />
	/// </summary>
	public List<Ta> Convert(List<Tb> objs)
	{
		return objs.Select(Convert).ToList();
	}
}