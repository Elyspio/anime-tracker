using System.Reflection;
using Example.Api.Web.Technical.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Example.Api.Web.Technical.Filters.Swagger;

/// <summary>
///     Permet que les paramètres des endpoints dans le swagger soient affichés en tant que required ssi ils ne sont pas
///     optionnels (int?)
/// </summary>
public sealed class SwaggerSetNullableOperationFilter : IOperationFilter
{
	private readonly NullabilityInfoContext _nullabilityContext = new();

	/// <inheritdoc />
	public void Apply(OpenApiOperation operation, OperationFilterContext context)
	{
		var contextParams = GetNullableInfos(context.MethodInfo);


		foreach (var param in context.ApiDescription.ParameterDescriptions.Where(p => p.Source == BindingSource.Query))
		{
			var operationParam = operation.Parameters.First(p => p.Name == param.Name);
			var parameterInfo = param.ParameterInfo();
			if (parameterInfo == default) continue;

			var nullabilityInfo = contextParams[param.Name];
			operationParam.Schema.Nullable = nullabilityInfo.WriteState == NullabilityState.Nullable || parameterInfo.HasDefaultValue;
			param.IsRequired = nullabilityInfo.WriteState == NullabilityState.NotNull && !parameterInfo.HasDefaultValue;
			operationParam.Required = param.IsRequired;
		}


		var bodyParam = context.ApiDescription.ParameterDescriptions.FirstOrDefault(p => p.Source == BindingSource.Body);
		if (bodyParam != default) operation.RequestBody.Required = bodyParam.IsRequired && !bodyParam.ParameterInfo().HasDefaultValue;
	}

	/// <summary>
	///     Renvoie tous les paramètres de la méthode en gérant l'attribut <see cref="FromQueryAttribute" />
	/// </summary>
	/// <returns>
	///     un dictionnaire avec comme clé le nom du paramètre et comme valeur les informations avec la notion de
	///     nullabilité
	/// </returns>
	private Dictionary<string, NullabilityInfo> GetNullableInfos(MethodBase method)
	{
		var allParameters = new Dictionary<string, NullabilityInfo>();
		var parameters = method.GetParameters();
		foreach (var parameter in parameters)
		{
			var hasFromQueryAttr = parameter.CustomAttributes.All(attr => attr.AttributeType == typeof(FromQueryAttribute));
			if (ParameterTypeIsClass(hasFromQueryAttr, parameter.ParameterType))
			{
				var innerProperties = GetInnerProperties(parameter);
				foreach (var innerParam in innerProperties) allParameters[TypeHelper.GetPropertyName(innerParam)] = _nullabilityContext.Create(innerParam);
			}
			else
			{
				allParameters[TypeHelper.GetPropertyName(parameter)] = _nullabilityContext.Create(parameter);
			}
		}

		return allParameters;
	}

	private static bool ParameterTypeIsClass(bool hasFromQueryAttr, Type type)
	{
		return hasFromQueryAttr && !type.IsPrimitive && type != typeof(string) && type.IsClass && !type.IsArray;
	}

	private IEnumerable<PropertyInfo> GetInnerProperties(ParameterInfo parameter)
	{
		return GetInnerProperties(parameter.ParameterType);
	}

	/// <summary>
	///     Récupère les sous propriété d'un type
	///     Récursif sur les propriété ayant l'attribut <see cref="FromQueryAttribute" />
	/// </summary>
	/// <returns></returns>
	private IEnumerable<PropertyInfo> GetInnerProperties(PropertyInfo parameter)
	{
		return GetInnerProperties(parameter.PropertyType);
	}


	/// <summary>
	///     Récupère les sous propriété d'un type
	///     Récursif sur les propriété ayant l'attribut <see cref="FromQueryAttribute" />
	/// </summary>
	/// <returns></returns>
	private IEnumerable<PropertyInfo> GetInnerProperties(Type type)
	{
		var ret = new List<PropertyInfo>();
		var properties = type.GetProperties();
		foreach (var property in properties)
		{
			var hasFromQueryAttr = property.CustomAttributes.Any(attr => attr.AttributeType == typeof(FromQueryAttribute));
			if (ParameterTypeIsClass(hasFromQueryAttr, property.PropertyType)) ret.AddRange(GetInnerProperties(property));
			else ret.Add(property);
		}

		return ret;
	}
}