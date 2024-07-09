using CommunityToolkit.WinUI.UI.Converters;
using System.Collections;

namespace Dock.WinUI3.Converters
{
    public class EmptyCollectionToObjectConverter : EmptyObjectToObjectConverter
    {
        /// <summary>
        /// Checks collection for emptiness.
        /// </summary>
        /// <param name="value">Value to be checked.</param>
        /// <returns>True if value is an empty collection or does not implement IEnumerable, false otherwise.</returns>
        protected override bool CheckValueIsEmpty(object value)
        {
            bool isEmpty = true;
            var collection = value as IEnumerable;
            if (collection != null)
            {
                var enumerator = collection.GetEnumerator();
                isEmpty = !enumerator.MoveNext();
            }

            return isEmpty;
        }
    }
}
