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
using PlasticGui;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using Object = UnityEngine.Object;


namespace LobstersUnited.HumbleDI.Editor {

    internal class Extras {
        public ReferenceSource source;

        public Extras(ReferenceSource source) {
            this.source = source;
        }
    }
    
    internal class BuiltInProviderSearch {

        readonly SearchProvider provider;
        
        public BuiltInProviderSearch(SearchProvider provider) {
            this.provider = provider;
        }

        public ISearchList Search(string filter, string searchPhrase = null) {
            var context = SearchService.CreateContext(
                new[] { provider },
                searchText: $"{filter} {searchPhrase}",
                flags: SearchFlags.Sorted | SearchFlags.OpenPicker
            );
            return SearchService.Request(context);
        }
    }

    internal static class Commons {
        static readonly bool DEBUG = false;
        
        public static readonly string SCENE_PROVIDER = "scene";
        public static readonly string ASSET_PROVIDER = "asset";

        public static readonly string SCENE_SEARCH_FILTER = "t:GameObject";
        public static readonly string ASSET_SEARCH_FILTER = "t:ScriptableObject .asset";
        
        public static SearchItem CreateItemFrom(SearchItem item, SearchProvider provider, SearchContext context, ReferenceSource source) {
            return provider.CreateItem(
                context, 
                item.id,
                item.GetLabel(item.context, true), 
                item.GetDescription(item.context, true),
                item.GetThumbnail(item.context, true),
                new Extras(source)
            );
        }
        
        public static bool IsFocusedWindowTypeName(string focusWindowName) {
            return EditorWindow.focusedWindow != null && EditorWindow.focusedWindow.GetType().ToString().EndsWith("." + focusWindowName);
        }

        public static void Log(string log) {
            if (!DEBUG)
                return;
            Debug.Log(log);
        }
    }


    internal abstract class AbsSearchProviderBuilder {

        public string id;
        public string name;
        public string filterId;
        public int priority;
        public bool showDetails = true;
        public ShowDetailsOptions showDetailsOptions = ShowDetailsOptions.Preview; // ShowDetailsOptions.Inspector | ShowDetailsOptions.Actions,
        public bool isExplicitProvider = false;

        public Func<SearchItem, Type, Object> toObject;
        
        public AbsSearchProviderBuilder(string id, string name, string filterId, int priority = 99999) {
            this.id = id;
            this.name = name;
            this.filterId = filterId;
            this.priority = priority;
        }
        
        public SearchProvider Build() {
            return new SearchProvider(id, name) {
                filterId = filterId,
                priority = priority,
                showDetails = showDetails,
                showDetailsOptions = showDetailsOptions,
                isExplicitProvider = isExplicitProvider,
                    
                isEnabledForContextualSearch = IsEnabledForContextualSearch,

                onEnable = OnEnable,
                onDisable = OnDisable,
                
                toObject = toObject,

                fetchItems = (context, items, provider) => FetchItems(context, provider),
            };
        }

        protected abstract IEnumerable<SearchItem> FetchItems(SearchContext context, SearchProvider provider);

        protected virtual Object ToObject(SearchItem item, Type type) {
            return null;
        }

        protected virtual void OnEnable() { }

        protected virtual void OnDisable() { }

        protected virtual Texture2D FetchThumbnail(SearchItem item, SearchContext context) {
            return null;
        }
        
        protected virtual Texture2D FetchPreview(SearchItem item, SearchContext context, Vector2 size, FetchPreviewOptions options) {
            return null;
        }

        protected virtual string FetchLabel(SearchItem item, SearchContext context) {
            return "";
        }
        
        protected virtual string FetchDescription(SearchItem item, SearchContext context) {
            return "";
        }

        protected virtual void TrackSelection(SearchItem item, SearchContext context) { }
        
        protected virtual void StartDrag(SearchItem item, SearchContext context) { }

        protected virtual bool IsEnabledForContextualSearch() {
            return Commons.IsFocusedWindowTypeName("SceneView") || Commons.IsFocusedWindowTypeName("SceneHierarchyWindow");
        }
        
    }

    internal static class InterfaceSearchProvider {
        
        static readonly string id = "humble_interface_search";
        static readonly string name = "Humble Interface Search";
        static readonly int priority = 99999;
        static readonly string filterId = "iface:";
    
        static SearchProvider sceneProvider;
        static SearchProvider assetProvider;

        static bool _allowSceneObjects = true;
        static Type _pickType; 
        static Action<Object> _pickCallback;

        public static void OpenInterfaceSearch(Type type = null, Action<Object> pickCallback = null) {
            _pickCallback = pickCallback;
            _pickType = type;
            
            var queryType = type != null ? type.Name : "";
            SearchService.ShowWindow(
                context: SearchService.CreateContext(id, $"{filterId} "), 
                multiselect: false,
                saveFilters: false
                );
        }

