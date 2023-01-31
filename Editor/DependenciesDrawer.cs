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
using UnityEngine;
using UnityEditor;


namespace LobstersUnited.HumbleDI.Editor {

    [CustomPropertyDrawer(typeof(InterfaceDependencies))]
    internal class DependenciesDrawer : PropertyDrawer, IDisposable {
        
        float totalHeight;

        bool isInit;

        DependencyManager dependencies;
        
        ObjectManager objectManager;
        Dictionary<int, ListFieldDrawer> listDrawers = new Dictionary<int, ListFieldDrawer>();

        SerializedProperty isFoldout;
        static readonly string isFoldoutPropName = "isFoldout";
        InterfaceDependencies iDeps;
        

        // Draw the property inside the given rect
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            isFoldout = property.FindPropertyRelative(isFoldoutPropName);

            property.serializedObject.Update();

            Enable(property, fieldInfo);

            DrawInterfaceDependenciesGUI(position);
            
            property.serializedObject.ApplyModifiedProperties();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return totalHeight;
        }

        public void DrawInterfaceDependenciesGUI (Rect position) {
            DrawDependenciesSectionGUI(position);
        }
        
        float CalculateDependencySectionHeight() {
            var lineHeight = DrawerUtils.lineHeight;
            var verticalSpacing = DrawerUtils.verticalSpacing;
            var lineWithSpacing = lineHeight + verticalSpacing;
            var headerHeight = lineHeight + verticalSpacing * 2 + 2;
            var singularFieldsHeight = 
                (dependencies.InterfaceDependencyCount - dependencies.InterfaceListDependencyCount) * lineWithSpacing;
            
            // calculate height of all interface list drawers
            var listFieldsHeight = 0.0f;
            foreach (var kv in listDrawers) {
                // foldout header for each list field
                listFieldsHeight += lineWithSpacing;
                if (iDeps.IsListFoldout(kv.Key)) {
                    listFieldsHeight += kv.Value.GetHeight();
                    // listFieldsHeight += verticalSpacing;
                }
            }
            
            // calculate height of all normal property drawers that are added to "Dependencies" section
            var normalPropsBlockHeight = 0.0f;
            foreach (var normalDep in dependencies.NormalFields) {
                var serializedProp = normalDep.SerializedProperty;
                normalPropsBlockHeight += EditorGUI.GetPropertyHeight(serializedProp, true) + verticalSpacing;
            }

            return headerHeight + singularFieldsHeight + listFieldsHeight + verticalSpacing + normalPropsBlockHeight;
        }
        
        void DrawDependenciesSectionGUI(Rect pos) {
            var startY = pos.y; // pos.yMin;
            
            pos.y += DrawerUtils.verticalSpacing;

            var foldout = iDeps.IsFoldout;
            
            // draw box background
            var sectionHeight = CalculateDependencySectionHeight();
            var boxPos = pos;
            boxPos.height = foldout ? sectionHeight : DrawerUtils.lineHeight;
            DrawerUtils.DrawBox(boxPos);
            
            // draw foldout
            var foldoutPos = pos;
            foldoutPos.height = DrawerUtils.lineHeight;
            // foldout = DrawerUtils.DrawFoldout(foldoutPos, foldout, "Dependencies");
            foldout = DrawerUtils.DrawFoldoutCustomLabel(foldoutPos, foldout, DrawDependencyLabelWithShortStats);
            pos.y += DrawerUtils.lineHeight;
            
            if (foldout) {
                pos.y += DrawDependenciesSectionContentGUI(pos);
            }
            SaveFoldoutState(foldout);

            // EditorGUI.indentLevel = initIndentLevel;

            // update full height of the section
            totalHeight = pos.y - startY;
        }
        
        void DrawDependencyLabelWithShortStats(Rect pos, bool hasFocus) {
            var label = new GUIContent("Dependencies");
            
            var labelStyle = new GUIStyle(EditorStyles.boldLabel);
            if (hasFocus) {
                labelStyle.normal.textColor = EditorStyles.foldout.active.textColor;
            }
            EditorGUI.LabelField(pos, label, labelStyle);
            
            DependencyIndicatorDrawer.DrawShortStats(pos, dependencies.UpdateStats());
        }

