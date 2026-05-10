using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TileTool.Models;

namespace TileTool.Services;

public interface IConfigService
{
    Task<TileToolConfig> LoadAsync(string outputFolder, CancellationToken cancellationToken = default);
    Task SaveAsync(string outputFolder, TileToolConfig config, CancellationToken cancellationToken = default);
    string GetConfigPath(string outputFolder);
}

public sealed class ConfigService : IConfigService
{
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    private const string ConfigFileName = "tiletool.json";

    public string GetConfigPath(string outputFolder) => Path.Combine(outputFolder, ConfigFileName);

    public async Task<TileToolConfig> LoadAsync(string outputFolder, CancellationToken cancellationToken = default)
    {
        var configPath = GetConfigPath(outputFolder);
        if (!File.Exists(configPath))
        {
            return new TileToolConfig { OutputFolder = outputFolder };
        }

        try
        {
            await using var stream = File.OpenRead(configPath);
            var config = await JsonSerializer.DeserializeAsync<TileToolConfig>(stream, _jsonOptions, cancellationToken);
            if (config == null)
                throw new InvalidDataException("Configuration file is empty.");

            config.OutputFolder = outputFolder;
            return config;
        }
        catch (JsonException ex)
        {
            Trace.TraceError("event=config_load_failed reason=json_error path={0} message={1}", configPath, ex.Message);
            throw new InvalidDataException("Configuration file is not valid JSON.", ex);
        }
        catch (IOException ex)
        {
            Trace.TraceError("event=config_load_failed reason=io_error path={0} message={1}", configPath, ex.Message);
            throw;
        }
    }

    public async Task SaveAsync(string outputFolder, TileToolConfig config, CancellationToken cancellationToken = default)
    {
        var configPath = GetConfigPath(outputFolder);
        Directory.CreateDirectory(outputFolder);

        await using var stream = new FileStream(
            configPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 8192,
            useAsync: true);

        await JsonSerializer.SerializeAsync(stream, config, _jsonOptions, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }
}