        public static void OpenInterfacePicker(Type type = null, bool allowSceneObjects = false, Action<Object> pickCallback = null) {
            _pickCallback = pickCallback;
            _pickType = type;
            _allowSceneObjects = allowSceneObjects;

            assetProvider = new InterfaceAssetSearchBuilder(
                new BuiltInProviderSearch(SearchService.GetProvider(Commons.ASSET_PROVIDER)),
                type
            ).Build();
            if (allowSceneObjects) {
                sceneProvider = new InterfaceSceneSearchBuilder(
                    new BuiltInProviderSearch(SearchService.GetProvider(Commons.SCENE_PROVIDER)),
                    type
                ).Build();
            }

            var providers = allowSceneObjects
                ? new[] { sceneProvider, assetProvider }
                : new[] { assetProvider };
            var context = SearchService.CreateContext(
                providers,
                searchText: "",
                flags: SearchFlags.Sorted | SearchFlags.OpenPicker
            );

            SearchService.ShowPicker(
                context,
                (item, _) => PickTheObject(item),
                PickTheObject
            );
        }

        //[SearchItemProvider]
        internal static SearchProvider CreateProvider() {
            
            return new SearchProvider(id, name) {
                filterId = filterId,
                
                priority = priority, // put example provider at a low priority

                toObject = ToObject,
                onEnable = OnEnable,
                onDisable = OnDisable,

                fetchItems = (context, items, provider) => FetchItems(context, provider),
                
                fetchThumbnail = (item, context) => AssetDatabase.GetCachedIcon(item.id) as Texture2D,
                
                fetchPreview = (item, context, size, options) => AssetDatabase.GetCachedIcon(item.id) as Texture2D,
                
                // fetchLabel = (item, context) => AssetDatabase.LoadMainAssetAtPath(item.id)?.name,
                
                // fetchDescription = (item, context) => AssetDatabase.LoadMainAssetAtPath(item.id)?.name,

                showDetails = true,

                // Shows handled actions in the preview inspector
                // Shows inspector view in the preview inspector (uses toObject)
                showDetailsOptions = ShowDetailsOptions.Preview, // ShowDetailsOptions.Inspector | ShowDetailsOptions.Actions,
                
                trackSelection = (item, context) => {
                    var asset = AssetDatabase.LoadMainAssetAtPath(item.id);
                    if (asset) {
                        EditorGUIUtility.PingObject(asset.GetInstanceID());
                    }
                    
                    var obj = item.ToObject<GameObject>();
                    if (obj) {
                        EditorGUIUtility.PingObject(obj);
                    }
                },

                // TODO: do we need this?
                startDrag = (item, context) => {
                    var obj = AssetDatabase.LoadMainAssetAtPath(item.id);
                    if (obj != null) {
                        DragAndDrop.PrepareStartDrag();
                        DragAndDrop.objectReferences = new UnityEngine.Object[] { obj };
                        DragAndDrop.StartDrag(item.label);
                    }
                },

                // This provider can be used in the scene and hierarchy contextual search
                isEnabledForContextualSearch = IsEnabledForContextualSearch,
                
                isExplicitProvider = true,
                
            };
        }
        
        static IEnumerable<SearchItem> FetchItems(SearchContext context, SearchProvider provider) {
            Commons.Log("ISP: search text " + context.searchText);

            if (assetProvider == null ||
                (provider.isExplicitProvider && context.filterId != filterId))
                yield break;

            if (_allowSceneObjects && sceneProvider !=null) {
                foreach(var res in SearchInterfacesInScene(_pickType, context.searchQuery)) {
                    if (res != null) {
                        var newItem = CreateItemFrom(res, provider, context, ReferenceSource.SCENE);
                        yield return newItem;
                    } else {
                        yield return null;
                    }
                }    
            }

            foreach(var res in SearchInterfacesInAssets(_pickType, context.searchQuery)) {
                if (res != null) {
                    var newItem = CreateItemFrom(res, provider, context, ReferenceSource.ASSET);
                    yield return newItem;
                } else {
                    yield return null;
                }
            }
        }

        static SearchItem CreateItemFrom(SearchItem item, SearchProvider provider, SearchContext context, ReferenceSource source) {
           return provider.CreateItem(
                context, 
                item.id,
                item.GetLabel(item.context, true), 
                item.GetDescription(item.context, true),
                item.GetThumbnail(item.context, true),
                new Extras(source)
            );
        }

        static ISearchList DoProviderSearch(SearchProvider provider, string filter, string searchPhrase = null) {
            var context = SearchService.CreateContext(
                new[] { provider },
                searchText: $"{filter} {searchPhrase}",
                flags: SearchFlags.Sorted | SearchFlags.OpenPicker
            );
            return SearchService.Request(context);
        }

