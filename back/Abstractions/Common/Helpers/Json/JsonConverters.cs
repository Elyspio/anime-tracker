using Newtonsoft.Json.Serialization;

namespace AnimeTracker.Api.Abstractions.Common.Helpers.Json;

/// <summary>
///     Contains custom JSON converters for specific serialization and deserialization behaviours.
/// </summary>
public static class JsonConverters
{
	/// <summary>
	///     Mark the property as password and replace content with stars on serialisation
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public sealed class Password : Attribute;

	/// <inheritdoc />
	internal sealed class JsonReplacePassword(IValueProvider baseValueProvider) : IValueProvider
	{
		/// <inheritdoc />
		public object GetValue(object target)
		{
			// var value = baseValueProvider.GetValue(target);

			// Return the replacement value instead of the original value
			return new string('*', Random.Shared.Next(16, 32));
		}

		/// <inheritdoc />
		public void SetValue(object target, object? value)
		{
			baseValueProvider.SetValue(target, value);
		}
	}
}