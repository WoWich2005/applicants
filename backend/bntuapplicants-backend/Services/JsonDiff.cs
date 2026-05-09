using System.Text.Json;

namespace bntuapplicants_backend.Services
{
    public static class JsonDiff
    {
        public static Dictionary<string, object?> Compute(object before, object after)
        {
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

            var beforeJson = JsonSerializer.SerializeToElement(before, options);
            var afterJson = JsonSerializer.SerializeToElement(after, options);

            var diff = new Dictionary<string, object?>();

            foreach (var prop in afterJson.EnumerateObject())
            {
                bool changed = !beforeJson.TryGetProperty(prop.Name, out var oldVal)
                    || oldVal.GetRawText() != prop.Value.GetRawText();

                if (changed)
                {
                    diff[prop.Name] = new
                    {
                        old = beforeJson.TryGetProperty(prop.Name, out var o)
                            ? JsonSerializer.Deserialize<object>(o.GetRawText())
                            : null,
                        @new = JsonSerializer.Deserialize<object>(prop.Value.GetRawText())
                    };
                }
            }

            return diff;
        }
    }
}
