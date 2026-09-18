using System.Text;
using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>프로젝트 소스의 문법 구조를 보존하며 명시적 블록과 일정한 배치를 적용합니다.</summary>
internal static class Program
{
    /// <summary>지정 폴더만 정리하며 --check에서는 파일을 수정하지 않습니다.</summary>
    private static int Main(string[] arguments)
    {
        string root = Path.GetFullPath(arguments[0]);
        bool checkOnly = arguments.Contains("--check");
        int changed = 0;
        int count = 0;
        int undocumentedCount = 0;
        int unbracedCount = 0;
        int expressionBodyCount = 0;
        int throwCount = 0;
        var options = new CSharpParseOptions(LanguageVersion.Latest, preprocessorSymbols: new[] { "UNITY_EDITOR", "ENABLE_INPUT_SYSTEM", "SW_DEBUG_MODE" });
        foreach (string path in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories).Order())
        {
            string original = File.ReadAllText(path);
            SyntaxTree tree = CSharpSyntaxTree.ParseText(original, options);
            var errors = tree.GetDiagnostics().Where(value => value.Severity == DiagnosticSeverity.Error).ToArray();
            if (errors.Length != 0)
            {
                Console.Error.WriteLine(path + ": " + string.Join("; ", errors.Select(value => value.ToString())));
                return 2;
            }

            if (arguments.Contains("--audit"))
            {
                var undocumented = tree.GetRoot().DescendantNodes().OfType<MemberDeclarationSyntax>()
                    .Where(value => value is BaseMethodDeclarationSyntax or PropertyDeclarationSyntax or BaseTypeDeclarationSyntax)
                    .Where(value => !value.GetLeadingTrivia().Any(trivia => trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)))
                    .Select(value => value switch
                    {
                        BaseMethodDeclarationSyntax method => method switch
                        {
                            MethodDeclarationSyntax named => named.Identifier.Text,
                            ConstructorDeclarationSyntax constructor => constructor.Identifier.Text,
                            _ => method.Kind().ToString()
                        },
                        PropertyDeclarationSyntax property => property.Identifier.Text,
                        BaseTypeDeclarationSyntax type => type.Identifier.Text,
                        _ => "?"
                    }).ToArray();
                if (undocumented.Length > 0)
                {
                    Console.WriteLine(Path.GetFileName(path) + ": " + string.Join(", ", undocumented));
                }
                undocumentedCount += undocumented.Length;
                var nodes = tree.GetRoot().DescendantNodes().ToArray();
                unbracedCount += nodes.Count(value => value switch
                {
                    IfStatementSyntax statement => statement.Statement is not BlockSyntax,
                    ElseClauseSyntax clause => clause.Statement is not BlockSyntax and not IfStatementSyntax,
                    ForStatementSyntax statement => statement.Statement is not BlockSyntax,
                    ForEachStatementSyntax statement => statement.Statement is not BlockSyntax,
                    WhileStatementSyntax statement => statement.Statement is not BlockSyntax,
                    DoStatementSyntax statement => statement.Statement is not BlockSyntax,
                    UsingStatementSyntax statement => statement.Statement is not BlockSyntax,
                    LockStatementSyntax statement => statement.Statement is not BlockSyntax,
                    _ => false
                });
                expressionBodyCount += nodes.Count(value => value switch
                {
                    BaseMethodDeclarationSyntax method => method.ExpressionBody != null,
                    LocalFunctionStatementSyntax function => function.ExpressionBody != null,
                    AccessorDeclarationSyntax accessor => accessor.ExpressionBody != null,
                    _ => false
                });
                throwCount += nodes.Count(value => value is ThrowStatementSyntax or ThrowExpressionSyntax);
                count++;
                continue;
            }

