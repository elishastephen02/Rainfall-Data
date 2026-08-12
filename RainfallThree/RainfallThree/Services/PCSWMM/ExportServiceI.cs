using RainfallThree.Models.PCSWMM;

namespace RainfallThree.Services.PCSWMM
{
    public interface ExportServiceI
    {
       Task<string> GenerateAsync(
       ExportRequest request,
       IProgress<ExportProgress>? progress = null);
    }
}
