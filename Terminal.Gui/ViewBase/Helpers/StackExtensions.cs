namespace Terminal.Gui.ViewBase;

/// <summary>Extension of <see cref="Stack{T}"/> helper to work with specific <see cref="IEqualityComparer{T}"/></summary>
public static class StackExtensions
{
    /// <param name="stack">The stack object.</param>
    /// <typeparam name="T">The stack object type.</typeparam>
    extension<T> (Stack<T> stack)
    {
        /// <summary>Check if the stack object contains the value to find.</summary>
        /// <param name="valueToFind">Value to find.</param>
        /// <param name="comparer">The comparison object.</param>
        /// <returns><c>true</c> If the value was found.<c>false</c> otherwise.</returns>
        public bool Contains (T valueToFind, IEqualityComparer<T>? comparer = null)
        {
            comparer ??= EqualityComparer<T>.Default;

            foreach (T obj in stack)
            {
                if (comparer.Equals (obj, valueToFind))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Move the first stack object value to the end.</summary>
        public void MoveNext ()
        {
            Stack<T> temp = new ();
            T last = stack.Pop ();

            while (stack.Count > 0)
            {
                T value = stack.Pop ();
                temp.Push (value);
            }

            temp.Push (last);

            while (temp.Count > 0)
            {
                stack.Push (temp.Pop ());
            }
        }

        /// <summary>Move the last stack object value to the top.</summary>
        public void MovePrevious ()
        {
            Stack<T> temp = new ();
            T? first = default;

            while (stack.Count > 0)
            {
                T value = stack.Pop ();
                temp.Push (value);

                if (stack.Count == 1)
                {
                    first = stack.Pop ();
                }
            }

            while (temp.Count > 0)
            {
                stack.Push (temp.Pop ());
            }

            if (first is { })
            {
                stack.Push (first);
            }
        }

        /// <summary>Move the stack object value to the index.</summary>
        /// <param name="valueToMove">Value to move.</param>
        /// <param name="index">The index where to move.</param>
        /// <param name="comparer">The comparison object.</param>
        public void MoveTo (T valueToMove, int index = 0, IEqualityComparer<T>? comparer = null)
        {
            if (index < 0)
            {
                return;
            }

            comparer ??= EqualityComparer<T>.Default;

            Stack<T> temp = new ();
            var toMove = default (T);
            int stackCount = stack.Count;
            var count = 0;

            while (stack.Count > 0)
            {
                T value = stack.Pop ();

                if (comparer.Equals (value, valueToMove))
                {
                    toMove = value;

                    break;
                }

                temp.Push (value);
                count++;
            }

            var idx = 0;

            while (stack.Count < stackCount)
            {
                if (count - idx == index)
                {
                    if (toMove is { })
                    {
                        stack.Push (toMove);
                    }
                }
                else
                {
                    stack.Push (temp.Pop ());
                }

                idx++;
            }
        }

        /// <summary>Replaces a stack object values that match with the value to replace.</summary>
        /// <param name="valueToReplace">Value to replace.</param>
        /// <param name="valueToReplaceWith">Value to replace with to what matches the value to replace.</param>
        /// <param name="comparer">The comparison object.</param>
        public void Replace (T valueToReplace, T valueToReplaceWith, IEqualityComparer<T>? comparer = null)
        {
            comparer ??= EqualityComparer<T>.Default;

            Stack<T> temp = new ();

            while (stack.Count > 0)
            {
                T value = stack.Pop ();

                if (comparer.Equals (value, valueToReplace))
                {
                    stack.Push (valueToReplaceWith);

                    break;
                }

                temp.Push (value);
            }

            while (temp.Count > 0)
            {
                stack.Push (temp.Pop ());
            }
        }
    }
}
