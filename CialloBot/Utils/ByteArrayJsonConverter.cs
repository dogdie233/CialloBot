﻿using System.Text.Json;
using System.Text.Json.Serialization;

namespace CialloBot.Utils
{
    public class ByteArrayJsonConverter : JsonConverter<byte[]>
    {
        public override byte[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetBytesFromBase64();
        }

        public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
        {
            writer.WriteBase64StringValue(value.AsSpan());
        }
    }
}
