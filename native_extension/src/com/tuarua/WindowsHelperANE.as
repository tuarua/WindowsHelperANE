/*
 * Copyright Tua Rua Ltd. (c) 2017.
 */
package com.tuarua {

import flash.desktop.NativeApplication;
import flash.events.EventDispatcher;
import flash.events.StatusEvent;
import flash.external.ExtensionContext;

public class WindowsHelperANE extends EventDispatcher {
    private static const name:String = "WindowsHelperANE";
    private var _isInited:Boolean = false;
    private var _isSupported:Boolean = false;
    private var ctx:ExtensionContext;

    public function WindowsHelperANE() {
        initiate();
    }

    protected function initiate():void {
        _isSupported = true;
        trace("[" + name + "] Initalizing ANE...");
        try {
            ctx = ExtensionContext.createExtensionContext("com.tuarua." + name, null);
            ctx.addEventListener(StatusEvent.STATUS, gotEvent);
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
        ctx.call("init");
    }

    public function findWindowByTitle(searchTerm:String):String {
        return ctx.call("findWindowByTitle", searchTerm) as String;
    }

    public function showWindow(maximise:Boolean = false):void {
        ctx.call("showWindow", maximise);
    }

    public function restartApp(delay:int = 2):Boolean {
        var success:Boolean = ctx.call("restartApp", delay);
        if (success) {
            NativeApplication.nativeApplication.exit();
        }
        return success;
    }

    public function setForegroundWindow():void {
        ctx.call("setForegroundWindow");
    }

    public function getDisplayDevices():Vector.<DisplayDevice> {
        return ctx.call("getDisplayDevices") as Vector.<DisplayDevice>;
    }

    public function setDisplayResolution(key:String, width:int, height:int, refreshRate:int = 0):Boolean {
        return ctx.call("setDisplayResolution", key, width, height, refreshRate);
    }

    public function registerHotKey(keycode:int, modifier:int):int {
        return int(ctx.call("registerHotKey", keycode, modifier));
    }

    public function unregisterHotKey(id:int):void {
        ctx.call("unregisterHotKey", id);
    }

    public function getNumLogicalProcessors():int {
        return int(ctx.call("getNumLogicalProcessors"));
    }

    public function getScaleFactor():Number {
        return Number(ctx.call("getScaleFactor"));
    }

    public function startAtLogin(name:String, start:Boolean = true):void {
        ctx.call("startAtLogin", name, start);
    }

    public function isSupported():Boolean {
        return _isSupported;
    }

    public function dispose():void {
        if (!ctx) {
            trace("[" + name + "] Error. ANE Already in a disposed or failed state...");
            return;
        }
        trace("[" + name + "] Unloading ANE...");
        ctx.removeEventListener(StatusEvent.STATUS, gotEvent);
        ctx.dispose();
        ctx = null;
    }


}
}
