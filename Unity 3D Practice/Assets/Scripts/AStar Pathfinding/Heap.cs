using System;
using UnityEngine;

public interface IHeapItem<T> : IComparable<T>
{
	int HeapIndex { get; set; }
}

public class Heap<T> where T : IHeapItem<T>
{
	private T[] _items;
	private int _currentItemCount;

	public int Count => _currentItemCount;

	public Heap(int maxHeapSize)
	{
		_items = new T[maxHeapSize];
	}

	public void AddLast(T item)
	{
		item.HeapIndex = _currentItemCount;
		
		_items[_currentItemCount] = item;
		SortUp(item);

		_currentItemCount++;
	}

	public T RemoveFirst()
	{
		T firstItem = _items[0];
		_currentItemCount--;

		// Put the last item on the first slot.
		_items[0] = _items[_currentItemCount];
		_items[0].HeapIndex = 0;
		SortDown(_items[0]);

		return firstItem;
	}

	public void UpdateItemPriority(T item)
	{
		SortUp(item);
	}

	public bool Contains(T item)
	{
		return item.HeapIndex < _currentItemCount && Equals(_items[item.HeapIndex], item);
	}

	public void Clear() => _currentItemCount = 0;

	private void SortUp(T item)
	{
		int parentIndex = (item.HeapIndex - 1) / 2;

		while (true)
		{
			T parent = _items[parentIndex];

			// If this item has a higher priority, which means lower F cost, then swap it with its parent.
			if (item.CompareTo(parent) > 0)
			{
				Swap(item, parent);
				parentIndex = (item.HeapIndex - 1) / 2;
			}
			else
			{
				break;
			}
		}
	}

	private void SortDown(T item)
	{
		while (true)
		{
			int leftChildIndex = item.HeapIndex * 2 + 1;
			int rightChildIndex = item.HeapIndex * 2 + 2;
			int swapIndex;

			// If this item has at least 1 child, on the left.
			if (leftChildIndex < _currentItemCount)
			{
				swapIndex = leftChildIndex;

				// If this item also has the right child and it has a higher priority (lower F cost).
				if (rightChildIndex < _currentItemCount && _items[leftChildIndex].CompareTo(_items[rightChildIndex]) < 0)
				{
					swapIndex = rightChildIndex;
				}

				// If this item has a lower priority compares to its highest-priority children, then swap them.
				if (item.CompareTo(_items[swapIndex]) < 0)
					Swap(item, _items[swapIndex]);
				else
					return;
			}
			// Otherwise, simply returns since this item's already in the correct position.
			else
				return;
		}
	}

	private void Swap(T a, T b)
	{
		_items[a.HeapIndex] = b;
		_items[b.HeapIndex] = a;

		(a.HeapIndex, b.HeapIndex) = (b.HeapIndex, a.HeapIndex);
	}
}
