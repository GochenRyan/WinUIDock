
using Dock.Model.Core;
using System.Windows.Input;

namespace Dock.Model.Controls;

/// <summary>
/// Document dock contract.
/// </summary>
public interface IDocumentDock : IDock
{
    /// <summary>
    /// Gets or sets if document dock can create new documents.
    /// </summary>
    bool CanCreateDocument { get; set; }

    /// <summary>
    /// Gets or sets command to create new document.
    /// </summary>
    ICommand? CreateDocument { get; set; }
}
