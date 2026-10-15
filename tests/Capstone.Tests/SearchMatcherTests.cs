using SharpQuest.Contracts;

namespace Capstone.Tests;

public class SearchMatcherTests
{
    private readonly SearchMatcher _matcher = SearchMatcher.Create();

    // R1: blank query matches every non-null text
    [Fact]
    public void Empty_query_matches_any_text()
    {
        Assert.True(_matcher.Matches("Iced Latte", ""),
            "R1: an empty query must match every non-null text");
    }

    [Fact]
    public void Whitespace_only_query_matches_any_text()
    {
        Assert.True(_matcher.Matches("Iced Latte", "   "),
            "R1: a whitespace-only query must match every non-null text");
    }

    // R2: case is ignored
    [Fact]
    public void Uppercase_query_matches_mixed_case_text()
    {
        Assert.True(_matcher.Matches("Iced Latte", "LATTE"),
            "R2: 'LATTE' must match 'Iced Latte' regardless of case");
    }

    [Fact]
    public void Lowercase_query_matches_uppercase_text()
    {
        Assert.True(_matcher.Matches("ICED LATTE", "latte"),
            "R2: 'latte' must match 'ICED LATTE' regardless of case");
    }

    // R3: diacritics are ignored, in both directions
    [Fact]
    public void Query_without_accent_matches_text_with_accent()
    {
        Assert.True(_matcher.Matches("Café Latte", "cafe"),
            "R3: 'cafe' must match 'Café Latte'");
    }

    [Fact]
    public void Query_with_accent_matches_text_without_accent()
    {
        Assert.True(_matcher.Matches("Cafe Latte", "café"),
            "R3: 'café' must match 'Cafe Latte'");
    }

    [Fact]
    public void Romanian_query_without_diacritics_matches_text_with_them()
    {
        Assert.True(_matcher.Matches("Plăcintă cu mere", "placinta"),
            "R3: 'placinta' must match 'Plăcintă cu mere'");
    }

    [Fact]
    public void Romanian_query_with_diacritics_matches_text_without_them()
    {
        Assert.True(_matcher.Matches("Placinta cu mere", "plăcintă"),
            "R3: 'plăcintă' must match 'Placinta cu mere'");
    }

    // R4: every word of the query must occur in the text
    [Fact]
    public void Query_with_a_word_missing_from_the_text_does_not_match()
    {
        Assert.False(_matcher.Matches("Iced Latte", "latte mocha"),
            "R4: 'mocha' is not in the text, so every-word matching must fail");
    }

    [Fact]
    public void Query_with_all_words_present_matches()
    {
        Assert.True(_matcher.Matches("Iced Caramel Latte", "iced latte"),
            "R4: when every word occurs in the text, it matches");
    }

    // R5: word order doesn't matter
    [Fact]
    public void Word_order_in_the_query_does_not_matter()
    {
        Assert.True(_matcher.Matches("Iced Latte", "latte iced"),
            "R5: 'latte iced' must match 'Iced Latte'");
    }

    // R6: a word may occur inside a longer word
    [Fact]
    public void Word_can_occur_at_the_end_of_a_longer_word()
    {
        Assert.True(_matcher.Matches("Cheesecake", "cake"),
            "R6: 'cake' must match inside 'Cheesecake', not only at the start of a word");
    }

    [Fact]
    public void Word_can_occur_in_the_middle_of_a_longer_word()
    {
        Assert.True(_matcher.Matches("Cappuccino", "pucci"),
            "R6: 'pucci' must match in the middle of 'Cappuccino'");
    }

    // Guard against a matcher that says yes to everything
    [Fact]
    public void Unrelated_query_does_not_match()
    {
        Assert.False(_matcher.Matches("Cheesecake", "tea"),
            "'tea' does not occur in 'Cheesecake', so it must not match");
    }

    // R7: null text never matches
    [Fact]
    public void Null_text_never_matches()
    {
        var result = _matcher.Matches(null, "latte");

        Assert.False(result, "R7: a null text must return false, not throw");
    }

    // R8: null query throws ArgumentNullException
    [Fact]
    public void Null_query_throws_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _matcher.Matches("Iced Latte", null!));
    }
}