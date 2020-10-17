del setup.log
del remove.log

msiexec /i Chem4Word-Setup.3.1.15.Release.5.msi /l*v setup.log

rem pause

msiexec /uninstall Chem4Word-Setup.3.1.15.Release.5.msi /l*v remove.log

pause

rem find "Property(" setup.log > properties.log
