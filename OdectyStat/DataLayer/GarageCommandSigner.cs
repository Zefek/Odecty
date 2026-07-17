using System.Buffers.Binary;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using OdectyStat1.Contracts;
using OdectyStat1.Dto;

namespace OdectyStat1.DataLayer;

public class GarageCommandSigner : IGarageCommandSigner
{
    private readonly byte[] key;
    private readonly int tagBytes;

    public GarageCommandSigner(IOptions<GarageSettings> options)
    {
        key = Convert.FromHexString(options.Value.SigningKeyHex);
        tagBytes = options.Value.SignatureBytes;
    }

    public byte[] Sign(uint correlationId, ReadOnlySpan<byte> nonce)
    {
        ReadOnlySpan<byte> intent = "open"u8;
        var buffer = new byte[4 + nonce.Length + intent.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, correlationId);
        nonce.CopyTo(buffer.AsSpan(4));
        intent.CopyTo(buffer.AsSpan(4 + nonce.Length));
        using var hmac = new HMACSHA256(key);
        var full = hmac.ComputeHash(buffer);
        return full.AsSpan(0, tagBytes).ToArray();
    }
}
