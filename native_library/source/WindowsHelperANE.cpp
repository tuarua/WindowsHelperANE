#include "WindowsHelperANE.h"
#include <sstream>
#include <windows.h>
#include <conio.h>

#include "ANEHelper.h"
#include <map>

ANEHelper aneHelper = ANEHelper(); //set of useful methods for working with FRE
const std::string ANE_NAME = "WindowsHelperANE";

DWORD air_windowID;
HWND air_windowHandle;
std::string searchTerm;
HWND found_windowHandle;
std::string found_windowTitle;

FREContext dllContext;
#include "stdafx.h"


std::wstring s2ws(const std::string& s) {
	using namespace std;
	int len;
	auto slength = int(s.length()) + 1;
	len = MultiByteToWideChar(CP_UTF8, 0, s.c_str(), slength, 0, 0);
	auto buf = new wchar_t[len];
	MultiByteToWideChar(CP_UTF8, 0, s.c_str(), slength, buf, len);
	wstring r(buf);
	delete[] buf;
	return r;
}

std::string wcharToString(const wchar_t* arg) {
	using namespace std;
	wstring ws(arg);
	string str(ws.begin(), ws.end());
	return str;
}

std::map<std::string, DISPLAY_DEVICE> displayDeviceMap;
struct DisplaySettings {
	int width;
	int height;
	int bitDepth;
	int refreshRate;
} typedef displaySettings;


bool hasDisplaySetting(std::vector<displaySettings> availableDisplaySettings, displaySettings check) {
	for (auto item : availableDisplaySettings) {
		if (item.width == check.width
			&& item.bitDepth == check.bitDepth
			&& item.height == check.height
			&& item.refreshRate == check.refreshRate) {
			return true;
		}
	}
	return false;
}


