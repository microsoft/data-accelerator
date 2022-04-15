﻿
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Build", "CA1307:The behavior of 'string.Replace(string, string)' could vary based on the current user's locale settings. Replace this call in 'DataX.Config.KeyVault.HashGenerator.GetHashCode(string)' with a call to 'string.Replace(string, string, System.StringComparison)'.", Justification = "Critical issues only", Scope = "member", Target = "~M:DataX.Config.KeyVault.HashGenerator.GetHashCode(System.String)~System.String")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Build", "CA1822:Member GetKeyVault does not access instance data and can be marked as static (Shared in VisualBasic)", Justification = "Critical issues only", Scope = "member", Target = "~M:DataX.Config.KeyVault.KeyVaultClient.GetKeyVault~DataX.Utility.KeyVault.KeyVaultUtility")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Build", "CA1054:Change the type of parameter secretUri of method KeyVaultClient.ResolveSecretUriAsync(string) from string to System.Uri, or provide an overload to KeyVaultClient.ResolveSecretUriAsync(string) that allows secretUri to be passed as a System.Uri object.", Justification = "Critical issues only", Scope = "member", Target = "~M:DataX.Config.KeyVault.KeyVaultClient.ResolveSecretUriAsync(System.String)~System.Threading.Tasks.Task{System.String}")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Build", "CA2007:Do not directly await a Task without calling ConfigureAwait", Justification = "Critical issues only", Scope = "member", Target = "~M:DataX.Config.KeyVault.KeyVaultClient.SaveSecretAsync(System.String,System.String,System.String)~System.Threading.Tasks.Task{System.String}")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Build", "CA1054:Change the type of parameter secretUri of method KeyVaultClient.SaveSecretAsync(string, string, string) from string to System.Uri, or provide an overload to KeyVaultClient.SaveSecretAsync(string, string, string) that allows secretUri to be passed as a System.Uri object.", Justification = "Critical issues only", Scope = "member", Target = "~M:DataX.Config.KeyVault.KeyVaultClient.SaveSecretAsync(System.String,System.String,System.String)~System.Threading.Tasks.Task{System.String}")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Build", "CA2007:Do not directly await a Task without calling ConfigureAwait", Justification = "Critical issues only", Scope = "member", Target = "~M:DataX.Config.KeyVault.KeyVaultClient.SaveSecretAsync(System.String,System.String,System.String,System.Boolean)~System.Threading.Tasks.Task{System.String}")]