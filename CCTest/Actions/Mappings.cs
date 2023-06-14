using CCTest.Models;
using ComboCurve.Api.Model;

namespace CCTest.Actions;

public static class Mappings
{
	public static WellInput ToApiWell(SqlWell well) =>
		new 
		(
			dataSource: "other",
			chosenID: well.ChosenID,
			_abstract: well.Abstract,
			api10: well.Api10,
			api12: well.Api12,
			api14: well.Api14,
			basin: well.Basin,
			block: well.Block,
			completionStartDate: well.CompletionStartDate,
			country: well.Country,
			county: well.County,
			currentOperator: well.CurrentOperator,
			currentOperatorAlias: well.currentOperatorAlias,
			currentOperatorCode: well.CurrentOperatorCode,
			currentOperatorTicker: well.CurrentOperatorTicker,
			dateRigRelease: well.DateRigRelease,
			district: well.District,
			drillStartDate: well.DrillStartDate,
			elevation: well.Elevation,
			field: well.Field,
			grossPerforatedInterval: well.grossPerforatedInterval,
			holeDirection: well.holeDirection,
			ihsId: well.IhsId,
			lateralLength: well.LateralLength,
			leaseName: well.LeaseName,
			leaseNumber: well.LeaseNumber,
			measuredDepth: well.measuredDepth,
			permitDate: well.PermitDate,
			play: well.Play,
			previousOperator: well.PreviousOperator,
			previousOperatorCode: well.previousOperatorCode,
			range: well.Range,
			section: well.Section,
			spudDate: well.spudDate,
			subplay: well.Subplay,
			surfaceLatitude: well.SurfaceLatitude,
			surfaceLongitude: well.SurfaceLongitude,
			survey: well.Survey,
			drillEndDate: well.DrillEndDate,
			targetFormation: well.TargetFormation,
			toeLatitude: well.ToeLatitude,
			toeLongitude: well.ToeLongitude,
			township: well.Township,
			trueVerticalDepth: well.TrueVerticalDepth,
			wellName: well.WellName,
			wellNumber: well.WellNumber,
			wellType: well.WellType,
			completionDesign: well.completionDesign,
			firstPropWeight: well.firstPropWeight,
			firstFluidVolume: well.firstFluidVolume
		);

	public static MonthlyProductionInput ToApi(Production prod) =>
		new 
		(
			dataSource: "other",
			chosenID: prod.ChosenID,
			date: prod.DATE,
			gas: prod.Gas,
			oil: prod.Oil,
			water: prod.Water,
			daysOn: prod.DaysOn
		);

    public static MonthlyProductionInput ToApiUpdate(Production prod) =>
        new
        (
            dataSource: "other",
            chosenID: prod.ChosenID,
            date: prod.DATE,
            gas: prod.Water,
            oil: prod.Gas,
            water: prod.Oil,
            daysOn: prod.DaysOn
        );
}
