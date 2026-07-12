namespace OdectyStat1.Contracts;

public interface IGarageCommandSigner
{
    byte[] Sign(uint correlationId, ReadOnlySpan<byte> nonce);
}
