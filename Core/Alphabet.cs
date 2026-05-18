namespace CoesarCipher.Core;

public sealed class Alphabet
{
    public string Lower { get; }
    public string Upper { get; }
    public int Length => Lower.Length;

    public Alphabet(string lower, string upper)
    {
        if (lower.Length != upper.Length)
            throw new ArgumentException("Длина нижнего и верхнего регистра должна совпадать");
        Lower = lower;
        Upper = upper;
    }

    // 33 буквы, Ё на своём месте
    public static readonly Alphabet Russian = new(
        "абвгдеёжзийклмнопрстуфхцчшщъыьэюя",
        "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ");

    public static readonly Alphabet English = new(
        "abcdefghijklmnopqrstuvwxyz",
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ");

    public static readonly IReadOnlyList<Alphabet> All = new[] { Russian, English };

    public bool TryFind(char c, out int index, out bool isUpper)
    {
        int i = Lower.IndexOf(c);
        if (i >= 0) { index = i; isUpper = false; return true; }
        i = Upper.IndexOf(c);
        if (i >= 0) { index = i; isUpper = true; return true; }
        index = -1; isUpper = false; return false;
    }

    public char At(int index, bool isUpper)
    {
        int n = Length;
        index = ((index % n) + n) % n;
        return isUpper ? Upper[index] : Lower[index];
    }
}
