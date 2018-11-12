#include "FreSharpMacros.h"
#include "WindowsHelperANE.h"
#include "FreSharpBridge.h"

extern "C" {

	CONTEXT_INIT(TRWH) {

		FREBRIDGE_INIT

			static FRENamedFunction extensionFunctions[] = {
			MAP_FUNCTION(init)
			,MAP_FUNCTION(findWindowByTitle)
			,MAP_FUNCTION(showWindow)
			,MAP_FUNCTION(hideWindow)
			,MAP_FUNCTION(setForegroundWindow)
			,MAP_FUNCTION(getDisplayDevices)
			,MAP_FUNCTION(setDisplayResolution)
			,MAP_FUNCTION(restartApp)
			,MAP_FUNCTION(registerHotKey)
			,MAP_FUNCTION(unregisterHotKey)
			,MAP_FUNCTION(getNumLogicalProcessors)
			,MAP_FUNCTION(getScaleFactor)
			,MAP_FUNCTION(startAtLogin)
		};

		SET_FUNCTIONS

	}

	CONTEXT_FIN(TRWH) {
		FreSharpBridge::GetController()->OnFinalize();
	}

	EXTENSION_INIT(TRWH)

	EXTENSION_FIN(TRWH)

}
