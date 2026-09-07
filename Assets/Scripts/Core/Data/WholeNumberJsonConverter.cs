using System;
using System.Globalization;
using Newtonsoft.Json;

namespace OrbitalEscape.Data
{
    public sealed class WholeNumberJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(int);

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return 0;
            }

            if (reader.TokenType == JsonToken.Integer)
            {
                return Convert.ToInt32(reader.Value, CultureInfo.InvariantCulture);
            }

            if (reader.TokenType == JsonToken.Float || reader.TokenType == JsonToken.String)
            {
                string serializedValue = Convert.ToString(reader.Value, CultureInfo.InvariantCulture);
                if (decimal.TryParse(
                    serializedValue,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out decimal numericValue) &&
                    numericValue >= int.MinValue &&
                    numericValue <= int.MaxValue)
                {
                    return decimal.ToInt32(decimal.Truncate(numericValue));
                }
            }

            throw new JsonSerializationException(
                $"Value '{reader.Value}' cannot be converted to a whole number.");
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue((int)value);
        }
    }
}
