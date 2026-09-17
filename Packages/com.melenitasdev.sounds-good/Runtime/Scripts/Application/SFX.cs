using System;
using UnityEngine;

namespace MelenitasDev.SoundsGood
{
    /// <summary>
    /// Serializable pseudo-enum for sound tags.
    /// Concrete values live in SFX_Generated.cs
    /// </summary>
    [Serializable]
    public partial struct SFX : IEquatable<SFX>
    {
        [SerializeField] private string value;
        
        internal SFX (string value) => this.value = string.IsNullOrEmpty(value) ? NULL_TAG : value;

        internal const string NULL_TAG = "__NULL__";
        public static readonly SFX Null = new SFX(NULL_TAG);
        public bool IsNull => string.IsNullOrEmpty(value) || value == NULL_TAG;
        
        public override string ToString () => string.IsNullOrEmpty(value) ? NULL_TAG : value;
        public bool Equals (SFX other) => value == other.value;
        public override int GetHashCode () => value?.GetHashCode() ?? 0;

        public static implicit operator string (SFX s) => string.IsNullOrEmpty(s.value) ? NULL_TAG : s.value;
    }
}