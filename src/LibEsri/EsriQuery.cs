using System.Text.Json.Nodes;
using System.Web;

namespace LibEsri;

public enum EsriUnit
{
	Foot,
	Meter,
	StatuteMile,
	Kilometer,
	NauticalMile,
	USNauticalMile
}

public enum EsriGeometryType
{
	Point,
	Multipoint,
	Polyline,
	Polygon,
	Envelope
}

public enum EsrieSriSpatialRel
{
	Intersects,
	Contains,
	Crosses,
	EnvelopeIntersects,
	IndexIntersects,
	Overlaps,
	Touches,
	Within,
	Relation,
}

public enum EsriTimeRelation
{
	TimeRelationAfterStartOverlapsEnd,
	TimeRelationOverlaps,
	TimeRelationOverlapsStartWithinEnd,
	TimeRelationWithin
}

public enum EsriSqlFormat
{
	None,
	Standard,
	Native
}

public enum EsriStatistics
{
	GroupByFieldsForStatisticstext,
	OrderByFields,
	Text,
	Time,
	Where,
	Geometry,
	GdbVersion,
	Percentile,
	StatisticType
}

public enum OutputFormat
{
	Json,
	PrettyJson,
	Html
}

public class EsriQuery
{
	public string? Where { get; set; }
	public string? Text { get; set; }
	public List<long>? ObjectIds { get; set; }
	public string? Time { get; set; }
	public EsriTimeRelation? TimeRelation { get; set; }
	public JsonObject? Geometry { get; set; }
	public EsriGeometryType? GeometryType { get; set; }
	public JsonObject? InSR {  get; set; }
	public EsrieSriSpatialRel? SpatialRel {  get; set; }
	public double? Distance { get; set; }
	public EsriUnit? Units { get; set; }
	public string? RelationParam { get; set; }
	public List<string>? OutFields { get; set; }
	public bool ReturnGeometry { get; set; }
	public bool ReturnTrueCurves { get; set; }
	public double? MaxAllowableOffset { get; set; }
	public int? GeometryPrecision { get; set; }
	public JsonObject? OutSR { get; set; }
	public string? HavingClause { get; set; }
	public bool ReturnIdsOnly { get; set; }
	public bool ReturnCountOnly { get; set; }
	public string? OrderByFields { get; set; }
	public string? GroupByFieldsForStatistics { get; set; }
	public EsriStatistics? OutStatistics { get; set; }
	public bool ReturnZ { get; set; }
	public bool ReturnM { get; set; }
	public string? GdbVersion { get; set; }
	public string? HistoricMoment { get; set; }
	public bool ReturnDistinctValues { get; set; }
	public int? ResultOffset { get; set; }
	public int? ResultRecordCount { get; set; }
	public bool ReturnExtentOnly { get; set; }
	public EsriSqlFormat SqlFormat { get; set; }
	public string? DatumTransformation { get; set; }
	public string? ParameterValues { get; set; }
	public string? RangeValues { get; set; }
	public string? QuantizationParameters { get; set; }
	public string FeatureEncoding { get; set; } = "esriDefault";
	public OutputFormat OutputFormat { get; set; }

	public FormUrlEncodedContent ToFormContent() => new(GetParameters());

	public string ToQueryString()
		=> string.Join("&", GetParameters().Select(kvp => $"{kvp.Key}={HttpUtility.UrlEncode(kvp.Value)}"));

	public Dictionary<string, string> GetParameters() => new()
	{
		{ "where", Where ?? string.Empty },
		{ "text", Text ?? string.Empty },
		{ "objectIds", ObjectIds is null ? string.Empty : string.Join(",", ObjectIds) },
		{ "time", Time ?? string.Empty },
		{ "timeRelation", GetTimeRelationString() },
		{ "geometry", Geometry?.ToJsonString() ?? string.Empty },
		{ "geometryType", GetGeometryTypeString() },
		{ "inSR", InSR?.ToJsonString() ?? string.Empty },
		{ "spatialRel", GetSpatialRelString() },
		{ "distance", Distance?.ToString() ?? string.Empty },
		{ "units", GetUnitsString() },
		{ "relationParam", RelationParam ?? string.Empty },
		{ "outFields", OutFields is null ? string.Empty : string.Join(",", OutFields) },
		{ "returnGeometry", ReturnGeometry ? "true" : "false" },
		{ "returnTrueCurves", ReturnTrueCurves ? "true" : "false" },
		{ "maxAllowableOffset", MaxAllowableOffset?.ToString() ?? string.Empty },
		{ "geometryPrecision", GeometryPrecision?.ToString() ?? string.Empty },
		{ "outSR", OutSR?.ToJsonString() ?? string.Empty },
		{ "havingClause", HavingClause ?? string.Empty },
		{ "returnIdsOnly", ReturnIdsOnly ? "true" : "false" },
		{ "returnCountOnly", ReturnCountOnly ? "true" : "false" },
		{ "orderByFields", OrderByFields ?? string.Empty },
		{ "groupByFieldsForStatistics", GroupByFieldsForStatistics ?? string.Empty },
		{ "outStatistics", GetEsriStatisticsString() },
		{ "returnZ", ReturnZ ? "true" : "false" },
		{ "returnM", ReturnM ? "true" : "false" },
		{ "gdbVersion", GdbVersion ?? string.Empty },
		{ "historicMoment", HistoricMoment ?? string.Empty },
		{ "returnDistinctValues", ReturnDistinctValues ? "true" : "false" },
		{ "resultOffset", ResultOffset?.ToString() ?? string.Empty },
		{ "resultRecordCount", ResultRecordCount?.ToString() ?? string.Empty },
		{ "returnExtentOnly", ReturnExtentOnly ? "true" : "false" },
		{ "sqlFormat", GetEsriSqlFormatString() },
		{ "datumTransformation", DatumTransformation ?? string.Empty },
		{ "parameterValues", ParameterValues ?? string.Empty },
		{ "rangeValues", RangeValues ?? string.Empty },
		{ "quantizationParameters", QuantizationParameters ?? string.Empty },
		{ "featureEncoding", FeatureEncoding ?? string.Empty },
		{ "f", GetOutputFormatString() }
	};

