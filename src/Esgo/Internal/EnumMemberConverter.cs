using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Esgo.Internal
{
    /// <summary>
    /// Enum as string by <c>[EnumMember(Value = "...")]</c>, in both directions.
    /// System.Text.Json ignores <c>EnumMember</c>; without this converter "Seite" would be sent
    /// instead of "seite". tools/Generate wires it into the generated models.
    /// </summary>
    /// <typeparam name="T">Generated enum type.</typeparam>
    public sealed class EnumMemberConverter<T> : JsonConverter<T> where T : struct, Enum
    {
        private static readonly Dictionary<T, string> ToText = typeof(T)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .ToDictionary(f => (T)f.GetValue(null)!, f => f.GetCustomAttribute<EnumMemberAttribute>()?.Value ?? f.Name);

        private static readonly Dictionary<string, T> FromText = ToText.ToDictionary(p => p.Value, p => p.Key);

        /// <summary>The wire value of an enum member, e.g. "seite".</summary>
        public static string TextOf(T value) => ToText[value];

        /// <inheritdoc />
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var text = reader.GetString();
            if (text != null && FromText.TryGetValue(text, out var value))
                return value;
            throw new JsonException("Unknown value '" + text + "' for " + typeof(T).Name + ".");
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            => writer.WriteStringValue(ToText[value]);
    }
}
