using System.Threading.Tasks.Dataflow;
using CCTest.Actions;
using CCTest.Models;
using ComboCurve.Api.Model;

namespace CCTest;

public static class WellBlocks
{	
	public static TransformBlock<int, IEnumerable<SqlWell>> GetWellsBlock(
		string table, int pageSize, int maxDegreeOfParallelism, CancellationToken token
	) => new (async page =>
			{
				try
				{
					var wells = await Sql.GetWellsAsync(table, page, pageSize, token);
					return wells;
				}
				catch(Exception ex)
				{
                    UI.ReportError(ex.Message);
                    return Array.Empty<SqlWell>();
				}
			},			
			new ExecutionDataflowBlockOptions
			{
				MaxDegreeOfParallelism = maxDegreeOfParallelism
			});

	public static TransformBlock<IEnumerable<SqlWell>, WellMultiStatusResponse> SaveWellsBlock(
		string projectID, int maxDegreeOfParallelism, CancellationToken token
	) => new (async page =>
			{
				try
				{
					if (page is null || !page.Any())
                        return new WellMultiStatusResponse();

					var wells = page.Select(s => Mappings.ToApiWell(s)).ToList();
                    var response = await Api.SaveWellsAsync(wells, projectID, token);
					return response;
				}
				catch (Exception ex)
				{
                    UI.ReportError(ex.Message);
                    return new WellMultiStatusResponse();
				}
			},		
			new ExecutionDataflowBlockOptions
			{
				MaxDegreeOfParallelism = maxDegreeOfParallelism
			});


	public static ActionBlock<WellMultiStatusResponse> GetSaveWellsBlock(
		string filePath, int maxDegreeOfParallelism, CancellationToken token)
		=> new (async response =>
		{
			try
			{
                var lines = response.Results.Select(s => $"{s.Id},{s.ChosenID}");
                await File.AppendAllLinesAsync(filePath, lines, token);
            }
			catch (Exception ex)
			{
				UI.ReportError(ex.Message);
			}
		},
		new ExecutionDataflowBlockOptions
		{
			MaxDegreeOfParallelism = 1
		});

	public static TransformBlock<IEnumerable<WellID>, IEnumerable<WellID>> DeleteWellsBlock(
		int maxDegreeOfParallelism, CancellationToken token)
		=> new (async wellIDs =>
		{
			try
			{
				if (wellIDs.Any())
					await Api.DeleteWellsAsync(wellIDs.Select(s => s.wellID).ToArray(), token);
            }
			catch (Exception ex)
			{
				UI.ReportError(ex.Message);
			}

			return wellIDs;
		},
		new ExecutionDataflowBlockOptions
		{
			MaxDegreeOfParallelism = maxDegreeOfParallelism
		});


    public static TransformBlock<IEnumerable<WellID>, IEnumerable<WellID>> GetIdWellBlock(
        int maxDegreeOfParallelism, string projectID, CancellationToken token)
        => new(async wellIDs =>
        {
            try
            {
				var emptyWellIDs = wellIDs.Where(w => string.IsNullOrEmpty(w.wellID)).Select(s => s.ChosenID);
				if (emptyWellIDs.Any())
				{
					var ids = await Api.GetWellIDs(emptyWellIDs.ToArray(), projectID, token);
					wellIDs = ids.Concat(wellIDs.Where(w => !string.IsNullOrEmpty(w.wellID))).ToArray();
				}
            }
            catch (Exception ex)
            {
                UI.ReportError(ex.Message);
            }

            return wellIDs;
        },
        new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = maxDegreeOfParallelism
        });
}
