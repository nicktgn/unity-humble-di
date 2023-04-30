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
    
    internal class CollectionWrapper : IList, IEnumerable<object> {

        FieldInfo field;
        Type type;
        IFaceFieldCategory category;
        object target;

        object collection;

        // ------------------------------------ //

        public Type ItemType => IsArray ? type.GetElementType() : type.GetGenericArguments()[0];
        
        public int Count {
            get {
                if (collection == null)
                    return 0;
                return IsArray ? AsArray.Length : AsIList.Count;
            }
        }

        public bool IsArray { get; private set; }
        public bool IsList { get; private set; }

        public object Object => collection;

        public Array AsArray => collection as Array;

        public IList AsIList => collection as IList;
        
        public bool AllNonNull {
            get {
                if (collection == null)
                    return false;
                return IsArray
                    ? AsArray.Cast<object>().All(o => o != null)
                    : AsIList.Cast<object>().All(o => o != null);
            }
        }

        public CollectionWrapper(FieldInfo field, object target) {
            this.field = field;
            type = field.FieldType;
            this.target = target;
            IsArray = type.IsArray;
            IsList = type.IsList();

            if (!IsArray && !IsList) {
                throw new ArgumentException("Provided field is not of the array or list type", nameof(field));
            }

            GetFromField();
        }

        public object GetFromField() {
            collection = field.GetValue(target);
            return collection;
        }

        public object Create(int capacity = 0) {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            collection = Activator.CreateInstance(type, capacity);
            field.SetValue(target, collection);
            return collection;
        }

        public int Add(object item) {
            if (collection == null) {
                Create();
            }
            if (IsArray) {
                return ArrayAdd(item);
            }
            return AsIList.Add(item);
        }

        public void RemoveAt(int index) {
            if (collection == null) {
                return;
            }
            if (IsArray) {
                ArrayRemoveAt(index);
            }
            else {
                AsIList.RemoveAt(index);
            }
        }

        public void Reorder(int index, int newIndex) {
            if (collection == null) {
                return;
            }
            if (index < 0 || index >= Count) {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            if (newIndex < 0 || newIndex >= Count) {
                throw new ArgumentOutOfRangeException(nameof(newIndex));
            }
            if (index == newIndex) {
                return;
            }
            if (IsArray) {
                ArrayReorder(index, newIndex);
            }
            else {
                ListReorder(index, newIndex);
            }
        }

        // ------------------------- //
        #region Rest of IList overrides (not used)

        public object this[int index] {
            get => IsArray ? AsArray.GetValue(index) : AsIList[index];
            set {
                if (IsArray) {
                    AsArray.SetValue(value, index);
                } else {
                    AsIList[index] = value;
                }
            }
        }
        
        public IEnumerator<object> GetEnumerator() {
            for (var i = 0; i < Count; i++) {
                yield return this[i];
            }
        }
        
        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public void CopyTo(Array array, int index) {
            throw new NotImplementedException();
        }

        public bool IsSynchronized { get; }

        public object SyncRoot { get; } = new object();

        public void Clear() {
            if (IsArray) {
                var arr = AsArray;
                for (var i = 0; i < Count; i++) {
                    arr.SetValue(null, i);
                }
            }
            else {
                AsIList.Clear();
            }
        }

        public bool Contains(object value) {
            if (IsList) {
                return AsIList.Contains(value);
            }
            var arr = AsArray;
            for (var i = 0; i < Count; i++) {
                if (arr.GetValue(i) == value)
                    return true;
            }
            return false;
        }

        public int IndexOf(object value) {
            if (IsList) {
                return AsIList.IndexOf(value);
            }
            var arr = AsArray;
            for (var i = 0; i < Count; i++) {
                if (arr.GetValue(i) == value)
                    return i;
            }
            return -1;
        }

        public void Insert(int index, object value) {
            if (IsArray) {
                ArrayInsert(index, value);
            } else {
                AsIList.Insert(index, value);
            }
        }

        public void Remove(object value) {
            if (IsArray) {
                ArrayRemove(value);
            } else {
                AsIList.Remove(value);
            }
        }

        public bool IsFixedSize => false;
        public bool IsReadOnly => false;

        #endregion

        // ------------------------- //
        #region Array Specific Implementations

        int ArrayAdd(object item) {
            var arr = AsArray;

            var index = Count;
            var newArr = (Array)Activator.CreateInstance(type, index + 1);
            for (var i = 0; i < index; i++) {
                newArr.SetValue(arr.GetValue(i), i);
            }
            collection = newArr;
            field.SetValue(target, collection);

            newArr.SetValue(item, index);
            return index;
        }

        void ArrayRemoveAt(int index) {
            if (index < 0 || index >= Count) {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            var arr = AsArray;
            var newArr = (Array)Activator.CreateInstance(type, Count - 1);
            var j = 0;
            for (var i = 0; i < Count; i++) {
                if (i == index)
                    continue;
                newArr.SetValue(arr.GetValue(i), j);
                j++;
            }
            collection = newArr;
            field.SetValue(target, collection);
        }

        void ArrayRemove(object value) {
            var arr = AsArray;
            var newArr = (Array)Activator.CreateInstance(type, Count - 1);
            var j = 0;
            for (var i = 0; i < Count; i++) {
                if (arr.GetValue(i) == value) {
                    continue;
                }
                newArr.SetValue(arr.GetValue(i), j);
                j++;
            }
            collection = newArr;
            field.SetValue(target, collection);
        }

        void ArrayReorder(int index, int newIndex) {
            var arr = AsArray;
            var item = arr.GetValue(index);

            if (index < newIndex) {
                for (var i = index; i < newIndex; i++) {
                    arr.SetValue(arr.GetValue(i + 1), i);
                }
            } else {
                for (var i = index; i >= index; i--) {
                    arr.SetValue(arr.GetValue(i - 1), i);
                }
            }
            arr.SetValue(item, newIndex);
        }

        void ArrayInsert(int index, object value) {
            if (index < 0 || index > Count) {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            var arr = AsArray;
            var newArr = (Array)Activator.CreateInstance(type, Count + 1);
            var j = 0;
            for (var i = 0; i < index; i++) {
                if (i == index) {
                    newArr.SetValue(value, i);
                    i--;
                }
                newArr.SetValue(arr.GetValue(i), j);
                j++;
            }
            collection = newArr;
            field.SetValue(target, collection);
        }

        #endregion

        // ------------------------- //
        #region List Specific Implementations

        void ListReorder(int index, int newIndex) {
            var list = AsIList;
            var item = list[index];
            list.RemoveAt(index);
            list.Insert(newIndex, item);
        }

        #endregion
    }

}
