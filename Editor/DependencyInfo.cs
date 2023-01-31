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

using System.Reflection;
using UnityEditor;

namespace LobstersUnited.HumbleDI.Editor {
    internal class DependencyInfo {

        public FieldInfo Field { get; }

        public bool IsInterface { get; } = false;
        public IFaceFieldCategory InterfaceCategory { get; } = IFaceFieldCategory.UNSUPPORTED;
        public SerializedProperty SerializedProperty { get; } = null;
        public CollectionWrapper CollectionWrapper { get; } = null;

        public bool IsExternal { get; } = false;
        public bool IsOptional { get; } = false;

        public DependencyInfo(ObjectManager objectManager, FieldInfo field, bool isIFace, DependencyAttribute attribute = null) {
            Field = field;
            IsInterface = isIFace;
            if (isIFace) {
                InterfaceCategory = InterfaceDependencies.GetIFaceFieldTypeCategory(field.FieldType);
                if (InterfaceCategory is IFaceFieldCategory.LIST or IFaceFieldCategory.ARRAY) {
                    CollectionWrapper = new CollectionWrapper(field, objectManager.ActualTarget);
                }
            } else {
                SerializedProperty = objectManager.GetSiblingSerializedProperty(field.Name);
            }
            
            if (attribute != null) {
                IsExternal = attribute.External;
                IsOptional = attribute.Optional;    
            }
        }
    }
}
