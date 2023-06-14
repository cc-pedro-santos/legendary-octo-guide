using ComboCurve.Api.Model;

namespace CCTest.Actions
{
    internal static class FileActions
    {
        public static void SaveErrorLogs(string pathFile, WellMultiStatusResponse response)
        {
            if (response.Results is not null
            &&  response.Results.Any(a => a.Errors is not null && a.Errors.Any()))
            {
                var errors = response.Results.Where(w => w.Errors is not null && w.Errors.Any())
                                      .SelectMany(s =>
                                      {
                                          var msg = new List<string>
                                          {
                                              "------",
                                              $"Chosen: {s.ChosenID}, ID: {s.Id}",
                                          };

                                          msg.AddRange(s.Errors.Select(s => s.Message));
                                          msg.Add("------");
                                          return msg;
                                      });

                File.AppendAllLines(pathFile, errors);
            }
        }

        public static void SaveErrorLogs(string pathFile, ProductionMultiStatusResponse response)
        {
            if (response.Results is not null
            && response.Results.Any(a => a.Errors is not null && a.Errors.Any()))
            {
                var errors = response.Results.Where(w => w.Errors is not null && w.Errors.Any())
                                      .SelectMany(s =>
                                      {
                                          var msg = new List<string>
                                          {
                                              "------",
                                              $"Well: {s.Well}",
                                          };

                                          msg.AddRange(s.Errors.Select(s => s.Message));
                                          msg.Add("------");
                                          return msg;
                                      });

                File.AppendAllLines(pathFile, errors);
            }
        }
    }
}
