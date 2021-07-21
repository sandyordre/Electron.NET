﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ElectronNET.API
{
    /// <summary>
    /// Detect keyboard events when the application does not have keyboard focus.
    /// </summary>
    public sealed class GlobalShortcut
    {
        private static GlobalShortcut _globalShortcut;
        private static object _syncRoot = new object();

        internal GlobalShortcut() { }

        internal static GlobalShortcut Instance
        {
            get
            {
                if (_globalShortcut == null)
                {
                    lock (_syncRoot)
                    {
                        if (_globalShortcut == null)
                        {
                            _globalShortcut = new GlobalShortcut();
                        }
                    }
                }

                return _globalShortcut;
            }
        }

        private Dictionary<string, Action> _shortcuts = new Dictionary<string, Action>();

        /// <summary>
        /// Registers a global shortcut of accelerator. 
        /// The callback is called when the registered shortcut is pressed by the user.
        /// 
        /// When the accelerator is already taken by other applications, this call will 
        /// silently fail.This behavior is intended by operating systems, since they don’t
        /// want applications to fight for global shortcuts.
        /// </summary>
        public void Register(string accelerator, Action function)
        {
            if (!_shortcuts.ContainsKey(accelerator))
            {
                _shortcuts.Add(accelerator, function);

                BridgeConnector.Off("globalShortcut-pressed");
                BridgeConnector.On<string>("globalShortcut-pressed", (shortcut) =>
                {
                    if (_shortcuts.ContainsKey(shortcut))
                    {
                        _shortcuts[shortcut.ToString()]();
                    }
                });

                BridgeConnector.Emit("globalShortcut-register", accelerator);
            }
        }

        /// <summary>
        /// When the accelerator is already taken by other applications, 
        /// this call will still return false. This behavior is intended by operating systems,
        /// since they don’t want applications to fight for global shortcuts.
        /// </summary>
        /// <returns>Whether this application has registered accelerator.</returns>
        public Task<bool> IsRegisteredAsync(string accelerator)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            
            BridgeConnector.On<bool>("globalShortcut-isRegisteredCompleted" + accelerator.GetHashCode(), (isRegistered) =>
            {
                BridgeConnector.Off("globalShortcut-isRegisteredCompleted" + accelerator.GetHashCode());

                taskCompletionSource.SetResult(isRegistered);
            });

            BridgeConnector.Emit("globalShortcut-isRegistered", accelerator);

            return taskCompletionSource.Task;
        }

        /// <summary>
        /// Unregisters the global shortcut of accelerator.
        /// </summary>
        public void Unregister(string accelerator)
        {
            _shortcuts.Remove(accelerator);
            BridgeConnector.Emit("globalShortcut-unregister", accelerator);
        }

        /// <summary>
        /// Unregisters all of the global shortcuts.
        /// </summary>
        public void UnregisterAll()
        {
            _shortcuts.Clear();
            BridgeConnector.Emit("globalShortcut-unregisterAll");
        }
    }
}