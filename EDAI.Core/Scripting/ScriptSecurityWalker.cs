using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EDAI.Core.Scripting;

/// <summary>
/// Walks a Roslyn syntax tree and accumulates violations for patterns that are
/// never permitted in scripts regardless of enabled permissions.
/// Run before compilation — a non-empty Violations list means the script must be rejected.
/// </summary>
internal sealed class ScriptSecurityWalker : CSharpSyntaxWalker
{
    private readonly List<string> _violations = [];
    public IReadOnlyList<string> Violations => _violations;

    // Reflection API members that are always blocked.
    private static readonly HashSet<string> BlockedMemberNames = new(StringComparer.Ordinal)
    {
        "Assembly",
        "GetMethod", "GetMethods",
        "GetProperty", "GetProperties",
        "GetField", "GetFields",
        "GetConstructor", "GetConstructors",
        "GetMembers", "GetMember",
        "GetRuntimeMethod", "GetRuntimeProperty", "GetRuntimeField", "GetRuntimeMember",
        "InvokeMember",
        "CreateInstance",
    };

    public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
    {
        var name = node.Name.Identifier.Text;
        if (BlockedMemberNames.Contains(name))
            _violations.Add($"Access to '.{name}' is not permitted in scripts.");
        base.VisitMemberAccessExpression(node);
    }

    public override void VisitMemberBindingExpression(MemberBindingExpressionSyntax node)
    {
        var name = node.Name.Identifier.Text;
        if (BlockedMemberNames.Contains(name))
            _violations.Add($"Access to '.{name}' is not permitted in scripts.");
        base.VisitMemberBindingExpression(node);
    }

    public override void VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        if (node.Expression is MemberAccessExpressionSyntax ma)
        {
            var typeName   = ma.Expression.ToString();
            var methodName = ma.Name.Identifier.Text;

            if (typeName == "Type" && methodName == "GetType")
                _violations.Add("Type.GetType() is not permitted in scripts.");

            if (typeName == "Assembly" && methodName is "Load" or "LoadFrom" or "LoadFile" or "LoadWithPartialName")
                _violations.Add($"Assembly.{methodName}() is not permitted in scripts.");

            if (typeName == "Activator" && methodName is "CreateInstance" or "CreateInstanceFrom")
                _violations.Add($"Activator.{methodName}() is not permitted in scripts.");
        }
        base.VisitInvocationExpression(node);
    }

    public override void VisitIdentifierName(IdentifierNameSyntax node)
    {
        if (node.Identifier.Text == "dynamic")
            _violations.Add("The 'dynamic' type is not permitted in scripts.");

        if (node.Identifier.Text == "AppDomain")
            _violations.Add("'AppDomain' is not permitted in scripts.");

        base.VisitIdentifierName(node);
    }

    public override void VisitUnsafeStatement(UnsafeStatementSyntax node)
    {
        _violations.Add("Unsafe code is not permitted in scripts.");
        base.VisitUnsafeStatement(node);
    }

    public override void VisitFixedStatement(FixedStatementSyntax node)
    {
        _violations.Add("Fixed statements are not permitted in scripts.");
        base.VisitFixedStatement(node);
    }

    public override void VisitAttribute(AttributeSyntax node)
    {
        var name = node.Name.ToString();
        if (name.EndsWith("DllImport", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith("LibraryImport", StringComparison.OrdinalIgnoreCase))
            _violations.Add("P/Invoke attributes are not permitted in scripts.");
        base.VisitAttribute(node);
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        if (node.Modifiers.Any(m => m.IsKind(SyntaxKind.ExternKeyword)))
            _violations.Add("Extern methods are not permitted in scripts.");
        base.VisitMethodDeclaration(node);
    }
}
