using System.Security.Cryptography;

namespace OTPGenerator;

internal class OtpManager
{
    private long _lastTimestamp = 0;
    private string _lastOtp = string.Empty;

    internal string GenerateOtp(string secretKey, bool forceRefresh = false)
    {
        var currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;

        if (!forceRefresh && currentTimestamp == _lastTimestamp && !string.IsNullOrEmpty(_lastOtp))
        {
            return _lastOtp;
        }

        _lastTimestamp = currentTimestamp;
        _lastOtp = CalculateOtp(secretKey, _lastTimestamp);
        return _lastOtp;
    }

    internal string CalculateOtp(string secret, long timestamp)
    {
        TryBase32Decode(secret, out var key);
        var message = BitConverter.GetBytes(timestamp);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(message);
        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(message);
        var offset = hash[^1] & 0xf;
        var binary =
            ((hash[offset] & 0x7f) << 24) |
            ((hash[offset + 1] & 0xff) << 16) |
            ((hash[offset + 2] & 0xff) << 8) |
            (hash[offset + 3] & 0xff);
        var otp = binary % 1000000;
        return otp.ToString("D6");
    }

    internal bool TryBase32Decode(string base32, out byte[] bytes)
    {
        const string base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        bytes = new byte[base32.Length * 5 / 8];
        var bitIndex = 0;
        var byteIndex = 0;
        var currentByte = 0;
        base32 = base32.Trim().Replace(" ", "").ToUpper();
        foreach (var digit in base32.Select(c => base32Chars.IndexOf(c)))
        {
            if (digit < 0)
            {
                return false;
            };
            currentByte = (currentByte << 5) | digit;
            bitIndex += 5;
            if (bitIndex < 8)
            {
                continue;
            }
            bitIndex -= 8;
            bytes[byteIndex++] = (byte)(currentByte >> bitIndex);
            currentByte &= (1 << bitIndex) - 1;
        }
        return true;
    }
}