namespace Baymont.Application.Dtos;

public record DeliveryDto(
    Guid Id,
    DateOnly DeliveryDate,
    Guid? UnitId,
    string UnitNumber,
    string CustomerName,
    string HandoverEngineerName,
    bool IsReceived,
    string? NotReceivedReason,
    bool IsFurnished,
    string? Comments,
    string? Complaints,
    uint RowVersion);

public record SaveDeliveryRequest(
    DateOnly DeliveryDate,
    Guid? UnitId,
    string UnitNumber,
    string CustomerName,
    string HandoverEngineerName,
    bool IsReceived,
    string? NotReceivedReason,
    bool IsFurnished,
    string? Comments,
    string? Complaints,
    uint RowVersion);

public record DeliveryListDto(
    IReadOnlyList<DeliveryDto> Items,
    int Total,
    int Received,
    int NotReceived,
    int Furnished);
