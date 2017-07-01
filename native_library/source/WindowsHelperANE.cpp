#include "WindowsHelperANE.h"
#include "FlashRuntimeExtensions.h"
#include "stdafx.h"
#include "FreSharpBridge.h"

extern "C" {


	void contextInitializer(void* extData, const uint8_t* ctxType, FREContext ctx, uint32_t* numFunctionsToSet, const FRENamedFunction** functionsToSet) {

		FreSharpBridge::InitController();
		FreSharpBridge::SetFREContext(ctx);
		FreSharpBridge::GetFunctions();

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
		{ (const uint8_t *) "unregisterHotKey","unregisterHotKey", &callSharpFunction },
		{ (const uint8_t *) "getNumLogicalProcessors","getNumLogicalProcessors", &callSharpFunction }
		
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
