using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace ExcelMerge.GUI
{
    internal static class CollectionExtensions
    {
        public static IEnumerable<List<KeyValuePair<TKey, TValue>>> SplitByRegularity<TKey, TValue>(
            this IEnumerable<KeyValuePair<TKey, TValue>> source,
            Func<List<KeyValuePair<TKey, TValue>>, KeyValuePair<TKey, TValue>, bool> condition)
        {
            var list = new List<KeyValuePair<TKey, TValue>>();
            foreach (var item in source)
            {
                if (list.Any() && !condition(list, item))
                {
                    yield return list;
                    list = new List<KeyValuePair<TKey, TValue>>();
                }
                list.Add(item);
            }
            if (list.Any())
                yield return list;
        }

        public static IEnumerable<List<T>> SplitByRegularity<T>(
            this IEnumerable<T> source,
            Func<List<T>, T, bool> condition)
        {
            var list = new List<T>();
            foreach (var item in source)
            {
                if (list.Any() && !condition(list, item))
                {
                    yield return list;
                    list = new List<T>();
                }
                list.Add(item);
            }
            if (list.Any())
                yield return list;
        }

        public static IEnumerable<T> GetAllKeyedServices<T>(this IServiceProvider provider, params string[] keys)
        {
            foreach (var key in keys)
                foreach (var item in provider.GetKeyedServices<T>(key))
                    yield return item;
        }
    }

    internal static class WpfExtensions
    {
        public static void ClearChildren<T>(this Panel panel, List<UIElement> excludes) where T : UIElement
        {
            var toRemove = panel.Children.OfType<T>().Except(excludes).ToList();
            foreach (var child in toRemove)
                panel.Children.Remove(child);
        }

        public static int? GetRow(this Grid grid, Point position)
        {
            if (!grid.RowDefinitions.Any())
                return null;

            var height = 0.0;
            for (int i = 0; i < grid.RowDefinitions.Count; i++)
            {
                height += grid.RowDefinitions[i].ActualHeight;
                if (position.Y <= height)
                    return i;
            }
            return null;
        }

        public static int? GetColumn(this Grid grid, Point position)
        {
            if (!grid.ColumnDefinitions.Any())
                return null;

            var width = 0.0;
            for (int i = 0; i < grid.ColumnDefinitions.Count; i++)
            {
                width += grid.ColumnDefinitions[i].ActualWidth;
                if (position.X <= width)
                    return i;
            }
            return null;
        }
    }
}
