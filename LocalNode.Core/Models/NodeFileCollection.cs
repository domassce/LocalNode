using System.Collections;
using LocalNode.Core.Interfaces;

namespace LocalNode.Core.Models;

//REIKALAVIMAS
public class NodeFileCollection<T> : IEnumerable<T> where T : IFileEntity
{
    private readonly List<T> _items = new();

    public void Add(T item) => _items.Add(item);

    //REIKALAVIMAS
    public IEnumerable<T> GetLargeFiles(long minSizeBytes)
    {
        foreach (var item in _items)
        {
            if (item.Size > minSizeBytes)
            {
                yield return item;
            }
        }
    }

    public IEnumerator<T> GetEnumerator() => new NodeEnumerator(_items);
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    //REIKALAVIMAS
    private class NodeEnumerator : IEnumerator<T>
    {
        private readonly List<T> _list;
        private int _index = -1;

        public NodeEnumerator(List<T> list) => _list = list;

        public T Current => _list[_index];
        object IEnumerator.Current => Current;

        public bool MoveNext() => ++_index < _list.Count;
        public void Reset() => _index = -1;
        public void Dispose() { }
    }
}