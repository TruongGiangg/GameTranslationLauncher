using System.Text.Json;
using System.Text.Json.Nodes;

namespace GameTranslationLauncher.Infrastructure.Tests;

internal sealed class TemporaryManifest : IDisposable
{
    private readonly TemporaryDirectory temporaryDirectory;

    private TemporaryManifest(TemporaryDirectory temporaryDirectory, string manifestPath)
    {
        this.temporaryDirectory = temporaryDirectory;
        ManifestPath = manifestPath;
    }

    public string ManifestPath { get; }

    public static TemporaryManifest Create(Action<JsonObject> mutate)
    {
        var json = File.ReadAllText(TestRepositoryPaths.TinyEdenManifestPath);
        var manifest = JsonNode.Parse(json)?.AsObject()
            ?? throw new InvalidDataException("Manifest Tiny Eden không đọc được trong test.");

        mutate(manifest);

        var temporaryDirectory = new TemporaryDirectory();
        var manifestPath = temporaryDirectory.GetPath("launcher-package.json");
        File.WriteAllText(
            manifestPath,
            manifest.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

        return new TemporaryManifest(temporaryDirectory, manifestPath);
    }

    public void Dispose()
    {
        temporaryDirectory.Dispose();
    }
}
