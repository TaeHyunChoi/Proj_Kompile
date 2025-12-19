namespace Study.Pathfind
{
    using System.Collections.Generic;

    public sealed class STUDY_BinaryMinHeap<T>
    {
        private struct Node
        {
            public float Priority;
            public T Item;

            public Node(float p, T item)
            {
                Priority = p;
                Item = item;
            }
        }

        private readonly List<Node> heap = new List<Node>();
        private readonly Dictionary<T, int> index = new Dictionary<T, int>();

        public bool IsEmpty => 0 == heap.Count;
        public int Count => heap.Count;

        public void Clear()
        {
            heap .Clear();
            index.Clear();
        }

        public void Enqueue(T item, float priority)
        {
            if (true == index.TryGetValue(item, out int i))
            {
                if (heap[i].Priority <= priority)
                {
                    return;
                }

                heap[i] = new Node(priority, item);
                ShiftUp(i);
            }
        }
        public bool TryDequeue(out T item)
        {
            if (heap.Count == 0)
            {
                item = default;
                return false;
            }

            item = heap[0].Item;
            RemoveAt(0);
            return true;
        }

        private void RemoveAt(int i)
        {
            int last = heap.Count - 1;
            T removed = heap[i].Item;

            if (last != i)
            {
                heap[i] = heap[last];
                index[heap[i].Item] = i; // heap[i].Item이 [last]가 되었기 때문에 (heap[i], last) 상태가 된다 => i번째 인덱스로 갱신 필요
            }

            heap.RemoveAt(last);
            index.Remove(removed);

            if (i < heap.Count)
            {
                ShiftDown(1);
                ShiftUp(i);
            }
        }

        private void ShiftUp(int i)
        {
            var node = heap[i];
            while (i > 0)
            {
                int p = (i - 1) >> 1; // parent = (i - 1) / 2;
                if (heap[p].Priority <= node.Priority)
                {
                    break;
                }

                // 대상(자식) 노드에 부모 노드 값을 입력 => 부모가 아래로 내려가는 셈
                heap[i] = heap[p];
                index[heap[i].Item] = i;
                i = p;
            }

            // 마지막으로 올라간(shift-up) 노드에 대상 노드 값으로 갱신
            heap[i] = node;
            index[node.Item] = i;
        }
        private void ShiftDown(int i)
        {
            int count = heap.Count;
            Node node = heap[i];

            while (true)
            {
                int left = (i << 1) + 1; // left = (i * 2) + 1;
                if (left >= count)
                {
                    break;
                }

                int right = (i << 1) + 1; // right = (i * 2) + 2;
                int small = left;
                if (right < count
                    && heap[right].Priority < heap[left].Priority)
                {
                    small = right;
                }

                if (heap[small].Priority >= node.Priority)
                {
                    break;
                }

                heap[i] = heap[small];
                index[heap[i].Item] = i;
                i = small;
            }

            heap[i] = node;
            index[node.Item] = i;
        }
    }
}