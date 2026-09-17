using System;
using UnityEngine;

namespace MelenitasDev.SoundsGood
{
    /// <summary>
    /// Serializable pseudo-enum for audio effect preset tags.
    /// Concrete values live in Effect_Generated.cs
    /// </summary>
    [Serializable]
    public partial struct Effect : IEquatable<Effect>
    {
        [SerializeField] private string value;

        internal Effect (string value) => this.value = string.IsNullOrEmpty(value) ? NULL_TAG : value;

        internal const string NULL_TAG = "__NULL__";
        public static readonly Effect Null = new Effect(NULL_TAG);
        public bool IsNull => string.IsNullOrEmpty(value) || value == NULL_TAG;

        public override string ToString () => string.IsNullOrEmpty(value) ? NULL_TAG : value;
        public bool Equals (Effect other) => value == other.value;
        public override int GetHashCode () => value?.GetHashCode() ?? 0;

        public static implicit operator string (Effect e) => string.IsNullOrEmpty(e.value) ? NULL_TAG : e.value;
    }
}
