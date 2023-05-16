using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Microsoft.Build.BuildLink.SourceCodes;

internal class BuildDescriptorSerializer : IBuildDescriptorSerializer
{
    private readonly JsonSerializerOptions _serializerOptions = CreateSerializerOptions(true);

    public string WriteToString(WorkingCopyBuildDescriptor buildDescriptor)
    {
        return JsonSerializer.Serialize(buildDescriptor, _serializerOptions);
    }

    public string PrependProperty(string jsonString, string propertyName, string propertyValue)
    {
        // Workarounding System.Text.Json lack of ability to insert at index
        string newstr = $"{{{Environment.NewLine}\"{propertyName}\": {JsonValue.Create(propertyValue).ToJsonString()},"
                        + jsonString.Substring(1);

        var node = JsonNode.Parse(newstr);
        return node.ToJsonString(_serializerOptions);
    }

    public WorkingCopyBuildDescriptor? ReadFromString(string value)
    {
        return JsonSerializer.Deserialize<WorkingCopyBuildDescriptor>(value, _serializerOptions);
    }

    public async Task<WorkingCopyBuildDescriptor?> ReadFromFileAsync(string filePath, CancellationToken token)
    {
        await using var stream = File.OpenRead(filePath);
        return await JsonSerializer.DeserializeAsync<WorkingCopyBuildDescriptor>(stream, _serializerOptions, token).ConfigureAwait(false);
    }

    public async Task WriteToFile(string filePath, WorkingCopyBuildDescriptor buildDescriptor, CancellationToken token)
    {
        await using var stream = File.OpenWrite(filePath);
        stream.SetLength(0);
        await JsonSerializer.SerializeAsync(stream, buildDescriptor, _serializerOptions, token)
            .ConfigureAwait(false);
    }

    private static JsonSerializerOptions CreateSerializerOptions(bool includeScriptConverter)
    {
        var opt = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters =
            {
                new ScriptGroupJsonConverter(),
                new OSPlatformJsonConverter(),
                new JsonStringEnumConverter()
            },
            TypeInfoResolver = new PrivateConstructorContractResolver()
        };

        if (includeScriptConverter)
        {
            opt.Converters.Add(new ScriptJsonConverter());
        }

        return opt;
    }

    // Needed to avoid need for public parameterless ctors
    private class PrivateConstructorContractResolver : DefaultJsonTypeInfoResolver
    {
        public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            JsonTypeInfo jsonTypeInfo = base.GetTypeInfo(type, options);

            if (jsonTypeInfo.Kind == JsonTypeInfoKind.Object && jsonTypeInfo.CreateObject is null)
            {
                if (jsonTypeInfo.Type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Length != 1)
                {
                    // The type doesn't have public constructors
                    jsonTypeInfo.CreateObject = () =>
                        Activator.CreateInstance(jsonTypeInfo.Type, true);
                }
            }

            return jsonTypeInfo;
        }
    }

    private class OSPlatformJsonConverter : JsonConverter<OSPlatform?>
    {
        public override OSPlatform? Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
            =>
                OSPlatformUtils.Parse(reader.GetString());

        public override void Write(
            Utf8JsonWriter writer,
            OSPlatform? value,
            JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStringValue(value.ToString());
        }
    }

    private class ScriptGroupJsonConverter : JsonConverter<ScriptGroup>
    {
        private JsonConverter<Script>? _valueConverter;

        private JsonConverter<Script> GetValueConverter(JsonSerializerOptions options)
        {
            if (_valueConverter == null)
            {
                _valueConverter = (JsonConverter<Script>)options
                    .GetConverter(typeof(Script));
            }
            return _valueConverter;
        }

        public override ScriptGroup Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null || reader.TokenType == JsonTokenType.None)
            {
                return ScriptGroup.NullScript;
            }

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }

            var scriptGroup = new ScriptGroup();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return scriptGroup;
                }

                // Get the key.
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException();
                }

                string? propertyName = reader.GetString();

                OSPlatform key = OSPlatformUtils.Parse(propertyName) ?? OSPlatformUtils.AnyPlatform;

                // Get the value.
                reader.Read();
                Script value = GetValueConverter(options).Read(ref reader, typeof(Script), options)!;

                // Add to dictionary.
                scriptGroup.Add(key, value);
            }

            throw new JsonException();
        }

        public override void Write(
            Utf8JsonWriter writer,
            ScriptGroup group,
            JsonSerializerOptions options)
        {
            if (group.IsNull())
            {
                writer.WriteNullValue();
                return;
            }


            // Following needed - as System.Text.Json doesn't support indexing dictionary by composite types

            writer.WriteStartObject();

            foreach ((OSPlatform key, Script value) in group)
            {
                var propertyName = key.ToString();
                writer.WritePropertyName
                    (options.PropertyNamingPolicy?.ConvertName(propertyName) ?? propertyName);

                GetValueConverter(options).Write(writer, value, options);
            }

            writer.WriteEndObject();
        }
    }

    private class ScriptJsonConverter : JsonConverter<Script>
    {
        private JsonSerializerOptions _downstreamOptions = CreateSerializerOptions(false);

        public override Script Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null || reader.TokenType == JsonTokenType.None)
            {
                return Script.NullScript;
            }

            return JsonSerializer.Deserialize<Script>(ref reader, _downstreamOptions)!;
        }

        public override void Write(
            Utf8JsonWriter writer,
            Script value,
            JsonSerializerOptions options)
        {
            if (value.IsNull())
            {
                writer.WriteNullValue();
                return;
            }

            JsonSerializer.Serialize(writer, value, _downstreamOptions);
        }
    }
}
