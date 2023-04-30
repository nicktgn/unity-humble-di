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


namespace LobstersUnited.HumbleDI.Editor {
    
    internal class DependencyManager {

        static readonly Type ATTR_TYPE = typeof(DependencyAttribute);
        
        ObjectManager objectManager;
        
        public DependencyManager(ObjectManager objectManager) {
            this.objectManager = objectManager;
            
            Enumerate();
        }

        ~DependencyManager() {
            objectManager = null;
        }

        public IList<DependencyInfo> Dependencies { get; private set; }

        public int InterfaceDependencyCount { get; private set; } = 0;

        public int InterfaceListDependencyCount { get; private set; } = 0;

        public int NormalDependencyCount { get; private set; } = 0;

        public IEnumerable<DependencyInfo> Interfaces => Dependencies.Where(dep => dep.IsInterface);

        public IEnumerable<DependencyInfo> NormalFields => Dependencies.Where(dep => !dep.IsInterface);

        void Enumerate() {
            var type = objectManager.ActualTarget.GetType();
            var iFaceFields = InterfaceDependencies.GetCompatibleFields(type);
            var markedDependencies = type.GetFieldsWithAttribute(ATTR_TYPE).ToHashSet();
            
            // take union
            Dependencies = new List<DependencyInfo>();
            foreach (var field in iFaceFields) {
                DependencyAttribute attribute = null;
                if (markedDependencies.Contains(field)) {
                    attribute = field.GetCustomAttribute<DependencyAttribute>();
                    markedDependencies.Remove(field);
                }
                var dep = new DependencyInfo(objectManager, field, true, attribute);
                Dependencies.Add(dep);
                InterfaceDependencyCount++;
                if (dep.CollectionWrapper != null) {
                    InterfaceListDependencyCount++;
                }
            }
            // remaining fields that are not interfaces
            foreach (var field in markedDependencies) {
                var attribute = field.GetCustomAttribute<DependencyAttribute>();
                var dep = new DependencyInfo(objectManager, field, false, attribute);
                Dependencies.Add(dep);
                NormalDependencyCount++;
            }
        }

        public DependencyStats UpdateStats() {
            var stats = new DependencyStats();
            foreach (var dep in Dependencies) {
                var isResolved = IsDependencyResolved(dep, true);
                if (isResolved) stats.Resolved += 1;
                else stats.Unresolved += 1;

                if (dep.IsExternal) stats.External += 1;
                if (dep.IsOptional) stats.Optional += 1;
            }
            return stats;
        }

        public bool IsDependencyResolved(DependencyInfo info, bool allowSkipExternal = false) {
            // if we're in prefab preview and we don't want to count unresolved externals towards total unresolved
            if (allowSkipExternal && objectManager.PrefabState == PrefabState.PREFAB && info.IsExternal)
                return true;

            if (info.IsInterface) {
                if (info.InterfaceCategory == IFaceFieldCategory.LIST || info.InterfaceCategory == IFaceFieldCategory.ARRAY) {
                    return info.CollectionWrapper.AllNonNull;
                }
                return objectManager.GetMappedObjectForField(info.Field) != null;
            }
            // if normal field
            return info.SerializedProperty.objectReferenceValue != null;
        } 
    }
}
