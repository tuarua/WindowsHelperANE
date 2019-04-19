using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;
using TuaRua.FreSharp;
using static WindowsHelperLib.ShowWindowCommands;
using FREObject = System.IntPtr;
using FREContext = System.IntPtr;
using Hwnd = System.IntPtr;
// ReSharper disable UnusedMember.Global

namespace WindowsHelperLib {
    public class MainController : FreSharpMainController {
        private Hwnd _foundWindow;
        private readonly Dictionary<string, DisplayDevice> _displayDeviceMap = new Dictionary<string, DisplayDevice>();
        private bool _isHotKeyManagerRegistered;

        public string[] GetFunctions() {
            FunctionsDict =
                new Dictionary<string, Func<FREObject, uint, FREObject[], FREObject>> {
                    {"init", InitController},
                    {"findWindowByTitle", FindWindowByTitle},
                    {"showWindow", ShowWindow},
                    {"hideWindow", HideWindow},
                    {"setForegroundWindow", SetForegroundWindow},
                    {"getDisplayDevices", GetDisplayDevices},
                    {"setDisplayResolution", SetDisplayResolution},
                    {"restartApp", RestartApp},
                    {"registerHotKey", RegisterHotKey},
                    {"unregisterHotKey", UnregisterHotKey},
                    {"getNumLogicalProcessors", GetNumLogicalProcessors},
                    {"startAtLogin", StartAtLogin}
                };

            return FunctionsDict.Select(kvp => kvp.Key).ToArray();
        }

        private FREObject InitController(FREContext ctx, uint argc, FREObject[] argv) {
            return FREObject.Zero;
        }

        private void HotKeyManager_HotKeyPressed(object sender, HotKeyEventArgs e) {
            var key = Convert.ToInt32(e.Key);
            var modifier = Convert.ToInt32(e.Modifiers);
            var sf = $"{{\"key\": {key}, \"modifier\": {modifier}}}";
            Context.DispatchEvent("ON_HOT_KEY", sf);
            /*
             Alt = 1,
        Control = 2,
        Shift = 4,
        Windows = 8,
        NoRepeat = 0x4000
            */
        }

        private FREObject RegisterHotKey(FREContext ctx, uint argc, FREObject[] argv) {
            var key = argv[0].AsInt();
            var modifier = argv[1].AsInt();
            var id = HotKeyManager.RegisterHotKey((Keys) key, (KeyModifiers) modifier);
            if (!_isHotKeyManagerRegistered) {
                HotKeyManager.HotKeyPressed += HotKeyManager_HotKeyPressed;
            }

            _isHotKeyManagerRegistered = true;
            return id.ToFREObject();
        }

        private FREObject UnregisterHotKey(FREContext ctx, uint argc, FREObject[] argv) {
            var id = argv[0].AsInt();
            HotKeyManager.UnregisterHotKey(id);
            return FREObject.Zero;
        }

        private FREObject GetNumLogicalProcessors(FREContext ctx, uint argc, FREObject[] argv) {
            return Environment.ProcessorCount.ToFREObject();
        }

        private FREObject FindWindowByTitle(FREContext ctx, uint argc, FREObject[] argv) {
            var searchTerm = argv[0].AsString();
            // ReSharper disable once SuggestVarOrType_SimpleTypes
            foreach (var pList in Process.GetProcesses()) {
                if (!string.IsNullOrEmpty(searchTerm) && !pList.MainWindowTitle.Contains(searchTerm)) continue;
                _foundWindow = pList.MainWindowHandle;
                return pList.MainWindowTitle.ToFREObject();
            }

            return FREObject.Zero;
        }

        private FREObject ShowWindow(FREContext ctx, uint argc, FREObject[] argv) {
            var maximise = argv[0].AsBool();
            if (WinApi.IsWindow(_foundWindow)) {
                WinApi.ShowWindow(_foundWindow, maximise ? SW_SHOWMAXIMIZED : SW_RESTORE);
            }

            return FREObject.Zero;
        }

