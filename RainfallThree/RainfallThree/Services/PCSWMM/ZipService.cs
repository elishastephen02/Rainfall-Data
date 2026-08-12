using System.IO.Compression;

namespace RainfallThree.Services.PCSWMM
{
    public class ZipService
    {
        public byte[] CreateZip(string folder)
        {
            string zipPath = Path.Combine(
                Path.GetTempPath(),
                $"Generated_PCSWMM_{Guid.NewGuid()}.zip");

            if (File.Exists(zipPath))
                File.Delete(zipPath);

            ZipFile.CreateFromDirectory(folder, zipPath);

            byte[] bytes = File.ReadAllBytes(zipPath);

            File.Delete(zipPath);

            return bytes;
        }
    }
}
