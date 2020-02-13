﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.MakeClassAbstract
{
    internal abstract class AbstractMakeClassAbstractCodeFixProvider<TClassDeclarationSyntax> : SyntaxEditorBasedCodeFixProvider
        where TClassDeclarationSyntax : SyntaxNode
    {
        protected abstract bool IsValidRefactoringContext(SyntaxNode? node, out TClassDeclarationSyntax? classDeclaration);

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.Compile;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            if (IsValidRefactoringContext(context.Diagnostics[0].Location?.FindNode(context.CancellationToken), out _))
            {
                context.RegisterCodeFix(
                    new MyCodeAction(c => FixAsync(context.Document, context.Diagnostics[0], c)),
                    context.Diagnostics);
            }

            return Task.CompletedTask;
        }

        protected sealed override Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor,
            CancellationToken cancellationToken)
        {
            for (var i = 0; i < diagnostics.Length; i++)
            {
                if (IsValidRefactoringContext(diagnostics[i].Location?.FindNode(cancellationToken), out var classDeclaration))
                {
                    editor.ReplaceNode(classDeclaration,
                        (currentClassDeclaration, generator) => generator.WithModifiers(currentClassDeclaration, DeclarationModifiers.Abstract));
                }
            }

            return Task.CompletedTask;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(FeaturesResources.Make_class_abstract, createChangedDocument, FeaturesResources.Make_class_abstract)
            {
            }
        }
    }
}
