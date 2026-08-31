using System.Security.Cryptography;
using System.Text;

namespace ClassesReborn;

internal static class FutureContentIds {
    internal static string Get(string name) {
        using var md5 = MD5.Create();
        var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(
            $"ClassesReborn.FutureContent.{name}"));
        return string.Concat(bytes.Select(value => value.ToString("x2")));
    }
}
