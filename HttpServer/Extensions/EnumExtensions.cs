using System.Reflection;
using HttpServer.Attributes;

namespace HttpServer.Extensions;

public static class EnumExtensions
{
    public static string StringValue<T>(this T value)
        where T : Enum
    {
        string fieldName = value.ToString();
        var field = typeof(T).GetField(fieldName, BindingFlags.Public | BindingFlags.Static);
        return field?.GetCustomAttribute<StringValueAttribute>()?.Value ?? fieldName;
    }
}
