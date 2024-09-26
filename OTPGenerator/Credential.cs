using System.Runtime.InteropServices;
using System.Text;

namespace OTPGenerator;

#region Windows Credential Manager API

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct Credential
{
    public uint Flags;
    public uint Type;
    public IntPtr TargetName;
    public IntPtr Comment;
    public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
    public uint CredentialBlobSize;
    public IntPtr CredentialBlob;
    public uint Persist;
    public uint AttributeCount;
    public IntPtr Attributes;
    public IntPtr TargetAlias;
    public IntPtr UserName;

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredWrite([In] ref Credential credential, [In] uint flags);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredRead(string target, uint type, uint flags, out IntPtr credential);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredFree([In] IntPtr buffer);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredDelete(string target, uint type, uint flags);

    internal static bool StoreCredential(string secret, string credentialName)
    {
        Credential credential = new Credential
        {
            Type = 1, // CRED_TYPE_GENERIC
            TargetName = Marshal.StringToCoTaskMemUni(credentialName),
            CredentialBlobSize = (uint)Encoding.Unicode.GetByteCount(secret),
            CredentialBlob = Marshal.StringToCoTaskMemUni(secret),
            Persist = 2 // CRED_PERSIST_LOCAL_MACHINE
        };

        bool result = CredWrite(ref credential, 0);

        Marshal.FreeCoTaskMem(credential.TargetName);
        Marshal.FreeCoTaskMem(credential.CredentialBlob);

        return result;
    }

    internal static string RetrieveCredential(string credentialName)
    {
        IntPtr credentialPtr;
        if (!CredRead(credentialName, 1, 0, out credentialPtr))
        {
            return null;
        }

        Credential credential = (Credential)Marshal.PtrToStructure(credentialPtr, typeof(Credential));
        string secret = Marshal.PtrToStringUni(credential.CredentialBlob, (int)credential.CredentialBlobSize / 2);
        CredFree(credentialPtr);

        return secret;
    }

    internal static bool DeleteCredential(string credentialName)
        => CredDelete(credentialName, 1, 0);
}

#endregion