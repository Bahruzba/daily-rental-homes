using DailyRentalHomes.Domain.Enums;

namespace DailyRentalHomes.Api.Contracts.MediaFiles;

public sealed class NewMediaFileRequest
{
    public long? RentalHomeId { get; set; }
    public long? BookingDepositId { get; set; }
    public MediaFileType FileType { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long? SizeBytes { get; set; }
    public int SortOrder { get; set; }
}
