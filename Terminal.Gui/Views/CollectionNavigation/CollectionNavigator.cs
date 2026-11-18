#nullable disable
using System.Collections;

namespace Terminal.Gui.Views;

/// <inheritdoc cref="CollectionNavigatorBase"/>
/// <remarks>This implementation is based on a static <see cref="Collection"/> of objects.</remarks>
internal class CollectionNavigator : CollectionNavigatorBase, IListCollectionNavigator
{
    private readonly object _collectionLock = new ();
    private IList _collection;

    /// <summary>Constructs a new CollectionNavigator.</summary>
    public CollectionNavigator () { }

    /// <summary>Constructs a new CollectionNavigator for the given collection.</summary>
    /// <param name="collection"></param>
    public CollectionNavigator (IList collection) { Collection = collection; }

    /// <inheritdoc/>
    public IList Collection
    {
        get
        {
            lock (_collectionLock)
            {
                return _collection;
            }
        }
        set
        {
            lock (_collectionLock)
            {
                _collection = value;
            }
        }
    }

    /// <inheritdoc/>
    protected override object ElementAt (int idx)
    {
        lock (_collectionLock)
        {
            return Collection [idx];
        }
    }

    /// <inheritdoc/>
    protected override int GetCollectionLength ()
    {
        lock (_collectionLock)
        {
            return Collection.Count;
        }
    }
}
