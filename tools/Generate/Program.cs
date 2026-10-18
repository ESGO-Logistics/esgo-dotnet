// Regenerates src/Esgo/Generated/Models.g.cs from the ESGO OpenAPI spec.
//
//   dotnet run --project tools/Generate              fetch the live spec, then generate
//   dotnet run --project tools/Generate -- --offline use the openapi.json in the repo
//
// NSwag is the generator; this program only sets what its command line cannot:
// C# names (weight_kg -> WeightKg) and an enum converter that honours
// [EnumMember] (System.Text.Json ignores it and would send "Any" instead of "any").
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using NJsonSchema;
using NJsonSchema.CodeGeneration;
using NJsonSchema.CodeGeneration.CSharp;
using NSwag;
using NSwag.CodeGeneration.CSharp;

const string SpecUrl = "https://api.es-go.de/v1/openapi.json";

// Same output on every machine (a German Windows would otherwise format numbers differently).
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

var root = FindRepoRoot();
var specFile = Path.Combine(root, "openapi.json");
var target = Path.Combine(root, "src", "Esgo", "Generated", "Models.g.cs");

if (!args.Contains("--offline"))
{
    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    var live = JsonNode.Parse(await http.GetStringAsync(SpecUrl))!;
    var text = SortKeys(live)!.ToJsonString(new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    });
    await File.WriteAllTextAsync(specFile, text.Replace("\r\n", "\n") + "\n");
    Console.WriteLine($"Spec fetched: {SpecUrl}");
}

var spec = await OpenApiDocument.FromFileAsync(specFile);
var settings = new CSharpClientGeneratorSettings
{
    GenerateClientClasses = false,
    GenerateClientInterfaces = false,
    GenerateExceptionClasses = false,
    GenerateDtoTypes = true,
    CSharpGeneratorSettings =
    {
        Namespace = "Esgo.Models",
        JsonLibrary = CSharpJsonLibrary.SystemTextJson,
        ClassStyle = CSharpClassStyle.Poco,
        GenerateNullableReferenceTypes = true,
        GenerateOptionalPropertiesAsNullable = true,
        GenerateDataAnnotations = false,
        DateTimeType = "System.DateTimeOffset",
        ArrayType = "System.Collections.Generic.IReadOnlyList",
        ArrayInstanceType = "System.Collections.Generic.List",
        PropertyNameGenerator = new PascalPropertyNames(),
        TypeNameGenerator = new PascalTypeNames(),
    },
};

var code = new CSharpClientGenerator(spec, settings).GenerateFile()
    .Replace("System.Text.Json.Serialization.JsonStringEnumConverter<", "global::Esgo.Internal.EnumMemberConverter<")
    .Replace("\r\n", "\n");
Directory.CreateDirectory(Path.GetDirectoryName(target)!);
await File.WriteAllTextAsync(target, code);
Console.WriteLine($"Generated: {Path.GetRelativePath(root, target)}");

static string FindRepoRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Esgo.slnx")))
        dir = dir.Parent;
    return dir?.FullName ?? throw new InvalidOperationException("Esgo.slnx not found above " + AppContext.BaseDirectory);
}

static JsonNode? SortKeys(JsonNode? node) => node switch
{
    JsonObject obj => new JsonObject(obj.OrderBy(p => p.Key, StringComparer.Ordinal)
        .Select(p => KeyValuePair.Create(p.Key, SortKeys(p.Value?.DeepClone())))),
    JsonArray arr => new JsonArray(arr.Select(x => SortKeys(x?.DeepClone())).ToArray()),
    _ => node?.DeepClone(),
};

static class Names
{
    public static string Pascal(string raw) => string.Concat(
        raw.Split(new[] { '_', '-', ' ', '.' }, StringSplitOptions.RemoveEmptyEntries)
           .Select(p => char.ToUpperInvariant(p[0]) + p.Substring(1)));
}

sealed class PascalPropertyNames : IPropertyNameGenerator
{
    public string Generate(JsonSchemaProperty property) => Names.Pascal(property.Name);
}

sealed class PascalTypeNames : DefaultTypeNameGenerator
{
    protected override string Generate(JsonSchema schema, string? typeNameHint)
        => Names.Pascal(base.Generate(schema, typeNameHint));
}
