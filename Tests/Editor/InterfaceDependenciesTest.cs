using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;


namespace LobstersUnited.HumbleDI.Tests {

    public class InterfaceDependenciesTest {

        // ------------------------------------- //
        #region COMMON SETUP
        
            interface IFaceOne { }

            interface IFaceTwo { }

            class IFaceOneImplementation : MonoBehaviour, IFaceOne { }

            class IFaceTwoImplementation : ScriptableObject, IFaceTwo { }

            class MBWithIFaceDeps : MonoBehaviour {
                [SerializeField] InterfaceDependencies iDeps;
                
                IFaceOne first;
                IFaceTwo second;
                
                public void Construct(IFaceOne first, IFaceTwo second) {
                    this.first = first;
                    this.second = second;
                }
            }

            [Serializable]
            class InnerClass {
                [SerializeField] InterfaceDependencies iDeps;
                
                IFaceOne first;
                IFaceTwo second;

                public InnerClass(IFaceOne first, IFaceTwo second) {
                    this.first = first;
                    this.second = second;
                }
            }

            class MBWithLevelOneIFaceDeps : MonoBehaviour {
                [SerializeField] InnerClass inner;
                
                public void Construct(InnerClass inner) {
                    this.inner = inner;
                }
            }

            static readonly string IFACE_ONE_TYPE_NAME = typeof(IFaceOne).AssemblyQualifiedName;
            static readonly string IFACE_TWO_TYPE_NAME = typeof(IFaceTwo).AssemblyQualifiedName;
            
            static readonly FieldInfo innerField = typeof(MBWithLevelOneIFaceDeps).GetField("inner", Utils.ALL_INSTANCE_FIELDS);

            static readonly Type idepsType = typeof(InterfaceDependencies);
            static readonly FieldInfo targetField = idepsType.GetField("target", Utils.ALL_INSTANCE_FIELDS);
            static readonly FieldInfo targetPathField = idepsType.GetField("targetPath", Utils.ALL_INSTANCE_FIELDS);
            static readonly FieldInfo fieldInfosField = idepsType.GetField("fieldInfos", Utils.ALL_INSTANCE_FIELDS);
            static readonly FieldInfo mappedObjectsField = idepsType.GetField("mappedObjects", Utils.ALL_INSTANCE_FIELDS);
            // TODO:
            //static readonly FieldInfo mappedPathsField = idepsType.GetField("mappedPaths", Utils.ALL_INSTANCE_FIELDS); 
            //static readonly FieldInfo mappedSourcesField = idepsType.GetField("mappedSources", Utils.ALL_INSTANCE_FIELDS);
            
            #endregion
        
        [Test]
        public void dummy_test() { }

        public class GetObjectPathsTests {

            [Test]
            public void should_get_null_target_object_path_from_null_ideps_path() {
                var path = InterfaceDependencies.GetTargetObjectPath(null);
                
                Assert.That(path, Is.Null);
            }

            [Test]
            public void should_get_null_target_object_path_from_zero_level_ideps_path() {
                var path = InterfaceDependencies.GetTargetObjectPath("iDeps");
                
                Assert.That(path, Is.Null);
            }
            
            [Test]
            public void should_get_level_one_target_object_path_from_level_two_ideps_path() {
                var path = InterfaceDependencies.GetTargetObjectPath("levelZero.levelOne.iDeps");
                
                Assert.That(path, Is.EqualTo("levelZero.levelOne"));
            }

            [Test]
            public void should_get_level_zero_array_element_target_object_path_from_level_one_ideps_path_in_array_element() {
                var path = InterfaceDependencies.GetTargetObjectPath("levelZero.Array.data[0].iDeps");
                
                Assert.That(path, Is.EqualTo("levelZero.[0]"));
            }
            
            [Test]
            public void should_get_level_one_array_element_target_object_path_from_level_two_ideps_path_in_nested_array_element() {
                var path = InterfaceDependencies.GetTargetObjectPath("levelZero.Array.data[0].levelOne.Array.data[1].iDeps");
                
                Assert.That(path, Is.EqualTo("levelZero.[0].levelOne.[1]"));
            }
        }

