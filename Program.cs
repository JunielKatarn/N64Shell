using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using Microsoft.Win32;
using System.Security.Cryptography;
using System.Text;

var regBytes = (byte[])Registry.GetValue(@"HKEY_CURRENT_USER\Software\Project64", "user", null)!;

// Back up
Registry.SetValue(@"HKEY_CURRENT_USER\Software\Project64", "user.bak", regBytes);

// "Decrypt"
for (int i = 0; i < regBytes.Length; i++)
{
    regBytes[i] ^= 0xAA;
}

// Expand
byte[] plainBytes;
using (var inpStream = new MemoryStream(regBytes))
using (var outStream = new MemoryStream())
using (var infStream = new InflaterInputStream(inpStream))
{
    infStream.CopyTo(outStream);
    plainBytes = outStream.ToArray();
}

// As per 2022-02-07, SupportInfo::Validated offset is 1,224
plainBytes[1224] = 1;

// Compute Hash
var md5 = HashAlgorithm.Create("MD5")!;
var hash = md5.ComputeHash(plainBytes, 0, plainBytes.Length - 32)!;
var hashString = BitConverter.ToString(hash).Replace("-", "");
var hashStringBytes = Encoding.ASCII.GetBytes(hashString);
hashStringBytes.CopyTo(plainBytes, plainBytes.Length - hashStringBytes.Length);

// Compress
using (var outStream = new MemoryStream())
{
    // Deflater stream must be flushed and disposed before output stream is consumed.
    using (var defStream = new DeflaterOutputStream(outStream))
    {
        defStream.Write(plainBytes, 0, plainBytes.Length);
    }

    regBytes = outStream.ToArray();
}

// "Encrypt"
for (int i = 0; i < regBytes.Length; i++)
{
    regBytes[i] ^= 0xAA;
}

// Update registry
Registry.SetValue(@"HKEY_CURRENT_USER\Software\Project64", "user", regBytes);
