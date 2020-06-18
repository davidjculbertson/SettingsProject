﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

#nullable enable

namespace SettingsProject
{
    internal class HighlightTextBlock : TextBlock
    {
        static HighlightTextBlock()
        {
            TextProperty.OverrideMetadata(
                typeof(HighlightTextBlock),
                new FrameworkPropertyMetadata("", null, (d, e) => ((HighlightTextBlock)d).OnCoerceTextPropertyValue(e)));
        }

        public static readonly DependencyProperty HighlightTextProperty = DependencyProperty.Register(
            nameof(HighlightText),
            typeof(string),
            typeof(HighlightTextBlock),
            new PropertyMetadata("", (d, e) => ((HighlightTextBlock)d).OnHighlightTextChanged(e.NewValue)));

        public static readonly DependencyProperty HighlightBrushProperty = DependencyProperty.Register(
            nameof(HighlightBrush),
            typeof(Brush),
            typeof(HighlightTextBlock),
            new PropertyMetadata(Brushes.Yellow));

        public string HighlightText
        {
            get => (string)GetValue(HighlightTextProperty);
            set => SetValue(HighlightTextProperty, value);
        }

        public Brush HighlightBrush
        {
            get => (Brush)GetValue(HighlightBrushProperty);
            set => SetValue(HighlightBrushProperty, value);
        }

        private bool _isUpdating;

        private object OnCoerceTextPropertyValue(object baseValue)
        {
            if (_isUpdating)
            {
                return string.Empty;
            }

            var highlightText = HighlightText;

            if (!string.IsNullOrWhiteSpace(highlightText))
            {
                if (baseValue is string s && !string.IsNullOrWhiteSpace(s))
                {
                    highlightText = highlightText.Trim();

                    int searchIndex = 0;
                    
                    int GetNextMatchIndex() => s.IndexOf(highlightText, searchIndex, StringComparison.CurrentCultureIgnoreCase);

                    var matchIndex = GetNextMatchIndex();

                    if (matchIndex == -1)
                    {
                        return baseValue;
                    }

                    var highlightBrush = HighlightBrush;

                    _isUpdating = true;

                    Inlines.Clear();

                    while (matchIndex != -1)
                    {
                        Inlines.Add(new Run(s.Substring(searchIndex, matchIndex - searchIndex)));
                        Inlines.Add(new Run(s.Substring(matchIndex, highlightText.Length)) { Background = highlightBrush });
                        searchIndex = matchIndex + highlightText.Length;
                        matchIndex = GetNextMatchIndex();
                    }

                    if (searchIndex < s.Length)
                    {
                        Inlines.Add(new Run(s.Substring(searchIndex)));
                    }

                    _isUpdating = false;
                }
            }

            return baseValue;
        }

        private void OnHighlightTextChanged(object newValue)
        {
            InvalidateProperty(TextProperty);
        }
    }
}