using System;
using System.Security.Cryptography;
using System.Text;

public static class PasswordHelper
{
    public static string Hash(string input)
    {
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(input))
        );
    }
}