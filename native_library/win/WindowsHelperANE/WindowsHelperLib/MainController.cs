using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using TuaRua.FreSharp;
using static WindowsHelperLib.ShowWindowCommands;
using FREObject = System.IntPtr;
using FREContext = System.IntPtr;
using Hwnd = System.IntPtr;

namespace WindowsHelperLib {
    public class MainController : FreSharpController {
        // ReSharper disable once NotAccessedField.Local
        private Hwnd _airWindow;

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
                    {"getNumLogicalProcessors",GetNumLogicalProcessors},


                };

            return FunctionsDict.Select(kvp => kvp.Key).ToArray();
        }

        public FREObject NotImplemented(FREContext ctx, uint argc, FREObject[] argv) {
            return FREObject.Zero;
        }

        public FREObject InitController(FREContext ctx, uint argc, FREObject[] argv) {
            _airWindow = Process.GetCurrentProcess().MainWindowHandle;
            return FREObject.Zero;
        }

        private void HotKeyManager_HotKeyPressed(object sender, HotKeyEventArgs e) {
            var key = Convert.ToInt32(e.Key);
            var modifier = Convert.ToInt32(e.Modifiers);
            var sf = $"{{\"key\": {key}, \"modifier\": {modifier}}}";
            Context.SendEvent("ON_HOT_KEY", sf);
            /*
             Alt = 1,
        Control = 2,
        Shift = 4,
        Windows = 8,
        NoRepeat = 0x4000
            */
        }

        public FREObject RegisterHotKey(FREContext ctx, uint argc, FREObject[] argv) {
            var key = Convert.ToInt32(new FreObjectSharp(argv[0]).Value);
            var modifier = Convert.ToInt32(new FreObjectSharp(argv[1]).Value);
            var id = HotKeyManager.RegisterHotKey((Keys) key, (KeyModifiers) modifier);
            if (!_isHotKeyManagerRegistered) {
                HotKeyManager.HotKeyPressed += HotKeyManager_HotKeyPressed;
            }
            _isHotKeyManagerRegistered = true;
            return new FreObjectSharp(id).RawValue;
        }

        public FREObject UnregisterHotKey(FREContext ctx, uint argc, FREObject[] argv) {
            var id = Convert.ToInt32(new FreObjectSharp(argv[0]).Value);
            HotKeyManager.UnregisterHotKey(id);
            return FREObject.Zero;
        }

        public FREObject GetNumLogicalProcessors(FREContext ctx, uint argc, FREObject[] argv) {
            return new FreObjectSharp(Environment.ProcessorCount).RawValue;
        }

        public FREObject FindWindowByTitle(FREContext ctx, uint argc, FREObject[] argv) {
            var searchTerm = Convert.ToString(new FreObjectSharp(argv[0]).Value);
            // ReSharper disable once SuggestVarOrType_SimpleTypes
            foreach (var pList in Process.GetProcesses()) {
                if (!string.IsNullOrEmpty(searchTerm) && !pList.MainWindowTitle.Contains(searchTerm)) continue;
                _foundWindow = pList.MainWindowHandle;
                return new FreObjectSharp(pList.MainWindowTitle).RawValue;
            }
            return FREObject.Zero;
        }

        public FREObject ShowWindow(FREContext ctx, uint argc, FREObject[] argv) {
            var maximise = (bool) new FreObjectSharp(argv[0]).Value;
            if (WinApi.IsWindow(_foundWindow)) {
                WinApi.ShowWindow(_foundWindow, maximise ? SW_SHOWMAXIMIZED : SW_RESTORE);
            }
            return FREObject.Zero;
        }

        public FREObject HideWindow(FREContext ctx, uint argc, FREObject[] argv) {
            if (WinApi.IsWindow(_foundWindow)) {
                WinApi.ShowWindow(_foundWindow, SW_HIDE);
            }
            return FREObject.Zero;
        }

        public FREObject SetForegroundWindow(FREContext ctx, uint argc, FREObject[] argv) {
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

        public FREObject GetDisplayDevices(FREContext ctx, uint argc, FREObject[] argv) {
            var tmp = new FREObject().Init("Vector.<com.tuarua.DisplayDevice>", null);
            var vecDisplayDevices = new FREArray(tmp);

            var dd = new DisplayDevice();
            dd.cb = Marshal.SizeOf(dd);

            _displayDeviceMap.Clear();

            try {
                uint index = 0;
                uint cnt = 0;
                while (WinApi.EnumDisplayDevices(null, index++, ref dd, 0)) {
                    var displayDevice = new FREObject().Init("com.tuarua.DisplayDevice", null);
                    var displayMonitor = new FREObject().Init("com.tuarua.Monitor", null);

                    displayDevice.SetProp("isPrimary",
                        dd.StateFlags.HasFlag(DisplayDeviceStateFlags.PrimaryDevice));
                    displayDevice.SetProp("isActive",
                        dd.StateFlags.HasFlag(DisplayDeviceStateFlags.AttachedToDesktop));
                    displayDevice.SetProp("isRemovable", dd.StateFlags.HasFlag(DisplayDeviceStateFlags.Removable));
                    displayDevice.SetProp("isVgaCampatible",
                        dd.StateFlags.HasFlag(DisplayDeviceStateFlags.VgaCompatible));

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

                    var freAvailableDisplaySettings = new FREArray(displayDevice.GetProp("availableDisplaySettings"));

                    uint cntAvailableSettings = 0;
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

                        var displaySettings = new FREObject().Init("com.tuarua.DisplaySettings", null);

                        displaySettings.SetProp("width", availdm.dmPelsWidth);
                        displaySettings.SetProp("height", availdm.dmPelsHeight);
                        displaySettings.SetProp("refreshRate", availdm.dmDisplayFrequency);
                        displaySettings.SetProp("bitDepth", Convert.ToInt32(availdm.dmBitsPerPel));
                        freAvailableDisplaySettings.Set(cntAvailableSettings, displaySettings);
                        cntAvailableSettings++;
                    }

                    displayMonitor.SetProp("friendlyName", monitor.DeviceString);
                    displayMonitor.SetProp("name", monitor.DeviceName);
                    displayMonitor.SetProp("id", monitor.DeviceID);
                    displayMonitor.SetProp("key", monitor.DeviceKey);

                    displayDevice.SetProp("friendlyName", dd.DeviceString);
                    displayDevice.SetProp("name", dd.DeviceName);
                    displayDevice.SetProp("id", dd.DeviceID);
                    displayDevice.SetProp("key", dd.DeviceKey);

                    var currentDisplaySettings = new FREObject().Init("com.tuarua.DisplaySettings", null);

                    currentDisplaySettings.SetProp("width", dm.dmPelsWidth);
                    currentDisplaySettings.SetProp("height", dm.dmPelsHeight);
                    currentDisplaySettings.SetProp("refreshRate", dm.dmDisplayFrequency);
                    currentDisplaySettings.SetProp("bitDepth", Convert.ToInt32(dm.dmBitsPerPel));

                    displayDevice.SetProp("currentDisplaySettings", currentDisplaySettings);
                    displayDevice.SetProp("monitor", displayMonitor);

                    vecDisplayDevices.Set(cnt, displayDevice);

                    _displayDeviceMap.Add(dd.DeviceKey, dd);

                    cnt++;
                }
            }
            catch (Exception e) {
                Trace(e.Message);
            }

            return vecDisplayDevices.RawValue;
        }

        public FREObject SetDisplayResolution(FREContext ctx, uint argc, FREObject[] argv) {
            var key = Convert.ToString(new FreObjectSharp(argv[0]).Value);
            var newWidth = Convert.ToInt32(new FreObjectSharp(argv[1]).Value);
            var newHeight = Convert.ToInt32(new FreObjectSharp(argv[2]).Value);
            var newRefreshRate = Convert.ToInt32(new FreObjectSharp(argv[3]).Value);

            if (!string.IsNullOrEmpty(key)) {
                var device = _displayDeviceMap[key];
                var dm = new Devmode();
                dm.dmSize = (short) Marshal.SizeOf(dm);

                if (WinApi.EnumDisplaySettings(device.DeviceName, WinApi.EnumCurrentSettings, ref dm) == 0) {
                    return new FreObjectSharp(false).RawValue;
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
                    ? new FreObjectSharp(false).RawValue
                    : new FreObjectSharp(WinApi.ChangeDisplaySettings(ref dm, 0) == 0).RawValue;
            }
            return FREObject.Zero;
        }

        public FREObject RestartApp(FREContext ctx, uint argc, FREObject[] argv) {
            var delay = Convert.ToInt32(new FreObjectSharp(argv[0]).Value);
            var wmiQuery =
                $"select CommandLine from Win32_Process where Name='{Process.GetCurrentProcess().ProcessName}.exe'";
            var searcher = new ManagementObjectSearcher(wmiQuery);
            var retObjectCollection = searcher.Get();
            var sf = (from ManagementObject retObject in retObjectCollection select $"{retObject["CommandLine"]}")
                .FirstOrDefault();
            if (string.IsNullOrEmpty(sf)) return new FreObjectSharp(false).RawValue;
            var info = new ProcessStartInfo {
                Arguments = "/C ping 127.0.0.1 -n " + delay + " && " + sf,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName = "cmd.exe"
            };
            Process.Start(info);
            return new FreObjectSharp(true).RawValue;
        }
    }
}