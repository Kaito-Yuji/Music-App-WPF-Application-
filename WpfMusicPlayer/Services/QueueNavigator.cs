using System;
using System.Collections.Generic;
using System.Linq;
using WpfMusicPlayer.Models;

namespace WpfMusicPlayer.Services
{
    /// <summary>
    /// Decides which queue position plays next. Deliberately free of audio and UI concerns, so the
    /// shuffle and repeat rules can be read and reasoned about on their own.
    ///
    /// Shuffle and repeat are independent, the way Spotify treats them: shuffle changes the order,
    /// repeat decides what happens at the ends of that order.
    /// </summary>
    public class QueueNavigator
    {
        private readonly Random _random;
        private readonly List<int> _shuffleOrder = new();
        private int _shuffleIndex = -1;
        private bool _isShuffleOn;

        public QueueNavigator(Random? random = null)
        {
            _random = random ?? new Random();
        }

        public int Count { get; private set; }

        public int CurrentIndex { get; private set; } = -1;

        public RepeatMode RepeatMode { get; set; } = RepeatMode.Off;

        public bool IsShuffleOn
        {
            get => _isShuffleOn;
            set
            {
                if (_isShuffleOn == value)
                    return;

                _isShuffleOn = value;

                if (_isShuffleOn)
                    GenerateShuffleOrder();
                else
                    ClearShuffleOrder();
            }
        }

        /// <summary>Re-points the navigator at a queue of <paramref name="count"/> items.</summary>
        public void Reset(int count, int startIndex)
        {
            Count = Math.Max(0, count);
            CurrentIndex = Count == 0 ? -1 : Math.Max(0, Math.Min(startIndex, Count - 1));

            if (IsShuffleOn)
                GenerateShuffleOrder();
        }

        /// <summary>
        /// Records that the listener jumped straight to <paramref name="index"/>, so the shuffle
        /// pointer follows them instead of carrying on from wherever it was.
        /// </summary>
        public void SetCurrent(int index)
        {
            if (index < 0 || index >= Count)
                return;

            CurrentIndex = index;

            if (!IsShuffleOn)
                return;

            if (_shuffleOrder.Count == 0)
                GenerateShuffleOrder();

            var position = _shuffleOrder.IndexOf(index);
            if (position == -1)
            {
                GenerateShuffleOrder();
                position = _shuffleOrder.IndexOf(index);
            }

            _shuffleIndex = position;
        }

        /// <summary>Next index to play, or null when playback should stop.</summary>
        public int? Next()
        {
            if (Count == 0)
                return null;

            if (RepeatMode == RepeatMode.RepeatOne)
                return CurrentIndex;

            if (!IsShuffleOn)
            {
                var next = CurrentIndex + 1;
                if (next < Count)
                    return next;

                return RepeatMode == RepeatMode.RepeatAll ? 0 : null;
            }

            if (_shuffleOrder.Count == 0)
                GenerateShuffleOrder();

            _shuffleIndex++;
            if (_shuffleIndex < _shuffleOrder.Count)
                return _shuffleOrder[_shuffleIndex];

            if (RepeatMode != RepeatMode.RepeatAll)
            {
                _shuffleIndex = _shuffleOrder.Count - 1;
                return null;
            }

            // A fresh order each lap, otherwise "shuffle" repeats the same sequence forever.
            // The new lap must not lead with the track that just finished.
            GenerateShuffleOrder(currentFirst: false);

            if (_shuffleOrder.Count > 1 && _shuffleOrder[0] == CurrentIndex)
                (_shuffleOrder[0], _shuffleOrder[1]) = (_shuffleOrder[1], _shuffleOrder[0]);

            _shuffleIndex = 0;
            return _shuffleOrder.Count > 0 ? _shuffleOrder[0] : null;
        }

        /// <summary>Previous index to play, or null when playback should stop.</summary>
        public int? Previous()
        {
            if (Count == 0)
                return null;

            if (RepeatMode == RepeatMode.RepeatOne)
                return CurrentIndex;

            if (!IsShuffleOn)
            {
                var previous = CurrentIndex - 1;
                if (previous >= 0)
                    return previous;

                return RepeatMode == RepeatMode.RepeatAll ? Count - 1 : null;
            }

            if (_shuffleOrder.Count == 0)
                GenerateShuffleOrder();

            _shuffleIndex--;
            if (_shuffleIndex >= 0)
                return _shuffleOrder[_shuffleIndex];

            if (RepeatMode != RepeatMode.RepeatAll)
            {
                _shuffleIndex = 0;
                return null;
            }

            _shuffleIndex = _shuffleOrder.Count - 1;
            return _shuffleIndex >= 0 ? _shuffleOrder[_shuffleIndex] : null;
        }

        /// <summary>Call after appending an item to the end of the queue.</summary>
        public void ItemAdded()
        {
            Count++;

            if (!IsShuffleOn)
                return;

            _shuffleOrder.Add(Count - 1);

            // Drop the newcomer somewhere in the part of the order that has not played yet
            var remainingStart = _shuffleIndex + 1;
            var lastPosition = _shuffleOrder.Count - 1;
            if (remainingStart < lastPosition)
            {
                var target = _random.Next(remainingStart, _shuffleOrder.Count);
                (_shuffleOrder[lastPosition], _shuffleOrder[target]) =
                    (_shuffleOrder[target], _shuffleOrder[lastPosition]);
            }
        }

        /// <summary>Call after removing the item at <paramref name="index"/> from the queue.</summary>
        public void ItemRemoved(int index)
        {
            if (index < 0 || index >= Count)
                return;

            Count--;

            if (IsShuffleOn)
            {
                var position = _shuffleOrder.IndexOf(index);
                if (position >= 0)
                {
                    _shuffleOrder.RemoveAt(position);
                    if (position <= _shuffleIndex)
                        _shuffleIndex--;
                }

                for (var i = 0; i < _shuffleOrder.Count; i++)
                {
                    if (_shuffleOrder[i] > index)
                        _shuffleOrder[i]--;
                }
            }

            if (index < CurrentIndex)
                CurrentIndex--;

            if (CurrentIndex >= Count)
                CurrentIndex = Count - 1;
        }

        /// <param name="currentFirst">
        /// When true the track playing now leads the shuffled order. That is what makes turning
        /// shuffle on leave the current track alone while still guaranteeing every other track in
        /// the queue plays exactly once before the lap ends.
        /// </param>
        private void GenerateShuffleOrder(bool currentFirst = true)
        {
            _shuffleOrder.Clear();

            if (Count == 0)
            {
                _shuffleIndex = -1;
                return;
            }

            _shuffleOrder.AddRange(Enumerable.Range(0, Count));

            // Fisher-Yates
            for (var i = _shuffleOrder.Count - 1; i > 0; i--)
            {
                var j = _random.Next(i + 1);
                (_shuffleOrder[i], _shuffleOrder[j]) = (_shuffleOrder[j], _shuffleOrder[i]);
            }

            if (!currentFirst || CurrentIndex < 0 || CurrentIndex >= Count)
            {
                _shuffleIndex = -1;
                return;
            }

            var position = _shuffleOrder.IndexOf(CurrentIndex);
            (_shuffleOrder[0], _shuffleOrder[position]) = (_shuffleOrder[position], _shuffleOrder[0]);
            _shuffleIndex = 0;
        }

        private void ClearShuffleOrder()
        {
            _shuffleOrder.Clear();
            _shuffleIndex = -1;
        }
    }
}
