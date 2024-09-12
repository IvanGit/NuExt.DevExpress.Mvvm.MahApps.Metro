using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MetroWpfApp.Models;

namespace MetroWpfApp.Converters
{
    internal sealed class PersonModelConverter : JsonConverter<PersonModel>
    {
        public override PersonModel? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException();
            }
            return new PersonModel() { Name = reader.GetString()! };
        }

        public override void Write(Utf8JsonWriter writer, PersonModel value, JsonSerializerOptions options)
        {
            if (string.IsNullOrWhiteSpace(value.Name))
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStringValue(value.Name);
            }
        }
    }
}
