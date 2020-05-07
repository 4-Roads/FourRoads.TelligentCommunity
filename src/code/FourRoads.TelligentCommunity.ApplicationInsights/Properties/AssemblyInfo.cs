using System.Reflection;
using System.Runtime.InteropServices;
using System.Web;
using FourRoads.TelligentCommunity.ApplicationInsights;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.

[assembly: AssemblyTitle("FourRoads.TelligentCommunity111.ApplicationInsights")]
[assembly: AssemblyDescription("Integration into Azure's application insights engine")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.

[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM

[assembly: Guid("13b32d98-e798-4204-aaf7-d8d996875970")]

[assembly: PreApplicationStartMethod(typeof(ExceptionHandlerStart), "Start")]