        public class GetTargetObjectTests {

            class ParentClass {
                public InnerClass[] innerArr;
            }

            class MBWithArrayOfInnerClass : MonoBehaviour {
                public InnerClass[] innerArr;
            }

            class MBWithListOfInnerClass : MonoBehaviour {
                public List<InnerClass> innerList;
            }

            class MBWithNestedArrayPathToIDeps : MonoBehaviour {
                public ParentClass[] parents;
            }

            [Test]
            public void should_return_null_if_target_is_null() {
                var targetObj = InterfaceDependencies.GetTargetObject(null, "somePath");
                
                Assert.That(targetObj, Is.Null);
            }

            [Test]
            public void should_get_original_target_if_target_path_is_null() {
                var target = A.Component<MBWithIFaceDeps>();
                
                var targetObj = InterfaceDependencies.GetTargetObject(target, null);
                
                Assert.That(targetObj, Is.SameAs(target));
            }
            
            [Test]
            public void should_get_original_target_if_target_path_is_empty() {
                var target = A.Component<MBWithIFaceDeps>();
                
                var targetObj = InterfaceDependencies.GetTargetObject(target, "");

                Assert.That(targetObj, Is.SameAs(target));
            }
            
            [Test]
            public void should_get_inner_target_object_for_level_one_target_path() {
                var target = A.Component<MBWithLevelOneIFaceDeps>();
                var inner = new InnerClass(null, null);
                innerField.SetValue(target, inner);
                
                var targetObj = InterfaceDependencies.GetTargetObject(target, "inner");
                
                Assert.That(targetObj, Is.SameAs(inner));
            }
            
            [Test]
            public void should_get_inner_target_object_if_path_is_a_second_element_of_level_zero_array() {
                var target = A.Component<MBWithArrayOfInnerClass>();
                target.innerArr = new [] {
                    new InnerClass(null, null),
                    new InnerClass(null, null),
                };
                    
                var targetObj = InterfaceDependencies.GetTargetObject(target, "innerArr.[1]");
                
                Assert.That(targetObj, Is.SameAs(target.innerArr[1]));
            }
            
            [Test]
            public void should_get_inner_target_object_if_path_is_a_second_element_of_level_zero_list() {
                var target = A.Component<MBWithListOfInnerClass>();
                target.innerList = new List<InnerClass>();
                var innerOne = new InnerClass(null, null);
                var innerTwo = new InnerClass(null, null);
                target.innerList.Add(innerOne);
                target.innerList.Add(innerTwo);
                    
                var targetObj = InterfaceDependencies.GetTargetObject(target, "innerList.[0]");
                
                Assert.That(targetObj, Is.SameAs(innerOne));
            }
            
            [Test]
            public void should_get_inner_target_object_if_path_is_through_nested_arrays() {
                var target = A.Component<MBWithNestedArrayPathToIDeps>();
                target.parents = new[] {
                    new ParentClass { innerArr = new[] { new InnerClass(null, null), new InnerClass(null, null) } },
                    new ParentClass { innerArr = new[] { new InnerClass(null, null) } },
                };
                
                var targetObj = InterfaceDependencies.GetTargetObject(target, "parents.[1].innerArr.[0]");
                
                Assert.That(targetObj, Is.SameAs(target.parents[1].innerArr[0]));
            }
        }

        public class GetCompatibleFieldsTests {

            class HasSomeIFaceFields {
                IFaceOne privateField;
                public IFaceTwo publicField;
                protected IFaceOne protectedField;
                public Transform nonIFaceField;
            }

            class HasArrayOfIFaceFields {
                IFaceOne first;
                IFaceTwo[] secondArr;
                public Transform nonIFaceField;
            }

            class HasListOfIFaceFields {
                IFaceOne first;
                List<IFaceTwo> secondList;
                IEnumerable<IFaceOne> firstEnumerable;
            }

