using CCTest;
using CCTest.Actions;
using Newtonsoft.Json;
using Spectre.Console;

static void LoadConfig()
{
	var filePath = AnsiConsole.Ask<string>("Where's your [green]config file[/]?");
	var file = filePath.Contains("config.json") ? filePath : Path.Combine(filePath, "config.json");

	var content = File.ReadAllText(file);
	MyAppContext.Config = JsonConvert.DeserializeObject<MyAppContext.Configuration>(content);
}

LoadConfig();
string op = string.Empty;

do
{
	AnsiConsole.Clear();
	op = UI.Menu();

	switch (op)
	{
		case UI.LoadTexasWellsOP:
			await UI.LoadTexasWellsAsync();
			break;
		case UI.LoadTexasLovingWellsOP:
			await UI.LoadTexasLovingWellsAsync();
			break;
        case UI.DeleteWellsOP:
            await UI.DeleteWellsFromFile();
            break;
        case UI.DeleteTexasLovingWellsOP:
            await UI.DeleteTexasLovingWellsAsync();
            break;
        case UI.DeleteTexasWellsOP:
            await UI.DeleteTexasWellsAsync();
            break;
        case UI.UpdateTexasLovingWellsOP:
            await UI.UpdateTexasLovingWellsAsync();
            break;
        case UI.UpdateTexasWellsOP:
            await UI.UpdateTexasWellsAsync();
            break;
    }
} while (op != UI.ExitOP);

Environment.Exit(0);