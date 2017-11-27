public class Container<T> where T : struct 
{
    public bool readOnly;
    public int Capacity{get{return _array.Length;}}
    private int _count;

    public int Count
    {
        private set
        {
            _count = value;
        }
        get{return _count;}
    }

    private T[] _array;

    public T[] Array
    {
        get
        {
            lock (_array)
            {
                return _array;
            }
        }
    }

    public Container(T[] sArray)
    {
        _array=sArray;
    }

    public Container(Container<T> source) 
    {
        lock (_array = new T[source.Count])
        {
            _count = source.Count;
            System.Array.Copy(source._array, _array, _count);
        }
    }

    public void CopyFrom(Container<T> source)
    {
        if(ReferenceEquals(this, source))
            return;
        lock (_array)
        {
            _count = Count;
            System.Array.Copy(source._array, _array, _count);
        }
    }

    public Container(int capacity)
    {
        _array = new T[capacity];
    }

    public void Add(T e)
    {
        lock (_array)
        {
            _array[Count++] = e;
        }
    }

    public void Add(int count)
    {
        Count+=count;
    }

    public void Add(ref T e)
    {
        lock (_array)
        {
            _array[Count++] = e;
        }
    }

    public void Remove(int index)
    {
        lock (_array)
        {
            --Count;
            if(Count > 0)
            {
                _array[index] = _array[Count];
            }
        }
    }

    public T this[int i]
    {
        get
        {
            lock (_array)
            {
                return _array[i];
            }
        }
        set
        {
            lock (_array)
            {
                _array[i] = value;
            }
        }
    }

    public void Execute(Job<T>.ExecuteDelegate action, int indexMin, int indexMax)
    {
        for(int index = indexMin; index < indexMax;++index)
        {
            action(index, ref _array[index]);
        }
    }
}