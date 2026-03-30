using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Data;
using VaultWinnow;
using VaultWinnow.Models;
using Xunit;

namespace VaultWinnow.Tests
{
    public class SelectionHelperTests
    {
        private static ListCollectionView CreateView(IEnumerable<VaultItem> items)
        {
            return new ListCollectionView(new List<VaultItem>(items));
        }

        [Fact]
        public void SelectAll_SelectsAllVisibleItems()
        {
            // Arrange
            var item1 = new VaultItem { Name = "One", IsSelected = false };
            var item2 = new VaultItem { Name = "Two", IsSelected = false };
            var items = new List<VaultItem> { item1, item2 };

            var view = CreateView(items);

            // Act
            SelectionHelper.SelectAll(view);

            // Assert
            Assert.True(item1.IsSelected);
            Assert.True(item2.IsSelected);
        }

        [Fact]
        public void ClearSelection_ClearsSelectionForAllVisibleItems()
        {
            // Arrange
            var item1 = new VaultItem { Name = "One", IsSelected = true };
            var item2 = new VaultItem { Name = "Two", IsSelected = true };
            var items = new List<VaultItem> { item1, item2 };

            var view = CreateView(items);

            // Act
            SelectionHelper.ClearSelection(view);

            // Assert
            Assert.False(item1.IsSelected);
            Assert.False(item2.IsSelected);
        }

        [Fact]
        public void InvertSelection_TogglesSelectionForAllVisibleItems()
        {
            // Arrange
            var item1 = new VaultItem { Name = "One", IsSelected = true };
            var item2 = new VaultItem { Name = "Two", IsSelected = false };
            var items = new List<VaultItem> { item1, item2 };

            var view = CreateView(items);

            // Act
            SelectionHelper.InvertSelection(view);

            // Assert
            Assert.False(item1.IsSelected);  // was true
            Assert.True(item2.IsSelected);   // was false
        }

    }
}