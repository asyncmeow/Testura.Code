using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Testura.Code.Builders.Base;
using Testura.Code.Builders.BuildMembers;
using Testura.Code.Generators.Class;
using Testura.Code.Generators.Common;
using Testura.Code.Models;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Testura.Code.Builders;

/// <summary>
/// Provides a builder to generate a class.
/// </summary>
public class ClassBuilder : TypeBuilderBase<ClassBuilder>
{
    private List<Parameter> _primaryConstructorParameters;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClassBuilder"/> class.
    /// </summary>
    /// <param name="name">Name of the class.</param>
    /// <param name="namespace">Name of the class namespace.</param>
    /// <param name="namespaceType">Type of namespace</param>
    public ClassBuilder(string name, string @namespace, NamespaceType namespaceType = NamespaceType.Classic)
        : base(name, @namespace, namespaceType)
    {
    }

    /// <summary>
    /// Add class fields.
    /// </summary>
    /// <param name="fields">A set of wanted fields.</param>
    /// <returns>The current class builder</returns>
    public ClassBuilder WithFields(params Field[] fields)
    {
        return With(new FieldBuildMember(fields.Select(FieldGenerator.Create)));
    }

    /// <summary>
    /// Add class fields.
    /// </summary>
    /// <param name="fields">An array of already declared fields.</param>
    /// <returns>The current class builder</returns>
    public ClassBuilder WithFields(params FieldDeclarationSyntax[] fields)
    {
        return With(new FieldBuildMember(fields));
    }

    /// <summary>
    /// Add class constructor.
    /// </summary>
    /// <param name="constructor">An already generated constructor.</param>
    /// <returns>The current class builder</returns>
    public ClassBuilder WithConstructor(params ConstructorDeclarationSyntax[] constructor)
    {
        return With(new ConstructorBuildMember(constructor));
    }

    /// <summary>
    /// Add primary constructor.
    /// </summary>
    /// <param name="parameters">Parameters in the constructor</param>
    /// <returns>The current class builder</returns>
    public ClassBuilder WithPrimaryConstructor(params Parameter[] parameters)
    {
        _primaryConstructorParameters = new List<Parameter>(parameters);
        return this;
    }

    protected override TypeDeclarationSyntax BuildBase()
    {
        var decl = ClassDeclaration(Name).WithBaseList(CreateBaseList()).WithModifiers(CreateModifiers());
        if (_primaryConstructorParameters != null && _primaryConstructorParameters.Any())
        {
            decl = decl.WithParameterList(
                ParameterGenerator.Create(_primaryConstructorParameters.ToArray()));
            if (HaveMembers)
            {
                decl = decl.WithOpenBraceToken(
                    Token(SyntaxKind.OpenBraceToken));
            }
        }

        return decl;
    }

    protected override TypeDeclarationSyntax FinishBase(TypeDeclarationSyntax type)
    {
        if (HaveMembers)
        {
            return type.WithCloseBraceToken(Token(SyntaxKind.CloseBraceToken));
        }

        return type.WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
    }
}
