// MIT License
//
// Copyright (c) 2022 Nick Tsygankov
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LobstersUnited.HumbleDI {

    internal static class Utils {

        public static readonly BindingFlags ALL_INSTANCE_FIELDS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        // ------------------------------------- //
        #region Type Extensions
        
        public static IEnumerable<FieldInfo> GetInterfaceFields(this Type type) {
            var fields = type.GetFields(ALL_INSTANCE_FIELDS);
            foreach (var field in fields) {
                if (!field.FieldType.IsInterface)
                    continue;

                yield return field;
            }
        }

        public static FieldInfo GetInterfaceFieldOfType(this Type type, Type fieldType) {
            var fields = type.GetFields(ALL_INSTANCE_FIELDS);
            return fields.FirstOrDefault(field => field.FieldType == fieldType);
        }
        
        public static string GetNameWithGenerics(this Type type) {
            var name = type.IsGenericType ? type.Name.Split('`')[0] : type.Name;
            if (type.IsGenericType) {
                var paramStr = type.GetGenericArguments().MapToString(t => t.Name);
                name += $"<{paramStr}>";
            }
            return name;
        }

        public static IEnumerable<ProviderAttribute> GetProviderAttributes(this Type type) {
            return type.GetCustomAttributes<ProviderAttribute>();
        }

        public static FieldType GetFieldType(this Type type) {
            if (type.IsArray) {
                return FieldType.ARRAY;
            }
            if (typeof(List<>).IsAssignableFrom(type)) {
                return FieldType.LIST;
            }
            if (typeof(IEnumerable).IsAssignableFrom(type)) {
                return FieldType.UNSUPPORTED;
            }
            return FieldType.SINGULAR;
        }
        
        #endregion
        
        public static Array CreateArray(Type t, int length = 0) {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));
            return (Array) Activator.CreateInstance(t.MakeArrayType(), length);
        }

        public static System.Collections.IEnumerable CreateList(Type t, int length = 0) {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));
            return (Array) Activator.CreateInstance(t, length);
        }

        public static string MapToString<T>(this IEnumerable<T> list, Func<T, string> map, string separator = ", ") {
            return string.Join(separator, list.Select(map).ToArray());
        }
    }
}
