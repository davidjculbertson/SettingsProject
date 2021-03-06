﻿using System.Windows;

#nullable enable

namespace Microsoft.VisualStudio.ProjectSystem.VS.Implementation.PropertyPages.Designer
{
    public partial class SearchBox
    {
        public static readonly DependencyProperty WatermarkTextProperty = DependencyProperty.Register(
            "WatermarkText",
            typeof(string),
            typeof(SearchBox),
            new System.Windows.PropertyMetadata(""));

        public static readonly DependencyProperty SearchTextProperty = DependencyProperty.Register(
            "SearchText",
            typeof(string),
            typeof(SearchBox),
            new System.Windows.PropertyMetadata(""));

        public SearchBox()
        {
            InitializeComponent();
        }

        public string WatermarkText
        {
            get => (string)GetValue(WatermarkTextProperty);
            set => SetValue(WatermarkTextProperty, value);
        }

        public string SearchText
        {
            get => (string)GetValue(SearchTextProperty);
            set => SetValue(SearchTextProperty, value);
        }

        private void OnClearSearchTextClicked(object sender, RoutedEventArgs e)
        {
            SearchText = "";
            _textBox.Focus();
        }
    }
}
