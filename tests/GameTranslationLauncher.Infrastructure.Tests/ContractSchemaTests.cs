using System.Text.Json;

namespace GameTranslationLauncher.Infrastructure.Tests;

[TestClass]
public sealed class ContractSchemaTests
{
    [TestMethod]
    public void ContractSchemas_ParseAsJsonObjects()
    {
        var contractsDirectory = Path.Combine(TestRepositoryPaths.LauncherRoot, "contracts");
        var schemaPaths = Directory.GetFiles(contractsDirectory, "*.schema.json");

        Assert.HasCount(3, schemaPaths);
        foreach (var schemaPath in schemaPaths)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(schemaPath));
            Assert.AreEqual(JsonValueKind.Object, document.RootElement.ValueKind, schemaPath);
        }
    }
}
