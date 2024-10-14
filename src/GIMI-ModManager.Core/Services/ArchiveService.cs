//ArchiveService.cs :
/* 包含解压压缩包、计算文件MD5哈希值、比较两个哈希值是否相等、
检查文件是否为压缩包、获取解压工具、提取压缩包中的文件、
使用7zip解压压缩包等方法。 */
using System.Diagnostics;
using System.Security.Cryptography;
using Serilog;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;

namespace GIMI_ModManager.Core.Services;

public class ArchiveService
{
    private readonly ILogger _logger;
    private readonly ExtractTool _extractTool;

    public ArchiveService(ILogger logger)
    {
        _logger = logger.ForContext<ArchiveService>();
        _extractTool = GetExtractTool();
    }

    // 解压压缩包
    public DirectoryInfo ExtractArchive(string archivePath, string destinationPath, bool overwritePath = false)
    {
        var archive = new FileInfo(archivePath);
        if (!archive.Exists)
            throw new FileNotFoundException("Archive not found", archivePath);

        if (!IsArchive(archivePath))
            throw new InvalidOperationException("File is not an archive");

        var destinationDirectory = Directory.CreateDirectory(destinationPath);

        var extractedFolder = Path.Combine(destinationDirectory.FullName, archive.Name);

        if (Directory.Exists(extractedFolder))
            throw new InvalidOperationException("Destination folder already exists, could not extract folder");

        Directory.CreateDirectory(extractedFolder);

        var extractor = Extractor(extractedFolder);

        extractor?.Invoke(archive.FullName, extractedFolder);

        return new DirectoryInfo(extractedFolder);
    }

    // 计算文件的MD5哈希值
    public async Task<string> CalculateFileMd5HashAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var file = new FileInfo(filePath);
        if (!file.Exists)
            throw new FileNotFoundException("File not found", filePath);


        using var md5 = MD5.Create();
        await using var stream = file.OpenRead();
        var hash = await md5.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(false);
        var convertedHash = BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
        return convertedHash;
    }

    // 比较两个哈希值是否相等
    public bool IsHashEqual(byte[] hash1, byte[] hash2)
    {
        return hash1.SequenceEqual(hash2);
    }

    // 检查文件是否为压缩包
    private bool IsArchive(string path)
    {
        return Path.GetExtension(path) switch
        {
            ".zip" => true,
            ".rar" => true,
            ".7z" => true,
            _ => false
        };
    }

    // 获取解压工具
    private Action<string, string>? Extractor(string archivePath)
    {
        Action<string, string>? action = null;

        if (_extractTool == ExtractTool.Bundled7Zip)
            action = Extract7Z;
        else if (_extractTool == ExtractTool.SharpCompress)
            action = Path.GetExtension(archivePath) switch
            {
                ".zip" => SharpExtract,
                ".rar" => SharpExtract,
                ".7z" => SharpExtract,
                _ => null
            };
        else if (_extractTool == ExtractTool.System7Zip) throw new NotImplementedException();

        return action;
    }

    // 提取压缩包中的文件
    private void ExtractEntries(IArchive archive, string extractPath)
    {
        _logger.Information("Extracting {ArchiveType} archive", archive.Type);
        foreach (var entry in archive.Entries)
        {
            _logger.Debug("Extracting {EntryName}", entry.Key);
            entry.WriteToDirectory(extractPath, new ExtractionOptions()
            {
                ExtractFullPath = true,
                Overwrite = true,
                PreserveFileTime = false
            });
        }
    }

    // 使用SharpCompress库解压压缩包
    private void SharpExtract(string archivePath, string extractPath)
    {
        using var archive = ZipArchive.Open(archivePath);
        ExtractEntries(archive, extractPath);
    }


    private enum ExtractTool
    {
        Bundled7Zip, // 7zip bundled with JASM
        SharpCompress, // SharpCompress library
        System7Zip // 7zip installed on the system
    }

    // 获取解压工具
    private ExtractTool GetExtractTool()
    {
        var bundled7ZFolder = Path.Combine(AppContext.BaseDirectory, @"Assets\7z\");
        if (File.Exists(Path.Combine(bundled7ZFolder, "7z.exe")) &&
            File.Exists(Path.Combine(bundled7ZFolder, "7-zip.dll")) &&
            File.Exists(Path.Combine(bundled7ZFolder, "7z.dll")))
        {
            _logger.Debug("Using bundled 7zip");
            return ExtractTool.Bundled7Zip;
        }

        _logger.Information("Bundled 7zip not found, using SharpCompress library");
        return ExtractTool.SharpCompress;
    }


    // 使用7zip解压压缩包
    private void Extract7Z(string archivePath, string extractPath)
    {
        var sevenZipPath = Path.Combine(AppContext.BaseDirectory, @"Assets\7z\7z.exe");
        var process = new Process
        {
            StartInfo =
            {
                FileName = sevenZipPath,
                Arguments = $"x \"{archivePath}\" -o\"{extractPath}\" -y",
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        _logger.Information("Extracting 7z archive with command: {Command}", process.StartInfo.Arguments);
        process.Start();
        process.WaitForExit();
        _logger.Information("7z extraction finished with exit code {ExitCode}", process.ExitCode);
    }
}
