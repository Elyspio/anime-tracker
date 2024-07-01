using System.Reflection;
using System.Text.Json.Serialization;
using AnimeTracker.Api.Web.Technical.Helpers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AnimeTracker.Api.Web.Technical.Filters.Swagger;

/// <summary>
///     Permet que les propriétés des objets de retour soient affichées en tant que required ssi elles ne sont pas nullable
/// </summary>
public sealed class NullableSchemaFilter : ISchemaFilter
{
	private readonly NullabilityInfoContext _nullabilityContext = new();

	/// <inheritdoc />
	public void Apply(OpenApiSchema schema, SchemaFilterContext context)
	{
		var members = RemoveIgnoredProperties(context.Type.GetProperties());

		var duplicatedMembers = members.Where(info => members.Count(i => i.Name == info.Name) > 1);

		var toRemoveMembers = duplicatedMembers.Where(i => i.DeclaringType != context.Type);

		members = members.Except(toRemoveMembers).ToList();

		var typesInfos = members.ToDictionary(prop => (property: prop.Name, runtime: TypeHelper.GetPropertyName(prop)), m => _nullabilityContext.Create(m));


		var notNullableProperties = schema
			.Properties
			.Where(x =>
			{
				var type = typesInfos.FirstOrDefault(pair => pair.Key.property == x.Key || pair.Key.runtime == x.Key);
				return type.Value.WriteState == NullabilityState.NotNull;
			})
			.Select(x => x.Key)
			.ToHashSet();

		schema.Required = notNullableProperties;
	}


	private List<PropertyInfo> RemoveIgnoredProperties(IEnumerable<PropertyInfo> infos)
	{
		return infos.Where(info => info.CustomAttributes.All(attr => attr.AttributeType != typeof(JsonIgnoreAttribute))).ToList();
	}
}