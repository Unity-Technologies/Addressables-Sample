using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal class ListWithEvents<T> : IList<T>
{
    private List<T> m_List = new List<T>();


    public event Action<T> OnElementAdded;
    public event Action<T> OnElementRemoved;

    private void InvokeAdded(T element)
    {
        if (OnElementAdded != null)
            OnElementAdded(element);
    }

    private void InvokeRemoved(T element)
    {
        if (OnElementRemoved != null)
            OnElementRemoved(element);
    }


    public T this[int index]
    {
        get { return m_List[index]; }
        set
        {
            T oldElement = m_List[index];
            m_List[index] = value;
            InvokeRemoved(oldElement);
            InvokeAdded(value);
        }
    }

    public int Count { get { return m_List.Count; } }

    public bool IsReadOnly { get { return ((IList<T>)m_List).IsReadOnly; } }

    public void Add(T item)
    {
        m_List.Add(item);
        InvokeAdded(item);
    }

    public void Clear()
    {
        foreach (T obj in m_List)
            InvokeRemoved(obj);
        m_List.Clear();
    }

    public bool Contains(T item)
    {
        return m_List.Contains(item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        m_List.CopyTo(array, arrayIndex);
    }

    public IEnumerator<T> GetEnumerator()
    {
        return m_List.GetEnumerator();
    }

    public int IndexOf(T item)
    {
        return m_List.IndexOf(item);
    }

    public void Insert(int index, T item)
    {
        m_List.Insert(index, item);
        InvokeAdded(item);
    }

    public bool Remove(T item)
    {
        bool ret = m_List.Remove(item);
        if (ret)
            InvokeRemoved(item);
        return ret;
    }

    public void RemoveAt(int index)
    {
        T item = m_List[index];
        m_List.RemoveAt(index);
        InvokeRemoved(item);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)m_List).GetEnumerator();
    }
}
