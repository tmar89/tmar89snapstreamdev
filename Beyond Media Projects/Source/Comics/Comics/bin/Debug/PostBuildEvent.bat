@echo off
if not exist "C:\Documents and Settings\All Users\Application Data\SnapStream\Beyond Media\Plugins\Comics" md "C:\Documents and Settings\All Users\Application Data\SnapStream\Beyond Media\Plugins\Comics"
copy /y "C:\Documents and Settings\Tommy\My Documents\Beyond Media Projects\Source\Comics\Comics\Plugins.xml" "C:\Documents and Settings\All Users\Application Data\SnapStream\Beyond Media\Plugins\Comics"
copy /y "C:\Documents and Settings\Tommy\My Documents\Beyond Media Projects\Source\Comics\Comics\manifest.xml" "C:\Documents and Settings\All Users\Application Data\SnapStream\Beyond Media\Plugins\Comics"
copy /y "C:\Documents and Settings\Tommy\My Documents\Beyond Media Projects\Source\Comics\Comics\ComicsScreen.xml" "C:\Documents and Settings\All Users\Application Data\SnapStream\Beyond Media\Plugins\Comics"
copy /y "C:\Documents and Settings\Tommy\My Documents\Beyond Media Projects\Source\Comics\Comics\ComicsSubscriptionsScreen.xml" "C:\Documents and Settings\All Users\Application Data\SnapStream\Beyond Media\Plugins\Comics"
copy /y "C:\Documents and Settings\Tommy\My Documents\Beyond Media Projects\Source\Comics\Comics\ComicsPreviewScreen.xml" "C:\Documents and Settings\All Users\Application Data\SnapStream\Beyond Media\Plugins\Comics"
copy /y "C:\Documents and Settings\Tommy\My Documents\Beyond Media Projects\Source\Comics\Comics\AvailableComics.xml" "C:\Documents and Settings\All Users\Application Data\SnapStream\Beyond Media\Plugins\Comics"
copy /y "C:\Documents and Settings\Tommy\My Documents\Beyond Media Projects\Source\Comics\Comics\bin\Debug\SnapStream.Plugins.Comics.dll" "C:\Documents and Settings\All Users\Application Data\SnapStream\Beyond Media\Plugins\Comics"
if not exist "C:\Documents and Settings\All Users\Application Data\SnapStream\Beyond Media\Plugins\Comics\Images" md "C:\Documents and Settings\All Users\Application Data\SnapStream\Beyond Media\Plugins\Comics\Images"
copy /y "C:\Documents and Settings\Tommy\My Documents\Beyond Media Projects\Source\Comics\Comics\Images\*.*" "C:\Documents and Settings\All Users\Application Data\SnapStream\Beyond Media\Plugins\Comics\Images"
if errorlevel 1 goto CSharpReportError
goto CSharpEnd
:CSharpReportError
echo Project error: A tool returned an error code from the build event
exit 1
:CSharpEnd