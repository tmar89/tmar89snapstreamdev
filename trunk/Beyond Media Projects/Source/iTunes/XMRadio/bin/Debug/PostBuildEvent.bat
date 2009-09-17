@echo off
if not exist "C:\Documents and Settings\All Users\Application Data\SnapStream\Beyond Media\Plugins\iTunes" md "C:\Documents and Settings\All Users\Application Data\SnapStream\Beyond Media\Plugins\iTunes"
copy /y "C:\Documents and Settings\Tommy\My Documents\Beyond Media Projects\Source\iTunes\XMRadio\Plugins.xml" "C:\Documents and Settings\All Users\Application Data\SnapStream\Beyond Media\Plugins\iTunes"
copy /y "C:\Documents and Settings\Tommy\My Documents\Beyond Media Projects\Source\iTunes\XMRadio\manifest.xml" "C:\Documents and Settings\All Users\Application Data\SnapStream\Beyond Media\Plugins\iTunes"
copy /y "C:\Documents and Settings\Tommy\My Documents\Beyond Media Projects\Source\iTunes\XMRadio\iTunesScreen.xml" "C:\Documents and Settings\All Users\Application Data\SnapStream\Beyond Media\Plugins\iTunes"
copy /y "C:\Documents and Settings\Tommy\My Documents\Beyond Media Projects\Source\iTunes\XMRadio\iTunesSettingsScreen.xml" "C:\Documents and Settings\All Users\Application Data\SnapStream\Beyond Media\Plugins\iTunes"
copy /y "C:\Documents and Settings\Tommy\My Documents\Beyond Media Projects\Source\iTunes\XMRadio\obj\Interop.WMPLib.dll" "C:\Documents and Settings\All Users\Application Data\SnapStream\Beyond Media\Plugins\iTunes"
copy /y "C:\Documents and Settings\Tommy\My Documents\Beyond Media Projects\Source\iTunes\XMRadio\bin\Debug\SnapStream.Plugins.iTunes.dll" "C:\Documents and Settings\All Users\Application Data\SnapStream\Beyond Media\Plugins\iTunes"
if errorlevel 1 goto CSharpReportError
goto CSharpEnd
:CSharpReportError
echo Project error: A tool returned an error code from the build event
exit 1
:CSharpEnd