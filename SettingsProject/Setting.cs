﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

#nullable enable

namespace SettingsProject
{
    internal sealed class Setting : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private bool _isSearchVisible = true;
        private bool _isConditionalVisible = true;
        private List<(SettingIdentity target, object visibleWhenValue)>? _dependentTargets;
        private ImmutableArray<SettingValue> _values;

        private readonly SettingContext _context;

        internal SettingMetadata Metadata { get; }

        public string Name => Metadata.Name;
        public string Page => Metadata.Page;
        public string Category => Metadata.Category;
        public string? Description => Metadata.Description;
        public int Priority => Metadata.Priority;
        public SettingIdentity Identity => Metadata.Identity;
        public bool SupportsPerConfigurationValues => Metadata.SupportsPerConfigurationValues;

        public bool HasPerConfigurationValues => Values.Any(value => !value.ConfigurationDimensions.IsEmpty);

        public ImmutableArray<SettingValue> Values
        {
            get => _values;
            set
            {
                _values = value;
                
                foreach (var settingValue in value)
                {
                    settingValue.Parent = this;
                    settingValue.PropertyChanged += OnSettingValuePropertyChanged;

                    void OnSettingValuePropertyChanged(object _, PropertyChangedEventArgs e)
                    {
                        if (e.PropertyName == nameof(SettingValue.Value))
                        {
                            UpdateDependentVisibilities();
                        }
                    }
                }

                OnPropertyChanged();
                OnPropertyChanged(nameof(HasPerConfigurationValues));
            }
        }

        public bool IsVisible => _isSearchVisible && _isConditionalVisible;

        public Setting(SettingContext context, SettingMetadata metadata, SettingValue value)
            : this(context, metadata, ImmutableArray.Create(value))
        {
        }

        public Setting(SettingContext context, SettingMetadata metadata, ImmutableArray<SettingValue> values)
        {
            _context = context;
            Metadata = metadata;
            Values = values;

            _context.AddSetting(this);
        }

        internal void UpdateDependentVisibilities()
        {
            // TODO model this as a graph with edges so that multiple upstream properties may influence a single downstream one

            if (_dependentTargets == null)
            {
                return;
            }

            foreach (var (targetIdentity, visibleWhenValue) in _dependentTargets)
            {
                var target = _context.GetSetting(targetIdentity);
                var wasVisible = target.IsVisible;

                bool isConditionallyVisible = false;

                // Target is visible if any upstream value matches
                foreach (var value in Values)
                {
                    if (Equals(visibleWhenValue, value.Value))
                    {
                        isConditionallyVisible = true;
                        break;
                    }
                }

                target._isConditionalVisible = isConditionallyVisible;

                if (wasVisible != target.IsVisible)
                {
                    target.OnPropertyChanged(nameof(IsVisible));
                }
            }
        }

        public void AddDependentTarget(SettingIdentity target, object visibleWhenValue)
        {
            _dependentTargets ??= new List<(SettingIdentity target, object visibleWhenValue)>();

            _dependentTargets.Add((target, visibleWhenValue));

            UpdateDependentVisibilities();
        }

        public void UpdateSearchState(string searchString)
        {
            var wasVisible = IsVisible;

            _isSearchVisible = MatchesSearchText(searchString);

            if (wasVisible != IsVisible)
            {
                OnPropertyChanged(nameof(IsVisible));
            }

            bool MatchesSearchText(string searchString)
            {
                if (Name.IndexOf(searchString, StringComparison.CurrentCultureIgnoreCase) != -1)
                    return true;

                if (Description != null && Description.IndexOf(searchString, StringComparison.CurrentCultureIgnoreCase) != -1)
                    return true;

                foreach (var value in _values)
                {
                    foreach (var enumValue in value.EnumValues)
                    {
                        if (enumValue.IndexOf(searchString, StringComparison.CurrentCultureIgnoreCase) != -1)
                            return true;
                    }
                }

                foreach (var searchTerm in Metadata.SearchTerms)
                {
                    if (searchTerm.IndexOf(searchString, StringComparison.CurrentCultureIgnoreCase) != -1)
                        return true;
                }

                // TODO search evaluated/unevaluated values too

                return false;
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Setting Clone(SettingContext context) => new Setting(context, Metadata, Values.Select(value => value.Clone()).ToImmutableArray()) { _dependentTargets = _dependentTargets };
    }
}
