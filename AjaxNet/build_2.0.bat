REM del "release\AjaxNet.2(including webevent).dll"
REM /define:"TRACE;NET20;NET20external;WEBEVENT;%DEFINE%"
REM ren "release\AjaxNet.2.dll" "release\AjaxNet.2(including webevent).dll"

del release\AjaxNet.2.dll
"%NET20%\csc.exe" %ARG% /out:"release\AjaxNet.2.dll" /target:library /define:"TRACE;NET20;NET20external;%DEFINE%" /r:"System.dll" /r:"System.Data.dll" /r:"System.Drawing.dll" /r:"System.Web.dll" /r:"System.Web.Services.dll" /r:"System.Xml.dll" /r:"System.Configuration.dll" "AssemblyInfo.cs" "Attributes\*.cs" "Configuration\*.cs" "Handler\*.cs" "Handler\AjaxProcessors\*.cs" "Handler\Security\*.cs" "Interfaces\*.cs" "JSON\Converters\*.cs" "JSON\Interfaces\*.cs" "JSON\*.cs" "JSON\JavaScriptObjects\*.cs" "Managment\*.cs" "Security\*.cs" "Services\*.cs" "Utilities\*.cs" /res:prototype.js,AjaxNet.2.prototype.js /res:core.js,AjaxNet.2.core.js /res:ms.js,AjaxNet.2.ms.js
