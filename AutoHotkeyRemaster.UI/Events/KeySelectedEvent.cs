﻿using AutoHotkeyRemaster.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoHotkeyRemaster.UI.Events
{
    public class KeySelectedEvent
    {
        public bool IsNew { get; set; }
        public Hotkey Hotkey { get; set; }
    }
}