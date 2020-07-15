﻿using AutoHotkeyRemaster.Models;
using AutoHotkeyRemaster.UI.Events;
using AutoHotkeyRemaster.UI.Helpers;
using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace AutoHotkeyRemaster.UI.ViewModels
{
    public class KeyboardViewModel : Screen, IHandle<ProfileChangedEvent>, IHandle<HotkeyModifiedEvent>, IHandle<ProfileDeletedEvent>
    {
        public event EventHandler OnProfileChanged;

        private readonly IEventAggregator _eventAggregator;

        private HotkeyProfile _profile = null;

        public HotkeyProfile Profile
        {
            get { return _profile; }
            set
            {
                _profile = value;
                NotifyOfPropertyChange(() => Profile);
            }
        }


        //For optimization
        public Dictionary<int, Hotkey> TriggerHotkeyPairs { get; set; } = new Dictionary<int, Hotkey>();

        //Key 눌렀을 때 이벤트를 발생시키기 위해
        public KeyboardViewModel(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            _eventAggregator.SubscribeOnUIThread(this);
        }


        private void SetTriggerHotkeyPairs(HotkeyProfile profile)
        {
            TriggerHotkeyPairs.Clear();

            foreach (var hotkey in profile.Hotkeys)
            {
                TriggerHotkeyPairs.Add(hotkey.Trigger.Key, hotkey);
            }
        }

        private ToggleButton _selectedButton = null;

        public async void OnKeyClick(object sender, RoutedEventArgs e)
        {
            //HACK : 나중에 tag 바꿔야 되니까 얘만 캐시해둔다.
            _selectedButton = sender as ToggleButton;
            int keycode = KeyConversionHelper.ExtractFromElementName(_selectedButton.Name);

            Hotkey hotkey;
            bool hasHotkey = TriggerHotkeyPairs.ContainsKey(keycode);

            if (hasHotkey)
            {
                hotkey = TriggerHotkeyPairs[keycode];
            }
            else
            {
                hotkey = new Hotkey(new KeyInfo(keycode, 0), new KeyInfo());
            }

            await _eventAggregator.PublishOnUIThreadAsync(new KeySelectedEvent { Hotkey = hotkey, IsNew = !hasHotkey });
        }

        public Task HandleAsync(ProfileChangedEvent message, CancellationToken cancellationToken)
        {
            //HACK : Profiel = message.Profile가 먼저 실행되면 왜인지는 모르겠으나, OnProfileChanged가 먼저 fire되면서
            //다소 이상한 동작을 함.
            SetTriggerHotkeyPairs(message.Profile);
            OnProfileChanged?.Invoke(this, null);

            Profile = message.Profile;

            return Task.CompletedTask;
        }

        public Task HandleAsync(HotkeyModifiedEvent message, CancellationToken cancellationToken)
        {
            if (message.ModifiedEvent == EHotkeyModifiedEvent.Added)
            {
                _selectedButton.Tag = "True";
                TriggerHotkeyPairs.Add(message.Hotkey.Trigger.Key, message.Hotkey);                
            }

            return Task.CompletedTask;
        }

        public Task HandleAsync(ProfileDeletedEvent message, CancellationToken cancellationToken)
        {
            Profile = null;

            return Task.CompletedTask;
        }
    }
}