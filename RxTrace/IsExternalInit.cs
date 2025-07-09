#if !(NET5_0_OR_GREATER)
using System.ComponentModel;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Dummy class that fixes CS0518
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit { }
}
#endif