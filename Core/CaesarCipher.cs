using System.Text;

namespace CoesarCipher.Core;

public enum CipherMode
{
    Encrypt,
    Decrypt
}

public static class CaesarCipher
{
    public static string Encrypt(string text, int key) => Shift(text, key);

    public static string Decrypt(string text, int key) => Shift(text, -key);

    public static string Process(string text, int key, CipherMode mode) =>
        mode == CipherMode.Encrypt ? Encrypt(text, key) : Decrypt(text, key);

    private static string Shift(string text, int key)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length == 0) return text;

        var sb = new StringBuilder(text.Length);
        foreach (char ch in text)
        {
            bool handled = false;
            foreach (var alphabet in Alphabet.All)
            {
                if (alphabet.TryFind(ch, out int idx, out bool isUpper))
                {
                    sb.Append(alphabet.At(idx + key, isUpper));
                    handled = true;
                    break;
                }
            }
            if (!handled) sb.Append(ch);
        }
        return sb.ToString();
    }
}
