namespace CCTest.Models;

public record WellID(string ChosenID, string wellID);

public class SqlWell
{
	public string ChosenID { get; set; }
	public string DataSource { get; set; }
	public string Abstract { get; set; }
	public string Api10 { get; set; }
	public string Api12 { get; set; }
	public string Api14 { get; set; }
	public string Basin { get; set; }
	public string Block { get; set; }
	public DateTime CompletionStartDate { get; set; }
	public string Country { get; set; }
	public string County { get; set; }
	public string CurrentOperator { get; set; }
	public string currentOperatorAlias { get; set; }
	public string CurrentOperatorCode { get; set; }
	public string CurrentOperatorTicker { get; set; }
	public DateTime DateRigRelease { get; set; }
	public string District { get; set; }
	public DateTime DrillStartDate { get; set; }
	public decimal Elevation { get; set; }
	public string Field { get; set; }
	public decimal grossPerforatedInterval { get; set; }
	public string holeDirection { get; set; }
	public string IhsId { get; set; }
	public decimal LateralLength { get; set; }
	public string LeaseName { get; set; }
	public string LeaseNumber { get; set; }
	public decimal measuredDepth { get; set; }
	public DateTime PermitDate { get; set; }
	public string Play { get; set; }
	public string PreviousOperator { get; set; }
	public string previousOperatorCode { get; set; }
	public string Range { get; set; }
	public string Section { get; set; }
	public DateTime spudDate { get; set; }
	public string STATE { get; set; }
	public string STATUS { get; set; }
	public string Subplay { get; set; }
	public decimal SurfaceLatitude { get; set; }
	public decimal SurfaceLongitude { get; set; }
	public string Survey { get; set; }
	public DateTime DrillEndDate { get; set; }
	public string TargetFormation { get; set; }
	public decimal ToeLatitude { get; set; }
	public decimal ToeLongitude { get; set; }
	public string Township { get; set; }
	public decimal TrueVerticalDepth { get; set; }
	public string WellName { get; set; }
	public string WellNumber { get; set; }
	public string WellType { get; set; }
	public DateTime UpdatedAt { get; set; }
	public string completionDesign { get; set; }
	public decimal firstPropWeight { get; set; }
	public decimal firstFluidVolume { get; set; }
	public decimal perflaterallength { get; set; }
	public string landingZone { get; set; }
	public decimal upperPerforation { get; set; }
	public decimal lowerPerforation { get; set; }
	public decimal firstStageCount { get; set; }
	public DateTime firstProdDate { get; set; }
	public string AllocationType { get; set; }
	public string PrimaryProduct { get; set; }
}