        private FREObject HideWindow(FREContext ctx, uint argc, FREObject[] argv) {
            if (WinApi.IsWindow(_foundWindow)) {
                WinApi.ShowWindow(_foundWindow, SW_HIDE);
            }

            return FREObject.Zero;
        }

        private FREObject SetForegroundWindow(FREContext ctx, uint argc, FREObject[] argv) {
            if (WinApi.IsWindow(_foundWindow)) {
                WinApi.SetForegroundWindow(_foundWindow);
            }

            return FREObject.Zero;
        }

        private struct DisplaySettings {
            public int Width;
            public int Height;
            public int BitDepth;
            public int RefreshRate;
        }

        private static bool HasDisplaySetting(IEnumerable<DisplaySettings> availableDisplaySettings,
            DisplaySettings check) {
            return availableDisplaySettings.Any(item => item.Width == check.Width
                                                        && item.BitDepth == check.BitDepth &&
                                                        item.Height == check.Height
                                                        && item.RefreshRate == check.RefreshRate);
        }

        private FREObject GetDisplayDevices(FREContext ctx, uint argc, FREObject[] argv) {
            var vecDisplayDevices = new FREArray("com.tuarua.DisplayDevice");

            var dd = new DisplayDevice();
            dd.cb = Marshal.SizeOf(dd);

            _displayDeviceMap.Clear();

            try {
                uint index = 0;
                while (WinApi.EnumDisplayDevices(null, index++, ref dd, 0)) {
                    dynamic displayDevice = new FreObjectSharp("com.tuarua.DisplayDevice");
                    dynamic displayMonitor = new FreObjectSharp("com.tuarua.Monitor");

                    displayDevice.isPrimary = dd.StateFlags.HasFlag(DisplayDeviceStateFlags.PrimaryDevice);
                    displayDevice.isActive = dd.StateFlags.HasFlag(DisplayDeviceStateFlags.AttachedToDesktop);
                    displayDevice.isRemovable = dd.StateFlags.HasFlag(DisplayDeviceStateFlags.Removable);
                    displayDevice.isVgaCampatible = dd.StateFlags.HasFlag(DisplayDeviceStateFlags.VgaCompatible);

                    var monitor = new DisplayDevice();
                    monitor.cb = Marshal.SizeOf(monitor);

                    if (!WinApi.EnumDisplayDevices(dd.DeviceName, index - 1, ref monitor, 0)) {
                        continue;
                    }

                    var dm = new Devmode();
                    dm.dmSize = (short) Marshal.SizeOf(dm);
                    if (WinApi.EnumDisplaySettings(dd.DeviceName, WinApi.EnumCurrentSettings, ref dm) == 0) {
                        continue;
                    }

                    var availdm = new Devmode();
                    availdm.dmSize = (short) Marshal.SizeOf(availdm);
                    IList<DisplaySettings> availableDisplaySettings = new List<DisplaySettings>();

                    var freAvailableDisplaySettings = displayDevice.availableDisplaySettings;
                    for (var iModeNum = 0;
                        WinApi.EnumDisplaySettings(dd.DeviceName, iModeNum, ref availdm) != 0;
                        iModeNum++) {
                        var settings = new DisplaySettings {
                            Width = availdm.dmPelsWidth,
                            Height = availdm.dmPelsHeight,
                            BitDepth = Convert.ToInt32(availdm.dmBitsPerPel),
                            RefreshRate = availdm.dmDisplayFrequency
                        };

                        if (HasDisplaySetting(availableDisplaySettings, settings)) continue;
                        availableDisplaySettings.Add(settings);

                        dynamic displaySettings = new FreObjectSharp("com.tuarua.DisplaySettings");
                        displaySettings.width = availdm.dmPelsWidth;
                        displaySettings.height = availdm.dmPelsHeight;
                        displaySettings.refreshRate = availdm.dmDisplayFrequency;
                        displaySettings.bitDepth = Convert.ToInt32(availdm.dmBitsPerPel);
                        freAvailableDisplaySettings.Push(displaySettings);
                    }

                    displayMonitor.friendlyName = monitor.DeviceString;
                    displayMonitor.name = monitor.DeviceName;
                    displayMonitor.id = monitor.DeviceID;
                    displayMonitor.key = monitor.DeviceKey;

                    displayDevice.friendlyName = dd.DeviceString;
                    displayDevice.name = dd.DeviceName;
                    displayDevice.id = dd.DeviceID;
                    displayDevice.key = dd.DeviceKey;

                    dynamic currentDisplaySettings = new FreObjectSharp("com.tuarua.DisplaySettings");
                    currentDisplaySettings.width = dm.dmPelsWidth;
                    currentDisplaySettings.height = dm.dmPelsHeight;
                    currentDisplaySettings.refreshRate = dm.dmDisplayFrequency;
                    currentDisplaySettings.bitDepth = Convert.ToInt32(dm.dmBitsPerPel);

                    displayDevice.currentDisplaySettings = currentDisplaySettings;
                    displayDevice.monitor = displayMonitor;
                    vecDisplayDevices.Push(displayDevice);

                    _displayDeviceMap.Add(dd.DeviceKey, dd);
                }
            }
            catch (Exception e) {
                Trace(e.Message);
                Trace(e.Source);
                Trace(e.StackTrace);
            }

            return vecDisplayDevices.RawValue;
        }

