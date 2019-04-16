
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Build", "CA1305:The behavior of 'string.Format(string, params object[])' could vary based on the current user's locale settings. Replace this call in 'ServiceEventSource.Message(string, params object[])' with a call to 'string.Format(IFormatProvider, string, params object[])'.", Justification = "Critical issues only", Scope = "member", Target = "~M:DataX.ServiceHost.ServiceFabric.ServiceEventSource.Message(System.String,System.Object[])")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Build", "CA1305:The behavior of 'string.Format(string, params object[])' could vary based on the current user's locale settings. Replace this call in 'ServiceEventSource.ServiceMessage(ServiceContext, string, params object[])' with a call to 'string.Format(IFormatProvider, string, params object[])'.", Justification = "Critical issues only", Scope = "member", Target = "~M:DataX.ServiceHost.ServiceFabric.ServiceEventSource.ServiceMessage(System.Fabric.ServiceContext,System.String,System.Object[])")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Build", "CA1801:Parameter requestTypeName of method ServiceRequestFailed is never used. Remove the parameter or use it in the method body.", Justification = "Critical issues only", Scope = "member", Target = "~M:DataX.ServiceHost.ServiceFabric.ServiceEventSource.ServiceRequestFailed(System.String,System.String)")]