	private string GetOutputFormatString() => OutputFormat switch
	{
		OutputFormat.Json => "json",
		OutputFormat.PrettyJson => "pjson",
		OutputFormat.Html => "html",
		_ => string.Empty
	};
	private string GetEsriSqlFormatString() => SqlFormat switch
	{
		EsriSqlFormat.None => "none",
		EsriSqlFormat.Standard => "standard",
		EsriSqlFormat.Native => "native",
		_ => string.Empty
	};
	private string GetEsriStatisticsString() => OutStatistics switch
	{
		EsriStatistics.GroupByFieldsForStatisticstext => "groupByFieldsForStatisticstext",
		EsriStatistics.OrderByFields => "orderByFields",
		EsriStatistics.Text => "text",
		EsriStatistics.Time => "time",
		EsriStatistics.Where => "where",
		EsriStatistics.Geometry => "geometry",
		EsriStatistics.GdbVersion => "gdbVersion",
		EsriStatistics.Percentile => "percentile",
		EsriStatistics.StatisticType => "statisticType",
		_ => string.Empty
	};
	private string GetSpatialRelString() => SpatialRel switch
	{
		EsrieSriSpatialRel.Intersects => "esriSpatialRelIntersects",
		EsrieSriSpatialRel.Contains => "esriSpatialRelContains",
		EsrieSriSpatialRel.Crosses => "esriSpatialRelCrosses",
		EsrieSriSpatialRel.EnvelopeIntersects => "esriSpatialRelEnvelopeIntersects",
		EsrieSriSpatialRel.IndexIntersects => "esriSpatialRelIndexIntersects",
		EsrieSriSpatialRel.Overlaps => "esriSpatialRelOverlaps",
		EsrieSriSpatialRel.Touches => "esriSpatialRelTouches",
		EsrieSriSpatialRel.Within => "esriSpatialRelWithin",
		EsrieSriSpatialRel.Relation => "esriSpatialRelRelation",
		_ => string.Empty
	};
	private string GetGeometryTypeString() => GeometryType switch
	{
		EsriGeometryType.Point => "esriGeometryPoint ",
		EsriGeometryType.Multipoint => "esriGeometryMultipoint",
		EsriGeometryType.Polyline => "esriGeometryPolyline",
		EsriGeometryType.Polygon => "esriGeometryPolygon",
		EsriGeometryType.Envelope => "esriGeometryEnvelope",
		_ => string.Empty
	};
	private string GetUnitsString() => Units switch
	{
		EsriUnit.Foot => "esriSRUnit_Foot",
		EsriUnit.Meter => "esriSRUnit_Meter",
		EsriUnit.StatuteMile => "esriSRUnit_StatuteMile",
		EsriUnit.Kilometer => "esriSRUnit_Kilometer",
		EsriUnit.NauticalMile => "esriSRUnit_NauticalMile",
		EsriUnit.USNauticalMile => "esriSRUnit_USNauticalMile",
		_ => string.Empty
	};
	private string GetTimeRelationString() => TimeRelation switch
	{
		EsriTimeRelation.TimeRelationAfterStartOverlapsEnd => "esriTimeRelationAfterStartOverlapsEnd",
		EsriTimeRelation.TimeRelationOverlaps => "esriTimeRelationOverlaps",
		EsriTimeRelation.TimeRelationOverlapsStartWithinEnd => "esriTimeRelationOverlapsStartWithinEnd",
		EsriTimeRelation.TimeRelationWithin => "esriTimeRelationWithin",
		_ => string.Empty
	};
}
