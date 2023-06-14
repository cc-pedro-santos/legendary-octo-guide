using System.Data.SqlClient;
using CCTest.Models;
using Dapper;

namespace CCTest.Actions;

public static class Sql
{
	private static async Task<T> WrapConnection<T>(Func<SqlConnection, CancellationToken, Task<T>> action, CancellationToken token)
	{
		using var connection = new SqlConnection(MyAppContext.Config.SQLConnectionString);
		await connection.OpenAsync(token);

		var result = await action(connection, token);

		await connection.CloseAsync();
		return result;
	}

	public static async Task<int> CountWellsAsync(string tb, CancellationToken token) =>
		await WrapConnection((sql, token) => sql.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {tb} WHERE {MyAppContext.Config.SQLFilterWellsQuery}", token), token);

	public static async Task<IEnumerable<SqlWell>> GetWellsAsync(string tb, int page, int pageSize, CancellationToken token) =>
		await WrapConnection<IEnumerable<SqlWell>>((sql, token) => 
		{
			var command = $@"
				SELECT * FROM {tb} WHERE {MyAppContext.Config.SQLFilterWellsQuery}
				ORDER BY ChosenID 
				OFFSET {page * pageSize} ROWS 
				FETCH NEXT {pageSize} ROWS ONLY";

			return sql.QueryAsync<SqlWell>(command, token);
		}, token);

	public static async Task<IEnumerable<Production>> GetProductionsAsync(string tb, string[] chosenIds, CancellationToken token) =>
		await WrapConnection<IEnumerable<Production>>((sql, _) => 
		{
			var command = $@"
				SELECT * 
				  FROM {tb} 
				WHERE ChosenId IN ({string.Join(",", chosenIds.Select(s => $"'{s}'"))})";

			return sql.QueryAsync<Production>(command);
		}, token);
}
