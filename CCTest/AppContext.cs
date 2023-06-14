namespace CCTest;

public static class MyAppContext
{
	public static Configuration Config { get; set; }

	public class Configuration
	{
		// SQL
		public string SQLConnectionString { get; set; } = string.Empty;
		public string SQLFilterWellsQuery { get; set; } = 
			"Basin = 'PERMIAN BASIN' AND holeDirection = 'HORIZONTAL'";

		// API
		public string ApiKey { get; set; } = string.Empty;
		public string ClientEmail { get; set; } = string.Empty;
		public string ClientID { get; set; } = string.Empty;
		public string PrivateKey { get; set; } = string.Empty;
		public string ApiUrl { get; set; } = string.Empty;
	}
}
