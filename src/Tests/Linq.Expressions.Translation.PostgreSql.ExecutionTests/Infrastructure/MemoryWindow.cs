namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests.Infrastructure;

// The window of one row as PostgreSQL computes it, for the in-memory side of the
// differential tests: the sorted partition, the row's position in it, its peers
// (rows equal on every ORDER BY key) and the rows of its frame.
public sealed class MemoryWindow<T>(
    IReadOnlyList<T> partition,
    int index,
    int[] peerStart,
    int[] peerEnd,
    int[] group,
    int first,
    int last
)
{
    private readonly IReadOnlyList<T> _partition = partition;

    private readonly int _index = index;

    private readonly int[] _peerStart = peerStart;

    private readonly int[] _peerEnd = peerEnd;

    private readonly int[] _group = group;

    private readonly int _first = first;

    private readonly int _last = last;

    public IEnumerable<T> Rows =>
        _first > _last ? [] : _partition.Skip(_first).Take(_last - _first + 1);

    public long RowNumber()
    {
        return _index + 1;
    }

    public long Rank()
    {
        return _peerStart[_index] + 1;
    }

    public long DenseRank()
    {
        return _group[_index] + 1;
    }

    public double PercentRank()
    {
        return _partition.Count == 1
            ? 0
            : (double)_peerStart[_index] / (_partition.Count - 1);
    }

    public double CumeDist()
    {
        return (double)_peerEnd[_index] / _partition.Count;
    }

    // The first count % buckets buckets hold one row more than the others.
    public int Ntile(int buckets)
    {
        int size = _partition.Count / buckets;
        int larger = _partition.Count % buckets;
        int inLarger = larger * (size + 1);
        return _index < inLarger
            ? (_index / (size + 1)) + 1
            : larger + ((_index - inLarger) / size) + 1;
    }

    public TValue Lag<TValue>(Func<T, TValue> value, int offset, TValue defaultValue)
    {
        return At(_index - offset, value, defaultValue);
    }

    public TValue Lead<TValue>(Func<T, TValue> value, int offset, TValue defaultValue)
    {
        return At(_index + offset, value, defaultValue);
    }

    public TValue FirstValue<TValue>(Func<T, TValue> value)
    {
        return _first > _last ? default! : value(_partition[_first]);
    }

    public TValue LastValue<TValue>(Func<T, TValue> value)
    {
        return _first > _last ? default! : value(_partition[_last]);
    }

    private TValue At<TValue>(int position, Func<T, TValue> value, TValue defaultValue)
    {
        return position >= 0 && position < _partition.Count
            ? value(_partition[position])
            : defaultValue;
    }
}
