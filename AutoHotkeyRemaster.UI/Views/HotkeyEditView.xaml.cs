﻿using AutoHotkeyRemaster.WPF.Events;
using AutoHotkeyRemaster.WPF.ViewModels;
using Caliburn.Micro;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace AutoHotkeyRemaster.WPF.Views
{
    /// <summary>
    /// Interaction logic for ProfileEditView.xaml
    /// </summary>
    public partial class HotkeyEditView : UserControl, IHandle<ProfileChangedEvent>, IHandle<ProfileDeletedEvent>
    {
        private HotkeyEditViewModel _hotkeyEditViewModel = null;

        private ToggleButton _currentOnSpecialKey = null;
        private ToggleButton _currentOnSelectKeyButton = null;

        public HotkeyEditView()
        {
            InitializeComponent();

            IoC.Get<IEventAggregator>().SubscribeOnUIThread(this);
        }

        private void OnSelectKeyChecked(object sender, RoutedEventArgs e)
        {
            if (_hotkeyEditViewModel == null)
            {
                _hotkeyEditViewModel = (HotkeyEditViewModel)DataContext;
            }

            var toggleButton = sender as ToggleButton;

            if (_currentOnSelectKeyButton != null && _currentOnSelectKeyButton != toggleButton)
            {
                _currentOnSelectKeyButton.IsChecked = false;
            }

            HotkeyEditViewModel.SelectingTargets target;

            if (toggleButton.Name.Contains(HotkeyEditViewModel.SelectingTargets.ActionKey.ToString()))
            {
                target = HotkeyEditViewModel.SelectingTargets.ActionKey;
            }
            else
            {
                target = HotkeyEditViewModel.SelectingTargets.EndingKey;
            }

            _hotkeyEditViewModel.StartSelectingKey(target);
            _currentOnSelectKeyButton = toggleButton;
        }

        private void OnSelectKeyUnChecked(object sender, RoutedEventArgs e)
        {
            ToggleButton toggleButton = sender as ToggleButton;

            if (_currentOnSelectKeyButton == toggleButton)
            {
                _currentOnSelectKeyButton = null;
                _hotkeyEditViewModel.StopSelectingKey();
            }
        }

        private void OnSpecialKeyUnChecked(object sender, RoutedEventArgs e)
        {
            ToggleButton specialKeyButton = sender as ToggleButton;

            if (_currentOnSpecialKey == specialKeyButton)
                _currentOnSpecialKey = null;
        }

        private void OnSpecialKeyChecked(object sender, RoutedEventArgs e)
        {
            if (_currentOnSpecialKey != null)
                _currentOnSpecialKey.IsChecked = false;

            _currentOnSpecialKey = sender as ToggleButton;
        }

        public Task HandleAsync(ProfileChangedEvent message, CancellationToken cancellationToken)
        {
            if (_currentOnSelectKeyButton != null)
                _currentOnSelectKeyButton.IsChecked = false;

            return Task.CompletedTask;
        }

        public Task HandleAsync(ProfileDeletedEvent message, CancellationToken cancellationToken)
        {
            if (_currentOnSelectKeyButton != null)
                _currentOnSelectKeyButton.IsChecked = false;

            return Task.CompletedTask;
        }
    }
}
