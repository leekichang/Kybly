﻿using AutoHotkeyRemaster.Models;
using AutoHotkeyRemaster.Services.Events;
using AutoHotkeyRemaster.Services.Helpers;
using Caliburn.Micro;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace AutoHotkeyRemaster.Services
{
    public enum HookState
    {
        Hooking,   //A profile is being hooked
        UnHooking, //Only activation key is enabled
    }

    /// <summary>
    /// Register keys to WindowsHookhandler. 
    /// Process hotkey is triggerd by WindowsHookhandler
    /// </summary>
    public class WindowsHookManager
    {
        public HookState HookState { get; set; } = HookState.UnHooking;

        private readonly IEventAggregator _eventAggregator;
        private readonly ApplicationModel _applicationModel;
        private readonly ProfileManager _profileManager;
        private readonly ProfileSwitchKeyTableManager _profileSwitchKeyTable;
        private readonly WindowsKeyboardHooker _keyboardHooker = new WindowsKeyboardHooker();
        private readonly Options _options;

        private int _activationKey => _applicationModel.Options.ActivationKey;
        private readonly Dictionary<int, int> _swtichKeys = new Dictionary<int, int>(); //Key : switch keycode, Value : profile to switch
        private readonly Dictionary<int, Hotkey> _profileHotkeys = new Dictionary<int, Hotkey>();  //Key : trigger, Value : Hotkey

        private HotkeyProfile _currentHookingProfile = null;
        private int _lastHookedProfileNum = -1;

        public WindowsHookManager(IEventAggregator eventAggregator,
            ApplicationModel applicationModel, ProfileManager profileManager, ProfileSwitchKeyTableManager profileSwitchKeyTable)
        {
            _eventAggregator = eventAggregator;
            _applicationModel = applicationModel;
            _profileManager = profileManager;
            _profileSwitchKeyTable = profileSwitchKeyTable;

            _applicationModel.ActivationKeyChange += OnActivationKeyChanged;
            _keyboardHooker.KeyHooked += HandleHookedEventAsync;
            _options = _applicationModel.Options;

            _keyboardHooker.StartHook(_activationKey, null);
        }

        //Used to clean all key strokes. Useful in case of unexpected bugs
        public void Shutdown()
        {
            _keyboardHooker.StopHook();
            _keyboardHooker.KeyHooked -= HandleHookedEventAsync;
            _applicationModel.ActivationKeyChange -= OnActivationKeyChanged;
            //For sure
            InputSimlationHelper.UpAllModifiers();
            InputSimlationHelper.UpAllModifiers();
        }

        public void PauseHook()
        {
            _keyboardHooker.StopHookKeyboard();
        }

        public void ResumeHook()
        {
            _keyboardHooker.StartHookKeyboard();
        }

        private async Task ChangeHookStateAsync()
        {
            if (HookState == HookState.UnHooking)
            {
                HookState = HookState.Hooking;

                await ActivateAsync(_lastHookedProfileNum);

                return;
            }

            HookState = HookState.UnHooking;

            await DeactivateAsync();

            return;
        }

        /// <summary>
        /// On the first activation, activates default profile. (profile1)
        /// </summary>
        /// <returns>Activated profile</returns>
        private async Task ActivateAsync(int profileNum)
        {
            _currentHookingProfile = _profileManager.FindProfileOrDefault(profileNum);

            var switchKeys = _profileSwitchKeyTable.SwitchKeyTable[_currentHookingProfile.ProfileNum - 1];

            for (int i = 1; i <= _profileManager.ProfileCount; i++)
            {
                if (i == _currentHookingProfile.ProfileNum)
                    continue;

                _swtichKeys.Add(switchKeys[i - 1], i);
            }

            foreach (var hotkey in _currentHookingProfile.Hotkeys)
            {
                _profileHotkeys.Add(hotkey.Trigger.Key, hotkey);
            }

            var registeredKeys = CreateRegisteredKeyDictionary();

            _keyboardHooker.StartHook(registeredKeys);

            await _eventAggregator.PublishOnUIThreadAsync(new HookStateChangeEvent
            {
                HookState = HookState.Hooking,
                HotkeyProfile = _currentHookingProfile
            });
        }

        private async Task DeactivateAsync()
        {
            _keyboardHooker.StopHook();

            _lastHookedProfileNum = _currentHookingProfile.ProfileNum;

            //Delete cache for case where user modified profile hotkeys while deactivated
            _profileHotkeys.Clear();
            _swtichKeys.Clear();
            _currentHookingProfile = null;

            //Only enables activation key            
            _keyboardHooker.StartHook(_activationKey, null);

            await _eventAggregator.PublishOnUIThreadAsync(new HookStateChangeEvent
            {
                HookState = HookState.UnHooking,
            });
        }

        private Dictionary<int, KeyInfo> CreateRegisteredKeyDictionary()
        {
            Dictionary<int, KeyInfo> registeredKeys = new Dictionary<int, KeyInfo>();

            //Registraion order : activation key -> switichkey -> profile hotkeys
            registeredKeys.Add(_applicationModel.Options.ActivationKey, null);

            foreach (var key in _swtichKeys.Keys)
            {
                if (!registeredKeys.ContainsKey(key)) registeredKeys.Add(key, null);
            }

            foreach (var trigger in _profileHotkeys.Keys)
            {
                if (!registeredKeys.ContainsKey(trigger)) registeredKeys.Add(trigger, _profileHotkeys[trigger].Action);
            }

            return registeredKeys;
        }

        private async Task SwtichProfileAsync(int toProfileNum)
        {
            await DeactivateAsync();
            await ActivateAsync(toProfileNum);
        }

        private void ProcessHotkeyAction(Hotkey hotkey, bool isDown)
        {
            _keyboardHooker.StopHookKeyboard();

            Debug.WriteLine($"trigger : {hotkey.Trigger}  |  action : {hotkey.Action}");

            if (isDown)
            {
                InputSimlationHelper.DownKey(hotkey.Action, _options.MouseDownDelayMiliseconds);
            }
            else
            {
                InputSimlationHelper.UpKey(hotkey.Action, _options.MouseUpDelayMiliseconds);

                if (hotkey.EndingAction != null)
                {
                    InputSimlationHelper.PressKey(hotkey.EndingAction);
                }
            }

            _keyboardHooker.StartHookKeyboard();
        }

        //As this is kinda like a event handler, allow this to be async void
        private async void HandleHookedEventAsync(KeyHookedArgs args)
        {
            int keycode = args.VkCode;

            if (args.IsPressed)
            {
                if (keycode == _activationKey)
                {
                    if (_profileManager.ProfileCount != 0)
                    {
                        await ChangeHookStateAsync();
                    }

                    return;
                }

                if (_swtichKeys.ContainsKey(keycode))
                {
                    await SwtichProfileAsync(_swtichKeys[keycode]);

                    return;
                }

                ProcessHotkeyAction(_profileHotkeys[keycode], true);

                return;
            }

            if (keycode == _activationKey || _swtichKeys.ContainsKey(keycode))
                return;

            ProcessHotkeyAction(_profileHotkeys[keycode], false);
        }
        private void OnActivationKeyChanged()
        {
            //Changing activation key means that current state is UnHooking
            _keyboardHooker.StopHook();
            _keyboardHooker.StartHook(_activationKey, null);
        }
    }
}
