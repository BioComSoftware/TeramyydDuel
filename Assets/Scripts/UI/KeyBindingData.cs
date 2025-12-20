using System;
using UnityEngine;

namespace Teramyyd.UI
{
    /// <summary>
    /// Represents a keybinding with its key and modifier keys (Ctrl, Shift, Alt)
    /// </summary>
    [Serializable]
    public struct KeyBindingData
    {
        public KeyCode key;
        public bool ctrl;
        public bool shift;
        public bool alt;

        public bool Equals(KeyBindingData other)
        {
            return key == other.key && ctrl == other.ctrl && shift == other.shift && alt == other.alt;
        }

        public override string ToString()
        {
            string result = "";
            if (ctrl) result += "Ctrl+";
            if (shift) result += "Shift+";
            if (alt) result += "Alt+";
            result += key.ToString();
            return result;
        }
    }
}
