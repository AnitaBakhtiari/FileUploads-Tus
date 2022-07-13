// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly:
    SuppressMessage("Performance", "U2U1105:Do not use string interpolation to concatenate strings",
        Justification = "Readability")]
[assembly: SuppressMessage("Design", "RCS1090:Call 'ConfigureAwait(false)'.", Justification = "<Pending>")]
[assembly:
    SuppressMessage("Design", "RCS1194:Implement exception constructors.", Justification = "Will add when needed")]