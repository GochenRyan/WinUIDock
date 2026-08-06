using Dock.Model;
using Dock.Model.Controls;
using Dock.Model.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Text;

namespace Dock.Serializer;

/// <summary>
/// A class that implements the <see cref="IDockSerializer"/> interface using JSON serialization.
/// </summary>
public sealed class DockSerializer : IDockSerializer
{
    private readonly JsonSerializerSettings _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="DockSerializer"/> class with the specified list type.
    /// </summary>
    /// <param name="listType">The type of list to use in the serialization process.</param>
    public DockSerializer(Type listType)
    {
        _settings = new JsonSerializerSettings()
        {
            Formatting = Formatting.Indented,
            TypeNameHandling = TypeNameHandling.Objects,
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            ContractResolver = new ListContractResolver(listType),
            NullValueHandling = NullValueHandling.Ignore,
            Converters =
            {
                new KeyValuePairConverter()
            }
        };
    }

    /// <inheritdoc/>
    public string Serialize<T>(T value)
    {
        return JsonConvert.SerializeObject(value, _settings);
    }

    /// <inheritdoc/>
    public T? Deserialize<T>(string text)
    {
        return JsonConvert.DeserializeObject<T>(text, _settings);
    }

    /// <inheritdoc/>
    public T? Load<T>(Stream stream)
    {
        using var streamReader = new StreamReader(stream, Encoding.UTF8);
        var text = streamReader.ReadToEnd();
        return Deserialize<T>(text);
    }

    /// <inheritdoc/>
    public T? Load<T>(Stream stream, DockableResolver? resolver)
    {
        var result = Load<T>(stream);

        if (resolver is null || result is not IDockable dockable)
        {
            return result;
        }

        return DockableResolution.Apply(dockable, resolver) is T resolved ? resolved : default;
    }

    /// <inheritdoc/>
    public void Save<T>(Stream stream, T value)
    {
        // A live layout's float geometry sits in the host's position cache between
        // layout ticks; flush it into the model so the file gets current positions.
        // No-op per window when it has no host (headless snapshots).
        if (value is IRootDock root && root.Windows is { } windows)
        {
            foreach (var window in windows)
            {
                window.Save();
            }
        }

        var text = Serialize(value);
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        // Callers hand in streams over EXISTING files (file pickers do not truncate
        // on overwrite). Shorter content over a longer file would leave the old
        // tail behind — unreadable JSON on the next load.
        if (stream.CanSeek)
        {
            stream.SetLength(0);
        }

        using var streamWriter = new StreamWriter(stream, Encoding.UTF8);
        streamWriter.Write(text);
    }
}