        /// <summary>
        /// Search for GameObjects in Scene where Components implement or derived from interface(s). If type of the interface is not specified,
        /// will return all GameObjects Components of which implement any interfaces. 
        /// </summary>
        /// <param name="type">Type of the interface looking for</param>
        static IEnumerable<SearchItem> SearchInterfacesInScene(Type type = null, string searchPhrase = null) {
            // TODO: consider restricting type matching only to IHumbleDI
            // TODO: consider restricting the search only to HDIMonoBehaviours

            var results = DoProviderSearch(sceneProvider, Commons.SCENE_SEARCH_FILTER, searchPhrase);
            foreach (var res in results) {
                if (res != null) {
                    var go = res.ToObject<GameObject>();
                    if (!go) continue;
                    
                    var hasInterfaces = go.GetComponents<Component>().Any(c => {
                        var iList = c.GetType().GetInterfaces();
                        return type == null ? iList.Length > 0 : iList.Any(i => i == type);
                    });
                    if (!hasInterfaces) continue;

                    yield return res;
                } else {
                    yield return null;
                }
            }
        }

        /// <summary>
        /// Search assets for ScriptableObjects which implement or derived from interface(s). If type of the interface is not specified,
        /// will return all ScriptableObjects which implement any interfaces. 
        /// </summary>
        /// <param name="type">Type of the interface looking for</param>
        static IEnumerable<SearchItem> SearchInterfacesInAssets(Type type = null, string searchPhrase = null) {
            var results = DoProviderSearch(assetProvider, Commons.ASSET_SEARCH_FILTER, searchPhrase);
            foreach (var res in results) {
                if (res != null) {
                    var so = res.ToObject<ScriptableObject>();
                    if (!so) continue;
                    
                    var iList = so.GetType().GetInterfaces();
                    var hasInterfaces = type == null ? iList.Length > 0 : iList.Any(i => i == type);
                    if (!hasInterfaces) continue;

                    yield return res;
                } else {
                    yield return null;
                }
            }
        }

        static void OnEnable() {
            Debug.Log("InterfaceSearchProvider: On Enable");
            Commons.Log("InterfaceSearchProvider: On Enable");
            sceneProvider = SearchService.GetProvider(Commons.SCENE_PROVIDER);
            assetProvider = SearchService.GetProvider(Commons.ASSET_PROVIDER);
        }

        static void OnDisable() {
            Commons.Log("InterfaceSearchProvider: On Disable");
            sceneProvider = null;
            assetProvider = null;
            
            _pickCallback = null;
            _pickType = null;
        }

        static Object ToObject(SearchItem item, Type type) {
            Commons.Log("ISP ToObject " + item.label + " | " + type.Name);

            if (item.data is not Extras extras) 
                return null;

            switch (extras.source) {
                case ReferenceSource.SCENE:
                    return EditorUtility.InstanceIDToObject(int.Parse(item.id));
                case ReferenceSource.ASSET: 
                    var asset = AssetDatabase.LoadAssetAtPath<Object>(item.id);
                    Commons.Log($"ISP: asset {asset} | item id {item.id}");
                    return asset;
            }
            return null;
        }

        static bool IsEnabledForContextualSearch() {
            return IsFocusedWindowTypeName("SceneView") || IsFocusedWindowTypeName("SceneHierarchyWindow");
        }
        
        static bool IsFocusedWindowTypeName(string focusWindowName) {
            return EditorWindow.focusedWindow != null && EditorWindow.focusedWindow.GetType().ToString().EndsWith("." + focusWindowName);
        }

        static void PickTheObject(SearchItem item) {
            if (_pickCallback == null || item == null) {
                return;
            }
            
            var obj = item.ToObject<Object>();
            var typeStr = obj != null ? obj.GetType().Name : "null";
            Commons.Log($"ISP: picked object is {obj} | type is {typeStr}");
            
            _pickCallback(obj);
        }
        
    }

    
    internal class InterfaceSceneSearchBuilder : AbsSearchProviderBuilder {

        static readonly string ID = "humble_interface_search_scene";
        static readonly string NAME = "Scene Interfaces";
        static readonly string FILTER_ID = "iface-scene:";
        static readonly int PRIORITY = 99991;
            
        BuiltInProviderSearch builtInSearch;
        Type pickType;
        
        public InterfaceSceneSearchBuilder(BuiltInProviderSearch builtInSearch, Type pickType) : base(ID, NAME, FILTER_ID, PRIORITY) {
            this.pickType = pickType;
            this.builtInSearch = builtInSearch;
            
            if (this.pickType == null) {
                throw new ArgumentNullException(nameof(pickType));
            }
            if (this.builtInSearch == null) {
                throw new ArgumentNullException(nameof(builtInSearch));
            }

            toObject = ToObject;
        }

