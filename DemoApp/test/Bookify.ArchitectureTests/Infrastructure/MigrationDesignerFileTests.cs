using System.IO;
using System.Linq;
using Xunit;

namespace Bookify.ArchitectureTests.Infrastructure;

public class MigrationDesignerFileTests
{
    [Fact]
    public void EveryMigration_ShouldHave_DesignerFile()
    {
        // DEMO: 2a leveraging unit tests
        // Adjust the path as needed if your test project is not at the solution root
        string migrationsPath = Path.Combine("..", "..", "..", "..", "..", "src", "Bookify.Infrastructure", "Migrations");
        var migrationFiles = Directory
            .EnumerateFiles(migrationsPath, "*.cs", SearchOption.TopDirectoryOnly)
            .Where(f => !f.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase) && !f.EndsWith("Snapshot.cs", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (string migrationFile in migrationFiles)
        {
            string designerFile = Path.ChangeExtension(migrationFile, ".Designer.cs");
            Assert.True(File.Exists(designerFile), $"Designer file missing for migration: {Path.GetFileName(migrationFile)}");
        }
    }
}
