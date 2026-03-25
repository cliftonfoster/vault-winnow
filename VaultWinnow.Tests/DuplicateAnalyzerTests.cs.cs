using System.Collections.Generic;
using VaultWinnow;
using VaultWinnow.Models;
using Xunit;

namespace VaultWinnow.Tests
{
    public class DuplicateAnalyzerTests
    {
        [Fact]
        public void AnalyzeDuplicates_Marks_Strict_For_Identical_Logins()
        {
            // Arrange: two identical login items on same host
            var items = new List<VaultItem>
            {
                new VaultItem
                {
                    Name = "Example",
                    Type = 1,
                    Login = new VaultLogin
                    {
                        Username = "user@example.com",
                        Password = "P@ssw0rd",
                        Uris = new List<VaultUri>
                        {
                            new VaultUri { Uri = "https://example.com/login" }
                        },
                        Totp = string.Empty  // explicitly no TOTP
                    },
                    Notes = "Some notes"

                },
                new VaultItem
                {
                    Name = "Example",
                    Type = 1,
                    Login = new VaultLogin
                    {
                        Username = "user@example.com",
                        Password = "P@ssw0rd",
                        Uris = new List<VaultUri>
                        {
                            new VaultUri { Uri = "https://example.com/login" }
                        },
                        Totp = string.Empty  // explicitly no TOTP
                    },
                    Notes = "Some notes"
                }

            };

            // Act
            DuplicateAnalyzer.AnalyzeDuplicates(items);

            // Assert
            Assert.Equal(DuplicateStatus.Strict, items[0].DuplicateStatus);
            Assert.Equal(DuplicateStatus.Strict, items[1].DuplicateStatus);
            Assert.Equal(2, items[0].DuplicateGroupSize);
            Assert.Equal(2, items[1].DuplicateGroupSize);
            Assert.NotEqual(0, items[0].DuplicateGroupId);
            Assert.Equal(items[0].DuplicateGroupId, items[1].DuplicateGroupId);
        }

        [Fact]
        public void AnalyzeDuplicates_Marks_Almost_When_Name_Differs()
        {
            // Arrange: two logins identical except for Name
            var items = new List<VaultItem>
    {
        new VaultItem
        {
            Type = 1, // Login
            Name = "Example A",
            Login = new VaultLogin
            {
                Username = "user@example.com",
                Password = "P@ssw0rd",
                Totp = string.Empty,
                Uris = new List<VaultUri>
                {
                    new VaultUri { Uri = "https://example.com/login" }
                }
            },
            Notes = "Some notes"
        },
        new VaultItem
        {
            Type = 1, // Login
            Name = "Example B", // different name
            Login = new VaultLogin
            {
                Username = "user@example.com",
                Password = "P@ssw0rd",
                Totp = string.Empty,
                Uris = new List<VaultUri>
                {
                    new VaultUri { Uri = "https://example.com/login" }
                }
            },
            Notes = "Some notes"
        }
    };

            // Act
            DuplicateAnalyzer.AnalyzeDuplicates(items);

            // Assert
            Assert.Equal(DuplicateStatus.Almost, items[0].DuplicateStatus);
            Assert.Equal(DuplicateStatus.Almost, items[1].DuplicateStatus);
            Assert.Equal(items[0].DuplicateGroupId, items[1].DuplicateGroupId);
            Assert.True(items[0].DuplicateGroupSize >= 2);
            Assert.True(items[1].DuplicateGroupSize >= 2);
        }

    }
}
