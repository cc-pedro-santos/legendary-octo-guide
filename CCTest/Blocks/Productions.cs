using System.Threading.Tasks.Dataflow;
using CCTest.Actions;
using CCTest.Models;
using ComboCurve.Api.Model;

namespace CCTest;

public static class ProductionBlocks
{
	public static TransformBlock<WellMultiStatusResponse, IEnumerable<Production>> GetProductionsBlock(
		string table, int maxDegreeOfParallelism, CancellationToken token
	) => new (async response =>
			{
				try
				{
					if (response is null || response.Results is null)
                        return Array.Empty<Production>();

                    var validProductions = response.Results.Where(w => w.Status == "Created");
					if (!validProductions.Any())
						return Array.Empty<Production>();

                    var output = await Sql.GetProductionsAsync(table, validProductions.Select(s => s.ChosenID).ToArray(), token);
                    return output;
                }
				catch(Exception ex)
				{
					UI.ReportError(ex.Message);
					return Array.Empty<Production>();
				}
			},			
			new ExecutionDataflowBlockOptions
			{
				MaxDegreeOfParallelism = maxDegreeOfParallelism
			}); 

	public static TransformBlock<IEnumerable<Production>, ProductionMultiStatusResponse> SaveProductionsBlock(
		 string projectID, int maxDegreeOfParallelism, CancellationToken token
    ) => new (async prods =>
		{
			try
			{
                if (!prods.Any())
                    return new ProductionMultiStatusResponse();

                var response = await Api.SaveProductionsAsync(prods.Select(s => Mappings.ToApi(s)).ToList(), projectID, token);
				return response;
			}
			catch (Exception ex)
			{
                UI.ReportError(ex.Message);
                return new ProductionMultiStatusResponse();

			}
		},			
		new ExecutionDataflowBlockOptions
		{
			MaxDegreeOfParallelism = maxDegreeOfParallelism
		});

    public static TransformBlock<IEnumerable<Production>, ProductionMultiStatusResponse> UpdateProductionsBlock(
        string projectID, int maxDegreeOfParallelism, CancellationToken token
    ) => new(async prods =>
		{
			try
			{
				if (!prods.Any())
					return new ProductionMultiStatusResponse();

				var response = await Api.UpdateProductionsAsync(prods.Select(s => Mappings.ToApiUpdate(s)).ToList(), projectID, token);
				return response;
			}
			catch (Exception ex)
			{
				UI.ReportError(ex.Message);
				return new ProductionMultiStatusResponse();

			}
		},
		new ExecutionDataflowBlockOptions
		{
			MaxDegreeOfParallelism = maxDegreeOfParallelism
		});

    public static TransformBlock<IEnumerable<WellID>, IEnumerable<WellID>> DeleteProductionsBlock(
		string projectID, int maxDegreeOfParallelism, CancellationToken token
	) => new (async wellIDs =>
		{
			try
			{
				foreach (var item in wellIDs)
				{
                    await Api.DeleteProductionAsync(projectID, item.wellID, token);
                    UI.ReportProductionDelete(item.wellID);
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
			MaxDegreeOfParallelism = maxDegreeOfParallelism * 2
		}); 
}
