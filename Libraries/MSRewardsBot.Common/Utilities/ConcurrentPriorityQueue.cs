using System.Collections.Generic;
using System.Threading;

namespace MSRewardsBot.Common.Utilities
{
    public class ConcurrentPriorityQueue<TElement, TPriority>
    {
        private readonly PriorityQueue<TElement, TPriority> _queue;
        private readonly Lock _lock = new Lock();

        public ConcurrentPriorityQueue()
        {
            _queue = new PriorityQueue<TElement, TPriority>();
        }

        public ConcurrentPriorityQueue(IComparer<TPriority> comparer)
        {
            _queue = new PriorityQueue<TElement, TPriority>(comparer);
        }


        /// <summary>
        ///  Adds the specified element with associated priority to the <see cref="PriorityQueue{TElement, TPriority}"/>.
        /// </summary>
        /// <param name="element">The element to add to the <see cref="PriorityQueue{TElement, TPriority}"/>.</param>
        /// <param name="priority">The priority with which to associate the new element.</param>
        public void Enqueue(TElement element, TPriority priority)
        {
            using (_lock.EnterScope())
            {
                _queue.Enqueue(element, priority);
            }
        }

        /// <summary>
        ///  Removes the minimal element from the <see cref="PriorityQueue{TElement, TPriority}"/>,
        ///  and copies it to the <paramref name="element"/> parameter,
        ///  and its associated priority to the <paramref name="priority"/> parameter.
        /// </summary>
        /// <param name="element">The removed element.</param>
        /// <param name="priority">The priority associated with the removed element.</param>
        /// <returns>
        ///  <see langword="true"/> if the element is successfully removed;
        ///  <see langword="false"/> if the <see cref="PriorityQueue{TElement, TPriority}"/> is empty.
        /// </returns>
        public bool TryDequeue(out TElement? element, out TPriority? priority)
        {
            using (_lock.EnterScope())
            {
                return _queue.TryDequeue(out element, out priority);
            }
        }

        /// <summary>
        ///  Returns a value that indicates whether there is a minimal element in the <see cref="PriorityQueue{TElement, TPriority}"/>,
        ///  and if one is present, copies it to the <paramref name="element"/> parameter,
        ///  and its associated priority to the <paramref name="priority"/> parameter.
        ///  The element is not removed from the <see cref="PriorityQueue{TElement, TPriority}"/>.
        /// </summary>
        /// <param name="element">The minimal element in the queue.</param>
        /// <param name="priority">The priority associated with the minimal element.</param>
        /// <returns>
        ///  <see langword="true"/> if there is a minimal element;
        ///  <see langword="false"/> if the <see cref="PriorityQueue{TElement, TPriority}"/> is empty.
        /// </returns>
        public bool TryPeek(out TElement? element, out TPriority? priority)
        {
            using (_lock.EnterScope())
            {
                return _queue.TryPeek(out element, out priority);
            }
        }

        /// <summary>
        ///  Gets the number of elements contained in the <see cref="PriorityQueue{TElement, TPriority}"/>.
        /// </summary>
        public int Count
        {
            get
            {
                using (_lock.EnterScope())
                {
                    return _queue.Count;
                }
            }
        }

        /// <summary>
        ///  Removes all items from the <see cref="PriorityQueue{TElement, TPriority}"/>.
        /// </summary>
        public void Clear()
        {
            using (_lock.EnterScope())
            {
                _queue.Clear();
            }
        }

        /// <summary>
        /// Returns an ordinated list
        /// </summary>
        public List<(TElement Element, TPriority Priority)> ToList()
        {
            using (_lock.EnterScope())
            {
                List<(TElement, TPriority)> list = new List<(TElement, TPriority)>();
                foreach ((TElement Element, TPriority Priority) item in _queue.UnorderedItems)
                {
                    list.Add((item.Element, item.Priority));
                }

                return list;
            }
        }
    }
}
