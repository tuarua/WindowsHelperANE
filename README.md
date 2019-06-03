# WindowsHelperANE

a set of methods for Adobe AIR for Windows

findWindowByTitle()  
showWindow()  
hideWindow()   
setForegroundWindow()   
getDisplayDevices()   
setDisplayResolution()   
registerHotKey()   
unregisterHotKey()   
getNumLogicalProcessors()   
startAtLogin()   


##### Windows Installation - Important!
The C# binaries(dlls) are now packaged inside the ANE. All of these **need to be deleted** from your AIRSDK.     
FreSharp.ane is now a required dependency for Windows. 

* This ANE was built with MS Visual Studio 2015. As such your machine (and user's machines) will need to have Microsoft Visual C++ 2015 Redistributable (x86) runtime installed.
https://www.microsoft.com/en-us/download/details.aspx?id=48145

* This ANE also uses .NET 4.7 Framework. This is included with Windows 10 April Update 2018.
