package {

import flash.display.Sprite;

import com.tuarua.WindowsHelperANE;

public class Main extends Sprite {
    private var ane:WindowsHelperANE = new WindowsHelperANE();

    public function Main() {
        var foundWindowTitle:String = ane.findWindowByTitle("Google Chrome");
        if(foundWindowTitle){
            trace("We have found window:",foundWindowTitle);
            ane.setForegroundWindow();
            ane.showWindow(true);//true to maximise
        }

    }
}
}
