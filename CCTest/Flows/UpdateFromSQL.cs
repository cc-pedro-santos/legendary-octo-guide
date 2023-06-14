using System.Threading.Tasks.Dataflow;
using CCTest.Actions;
using CCTest.Models;
using ComboCurve.Api.Model;

namespace CCTest;

public class UpdateFromSQL
{
    int PageSize { get; }
    int MaxDegreeOfParallelism { get; }
    int CompletionCount { get; set; }

    public UpdateFromSQL(int pageSize, int maxDegreeOfParallelism)
    {
        PageSize = pageSize;
        MaxDegreeOfParallelism = maxDegreeOfParallelism;
    }

    public async Task StartAsync(
        string wellTable, string prodTable, string projectID, int wellsCount, CancellationToken token)
    {
        var currentPage = 0;
        var pages = (wellsCount / PageSize) + 1;

        var postBlock = SetupBlocks(wellTable, prodTable, projectID, token);

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
        catch (Exception ex)
        {
            UI.ReportError(ex.Message);
        }

        CompleteBlocks();
    }

    IDataflowBlock[] Blocks { get; set; }

    private ITargetBlock<int> SetupBlocks(
        string wellTable, string prodTable, string projectID, CancellationToken token)
    {
        var errorProdPath = Path.Combine(Directory.GetCurrentDirectory(), $"error_productions_{DateTime.Now:yyyyMMddHHmmss}.txt");

        // Wells
        var getWellSql = WellBlocks.GetWellsBlock(wellTable, PageSize, MaxDegreeOfParallelism, token);
        var transformWell = new TransformBlock<IEnumerable<SqlWell>, WellMultiStatusResponse>(
            c => new WellMultiStatusResponse(results: c.Select(s => new WellStatus("Created", 201, chosenID: s.ChosenID)).ToList()),
            new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism });

        // Productions
        var getProdSql = ProductionBlocks.GetProductionsBlock(prodTable, MaxDegreeOfParallelism, token);
        var saveProd = ProductionBlocks.UpdateProductionsBlock(projectID, MaxDegreeOfParallelism, token);

        // End Line
        var completionBlock = new ActionBlock<ProductionMultiStatusResponse>(c =>
        {
            CompletionCount++;
            UI.ReportProgressProduction(c);
            FileActions.SaveErrorLogs(errorProdPath, c);
        },
        new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = 1
        });

        // Flow
        getWellSql.LinkTo(transformWell);
        transformWell.LinkTo(getProdSql);
        getProdSql.LinkTo(saveProd);
        saveProd.LinkTo(completionBlock);

        Blocks = new IDataflowBlock[]
        {
            getWellSql,
            transformWell,
            getProdSql,
            saveProd,
            completionBlock,
        };

        return getWellSql;
    }

    private void CompleteBlocks()
    {
        foreach (var block in Blocks)
            block.Complete();
    }
}
