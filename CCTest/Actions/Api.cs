using CCTest.Models;
using ComboCurve.Api.Api;
using ComboCurve.Api.Auth;
using ComboCurve.Api.Client;
using ComboCurve.Api.Model;
using Polly;

namespace CCTest.Actions;

public static class Api
{
	private static AsyncPolicy RetryPolicy = Policy
		.Handle<HttpRequestException>()
		.RetryAsync(3)
		.WithPolicyKey("api_retry");

	private static ComboCurveV1Api _api = null!;
	private static ComboCurveV1Api GetClient()
	{
		if (_api is null)
		{
            var apiKey = MyAppContext.Config.ApiKey;
			var serviceAccount = new ServiceAccount
            {
                ClientEmail = MyAppContext.Config.ClientEmail,
                ClientId = MyAppContext.Config.ClientID,
                PrivateKey = MyAppContext.Config.PrivateKey
            };

            _api = new ComboCurveV1Api(serviceAccount, apiKey, basePath: MyAppContext.Config.ApiUrl);
		}

		return _api;
	}

	public static async Task<WellMultiStatusResponse> SaveWellsAsync(
		List<WellInput> wells, string projectID, CancellationToken token)
	{
		var client = GetClient();

		return string.IsNullOrEmpty(projectID) 
			? await RetryPolicy.ExecuteAsync(() => client.PostWellsAsync(wells, token))
			: await RetryPolicy.ExecuteAsync(() => client.PostProjectsWellsAsync(projectID, wells, token));
	}

	public static async Task<ProductionMultiStatusResponse> SaveProductionsAsync(
		List<MonthlyProductionInput> productions, string projectID, CancellationToken token)
	{
		var client = GetClient();

		var output = new ProductionMultiStatusResponse(new List<ErrorEntry>(productions.Count), new List<ProductionStatus>(productions.Count), 0, 0);
		var pages = (productions.Count / 19500) +1;
		var current = 0;

		while(pages > 0)
		{
			var prodData = productions.Skip(current++ * 19500).Take(19500);
			if (prodData.Any())
			{
				var response = string.IsNullOrEmpty(projectID)
					? await RetryPolicy.ExecuteAsync(() => client.PostMonthlyProductionsAsync(productions, token))
					: await RetryPolicy.ExecuteAsync(() => client.PostProjectsMonthlyProductionsAsync(projectID, productions, token));

                output.GeneralErrors.AddRange(response.GeneralErrors ?? Enumerable.Empty<ErrorEntry>());
                output.Results.AddRange(response.Results ?? Enumerable.Empty<ProductionStatus>());
				output.SuccessCount += response.SuccessCount;
				output.FailedCount += response.FailedCount;
            }
		}

		return output;
	}

    public static async Task<ProductionMultiStatusResponse> UpdateProductionsAsync(
        List<MonthlyProductionInput> productions, string projectID, CancellationToken token)
    {
        var client = GetClient();
		return string.IsNullOrEmpty(projectID)
			? await RetryPolicy.ExecuteAsync(() => client.PutMonthlyProductionsAsync(productions, token))
			: await RetryPolicy.ExecuteAsync(() => client.PutProjectsMonthlyProductionsAsync(projectID, productions, token));
    }

    public static async Task<ProjectMultiStatusResponse> CreateProject(string name, CancellationToken token)
	{
		var client = GetClient();
		var project = new ProjectInput(name);

		return await RetryPolicy.ExecuteAsync(() => client.PostProjectsAsync(new List<ProjectInput> { project }, token));
	}

	public static async Task DeleteProductionAsync(string projectID, string wellID, CancellationToken token)
	{
		if (string.IsNullOrEmpty(projectID))
		{
			var client = GetClient();
			await RetryPolicy.ExecuteAsync(() => client.DeleteMonthlyProductionsAsync(wellID, token));
		}
		else
		{
			await RetryPolicy.ExecuteAsync(() => DeleteProjectsMonthlyProductions(projectID, wellID, token));
		}
    }

	public static async Task DeleteWellsAsync(string projectID, string[] wellIDs, CancellationToken token)
	{
		var client = GetClient();
		if (string.IsNullOrEmpty(projectID))
			 await RetryPolicy.ExecuteAsync(() => client.DeleteWellsAsync(id: wellIDs, cancellationToken: token));
		else await RetryPolicy.ExecuteAsync(() => client.DeleteProjectWellsAsync(projectID, id: wellIDs, cancellationToken: token));

    }

    public static async Task<IEnumerable<WellID>> GetWellIDs(string[] wellChosenIDS, string projectID, CancellationToken token)
    {
        var client = GetClient();
        var output = string.IsNullOrEmpty(projectID)
			? await RetryPolicy.ExecuteAsync(() => client.GetWellsAsync(chosenID: wellChosenIDS, take: wellChosenIDS.Length, cancellationToken: token))
            : await RetryPolicy.ExecuteAsync(() => client.GetProjectWellsAsync(projectID, chosenID: wellChosenIDS, take: wellChosenIDS.Length, cancellationToken: token));

        return output.Select(s => new WellID(string.Empty, s.Id));
    }

    public static async Task<ApiResponse<object>> DeleteProjectsMonthlyProductions(string projectId, string well, CancellationToken token)
    {
        var AsynchronousClient = new ApiClient(MyAppContext.Config.ApiUrl);
        var Configuration = ComboCurve.Api.Client.Configuration.MergeConfigurations(GlobalConfiguration.Instance, new Configuration
        {
            BasePath = MyAppContext.Config.ApiUrl
        });

        var localVarRequestOptions = new RequestOptions();

        string[] _contentTypes = Array.Empty<string>();
        string[] _accepts = Array.Empty<string>();

        var localVarContentType = ClientUtils.SelectHeaderContentType(_contentTypes);
        if (localVarContentType != null) localVarRequestOptions.HeaderParameters.Add("Content-Type", localVarContentType);

        var localVarAccept = ClientUtils.SelectHeaderAccept(_accepts);
        if (localVarAccept != null) localVarRequestOptions.HeaderParameters.Add("Accept", localVarAccept);

        localVarRequestOptions.PathParameters.Add("projectId", ClientUtils.ParameterToString(projectId));
        localVarRequestOptions.QueryParameters.Add(ClientUtils.ParameterToMultiMap("", "well", well));
        
        if (!string.IsNullOrEmpty(Configuration.GetApiKeyWithPrefix("x-api-key")))
            localVarRequestOptions.HeaderParameters.Add("x-api-key", Configuration.GetApiKeyWithPrefix("x-api-key"));

        if (!string.IsNullOrEmpty(Configuration.AccessToken))
            localVarRequestOptions.HeaderParameters.Add("Authorization", "Bearer " + Configuration.AccessToken);

        var localVarResponse = await AsynchronousClient.DeleteAsync<object>(
			"/v1/projects/{projectId}/monthly-productions", 
			localVarRequestOptions, 
			Configuration, 
			token
		);

        return localVarResponse;
    }
}
