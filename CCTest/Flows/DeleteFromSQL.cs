using System.Threading.Tasks.Dataflow;
using CCTest.Actions;
using CCTest.Models;
using ComboCurve.Api.Model;

namespace CCTest;

public class DeleteFromSQL
{
    int PageSize { get; }
    int MaxDegreeOfParallelism { get; }
    int CompletionCount { get; set; }

    public DeleteFromSQL(int pageSize, int maxDegreeOfParallelism)
    {
        PageSize = pageSize;
        MaxDegreeOfParallelism = maxDegreeOfParallelism;
    }

    public async Task StartAsync(
        string wellTable, string projectID, int wellsCount, CancellationToken token)
    {
        var currentPage = 0;
        var pages = (wellsCount / PageSize) + 1;

        var postBlock = SetupBlocks(wellTable, projectID, token);

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

    private ITargetBlock<int> SetupBlocks(string wellTable, string projectID, CancellationToken token)
    {
        // Wells
        var getWellSql = WellBlocks.GetWellsBlock(wellTable, PageSize, MaxDegreeOfParallelism, token);
        var getWellID = WellBlocks.GetIdWellBlock(MaxDegreeOfParallelism, projectID, token);
        var deleteWell = WellBlocks.DeleteWellsBlock(MaxDegreeOfParallelism, token);

        var transformWell = new TransformBlock<IEnumerable<SqlWell>, IEnumerable<WellID>>(
            c => c.Select(s => new WellID(s.ChosenID, string.Empty)),
            new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism });

        // Productions
        var deleteProd = ProductionBlocks.DeleteProductionsBlock(token, MaxDegreeOfParallelism);

        // End Line
        var completionBlock = new ActionBlock<IEnumerable<WellID>>(c =>
        {
            if (c.Any())
            {
                var report = new WellMultiStatusResponse(new List<ErrorEntry>(), new List<WellStatus>(), 0, c.Count());
                UI.ReportProgressWell(report);
            }
            
            CompletionCount++;            
        },
        new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = 1
        });

        getWellSql.LinkTo(transformWell);
        transformWell.LinkTo(getWellID);
        getWellID.LinkTo(deleteProd);
        deleteProd.LinkTo(deleteWell);
        deleteWell.LinkTo(completionBlock);

        Blocks = new IDataflowBlock[]
        {
            getWellSql,
            transformWell,
            getWellID,
            deleteProd,
            deleteWell,
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
