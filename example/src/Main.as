package {

import com.tuarua.DisplayDevice;
import com.tuarua.DisplaySettings;
import com.tuarua.HotKeyEvent;
import com.tuarua.HotKeyModifier;

import flash.desktop.NativeApplication;

import flash.display.Sprite;

import com.tuarua.WindowsHelperANE;

import flash.events.Event;

import flash.events.MouseEvent;
import flash.ui.Keyboard;

public class Main extends Sprite {
    private var ane:WindowsHelperANE;
    private var btn:Sprite = new Sprite();
    private var hasActivated:Boolean;
    private var hotKeyIds:Array = [];

    public function Main() {
        this.addEventListener(Event.ACTIVATE, onActivated);

        btn.graphics.beginFill(0x0033FF);
        btn.graphics.drawRect(0, 0, 100, 100);
        btn.graphics.endFill();
        btn.addEventListener(MouseEvent.CLICK, getNumLogicalProcessors);
        addChild(btn);
    }

    private function getNumLogicalProcessors(event:MouseEvent):void {
        var numLogicalProcessors:int = ane.getNumLogicalProcessors();
        trace("numLogicalProcessors", numLogicalProcessors);
    }

    private function onRestartApp(event:MouseEvent):void {
        var isRestarting:Boolean = ane.restartApp(1);
        trace(isRestarting);
    }

    private function onActivated(event:Event):void {
        if (!hasActivated) {
            ane = new WindowsHelperANE();
            ane.init();
            ane.addEventListener(HotKeyEvent.ON_HOT_KEY, onHotKeyEvent);

            var foundWindowTitle:String = ane.findWindowByTitle("Google Chrome");
            if (foundWindowTitle) {
                trace("We have found window:", foundWindowTitle);
                ane.setForegroundWindow();
                ane.showWindow(true);//true to maximise
            }

            trace("foundWindowTitle", foundWindowTitle);
        }
        hasActivated = true;
    }

    private function onHotKeyEvent(event:HotKeyEvent):void {
        if (event.params.key == Keyboard.A && event.params.modifier == HotKeyModifier.ALT) {
            trace("Alt A Pressed as a hotkey");
        }

        if (event.params.key == Keyboard.F10 && event.params.modifier == HotKeyModifier.CONTROL) {
            trace("Ctrl F10 Pressed as a hotkey");
        }

        if (event.params.key == Keyboard.X && event.params.modifier == HotKeyModifier.SHIFT) {
            trace("Shift F10 Pressed as a hotkey");
        }
    }

    private function onAddHotKey(event:MouseEvent):void {
        trace("add a hot key")
        var hotKeyId1:int = ane.registerHotKey(Keyboard.A, HotKeyModifier.ALT);
        var hotKeyId2:int = ane.registerHotKey(Keyboard.F10, HotKeyModifier.CONTROL);
        var hotKeyId3:int = ane.registerHotKey(Keyboard.X, HotKeyModifier.SHIFT);
        hotKeyIds.push(hotKeyId1);
        hotKeyIds.push(hotKeyId2);
        hotKeyIds.push(hotKeyId3);
        trace("hot key added", hotKeyId1);
        trace("hot key added", hotKeyId2);
        trace("hot key added", hotKeyId3);


    }

    private function removeAllHotKeys():void {
        for each (var hotKey:int in hotKeyIds) {
            ane.unregisterHotKey(hotKey);
        }
    }

    private function onGetDisplayDevices(event:MouseEvent):void {
        var displayDevices:Vector.<DisplayDevice> = ane.getDisplayDevices();
        for each (var dd:Object in displayDevices) {
            trace(dd.currentDisplaySettings.width, dd.currentDisplaySettings.height);
            trace(dd.key)
        }

        if (displayDevices.length > 0) {
            var firstDisplay:DisplayDevice = displayDevices[0];
            for each (var ds:DisplaySettings in firstDisplay.availableDisplaySettings) {
                if (ds.width == 1600 && ds.height == 900) {
                    ane.setDisplayResolution(firstDisplay.key, 1600, 900);
                    break;
                }
            }
        }
    }

}
}
