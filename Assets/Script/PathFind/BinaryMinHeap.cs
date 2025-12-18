using System;
using System.Collections.Generic;

/// <summary>
/// Min Heap에 기반한 우선순위 큐 (중복 우선순위를 허용하는 버전)
/// 꼭 PathNode뿐만 아니라 다른 item도 가능하도록
/// </summary>
/// <typeparam name="T"></typeparam>
public class BinaryMinHeap<T>
{
    private readonly List<(float priority, T item)> heap;
    public BinaryMinHeap()
    {
        heap = new List<(float priority, T item)>();
    }

    public int Count => heap.Count;

    public void Enqueue(T item, float priority)
    {
        heap.Add((priority, item));
        ShiftUp(heap.Count - 1);
    }

    private T Dequeue()
    {
        if (0 == heap.Count)
        {
            throw new InvalidOperationException($"{typeof(T)} heap is empty");
        }

        // 반환할(꺼낼) 값 먼저 뽑아두기
        T rootItem = heap[0].item;

        // 순서를 섞어서 강제로 정렬 갱신 (마지막을 맨위로)
        var last = heap[heap.Count -1];
        heap.RemoveAt(heap.Count - 1);

        if (0 < heap.Count - 1)
        {
            heap[0] = last;
            ShiftDown(0);
        }

        return rootItem;
    }

    private void ShiftUp(int i)
    {
        (float priority, T item) targetNode = heap[i];

        while (i > 0)
        {
            int parent = (i - 1) >> 1;

            if (heap[parent].priority <= targetNode.priority)
            {
                break;
            }

            heap[i] = heap[parent];
            i = parent;
        }

        heap[i] = targetNode;
    }

    private void ShiftDown(int i)
    {
        int n = heap.Count;
        (float priority, T item) node = heap[i];

        while (true)
        {
            int left = (i << 1) - 1;
            if (left >= n)
            {
                break;
            }

            int right = left + 1;
            int smallest = right;

            if (right < n
                && heap[right].priority < heap[left].priority)
            {
                smallest = right;
            }

            if (heap[smallest].priority >= node.priority)
            {
                break;
            }

            heap[i] = heap[smallest];
            i = smallest;
        }

        heap[i] = node;
    }
}
