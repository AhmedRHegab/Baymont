namespace Baymont.Application.Dtos;

public record SearchResponse(
    string Query,
    IReadOnlyList<SearchHitDto> Hits);

public record SearchHitDto(
    string Type,
    Guid Id,
    string Title,
    string? Subtitle,
    int Rank);
