namespace Example.Api.Abstractions.Common.Assemblers;

/// <summary>
///     Assembling interface
/// </summary>
/// <typeparam name="Ta"></typeparam>
/// <typeparam name="Tb"></typeparam>
public interface IBaseAssembler<Ta, Tb>
{
	/// <summary>
	///     Convert an object of <typeparamref name="Ta" />  to a <typeparamref name="Tb" />
	/// </summary>
	Tb Convert(Ta obj);

	/// <summary>
	///     Convert an object of <typeparamref name="Tb" />  to a <typeparamref name="Ta" />
	/// </summary>
	Ta Convert(Tb obj);

	/// <summary>
	///     Convert a list <typeparamref name="Ta" />  to a list of <typeparamref name="Tb" />
	/// </summary>
	IEnumerable<Tb> Convert(IEnumerable<Ta> objs);

	/// <summary>
	///     Convert a enumerable of <typeparamref name="Tb" />  to a enumerable  of <typeparamref name="Ta" />
	/// </summary>
	IEnumerable<Ta> Convert(IEnumerable<Tb> objs);

	/// <summary>
	///     Convert a list of <typeparamref name="Ta" />  to a list  of <typeparamref name="Tb" />
	/// </summary>
	List<Tb> Convert(List<Ta> objs);

	/// <summary>
	///     Convert a list of <typeparamref name="Tb" />  to a list of <typeparamref name="Ta" />
	/// </summary>
	List<Ta> Convert(List<Tb> objs);
}