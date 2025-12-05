namespace FileSort.Generator;

/// <summary>
/// Manages a pool of base strings and generates variations for test file generation.
/// </summary>
internal sealed class TextPool
{
    private readonly List<string> _baseStrings;
    private readonly Random _random;
    private readonly int _duplicateRatioPercent;
    private readonly string[] _words = new[]
    {
        "Apple", "Banana", "Cherry", "Date", "Elderberry", "Fig", "Grape", "Honeydew",
        "Kiwi", "Lemon", "Mango", "Orange", "Papaya", "Quince", "Raspberry", "Strawberry",
        "Tomato", "Watermelon", "Apricot", "Blueberry", "Cantaloupe", "Dragonfruit",
        "Guava", "Lychee", "Passionfruit", "Pineapple", "Plum", "Pomegranate"
    };

    public TextPool(int duplicateRatioPercent, int? seed = null)
    {
        _duplicateRatioPercent = duplicateRatioPercent;
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
        _baseStrings = new List<string>();

        // Initialize with some base strings
        InitializeBaseStrings();
    }

    private void InitializeBaseStrings()
    {
        // Create initial base strings
        for (int i = 0; i < 50; i++)
        {
            string baseText = GenerateRandomText();
            _baseStrings.Add(baseText);
        }
    }

    public string GetNextText()
    {
        // Respect duplicate ratio
        if (_random.Next(100) < _duplicateRatioPercent && _baseStrings.Count > 0)
        {
            // Return an existing string
            return _baseStrings[_random.Next(_baseStrings.Count)];
        }

        // Generate new text
        string newText = GenerateRandomText();
        
        // Add to pool (with some probability to keep pool size manageable)
        if (_random.Next(100) < 30) // 30% chance to add to pool
        {
            _baseStrings.Add(newText);
        }

        return newText;
    }

    private string GenerateRandomText()
    {
        // Generate 1-4 words
        int wordCount = _random.Next(1, 5);
        var selectedWords = new List<string>(wordCount);

        for (int i = 0; i < wordCount; i++)
        {
            selectedWords.Add(_words[_random.Next(_words.Length)]);
        }

        return string.Join(" ", selectedWords);
    }
}
