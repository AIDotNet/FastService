using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using FastService.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace FastService.Analyzers
{
	[Generator(LanguageNames.CSharp)]
	public class FastGenerator : IIncrementalGenerator
	{
		private ImmutableArray<ClassInfo> GetReferencedClasses(Compilation compilation)
		{
			var builder = ImmutableArray.CreateBuilder<ClassInfo>();

			foreach (var reference in compilation.References)
			{
				var symbol = compilation.GetAssemblyOrModuleSymbol(reference);
				if (symbol is IAssemblySymbol assemblySymbol)
				{
					foreach (var type in assemblySymbol.GlobalNamespace.GetNamespaceTypes())
					{
						// 检查类型是否继承自 FastService
						if (InheritsFromFastService(type))
						{
							var classInfo = GetClassInfo(type);
							if (classInfo != null)
							{
								builder.Add(classInfo);
							}
						}
					}
				}
			}

			return builder.ToImmutable();
		}

		// 新增方法：从类型符号获取类信息
		private ClassInfo? GetClassInfo(INamedTypeSymbol classSymbol)
		{
			// 类似于原来的 GetSemanticTargetForGeneration 方法
			// 获取命名空间、类名、属性、方法等信息
			var namespaceName = GetFullNamespace(classSymbol.ContainingNamespace);
			var className = classSymbol.Name;
			var methods = classSymbol.GetMembers().OfType<IMethodSymbol>()
				.Where(m => m.DeclaredAccessibility == Accessibility.Public && !m.IsStatic && m.MethodKind == MethodKind.Ordinary)
				.ToList();

			// 获取属性等，省略具体实现

			return new ClassInfo
			{
				Namespace = namespaceName,
				ClassName = className,
				// 其他属性赋值
				Methods = methods
			};
		}

		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			// 获取编译对象
			var compilationProvider = context.CompilationProvider;

			// 收集当前项目的类信息（保持原有功能）
			var classDeclarations = context.SyntaxProvider
				.CreateSyntaxProvider(
					predicate: IsCandidateClass,
					transform: GetSemanticTargetForGeneration)
				.Where(m => m != null)
				.Collect();

			// 注册源输出，生成辅助代码
			context.RegisterSourceOutput(compilationProvider.Combine(classDeclarations),
				(spc, source) =>
				{
					var (compilation, classes) = source;

					// 获取引用的程序集中的类型
					var referencedClasses = GetReferencedClasses(compilation);

					// 将当前项目和引用程序集的类合并
					var allClasses = classes.AddRange(referencedClasses);

					GenerateSource(spc, compilation, allClasses);
				});
		}

		/// <summary>
		/// 判断一个语法节点是否可能是目标类。
		/// </summary>
		private static bool IsCandidateClass(SyntaxNode node, CancellationToken _)
		{
			return node is ClassDeclarationSyntax c && c.BaseList != null;
		}

		/// <summary>
		/// 从语法上下文中提取目标类的信息。
		/// </summary>
		private static ClassInfo? GetSemanticTargetForGeneration(GeneratorSyntaxContext context, CancellationToken cancellationToken)
		{
			var classDeclaration = (ClassDeclarationSyntax)context.Node;
			var model = context.SemanticModel;

			// 获取类的符号信息
			if (model.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol classSymbol)
				return null;

			// 检查类是否继承自 FastService
			if (!InheritsFromFastService(classSymbol))
				return null;

			// 获取命名空间
			var namespaceName = GetFullNamespace(classSymbol.ContainingNamespace);

			// 获取类名
			var className = classSymbol.Name;

			// 获取类级别的特性
			var attributes = classSymbol.GetAttributes();

			// 获取 RouteAttribute
			var routeAttr = attributes.FirstOrDefault(a => a.AttributeClass?.Name == "RouteAttribute");
			var route = routeAttr?.ConstructorArguments.FirstOrDefault().Value as string ?? $"/api/{className.TrimEnd("Service")}";

			// 获取 Tags 属性
			var tagsAttr = attributes.FirstOrDefault(a => a.AttributeClass?.Name == "Tags");
			var tags = tagsAttr?.ConstructorArguments.FirstOrDefault().Value as string;

			// 获取 FilterAttributes
			var filterAttributes = attributes.Where(a => a.AttributeClass?.Name == "FilterAttribute" || a.AttributeClass?.Name == "Filter").ToList();

			// 获取所有公共非静态方法
			var methods = classSymbol.GetMembers().OfType<IMethodSymbol>()
				.Where(m => m.DeclaredAccessibility == Accessibility.Public && !m.IsStatic && m.MethodKind == MethodKind.Ordinary)
				.ToList();

			return new ClassInfo
			{
				Namespace = namespaceName,
				ClassName = className,
				Route = route,
				Tags = tags,
				FilterAttributes = filterAttributes,
				Methods = methods
			};
		}

		/// <summary>
		/// 检查一个类是否继承自 FastService。
		/// </summary>
		private static bool InheritsFromFastService(INamedTypeSymbol typeSymbol)
		{
			var baseType = typeSymbol.BaseType;
			while (baseType != null)
			{
				if (baseType.Name == "FastApi")
					return true;
				baseType = baseType.BaseType;
			}
			return false;
		}


		/// <summary>
		/// 获取完整的命名空间名称。
		/// </summary>
		private static string GetFullNamespace(INamespaceSymbol namespaceSymbol)
		{
			if (namespaceSymbol == null || namespaceSymbol.IsGlobalNamespace)
				return string.Empty;

			var parts = new Stack<string>();
			var current = namespaceSymbol;
			while (current != null && !current.IsGlobalNamespace)
			{
				parts.Push(current.Name);
				current = current.ContainingNamespace;
			}
			return string.Join(".", parts);
		}

		/// <summary>
		/// 生成辅助源代码，包括 DI 注册和 API 映射。
		/// </summary>
		private void GenerateSource(SourceProductionContext context, Compilation compilation, ImmutableArray<ClassInfo?> classes)
		{
			var classInfos = classes.Where(c => c != null).Select(c => c!).ToList();

			if (!classInfos.Any())
				return;

			// 生成 DI 注册代码
			var diRegistration = GenerateDIRegistration(classInfos);

			// 生成 API 映射代码
			var apiMappings = GenerateAPIMappings(classInfos, compilation, context.CancellationToken);

			// 构建最终的源代码
			var sourceBuilder = new StringBuilder();
			sourceBuilder.AppendLine("// <auto-generated/>");
			sourceBuilder.AppendLine("using Microsoft.AspNetCore.Builder;");
			sourceBuilder.AppendLine("using Microsoft.Extensions.DependencyInjection;");
			sourceBuilder.AppendLine();
			sourceBuilder.AppendLine("namespace FastService.Extensions");
			sourceBuilder.AppendLine("{");
			sourceBuilder.AppendLine("    public static class FastExtensions");
			sourceBuilder.AppendLine("    {");
			sourceBuilder.AppendLine("        public static IServiceCollection WithFast(this IServiceCollection services,ServiceLifetime lifetime = ServiceLifetime.Scoped)");
			sourceBuilder.AppendLine("        {");
			sourceBuilder.AppendLine(diRegistration);
			sourceBuilder.AppendLine("            return services;");
			sourceBuilder.AppendLine("        }");
			sourceBuilder.AppendLine();
			sourceBuilder.AppendLine("        public static WebApplication MapFast(this WebApplication app)");
			sourceBuilder.AppendLine("        {");
			sourceBuilder.AppendLine(apiMappings);
			sourceBuilder.AppendLine("            return app;");
			sourceBuilder.AppendLine("        }");
			sourceBuilder.AppendLine("    }");
			sourceBuilder.AppendLine("}");

			// 添加生成的源代码
			context.AddSource("FastExtensions.g.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
		}

		/// <summary>
		/// 生成 DI 注册代码。
		/// </summary>
		private string GenerateDIRegistration(List<ClassInfo> classInfos)
		{
			var sb = new StringBuilder();
			foreach (var classInfo in classInfos)
			{
				// 需要根据lifetime 创建不同的生命周期 
				sb.AppendLine($"			if(lifetime == ServiceLifetime.Singleton)");
				sb.AppendLine($"				services.AddSingleton<{classInfo.Namespace}.{classInfo.ClassName}>();");
				sb.AppendLine($"			else if(lifetime == ServiceLifetime.Scoped)");
				sb.AppendLine($"				services.AddScoped<{classInfo.Namespace}.{classInfo.ClassName}>();");
				sb.AppendLine($"			else if(lifetime == ServiceLifetime.Transient)");
				sb.AppendLine($"				services.AddTransient<{classInfo.Namespace}.{classInfo.ClassName}>();");
			}
			return sb.ToString();
		}

		/// <summary>
		/// 生成 API 映射代码。
		/// </summary>
		private string GenerateAPIMappings(List<ClassInfo> classInfos, Compilation compilation, CancellationToken cancellationToken)
		{
			var sb = new StringBuilder();
			foreach (var classInfo in classInfos)
			{
				var instanceName = Char.ToLowerInvariant(classInfo.ClassName[0]) + classInfo.ClassName.TrimEnd("Service");
				sb.AppendLine($"            var {instanceName} = app.MapGroup(\"{classInfo.Route}\"){GenerateClassAttributes(classInfo)};");

				foreach (var method in classInfo.Methods)
				{
					var methodCode = GenerateMethodMapping(method, classInfo, compilation, instanceName, cancellationToken);
					if (!string.IsNullOrWhiteSpace(methodCode))
						sb.AppendLine(methodCode);
				}
				sb.AppendLine();
			}
			return sb.ToString();
		}

		/// <summary>
		/// 生成类级别的特性代码（如过滤器和标签）。
		/// </summary>
		private string GenerateClassAttributes(ClassInfo classInfo)
		{
			var sb = new StringBuilder();

			// 添加类级别的过滤器
			foreach (var filterAttr in classInfo.FilterAttributes)
			{
				if (filterAttr.ConstructorArguments.Length > 0)
				{
					var filterTypes = filterAttr.ConstructorArguments[0].Values;
					foreach (var filter in filterTypes)
					{
						if (filter.Value is INamedTypeSymbol filterType)
						{
							sb.AppendLine($".AddEndpointFilter<{filterType.ToDisplayString()}>()");
						}
					}
				}
			}

			// 添加标签
			if (!string.IsNullOrEmpty(classInfo.Tags))
			{
				sb.AppendLine($".WithTags(\"{classInfo.Tags}\")");
			}

			return sb.ToString();
		}

		/// <summary>
		/// 生成方法级别的 API 映射代码。
		/// </summary>
		private string GenerateMethodMapping(IMethodSymbol method, ClassInfo classInfo, Compilation compilation, string instanceName, CancellationToken cancellationToken)
		{
			// 跳过具有 IgnoreRouteAttribute 的方法
			if (method.GetAttributes().Any(a => a.AttributeClass?.Name == "IgnoreRouteAttribute"))
				return string.Empty;

			// 确定 HTTP 方法和路由
			var (httpMethod, route) = DetermineHttpMethodAndRoute(method);

			// 获取方法级别的属性，排除 FilterAttribute
			var methodAttributes = method.GetAttributes().Where(a => a.AttributeClass?.Name != "FilterAttribute").ToList();

			// 构建属性字符串
			var attributesString = string.Join("\n", methodAttributes.Select(a => $"                [{a.ToString()}]"));
			// 获取方法级别的过滤器
			var filterAttributes = method.GetAttributes().Where(a => a.AttributeClass?.Name == "FilterAttribute").ToList();
			var filterExtensions = new StringBuilder();
			foreach (var filterAttr in filterAttributes)
			{
				if (filterAttr.ConstructorArguments.Length > 0)
				{
					var filterTypes = filterAttr.ConstructorArguments[0].Values;
					foreach (var filter in filterTypes)
					{
						if (filter.Value is INamedTypeSymbol filterType)
						{
							filterExtensions.AppendLine($"                .AddEndpointFilter<{filterType.ToDisplayString()}>()");
						}
					}
				}
			}

			// 获取方法参数
			var parameters = method.Parameters;
			var parameterList = string.Join(", ", parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}"));
			var parameterNames = string.Join(", ", parameters.Select(p => p.Name));

			// 确定服务实例名称
			var serviceInstance = Char.ToLowerInvariant(classInfo.ClassName[0]) + classInfo.ClassName.TrimEnd("Service");

			// 构建 Lambda 表达式
			string lambda;
			if (parameters.Length > 0)
			{
				lambda = $"async ({classInfo.Namespace}.{classInfo.ClassName} {serviceInstance}, {parameterList}) => await {serviceInstance}.{method.Name}({parameterNames})";
			}
			else
			{
				lambda = $"async ({classInfo.Namespace}.{classInfo.ClassName} {serviceInstance}) => await {serviceInstance}.{method.Name}()";
			}

			// 组合所有部分生成方法代码
			var methodCode = $@"
                {instanceName}.Map{httpMethod}(""{route}"", 
{attributesString}
			{lambda}){filterExtensions}; ";


			return methodCode;
		}

		/// <summary>
		/// 确定方法的 HTTP 方法和路由。
		/// </summary>
		private (string httpMethod, string route) DetermineHttpMethodAndRoute(IMethodSymbol method)
		{
			// 检查是否有 HTTP 方法特性
			var httpMethodAttr = method.GetAttributes().FirstOrDefault(a => a.AttributeClass != null &&
				(a.AttributeClass.Name == "HttpGetAttribute" ||
				 a.AttributeClass.Name == "HttpPostAttribute" ||
				 a.AttributeClass.Name == "HttpPutAttribute" ||
				 a.AttributeClass.Name == "HttpDeleteAttribute"));

			if (httpMethodAttr != null)
			{
				var attrName = httpMethodAttr.AttributeClass.Name;
				string httpMethod = attrName switch
				{
					"HttpGetAttribute" => "Get",
					"HttpPostAttribute" => "Post",
					"HttpPutAttribute" => "Put",
					"HttpDeleteAttribute" => "Delete",
					_ => "Post"
				};

				// 获取路由参数，如果有
				var route = httpMethodAttr.ConstructorArguments.FirstOrDefault().Value as string ?? method.Name;

				return (httpMethod, route);
			}

			// 根据方法名推断 HTTP 方法和路由
			var methodName = method.Name;
			string inferredHttpMethod = "Post";
			string inferredRoute = methodName;

			if (methodName.StartsWith("Get", StringComparison.OrdinalIgnoreCase))
			{
				inferredHttpMethod = "Get";
				inferredRoute = methodName.Substring(3);
			}
			else if (methodName.StartsWith("Remove", StringComparison.OrdinalIgnoreCase) || methodName.StartsWith("Delete", StringComparison.OrdinalIgnoreCase))
			{
				inferredHttpMethod = "Delete";
				inferredRoute = methodName.StartsWith("Remove", StringComparison.OrdinalIgnoreCase) ? methodName.Substring(6) : methodName.Substring(6);
			}
			else if (methodName.StartsWith("Post", StringComparison.OrdinalIgnoreCase) ||
					 methodName.StartsWith("Create", StringComparison.OrdinalIgnoreCase) ||
					 methodName.StartsWith("Add", StringComparison.OrdinalIgnoreCase) ||
					 methodName.StartsWith("Insert", StringComparison.OrdinalIgnoreCase))
			{
				inferredHttpMethod = "Post";
				inferredRoute = methodName.StartsWith("Post", StringComparison.OrdinalIgnoreCase) ? methodName.Substring(4) :
								 methodName.StartsWith("Create", StringComparison.OrdinalIgnoreCase) ? methodName.Substring(6) :
								 methodName.StartsWith("Add", StringComparison.OrdinalIgnoreCase) ? methodName.Substring(3) :
								 methodName.StartsWith("Insert", StringComparison.OrdinalIgnoreCase) ? methodName.Substring(6) :
								 methodName;
			}
			else if (methodName.StartsWith("Put", StringComparison.OrdinalIgnoreCase) ||
					 methodName.StartsWith("Update", StringComparison.OrdinalIgnoreCase) ||
					 methodName.StartsWith("Modify", StringComparison.OrdinalIgnoreCase))
			{
				inferredHttpMethod = "Put";
				inferredRoute = methodName.StartsWith("Put", StringComparison.OrdinalIgnoreCase) ? methodName.Substring(3) :
								 methodName.StartsWith("Update", StringComparison.OrdinalIgnoreCase) ? methodName.Substring(6) :
								 methodName.StartsWith("Modify", StringComparison.OrdinalIgnoreCase) ? methodName.Substring(6) :
								 methodName;
			}

			// 移除 'Async' 后缀
			if (inferredRoute.EndsWith("Async", StringComparison.OrdinalIgnoreCase))
			{
				inferredRoute = inferredRoute.Substring(0, inferredRoute.Length - 5);
			}

			// 移除 'Service' 后缀
			if (inferredRoute.EndsWith("Service", StringComparison.OrdinalIgnoreCase))
			{
				inferredRoute = inferredRoute.Substring(0, inferredRoute.Length - 7);
			}

			// 确保路由以 '/' 开头
			if (!inferredRoute.StartsWith("/"))
				inferredRoute = "/" + inferredRoute;

			return (inferredHttpMethod, inferredRoute);
		}

		/// <summary>
		/// 辅助类，用于存储类的信息。
		/// </summary>
		private class ClassInfo
		{
			public string Namespace { get; set; } = string.Empty;
			public string ClassName { get; set; } = string.Empty;
			public string Route { get; set; } = string.Empty;
			public string? Tags { get; set; }
			public List<AttributeData> FilterAttributes { get; set; } = new List<AttributeData>();
			public List<IMethodSymbol> Methods { get; set; } = new List<IMethodSymbol>();
		}
	}
}