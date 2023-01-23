using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace LobstersUnited.HumbleDI.Editor {

    class PropertyDependenciesDrawer {

        static readonly Type ATTR_TYPE = typeof(DependencyAttribute);

        List<SerializedProperty> serializedProperties;

        public PropertyDependenciesDrawer(ObjectManager objectManager, IEnumerable<FieldInfo> iFaceFields = null) {
            serializedProperties = new List<SerializedProperty>();

            var fields = EnumerateNormalDependencies(objectManager, iFaceFields);
            // find SerializedProperty for each
            foreach (var field in fields)
            {
                var serializedProp = objectManager.GetSiblingSerializedProperty(field.Name);
                if (serializedProp != null)
                {
                    serializedProperties.Add(serializedProp);
                }
            }
        }

        public int Count => serializedProperties.Count;

        public float GetHeight() {
            var height = 0.0f;

            foreach (var prop in serializedProperties) {
                height += EditorGUI.GetPropertyHeight(prop, true);
                height += DrawerUtils.verticalSpacing;
            }

            return height;
        }

        public float Draw(Rect position) {
            var startY = position.y;
            position.height = DrawerUtils.lineHeight;
            
            foreach (var prop in serializedProperties) {
                EditorGUI.PropertyField(position, prop, true);
                position.y += EditorGUI.GetPropertyHeight(prop, true) + DrawerUtils.verticalSpacing;
            }

            return position.y - startY;
        }
        
        IEnumerable<FieldInfo> EnumerateNormalDependencies(ObjectManager objectManager, IEnumerable<FieldInfo> iFaceFields = null) {
            // enumerate fields that have custom attribute which adds them to Dependencies section
            var fields = objectManager.ActualTarget.GetType().GetFieldsWithAttribute(ATTR_TYPE);
            // filter out interface fields
            if (iFaceFields != null) {
                var iFaceSet = new HashSet<FieldInfo>(iFaceFields);
                fields = fields.Where(f => !iFaceSet.Contains(f));
            }
            return fields;
        }
    }
    
}