            [Test]
            public void should_get_all_iface_fields_including_non_public() {
                var type = typeof(HasSomeIFaceFields);

                var fields = InterfaceDependencies.GetCompatibleFields(type);

                var fieldNames = fields.Select(f => f.Name);
                Assert.That(fieldNames, Is.EqualTo(new [] { "privateField", "publicField", "protectedField" }));
            }

            [Test]
            public void should_get_fields_that_are_arrays_of_iface_type() {
                var type = typeof(HasArrayOfIFaceFields);

                var fields = InterfaceDependencies.GetCompatibleFields(type);

                var fieldNames = fields.Select(f => f.Name);
                Assert.That(fieldNames, Is.EqualTo(new [] { "first", "secondArr" }));
            }

            [Test]
            public void should_get_fields_that_are_lists_of_iface_type() {
                var type = typeof(HasListOfIFaceFields);

                var fields = InterfaceDependencies.GetCompatibleFields(type);
                
                var fieldNames = fields.Select(f => f.Name);
                Assert.That(fieldNames, Is.EqualTo(new [] { "first", "secondList" }));
            }

        }

        public class SerializationTests {
            
            // ------------------------------------------ //
            #region SETUP

            static readonly Type mbWithDepsType = typeof(MBWithIFaceDeps);
            static readonly FieldInfo l0_iDepsField = mbWithDepsType.GetField("iDeps", Utils.ALL_INSTANCE_FIELDS);
            static readonly FieldInfo l0_firstField = mbWithDepsType.GetField("first", Utils.ALL_INSTANCE_FIELDS);
            static readonly FieldInfo l0_secondField = mbWithDepsType.GetField("second", Utils.ALL_INSTANCE_FIELDS);
            
            static readonly Type innerClassType = typeof(InnerClass);
            static readonly FieldInfo l1_iDepsField = innerClassType.GetField("iDeps", Utils.ALL_INSTANCE_FIELDS);
            static readonly FieldInfo l1_firstField = innerClassType.GetField("first", Utils.ALL_INSTANCE_FIELDS);
            static readonly FieldInfo l1_secondField = innerClassType.GetField("second", Utils.ALL_INSTANCE_FIELDS);

            // Emulating what Unity would do when deserializing serializable datas
            static void SetSerializedData(object iDeps, Object target, string targetPath, Object[] mappedObjects, int validationLevel = 0) {
                targetField.SetValue(iDeps, target);
                targetPathField.SetValue(iDeps, targetPath);
                
                fieldInfosField.SetValue(iDeps, new [] {
                    new IFaceFieldInfo("first", IFACE_ONE_TYPE_NAME, IFaceFieldCategory.SINGULAR),
                    new IFaceFieldInfo("second", IFACE_TWO_TYPE_NAME, IFaceFieldCategory.SINGULAR)
                });
                mappedObjectsField.SetValue(iDeps, mappedObjects);
            }
            
            #endregion

            // ----------------------------------------- //
            
            [Test]
            public void should_have_empty_serialization_data() {
                var iDeps = new InterfaceDependencies(null);
                
                Assert.That(targetField.GetValue(iDeps), Is.EqualTo(null));
                Assert.That(targetPathField.GetValue(iDeps), Is.EqualTo(null));
                Assert.That(fieldInfosField.GetValue(iDeps), Is.EqualTo(null));
                Assert.That(mappedObjectsField.GetValue(iDeps), Is.EqualTo(null));
            }

            [Test]
            public void should_set_target_object_through_constructor() {
                var mb = A.Component<MBWithIFaceDeps>();
                
                var iDeps = new InterfaceDependencies(mb);
                
                Assert.That(targetField.GetValue(iDeps), Is.EqualTo(mb));
                Assert.That(targetPathField.GetValue(iDeps), Is.EqualTo(null));
            }

            [Test]
            public void should_set_target_object_through_constructor_at_level_two() {
                var mb = A.Component<MBWithIFaceDeps>();
                
                var iDeps = new InterfaceDependencies(mb, "levelOne.levelTwo.iDeps");
                
                Assert.That(targetField.GetValue(iDeps), Is.EqualTo(mb));
                Assert.That(targetPathField.GetValue(iDeps), Is.EqualTo("levelOne.levelTwo"));
            }

