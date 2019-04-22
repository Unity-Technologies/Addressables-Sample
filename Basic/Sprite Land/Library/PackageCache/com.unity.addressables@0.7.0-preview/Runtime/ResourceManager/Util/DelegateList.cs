using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.Util;

internal class DelegateList<T>
{
    Func<Action<T>, LinkedListNode<Action<T>>> m_acquireFunc;
    Action<LinkedListNode<Action<T>>> m_releaseFunc;
    LinkedList<Action<T>> m_callbacks;
    bool m_invoking = false;
    public DelegateList(Func<Action<T>, LinkedListNode<Action<T>>> acquireFunc, Action<LinkedListNode<Action<T>>> releaseFunc)
    {
        if (acquireFunc == null)
            throw new ArgumentNullException("acquireFunc");
        if (releaseFunc == null)
            throw new ArgumentNullException("releaseFunc");
        m_acquireFunc = acquireFunc;
        m_releaseFunc = releaseFunc;
    }

    public int Count { get { return m_callbacks == null ? 0 : m_callbacks.Count; } }

    public void Add(Action<T> action)
    {
        var node = m_acquireFunc(action);
        if (m_callbacks == null)
            m_callbacks = new LinkedList<Action<T>>();
        m_callbacks.AddLast(node);
    }

    public void Remove(Action<T> action)
    {
        if (m_callbacks == null)
            return;

        var node = m_callbacks.First;
        while (node != null)
        {
            if (node.Value == action)
            {
                if (m_invoking)
                {
                    node.Value = null;
                }
                else
                {
                    m_callbacks.Remove(node);
                    m_releaseFunc(node);
                }
                return;
            }
            node = node.Next;
        }
    }

    public void Invoke(T res)
    {
        if (m_callbacks == null)
            return;

        m_invoking = true;
        var node = m_callbacks.First;
        while (node != null)
        {
            if (node.Value != null)
            {
                try
                {
                    node.Value(res);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }
            node = node.Next;
        }
        m_invoking = false;
        var r = m_callbacks.First;
        while (r != null)
        {
            var next = r.Next;
            if (r.Value == null)
            {
                m_callbacks.Remove(r);
                m_releaseFunc(r);
            }
            r = next;
        }
    }

    public void Clear()
    {
        if (m_callbacks == null)
            return;
        var node = m_callbacks.First;
        while (node != null)
        {
            var next = node.Next;
            m_callbacks.Remove(node);
            m_releaseFunc(node);
            node = next;
        }
    }

    public static DelegateList<T> CreateWithGlobalCache()
    {
        return new DelegateList<T>(GlobalLinkedListNodeCache<Action<T>>.Acquire, GlobalLinkedListNodeCache<Action<T>>.Release);
    }
}
