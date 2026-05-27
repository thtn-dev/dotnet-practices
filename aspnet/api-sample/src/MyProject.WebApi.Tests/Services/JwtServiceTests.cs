using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MyProject.WebApi.Modules.Identity;
using MyProject.WebApi.Modules.Identity.Abstractions;
using MyProject.WebApi.Modules.Identity.Services;
using NSubstitute;

namespace MyProject.WebApi.Tests.Services;

public sealed class JwtServiceTests
{
    [Fact]
    public void GenerateToken_ShouldCreateJwtToken_WithExpectedClaims()
    {
        // Arrange
        using var rsa = RSA.Create(2048);

        var privateKey = new RsaSecurityKey(rsa)
        {
            KeyId = Guid.NewGuid().ToString()
        };

        var publicKey = new RsaSecurityKey(rsa)
        {
            KeyId = privateKey.KeyId
        };

        var keyProvider = Substitute.For<IRsaKeyProvider>();
        keyProvider.PrivateKey.Returns(privateKey);
        keyProvider.PublicKey.Returns(publicKey);

        var logger = Substitute.For<ILogger<JwtService>>();

        var options = Options.Create(new JwtSettings
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            AccessTokenExpiryMinutes = 15
        });

        var jwtService = new JwtService(keyProvider, logger, options);

        // Act
        var token = jwtService.GenerateToken(
            userId: "user-1",
            username: "nathan",
            roles: ["Admin"]);

        // Assert
        token.Should().NotBeNullOrWhiteSpace();

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Issuer.Should().Be("test-issuer");
        jwt.Audiences.Should().Contain("test-audience");
        jwt.Header.Alg.Should().Be(SecurityAlgorithms.RsaSsaPssSha256);

        jwt.Claims.Should().Contain(c =>
            c.Type == JwtRegisteredClaimNames.Sub &&
            c.Value == "user-1");

        jwt.Claims.Should().Contain(c =>
            c.Type == JwtRegisteredClaimNames.UniqueName &&
            c.Value == "nathan");

        jwt.Claims.Should().Contain(c =>
            c.Type == ClaimTypes.Role &&
            c.Value == "Admin");

        jwt.Claims.Should().Contain(c =>
            c.Type == JwtRegisteredClaimNames.Jti);
    }
}