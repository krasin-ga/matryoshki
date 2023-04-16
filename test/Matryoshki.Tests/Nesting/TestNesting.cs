using Matryoshki.Abstractions;

namespace Matryoshki.Tests.Nesting;

public record TestNesting : INesting<SimpleAdornment, MemberNameAdornment>;