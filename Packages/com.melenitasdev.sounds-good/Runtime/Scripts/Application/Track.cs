using System;
using UnityEngine;

namespace MelenitasDev.SoundsGood
{
    /// <summary>
    /// Serializable pseudo-enum for music tags.
    /// Concrete values live in Track_Generated.cs
    /// </summary>
    [Serializable]
    public partial struct Track : IEquatable<Track>
    {
        [SerializeField] private string value;

        internal Track (string value) => this.value = string.IsNullOrEmpty(value) ? NULL_TAG : value;

        internal const string NULL_TAG = "__NULL__";
        public static readonly Track Null = new Track(NULL_TAG);
        public bool IsNull => string.IsNullOrEmpty(value) || value == NULL_TAG;

        public override string ToString () => string.IsNullOrEmpty(value) ? NULL_TAG : value;
        public bool Equals (Track other) => value == other.value;
        public override int GetHashCode () => value?.GetHashCode() ?? 0;

        public static implicit operator string (Track s) => string.IsNullOrEmpty(s.value) ? NULL_TAG : s.value;
    }
}