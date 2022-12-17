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
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace LobstersUnited.HumbleDI {
    
    public class ProviderAttribute : Attribute {

        public string path = null;

        Type type;
        int maxLookupDepth;
        
        public ProviderAttribute(Type type, int maxLookupDepth = 1) {
            this.type = type;
            this.maxLookupDepth = maxLookupDepth;
        }

        public string Find(Type providerType) {
            // zero level
            if (type.IsAssignableFrom(providerType)) {
                path = string.Empty;
            } else {
                var field = providerType.GetFields(Utils.ALL_INSTANCE_FIELDS).FirstOrDefault(f => type.IsAssignableFrom(f.FieldType));
                if (field != null) {
                    path = field.Name;
                }

                // TODO:
                // var pathBuilder = new StringBuilder();
                // var curLevel = 1;
                // var stack = new Stack<Type>();
                // stack.Push(providerType);
                //
                // // BFS
                // while (stack.Count > 0 || curLevel <= maxLookupDepth) {
                //     var curLevelType = stack.Pop();
                //     var fields = curLevelType.GetFields(Utils.ALL_INSTANCE_FIELDS);
                //
                //     var matchingField = fields.FirstOrDefault(f => type.IsAssignableFrom(f.FieldType));
                //     if (matchingField != null) {
                //         pathBuilder.
                //         
                //     } else {
                //         for ()
                //     }
                // }

            }

            if (path == null) {
                // TODO: expand on this error message to provide more useful hints on what to do about it.
                throw new NotAProviderException($"Type {providerType.Name} is not a provider of {type.Name}");    
            }
            return path;
        }
    }

    public class NotAProviderException : Exception {
        
        public NotAProviderException() : base() { }
        
        public NotAProviderException(string msg) : base(msg) { }
    }

}
