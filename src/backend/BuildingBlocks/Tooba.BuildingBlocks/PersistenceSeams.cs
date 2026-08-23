namespace Tooba.BuildingBlocks;

public interface IDatabaseConnectionResolver
{
    string Resolve(ConnectionReference reference);
}

public static class UuidV7
{
    public static Guid New()
    {
        Span<byte> bytes = stackalloc byte[16];
        var unixMs = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        System.Buffers.Binary.BinaryPrimitives.WriteUInt64BigEndian(bytes, unixMs << 16);
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x70);
        Random.Shared.NextBytes(bytes[8..]);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes, bigEndian: true);
    }
}