        void SaveFoldoutState(bool foldout) {
            if (foldout == iDeps.IsFoldout)
                return;
            // TODO: consider if this is necessary
            // EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            iDeps.IsFoldout = foldout;
        }

        float DrawDependenciesSectionContentGUI(Rect pos) {
            var startY = pos.y;
            var verticalSpacing = DrawerUtils.verticalSpacing;
            
            pos.y += verticalSpacing;
            pos.y += DrawerUtils.DrawSeparatorLine(pos);
            pos.y += verticalSpacing;
            
            EditorGUI.indentLevel += 1;

            var index = 0;
            foreach (var dep in dependencies.Interfaces) {
                var cat = dep.InterfaceCategory;
                if (cat == IFaceFieldCategory.SINGULAR) {
                    pos.y += DrawInterfacePropertyGUI(pos, index, dep);
                } else {
                    pos.y += DrawInterfaceListGUI(pos, index, dep);
                }
                pos.y += verticalSpacing;
                index++;
            }
            
            // Draw normal properties that were moved under "Dependencies" section
            pos.y += DrawNormalDependencyPropsGUI(pos);

            EditorGUI.indentLevel -= 1;
            
            return pos.y - startY;
        }

        float DrawInterfaceListGUI(Rect pos, int index, DependencyInfo dep) {
            var listField = dep.Field;
            
            // var id = GUIUtility.GetControlID(FocusType.Keyboard, pos);
            var startY = pos.y;

            var listMgr = listDrawers.GetValueOrDefault(index);
            if (listMgr == null) {
                return 0;
            }

            var listFoldout = iDeps.IsListFoldout(index);

            // Draw stat indicator
            DependencyIndicatorDrawer.DrawDependencyIndicator(pos, dependencies, dep);
            
            // draw foldout header - labeled with field name
            var label = ObjectNames.NicifyVariableName(listField.Name);
            var labelPos = pos;
            labelPos.height = DrawerUtils.lineHeight;
            labelPos.x += 12;
            labelPos.width -= 12;
            // listFoldout = DrawerUtils.DrawFoldout(labelPos, listFoldout, label);
            listFoldout = EditorGUI.Foldout(labelPos, listFoldout, new GUIContent(label), true);
            pos.y += DrawerUtils.lineHeight;
            pos.y += DrawerUtils.verticalSpacing;
            
            if (listFoldout) {
                EditorGUI.indentLevel++;
                
                // account for indent
                var indentWidth = DrawerUtils.IndentWidth;
                pos.x += indentWidth;
                pos.width -= indentWidth;
                
                listMgr.Draw(pos);
                pos.y += listMgr.GetHeight();
                
                EditorGUI.indentLevel--;
            }
            iDeps.SetListFoldout(index, listFoldout);

            return pos.y - startY;
        }

