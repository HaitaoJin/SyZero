using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SyZero.Service
{
    internal static class ServiceEndpointMetadataCollector
    {
        public static List<ServiceApiEndpointInfo> Collect(IServiceProvider serviceProvider)
        {
            var endpoints = new List<ServiceApiEndpointInfo>();
            try
            {
                if (!TryCollectFromApiDescriptions(serviceProvider, endpoints))
                {
                    TryCollectFromEndpointDataSources(serviceProvider, endpoints);
                }
            }
            catch
            {
                return new List<ServiceApiEndpointInfo>();
            }

            return endpoints
                .Where(item => !string.IsNullOrWhiteSpace(item.Method) && !string.IsNullOrWhiteSpace(item.Path))
                .GroupBy(item => item.Method + "::" + NormalizePath(item.Path), StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(item => item.Path, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.Method, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool TryCollectFromApiDescriptions(IServiceProvider serviceProvider, List<ServiceApiEndpointInfo> endpoints)
        {
            var providerType = FindType(
                "Microsoft.AspNetCore.Mvc.ApiExplorer.IApiDescriptionGroupCollectionProvider, Microsoft.AspNetCore.Mvc.ApiExplorer",
                "Microsoft.AspNetCore.Mvc.ApiExplorer.IApiDescriptionGroupCollectionProvider");
            if (providerType == null)
            {
                return false;
            }

            var provider = serviceProvider.GetService(providerType);
            if (provider == null)
            {
                return false;
            }

            var groups = GetPropertyValue(provider, "ApiDescriptionGroups");
            var items = AsEnumerable(GetPropertyValue(groups, "Items"));
            if (items == null)
            {
                return false;
            }

            foreach (var group in items)
            {
                var groupName = GetStringProperty(group, "GroupName");
                var descriptions = AsEnumerable(GetPropertyValue(group, "Items"));
                if (descriptions == null)
                {
                    continue;
                }

                foreach (var description in descriptions)
                {
                    var method = (GetStringProperty(description, "HttpMethod") ?? string.Empty).Trim().ToUpperInvariant();
                    var path = NormalizePath(GetStringProperty(description, "RelativePath"));
                    if (string.IsNullOrWhiteSpace(method) || string.IsNullOrWhiteSpace(path))
                    {
                        continue;
                    }

                    var actionDescriptor = GetPropertyValue(description, "ActionDescriptor");
                    endpoints.Add(new ServiceApiEndpointInfo
                    {
                        Method = method,
                        Path = path,
                        GroupName = groupName,
                        Controller = GetRouteValue(actionDescriptor, "controller"),
                        Action = GetRouteValue(actionDescriptor, "action"),
                        DisplayName = GetStringProperty(actionDescriptor, "DisplayName")
                    });
                }
            }

            return endpoints.Count > 0;
        }

        private static bool TryCollectFromEndpointDataSources(IServiceProvider serviceProvider, List<ServiceApiEndpointInfo> endpoints)
        {
            var dataSourceType = FindType(
                "Microsoft.AspNetCore.Routing.EndpointDataSource, Microsoft.AspNetCore.Routing.Abstractions",
                "Microsoft.AspNetCore.Routing.EndpointDataSource");
            if (dataSourceType == null)
            {
                return false;
            }

            var enumerableType = typeof(IEnumerable<>).MakeGenericType(dataSourceType);
            var dataSources = AsEnumerable(serviceProvider.GetService(enumerableType));
            if (dataSources == null)
            {
                return false;
            }

            foreach (var dataSource in dataSources)
            {
                var routeEndpoints = AsEnumerable(GetPropertyValue(dataSource, "Endpoints"));
                if (routeEndpoints == null)
                {
                    continue;
                }

                foreach (var endpoint in routeEndpoints)
                {
                    var methods = GetHttpMethods(endpoint);
                    var path = NormalizePath(GetEndpointRoutePattern(endpoint));
                    if (methods.Count == 0 || string.IsNullOrWhiteSpace(path))
                    {
                        continue;
                    }

                    foreach (var method in methods)
                    {
                        endpoints.Add(new ServiceApiEndpointInfo
                        {
                            Method = method,
                            Path = path,
                            DisplayName = GetStringProperty(endpoint, "DisplayName")
                        });
                    }
                }
            }

            return endpoints.Count > 0;
        }

        private static string NormalizePath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            var normalized = path.Trim();
            var queryIndex = normalized.IndexOf('?');
            if (queryIndex >= 0)
            {
                normalized = normalized.Substring(0, queryIndex);
            }
            if (!normalized.StartsWith("/", StringComparison.Ordinal))
            {
                normalized = "/" + normalized;
            }
            return normalized;
        }

        private static Type? FindType(string assemblyQualifiedName, string fullName)
        {
            var type = Type.GetType(assemblyQualifiedName, throwOnError: false);
            if (type != null)
            {
                return type;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(fullName, throwOnError: false);
                if (type != null)
                {
                    return type;
                }
            }
            return null;
        }

        private static object? GetPropertyValue(object? instance, string propertyName)
        {
            if (instance == null)
            {
                return null;
            }

            var property = instance.GetType().GetProperty(propertyName);
            return property?.GetValue(instance);
        }

        private static string? GetStringProperty(object? instance, string propertyName)
            => GetPropertyValue(instance, propertyName)?.ToString();

        private static IEnumerable? AsEnumerable(object? value) => value as IEnumerable;

        private static string? GetRouteValue(object? actionDescriptor, string key)
        {
            var routeValues = GetPropertyValue(actionDescriptor, "RouteValues");
            if (routeValues == null)
            {
                return null;
            }

            if (routeValues is IDictionary dictionary)
            {
                foreach (DictionaryEntry entry in dictionary)
                {
                    if (entry.Key is string name && string.Equals(name, key, StringComparison.OrdinalIgnoreCase))
                    {
                        return entry.Value?.ToString();
                    }
                }
            }

            var keys = AsEnumerable(GetPropertyValue(routeValues, "Keys"));
            if (keys == null)
            {
                return null;
            }

            var itemProperty = routeValues.GetType().GetProperty("Item");
            foreach (var itemKey in keys)
            {
                if (itemKey is string name && string.Equals(name, key, StringComparison.OrdinalIgnoreCase))
                {
                    return itemProperty?.GetValue(routeValues, new object[] { name })?.ToString();
                }
            }
            return null;
        }

        private static List<string> GetHttpMethods(object endpoint)
        {
            var methods = new List<string>();
            var metadata = AsEnumerable(GetPropertyValue(endpoint, "Metadata"));
            if (metadata == null)
            {
                return methods;
            }

            foreach (var item in metadata)
            {
                if (item == null || !string.Equals(item.GetType().FullName, "Microsoft.AspNetCore.Routing.HttpMethodMetadata", StringComparison.Ordinal))
                {
                    continue;
                }

                var httpMethods = AsEnumerable(GetPropertyValue(item, "HttpMethods"));
                if (httpMethods == null)
                {
                    continue;
                }

                foreach (var httpMethod in httpMethods)
                {
                    var value = httpMethod?.ToString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        methods.Add(value.Trim().ToUpperInvariant());
                    }
                }
            }

            return methods;
        }

        private static string? GetEndpointRoutePattern(object endpoint)
        {
            var pattern = GetPropertyValue(endpoint, "RoutePattern");
            return GetStringProperty(pattern, "RawText");
        }
    }
}
