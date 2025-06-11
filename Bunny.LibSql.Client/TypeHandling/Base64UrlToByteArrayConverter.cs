using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bunny.LibSql.Client.TypeHandling;

public class Base64UrlToByteArrayConverter : JsonConverter<byte[]>
{
    public override byte[] Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var s = reader.GetString() ?? "";
        // URL-safe ⇒ standard:
        s = s.Replace('-', '+').Replace('_', '/');
        // pad
        int pad = 4 - (s.Length % 4);
        if (pad < 4) s += new string('=', pad);

        return Convert.FromBase64String(s);
    }

    public override void Write(
        Utf8JsonWriter writer,
        byte[] value,
        JsonSerializerOptions options)
    {
        writer.WriteBase64StringValue(value);
        /*// back to URL-safe if you like:
        var s = Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+','-')
            .Replace('/','_');
        writer.WriteStringValue(s);*/
    }
}