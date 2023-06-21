using System.Threading.Tasks.Dataflow;
using CCTest.Actions;
using CCTest.Models;
using ComboCurve.Api.Model;

namespace CCTest;

public class DeleteFromFile
{
	int MaxDegreeOfParallelism { get; }
	int CompletionCount { get; set; }

	public DeleteFromFile(int maxDegreeOfParallelism)
	{
        MaxDegreeOfParallelism = maxDegreeOfParallelism;
	}

	public async Task StartAsync(string filePath, string projectID, CancellationToken token)
	{
		var sourceBlock = SetupBlocks(projectID, token);

		using var reader = new StreamReader(filePath);
		var newLine = await reader.ReadLineAsync();
		var lines = 0;

		do 
		{
			if (newLine == null)
				break;

			var aux = newLine.Split(',');
			sourceBlock.Post(new WellID(aux[1], aux[0]));

			lines++;
			newLine = await reader.ReadLineAsync();
		}
		while (newLine != null);

		while (CompletionCount < lines && !token.IsCancellationRequested)
			await Task.Delay(TimeSpan.FromMilliseconds(250), token);

        CompleteBlocks();
    }

	IDataflowBlock[] Blocks { get; set; }

	private ITargetBlock<WellID> SetupBlocks(string projectID, CancellationToken token)
	{
		var singleThreadConfig = new ExecutionDataflowBlockOptions
		{
			MaxDegreeOfParallelism = 1
		};

		var getId = WellBlocks.GetIdWellBlock(MaxDegreeOfParallelism, projectID, token);
		var deleteProds = ProductionBlocks.DeleteProductionsBlock(projectID, MaxDegreeOfParallelism, token);
		var deleteWells = WellBlocks.DeleteWellsBlock(projectID, 1, token);

		// Batch
		var batchDelete = new BatchBlock<WellID>(100, 
			new GroupingDataflowBlockOptions { Greedy = true, }
		);

		// End Line
		var completionBlock = new ActionBlock<IEnumerable<WellID>>(c => 
		{
			var report = new WellMultiStatusResponse(new List<ErrorEntry>(), new List<WellStatus>(), c.Count(), 0);
			UI.ReportProgressWell(report);

			CompletionCount += c.Count();

		}, singleThreadConfig);

		// Flow
		batchDelete.LinkTo(getId);
		getId.LinkTo(deleteProds);
        deleteProds.LinkTo(deleteWells);
        deleteWells.LinkTo(completionBlock);

		Blocks = new IDataflowBlock[] 
		{ 
			deleteProds,
			deleteWells,
			completionBlock,
		};

		return batchDelete;
	}

	private void CompleteBlocks()
	{
		foreach (var block in Blocks)
			block.Complete();
	}
}