extern "C" {


	int logLevel = 1;
	//sends a trace back to Flash.
	extern void trace(std::string msg) {
		auto value = "["+ANE_NAME+"] " + msg;
		//if (logLevel > 0)
		aneHelper.dispatchEvent(dllContext, "TRACE", value);
	}

// define this, to save typing each time. We can use FRE_FUNCTION(funcName) as below.
#define FRE_FUNCTION(fn) FREObject (fn)(FREContext context, void* functionData, uint32_t argc, FREObject argv[])

	// https://msdn.microsoft.com/en-us/library/windows/desktop/ms633539(v=vs.85).aspx

	//loops over windows and finds as per our 'searchTerm'
	BOOL CALLBACK getWindowByTitle(HWND hwnd, LPARAM lParam) {
		using namespace std;
		char buffer[1024];
		auto written = GetWindowTextA(hwnd, buffer, 1024);	
		if (written && strstr(buffer, searchTerm.c_str()) != nullptr) {
			found_windowTitle = string(buffer);
			found_windowHandle = hwnd;
			return false;
		}
		return true;
	}


	//finds window by title
	FRE_FUNCTION(findWindowByTitle) {
		//ListDisplayDevices();

		searchTerm = aneHelper.getString(argv[0]); //convert FRE arg into string
		EnumWindows(getWindowByTitle, NULL);
		return aneHelper.getFREObject(found_windowTitle);
	}
	
	//sets the window infront of all others
	FRE_FUNCTION(setForegroundWindow) {
		if (IsWindow(found_windowHandle)) {
			SetForegroundWindow(found_windowHandle);
		}
		return nullptr;
	}

	FRE_FUNCTION(hideWindow) {
		ShowWindow(found_windowHandle, SW_HIDE);
		return nullptr;
	}

	//shows and restores the window
	FRE_FUNCTION(showWindow) {
		auto maximise = aneHelper.getBool(argv[0]);
		if (IsWindow(found_windowHandle)) {
			ShowWindow(found_windowHandle, maximise ? SW_SHOWMAXIMIZED : SW_RESTORE);
		}
		return nullptr; //same as NULL
	}

	//gets list of display devices, the monitor, and available resolutions
	//https://github.com/mushasun/mapinect/blob/4d2063e84d44ab28f769e78315a9fa7263a31eb3/branches/quad-with-plane-intersection/src/lpmt/monitor.cpp

	FRE_FUNCTION(getDisplayDevices) {
		auto vecDisplayDevices = aneHelper.createFREObject("Vector.<com.tuarua.DisplayDevice>");
		auto index = 0;
		auto cnt = 0;
		DISPLAY_DEVICE dd;
		dd.cb = sizeof(DISPLAY_DEVICE);

		displayDeviceMap.clear();

		while (EnumDisplayDevices(nullptr, index++, &dd, 0)) {
			auto displayDevice = aneHelper.createFREObject("com.tuarua.DisplayDevice");
			auto displayMonitor = aneHelper.createFREObject("com.tuarua.Monitor");

			if (dd.StateFlags & DISPLAY_DEVICE_PRIMARY_DEVICE) {
				aneHelper.setProperty(displayDevice, "isPrimary", true);
			}
			if (dd.StateFlags & DISPLAY_DEVICE_ACTIVE) {
				aneHelper.setProperty(displayDevice, "isActive", true);
			}
			if (dd.StateFlags & DISPLAY_DEVICE_REMOVABLE) {
				aneHelper.setProperty(displayDevice, "isRemovable", true);
			}
			if (dd.StateFlags & DISPLAY_DEVICE_VGA_COMPATIBLE) {
				aneHelper.setProperty(displayDevice, "isVgaCampatible", true);
			}

			DISPLAY_DEVICE monitor;
			monitor.cb = sizeof(DISPLAY_DEVICE);
			if (!EnumDisplayDevices(dd.DeviceName, index-1, &monitor, 0)) {
				continue;
			}

			DEVMODE dm;
			dm.dmSize = sizeof(DEVMODE);
			if (!EnumDisplaySettings(dd.DeviceName, ENUM_CURRENT_SETTINGS, &dm)) {
				continue;
			}

			DEVMODE availdm = { 0 };
			availdm.dmSize = sizeof(availdm);
			std::vector<displaySettings> availableDisplaySettings = {};
			FREObject freAvailableDisplaySettings = aneHelper.getProperty(displayDevice, "availableDisplaySettings");

			auto cntAvailableSettings = 0;
			for (auto iModeNum = 0; EnumDisplaySettings(dd.DeviceName, iModeNum, &availdm) != 0; iModeNum++) {
				DisplaySettings settings = {};
				settings.width = availdm.dmPelsWidth;
				settings.height = availdm.dmPelsHeight;
				settings.bitDepth = availdm.dmBitsPerPel;
				settings.refreshRate = availdm.dmDisplayFrequency;

				if (!hasDisplaySetting(availableDisplaySettings, settings)) {
					availableDisplaySettings.push_back(settings);

					auto availabletDisplaySettings = aneHelper.createFREObject("com.tuarua.DisplaySettings");
					aneHelper.setProperty(availabletDisplaySettings, "width", static_cast<int32_t>(availdm.dmPelsWidth));
					aneHelper.setProperty(availabletDisplaySettings, "height", static_cast<int32_t>(availdm.dmPelsHeight));
					aneHelper.setProperty(availabletDisplaySettings, "refreshRate", static_cast<int32_t>(availdm.dmDisplayFrequency));
					aneHelper.setProperty(availabletDisplaySettings, "bitDepth", static_cast<int32_t>(availdm.dmBitsPerPel));

					FRESetArrayElementAt(freAvailableDisplaySettings, cntAvailableSettings, availabletDisplaySettings);
					cntAvailableSettings++;
				}
			}

			aneHelper.setProperty(displayMonitor, "friendlyName", wcharToString(monitor.DeviceString));
			aneHelper.setProperty(displayMonitor, "name", wcharToString(monitor.DeviceName));
			aneHelper.setProperty(displayMonitor, "id", wcharToString(monitor.DeviceID));
			aneHelper.setProperty(displayMonitor, "key", wcharToString(monitor.DeviceKey));

			aneHelper.setProperty(displayDevice, "id", wcharToString(dd.DeviceID));
			aneHelper.setProperty(displayDevice, "name", wcharToString(dd.DeviceName));
			aneHelper.setProperty(displayDevice, "friendlyName", wcharToString(dd.DeviceString));
			aneHelper.setProperty(displayDevice, "key", wcharToString(dd.DeviceKey));

			auto currentDisplaySettings = aneHelper.createFREObject("com.tuarua.DisplaySettings");
			aneHelper.setProperty(currentDisplaySettings, "width", static_cast<int32_t>(dm.dmPelsWidth));
			aneHelper.setProperty(currentDisplaySettings, "height", static_cast<int32_t>(dm.dmPelsHeight));
			aneHelper.setProperty(currentDisplaySettings, "refreshRate", static_cast<int32_t>(dm.dmDisplayFrequency));
			aneHelper.setProperty(currentDisplaySettings, "bitDepth", static_cast<int32_t>(dm.dmBitsPerPel));

			aneHelper.setProperty(displayDevice, "currentDisplaySettings", currentDisplaySettings);
			aneHelper.setProperty(displayDevice, "monitor", displayMonitor);

			FRESetArrayElementAt(vecDisplayDevices, cnt, displayDevice);

			displayDeviceMap.insert(make_pair(wcharToString(dd.DeviceKey), dd));

			cnt++;
		}

		return vecDisplayDevices;
	}
	

	FRE_FUNCTION(setDisplayResolution) {
		auto key = aneHelper.getString(argv[0]);
		auto newWidth = aneHelper.getInt32(argv[1]);
		auto newHeight = aneHelper.getInt32(argv[2]);
		auto newRefreshRate = aneHelper.getInt32(argv[3]);

		auto search = displayDeviceMap.find(key);
		if (search != displayDeviceMap.end()) {
			auto foundDevice = search->second;

			DEVMODE dm;
			dm.dmSize = sizeof(DEVMODE);
			if (!EnumDisplaySettings(foundDevice.DeviceName, ENUM_CURRENT_SETTINGS, &dm)) {
				return aneHelper.getFREObject(false);
			}

			dm.dmPelsWidth = newWidth;
			dm.dmPelsHeight = newHeight;
			if(newRefreshRate > 0) {
				dm.dmFields = (DM_PELSWIDTH | DM_PELSHEIGHT | DM_DISPLAYFREQUENCY);
				dm.dmDisplayFrequency = newRefreshRate;
			} else {
				dm.dmFields = (DM_PELSWIDTH | DM_PELSHEIGHT);
			}
			
			if (ChangeDisplaySettings(&dm, CDS_TEST) != DISP_CHANGE_SUCCESSFUL) {
				return aneHelper.getFREObject(false);
			}

			return aneHelper.getFREObject(ChangeDisplaySettings(&dm, 0) == DISP_CHANGE_SUCCESSFUL);

		}
		
		return aneHelper.getFREObject(false);
	}

	/*
	 * Gets the current AIR window, in case we need it.
	 */
	BOOL CALLBACK EnumProc(HWND hwnd, LPARAM lParam) {
		GetWindowThreadProcessId(hwnd, &air_windowID);
		if (air_windowID == lParam) {
			air_windowHandle = hwnd;
			return false;
		}
		return true;
	}



	void contextInitializer(void* extData, const uint8_t* ctxType, FREContext ctx, uint32_t* numFunctionsToSet, const FRENamedFunction** functionsToSet) {
		
		auto processID = GetCurrentProcessId();
		EnumWindows(EnumProc, processID);

		static FRENamedFunction extensionFunctions[] = {
		{ reinterpret_cast<const uint8_t*>("findWindowByTitle"),nullptr, &findWindowByTitle },
		{ reinterpret_cast<const uint8_t*>("showWindow"),nullptr, &showWindow },
		{ reinterpret_cast<const uint8_t*>("hideWindow"),nullptr, &hideWindow },
		{ reinterpret_cast<const uint8_t*>("setForegroundWindow"),nullptr, &setForegroundWindow },
		{ reinterpret_cast<const uint8_t*>("getDisplayDevices"),nullptr, &getDisplayDevices },
		{ reinterpret_cast<const uint8_t*>("setDisplayResolution"),nullptr, &setDisplayResolution }

		};

		*numFunctionsToSet = sizeof(extensionFunctions) / sizeof(FRENamedFunction);
		*functionsToSet = extensionFunctions;
		dllContext = ctx;
	}


	void contextFinalizer(FREContext ctx) {
	}

	void TRWHExtInizer(void** extData, FREContextInitializer* ctxInitializer, FREContextFinalizer* ctxFinalizer) {
		*ctxInitializer = &contextInitializer;
		*ctxFinalizer = &contextFinalizer;
	}

	void TRWHExtFinizer(void* extData) {
		FREContext nullCTX;
		nullCTX = nullptr;
		contextFinalizer(nullCTX);
	}
}
