using ComboCurve.Api.Model;
using Spectre.Console;

namespace CCTest.Actions;

public static class UI
{
	public const string LoadTexasWellsOP = "Load Texas Wells";
	public const string LoadTexasLovingWellsOP = "Load Texas Loving Wells";
	public const string DeleteWellsOP = "Delete Wells From File";
    public const string DeleteTexasWellsOP = "Delete Texas Wells";
    public const string DeleteTexasLovingWellsOP = "Delete Texas Loving Wells";
    public const string UpdateTexasWellsOP = "Update Texas Wells";
    public const string UpdateTexasLovingWellsOP = "Update Texas Loving Wells";
    public const string ExitOP = "[red]Exit[/]";

	private static ProgressContext Progress { get; set; }
	private static ProgressTask WellProgress { get; set; }
    private static ProgressTask ProductionProgress { get; set; }
	private static CancellationTokenSource CancelToken { get; set; }

    public static string Menu() 
	{
		CleanUIInstances();
		return AnsiConsole.Prompt(new SelectionPrompt<string>()
			.Title("What's your [green]choose[/]?")
			.PageSize(10)
			.MoreChoicesText("[grey](Move up and down)[/]")
			.AddChoices(new[] {
                LoadTexasWellsOP,
				LoadTexasLovingWellsOP,
				DeleteWellsOP,
				DeleteTexasWellsOP,
				DeleteTexasLovingWellsOP,
				UpdateTexasWellsOP,
				UpdateTexasLovingWellsOP,
				ExitOP,
			}));
	}

	private static async Task<(string, bool)> AskAboutProject(CancellationToken token)
	{
		if (AnsiConsole.Ask<bool>("Do you want to [green]create a new project[/]?"))
		{
			var name = AnsiConsole.Ask<string>("What's the [green]project name[/]?");
			var response = await Api.CreateProject(name, token);

			if (response is not null && response.SuccessCount != 1 && response.Results is not null)
			{
				ReportError(response.Results[0].Errors[0].Message);
				return (string.Empty, false);
			}

			return (response.Results[0].Id, true);
		}

		return (string.Empty, true);
	}

    #region .:: Update ::.

    public static Task UpdateTexasWellsAsync() =>
        UpdateWellsFromSQL("CCTest.Temp_Wells_USA_Texas", "CCTest.Temp_Production_USA_Texas_Load");

    public static Task UpdateTexasLovingWellsAsync() =>
        UpdateWellsFromSQL("CCTest.Temp_Wells_USA_Texas_Loving", "CCTest.Temp_Production_USA_Texas_Loving_Load");

    public static async Task UpdateWellsFromSQL(string wellTb, string prodTb)
    {
        var pageSize = AnsiConsole.Ask("How many [green]wells per query?[/]? (100)", 100);
        var maxDegreeOfParallelism = AnsiConsole.Ask("How many [green]parallel queries?[/]? (4)", 4);
        var projectID = AnsiConsole.Ask("Project ID? [gray](let empty if not)[/]", string.Empty);

        CancelToken = new CancellationTokenSource();

        Console.Clear();
        Console.CancelKeyPress += Console_CancelKeyPress;

        AnsiConsole.MarkupLine("Updating: [green](GAS = WATER) (OIL = GAS) (WATER = OIL)[/]");

        await AnsiConsole.Status()
            .AutoRefresh(true)
            .StartAsync("Updating Productions... Press [red]ctrl + c[/] anytime to stop", async ctx =>
            {
                ctx.Spinner(Spinner.Known.Pong);
                ctx.SpinnerStyle(Style.Parse("blue"));

                var wellsCount = await Sql.CountWellsAsync(wellTb, CancelToken.Token);
                var task = new UpdateFromSQL(pageSize, maxDegreeOfParallelism);

                await task.StartAsync(wellTb, prodTb, projectID, wellsCount, CancelToken.Token);
            });

        Console.CancelKeyPress -= Console_CancelKeyPress;

        AnsiConsole.MarkupLine("That's all folks");
        AnsiConsole.MarkupLine("Press anything to go to the menu [green]:)[/]");

        Console.ReadLine();
    }