            var rewritten = new ExplicitBlockRewriter().Visit(tree.GetRoot())!;
            string formatted = rewritten.NormalizeWhitespace("    ", "\r\n").ToFullString().TrimEnd() + "\r\n";
            formatted = ExpandDocumentation(formatted);
            formatted = IndentRegions(formatted);
            formatted = ApplyLayout(formatted, options);
            var outputErrors = CSharpSyntaxTree.ParseText(formatted, options).GetDiagnostics().Where(value => value.Severity == DiagnosticSeverity.Error).ToArray();
            if (outputErrors.Length != 0)
            {
                Console.Error.WriteLine(path + ": " + string.Join("; ", outputErrors.Select(value => value.ToString())));
                return 3;
            }
            if (formatted != original)
            {
                changed++;
                if (!checkOnly)
                {
                    File.WriteAllText(path, formatted, new UTF8Encoding(false));
                }
            }
            count++;
        }
        if (arguments.Contains("--audit"))
        {
            Console.WriteLine($"검사 파일: {count}, XML 주석 누락: {undocumentedCount}, 중괄호 누락: {unbracedCount}, 축약 함수: {expressionBodyCount}, 명시적 throw: {throwCount}");
            return undocumentedCount + unbracedCount + expressionBodyCount + throwCount > 0 ? 1 : 0;
        }
        Console.WriteLine($"검사 파일: {count}, {(checkOnly ? "정리 필요" : "정리 완료")}: {changed}");
        return checkOnly && changed > 0 ? 1 : 0;
    }

    private static string ExpandDocumentation(string source)
    {
        return Regex.Replace(source, @"(?m)^([ \t]*)/// <summary>([^\r\n]+)</summary>\r?$", match =>
            match.Groups[1].Value + "/// <summary>\r\n" + match.Groups[1].Value + "/// " + match.Groups[2].Value
            + "\r\n" + match.Groups[1].Value + "/// </summary>");
    }

    /// <summary>문법 토큰 사이의 공백만 바꾸어 긴 호출과 조건을 읽기 쉽게 나눕니다.</summary>
    private static string ApplyLayout(string source, CSharpParseOptions options)
    {
        var root = CSharpSyntaxTree.ParseText(source, options).GetCompilationUnitRoot();
        var edits = new Dictionary<(int Start, int Length), string>();
        string Indent(int position)
        {
            int start = source.LastIndexOf('\n', Math.Max(0, position - 1)) + 1;
            int end = start;
            while (end < source.Length && source[end] == ' ')
            {
                end++;
            }
            return source[start..end];
        }
        void Gap(SyntaxToken left, SyntaxToken right, string replacement)
        {
            int start = left.Span.End;
            int length = right.SpanStart - start;
            if (length >= 0 && source.AsSpan(start, length).Trim().IsEmpty)
            {
                edits[(start, length)] = replacement;
            }
        }
        bool IsLong(SyntaxNode node)
        {
            return node.Span.Length + Indent(node.SpanStart).Length > 120;
        }

        foreach (var list in root.DescendantNodes().OfType<ArgumentListSyntax>())
        {
            if (list.Arguments.Count > 1 && IsLong(list))
            {
                string continuation = "\r\n" + Indent(list.SpanStart) + "    ";
                Gap(list.OpenParenToken, list.Arguments[0].GetFirstToken(), continuation);
                foreach (var comma in list.Arguments.GetSeparators())
                {
                    Gap(comma, comma.GetNextToken(), continuation);
                }
            }
            if (list.Arguments.Count > 0 && list.Arguments.Last().GetLastToken().IsKind(SyntaxKind.CloseBraceToken))
            {
                Gap(list.Arguments.Last().GetLastToken(), list.CloseParenToken, "");
            }
        }
        foreach (var list in root.DescendantNodes().OfType<ParameterListSyntax>())
        {
            if (list.Parameters.Count > 1 && IsLong(list.Parent!))
            {
                // 본문 길이는 제외하고 선언부 길이만 확인합니다.
                int declarationStart = list.Parent!.SpanStart;
                if (list.Span.End - declarationStart + Indent(list.SpanStart).Length <= 120)
                {
                    continue;
                }
                string continuation = "\r\n" + Indent(list.SpanStart) + "    ";
                Gap(list.OpenParenToken, list.Parameters[0].GetFirstToken(), continuation);
                foreach (var comma in list.Parameters.GetSeparators())
                {
                    Gap(comma, comma.GetNextToken(), continuation);
                }
            }
        }
        foreach (var expression in root.DescendantNodes().OfType<BinaryExpressionSyntax>())
        {
            if (!expression.IsKind(SyntaxKind.LogicalAndExpression) && !expression.IsKind(SyntaxKind.LogicalOrExpression))
            {
                continue;
            }
            SyntaxNode outer = expression;
            while (outer.Parent is BinaryExpressionSyntax parent && (parent.IsKind(SyntaxKind.LogicalAndExpression) || parent.IsKind(SyntaxKind.LogicalOrExpression)))
            {
                outer = parent;
            }
            if (IsLong(outer))
            {
                Gap(expression.Left.GetLastToken(), expression.OperatorToken, "\r\n" + Indent(outer.SpanStart) + "    ");
            }
        }
        foreach (var conditional in root.DescendantNodes().OfType<ConditionalExpressionSyntax>())
        {
            SyntaxNode outer = conditional;
            int depth = 1;
            while (outer.Parent is ConditionalExpressionSyntax parent)
            {
                outer = parent;
                depth++;
            }
            if (IsLong(outer))
            {
                string continuation = "\r\n" + Indent(outer.SpanStart) + new string(' ', depth * 4);
                Gap(conditional.Condition.GetLastToken(), conditional.QuestionToken, continuation);
                Gap(conditional.WhenTrue.GetLastToken(), conditional.ColonToken, continuation);
            }
        }
        foreach (var field in root.DescendantNodes().OfType<FieldDeclarationSyntax>())
        {
            if (field.AttributeLists.Count > 0)
            {
                var last = field.AttributeLists.Last().GetLastToken();
                Gap(last, last.GetNextToken(), " ");
            }
        }
        foreach (var initializer in root.DescendantNodes().OfType<InitializerExpressionSyntax>())
        {
            SyntaxToken next = initializer.CloseBraceToken.GetNextToken();
            if (next.IsKind(SyntaxKind.CloseParenToken) || next.IsKind(SyntaxKind.CommaToken))
            {
                Gap(initializer.CloseBraceToken, next, "");
            }
        }
        foreach (var edit in edits.OrderByDescending(value => value.Key.Start))
        {
            source = source.Remove(edit.Key.Start, edit.Key.Length).Insert(edit.Key.Start, edit.Value);
        }
        // 문서 주석이 있는 멤버와 영역 종료는 빈 줄로 구분합니다.
        source = Regex.Replace(source, @"(?m)^([^\r\n]+)\r\n([ \t]*/// <summary>)", match =>
            match.Groups[1].Value.Trim() == "{" || match.Groups[1].Value.TrimStart().StartsWith("#region") || match.Groups[1].Value.TrimStart().StartsWith("///")
                ? match.Value : match.Groups[1].Value + "\r\n\r\n" + match.Groups[2].Value);
        source = Regex.Replace(source, @"(?m)([^\r\n])\r\n([ \t]*#endregion)", "$1\r\n\r\n$2");
        source = Regex.Replace(source, @"(?m)([ \t]*#endregion[^\r\n]*)\r\n\r\n([ \t]*})", "$1\r\n$2");

        // 조건부 using은 전처리 범위를 유지하도록 기존 순서를 보존합니다.
        if (root.Usings.Count > 0 && !root.Usings.Any(value => value.ContainsDirectives))
        {
            var grouped = root.Usings.Select(value => value.WithoutTrivia().ToString()).Distinct()
                .OrderBy(value => value.StartsWith("using System") ? 0 : value.StartsWith("using Unity") ? 1 : value.StartsWith("using SW.") ? 3 : value.StartsWith("using ProjectT.") ? 4 : 2)
                .ThenBy(value => value.TrimEnd(';'), StringComparer.Ordinal).ToArray();
            string group = "";
            var directives = new List<string>();
            foreach (string directive in grouped)
            {
                string current = directive.StartsWith("using System") || directive.StartsWith("using Unity") ? "framework"
                    : directive.StartsWith("using SW.") ? "SW" : directive.StartsWith("using ProjectT.") ? "ProjectT" : "packages";
                if (group != "" && group != current)
                {
                    directives.Add("");
                }
                directives.Add(directive);
                group = current;
            }
            source = Regex.Replace(source, @"\A(?:using [^\r\n]+;\r\n)+", string.Join("\r\n", directives) + "\r\n");
        }
        return source;
    }

    private static string IndentRegions(string source)
    {
        string[] lines = source.Split("\r\n");
        var indents = new Stack<string>();
        for (int index = 0; index < lines.Length; index++)
        {
            string trimmed = lines[index].TrimStart();
            if (trimmed.StartsWith("#region "))
            {
                string indent = "";
                for (int next = index + 1; next < lines.Length; next++)
                {
                    if (string.IsNullOrWhiteSpace(lines[next]) || lines[next].TrimStart().StartsWith("#"))
                    {
                        continue;
                    }
                    indent = lines[next][..(lines[next].Length - lines[next].TrimStart().Length)];
                    break;
                }
                indents.Push(indent);
                lines[index] = indent + trimmed;
            }
            else if (trimmed.StartsWith("#endregion") && indents.Count > 0)
            {
                lines[index] = indents.Pop() + trimmed;
            }
        }
        return string.Join("\r\n", lines);
    }
}

