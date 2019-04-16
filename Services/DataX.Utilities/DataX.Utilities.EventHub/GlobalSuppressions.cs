
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Build", "CA2234:Modify 'EventHubUtil.CreateIotHubEndpointConsumerGroup(string)' to call 'HttpClient.PutAsync(Uri, HttpContent)' instead of 'HttpClient.PutAsync(string, HttpContent)'.", Justification = "Critical issues only", Scope = "member", Target = "~M:DataX.Utilities.EventHub.EventHubUtil.CreateIotHubEndpointConsumerGroup(System.String)~System.Boolean")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Build", "CA2234:Modify 'EventHubUtil.DeleteIotHubEndpointConsumerGroup(string)' to call 'HttpClient.DeleteAsync(Uri)' instead of 'HttpClient.DeleteAsync(string)'.", Justification = "Critical issues only", Scope = "member", Target = "~M:DataX.Utilities.EventHub.EventHubUtil.DeleteIotHubEndpointConsumerGroup(System.String)~System.Boolean")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Build", "CA1822:Member GetAccessToken does not access instance data and can be marked as static (Shared in VisualBasic)", Justification = "Critical issues only", Scope = "member", Target = "~M:DataX.Utilities.EventHub.EventHubUtil.GetAccessToken(System.String,System.String,System.String)~System.String")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Build", "CA1724:The type name EventHub conflicts in whole or in part with the namespace name 'DataX.Utilities.EventHub'. Change either name to eliminate the conflict.", Justification = "Critical issues only", Scope = "type", Target = "~T:DataX.Utilities.EventHub.EventHub")]

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "Critical issues only", Scope = "member", Target = "~F:DataX.Utilities.EventHub.EventHubUtil._defaultLocation")]
