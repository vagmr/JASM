using System.Diagnostics;
using GIMI_ModManager.Core.Contracts.Entities;
using GIMI_ModManager.Core.Entities;
using Serilog;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;

namespace GIMI_ModManager.Core.Services;

// 拖拽扫描器类，用于处理拖拽进来的文件或文件夹
public sealed class DragAndDropScanner
{
    private readonly ILogger _logger = Log.ForContext<DragAndDropScanner>();
    private readonly string _tmpFolder = Path.Combine(Path.GetTempPath(), "JASM_TMP"); // 临时文件夹路径

    // 工作文件夹路径，用于存放解压后的文件
    private string _workFolder = Path.Combine(Path.GetTempPath(), "JASM_TMP", Guid.NewGuid().ToString("N"));

    private ExtractTool _extractTool; // 解压工具枚举

    // 构造函数，初始化解压工具
    public DragAndDropScanner()
    {
        _extractTool = GetExtractTool();
    }

    // 扫描并获取内容
    public async Task<DragAndDropScanResult> ScanAndGetContentsAsync(string path, Func<Task<string>> passwordPrompt)
    {
        PrepareWorkFolder(); // 准备工作文件夹

        _workFolder = Path.Combine(_workFolder, Path.GetFileName(path)); // 设置工作文件夹路径

        if (IsArchive(path)) // 如果是压缩文件
        {
            var copiedArchive = new FileInfo(path); // 获取文件信息
            copiedArchive = copiedArchive.CopyTo(Path.Combine(_tmpFolder, Path.GetFileName(path)), true); // 复制到临时文件夹

            var result = await ExtractorAsync(copiedArchive.FullName, passwordPrompt); // 获取解压方法

            if (result != null)
            {
                await result.Invoke(copiedArchive.FullName); // 执行解压
            }
        }
        else if (Directory.Exists(path)) // 如果是文件夹
        {
            var modFolder = new Mod(new DirectoryInfo(path)); // 创建Mod对象
            modFolder.CopyTo(_workFolder); // 复制到工作文件夹
        }
        else
            throw new Exception("No valid mod folder or archive found"); // 如果不是有效的文件夹或压缩文件，抛出异常

        return new DragAndDropScanResult() // 返回扫描结果
        {
            ExtractedFolder = new Mod(new DirectoryInfo(_workFolder).Parent!) // 设置解压后的文件夹
        };
    }

    // 准备工作文件夹
    private void PrepareWorkFolder()
    {
        Directory.CreateDirectory(_tmpFolder); // 创建临时文件夹
        Directory.CreateDirectory(_workFolder); // 创建工作文件夹
    }

    // 判断是否是压缩文件
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

    // 获取解压方法
    private async Task<Func<string, Task>?> ExtractorAsync(string path, Func<Task<string>> passwordPrompt)
    {
        Func<string, Task>? action = null;

        if (_extractTool == ExtractTool.Bundled7Zip) // 如果使用内置7zip
            action = async (p) => await Extract7ZAsync(p, passwordPrompt);
        else if (_extractTool == ExtractTool.SharpCompress) // 如果使用SharpCompress库
            action = Path.GetExtension(path) switch
            {
                ".zip" => async (p) => await SharpExtractZipAsync(p, passwordPrompt),
                ".rar" => async (p) => await SharpExtractRarAsync(p, passwordPrompt),
                ".7z" => async (p) => await SharpExtract7zAsync(p, passwordPrompt),
                _ => null
            };
        // 如果使用系统7zip，抛出未实现异常
        else if (_extractTool == ExtractTool.System7Zip) throw new NotImplementedException();

        return action;
    }

    // 解压条目
    private void ExtractEntries(IArchive archive)
    {
        _logger.Information("Extracting {ArchiveType} archive", archive.Type); // 记录日志
        foreach (var entry in archive.Entries) // 遍历压缩文件条目
        {
            _logger.Debug("Extracting {EntryName}", entry.Key); // 记录日志
            entry.WriteToDirectory(_workFolder, new ExtractionOptions() // 解压到工作文件夹
            {
                ExtractFullPath = true,
                Overwrite = true,
                PreserveFileTime = false
            });
        }
    }

    // 使用SharpCompress库解压Zip文件
    private void SharpExtractZip(string path)
    {
        using var archive = ZipArchive.Open(path);
        ExtractEntries(archive);
    }

    // 使用SharpCompress库解压Rar文件
    private void SharpExtractRar(string path)
    {
        using var archive = RarArchive.Open(path);
        ExtractEntries(archive);
    }

    // 使用SharpCompress库解压7z文件
    private void SharpExtract7z(string path)
    {
        using var archive = ArchiveFactory.Open(path);
        ExtractEntries(archive);
    }

    // 解压工具枚举
    private enum ExtractTool
    {
        Bundled7Zip, // 内置7zip
        SharpCompress, // SharpCompress库
        System7Zip // 系统7zip
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

    // 使用内置7zip解压文件
    private async Task Extract7ZAsync(string path, Func<Task<string>> passwordPrompt)
    {
        var sevenZipPath = Path.Combine(AppContext.BaseDirectory, @"Assets\7z\7z.exe");
        var process = new Process
        {
            StartInfo =
            {
                FileName = sevenZipPath,
                Arguments = $"x \"{path}\" -o\"{_workFolder}\" -y",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            }
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (output.Contains("Enter password") || error.Contains("Wrong password"))
        {
            var password = await passwordPrompt();
            if (string.IsNullOrEmpty(password))
                throw new OperationCanceledException("Password input was cancelled.");

            process.StartInfo.Arguments = $"x \"{path}\" -o\"{_workFolder}\" -y -p{password}";
            process.Start();
            await process.WaitForExitAsync();
        }

        _logger.Information("7z extraction finished with exit code {ExitCode}", process.ExitCode);
    }

}

// 拖拽扫描结果类
public class DragAndDropScanResult
{
    public IMod ExtractedFolder { get; init; } = null!; // 解压后的文件夹
    public string[] IgnoredMods { get; init; } = Array.Empty<string>(); // 被忽略的Mod列表
}
