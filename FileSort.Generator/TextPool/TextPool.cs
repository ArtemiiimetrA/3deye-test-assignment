namespace FileSort.Generator.TextPool;

/// <summary>
///     Manages a pool of base strings and generates variations for test file generation.
/// </summary>
internal sealed class TextPool
{
    private readonly List<string> _baseStrings;
    private readonly int _duplicateRatioPercent;
    private readonly int _maxWordsPerString;
    private readonly Random _random;

    private readonly string[] _words =
    {
        "Apple", "Banana", "Cherry", "Date", "Elderberry", "Fig", "Grape", "Honeydew",
        "Kiwi", "Lemon", "Mango", "Orange", "Papaya", "Quince", "Raspberry", "Strawberry",
        "Tomato", "Watermelon", "Apricot", "Blueberry", "Cantaloupe", "Dragonfruit",
        "Guava", "Lychee", "Passionfruit", "Pineapple", "Plum", "Pomegranate",
        "Avocado", "Blackberry", "Coconut", "Cranberry", "Grapefruit", "Jackfruit",
        "Kumquat", "Mandarin", "Nectarine", "Peach", "Pear", "Persimmon",
        "Tangerine", "Ugli", "Yuzu", "Zucchini", "Acai", "Acerola",
        "Breadfruit", "Carambola", "Durian", "Feijoa", "Gooseberry", "Huckleberry",
        "Jabuticaba", "Longan", "Mulberry", "Noni", "Olive", "Pomelo",
        "Soursop", "Tamarind", "Vanilla", "Wampee", "Xigua"
    };

    public TextPool(int duplicateRatioPercent, int maxWordsPerString, int? seed = null)
    {
        _duplicateRatioPercent = duplicateRatioPercent;
        _maxWordsPerString = maxWordsPerString;
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
        _baseStrings = new List<string>();

        InitializeBaseStrings();
    }

    private void InitializeBaseStrings()
    {
        for (var i = 0; i < 50; i++)
        {
            var baseText = GenerateRandomText();
            _baseStrings.Add(baseText);
        }
    }

    public string GetNextText()
    {
        // Respect duplicate ratio
        if (_random.Next(100) < _duplicateRatioPercent && _baseStrings.Count > 0)
            // Return an existing string
            return _baseStrings[_random.Next(_baseStrings.Count)];

        // Generate new text
        var newText = GenerateRandomText();

        // Add to pool (with some probability to keep pool size manageable)
        if (_random.Next(100) < 30) // 30% chance to add to pool
            _baseStrings.Add(newText);

        return newText;
    }

    private string GenerateRandomText()
    {
        // Generate 1 to maxWordsPerString words
        var wordCount = _random.Next(1, _maxWordsPerString + 1);
        var selectedWords = new List<string>(wordCount);

        for (var i = 0; i < wordCount; i++) selectedWords.Add(_words[_random.Next(_words.Length)]);

        return string.Join(" ", selectedWords);
    }
}