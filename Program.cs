using Microsoft.Data.Sqlite;
using System.Reflection;

Console.WriteLine("Attempting to bypass Kobo login requirement");
Console.WriteLine();

var driveInfo = new DriveInfo(Environment.CurrentDirectory);
try
{
    if (!driveInfo.IsReady)
        throw new("Drive is not ready");

    if (driveInfo.DriveType != DriveType.Removable)
        throw new(@"
Drive is not removable!
This utility must be run from a folder on the Kobo device itself.
If you're currently running this from a folder on your computer, 
- Choose 'Set up device with USB'
- Connect your device to the computer
- Move this executable to the Kobo drive
- Run again");

    var dbpaths = Directory.EnumerateFiles(Directory.GetDirectoryRoot(Environment.CurrentDirectory), "KoboReader.sqlite", SearchOption.AllDirectories)
        .ToList();

    if (dbpaths.Count == 0)
        throw new("Failed to find KoboReader.sqlite database");
    else if (dbpaths.Count > 1)
        throw new("Found multiple files called KoboReader.sqlite");

    Console.WriteLine($"Drive name: {driveInfo.Name}");
    Console.WriteLine($"Drive label: {driveInfo.VolumeLabel}");
    Console.WriteLine($"Database file: {dbpaths.Single()}");

    using (new ScopedColor(ConsoleColor.Yellow))
        Console.WriteLine("If the above information looks correct, type 'yes' to continue");

    var prompt = (Console.ReadLine() ?? string.Empty)
        .Trim(" '\"".ToCharArray());

    if (prompt.ToUpperInvariant() != "YES")
        throw new("Cancelled by user");

    using (var conn = OpenDb(dbpaths.Single()))
    {
        var scripts = new[]
        {
            "1 - Users.sql",
            //"2 - Analytics events.sql",
        };

        foreach (var script in scripts)
        {
            Console.WriteLine($"Running script {script}");

            using var command = conn.CreateCommand();
            command.CommandText = ReadEmbeddedScript(script);
            command.ExecuteNonQuery();

            Console.WriteLine("\tScript completed without errors");
        }
    }

    using (new ScopedColor(ConsoleColor.Green))
        Console.WriteLine("Done");
}
catch (Exception e)
{
    using (new ScopedColor(ConsoleColor.Red))
        Console.WriteLine($"An error occurred: {e.Message}");
#if DEBUG
    throw;
#endif
}

Console.WriteLine("Press enter to continue");
Console.ReadLine();

SqliteConnection OpenDb(string dbpath)
{
    var connection = new SqliteConnection($"Data Source={dbpath}");
    connection.Open();
    return connection;
}

string ReadEmbeddedScript(string name)
{
    var assembly = Assembly.GetExecutingAssembly();
    var resourceName = assembly.GetManifestResourceNames().Single(str => str.EndsWith(name));
    using (var stream = assembly.GetManifestResourceStream(resourceName)!)
    using (var reader = new StreamReader(stream))
        return reader.ReadToEnd();
}

class ScopedColor : IDisposable
{
    public ConsoleColor OldColor { get; }
    public void Dispose() => Console.ForegroundColor = OldColor;

    public ScopedColor(ConsoleColor color)
    {
        OldColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
    }
}