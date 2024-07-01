using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace AnimeTracker.Api.Abstractions.Common.Helpers.Json;

/// <summary>
///     Custom JSON contract resolver for the application.
/// </summary>
public sealed class JsonContractResolver : CamelCasePropertyNamesContractResolver
{
	/// <inheritdoc />
	protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
	{
		var property = base.CreateProperty(member, memberSerialization);

		// If the property has the JsonConverters.Password attribute, apply the password masking
		if (property.AttributeProvider!.GetAttributes(typeof(JsonConverters.Password), true).FirstOrDefault() is JsonConverters.Password)
			property.ValueProvider = new JsonConverters.JsonReplacePassword(property.ValueProvider!);

		return property;
	}
}