        ~InterfaceSceneSearchBuilder() {
            OnDisable();
        }

        protected override IEnumerable<SearchItem> FetchItems(SearchContext context, SearchProvider provider) {
            Commons.Log("ISP-Scene: fetch : " + context.searchText);
            
            foreach(var res in SearchInterfacesInScene(pickType, context.searchQuery)) {
                if (res != null) {
                    // var newItem = Commons.CreateItemFrom(res, provider, context, ReferenceSource.SCENE);
                    // yield return newItem;
                    // res.data = ReferenceSource.SCENE;
                    yield return res;
                } else {
                    yield return null;
                }
            }
        }
        
        /// <summary>
        /// Search for GameObjects in Scene where Components implement or derived from interface(s). If type of the interface is not specified,
        /// will return all GameObjects Components of which implement any interfaces. 
        /// </summary>
        /// <param name="type">Type of the interface looking for</param>
        IEnumerable<SearchItem> SearchInterfacesInScene(Type type = null, string searchPhrase = null) {
            // TODO: consider restricting type matching only to IHumbleDI
            // TODO: consider restricting the search only to HDIMonoBehaviours

            var results = builtInSearch.Search(Commons.SCENE_SEARCH_FILTER, searchPhrase);
            foreach (var res in results) {
                if (res != null) {
                    var go = res.ToObject<GameObject>();
                    if (!go) continue;
                    
                    var hasInterfaces = go.GetComponents<Component>().Any(c => {
                        var iList = c.GetType().GetInterfaces();
                        return type == null ? iList.Length > 0 : iList.Any(i => i == type);
                    });
                    if (!hasInterfaces) continue;

                    yield return res;
                } else {
                    yield return null;
                }
            }
        }

        protected override void OnEnable() {
            Commons.Log("ISP-Scene: On Enable");
        }
        
        protected override void OnDisable() {
            Commons.Log("ISP-Scene: On Disable");
            builtInSearch = null;
            pickType = null;
        }

        protected override Object ToObject(SearchItem item, Type type) {
            Commons.Log("ISP-Scene ToObject " + item.label + " | " + type.Name);
            return EditorUtility.InstanceIDToObject(int.Parse(item.id));
        }
    }

    internal class InterfaceAssetSearchBuilder : AbsSearchProviderBuilder {

        static readonly string ID = "humble_interface_search_asset";
        static readonly string NAME = "Asset Interfaces";
        static readonly string FILTER_ID = "iface-asset:";
        static readonly int PRIORITY = 99992;
            
        BuiltInProviderSearch builtInSearch;
        Type pickType;
        
        public InterfaceAssetSearchBuilder(BuiltInProviderSearch builtInSearch, Type pickType) : base(ID, NAME, FILTER_ID, PRIORITY) {
            this.pickType = pickType;
            this.builtInSearch = builtInSearch;
            
            if (this.pickType == null) {
                throw new ArgumentNullException(nameof(pickType));
            }
            if (this.builtInSearch == null) {
                throw new ArgumentNullException(nameof(builtInSearch));
            }
        }

        ~InterfaceAssetSearchBuilder() {
            OnDisable();
        }

        protected override IEnumerable<SearchItem> FetchItems(SearchContext context, SearchProvider provider) {
            Commons.Log("ISP-Asset: fetch : " + context.searchText);
            
            foreach(var res in SearchInterfacesInAssets(pickType, context.searchQuery)) {
                if (res != null) {
                    // var newItem = Commons.CreateItemFrom(res, provider, context, ReferenceSource.SCENE);
                    // yield return newItem;
                    // res.data = ReferenceSource.SCENE;
                    yield return res;
                } else {
                    yield return null;
                }
            }
        }
        
        /// <summary>
        /// Search for GameObjects in Scene where Components implement or derived from interface(s). If type of the interface is not specified,
        /// will return all GameObjects Components of which implement any interfaces. 
        /// </summary>
        /// <param name="type">Type of the interface looking for</param>
        IEnumerable<SearchItem> SearchInterfacesInAssets(Type type = null, string searchPhrase = null) {
            var results = builtInSearch.Search(Commons.ASSET_SEARCH_FILTER, searchPhrase);
            foreach (var res in results) {
                if (res != null) {
                    var so = res.ToObject<ScriptableObject>();
                    if (!so) continue;
                    
                    var iList = so.GetType().GetInterfaces();
                    var hasInterfaces = type == null ? iList.Length > 0 : iList.Any(i => i == type);
                    if (!hasInterfaces) continue;

                    yield return res;
                } else {
                    yield return null;
                }
            }
        }

        protected override void OnEnable() {
            Commons.Log("ISP-Asset: On Enable");
        }
        
        protected override void OnDisable() {
            Commons.Log("ISP-Asset: On Disable");
            builtInSearch = null;
            pickType = null;
        }
    }

}
