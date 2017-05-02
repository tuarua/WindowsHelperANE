#include "WindowsHelperANE.h"
#include <sstream>
#include <windows.h>
#include <conio.h>
#include <map>
#include <vector>
#include <string>
const std::string ANE_NAME = "WindowsHelperANE";
#include "stdafx.h"


std::vector<std::string> funcArray;
namespace ManagedCode {
	using namespace System;
	using namespace System::Windows;
	using namespace System::Windows::Interop;
	using namespace System::Windows::Media;
	using namespace System::Collections::Generic;
	using FREObjectSharp = IntPtr;
	using FREContextSharp = IntPtr;
	using FREArgvSharp = array<FREObjectSharp>^;

	ref class ManagedGlobals {
	public:
		static WindowsHelperLib::MainController^ controller = nullptr;
	};

	array<FREObjectSharp>^ MarshalFREArray(array<FREObject>^ argv, uint32_t argc) {
		array<FREObjectSharp>^ arr = gcnew array<FREObjectSharp>(argc);
		for (uint32_t i = 0; i < argc; i++) {
			arr[i] = FREObjectSharp(argv[i]);
		}
		return arr;
	}

	void MarshalString(String ^ s, std::string& os) {
		using namespace Runtime::InteropServices;
		const char* chars =
			(const char*)(Marshal::StringToHGlobalAnsi(s)).ToPointer();
		os = chars;
		Marshal::FreeHGlobal(FREObjectSharp((void*)chars));
	}

	FREObject CallSharpFunction(String^ name, FREContext context, array<FREObject>^ argv, uint32_t argc) {
		return (FREObject)ManagedGlobals::controller->CallSharpFunction(name, FREContextSharp(context), argc, MarshalFREArray(argv, argc));
	}

	void SetFREContext(FREContext freContext) {
		ManagedGlobals::controller->SetFreContext(FREContextSharp(freContext));
	}

	void InitController() {
		ManagedGlobals::controller = gcnew WindowsHelperLib::MainController();
	}

	std::vector<std::string> GetFunctions() {
		std::vector<std::string> ret;
		array<String^>^ mArray = ManagedGlobals::controller->GetFunctions();
		int i = 0;
		for (i = 0; i < mArray->Length; ++i) {
			std::string itemStr = "";
			MarshalString(mArray[i], itemStr);
			ret.push_back(itemStr);
		}
		return ret;
	}

}

extern "C" {

	// define this, to save typing each time. We can use FRE_FUNCTION(funcName) as below.
#define FRE_FUNCTION(fn) FREObject (fn)(FREContext context, void* functionData, uint32_t argc, FREObject argv[])

	array<FREObject>^ getArgvAsArray(FREObject argv[], uint32_t argc) {
		array<FREObject>^ arr = gcnew array<FREObject>(argc);
		for (uint32_t i = 0; i < argc; i++) {
			arr[i] = argv[i];
		}
		return arr;
	}

	FRE_FUNCTION(callSharpFunction) {
		std::string fName = std::string((const char*)functionData);
		return ManagedCode::CallSharpFunction(gcnew System::String(fName.c_str()), context, getArgvAsArray(argv, argc), argc);
	}

	void contextInitializer(void* extData, const uint8_t* ctxType, FREContext ctx, uint32_t* numFunctionsToSet, const FRENamedFunction** functionsToSet) {

		ManagedCode::InitController();
		ManagedCode::SetFREContext(ctx);
		funcArray = ManagedCode::GetFunctions();

		static FRENamedFunction extensionFunctions[] = {
		{ (const uint8_t *) "init","init", &callSharpFunction },
		{ (const uint8_t *) "findWindowByTitle","findWindowByTitle", &callSharpFunction },
		{ (const uint8_t *) "showWindow","showWindow", &callSharpFunction },
		{ (const uint8_t *) "hideWindow","hideWindow", &callSharpFunction },
		{ (const uint8_t *) "setForegroundWindow","setForegroundWindow", &callSharpFunction },
		{ (const uint8_t *) "getDisplayDevices","getDisplayDevices", &callSharpFunction },
		{ (const uint8_t *) "setDisplayResolution","setDisplayResolution", &callSharpFunction },
		{ (const uint8_t *) "restartApp","restartApp", &callSharpFunction },
		{ (const uint8_t *) "registerHotKey","registerHotKey", &callSharpFunction },
		{ (const uint8_t *) "unregisterHotKey","unregisterHotKey", &callSharpFunction }
		};

		*numFunctionsToSet = sizeof(extensionFunctions) / sizeof(FRENamedFunction);
		*functionsToSet = extensionFunctions;

	}


	void contextFinalizer(FREContext ctx) {
	}

	void TRWHExtInizer(void** extData, FREContextInitializer* ctxInitializer, FREContextFinalizer* ctxFinalizer) {
		*ctxInitializer = &contextInitializer;
		*ctxFinalizer = &contextFinalizer;
	}

	void TRWHExtFinizer(void* extData) {
		contextFinalizer(nullptr);
	}
}
