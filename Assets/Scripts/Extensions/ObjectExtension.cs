namespace OrbitalEscape.Extensions
{
    public static class ObjectExtension
    {
        public static bool IsNull(this UnityEngine.Object obj) => obj == null || obj.Equals(null);

        public static bool IsNotNull(this UnityEngine.Object obj) => !IsNull(obj);
    }
}
