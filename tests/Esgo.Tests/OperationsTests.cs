using System.Reflection;
using System.Text.Json;
using Xunit;

namespace Esgo.Tests;

/// <summary>
/// Operations.cs must cover the spec exactly. When the API gains an endpoint, regenerate.yml
/// updates openapi.json and these tests fail until the operation is added.
/// </summary>
public class OperationsTests
{
    private static readonly JsonDocument Spec = JsonDocument.Parse(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "openapi.json")));

    private static IEnumerable<(string Id, string Method, string Path, JsonElement Operation)> SpecOperations()
    {
        foreach (var path in Spec.RootElement.GetProperty("paths").EnumerateObject())
            foreach (var method in path.Value.EnumerateObject())
                if (method.Value.ValueKind == JsonValueKind.Object && method.Value.TryGetProperty("operationId", out var id))
                    yield return (id.GetString()!, method.Name.ToUpperInvariant(), path.Name, method.Value);
    }

    private static string? RefName(JsonElement? schema)
        => schema is { } s && s.TryGetProperty("$ref", out var r) ? r.GetString()!.Split('/').Last() : null;

    [Fact]
    public void Every_spec_operation_has_an_sdk_method()
    {
        Assert.Equal(
            SpecOperations().Select(o => o.Id).OrderBy(x => x),
            Operations.All.Select(o => o.OperationId).OrderBy(x => x));
    }

    [Fact]
    public void Method_path_and_models_match_the_spec()
    {
        foreach (var (id, method, path, op) in SpecOperations())
        {
            var sdk = Operations.All.Single(o => o.OperationId == id);
            Assert.Equal(method, sdk.Method);
            Assert.Equal(path, sdk.Path);

            JsonElement? body = op.TryGetProperty("requestBody", out var rb)
                ? rb.GetProperty("content").GetProperty("application/json").GetProperty("schema")
                : null;
            Assert.Equal(RefName(body), sdk.Request?.Name);

            var success = op.GetProperty("responses").EnumerateObject().First(r => r.Name.StartsWith("2"));
            var response = success.Value.GetProperty("content").GetProperty("application/json").GetProperty("schema");
            Assert.Equal(RefName(response), sdk.Response.Name);
        }
    }

    [Fact]
    public void Sdk_methods_exist_on_the_client()
    {
        foreach (var op in Operations.All)
        {
            var parts = op.SdkMethod.Split('.');
            var resource = typeof(EsgoClient).GetProperty(parts[0], BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(resource);
            var method = resource!.PropertyType.GetMethod(parts[1], BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(method);
            Assert.Equal(op.Response, method!.ReturnType.GetGenericArguments().Single());
        }
    }
}
