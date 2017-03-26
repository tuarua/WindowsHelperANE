package
{
	import com.tuarua.DisplayDevice;
	import com.tuarua.WindowsHelperANE;
	
	import flash.display.Sprite;
	import flash.events.MouseEvent;
	
	public class WindowsHelperANEExample extends Sprite
	{
		private var ane:WindowsHelperANE = new WindowsHelperANE();
		private var btn:Sprite = new Sprite();
		public function WindowsHelperANEExample()
		{
			btn.addEventListener(MouseEvent.CLICK, onClick);
			btn.graphics.beginFill(0x333333);
			btn.graphics.drawRect(50,50,100,100);
			btn.graphics.endFill();
			addChild(btn);
			
			var foundWindowTitle:String = ane.findWindowByTitle("Google Chrome");
			if(foundWindowTitle){
				trace("We have found window:",foundWindowTitle);
				ane.setForegroundWindow();
				ane.showWindow(true);//true to maximise
			}
			
		}
		
		protected function onClick(event:MouseEvent):void {
			var displayDevices:Vector.<DisplayDevice> = ane.getDisplayDevices();
			var currentDisplay:DisplayDevice;
			trace(displayDevices.length);
			for each(var displayDevice:DisplayDevice in displayDevices){
				
				if(displayDevice.isActive)
					currentDisplay = displayDevice;
				
				trace("monitor Name:",displayDevice.monitor.name);
				trace("monitor id:",displayDevice.monitor.id);
				trace("monitor key:",displayDevice.monitor.key);
				trace("monitor friendly Name:",displayDevice.monitor.friendlyName);
				
				trace("device ID:",displayDevice.id);
				trace("device Key:",displayDevice.key);
				trace("device Name:",displayDevice.name);
				
				trace("device friendly Name:",displayDevice.friendlyName);
				trace("isPrimary:",displayDevice.isPrimary);
				trace("isActive:",displayDevice.isActive);
				trace("isRemovable:",displayDevice.isRemovable);
				trace("isVgaCampatible:",displayDevice.isVgaCampatible);
				trace("_______________________________")
			}
			
			trace("active key",currentDisplay.key);
			
			var hasSet:Boolean = ane.setDisplayResolution(currentDisplay.key, 1600, 900, 48);
			trace("hasSet",hasSet);
		}
	}
}