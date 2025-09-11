using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace XYVR.Core;

public class SpecificsConverter : JsonConverter
{
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var token = JToken.ReadFrom(reader);
        
        if (token.Type == JTokenType.Null)
        {
            return null;
        }
    
        return token.ToObject<ImmutableVRChatSpecifics>(serializer);
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value != null)
        {
            serializer.Serialize(writer, value);
        }
        else
        {
            writer.WriteNull();
        }
    }

    public override bool CanConvert(Type objectType)
    {
        return false;
    }
}