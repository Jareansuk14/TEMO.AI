using System.Runtime.CompilerServices;

namespace TEMO.AI;

public static class VaultGate
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string Get(Vk slot) => VaultCore.Resolve(slot);
}
