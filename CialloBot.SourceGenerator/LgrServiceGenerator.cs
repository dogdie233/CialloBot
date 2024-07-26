﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using System.Linq;

namespace CialloBot.SourceGenerator
{
    [Generator]
    public class LgrServiceGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            var events = GetEvents(context);
            var eventsSource = "";
            var bindSource = "";
            var unbindSource = "";
            foreach (var ent in events)
            {
                var eventArgType = ((GenericNameSyntax)((NullableTypeSyntax)ent.Declaration.Type).ElementType).TypeArgumentList.Arguments[0];
                var eventName = ent.Declaration.Variables[0].Identifier.Text;
                eventsSource += $@"
{ent.ToFullString()}
private void Wrapper{eventName}(BotContext context, {eventArgType.ToString()} e)
{{
    try
    {{
        this.{eventName}?.Invoke(context, e);
    }}
    catch (Exception ex)
    {{
        logger.LogError(ex, $""An exception was occurred when executing event {eventName} in plugin {{this.pluginId}}"");
    }}
}}";
                bindSource += $"this.GlobalInvoker.{eventName} += Wrapper{eventName};\n";
                unbindSource += $"this.GlobalInvoker.{eventName} -= Wrapper{eventName};\n";
            }

            var classSource = $@"// <auto-generated/>
using CialloBot.Services;

using Lagrange.Core;
using Lagrange.Core.Event;
using Lagrange.Core.Event.EventArg;
using static Lagrange.Core.Event.EventInvoker;

using Microsoft.Extensions.Logging;

using System;

namespace CialloBot.Plugin.ServiceWrapper;

public partial class LgrService
{{
    partial void RegisterEvents()
    {{
        {bindSource}
    }}

    partial void UnregisterEvents()
    {{
        {unbindSource}
    }}

    {eventsSource}
}
";
            context.AddSource("LgrService.Events.g.cs", classSource);
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }

        private EventFieldDeclarationSyntax[] GetEvents(GeneratorExecutionContext context)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(context.AdditionalFiles.First(t => t.Path.EndsWith("EventInvoker.Events.cs")).GetText());
            var root = syntaxTree.GetCompilationUnitRoot();

            ClassDeclarationSyntax eventsClass = null;
            foreach (var member in root.Members)
            {
                if (member is BaseNamespaceDeclarationSyntax namespaceSyntax)
                    eventsClass = namespaceSyntax.Members
                        .Where(mb => mb.IsKind(SyntaxKind.ClassDeclaration))
                        .Cast<ClassDeclarationSyntax>()
                        .First(c => c.Identifier.ValueText == "EventInvoker");
                else if (member is ClassDeclarationSyntax classSyntax && classSyntax.Identifier.ValueText == "EventInvoker")
                    eventsClass = classSyntax;

                if (eventsClass != null)
                    break;
            }

            var events = eventsClass.Members
                .Where(mb => mb.IsKind(SyntaxKind.EventFieldDeclaration))
                .Cast<EventFieldDeclarationSyntax>()
                .Where(syntax => syntax.Modifiers.Any(SyntaxKind.PublicKeyword));

            return events.ToArray();
        }
    }
}
