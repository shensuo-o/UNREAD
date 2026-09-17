using System;
using UnityEngine;

namespace MelenitasDev.SoundsGood
{
    /// <summary>
    /// Serializable pseudo-enum for output tags.
    /// Concrete values live in Output_Generated.cs
    /// </summary>
    [Serializable]
    public partial struct Output : IEquatable<Output>
    {
        [SerializeField] private string value;

        internal Output (string value) => this.value = string.IsNullOrEmpty(value) ? NULL_TAG : value;

        internal const string NULL_TAG = "__NULL__";
        public static readonly Output Null = new Output(NULL_TAG);
        public bool IsNull => string.IsNullOrEmpty(value) || value == NULL_TAG;

        public override string ToString () => string.IsNullOrEmpty(value) ? NULL_TAG : value;
        public bool Equals (Output other) => value == other.value;
        public override int GetHashCode () => value?.GetHashCode() ?? 0;

        public static implicit operator string (Output s) => string.IsNullOrEmpty(s.value) ? NULL_TAG : s.value;
    }
}