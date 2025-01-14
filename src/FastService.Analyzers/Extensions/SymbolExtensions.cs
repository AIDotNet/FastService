using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace FastService.Analyzers.Extensions;

public static class SymbolExtensions
{
    public static IEnumerable<INamedTypeSymbol> GetNamespaceTypes(this INamespaceSymbol @namespace)
    {
        foreach (var type in @namespace.GetTypeMembers())
        {
            yield return type;

            // 递归获取嵌套的类型
            foreach (var nestedType in type.GetTypeMembers())
            {
                yield return nestedType;
            }
        }

        // 递归获取子命名空间的类型
        foreach (var nestedNamespace in @namespace.GetNamespaceMembers())
        {
            foreach (var type in nestedNamespace.GetNamespaceTypes())
            {
                yield return type;
            }
        }
    }
}