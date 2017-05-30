/*
 * Copyright Tua Rua Ltd. (c) 2017.
 */
package com.tuarua {
import com.tuarua.fre.ANEContext;

import flash.desktop.NativeApplication;
import flash.events.EventDispatcher;
import flash.events.StatusEvent;
import flash.external.ExtensionContext;

public class WindowsHelperANE extends EventDispatcher {
    private static const name:String = "WindowsHelperANE";
    private var _isInited:Boolean = false;
    private var _isSupported:Boolean = false;


    public function WindowsHelperANE() {
        initiate();
    }

    protected function initiate():void {
        _isSupported = true;
        trace("[" + name + "] Initalizing ANE...");
        try {
            ANEContext.ctx = ExtensionContext.createExtensionContext("com.tuarua." + name, null);
            ANEContext.ctx.addEventListener(StatusEvent.STATUS, gotEvent);
        } catch (e:Error) {
            trace(e.message);
            trace("[" + name + "] ANE Not loaded properly.  Future calls will fail.");
        }
    }


    private function gotEvent(event:StatusEvent):void {
        var pObj:Object;
        //trace(event.level);
        switch (event.level) {
            case "TRACE":
                trace(event.code);
                break;
            case HotKeyEvent.ON_HOT_KEY:
                try {
                    pObj = JSON.parse(event.code);
                    dispatchEvent(new HotKeyEvent(HotKeyEvent.ON_HOT_KEY, pObj));
                } catch (e:Error) {
                    trace(e.message);
                }
                break;
            default:
                break;
        }
    }


    public function init():void {
        ANEContext.ctx.call("init");
    }

    public function findWindowByTitle(searchTerm:String):String {
        return ANEContext.ctx.call("findWindowByTitle", searchTerm) as String;
    }

    public function showWindow(maximise:Boolean = false):void {
        ANEContext.ctx.call("showWindow", maximise);
    }

    public function restartApp(delay:int = 2):Boolean {
        var success:Boolean = ANEContext.ctx.call("restartApp", delay);
        if (success) {
            NativeApplication.nativeApplication.exit();
        }
        return success;
    }

    public function setForegroundWindow():void {
        ANEContext.ctx.call("setForegroundWindow");
    }

    public function getDisplayDevices():Vector.<DisplayDevice> {
        return ANEContext.ctx.call("getDisplayDevices") as Vector.<DisplayDevice>;
    }

    public function setDisplayResolution(key:String, width:int, height:int, refreshRate:int = 0):Boolean {
        return ANEContext.ctx.call("setDisplayResolution", key, width, height, refreshRate);
    }

    public function registerHotKey(keycode:int, modifier:int):int {
        return int(ANEContext.ctx.call("registerHotKey", keycode, modifier));
    }

    public function unregisterHotKey(id:int):void {
        ANEContext.ctx.call("unregisterHotKey", id);
    }

    public function isSupported():Boolean {
        return _isSupported;
    }

    public function dispose():void {
        if (!ANEContext.ctx) {
            trace("[" + name + "] Error. ANE Already in a disposed or failed state...");
            return;
        }
        trace("[" + name + "] Unloading ANE...");
        ANEContext.ctx.removeEventListener(StatusEvent.STATUS, gotEvent);
        ANEContext.ctx.dispose();
        ANEContext.ctx = null;
    }


}
}
