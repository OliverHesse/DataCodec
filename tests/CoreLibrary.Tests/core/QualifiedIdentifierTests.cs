namespace CoreLibrary.Tests;

public class QualifiedIdentifierTests
{
    [Fact]
    public void From_ValidParts_SetsPropertiesAndFormats()
    {
        var id = QualifiedIdentifier.From("game", "stone");

        Assert.Equal("game", id.Namespace);
        Assert.Equal("stone", id.Identifier);
        Assert.Equal("game:stone", id.ToString());
    }

    [Theory]
    [InlineData("", "stone")]
    [InlineData("game", "")]
    [InlineData("ga:me", "stone")]
    [InlineData("game", "sto:ne")]
    public void From_InvalidParts_Throws(string ns, string identifier)
    {
        Assert.Throws<ArgumentException>(() => QualifiedIdentifier.From(ns, identifier));
    }

    [Fact]
    public void From_NullParts_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => QualifiedIdentifier.From(null!, "stone"));
        Assert.Throws<ArgumentNullException>(() => QualifiedIdentifier.From("game", null!));
    }

    [Theory]
    [InlineData(null, "stone")]
    [InlineData("game", null)]
    [InlineData("", "stone")]
    [InlineData("game", "")]
    [InlineData("ga:me", "stone")]
    [InlineData("game", "sto:ne")]
    public void TryFrom_InvalidParts_ReturnsFalse(string? ns, string? identifier)
    {
        Assert.False(QualifiedIdentifier.TryFrom(ns, identifier, out var result));
        Assert.Null(result);
    }

    [Fact]
    public void TryFrom_ValidParts_ReturnsTrue()
    {
        Assert.True(QualifiedIdentifier.TryFrom("game", "stone", out var result));
        Assert.Equal("game:stone", result.ToString());
    }

    [Fact]
    public void Parse_FullForm_Splits()
    {
        var id = QualifiedIdentifier.Parse("game:stone");

        Assert.Equal("game", id.Namespace);
        Assert.Equal("stone", id.Identifier);
    }

    [Fact]
    public void Parse_NoSeparator_UsesDefaultNamespace()
    {
        var id = QualifiedIdentifier.Parse("stone");

        Assert.Equal(QualifiedIdentifier.DefaultNamespace, id.Namespace);
        Assert.Equal("core:stone", id.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData(":stone")]
    [InlineData("game:")]
    [InlineData(":")]
    [InlineData("a:b:c")]
    public void Parse_Invalid_ThrowsFormat(string raw)
    {
        Assert.Throws<FormatException>(() => QualifiedIdentifier.Parse(raw));
    }

    [Fact]
    public void Parse_Null_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => QualifiedIdentifier.Parse(null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(":stone")]
    [InlineData("game:")]
    [InlineData("a:b:c")]
    public void TryParse_Invalid_ReturnsFalse(string? raw)
    {
        Assert.False(QualifiedIdentifier.TryParse(raw, out var result));
        Assert.Null(result);
    }

    [Fact]
    public void TryParse_Valid_ReturnsTrue()
    {
        Assert.True(QualifiedIdentifier.TryParse("game:stone", out var result));
        Assert.Equal(QualifiedIdentifier.From("game", "stone"), result);
    }

    [Fact]
    public void Equality_SameParts_AreEqual()
    {
        var a = QualifiedIdentifier.From("game", "stone");
        var b = QualifiedIdentifier.Parse("game:stone");

        Assert.True(a.Equals(b));
        Assert.True(a.Equals((object)b));
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentParts_AreNotEqual()
    {
        var a = QualifiedIdentifier.From("game", "stone");

        Assert.NotEqual(a, QualifiedIdentifier.From("game", "dirt"));
        Assert.NotEqual(a, QualifiedIdentifier.From("other", "stone"));
        Assert.NotEqual(a, QualifiedIdentifier.From("Game", "Stone"));
        Assert.False(a.Equals(null));
        Assert.False(a == null);
        Assert.True(a != null);
    }

    [Fact]
    public void Equality_BothNull_AreEqual()
    {
        QualifiedIdentifier? a = null;
        QualifiedIdentifier? b = null;

        Assert.True(a == b);
    }

    [Fact]
    public void NamespaceEquals_ComparesNamespaceOnly()
    {
        var id = QualifiedIdentifier.From("game", "stone");

        Assert.True(id.NamespaceEquals("game"));
        Assert.False(id.NamespaceEquals("Game"));
        Assert.False(id.NamespaceEquals((string?)null));
        Assert.True(id.NamespaceEquals(QualifiedIdentifier.From("game", "dirt")));
        Assert.False(id.NamespaceEquals(QualifiedIdentifier.From("other", "stone")));
        Assert.False(id.NamespaceEquals((QualifiedIdentifier?)null));
    }

    [Fact]
    public void IdentifierEquals_ComparesIdentifierOnly()
    {
        var id = QualifiedIdentifier.From("game", "stone");

        Assert.True(id.IdentifierEquals("stone"));
        Assert.False(id.IdentifierEquals("Stone"));
        Assert.False(id.IdentifierEquals((string?)null));
        Assert.True(id.IdentifierEquals(QualifiedIdentifier.From("other", "stone")));
        Assert.False(id.IdentifierEquals(QualifiedIdentifier.From("game", "dirt")));
        Assert.False(id.IdentifierEquals((QualifiedIdentifier?)null));
    }
}