    #endregion

    #region .:: Delete ::.

    public static async Task DeleteWellsFromFile()
	{
        var maxDegreeOfParallelism = AnsiConsole.Ask("How many [green]parallel queries?[/]? (4)", 4);
        var path = AnsiConsole.Ask<string>("Where is the deletion file?");
        var projectID = AnsiConsole.Ask("Project ID? [gray](let empty if not)[/]", string.Empty);

        if (File.Exists(path) is false)
		{
			AnsiConsole.MarkupLine("File not found =/");
			return;
		}

        CancelToken = new CancellationTokenSource();

		Console.Clear();
		Console.CancelKeyPress += Console_CancelKeyPress;

		await AnsiConsole.Status()
			.AutoRefresh(true)
			.StartAsync("Deleting everything... Press [red]ctrl + c[/] anytime to stop", async ctx =>
			{
				ctx.Spinner(Spinner.Known.Star);
        		ctx.SpinnerStyle(Style.Parse("green"));

				var task = new DeleteFromFile(maxDegreeOfParallelism);
				await task.StartAsync(path, projectID, CancelToken.Token);
			});

        Console.CancelKeyPress -= Console_CancelKeyPress;

        AnsiConsole.MarkupLine("That's all folks");
        AnsiConsole.MarkupLine("Press anything to go to the menu [green]:)[/]");

		Console.ReadLine();
    }

    public static Task DeleteTexasWellsAsync() =>
        DeleteWellsFromSQL("CCTest.Temp_Wells_USA_Texas");

    public static Task DeleteTexasLovingWellsAsync() =>
        DeleteWellsFromSQL("CCTest.Temp_Wells_USA_Texas_Loving");

