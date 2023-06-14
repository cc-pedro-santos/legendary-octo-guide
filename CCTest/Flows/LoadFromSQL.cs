using System.Threading.Tasks.Dataflow;
using CCTest.Actions;
using ComboCurve.Api.Model;

namespace CCTest;

public class LoadFromSQL
{
	int PageSize { get; }
	int MaxDegreeOfParallelism { get; }
	int CompletionCount { get; set; }

	public LoadFromSQL(int pageSize, int maxDegreeOfParallelism)
	{
		PageSize = pageSize;
		MaxDegreeOfParallelism = maxDegreeOfParallelism;
	}

	public async Task StartAsync(
		string wellTable, string prodTable, string projectID, string filePath, int wellsCount, CancellationToken token)
	{
        var currentPage = 0;
        var pages = (wellsCount / PageSize) + 1;

        var postBlock = SetupBlocks(wellTable, prodTable, projectID, filePath, token);

        try
		{
            while (currentPage <= pages)
            {
                if (postBlock.Post(currentPage))
                    currentPage++;
                else
                    await Task.Delay(TimeSpan.FromSeconds(1), token);
            }

            while (CompletionCount <= pages || token.IsCancellationRequested)
                await Task.Delay(TimeSpan.FromMilliseconds(250), token);
        }
		catch(Exception ex)
		{
			UI.ReportError(ex.Message);
		}
		
		CompleteBlocks();
	}

	IDataflowBlock[] Blocks { get; set; }

	private ITargetBlock<int> SetupBlocks(
		string wellTable, string prodTable, string projectID, string filePath, CancellationToken token)
	{
		var errorWellPath = Path.Combine(Directory.GetCurrentDirectory(), $"error_wells_{DateTime.Now:yyyyMMddHHmmss}.txt");
        var errorProdPath = Path.Combine(Directory.GetCurrentDirectory(), $"error_productions_{DateTime.Now:yyyyMMddHHmmss}.txt");

        // Wells
        var getWellSql = WellBlocks.GetWellsBlock(wellTable, PageSize, MaxDegreeOfParallelism, token);
		var saveWellApi = WellBlocks.SaveWellsBlock(projectID, MaxDegreeOfParallelism, token);

		// Productions
		var getProdSql = ProductionBlocks.GetProductionsBlock(prodTable, MaxDegreeOfParallelism, token);
		var saveProd = ProductionBlocks.SaveProductionsBlock(projectID, MaxDegreeOfParallelism, token);

		// Control
		var broadcastWells = new BroadcastBlock<WellMultiStatusResponse>(c => c);
		var broadcastProductions = new BroadcastBlock<ProductionMultiStatusResponse>(c => c);

		// Progress
		var wellProgress = new ActionBlock<WellMultiStatusResponse>(c =>
		{
			FileActions.SaveErrorLogs(errorWellPath, c);
            UI.ReportProgressWell(c);
		},
		new ExecutionDataflowBlockOptions
		{
			MaxDegreeOfParallelism = 1
		});

		var fileBlock = WellBlocks.GetSaveWellsBlock(filePath, MaxDegreeOfParallelism, token);

		// End Line
		var completionBlock = new ActionBlock<ProductionMultiStatusResponse>(c =>
			{
                FileActions.SaveErrorLogs(errorWellPath, c);
                UI.ReportProgressProduction(c);
				CompletionCount++;
			}, 
			new ExecutionDataflowBlockOptions
			{
				MaxDegreeOfParallelism = 1
			});

		// Flow
		getWellSql.LinkTo(saveWellApi);
		saveWellApi.LinkTo(broadcastWells);
		broadcastWells.LinkTo(getProdSql);
		getProdSql.LinkTo(saveProd);
		saveProd.LinkTo(broadcastProductions);
		broadcastProductions.LinkTo(completionBlock);

		broadcastWells.LinkTo(wellProgress);
		broadcastWells.LinkTo(fileBlock);

		Blocks = new IDataflowBlock[] 
		{ 
			getWellSql, 
			saveWellApi, 
			getProdSql, 
			saveProd, 
			broadcastWells, 
			broadcastProductions, 
			completionBlock,
			wellProgress,
		};

		return getWellSql;
	}

	private void CompleteBlocks()
	{
		foreach (var block in Blocks)
			block.Complete();
	}
}
