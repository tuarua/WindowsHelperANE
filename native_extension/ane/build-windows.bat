REM Get the path to the script and trim to get the directory.
@echo off
SET SZIP="C:\Program Files\7-Zip\7z.exe"
SET AIR_PATH="D:\dev\sdks\AIR\AIRSDK_32\bin\"
echo Setting path to current directory to:
SET pathtome=%~dp0
cd %pathtome%

SET projectName=WindowsHelperANE

REM Setup the directory.
echo Making directories.

IF NOT EXIST platforms mkdir platforms
IF NOT EXIST platforms\win mkdir platforms\win
IF NOT EXIST platforms\win\x86  mkdir platforms\win\x86
IF NOT EXIST platforms\win\x86\release mkdir platforms\win\x86\release
IF NOT EXIST platforms\win\x64  mkdir platforms\win\x64
IF NOT EXIST platforms\win\x64\release mkdir platforms\win\x64\release



REM Copy SWC into place.
echo Copying SWC into place.
echo ..\bin\%projectName%.swc
copy ..\bin\%projectName%.swc .



REM contents of SWC.
echo Extracting files form SWC.
echo %projectName%.swc
copy %projectName%.swc %projectName%Extract.swc
ren %projectName%Extract.swc %projectName%Extract.zip

call %SZIP% e %projectName%Extract.zip

del %projectName%Extract.zip

REM Copy library.swf to folders.
echo Copying library.swf into place.
copy library.swf platforms\win\x86\release
copy library.swf platforms\win\x64\release


REM Copy native libraries into place.
echo Copying native libraries into place.

copy ..\..\native_library\win\%projectName%\x86\Release\%projectName%.dll platforms\win\x86\release
copy ..\..\native_library\win\%projectName%\x64\Release\%projectName%.dll platforms\win\x64\release

copy ..\..\native_library\win\%projectName%\x86\Release\%projectName%Lib.dll platforms\win\x86\release
copy ..\..\native_library\win\%projectName%\x64\Release\%projectName%Lib.dll platforms\win\x64\release


REM Run the build command.
echo Building Release.

call %AIR_PATH%adt.bat -package -target ane %projectName%.ane extension_win.xml -swc %projectName%.swc ^
-platform Windows-x86 -C .\platforms\win\x86\release %projectName%.dll %projectName%Lib.dll library.swf ^
-platform Windows-x86-64 -C .\platforms\win\x64\release %projectName%.dll %projectName%Lib.dll library.swf ^
-platform default -C . "library.swf"


call DEL /F /Q /A platforms\win\x86\release\%projectName%.dll
call DEL /F /Q /A platforms\win\x64\release\%projectName%.dll
call DEL /F /Q /A platforms\win\x86\release\%projectName%Lib.dll
call DEL /F /Q /A platforms\win\x64\release\%projectName%Lib.dll
call DEL /F /Q /A platforms\win\x86\release\library.swf
call DEL /F /Q /A platforms\win\x64\release\library.swf
call DEL /F /Q /A %projectName%.swc
call DEL /F /Q /A library.swf
call DEL /F /Q /A catalog.xml

echo FIN