            [Test]
            public void should_set_target_object_through_setter() {
                var mb = A.Component<MBWithIFaceDeps>();
                var iDeps = new InterfaceDependencies(null);
                
                iDeps.SetTarget(mb, "iDeps");
                
                Assert.That(targetField.GetValue(iDeps), Is.EqualTo(mb));
                Assert.That(targetPathField.GetValue(iDeps), Is.EqualTo(null));
            }
            
            [Test]
            public void should_set_target_object_through_setter_at_level_two() {
                var mb = A.Component<MBWithIFaceDeps>();
                var iDeps = new InterfaceDependencies(null);
                
                iDeps.SetTarget(mb, "levelOne.levelTwo.iDeps");
                
                Assert.That(targetField.GetValue(iDeps), Is.EqualTo(mb));
                Assert.That(targetPathField.GetValue(iDeps), Is.EqualTo("levelOne.levelTwo"));
            }

            [Test]
            public void should_not_serialize_if_target_is_null() {
                var iDeps = new InterfaceDependencies(null);
                
                Assert.DoesNotThrow(() => iDeps.OnBeforeSerialize());
                
                Assert.That(targetField.GetValue(iDeps), Is.EqualTo(null));
                Assert.That(targetPathField.GetValue(iDeps), Is.EqualTo(null));
                Assert.That(fieldInfosField.GetValue(iDeps), Is.EqualTo(null));
                Assert.That(mappedObjectsField.GetValue(iDeps), Is.EqualTo(null));
            }

            [Test]
            public void should_serialize_interface_fields() {
                var first = A.Component<IFaceOneImplementation>();
                var second = A.SO<IFaceTwoImplementation>();
                var target = A.Component<MBWithIFaceDeps>();
                target.Construct(first, second);
                // inject and initialize interface dependencies object (normally done by the drawer)
                var iDeps = new InterfaceDependencies(target, "iDeps"); 
                l0_iDepsField.SetValue(target, iDeps);
                
                iDeps.OnBeforeSerialize();

                Assert.That(targetField.GetValue(iDeps), Is.EqualTo(target));
                Assert.That(targetPathField.GetValue(iDeps), Is.EqualTo(null));
                Assert.That(fieldInfosField.GetValue(iDeps), Is.EqualTo(new [] {
                    new IFaceFieldInfo("first", IFACE_ONE_TYPE_NAME, IFaceFieldCategory.SINGULAR),
                    new IFaceFieldInfo("second", IFACE_TWO_TYPE_NAME, IFaceFieldCategory.SINGULAR),
                }));
                Assert.That(mappedObjectsField.GetValue(iDeps), Is.EqualTo(new Object[] { first, second }));
            }

            [Test]
            public void should_serialize_interface_fields_nested_at_level_one() {
                var first = A.Component<IFaceOneImplementation>();
                var second = A.SO<IFaceTwoImplementation>();
                var inner = new InnerClass(first, second);
                var target = A.Component<MBWithLevelOneIFaceDeps>();
                target.Construct(inner);
                // inject and initialize interface dependencies object (normally done by the drawer)
                var iDeps = new InterfaceDependencies(target, "inner.iDeps");
                l1_iDepsField.SetValue(inner, iDeps);
                
                iDeps.OnBeforeSerialize();

                Assert.That(targetField.GetValue(iDeps), Is.EqualTo(target));
                Assert.That(targetPathField.GetValue(iDeps), Is.EqualTo("inner"));
                Assert.That(fieldInfosField.GetValue(iDeps), Is.EqualTo(new [] {
                    new IFaceFieldInfo("first", IFACE_ONE_TYPE_NAME, IFaceFieldCategory.SINGULAR),
                    new IFaceFieldInfo("second", IFACE_TWO_TYPE_NAME, IFaceFieldCategory.SINGULAR),
                }));
                Assert.That(mappedObjectsField.GetValue(iDeps), Is.EqualTo(new Object[] { first, second }));
            }

