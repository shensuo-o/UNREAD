using System;
using UnityEngine;

namespace MelenitasDev.SoundsGood
{
    /// <summary>
    /// Serializable pseudo-enum for occlusion material tags.
    /// Concrete values live in OcclusionMaterial_Generated.cs
    /// </summary>
    [Serializable]
    public partial struct OcclusionMaterial : IEquatable<OcclusionMaterial>
    {
        [SerializeField] private string value;

        internal OcclusionMaterial (string value) => this.value = string.IsNullOrEmpty(value) ? NULL_TAG : value;

        internal const string NULL_TAG = "__NULL__";
        public static readonly OcclusionMaterial Null = new OcclusionMaterial(NULL_TAG);
        public bool IsNull => string.IsNullOrEmpty(value) || value == NULL_TAG;

        public override string ToString () => string.IsNullOrEmpty(value) ? NULL_TAG : value;
        public bool Equals (OcclusionMaterial other) => value == other.value;
        public override int GetHashCode () => value?.GetHashCode() ?? 0;

        public static implicit operator string (OcclusionMaterial m) => string.IsNullOrEmpty(m.value) ? NULL_TAG : m.value;
    }
}
