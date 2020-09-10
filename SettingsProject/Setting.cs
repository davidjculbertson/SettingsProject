﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

#nullable enable

namespace SettingsProject
{
    internal sealed class SettingContext
    {
        private readonly Dictionary<SettingIdentity, Setting> _settingByIdentity = new Dictionary<SettingIdentity, Setting>();

        public void AddSetting(Setting setting)
        {
            _settingByIdentity.Add(setting.Identity, setting);
        }

        public Setting GetSetting(in SettingIdentity targetIdentity)
        {
            return _settingByIdentity[targetIdentity];
        }
    }

    internal class Setting : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private bool _isSearchVisible = true;
        private bool _isConditionalVisible = true;
        private List<(SettingIdentity target, object visibleWhenValue)>? _dependentTargets;
        private ImmutableArray<ISettingValue> _values;

        private readonly SettingContext _context;

        protected internal SettingMetadata Metadata { get; }

        public string Name => Metadata.Name;
        public string Page => Metadata.Page;
        public string Category => Metadata.Category;
        public string? Description => Metadata.Description;
        public int Priority => Metadata.Priority;
        public SettingIdentity Identity => Metadata.Identity;
        public ImmutableArray<string> EnumValues => Metadata.EnumValues;
        public bool SupportsPerConfigurationValues => Metadata.SupportsPerConfigurationValues;

        public bool HasDescription => !string.IsNullOrWhiteSpace(Metadata.Description) && Metadata.Editor?.ShouldShowDescription(Values) != false;

        public bool HasPerConfigurationValues => Values.Any(value => value.Configuration != null);

        public ImmutableArray<ISettingValue> Values
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

        public Setting(SettingContext context, string name, string? description, string page, string category, int priority, string editorType, UnconfiguredSettingValue value, ImmutableArray<string>? enumValues = null, bool supportsPerConfigurationValues = false)
            : this(context, new SettingMetadata(name, page, category, description, priority, editorType, supportsPerConfigurationValues, enumValues ?? ImmutableArray<string>.Empty), ImmutableArray.Create<ISettingValue>(value))
        {
        }

        public Setting(SettingContext context, string name, string? description, string page, string category, int priority, string editorType, ImmutableArray<string>? enumValues, ImmutableArray<ConfiguredSettingValue> values)
            : this(context, new SettingMetadata(name, page, category, description, priority, editorType, supportsPerConfigurationValues: true, enumValues ?? ImmutableArray<string>.Empty), values.CastArray<ISettingValue>())
        {
        }

        public Setting(SettingContext context, SettingMetadata metadata, ImmutableArray<ISettingValue> values)
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

            _isSearchVisible = Name.IndexOf(searchString, StringComparison.CurrentCultureIgnoreCase) != -1
                || (Description != null && Description.IndexOf(searchString, StringComparison.CurrentCultureIgnoreCase) != -1)
                || MatchesSearchText(searchString);

            if (wasVisible != IsVisible)
            {
                OnPropertyChanged(nameof(IsVisible));
            }

            bool MatchesSearchText(string searchString)
            {
                foreach (var enumValue in Metadata.EnumValues)
                {
                    if (enumValue.IndexOf(searchString, StringComparison.CurrentCultureIgnoreCase) != -1)
                        return true;
                }

                return false;
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public virtual Setting Clone(SettingContext context) => new Setting(context, Metadata, Values.Select(value => value.Clone()).ToImmutableArray()) { _dependentTargets = _dependentTargets };
    }

    internal sealed class LinkAction : Setting
    {
        public LinkAction(SettingContext context, string name, string? description, string page, string category, int priority)
            : base(context, new SettingMetadata(name, page, category, description, priority, null, supportsPerConfigurationValues: false, ImmutableArray<string>.Empty), ImmutableArray<ISettingValue>.Empty)
        {
        }

        private LinkAction(SettingContext context, SettingMetadata metadata)
            : base(context, metadata, ImmutableArray<ISettingValue>.Empty)
        {
        }

        public string HeadingText => Description != null ? Name : "";
        
        public string LinkText => Description ?? Name;

        public override Setting Clone(SettingContext context) => new LinkAction(context, Metadata);
    }
}
