namespace Dock.Model.Core;
/// <summary>
/// Docking serializer contract.
/// </summary>
public interface IDockSerializer
{
    /// <summary>
    /// Serializes the specified object to a string.
    /// </summary>
    /// <param name="value">The object to serialize.</param>
    /// <typeparam name="T">The type of the object to serialize.</typeparam>
    /// <returns>A string representation of the serialized object.</returns>
    string Serialize<T>(T value);

    /// <summary>
    /// Deserializes the specified string to an object.
    /// </summary>
    /// <param name="text">The string to deserialize.</param>
    /// <typeparam name="T">The type of the object to deserialize.</typeparam>
    /// <returns>The deserialized object, or null if the deserialization fails.</returns>
    T? Deserialize<T>(string text);

    /// <summary>
    /// Loads an object from the specified stream.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <typeparam name="T">The type of the object to load.</typeparam>
    /// <returns>The loaded object, or null if the deserialization fails.</returns>
    T? Load<T>(Stream stream);

    /// <summary>
    /// Loads a layout and runs <paramref name="resolver"/> over every dockable in
    /// it, so the host can adopt its own live objects (restoring the
    /// <c>[JsonIgnore]</c> panel content and keeping the references it holds valid)
    /// or drop nodes the current build no longer knows about.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="resolver">The resolution callback; null behaves like <see cref="Load{T}(Stream)"/>.</param>
    /// <typeparam name="T">The type of the object to load.</typeparam>
    /// <returns>The loaded and resolved object, or null.</returns>
    T? Load<T>(Stream stream, DockableResolver? resolver);

    /// <summary>
    /// Saves the specified object to the specified stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="value">The object to save.</param>
    /// <typeparam name="T">The type of the object to save.</typeparam>
    void Save<T>(Stream stream, T value);
}