        float DrawInterfacePropertyGUI(Rect pos, int index, DependencyInfo dep) {
            var field = dep.Field;
            var obj = objectManager.GetMappedObjectForField(field);

            var id = GUIUtility.GetControlID(FocusType.Keyboard, pos);
            var startY = pos.y;

            var lineHeight = DrawerUtils.lineHeight;
            var pickerWidth = DrawerUtils.pickerWidth;

            pos.height = lineHeight;
    
            // process focus
            DrawerUtils.ProcessFocus(pos, id);
            
            // Draw stat indicator
            DependencyIndicatorDrawer.DrawDependencyIndicator(pos, dependencies, dep);
            
            // Draw label
            var label = ObjectNames.NicifyVariableName(field.Name);
            var labelPos = pos;
            var labelStyle = new GUIStyle(EditorStyles.label);
            
            if (DrawerUtils.HasKBFocus(id)) {
                labelStyle.normal = EditorStyles.label.focused;
            }
            var fieldPos = EditorGUI.PrefixLabel(labelPos, id, new GUIContent(label), labelStyle);
            labelPos.width = EditorGUIUtility.labelWidth;

            DrawerUtils.ProcessDragAndDrop(id, fieldPos, !objectManager.IsPersistent, 
                objToValidate => Utils.FindComponentOrSO(field.FieldType, objToValidate),
                drop => {
                    objectManager.SetObjectToField(field, drop);
                    obj = drop;
                }
            );

            // Draw field
            var isHovered = DrawerUtils.DetectHover(fieldPos);
            var content = objectManager.GetContentFromObject(obj, field.FieldType);
            var fieldStyle = new GUIStyle(EditorStyles.objectField);
            if (DrawerUtils.IsRepaint) {
                fieldStyle.Draw(fieldPos, content, id, DrawerUtils.HasDnD(id), isHovered);
            }
            
            // Draw picker
            DrawerUtils.GetPickerRect(fieldPos, out var pickerRect, out var iconRect);
            var pickerStyle = new GUIStyle(EditorStyles.helpBox);
            if (DrawerUtils.DetectHover(pickerRect)) {
                pickerStyle.normal.background = Texture2D.linearGrayTexture;
            }
            GUI.Box(pickerRect, "", pickerStyle);
            GUI.DrawTexture(iconRect, DrawerUtils.iconPick);

            // reduce trigger area of the field because covered by the picker icon
            fieldPos.width -= pickerWidth;
            
            // process clickss
            var fieldClick = DrawerUtils.ProcessMouseDown(fieldPos);
            if (fieldClick != null) {
                if (obj != null) {
                    // Selection.objects = new[] { obj };
                    EditorGUIUtility.PingObject(obj);
                }
            }
            var pickerClick = DrawerUtils.ProcessMouseDown(pickerRect);
            if (pickerClick != null) {
                objectManager.OpenObjectPicker(field.FieldType, pickedObject => {
                    objectManager.SetObjectToField(field, pickedObject);
                    GUI.changed = true;
                });
            }

            pos.y += lineHeight;
            return pos.y - startY;
        }
        
        public float DrawNormalDependencyPropsGUI(Rect position) {
            var startY = position.y;
            position.height = DrawerUtils.lineHeight;
            
            foreach (var dep in dependencies.NormalFields) {
                var serializedProp = dep.SerializedProperty;
                
                // Draw dependency indicator
                DependencyIndicatorDrawer.DrawDependencyIndicator(position, dependencies, dep);
                
                // Draw property
                var propPos = position;
                // propPos.x += DependencyIndicatorDrawer.ICON_WIDTH_WITH_SPACING; 
                EditorGUI.PropertyField(propPos, serializedProp, true);
                position.y += EditorGUI.GetPropertyHeight(serializedProp, true) + DrawerUtils.verticalSpacing;
            }

            return position.y - startY;
        }

        // --------------------------------- //
        #region SETUP

        public void Enable(SerializedProperty property, FieldInfo iDepsField) {
            if (isInit) return;

            objectManager = new ObjectManager(property);
            iDeps = objectManager.BindInterfaceDependencies(iDepsField, property.propertyPath);

            dependencies = new DependencyManager(objectManager);
            
            // create list field drawers
            var count = 0;
            foreach (var dep in dependencies.Interfaces) {
                if (dep.CollectionWrapper == null)
                    continue;
                var listMgr = new ListFieldDrawer(dep.CollectionWrapper, objectManager);
                listDrawers.Add(count, listMgr);
                count++;
            }
            
            isInit = true;
        }

        public void Disable() {
            isInit = false;
            objectManager.Cleanup();
            objectManager = null;
            
            foreach (var drawer in listDrawers.Values) {
                drawer.Dispose();
            }
            listDrawers.Clear();

            iDeps = null;
        }

        #endregion
        
        // --------------------------------- //
        #region CLEANUP

        void ReleaseUnmanagedResources() {
            Disable();
        }
        void Dispose(bool disposing) {
            ReleaseUnmanagedResources();
            if (disposing) {
                isFoldout?.Dispose();
            }
        }
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~DependenciesDrawer() {
            Dispose(false);
        }
        
        #endregion
    }
}
