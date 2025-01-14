using Fast.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Fast.Analyzers
{
	[Generator(LanguageNames.CSharp)]
	public class FastGenerator : IIncrementalGenerator
	{
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			var classDeclarations = context.SyntaxProvider
				.CreateSyntaxProvider(
					predicate: (node, _) => node is ClassDeclarationSyntax,
					transform: (context, _) => (ClassDeclarationSyntax)context.Node)
				.Where(cls => cls.BaseList?.Types.Any(baseType => baseType.ToString() == "FastService") == true)
				.Collect();

			context.RegisterSourceOutput(classDeclarations, (context, classes) => GenerateSource(context, classes));
		}

		private void GenerateSource(SourceProductionContext context, IEnumerable<ClassDeclarationSyntax> classes)
		{
			var classNames = new List<string>();
			var apis = new List<string>();
			foreach (var classDeclaration in classes)
			{
				apis.Add(GenerateClass(classDeclaration, context));

				// 生成一个注册类，将上面所有类都注册到DI
				var className = classDeclaration.Identifier.Text;
				var namespaceName = ((NamespaceDeclarationSyntax)classDeclaration.Parent).Name.ToString();

				classNames.Add($"{namespaceName}.{className}");
			}
			var register = $@"
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;

namespace Fast.Extensions
{{
	public static class FastExtensions
	{{
		public static IServiceCollection WithFast(this IServiceCollection services)
		{{
			{string.Join("\n", classNames.Select(name => $"services.AddSingleton<{name}>(); "))}
			return services;
		}}
	
		public static WebApplication MapFast(this WebApplication app)
		{{

{string.Join("\n", apis)}

			return app;
		}}
		
	}}
}}
";
			context.AddSource("FastExtensions.cs", register);
		}

		public string GenerateClass(ClassDeclarationSyntax classDeclaration, SourceProductionContext context)
		{

			var className = classDeclaration.Identifier.Text;
			var namespaceName = ((NamespaceDeclarationSyntax)classDeclaration.Parent).Name.ToString();

			className = className.TrimEnd("Service");

			var service = $"{namespaceName}.{className}";
			// 获取这个类的RouteAttribute
			var routeAttribute = classDeclaration.AttributeLists
				.SelectMany(list => list.Attributes)
				.FirstOrDefault(attr => attr.Name.ToString() == "RouteAttribute");

			// 转换成RouteAttribute类型得到Route属性
			var route = routeAttribute?.ArgumentList?.Arguments
				.FirstOrDefault()
				?.Expression
				?.ToString()
				?.Trim('"') ?? "/api/" + className;

			// 扫描这个类中所有的方法，所有非static public 的方法
			var methods = classDeclaration.Members
				.OfType<MethodDeclarationSyntax>()
				.Where(method => method.Modifiers.Any(SyntaxKind.PublicKeyword) && !method.Modifiers.Any(SyntaxKind.StaticKeyword))
				.ToList();
			var attribute = string.Empty;
			// 生成一个新的类
			// 获取这个类是否有FilterAttribute
			var filterAttribute = classDeclaration.AttributeLists
				.SelectMany(list => list.Attributes)
				.FirstOrDefault(attr => attr.Name.ToString() == "Filter" || attr.Name.ToString() == "FilterAttribute");

			if (filterAttribute != null)
			{
				// 获取 FilterAttribute 的 Types 参数，得到所有的 Type
				var filters = filterAttribute.ArgumentList?.Arguments
					.Select(arg => arg.Expression)
					.OfType<TypeOfExpressionSyntax>()
					.Select(typeOfExpr =>
					{
						if (typeOfExpr.Type is QualifiedNameSyntax qualified)
						{
							return qualified.ToString();
						}
						else if (typeOfExpr.Type is IdentifierNameSyntax identifier)
						{
							var namespaceName = GetNamespaceFromIdentifier(identifier);
							return !string.IsNullOrEmpty(namespaceName)
								? $"{namespaceName}.{identifier.Identifier.Text}"
								: identifier.Identifier.Text;
						}
						return typeOfExpr.Type.ToString();
					})
					.ToList();

				if (filters?.Any() == true)
				{
					foreach (var type in filters)
					{
						attribute += $"\n				.AddEndpointFilter<{type}>()";
					}
				}
			}

			// 获取是否有Tags
			var tagsAttribute = classDeclaration.AttributeLists
				.SelectMany(list => list.Attributes)
				.FirstOrDefault(attr => attr.Name.ToString() == "Tags");

			if (tagsAttribute != null)
			{
				// Tags的注释
				var tags = tagsAttribute?.ArgumentList?.Arguments
				.FirstOrDefault()
				?.Expression
				?.ToString()
				?.Trim('"');

				if (!string.IsNullOrEmpty(tags))
				{
					attribute += $"\n				.WithTags(@\"{tags}\")";
				}

			}


				var api = $@"
			var {className.ToLower()} = app.MapGroup(""{route}""){attribute};
			
{string.Join("\n", methods.Select(x => GenerateMethod(x, $"{namespaceName}.{classDeclaration.Identifier.Text}", className.ToLower())).Where(x => !string.IsNullOrWhiteSpace(x)))}

";


			return api;
		}

		public string GenerateMethod(MethodDeclarationSyntax method, string service, string apiName)
		{
			// 如果继承IgnoreRouteAttribute则忽略
			if (method.AttributeLists.SelectMany(list => list.Attributes).Any(attr => attr.Name.ToString() == "IgnoreRouteAttribute"))
			{
				return "";
			}

			var methodName = method.Identifier.Text;
			// 获取方法的参数列表
			var parameters = method.ParameterList.Parameters;
			var parameterList = string.Join(", ", parameters.Select(p => GetFullTypeName(p.Type) + " " + p.Identifier.Text));
			var parameterNames = string.Join(", ", parameters.Select(p => p.Identifier.Text));

			// 添加一个辅助方法来获取完整的类型名称
			string GetFullTypeName(TypeSyntax type)
			{
				if (type is QualifiedNameSyntax qualified)
				{
					return qualified.ToString();
				}
				else if (type is IdentifierNameSyntax identifier)
				{
					// 获取identifier所在的命名空间
					var namespaceName = GetNamespaceFromIdentifier(identifier);

					// 对于基本类型和在当前命名空间下的类型，可能需要添加更多逻辑来解析完整命名空间
					return namespaceName + "." + identifier.Identifier.Text;
				}
				else if (type is GenericNameSyntax generic)
				{
					var typeArgs = string.Join(", ", generic.TypeArgumentList.Arguments.Select(GetFullTypeName));
					return $"{generic.Identifier.Text}<{typeArgs}>";
				}
				else if (type is ArrayTypeSyntax array)
				{
					return GetFullTypeName(array.ElementType) + "[]";
				}

				return type.ToString();
			}

			var (httpMethod, name) = ParseMethod(method);
			var routeAttribute = method.AttributeLists
				.SelectMany(list => list.Attributes)
				.FirstOrDefault(attr => attr.Name.ToString() == "RouteAttribute");

			var route = routeAttribute?.ArgumentList?.Arguments
				.FirstOrDefault()
				?.Expression
				?.ToString()
				?.Trim('"') ?? name;
			// 获取方法的完整注释
			var comment = string.Empty;
			var extend = string.Empty;
			if (!string.IsNullOrEmpty(comment))
			{
				extend = ".WithDescription(@\"" + comment + "\")";
			}

			// 获取方法是否有FilterAttribute
			var filterAttribute = method.AttributeLists
				.SelectMany(list => list.Attributes)
				.FirstOrDefault(attr => attr.Name.ToString() == "Filter" || attr.Name.ToString() == "FilterAttribute");

			if (filterAttribute != null)
			{
				// 获取 FilterAttribute 的 Types 参数，得到所有的 Type
				var filters = filterAttribute.ArgumentList?.Arguments
					.Select(arg => arg.Expression)
					.OfType<TypeOfExpressionSyntax>()
					.Select(typeOfExpr =>
					{
						if (typeOfExpr.Type is QualifiedNameSyntax qualified)
						{
							return qualified.ToString();
						}
						else if (typeOfExpr.Type is IdentifierNameSyntax identifier)
						{
							var namespaceName = GetNamespaceFromIdentifier(identifier);
							return !string.IsNullOrEmpty(namespaceName)
								? $"{namespaceName}.{identifier.Identifier.Text}"
								: identifier.Identifier.Text;
						}
						return typeOfExpr.Type.ToString();
					})
					.ToList();

				if (filters?.Any() == true)
				{
					foreach (var type in filters)
					{
						extend += $"\n				.AddEndpointFilter<{type}>()\n";
					}
				}
			}

			var attribute = string.Empty;

			// 获取方法中所有的特性,然后生成特性代码
			var attributes = method.AttributeLists.SelectMany(list => list.Attributes);
			foreach (var attr in attributes)
			{
				// 忽略FilterAttribute
				if (attr.Name.ToString() == "FilterAttribute")
				{
					continue;
				}

				attribute += $"[{attr.ToString()}]\n";
			}

			// 根据是否有参数生成不同的代码
			if (parameters.Count > 0)
			{
				return $@"
            {apiName}.Map{httpMethod}(""{route}"",{attribute} 
				async ({service} service, {parameterList}) =>
					await service.{methodName}({parameterNames}) ){extend};
        ";
			}
			else
			{
				return $@"
            {apiName}.Map{httpMethod}(""{route}"",{attribute}  async ({service} service) =>
				await service.{methodName}()){extend};
        ";
			}
		}

		public (string method, string name) ParseMethod(MethodDeclarationSyntax methodDeclaration)
		{
			//Debugger.Launch();
			// 获取它是否有HttpGet | HttpPost | HttpPut | HttpDelete
			var httpMethod = methodDeclaration.AttributeLists
				.SelectMany(list => list.Attributes)
				.FirstOrDefault(attr => attr.Name.ToString().StartsWith("Http"));

			// 根据方法名形成Method
			var methodName = methodDeclaration.Identifier.Text;
			var method = "Post";
			// Get Remove Delete Post Put Create Update Add Insert Modify
			// 如果方法名以这些开头则去掉，并且根据这个生成方法
			if (methodName.StartsWith("Get", System.StringComparison.CurrentCultureIgnoreCase))
			{
				methodName = methodName.Substring(3);
				method = "Get";
			}
			else if (methodName.StartsWith("Remove", System.StringComparison.CurrentCultureIgnoreCase) || methodName.StartsWith("Delete", System.StringComparison.CurrentCultureIgnoreCase))
			{
				methodName = methodName.Substring(6);
				method = "Delete";
			}
			else if (methodName.StartsWith("Post", System.StringComparison.CurrentCultureIgnoreCase) ||
				methodName.StartsWith("Create", System.StringComparison.CurrentCultureIgnoreCase) ||
				methodName.StartsWith("Add", System.StringComparison.CurrentCultureIgnoreCase) ||
				methodName.StartsWith("Insert", System.StringComparison.CurrentCultureIgnoreCase))
			{
				methodName = methodName.TrimStart("Post").TrimStart("Create").TrimStart("Add").TrimStart("Insert");
				method = "Post";
			}
			else if (methodName.StartsWith("Put", System.StringComparison.CurrentCultureIgnoreCase) ||
				methodName.StartsWith("Update", System.StringComparison.CurrentCultureIgnoreCase) ||
				methodName.StartsWith("Modify", System.StringComparison.CurrentCultureIgnoreCase))
			{
				methodName = methodName.TrimStart("Put").TrimStart("Update").TrimStart("Modify");
				method = "Put";
			}

			// 删除后缀Async
			if (methodName.EndsWith("Async"))
			{
				methodName = methodName.Substring(0, methodName.Length - 5);
			}

			if (methodName.EndsWith("Service"))
			{
				methodName = methodName.Substring(0, methodName.Length - 7);
			}

			// 如果有则使用这个
			if (httpMethod != null)
			{
				return (httpMethod.Name.ToString().Substring(4), methodName ?? methodDeclaration.Identifier.Text);
			}

			// 返回方法名和方法

			return (method, methodName);
		}
		string GetNamespaceFromIdentifier(IdentifierNameSyntax identifier)
		{
			// 从当前节点开始向上遍历，直到找到命名空间声明
			SyntaxNode currentNode = identifier;
			while (currentNode != null && !(currentNode is NamespaceDeclarationSyntax))
			{
				currentNode = currentNode.Parent;
			}

			if (currentNode is NamespaceDeclarationSyntax namespaceDeclaration)
			{
				return namespaceDeclaration.Name.ToString();
			}

			// 如果找不到命名空间，返回空字符串或者适当的默认值
			return string.Empty;
		}
	}
}