    public static async Task DeleteWellsFromSQL(string wellTb)
    {
        var pageSize = AnsiConsole.Ask("How many [green]wells per query?[/]? (100)", 100);
        var maxDegreeOfParallelism = AnsiConsole.Ask("How many [green]parallel queries?[/]? (4)", 4);
        var projectID = AnsiConsole.Ask("Project ID? [gray](let empty if not)[/]", string.Empty);

        CancelToken = new CancellationTokenSource();

        Console.Clear();
        Console.CancelKeyPress += Console_CancelKeyPress;

        await AnsiConsole.Status()
            .AutoRefresh(true)
            .StartAsync("Deleting everything... Press [red]ctrl + c[/] anytime to stop", async ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots10);
                ctx.SpinnerStyle(Style.Parse("yellow"));

                var wellsCount = await Sql.CountWellsAsync(wellTb, CancelToken.Token);
                var task = new DeleteFromSQL(pageSize, maxDegreeOfParallelism);

                await task.StartAsync(wellTb, projectID, wellsCount, CancelToken.Token);
            });

        Console.CancelKeyPress -= Console_CancelKeyPress;

        AnsiConsole.MarkupLine("That's all folks");
        AnsiConsole.MarkupLine("Press anything to go to the menu [green]:)[/]");

        Console.ReadLine();
    }

    #endregion

    #region .:: Load From SQL ::.

    public static Task LoadTexasWellsAsync() =>
		LoadCompanyWellsAsync("CCTest.Temp_Wells_USA_Texas", "CCTest.Temp_Production_USA_Texas_Load");

	public static Task LoadTexasLovingWellsAsync() =>
		LoadCompanyWellsAsync("CCTest.Temp_Wells_USA_Texas_Loving", "CCTest.Temp_Production_USA_Texas_Loving_Load");

    private static async Task LoadCompanyWellsAsync(string wellTb, string prodTb)
	{
		var pageSize = AnsiConsole.Ask("How many [green]wells per query?[/]? (100)", 100);
		var maxDegreeOfParallelism = AnsiConsole.Ask("How many [green]parallel queries?[/]? (4)", 4);

        CancelToken = new CancellationTokenSource();

		var (project, success) = await AskAboutProject(CancelToken.Token);

		AnsiConsole.Clear();

		if (!success)
			return;

		if (!string.IsNullOrEmpty(project))
			AnsiConsole.MarkupLine($"[red]PROJECT ID: {project}[/]");

		var fileName = $"import_{DateTime.Now:yyyyMMddHHmmss}.csv";
		var filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

		AnsiConsole.MarkupLine("The saved wells will be saved here: [blue]{0}[/]", filePath);
		AnsiConsole.MarkupLine("Loading wells... Press [red]ctrl + c[/] anytime to stop");

		Console.CancelKeyPress += Console_CancelKeyPress;

		await AnsiConsole.Progress()
			.AutoRefresh(true)
			.AutoClear(false)
			.HideCompleted(false)
			.Columns(new ProgressColumn[] 
			{
				new TaskDescriptionColumn(),
				new ProgressBarColumn(),     
				new PercentageColumn(),
				new ElapsedTimeColumn(),
				new SpinnerColumn(),       
			})
			.StartAsync(async ctx =>
			{
				var wellsCount = await Sql.CountWellsAsync(wellTb, CancelToken.Token);
				var pages = (wellsCount / pageSize) + 1;

				Progress = ctx;
                WellProgress = ctx.AddTask("Wells", true, pages);
				ProductionProgress = ctx.AddTask("Production", true, pages);

				var load = new LoadFromSQL(pageSize, maxDegreeOfParallelism);
				await load.StartAsync(wellTb, prodTb, project, filePath, wellsCount, CancelToken.Token);
			});

		
        Console.CancelKeyPress -= Console_CancelKeyPress;
        Console.ReadLine();

        AnsiConsole.MarkupLine("That's all folks");
        AnsiConsole.MarkupLine("Press anything to go to the menu [green]:)[/]");

        Console.ReadLine();
    }

	#endregion

	#region  .:: Progress ::.

	public static void ReportProgressWell(WellMultiStatusResponse r)
	{
		if (r is not null && r.SuccessCount is not null && r.FailedCount is not null)
		{
			var exists = r.Results.Count(c => c.Errors is not null 
											&& c.Errors.Count == 1 
											&& c.Errors[0].Name == "WellExistsError");
			var msg = $"[gray]WELL API:[/] {r.SuccessCount + r.FailedCount}|[green]{r.SuccessCount}[/]|[red]{r.FailedCount}[/]|[blue]{exists}[/]";
			AnsiConsole.MarkupLine(msg);
		}

		if (WellProgress is not null && !WellProgress.IsFinished)
		{
            WellProgress.Increment(1);
			Progress.Refresh();
		}
	}

    public static void ReportProgressProduction(ProductionMultiStatusResponse r)
    {
        if (r is not null && r.SuccessCount is not null && r.FailedCount is not null)
            AnsiConsole.MarkupLine($"[gray]PROD API:[/] {r.SuccessCount + r.FailedCount}|[green]{r.SuccessCount}[/]|[red]{r.FailedCount}[/]");

        if (ProductionProgress is not null && !ProductionProgress.IsFinished)
		{
            ProductionProgress.Increment(1);
			Progress.Refresh();
		}
	}

	public static void ReportProductionDelete(string wellID)
    {
		var msg = $"[gray]PROD API:[/] {wellID} production deleted";
		AnsiConsole.MarkupLine(msg);
	}

    public static void ReportError(string error) =>
		AnsiConsole.MarkupLine($"[red]LOG::[/] {error}");

	#endregion

	private static void CleanUIInstances()
	{
		Progress = null;
		WellProgress = null;
		ProductionProgress = null;
	}

	private static void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
	{
        AnsiConsole.MarkupLine($"[red]STOPING[/]");
        CancelToken.Cancel();
	}
}