            [Test]
            public void should_not_deserialize_if_target_is_null() {
                var target = A.Component<MBWithIFaceDeps>();
                // inject and initialize interface dependencies object (when deserializing, normally done by the Unity itself)
                var iDeps = new InterfaceDependencies(null);
                var mappedObjects = new Object[] {
                    A.Component<IFaceOneImplementation>(),
                    A.SO<IFaceTwoImplementation>()
                };
                // set target to null
                SetSerializedData(iDeps, null, null, mappedObjects);
                
                Assert.DoesNotThrow(() => iDeps.OnAfterDeserialize());

                Assert.That(l0_firstField.GetValue(target), Is.Null);
                Assert.That(l0_secondField.GetValue(target), Is.Null);
            }
            
            [Test]
            public void should_deserialize_interface_fields() {
                var target = A.Component<MBWithIFaceDeps>();
                // inject and initialize interface dependencies object (when deserializing, normally done by the Unity itself)
                var iDeps = new InterfaceDependencies(null);
                var mappedObjects = new Object[] {
                    A.Component<IFaceOneImplementation>(),
                    A.SO<IFaceTwoImplementation>()
                };
                SetSerializedData(iDeps, target, null, mappedObjects);
                l0_iDepsField.SetValue(target, iDeps);

                iDeps.OnAfterDeserialize();
                
                Assert.That(l0_firstField.GetValue(target), Is.EqualTo(mappedObjects[0]));
                Assert.That(l0_secondField.GetValue(target), Is.EqualTo(mappedObjects[1]));
            }
            
            [Test]
            public void should_deserialize_interface_fields_nested_at_level_one() {
                var target = A.Component<MBWithLevelOneIFaceDeps>();
                // inject the default "deserialized" InnerClass object (when deserializing, normally done by the Unity itself)
                var inner = new InnerClass(null, null);
                innerField.SetValue(target, inner);
                // inject and initialize interface dependencies object under target Unity Object (when deserializing, normally done by the Unity itself)
                var iDeps = new InterfaceDependencies(null);
                var mappedObjects = new Object[] {
                    A.Component<IFaceOneImplementation>(),
                    A.SO<IFaceTwoImplementation>()
                };
                SetSerializedData(iDeps, target, "inner", mappedObjects);
                l1_iDepsField.SetValue(inner, iDeps);

                iDeps.OnAfterDeserialize();
                
                Assert.That(l1_firstField.GetValue(inner), Is.EqualTo(mappedObjects[0]));
                Assert.That(l1_secondField.GetValue(inner), Is.EqualTo(mappedObjects[1]));
            }
            
        }


        public class IFaceCollectionTests {

            class MBWithArrayOfIFaceField : MonoBehaviour {
                
                [SerializeField] InterfaceDependencies iDeps;
                
                public IFaceOne[] firstArr;

                public IFaceTwo second;
                
                public void Construct(IFaceOne[] arr, IFaceTwo second) {
                    firstArr = arr;
                    this.second = second;
                }
            }
            
            class MBWithListOfIFaceField : MonoBehaviour {
                
                [SerializeField] InterfaceDependencies iDeps;
                
                public List<IFaceOne> firstList;

                public IFaceTwo second;
                
                public void Construct(List<IFaceOne> list, IFaceTwo second) {
                    firstList = list;
                    this.second = second;
                }
            }

            static readonly string IFACE_ONE_ARRAY_TYPE_NAME = typeof(IFaceOne[]).AssemblyQualifiedName;
            static readonly string IFACE_ONE_LIST_TYPE_NAME = typeof(List<IFaceOne>).AssemblyQualifiedName;
            
            static readonly Type mbWithArraysType = typeof(MBWithArrayOfIFaceField);
            static readonly FieldInfo iDepsField_Arrays = mbWithArraysType.GetField("iDeps", Utils.ALL_INSTANCE_FIELDS);
            
            static readonly Type mbWithListsType = typeof(MBWithListOfIFaceField);
            static readonly FieldInfo iDepsField_Lists = mbWithListsType.GetField("iDeps", Utils.ALL_INSTANCE_FIELDS);
            

