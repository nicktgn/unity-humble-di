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

using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;


namespace LobstersUnited.HumbleDI.Editor {
    
    internal class ListFieldDrawer {

        ObjectManager objectManager;

        ReorderableList gui;
        CollectionWrapper list;

        public ListFieldDrawer(FieldInfo field, object target, ObjectManager objectManager) {
            list = new CollectionWrapper(field, target);

            gui = new ReorderableList(list, null, true, false, true, true) {
                drawElementCallback = DrawElement,
                onAddCallback = OnAdd,
                onRemoveCallback = OnRemove,
                onReorderCallbackWithDetails = OnReorder,
                onCanRemoveCallback = CanRemove,
                multiSelect = false,
            };

            this.objectManager = objectManager;
        }

        public void Dispose() {
            objectManager = null;
            gui = null;
            list = null;
        }

        public float GetHeight() {
            return gui.GetHeight();
        }

        public void Draw(Rect rect) {
            gui.DoList(rect);
        }

        void DrawElement(Rect rect, int index, bool active, bool focused) {
            var id = GUIUtility.GetControlID(FocusType.Keyboard, rect);
            var itemType = list.ItemType;

            // adjust line height
            rect.y -= (rect.height - DrawerUtils.lineHeight - DrawerUtils.verticalSpacing) / 2;
            rect.height = DrawerUtils.lineHeight + DrawerUtils.verticalSpacing;

            var obj = list[index] as Object;

            // get measurements
            CalculateLabelWithPrefix(rect, out var labelPos, out var fieldPos);
            DrawerUtils.GetPickerRect(fieldPos, out var pickerRect, out var iconRect);
            
            // handle focus
            if (focused) {
                GUIUtility.keyboardControl = id;
            }
            DrawerUtils.ProcessFocus(rect, id, () => {
                gui.Select(index);
            });
            
            // handle DnD
            DrawerUtils.ProcessDragAndDrop(id, fieldPos, !objectManager.IsPersistent, 
                objToValidate => Utils.FindComponentOrSO(itemType, objToValidate),
                drop => {
                    SetObjectAsListItem(drop, index);
                    obj = drop;
                }
            );
            
            // Draw index label
            var label = new GUIContent(index.ToString());
            var labelStyle = new GUIStyle(EditorStyles.boldLabel);
            var isHovered = DrawerUtils.DetectHover(labelPos);
            if (DrawerUtils.IsRepaint) {
                labelStyle.Draw(labelPos, label, id, false, isHovered);
            }

            // Draw field
            isHovered = DrawerUtils.DetectHover(fieldPos);
            var content = objectManager.GetContentFromObject(obj, itemType);
            var fieldStyle = new GUIStyle(EditorStyles.objectField);
            if (DrawerUtils.IsRepaint) {
                fieldStyle.Draw(fieldPos, content, id, false, isHovered);
            }
            
            // Draw picker
            var pickerStyle = new GUIStyle(EditorStyles.helpBox);
            if (DrawerUtils.DetectHover(pickerRect)) {
                pickerStyle.normal.background = Texture2D.linearGrayTexture;
            }
            GUI.Box(pickerRect, "", pickerStyle);
            GUI.DrawTexture(iconRect, DrawerUtils.iconPick);
            
            // process clicks
            // reduce trigger area of the field because covered by the picker icon
            fieldPos.width -= DrawerUtils.pickerWidth;
            var fieldClick = DrawerUtils.ProcessMouseDown(fieldPos);
            if (fieldClick != null && obj != null) {
                // Selection.objects = new[] { obj };
                EditorGUIUtility.PingObject(obj);
            }

            var pickerClick = DrawerUtils.ProcessMouseDown(pickerRect);
            if (pickerClick != null) {
                objectManager.OpenObjectPicker(itemType, pickedObject => {
                    SetObjectAsListItem(pickedObject, index);
                    GUI.changed = true;
                });
            }
        }

        void SetObjectAsListItem(Object obj, int index) {
            objectManager.RecordUndoHierarchy();
            list[index] = obj;
        }

        void CalculateLabelWithPrefix(Rect totalPosition, out Rect labelPosition, out Rect fieldPosition) {
            labelPosition = new Rect(totalPosition.x, totalPosition.y + 1, EditorGUIUtility.labelWidth, DrawerUtils.lineHeight);
            fieldPosition = new Rect(
                totalPosition.x + EditorGUIUtility.labelWidth + DrawerUtils.prefixPaddingRight, 
                totalPosition.y + 4, 
                totalPosition.width - EditorGUIUtility.labelWidth - DrawerUtils.prefixPaddingRight, 
                DrawerUtils.lineHeight
            );
            var unindent = DrawerUtils.singleIndentSpacing * 3 + 5;
            fieldPosition.x -= unindent;
            fieldPosition.width += unindent;
            labelPosition.width -= unindent;
        }

        void OnAdd(ReorderableList reorderableList) {
            objectManager.RecordUndoHierarchy();
            list.Add(null);
        }

        void OnRemove(ReorderableList reorderableList) {
            objectManager.RecordUndoHierarchy();
            var selected = reorderableList.selectedIndices;
            var idx = selected.Count > 0 ? selected.FirstOrDefault() : list.Count - 1;
            list.RemoveAt(idx);
        }

        void OnReorder(ReorderableList reorderableList, int index, int newIndex) {
            objectManager.RecordUndoHierarchy();
            list.Reorder(index, newIndex);
        }

        bool CanRemove(ReorderableList reorderableList) {
            return list.Count > 0;
        }
    }
}
