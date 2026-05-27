using BuildingBlocks.SharedKernel.Common;
using FluentAssertions;

namespace BuildingBlocks.UnitTests.ValueObject;

public sealed class EmailAddressTests
{
    [Theory]
    [InlineData("test@example.com")]
    [InlineData("   user.name@example.com  ")]
    [InlineData("user+tag@example.com")]
    [InlineData("user_Name@example.CO.uk")]
    [InlineData("user-namE@example-domain.com   ")]
    public void Create_ShouldReturnEmailAddress_WhenEmailIsValid(string email)
    {
        // Act
        var emailAddress = EmailAddress.Create(email);
        
        // Assert
        emailAddress.Should().NotBeNull();
        emailAddress.Value.Should().Be(email.ToLowerInvariant().Trim());
    }

    [Fact]
    public void Create_ShouldExposeDomainPart()
    {
        // Arrange
        var email = EmailAddress.Create("thtntrungnam@GMAIL.com");
        
        // Act
        var domain = email.Domain;
        
        // Assert
        email.Domain.Should().Be("gmail.com");
    }
    
    [Fact]
    public void Create_ShouldExposeLocalPart()
    {
        // Arrange
        var emailAddress = EmailAddress.Create("test@example.com");

        // Assert
        emailAddress.LocalPart.Should().Be("test");
    }
    
    [Fact]
    public void ToString_ShouldReturnEmailValue()
    {
        // Arrange
        var emailAddress = EmailAddress.Create("test@example.com");

        // Act
        var result = emailAddress.ToString();

        // Assert
        result.Should().Be("test@example.com");
    }
    
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("invalid-email")]
    [InlineData("test@")]
    [InlineData("@example.com")]
    [InlineData("test@example")]
    [InlineData("test@@example.com")]
    [InlineData("test@example..com")]
    [InlineData("test@.com")]
    [InlineData("test@example.")]
    [InlineData(null)]
    public void Create_ShouldThrowArgumentException_WhenEmailIsInvalid(string? email)
    {
        // Act
        var act = () => EmailAddress.Create(email);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithMessage($"Invalid email address: {email}*")
            .WithParameterName(nameof(email));
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenEmailExceedsMaxLength()
    {
        // Arrange
        var localPart = new string('a', 64);
        var domainPart = string.Join(".", new string('c', 60), new string('c', 60), new string('c', 60), new string('c', 60));
        var email = $"{localPart}@{domainPart}";
        // Act
        var act = () => EmailAddress.Create(email);
        
        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithMessage($"Invalid email address: {email}*")
            .WithParameterName(nameof(email));
    }
    
    [Fact]
    public void Create_ShouldThrowArgumentException_WhenLocalPartExceeds64Characters()
    {
        // Arrange
        var localPart = new string('a', 65);
        var email = $"{localPart}@example.com";

        // Act
        var act = () => EmailAddress.Create(email);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("USER+TAG@example.com")]
    [InlineData("test@example.com")]
    [InlineData("   user.name@example.com  ")]
    [InlineData("user+tag@example.com")]
    [InlineData("user_Name@example.CO.uk")]
    [InlineData("user-namE@example-domain.com   ")]
    public void TryCreate_ShouldReturnTrue_WhenEmailIsValid(string email)
    {
        // Act
        var isValidEmail = EmailAddress.TryCreate(email, out var emailAddress);
        
        // Assert
        isValidEmail.Should().BeTrue();
        emailAddress.Should().NotBeNull();
        emailAddress!.Value.Should().Be(email.Trim().ToLowerInvariant());
    }
    
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("invalid-email")]
    [InlineData("test@")]
    [InlineData("@example.com")]
    [InlineData("test@example")]
    [InlineData(null)]
    public void TryCreate_ShouldReturnFalse_WhenEmailIsInvalid(string? email)
    {
        // Act
        var result = EmailAddress.TryCreate(email, out var emailAddress);

        // Assert
        result.Should().BeFalse();
        emailAddress.Should().BeNull();
    }
    
    [Fact]
    public void ImplicitStringOperator_ShouldReturnEmailValue()
    {
        // Arrange
        var emailAddress = EmailAddress.Create("TEST@example.com");

        // Act
        string value = emailAddress;

        // Assert
        value.Should().Be("test@example.com");
    }

    [Fact]
    public void ExplicitEmailAddressOperator_ShouldCreateEmailAddress()
    {
        // Act
        var emailAddress = (EmailAddress)"TEST@Example.com";

        // Assert
        emailAddress.Value.Should().Be("test@example.com");
    }
}