namespace server.Models.Enums;

public enum CalculationPeriod
{
    Monthly = 1,
    Quarterly = 2,
    Yearly = 3
}

public enum MetricSourceType
{
    Excel = 1,
    Manual = 2,
    ExternalApi = 3
}

public enum FieldDataType
{
    String = 1,
    Number = 2,
    Date = 3,
    Boolean = 4
}

public enum ImportStatus
{
    Pending = 1,
    Processing = 2,
    Success = 3,
    PartialSuccess = 4,
    Failed = 5
}