            class TestClass {
                public IFaceOne[] arrField;
                public List<IFaceOne> listField;

                public TestClass() {
                    arrField = new[] {
                        A.Component<IFaceOneImplementation>(),
                        A.Component<IFaceOneImplementation>(),
                    };
                    listField = new List<IFaceOne>();
                    listField.Add(A.Component<IFaceOneImplementation>());
                }
            }
            
            // Emulating what Unity would do when deserializing serializable datas
            static void SetSerializedDataWithArrays(object iDeps, Object target, string targetPath, Object[] mappedObjects) {
                targetField.SetValue(iDeps, target);
                targetPathField.SetValue(iDeps, targetPath);
                
                fieldInfosField.SetValue(iDeps, new [] {
                    new IFaceFieldInfo("firstArr", IFACE_ONE_ARRAY_TYPE_NAME, IFaceFieldCategory.ARRAY, 2),
                    new IFaceFieldInfo("second", IFACE_TWO_TYPE_NAME, IFaceFieldCategory.SINGULAR),
                });
                mappedObjectsField.SetValue(iDeps, mappedObjects);
            }
            
            static void SetSerializedDataWithLists(object iDeps, Object target, string targetPath, Object[] mappedObjects) {
                targetField.SetValue(iDeps, target);
                targetPathField.SetValue(iDeps, targetPath);
                
                fieldInfosField.SetValue(iDeps, new [] {
                    new IFaceFieldInfo("firstList", IFACE_ONE_LIST_TYPE_NAME, IFaceFieldCategory.LIST, 2),
                    new IFaceFieldInfo("second", IFACE_TWO_TYPE_NAME, IFaceFieldCategory.SINGULAR),
                });
                mappedObjectsField.SetValue(iDeps, mappedObjects);
            }

            [Test]
            public void test() {
                var obj = new TestClass();
                var type = typeof(TestClass);
                var arrFieldField = type.GetField("arrField", Utils.ALL_INSTANCE_FIELDS);
                var listFieldField = type.GetField("listField", Utils.ALL_INSTANCE_FIELDS);

                var arrHash1 = obj.arrField[0].GetHashCode();
                var arrHash2 = obj.arrField[1].GetHashCode();
                var arr = arrFieldField.GetValue(obj) as Array;
                var count = arr.Length;
                arr.SetValue(A.Component<IFaceOneImplementation>(), 1); 
                var newArrHash2 = obj.arrField[1].GetHashCode();

                var arrItemProp = arrFieldField.FieldType.GetProperty("Item");

                var listHash1 = obj.listField[0].GetHashCode();
                var list = listFieldField.GetValue(obj);
                var countProp = list.GetType().GetProperty("Count");
                count = (int) countProp.GetValue(list);
                var itemProp = list.GetType().GetProperty("Item");
                itemProp.SetValue(list, A.Component<IFaceOneImplementation>(), new object[] { 0 });
                var newListHash1 = obj.listField[0].GetHashCode();

                var arrType = typeof(IFaceOne[]);
                var newArr = (Array) Activator.CreateInstance(arrType, 3);

                var listType = typeof(List<IFaceOne>);
                var newList = Activator.CreateInstance(listType, 2);  
                
                Debug.Log(arr);
            }

            [Test]
            public void should_serialize_iface_arrays() {
                var target = A.Component<MBWithArrayOfIFaceField>();
                var arrItem0 = A.Component<IFaceOneImplementation>();
                var arrItem1 = A.Component<IFaceOneImplementation>();
                var arr = new IFaceOne[] { arrItem0, arrItem1 };
                var second = A.SO<IFaceTwoImplementation>();
                target.Construct(arr, second);
                // inject and initialize interface dependencies object (normally done by the drawer)
                var iDeps = new InterfaceDependencies(target, "iDeps");
                iDepsField_Arrays.SetValue(target, iDeps);
                
                iDeps.OnBeforeSerialize();
                
                Assert.That(targetField.GetValue(iDeps), Is.EqualTo(target));
                Assert.That(targetPathField.GetValue(iDeps), Is.EqualTo(null));
                Assert.That(fieldInfosField.GetValue(iDeps), Is.EqualTo(new [] {
                    new IFaceFieldInfo("firstArr", IFACE_ONE_ARRAY_TYPE_NAME, IFaceFieldCategory.ARRAY, 2),
                    new IFaceFieldInfo("second", IFACE_TWO_TYPE_NAME, IFaceFieldCategory.SINGULAR),
                }));
                Assert.That(mappedObjectsField.GetValue(iDeps), Is.EqualTo(new Object[] {
                    arrItem0,
                    arrItem1,
                    second
                }));
            }
            
