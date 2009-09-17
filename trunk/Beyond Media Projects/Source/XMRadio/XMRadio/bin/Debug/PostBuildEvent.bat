@echo off
if not exist "C:\Documents and Settings\All Users\Application Data\SnapStream\Beyond Media\Plugins\XMRadio" md "C:\Documents and Settings\All Users\Application Data\SnapStream\Beyond Media\Plugins\XMRadio"
copy /y "C:\Documents and Settings\Tommy\My Documents\Beyond Media Projects\Source\XMRadio\XMRadio\Plugins.xml" "C:\Documents and Settings\All Users\Application Data\SnapStream\Beyond Media\Plugins\XMRadio"
copy /y "C:\Documents and Settings\Tommy\My Documents\Beyond Media Projects\Source\XMRadio\XMRadio\manifest.xml" "C:\Documents and Settings\All Users\Application Data\SnapStream\Beyond Media\Plugins\XMRadio"
if not exist "C:\Documents and Settings\All Users\Application Data\SnapStream\Beyond Media\Plugins\XMRadio\Images" md "C:\Documents and Settings\All Users\Application Data\SnapStream\Beyond Media\Plugins\XMRadio\Images"  
copy /y "C:\Documents and Settings\Tommy\My Documents\Beyond Media Projects\Source\XMRadio\XMRadio\Images\*.*" "C:\Documents and Settings\All Users\Application Data\SnapStream\Beyond Media\Plugins\XMRadio\Images"  
copy /y "C:\Documents and Settings\Tommy\My Documents\Beyond Media Projects\Source\XMRadio\XMRadio\XMRadioScreen.xml" "C:\Documents and Settings\All Users\Application Data\SnapStream\Beyond Media\Plugins\XMRadio"
copy /y "C:\Documents and Settings\Tommy\My Documents\Beyond Media Projects\Source\XMRadio\XMRadio\XMSettingsScreen.xml" "C:\Documents and Settings\All Users\Application Data\SnapStream\Beyond Media\Plugins\XMRadio"
copy /y "C:\Documents and Settings\Tommy\My Documents\Beyond Media Projects\Source\XMRadio\XMRadio\XMVolumeOverlaySkin.xml" "C:\Documents and Settings\All Users\Application Data\SnapStream\Beyond Media\Plugins\XMRadio"
copy /y "C:\Documents and Settings\Tommy\My Documents\Beyond Media Projects\Source\XMRadio\XMRadio\obj\Interop.WMPLib.dll" "C:\Documents and Settings\All Users\Application Data\SnapStream\Beyond Media\Plugins\XMRadio"
copy /y "C:\Documents and Settings\Tommy\My Documents\Beyond Media Projects\Source\XMRadio\XMRadio\bin\Debug\SnapStream.Plugins.XMRadio.dll" "C:\Documents and Settings\All Users\Application Data\SnapStream\Beyond Media\Plugins\XMRadio"
if errorlevel 1 goto CSharpReportError
goto CSharpEnd
:CSharpReportError
echo Project error: A tool returned an error code from the build event
exit 1
:CSharpEnd