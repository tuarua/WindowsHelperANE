/**
 * Created by Eoin Landy on 02/05/2017.
 */
package com.tuarua {
import flash.events.Event;

public class HotKeyEvent extends Event {
    public var params:Object;
    public static const ON_HOT_KEY:String = "ON_HOT_KEY";
    public function HotKeyEvent(type:String, params:Object=null, bubbles:Boolean=false, cancelable:Boolean=false) {
        super(type, bubbles, cancelable);
        this.params = params;
    }
    public override function clone():Event {
        return new HotKeyEvent(type, this.params, bubbles, cancelable);
    }
    public override function toString():String {
        return formatToString("ToastEvent", "params", "type", "bubbles", "cancelable");
    }
}
}