            [Test]
            public void should_serialize_iface_lists() {
                var target = A.Component<MBWithListOfIFaceField>();
                var listItem0 = A.Component<IFaceOneImplementation>();
                var listItem1 = A.Component<IFaceOneImplementation>();
                var list = new List<IFaceOne> { listItem0, listItem1 };
                var second = A.SO<IFaceTwoImplementation>();
                target.Construct(list, second);
                // inject and initialize interface dependencies object (normally done by the drawer)
                var iDeps = new InterfaceDependencies(target, "iDeps");
                iDepsField_Lists.SetValue(target, iDeps);
                
                iDeps.OnBeforeSerialize();
                
                Assert.That(targetField.GetValue(iDeps), Is.EqualTo(target));
                Assert.That(targetPathField.GetValue(iDeps), Is.EqualTo(null));
                Assert.That(fieldInfosField.GetValue(iDeps), Is.EqualTo(new [] {
                    new IFaceFieldInfo("firstList", IFACE_ONE_LIST_TYPE_NAME, IFaceFieldCategory.LIST, 2),
                    new IFaceFieldInfo("second", IFACE_TWO_TYPE_NAME, IFaceFieldCategory.SINGULAR),
                }));
                Assert.That(mappedObjectsField.GetValue(iDeps), Is.EqualTo(new Object[] {
                    listItem0,
                    listItem1,
                    second
                }));
            }
            
            [Test]
            public void should_deserialize_iface_arrays() {
                var target = A.Component<MBWithArrayOfIFaceField>();
                var mappedObjects = new Object[] {
                    A.Component<IFaceOneImplementation>(),
                    A.Component<IFaceOneImplementation>(),
                    A.SO<IFaceTwoImplementation>()
                };
                // inject and initialize interface dependencies object (normally done by the drawer)
                var iDeps = new InterfaceDependencies(null);
                SetSerializedDataWithArrays(iDeps, target, null, mappedObjects);
                iDepsField_Arrays.SetValue(target, iDeps);
                
                iDeps.OnAfterDeserialize();
                
                Assert.That(target.firstArr.Length, Is.EqualTo(2));
                Assert.That(target.firstArr[0], Is.EqualTo(mappedObjects[0]));
                Assert.That(target.firstArr[1], Is.EqualTo(mappedObjects[1]));
                Assert.That(target.second, Is.EqualTo(mappedObjects[2]));
            }
            
            [Test]
            public void should_deserialize_iface_lists() {
                var target = A.Component<MBWithListOfIFaceField>();
                var mappedObjects = new Object[] {
                    A.Component<IFaceOneImplementation>(),
                    A.Component<IFaceOneImplementation>(),
                    A.SO<IFaceTwoImplementation>()
                };
                // inject and initialize interface dependencies object (normally done by the drawer)
                var iDeps = new InterfaceDependencies(null);
                SetSerializedDataWithLists(iDeps, target, null, mappedObjects);
                iDepsField_Lists.SetValue(target, iDeps);
                
                iDeps.OnAfterDeserialize();
                
                Assert.That(target.firstList.Count, Is.EqualTo(2));
                Assert.That(target.firstList[0], Is.EqualTo(mappedObjects[0]));
                Assert.That(target.firstList[1], Is.EqualTo(mappedObjects[1]));
                Assert.That(target.second, Is.EqualTo(mappedObjects[2]));
            }
        }
    }
}