        private FREObject SetDisplayResolution(FREContext ctx, uint argc, FREObject[] argv) {
            var key = argv[0].AsString();
            var newWidth = argv[1].AsInt();
            var newHeight = argv[2].AsInt();
            var newRefreshRate = argv[3].AsInt();

            if (string.IsNullOrEmpty(key)) return FREObject.Zero;
            var device = _displayDeviceMap[key];
            var dm = new Devmode();
            dm.dmSize = (short) Marshal.SizeOf(dm);

            if (WinApi.EnumDisplaySettings(device.DeviceName, WinApi.EnumCurrentSettings, ref dm) == 0) {
                return false.ToFREObject();
            }

            dm.dmPelsWidth = newWidth;
            dm.dmPelsHeight = newHeight;

            var flgs = DevModeFlags.DM_PELSWIDTH | DevModeFlags.DM_PELSHEIGHT;

            if (newRefreshRate > 0) {
                flgs |= DevModeFlags.DM_DISPLAYFREQUENCY;
                dm.dmDisplayFrequency = newRefreshRate;
            }

            dm.dmFields = (int) flgs;

            return WinApi.ChangeDisplaySettings(ref dm, (int) ChangeDisplaySettingsFlags.CdsTest) != 0
                ? false.ToFREObject()
                : (WinApi.ChangeDisplaySettings(ref dm, 0) == 0).ToFREObject();
        }

        private FREObject RestartApp(FREContext ctx, uint argc, FREObject[] argv) {
            var delay = argv[0].AsInt();
            var wmiQuery =
                $"select CommandLine from Win32_Process where Name='{Process.GetCurrentProcess().ProcessName}.exe'";
            var searcher = new ManagementObjectSearcher(wmiQuery);
            var retObjectCollection = searcher.Get();
            var sf = (from ManagementObject retObject in retObjectCollection select $"{retObject["CommandLine"]}")
                .FirstOrDefault();
            if (string.IsNullOrEmpty(sf)) return false.ToFREObject();
            var info = new ProcessStartInfo {
                Arguments = "/C ping 127.0.0.1 -n " + delay + " && " + sf,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName = "cmd.exe"
            };
            Process.Start(info);
            return true.ToFREObject();
        }

        private FREObject StartAtLogin(FREContext ctx, uint argc, FREObject[] argv) {
            var name = argv[0].AsString();
            var start = argv[1].AsBool();
            var rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (rk == null) return FREObject.Zero;
            if (start) {
                rk.SetValue(name, Process.GetCurrentProcess().MainModule.FileName);
            }
            else {
                rk.DeleteValue(name, false);
            }

            return FREObject.Zero;
        }

        public override void OnFinalize() { }
        public override string TAG => "MainController";
    }
}