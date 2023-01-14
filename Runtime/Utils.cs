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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;


namespace LobstersUnited.HumbleDI {

    internal static class Utils {

        static readonly Type LIST_TYPE = typeof(List<>);
        static readonly Type IENUMERABLE_TYPE = typeof(IEnumerable<>);

        
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

        public static bool IsList(this Type type) {
            return type.IsGenericType && type.GetGenericTypeDefinition() == LIST_TYPE;
        }

        public static bool IsIEnumerable(this Type type) {
            return type.IsGenericType && type.GetGenericTypeDefinition() == IENUMERABLE_TYPE;
        }

        public static string GetNameWithGenerics(this Type type) {
            var name = type.IsGenericType ? type.Name.Split('`')[0] : type.Name;
            if (type.IsGenericType) {
                var paramStr = type.GetGenericArguments().MapToString(t => t.Name);
                name += $"<{paramStr}>";
            }
            return name;
        }

        #endregion
        
        
        public static string MapToString<T>(this IEnumerable<T> list, Func<T, string> map, string separator = ", ") {
            return string.Join(separator, list.Select(map).ToArray());
        }
        
        // ------------------------------------- //
        #region Unity Object Utils

        public static UnityEngine.Object FindComponentOrSO(Type matchingType, UnityEngine.Object obj) {
            var gameObject = obj as GameObject;
            if (gameObject) {
                var cmp = gameObject.GetComponents<Component>().Where(c => {
                    // TODO: consider using field.FieldType.IsAssignableFrom(c.GetType())
                    var iList = c.GetType().GetInterfaces();
                    return iList.Any(i => i == matchingType);
                }).FirstOrDefault();
                return cmp;
            }
            
            var so = obj as ScriptableObject;
            if (so) {
                // TODO: consider using field.FieldType.IsAssignableFrom(so.GetType())
                var hasRequiredIFace = so.GetType().GetInterfaces().Any(i => i == matchingType);
                if (hasRequiredIFace) return so;
            }
            return null;
        }

        #endregion
    }
}
