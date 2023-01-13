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
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;


namespace LobstersUnited.HumbleDI.Editor {
    
    internal class ObjectManager {

        /// <summary>
        /// Unity Object target (target of the Inspector)
        /// </summary>
        public Object target;
       
        /// <summary>
        /// Actual c# object which contains InterfaceDependencies field. Can be nested inside of `target` or `target` itself
        ///  depending on the `SerializedProperty.propertyPath` 
        /// </summary>
        public object actualTarget;

        /// <summary>
        /// Caching the assigned values of the interface fields to avoid serialization / deserialization timing bugs
        /// </summary>
        Dictionary<FieldInfo, Object> fieldCache;

        public ObjectManager(Object target, string iDepsFieldPath) {
            this.target = target;
            actualTarget = InterfaceDependencies.GetTargetObjectRelativeToIDeps(target, iDepsFieldPath);

            fieldCache = new Dictionary<FieldInfo, Object>();
        }

        ~ObjectManager() {
            Cleanup();
        }

        public void Cleanup() {
            target = null;
            actualTarget = null;
            fieldCache.Clear();
        }

        public InterfaceDependencies BindInterfaceDependencies(FieldInfo iDepsField, string iDepsFieldPath) {
            var obj = iDepsField.GetValue(actualTarget);
            switch (obj) {
                case null:
                    // Debug.Log("CREATE new Instance of InterfaceDependencies");
                    obj = new InterfaceDependencies(target, iDepsFieldPath);
                    iDepsField.SetValue(actualTarget, obj);
                    break;
                case InterfaceDependencies iDeps:
                    iDeps.SetTarget(target, iDepsFieldPath);
                    break;
            }
            return obj as InterfaceDependencies;
        }

        public void OpenObjectPicker(Type type, Action<Object> pickCallback) {
            InterfaceSearchProvider.OpenInterfacePicker(type, pickedObject => {
                var componentOrSO = Utils.FindComponentOrSO(type, pickedObject);
                pickCallback(componentOrSO);
            });
        }

        public void RecordUndo() {
            Undo.RecordObject(target, "Set interface field ref");
        }

        public void RecordUndoHierarchy() {
            Undo.RegisterFullObjectHierarchyUndo(target, "Set interface field list item");
        }

        public void SetObjectToField(FieldInfo field, Object pickedObj) {
            RecordUndo();
            field.SetValue(actualTarget, pickedObj);
            
            fieldCache[field] = pickedObj;
            
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        public Object GetMappedObjectForField(FieldInfo field) {
            var obj = fieldCache.GetValueOrDefault(field);
            if (obj == null) {
                obj = field.GetValue(actualTarget) as Object;
                fieldCache[field] = obj;
            }
            return obj;
        }
        
        public GUIContent GetContentFromObject(Object obj, Type type) {
            var typeName = type.GetNameWithGenerics();
            var text = obj == null 
                ? $"None ({typeName})" 
                : $"{obj.name} ({obj.GetType().Name}) ({typeName})";
            var img = obj == null
                ? null
                : EditorGUIUtility.GetIconForObject(obj);
            return new GUIContent(text, img);
        }

        
    }
}
