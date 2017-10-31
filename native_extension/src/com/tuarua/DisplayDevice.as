package com.tuarua {
[RemoteClass(alias="com.tuarua.DisplayDevice")]
public class DisplayDevice {
	public var isPrimary:Boolean = false;
    public var isActive:Boolean = false;
    public var isRemovable:Boolean = false;
    public var isVgaCampatible:Boolean = false;
    public var id:String;
    public var name:String;
    public var monitor:Monitor;
    public var friendlyName:String;
    public var key:String;
    public var currentDisplaySettings:DisplaySettings;
    public var availableDisplaySettings:Vector.<DisplaySettings> = new <DisplaySettings>[];
    public function DisplayDevice() {
    }
}
}