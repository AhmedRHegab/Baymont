namespace Baymont.Application.Dtos;

public record ImportRowIssueDto(int Row, string Message);

public record ImportResultDto(
    int TotalRows,
    int NewOwners,
    int NewUnits,
    int SkippedExisting,
    IReadOnlyList<ImportRowIssueDto> Errors,
    bool Committed);