/// <summary>조건문·반복문과 함수를 명시적인 블록으로 변환합니다.</summary>
internal sealed class ExplicitBlockRewriter : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        var result = (ClassDeclarationSyntax)base.VisitClassDeclaration(node)!;
        if (node.DescendantTrivia().Any(value => value.IsKind(SyntaxKind.RegionDirectiveTrivia)) || result.Members.Count == 0)
        {
            return result;
        }

        var members = result.Members.ToArray();
        string? previousCategory = null;
        bool functionsStarted = false;
        for (int index = 0; index < members.Length; index++)
        {
            var member = members[index];
            functionsStarted |= member is BaseMethodDeclarationSyntax;
            string category = functionsStarted ? "함수" : member is FieldDeclarationSyntax ? "필드"
                : member is PropertyDeclarationSyntax or EventFieldDeclarationSyntax or EventDeclarationSyntax ? "프로퍼티" : "데이터";
            if (category != previousCategory)
            {
                string prefix = index > 0 ? "#endregion // " + previousCategory + "\r\n\r\n" : "";
                members[index] = member.WithLeadingTrivia(SyntaxFactory.ParseLeadingTrivia(prefix + "#region " + category + "\r\n").AddRange(member.GetLeadingTrivia()));
                previousCategory = category;
            }
        }
        return result.WithMembers(SyntaxFactory.List(members)).WithCloseBraceToken(result.CloseBraceToken.WithLeadingTrivia(
            SyntaxFactory.ParseLeadingTrivia("#endregion // " + previousCategory + "\r\n").AddRange(result.CloseBraceToken.LeadingTrivia)));
    }

    private static BlockSyntax Block(StatementSyntax statement)
    {
        return statement as BlockSyntax ?? SyntaxFactory.Block(statement);
    }

    private static BlockSyntax MethodBody(ArrowExpressionClauseSyntax expression, bool returnsVoid)
    {
        StatementSyntax statement = expression.Expression is ThrowExpressionSyntax thrown
            ? SyntaxFactory.ThrowStatement(thrown.Expression)
            : returnsVoid ? SyntaxFactory.ExpressionStatement(expression.Expression) : SyntaxFactory.ReturnStatement(expression.Expression);
        return SyntaxFactory.Block(statement);
    }

    public override SyntaxNode? VisitIfStatement(IfStatementSyntax node)
    {
        var result = (IfStatementSyntax)base.VisitIfStatement(node)!;
        return result.WithStatement(Block(result.Statement));
    }

    public override SyntaxNode? VisitElseClause(ElseClauseSyntax node)
    {
        var result = (ElseClauseSyntax)base.VisitElseClause(node)!;
        return result.Statement is IfStatementSyntax ? result : result.WithStatement(Block(result.Statement));
    }

    public override SyntaxNode? VisitForStatement(ForStatementSyntax node)
    {
        var result = (ForStatementSyntax)base.VisitForStatement(node)!;
        return result.WithStatement(Block(result.Statement));
    }

    public override SyntaxNode? VisitForEachStatement(ForEachStatementSyntax node)
    {
        var result = (ForEachStatementSyntax)base.VisitForEachStatement(node)!;
        return result.WithStatement(Block(result.Statement));
    }

    public override SyntaxNode? VisitWhileStatement(WhileStatementSyntax node)
    {
        var result = (WhileStatementSyntax)base.VisitWhileStatement(node)!;
        return result.WithStatement(Block(result.Statement));
    }

    public override SyntaxNode? VisitDoStatement(DoStatementSyntax node)
    {
        var result = (DoStatementSyntax)base.VisitDoStatement(node)!;
        return result.WithStatement(Block(result.Statement));
    }

    public override SyntaxNode? VisitUsingStatement(UsingStatementSyntax node)
    {
        var result = (UsingStatementSyntax)base.VisitUsingStatement(node)!;
        return result.WithStatement(Block(result.Statement));
    }

    public override SyntaxNode? VisitLockStatement(LockStatementSyntax node)
    {
        var result = (LockStatementSyntax)base.VisitLockStatement(node)!;
        return result.WithStatement(Block(result.Statement));
    }

    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        var result = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node)!;
        if (result.ExpressionBody == null)
        {
            return result;
        }
        return result.WithBody(MethodBody(result.ExpressionBody, result.ReturnType.ToString() == "void")).WithExpressionBody(null).WithSemicolonToken(default);
    }

    public override SyntaxNode? VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
    {
        var result = (ConstructorDeclarationSyntax)base.VisitConstructorDeclaration(node)!;
        return result.ExpressionBody == null ? result : result.WithBody(MethodBody(result.ExpressionBody, true)).WithExpressionBody(null).WithSemicolonToken(default);
    }

    public override SyntaxNode? VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
    {
        var result = (LocalFunctionStatementSyntax)base.VisitLocalFunctionStatement(node)!;
        return result.ExpressionBody == null ? result : result.WithBody(MethodBody(result.ExpressionBody, result.ReturnType.ToString() == "void")).WithExpressionBody(null).WithSemicolonToken(default);
    }

    public override SyntaxNode? VisitAccessorDeclaration(AccessorDeclarationSyntax node)
    {
        var result = (AccessorDeclarationSyntax)base.VisitAccessorDeclaration(node)!;
        return result.ExpressionBody == null ? result : result.WithBody(MethodBody(result.ExpressionBody, !result.IsKind(SyntaxKind.GetAccessorDeclaration))).WithExpressionBody(null).WithSemicolonToken(default);
    }
}
