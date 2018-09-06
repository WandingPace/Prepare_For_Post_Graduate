using System.Collections.Generic;
/// <summary>
/// 通用对象池类
/// </summary>
/// <typeparam name="T"></typeparam>
public class ObjectPool<T> where T : class, new()
{
	private Stack<T> _pool = null;

	public ObjectPool()
	{
		_pool = new Stack<T>();
	}

	public ObjectPool(int capacity)
	{
		_pool = new Stack<T>(capacity);
	}

	public T Acquire()
	{

		if (_pool.Count == 0)
		{
			return new T();
		}
		return _pool.Pop();

	}

	public void Release(T item)
	{
		if (null == item)
			return;

		if (_pool.Contains(item))
			return;

		_pool.Push(item);

	}

	public void Clear()
	{
		_pool.Clear();
	}

	public int _Count
	{
		get
		{
			return _pool.Count;
		}
	}
}
