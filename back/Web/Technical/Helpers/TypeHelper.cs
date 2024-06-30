using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Serialization;

namespace Example.Api.Web.Technical.Helpers;

/// <summary>
/// </summary>
public static class TypeHelper
{
	private static readonly JsonSerializerOptions Options = new()
	{
		Converters =
		{
			new JsonStringEnumConverter()
		}
	};


	/// <summary>
	///     Renvoie le nom de la propriété en accord avec l'attribut <see cref="JsonIgnoreAttribute" />
	/// </summary>
	/// <param name="info"></param>
	/// <returns></returns>
	public static string GetPropertyName(MemberInfo info)
	{
		var nameFromAttributes = GetPropertyNameFromAttribut(info.CustomAttributes.ToList());
		return nameFromAttributes ?? new CamelCaseNamingStrategy().GetPropertyName(info.Name!, false);
	}

	/// <summary>
	///     Renvoie le nom de la propriété en accord avec l'attribut <see cref="JsonIgnoreAttribute" />
	/// </summary>
	/// <param name="info"></param>
	/// <returns></returns>
	public static string GetPropertyName(ParameterInfo info)
	{
		var nameFromAttributes = GetPropertyNameFromAttribut(info.CustomAttributes.ToList());
		return nameFromAttributes ?? new CamelCaseNamingStrategy().GetPropertyName(info.Name!, false);
	}


	private static string? GetPropertyNameFromAttribut(List<CustomAttributeData> attributes)
	{
		var jsonPropertyNameAttribute = attributes.FirstOrDefault(attr => attr.AttributeType == typeof(JsonPropertyNameAttribute));
		if (jsonPropertyNameAttribute != default) return jsonPropertyNameAttribute.ConstructorArguments[0].Value!.ToString()!;

		var fromQueryAttribute = attributes.FirstOrDefault(attr => attr.AttributeType == typeof(FromQueryAttribute));
		if (fromQueryAttribute == default) return null;
		var arg = fromQueryAttribute.NamedArguments.FirstOrDefault(arg => arg.MemberName == "Name");
		return arg != default ? arg.TypedValue.Value!.ToString()! : null;
	}
}